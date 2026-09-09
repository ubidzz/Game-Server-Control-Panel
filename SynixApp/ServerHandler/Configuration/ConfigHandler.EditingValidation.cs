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
	public static partial class ConfigHandler
	{
		private static string BuildUpdatedText(
			string originalText,
			IReadOnlyCollection<ConfigLine> data,
			ConfigFormat format)
		{
			ParsedDocument originalDocument = ParseDocument(originalText, format);
			Dictionary<string, ConfigLine> updatesById = data
				.Where(item => !string.IsNullOrWhiteSpace(item.Id))
				.GroupBy(item => item.Id, StringComparer.Ordinal)
				.ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);

			Dictionary<string, List<ConfigLine>> legacyUpdatesByKey = data
				.Where(item => string.IsNullOrWhiteSpace(item.Id))
				.GroupBy(item => item.Key, StringComparer.Ordinal)
				.ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

			List<Replacement> replacements = new();
			Dictionary<string, string> expectedValuesById =
				new(StringComparer.Ordinal);
			HashSet<string> unmatchedChangedIds = updatesById.Values
				.Where(UserChangedValue)
				.Select(item => item.Id)
				.ToHashSet(StringComparer.Ordinal);

			foreach (ParsedValue sourceValue in originalDocument.Values)
			{
				ConfigLine? updatedValue = null;
				bool matchedById = updatesById.TryGetValue(sourceValue.Id, out updatedValue);
				if (!matchedById &&
					legacyUpdatesByKey.TryGetValue(sourceValue.Key, out List<ConfigLine>? legacyMatches) &&
					legacyMatches.Count == 1)
				{
					updatedValue = legacyMatches[0];
				}

				if (updatedValue == null || !UserChangedValue(updatedValue))
				{
					continue;
				}

				if (matchedById &&
					(!string.Equals(sourceValue.Key, updatedValue.Key, StringComparison.Ordinal) ||
					 !string.Equals(sourceValue.Path, updatedValue.Path, StringComparison.Ordinal)))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.LocationChanged",
							updatedValue.Key));
				}

				unmatchedChangedIds.Remove(sourceValue.Id);
				if (ValuesAreEquivalent(sourceValue, updatedValue.Value))
				{
					continue;
				}

				if (updatedValue.HasOriginalValue &&
					!ValuesAreEquivalent(sourceValue, updatedValue.OriginalValue))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.DiskValueChanged",
							sourceValue.Key));
				}

				string replacementValue = FormatReplacement(
					sourceValue,
					updatedValue.Value,
					format);
				replacements.Add(new Replacement
				{
					Start = sourceValue.Start,
					Length = sourceValue.Length,
					ExpectedOriginalToken = sourceValue.OriginalToken,
					Value = replacementValue
				});
				expectedValuesById[sourceValue.Id] = NormalizeExpectedValue(
					sourceValue,
					updatedValue.Value);
			}

			if (unmatchedChangedIds.Count > 0)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.SettingsMissing"));
			}

			string updatedText = ApplyLexicalSpanReplacements(
				originalText,
				replacements);
			VerifyLexicalPreservation(originalText, updatedText, replacements);
			ValidateUpdatedDocument(
				originalDocument,
				updatedText,
				format,
				expectedValuesById);
			return updatedText;
		}

		private static bool ValuesAreEquivalent(ParsedValue source, string updatedValue)
		{
			if (source.Type == ConfigValueType.Boolean &&
				TryParseBoolean(source.Value, out bool sourceBoolean) &&
				TryParseBoolean(updatedValue, out bool updatedBoolean))
			{
				return sourceBoolean == updatedBoolean;
			}

			return string.Equals(source.Value, updatedValue, StringComparison.Ordinal);
		}

		private static bool UserChangedValue(ConfigLine value)
		{
			if (!value.HasOriginalValue)
			{
				return true;
			}

			if (value.Type == ConfigValueType.Boolean &&
				TryParseBoolean(value.OriginalValue, out bool originalBoolean) &&
				TryParseBoolean(value.Value, out bool currentBoolean))
			{
				return originalBoolean != currentBoolean;
			}

			return !string.Equals(
				value.OriginalValue,
				value.Value,
				StringComparison.Ordinal);
		}

		private static string NormalizeExpectedValue(
			ParsedValue source,
			string requestedValue)
		{
			string value = requestedValue ?? string.Empty;
			if (source.Type == ConfigValueType.Boolean &&
				TryParseBoolean(value, out bool booleanValue))
			{
				return booleanValue ? "True" : "False";
			}

			if (source.Type == ConfigValueType.Number)
			{
				return value.Trim();
			}

			return source.Style == ScalarStyle.JsonNull &&
				value.Equals("null", StringComparison.OrdinalIgnoreCase)
				? "null"
				: value;
		}

		private static string ApplyLexicalSpanReplacements(
			string originalText,
			List<Replacement> replacements)
		{
			if (replacements.Count == 0)
			{
				return originalText;
			}

			List<Replacement> ordered = replacements
				.OrderByDescending(item => item.Start)
				.ToList();
			int previousStart = originalText.Length + 1;
			StringBuilder builder = new(originalText);

			foreach (Replacement replacement in ordered)
			{
				if (replacement.Start < 0 ||
					replacement.Length < 0 ||
					replacement.Start + replacement.Length > originalText.Length)
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.SourceMapping"));
				}

				if (replacement.ExpectedOriginalToken.Length != replacement.Length ||
					!originalText.AsSpan(replacement.Start, replacement.Length)
						.SequenceEqual(replacement.ExpectedOriginalToken.AsSpan()))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.SourceSpanChanged"));
				}

				if (replacement.Start + replacement.Length > previousStart)
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.OverlappingValues"));
				}

				builder.Remove(replacement.Start, replacement.Length);
				builder.Insert(replacement.Start, replacement.Value);
				previousStart = replacement.Start;
			}

			return builder.ToString();
		}

		private static void VerifyLexicalPreservation(
			string originalText,
			string updatedText,
			IReadOnlyCollection<Replacement> replacements)
		{
			if (replacements.Count == 0)
			{
				if (!string.Equals(originalText, updatedText, StringComparison.Ordinal))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.UnrequestedChange"));
				}
				return;
			}

			List<Replacement> ordered = replacements
				.OrderBy(item => item.Start)
				.ToList();
			int originalCursor = 0;
			int updatedCursor = 0;

			foreach (Replacement replacement in ordered)
			{
				int unchangedLength = replacement.Start - originalCursor;
				if (unchangedLength < 0 ||
					updatedCursor + unchangedLength > updatedText.Length ||
					!originalText.AsSpan(originalCursor, unchangedLength)
						.SequenceEqual(updatedText.AsSpan(updatedCursor, unchangedLength)))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.OutsideSpanChange"));
				}

				originalCursor = replacement.Start;
				updatedCursor += unchangedLength;
				if (updatedCursor + replacement.Value.Length > updatedText.Length ||
					!updatedText.AsSpan(updatedCursor, replacement.Value.Length)
						.SequenceEqual(replacement.Value.AsSpan()))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.ReplacementSpan"));
				}

				originalCursor += replacement.Length;
				updatedCursor += replacement.Value.Length;
			}

			int trailingLength = originalText.Length - originalCursor;
			if (trailingLength < 0 ||
				updatedCursor + trailingLength != updatedText.Length ||
				!originalText.AsSpan(originalCursor, trailingLength)
					.SequenceEqual(updatedText.AsSpan(updatedCursor, trailingLength)))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.TrailingTextChange"));
			}
		}

		private static void ValidateUpdatedDocument(
			ParsedDocument originalDocument,
			string updatedText,
			ConfigFormat format,
			Dictionary<string, string> expectedValuesById)
		{
			if (format == ConfigFormat.XML)
			{

				XmlDocument xmlDocument = new() { PreserveWhitespace = true };
				xmlDocument.LoadXml(updatedText);
			}

			ParsedDocument reparsedDocument = ParseDocument(updatedText, format);
			if (originalDocument.Values.Count != reparsedDocument.Values.Count ||
				!originalDocument.Values.Select(value => value.Id).SequenceEqual(
					reparsedDocument.Values.Select(value => value.Id),
					StringComparer.Ordinal))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.StructureChange"));
			}

			Dictionary<string, ParsedValue> reparsedValues = reparsedDocument.Values
				.ToDictionary(value => value.Id, value => value, StringComparer.Ordinal);

			foreach ((string changedId, string expectedValue) in expectedValuesById)
			{
				if (!reparsedValues.TryGetValue(changedId, out ParsedValue? reparsedValue) ||
					!ValuesAreEquivalent(reparsedValue, expectedValue))
				{
					ParsedValue? source = originalDocument.Values.FirstOrDefault(
						value => value.Id == changedId);
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.ValueStructureChange",
							source?.Key ?? LocalizationManager.Get(
								"Configuration.Editor.GenericSetting")));
				}
			}
		}

		private static string FormatReplacement(
			ParsedValue source,
			string requestedValue,
			ConfigFormat format)
		{
			string value = requestedValue ?? string.Empty;
			if (source.OriginalToken.Contains('\r') ||
				source.OriginalToken.Contains('\n'))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.SourceLineBreaks",
						source.Key));
			}

			if (value.Contains('\r') || value.Contains('\n'))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.LineBreak",
						source.Key));
			}

			if (format == ConfigFormat.YAML)
				return FormatYamlReplacement(source, value);

			if (source.Type == ConfigValueType.Boolean)
			{
				if (!TryParseBoolean(value, out bool booleanValue))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.Boolean",
							source.Key));
				}

				value = source.Style == ScalarStyle.JsonBoolean
					? (booleanValue ? "true" : "false")
					: PreserveBooleanCasing(source.OriginalToken, booleanValue);
			}
			else if (source.Type == ConfigValueType.Number)
			{
				if (!IsValidNumber(value, source.Style == ScalarStyle.JsonNumber))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.Number",
							source.Key));
				}
				value = value.Trim();
			}

			return source.Style switch
			{
				ScalarStyle.JsonString => EncodeJsonStringToken(value),
				ScalarStyle.JsonNumber => value,
				ScalarStyle.JsonBoolean => value.ToLowerInvariant(),
				ScalarStyle.JsonNull => value.Equals("null", StringComparison.OrdinalIgnoreCase)
					? "null"
					: EncodeJsonStringToken(value),
				ScalarStyle.QuotedSingle => QuoteValue(value, '\''),
				ScalarStyle.QuotedDouble => QuoteValue(value, '"'),
				ScalarStyle.XmlAttribute => EscapeXml(value),
				ScalarStyle.XmlText => EscapeXml(value),
				ScalarStyle.XmlCData => value.Contains("]]>", StringComparison.Ordinal)
					? throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Editor.Error.CDataTerminator",
							source.Key))
					: value,
				_ => FormatRawValue(source, value, format)
			};
		}

		private static string FormatRawValue(
			ParsedValue source,
			string value,
			ConfigFormat format)
		{
			if (format == ConfigFormat.StandardINI && NeedsIniQuotes(source, value))
			{
				return QuoteValue(value, '"');
			}

			return value;
		}

		private static bool NeedsIniQuotes(ParsedValue source, string value)
		{
			if (source.IsCompositeValue &&
				(value.Contains(',') || value.Contains('(') || value.Contains(')')))
			{
				return true;
			}

			string trimmed = value.Trim();
			if (trimmed.Length >= 2 && trimmed[0] == '(' && trimmed[^1] == ')')
			{
				return true;
			}

			for (int index = 1; index < value.Length; index++)
			{
				if (!char.IsWhiteSpace(value[index - 1]))
				{
					continue;
				}

				if (value[index] == ';' || value[index] == '#' ||
					(value[index] == '/' && index + 1 < value.Length && value[index + 1] == '/'))
				{
					return true;
				}
			}

			return false;
		}

		private static string QuoteValue(string value, char quote)
		{
			StringBuilder builder = new(value.Length + 2);
			builder.Append(quote);
			foreach (char character in value)
			{
				if (character == quote)
				{
					builder.Append('\\');
				}
				builder.Append(character);
			}
			builder.Append(quote);
			return builder.ToString();
		}

		private static string EncodeJsonStringToken(string value)
		{
			StringBuilder builder = new(value.Length + 2);
			builder.Append('"');
			for (int index = 0; index < value.Length; index++)
			{
				char character = value[index];
				switch (character)
				{
					case '"': builder.Append("\\\""); break;
					case '\\': builder.Append("\\\\"); break;
					case '\b': builder.Append("\\b"); break;
					case '\f': builder.Append("\\f"); break;
					case '\n': builder.Append("\\n"); break;
					case '\r': builder.Append("\\r"); break;
					case '\t': builder.Append("\\t"); break;
					default:
						if (character < ' ')
						{
							builder.Append("\\u");
							builder.Append(((int)character).ToString(
								"X4",
								CultureInfo.InvariantCulture));
						}
						else if (char.IsHighSurrogate(character))
						{
							if (index + 1 >= value.Length ||
								!char.IsLowSurrogate(value[index + 1]))
							{
								throw new InvalidDataException(
									LocalizationManager.Get(
										"Configuration.Editor.Error.JsonUnicodeSurrogate"));
							}

							builder.Append(character);
							builder.Append(value[++index]);
						}
						else if (char.IsLowSurrogate(character))
						{
							throw new InvalidDataException(
								LocalizationManager.Get(
									"Configuration.Editor.Error.JsonUnicodeSurrogate"));
						}
						else
						{
							builder.Append(character);
						}
						break;
				}
			}
			builder.Append('"');
			return builder.ToString();
		}

		private static string EscapeXml(string value)
		{
			return SecurityElement.Escape(value) ?? string.Empty;
		}

		private static bool IsValidNumber(string value, bool requireJsonNumber)
		{
			string trimmed = value.Trim();
			if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				return !requireJsonNumber && long.TryParse(
					trimmed[2..],
					NumberStyles.HexNumber,
					CultureInfo.InvariantCulture,
					out _);
			}

			if (!double.TryParse(
				trimmed,
				NumberStyles.Float,
				CultureInfo.InvariantCulture,
				out double number) ||
				!double.IsFinite(number))
			{
				return false;
			}

			if (!requireJsonNumber)
			{
				return true;
			}

			try
			{
				using JsonDocument document = JsonDocument.Parse(trimmed);
				return document.RootElement.ValueKind == JsonValueKind.Number;
			}
			catch (JsonException)
			{
				return false;
			}
		}

		private static string PreserveBooleanCasing(string originalValue, bool value)
		{
			string original = originalValue.Trim();
			if (original.Length >= 2 &&
				((original[0] == '"' && original[^1] == '"') ||
				 (original[0] == '\'' && original[^1] == '\'')))
			{
				original = original[1..^1];
			}
			if (original.All(character => !char.IsLetter(character) || char.IsUpper(character)))
			{
				return value ? "TRUE" : "FALSE";
			}

			if (original.Length > 0 && char.IsUpper(original[0]))
			{
				return value ? "True" : "False";
			}

			return value ? "true" : "false";
		}

	}
}
