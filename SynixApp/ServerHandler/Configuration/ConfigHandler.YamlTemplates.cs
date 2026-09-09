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
using System.Text;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	public static partial class ConfigHandler
	{
		internal static string ExpandYamlTemplate(string template, IReadOnlyDictionary<string, string> values)
		{
			// Parse safe markers first so quoted/unquoted placeholders and text inside
			// quoted descriptions all become one scalar, never YAML syntax from input.
			string prefix;
			do { prefix = "synix_yaml_" + Guid.NewGuid().ToString("N") + "_"; }
			while (template.Contains(prefix, StringComparison.Ordinal) ||
				values.Values.Any(value => value.Contains(prefix, StringComparison.Ordinal)));
			Dictionary<string, string> markers = values.Keys.Select((key, index) => (key, index))
				.ToDictionary(pair => pair.key, pair => prefix + pair.index + "_end", StringComparer.Ordinal);
			Dictionary<string, string> markerKeys = markers.ToDictionary(pair => pair.Value, pair => pair.Key);
			string marked = Regex.Replace(template, @"\{[A-Za-z]+\}",
				match => markers.GetValueOrDefault(match.Value, match.Value),
				RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
			ParsedDocument document = new YamlConfigScanner(marked).Parse();
			List<Replacement> replacements = [];
			Dictionary<string, string> expected = new(StringComparer.Ordinal);
			foreach (ParsedValue source in document.Values)
			{
				if (source.Path.Contains(prefix, StringComparison.Ordinal))
					throw YamlError(1);
				if (!source.Value.Contains(prefix, StringComparison.Ordinal))
					continue;
				string expanded = Regex.Replace(source.Value, prefix + @"[0-9]+_end",
					match => values[markerKeys[match.Value]], RegexOptions.NonBacktracking);
				string token;
				if (markerKeys.TryGetValue(source.Value, out string? exactKey) &&
					exactKey is "{Port}" or "{QueryPort}" or "{RCONPort}" or "{MaxPlayers}" or
						"{WorldSeed}" or "{WorldSize}" or "{AppPort}")
				{
					if (!IsYamlNumber(expanded))
						throw new InvalidDataException(LocalizationManager.Get(
							"Configuration.Editor.Error.Number", source.Key));
					token = expanded;
				}
				else if (exactKey is "{HasPassword}" or "{EnableRcon}" or "{IsPvp}" or "{IsPve}" or "{Crossplay}" &&
					bool.TryParse(expanded, out bool boolean))
					token = boolean ? "true" : "false";
				else
					token = QuoteYamlString(expanded);
				replacements.Add(new Replacement
				{
					Start = source.Start, Length = source.Length,
					ExpectedOriginalToken = source.OriginalToken, Value = token
				});
				expected.Add(source.Id, expanded);
			}
			StringBuilder result = new(marked);
			foreach (Replacement replacement in replacements.OrderByDescending(value => value.Start))
				result.Remove(replacement.Start, replacement.Length).Insert(replacement.Start, replacement.Value);
			string output = result.ToString();
			ValidateUpdatedDocument(document, output, ConfigFormat.YAML, expected);
			// Documentation comments keep the author's original placeholder spelling.
			return Regex.Replace(output, prefix + @"[0-9]+_end",
				match => markerKeys[match.Value], RegexOptions.NonBacktracking);
		}

		internal static (bool Changed, IReadOnlyList<string> Missing) UpdateYamlManagedValues(
			string path, IReadOnlyList<ConfigLine> desiredValues)
		{
			ConfigurationTextSnapshot snapshot = ConfigurationTextSnapshot.Read(path);
			YamlConfigScanner scanner = new(snapshot.Text);
			ParsedDocument original = scanner.Parse();
			Dictionary<string, ParsedValue> existing = original.Values.ToDictionary(value => value.Id);
			List<ConfigLine> updates = [];
			List<ConfigLine> additions = [];
			List<string> missing = [];

			foreach (ConfigLine desired in desiredValues)
			{
				if (existing.TryGetValue(desired.Id, out ParsedValue? current))
				{
					if (current.Path != desired.Path || current.Key != desired.Key || current.Type != desired.Type)
					{
						missing.Add(desired.Path);
						continue;
					}
					updates.Add(new ConfigLine
					{
						Id = current.Id, Key = current.Key, Path = current.Path, Section = current.Section,
						Type = current.Type, OriginalValue = current.Value, HasOriginalValue = true,
						Value = desired.Value
					});
				}
				else if (scanner.Mappings.ContainsKey(desired.Section) &&
					!scanner.Mappings.ContainsKey(desired.Path) &&
					!original.Values.Any(value => value.Path.StartsWith(desired.Path + "[", StringComparison.Ordinal)) &&
					desired.Path == YamlPath(desired.Section, desired.Key))
				{
					// Add only template-managed scalars under an existing mapping.
					// Commented examples stay comments; do not enable unrelated services.
					additions.Add(desired);
				}
				else
					missing.Add(desired.Path);
			}

			string updated = BuildUpdatedText(snapshot.Text, updates, ConfigFormat.YAML);
			YamlConfigScanner updatedScanner = new(updated);
			updatedScanner.Parse();
			string newline = snapshot.Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" :
				snapshot.Text.Contains('\r') ? "\r" : "\n";
			foreach (IGrouping<string, ConfigLine> group in additions
				.GroupBy(value => value.Section)
				.OrderByDescending(group => updatedScanner.Mappings[group.Key].InsertAt))
			{
				YamlConfigScanner.Mapping mapping = updatedScanner.Mappings[group.Key];
				string insertion = string.Concat(group.Select(value =>
				{
					string token = FormatYamlReplacement(new ParsedValue
					{
						Key = value.Key, Type = value.Type, Style = ScalarStyle.YamlDouble, Length = 1
					}, value.Value);
					return new string(' ', mapping.Indent) + QuoteYamlString(value.Key) + ": " + token + newline;
				}));
				updated = updated.Insert(mapping.InsertAt, insertion);
			}

			ParsedDocument final = new YamlConfigScanner(updated).Parse();
			Dictionary<string, ParsedValue> finalValues = final.Values.ToDictionary(value => value.Id);
			if (final.Values.Count != original.Values.Count + additions.Count ||
				original.Values.Any(value => !finalValues.ContainsKey(value.Id)))
				throw new InvalidDataException(LocalizationManager.Get("Configuration.Editor.Error.StructureChange"));
			foreach (ConfigLine value in updates.Concat(additions))
			{
				if (!finalValues.TryGetValue(value.Id, out ParsedValue? saved) ||
					saved.Type != value.Type || !ValuesAreEquivalent(saved, value.Value))
					throw new InvalidDataException(LocalizationManager.Get(
						"Configuration.Editor.Error.ValueStructureChange", value.Key));
			}

			bool changed = !string.Equals(snapshot.Text, updated, StringComparison.Ordinal);
			if (changed)
			{
				if (!string.Equals(ConfigurationTextSnapshot.Read(path).Text, snapshot.Text, StringComparison.Ordinal))
					throw new InvalidDataException(LocalizationManager.Get(
						"Configuration.Editor.Error.SourceSpanChanged"));
				ConfigurationFileWriter.WriteAtomically(path, snapshot.Encode(updated));
			}
			return (changed, missing);
		}
	}
}
