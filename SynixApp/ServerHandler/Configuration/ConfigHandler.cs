// ============================================================================
// PROJECT: Synix Game Server Control Panel
// AUTHOR: Jason Turner (ubidzz)
// COPYRIGHT: © 2026 All Rights Reserved.
//
// LEGAL NOTICE:
// This source code is proprietary and confidential.
// 1. Permission is granted for PERSONAL, NON-COMMERCIAL use only.
// 2. You may modify this code for your own use, but you may NOT redistribute,
//    rebrand, or sell this code or derivative works without written consent.
// 3. The "Synix" brand and logic remain the property of Jason Turner.
// ============================================================================
using System.Globalization;
using System.Net;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	public enum ConfigFormat
	{
		StandardINI = 0,
		XML = 2,
		JSON = 3,
		Space = 4,
		SII = 5,
		YAML = 6
	}

	public enum ConfigValueType
	{
		Text,
		Number,
		Boolean,
		Secret,
		Null
	}

	public sealed class ConfigLine
	{
		public string Id { get; set; } = string.Empty;
		public string Key { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public string Section { get; set; } = string.Empty;
		public string Value { get; set; } = string.Empty;
		public string OriginalValue { get; set; } = string.Empty;
		public bool HasOriginalValue { get; set; }
		public ConfigValueType Type { get; set; } = ConfigValueType.Text;
	}

	public static partial class ConfigHandler
	{
		internal static bool TryGetFormatFromPath(
			string path,
			out ConfigFormat format)
		{
			format = Path.GetExtension(path).ToLowerInvariant() switch
			{
				".json" or ".eco" => ConfigFormat.JSON,
				".xml" => ConfigFormat.XML,
				".sii" => ConfigFormat.SII,
				".yaml" or ".yml" => ConfigFormat.YAML,
				".ini" or ".cfg" or ".conf" or ".properties" =>
					ConfigFormat.StandardINI,
				_ => (ConfigFormat)(-1)
			};

			return (int)format >= 0;
		}

		private enum ScalarStyle
		{
			Raw,
			QuotedSingle,
			QuotedDouble,
			JsonString,
			JsonNumber,
			JsonBoolean,
			JsonNull,
			XmlAttribute,
			XmlText,
			XmlCData,
			YamlPlain,
			YamlSingle,
			YamlDouble
		}

		private sealed class ParsedValue
		{
			public string Id { get; init; } = string.Empty;
			public string Key { get; init; } = string.Empty;
			public string Path { get; init; } = string.Empty;
			public string Section { get; init; } = string.Empty;
			public string Value { get; init; } = string.Empty;
			public string OriginalToken { get; init; } = string.Empty;
			public ConfigValueType Type { get; init; }
			public ScalarStyle Style { get; init; }
			public int Start { get; init; }
			public int Length { get; init; }
			public bool IsCompositeValue { get; init; }
		}

		private sealed class ParsedDocument
		{
			public List<ParsedValue> Values { get; } = new();
		}

		private sealed class Replacement
		{
			public int Start { get; init; }
			public int Length { get; init; }
			public string ExpectedOriginalToken { get; init; } = string.Empty;
			public string Value { get; init; } = string.Empty;
		}

		private sealed class IdentityBuilder
		{
			private readonly Dictionary<string, int> _occurrences =
				new(StringComparer.Ordinal);

			public string Create(string formatPrefix, string logicalPath)
			{
				_occurrences.TryGetValue(logicalPath, out int occurrence);
				_occurrences[logicalPath] = occurrence + 1;

				string encodedPath = Convert.ToBase64String(
					Encoding.UTF8.GetBytes(logicalPath));
				return $"{formatPrefix}:{encodedPath}:{occurrence}";
			}
		}

		private sealed class XmlElementFrame
		{
			public string NumericPath { get; init; } = string.Empty;
			public string DisplayPath { get; init; } = string.Empty;
			public int ContentStart { get; init; }
			public int NextChildIndex { get; set; }
			public bool HasChildElements { get; set; }
			public bool HasExplicitTextEntry { get; set; }
			public bool ContainsOtherMarkup { get; set; }
		}

		private sealed class XmlAttributeToken
		{
			public string Name { get; init; } = string.Empty;
			public string Value { get; init; } = string.Empty;
			public int ValueStart { get; init; }
			public int ValueLength { get; init; }
		}

		public static List<ConfigLine> LoadConfig(string path, ConfigFormat format)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException(LocalizationManager.Get(
					"Configuration.Editor.Error.SinglePathRequired"), nameof(path));
			}

			if (!File.Exists(path))
			{
				return new List<ConfigLine>();
			}

			ConfigurationTextSnapshot snapshot = ConfigurationTextSnapshot.Read(path);
			return LoadConfigText(snapshot.Text, format);
		}

		internal static List<ConfigLine> LoadConfigText(
			string text,
			ConfigFormat format)
		{
			ArgumentNullException.ThrowIfNull(text);
			ParsedDocument document = ParseDocument(text, format);
			return document.Values.Select(value => new ConfigLine
			{
				Id = value.Id,
				Key = value.Key,
				Path = value.Path,
				Section = value.Section,
				Value = value.Value,
				OriginalValue = value.Value,
				HasOriginalValue = true,
				Type = value.Type
			}).ToList();
		}


		public static string CreatePreview(
			string path,
			IReadOnlyCollection<ConfigLine> data,
			ConfigFormat format)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.FileNotFound"),
					path);
			}

			ConfigurationTextSnapshot snapshot = ConfigurationTextSnapshot.Read(path);
			return BuildUpdatedText(snapshot.Text, data, format);
		}

		public static void SaveConfig(
			string path,
			IReadOnlyCollection<ConfigLine> data,
			ConfigFormat format)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.FileNotFound"),
					path);
			}

			ConfigurationTextSnapshot snapshot = ConfigurationTextSnapshot.Read(path);
			string updatedText = BuildUpdatedText(snapshot.Text, data, format);
			if (string.Equals(updatedText, snapshot.Text, StringComparison.Ordinal))
			{
				return;
			}

			ConfigurationFileWriter.WriteAtomically(path, snapshot.Encode(updatedText));
		}


		public static string GetFormatDisplayName(ConfigFormat format)
		{
			return format switch
			{
				ConfigFormat.JSON => "JSON",
				ConfigFormat.XML => "XML",
				ConfigFormat.Space => "SPACE",
				ConfigFormat.SII => "SII",
				ConfigFormat.YAML => "YAML",
				_ => "INI"
			};
		}


		private static ParsedDocument ParseDocument(string text, ConfigFormat format)
		{
			return format switch
			{
				ConfigFormat.StandardINI => ParseIniDocument(text),
				ConfigFormat.JSON => new JsonConfigScanner(text).Parse(),
				ConfigFormat.XML => ParseXmlDocument(text),
				ConfigFormat.Space => ParseSpaceDocument(text),
				ConfigFormat.SII => ParseSiiDocument(text),
				ConfigFormat.YAML => new YamlConfigScanner(text).Parse(),
				_ => throw new NotSupportedException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.FormatUnsupported",
						format))
			};
		}

		private static ConfigValueType DetectValueType(string key, string value)
		{
			if (IsSensitiveKey(key))
			{
				return ConfigValueType.Secret;
			}

			if (TryParseBoolean(value, out _))
			{
				return ConfigValueType.Boolean;
			}

			if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
			{
				return ConfigValueType.Null;
			}

			if (IsValidNumber(value, false))
			{
				return ConfigValueType.Number;
			}

			return ConfigValueType.Text;
		}

		private static bool TryParseBoolean(string value, out bool result)
		{
			return bool.TryParse(value.Trim(), out result);
		}

		private static bool IsSensitiveKey(string key)
		{
			string normalized = key.Replace("_", string.Empty)
				.Replace("-", string.Empty)
				.Replace(".", string.Empty)
				.ToLowerInvariant();
			if (normalized == "bsavepassword")
			{
				return false;
			}

			return normalized.Contains("password") ||
				normalized.EndsWith("pwd", StringComparison.Ordinal) ||
				normalized.Contains("passwd") ||
				normalized.Contains("secret") ||
				normalized.Contains("token") ||
				normalized.Contains("apikey") ||
				normalized.Contains("webhook");
		}

		private static string GetParentPath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return string.Empty;
			}

			int dotIndex = path.LastIndexOf('.');
			int arrayIndex = path.LastIndexOf('[');
			int splitIndex = Math.Max(dotIndex, arrayIndex);
			return splitIndex > 0 ? path[..splitIndex] : string.Empty;
		}

		private static string GetLeafName(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return string.Empty;
			}

			int dotIndex = path.LastIndexOf('.');
			return dotIndex >= 0 && dotIndex + 1 < path.Length
				? path[(dotIndex + 1)..]
				: path;
		}
	}
}
