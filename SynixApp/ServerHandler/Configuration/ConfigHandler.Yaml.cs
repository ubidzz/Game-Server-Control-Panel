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
using System.Text;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	public static partial class ConfigHandler
	{
		// A deliberately bounded, lexical editor for block mappings/sequences and
		// single-line scalars, not an object deserializer. Unsupported YAML is rejected
		// before any write; tags/aliases are never resolved or executed.
		private sealed class YamlConfigScanner(string text)
		{
			private sealed record Line(int Start, int Content, int End, int Number)
			{
				internal int Indent => Content - Start;
			}

			internal sealed record Mapping(int InsertAt, int Indent);
			internal Dictionary<string, Mapping> Mappings { get; } = new(StringComparer.Ordinal);
			private readonly List<Line> _lines = [];
			private readonly ParsedDocument _document = new();
			private readonly IdentityBuilder _identities = new();
			private int _index;

			internal ParsedDocument Parse()
			{
				if (text.Length > 4 * 1024 * 1024)
					throw YamlError(1);
				bool started = false;
				bool ended = false;
				int number = 0;
				for (int start = 0; start < text.Length;)
				{
					int end = FindLineEnd(text, start);
					int content = start;
					number++;
					if (number > 100000)
						throw YamlError(number);
					while (content < end && text[content] == ' ')
						content++;
					if (content < end && text[content] != '#')
					{
						ValidateYamlCharacters(text[content..end], number);
						string trimmed = text[content..end].TrimEnd(' ', '\t');
						if (content == start && IsMarker(trimmed, "---"))
						{
							if (started || ended || _lines.Count != 0)
								throw YamlError(number);
							started = true;
						}
						else if (content == start && IsMarker(trimmed, "..."))
						{
							if (ended)
								throw YamlError(number);
							ended = true;
						}
						else
						{
							if (ended || text[content] == '\t')
								throw YamlError(number);
							_lines.Add(new Line(start, content, end, number));
						}
					}
					start = end;
					if (start < text.Length && text[start] == '\r') start++;
					if (start < text.Length && text[start] == '\n') start++;
				}

				if (_lines.Count > 0)
				{
					if (_lines[0].Indent != 0)
						throw YamlError(_lines[0].Number);
					ParseBlock(0, string.Empty, 0);
					if (_index != _lines.Count)
						throw YamlError(_lines[_index].Number);
				}
				return _document;
			}

			private static bool IsMarker(string value, string marker) =>
				value == marker || (value.StartsWith(marker, StringComparison.Ordinal) &&
					value.Length > marker.Length && value[marker.Length] is ' ' or '\t' &&
					value[marker.Length..].TrimStart(' ', '\t').StartsWith('#'));

			private bool IsSequence(Line line) =>
				text[line.Content] == '-' &&
				(line.Content + 1 == line.End || text[line.Content + 1] is ' ' or '\t');

			private void ParseBlock(int indent, string path, int depth)
			{
				if (depth > 64)
					throw YamlError(_lines[_index].Number);
				if (IsSequence(_lines[_index]))
					ParseSequence(indent, path, depth);
				else
					ParseMapping(indent, path, depth);
			}

			private void ParseMapping(int indent, string path, int depth, Line? compactFirst = null)
			{
				HashSet<string> keys = new(StringComparer.Ordinal);
				// Compact "- key: value" maps cannot be safely extended at the dash.
				if (compactFirst == null)
					Mappings.Add(path, new Mapping(_lines[_index].Start, indent));
				else
					ParseEntry(compactFirst, path, keys, depth);

				while (_index < _lines.Count && _lines[_index].Indent == indent &&
					!IsSequence(_lines[_index]))
				{
					Line line = _lines[_index++];
					ParseEntry(line, path, keys, depth);
				}
				if (_index < _lines.Count && _lines[_index].Indent > indent)
					throw YamlError(_lines[_index].Number);
			}

			private void ParseSequence(int indent, string path, int depth)
			{
				int item = 0;
				while (_index < _lines.Count && _lines[_index].Indent == indent &&
					IsSequence(_lines[_index]))
				{
					Line line = _lines[_index++];
					int start = line.Content + 1;
					SkipSpaces(ref start, line.End);
					string itemPath = $"{path}[{item++}]";
					if (start < line.End && text[start] != '#' &&
						FindMappingColon(start, line.End, line.Number) >= 0)
					{
						if (depth >= 64)
							throw YamlError(line.Number);
						ParseMapping(start - line.Start, itemPath, depth + 1,
							line with { Content = start });
					}
					else
						ParseValue(line, start, itemPath, $"[{item - 1}]", path, depth);
				}
				if (_index < _lines.Count && _lines[_index].Indent > indent)
					throw YamlError(_lines[_index].Number);
			}

			private void ParseEntry(Line line, string parent, HashSet<string> keys, int depth)
			{
				int colon = FindMappingColon(line.Content, line.End, line.Number);
				if (colon <= line.Content)
					throw YamlError(line.Number);
				string token = text[line.Content..colon].TrimEnd(' ', '\t');
				string key = DecodeYamlScalar(token, line.Number, out _);
				if (key.Length == 0 || key.Length > 1024 || key == "<<" ||
					key.Any(char.IsControl) || !keys.Add(key))
					throw YamlError(line.Number);
				string path = YamlPath(parent, key);
				int start = colon + 1;
				SkipSpaces(ref start, line.End);
				ParseValue(line, start, path, key, parent, depth);
			}

			private void ParseValue(Line line, int start, string path, string key, string parent, int depth)
			{
				bool empty = start == line.End || text[start] == '#';
				bool hasChild = _index < _lines.Count &&
					(_lines[_index].Indent > line.Indent ||
					 (empty && _lines[_index].Indent == line.Indent &&
					  !IsSequence(line) && IsSequence(_lines[_index])));
				if (hasChild)
				{
					if (!empty)
						throw YamlError(_lines[_index].Number);
					ParseBlock(_lines[_index].Indent, path, depth + 1);
					return;
				}

				int end = empty ? start : FindScalarEnd(start, line.End, line.Number);
				string token = text[start..end];
				string value = DecodeYamlScalar(token, line.Number, out ScalarStyle style);
				ConfigValueType type = IsSensitiveKey(path) ? ConfigValueType.Secret :
					style != ScalarStyle.YamlPlain ? ConfigValueType.Text :
					IsYamlNull(token) ? ConfigValueType.Null :
					value is "true" or "True" or "TRUE" or "false" or "False" or "FALSE" ? ConfigValueType.Boolean :
					IsYamlNumber(value) ? ConfigValueType.Number : ConfigValueType.Text;
				if (type == ConfigValueType.Null)
					value = "null";
				_document.Values.Add(new ParsedValue
				{
					Id = _identities.Create("yaml", path),
					Key = key,
					Path = path,
					Section = parent,
					Value = value,
					Type = type,
					Style = style,
					Start = start,
					Length = end - start,
					OriginalToken = token
				});
			}

			private void SkipSpaces(ref int start, int end)
			{
				while (start < end && text[start] is ' ' or '\t')
					start++;
			}

			private int FindMappingColon(int start, int end, int number)
			{
				if (start < end && text[start] is '\'' or '"')
				{
					int after = FindQuoteEnd(start, end, number);
					SkipSpaces(ref after, end);
					return after < end && text[after] == ':' &&
						(after + 1 == end || text[after + 1] is ' ' or '\t') ? after : -1;
				}
				for (int index = start; index < end; index++)
				{
					if (text[index] == '#' && (index == start || text[index - 1] is ' ' or '\t'))
						return -1;
					if (text[index] == ':' && (index + 1 == end || text[index + 1] is ' ' or '\t'))
						return index;
				}
				return -1;
			}

			private int FindScalarEnd(int start, int end, int number)
			{
				if (text[start] is '\'' or '"')
				{
					int after = FindQuoteEnd(start, end, number);
					int tail = after;
					SkipSpaces(ref tail, end);
					if (tail < end && (tail == after || text[tail] != '#'))
						throw YamlError(number);
					return after;
				}
				for (int index = start; index < end; index++)
				{
					if (text[index] == '#' && (index == start || text[index - 1] is ' ' or '\t'))
					{
						end = index;
						break;
					}
				}
				while (end > start && text[end - 1] is ' ' or '\t')
					end--;
				return end;
			}

			private int FindQuoteEnd(int start, int end, int number)
			{
				char quote = text[start];
				for (int index = start + 1; index < end; index++)
				{
					if (quote == '"' && text[index] == '\\')
					{
						index++;
						continue;
					}
					if (text[index] != quote) continue;
					if (quote == '\'' && index + 1 < end && text[index + 1] == '\'')
					{
						index++;
						continue;
					}
					return index + 1;
				}
				throw YamlError(number);
			}
		}

		private static string YamlPath(string parent, string key)
		{
			bool simple = key.Length > 0 && (char.IsAsciiLetter(key[0]) || key[0] == '_') &&
				key.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');
			string segment = simple ? key : $"[{EncodeJsonStringToken(key)}]";
			return parent.Length == 0 ? segment : simple ? $"{parent}.{segment}" : parent + segment;
		}

		private static InvalidDataException YamlError(int line) => new(
			LocalizationManager.Get("Configuration.Editor.Error.YamlUnsupported", line));

		private static void ValidateYamlCharacters(string value, int line)
		{
			for (int index = 0; index < value.Length; index++)
			{
				char character = value[index];
				if ((character < ' ' && character != '\t') ||
					character is '\u007f' or '\u0085' or '\u2028' or '\u2029' or '\ufeff' or '\ufffe' or '\uffff' ||
					(character >= '\u0080' && character < '\u00a0'))
					throw YamlError(line);
				if (char.IsHighSurrogate(character))
				{
					if (index + 1 >= value.Length || !char.IsLowSurrogate(value[++index]))
						throw YamlError(line);
				}
				else if (char.IsLowSurrogate(character))
					throw YamlError(line);
			}
		}

		private static bool IsYamlNull(string value) =>
			value.Length == 0 || value is "~" or "null" or "Null" or "NULL";

		private static readonly Regex YamlNumberPattern = new(
			@"\A(?:[-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][-+]?[0-9]+)?|0x[0-9a-fA-F]+|0o[0-7]+|[-+]?\.(?:inf|Inf|INF)|\.(?:nan|NaN|NAN))\z",
			RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

		private static bool IsYamlNumber(string value) => YamlNumberPattern.IsMatch(value);

		internal static string QuoteYamlString(string value)
		{
			ValidateYamlCharacters(value, 1);
			return EncodeJsonStringToken(value);
		}

		private static bool IsPlainYamlText(string value)
		{
			if (value.Length == 0 || value != value.Trim(' ', '\t') ||
				"[]{}#,|>&*!%@\u0060'\"".Contains(value[0]) ||
				(value[0] is '-' or '?' or ':' && (value.Length == 1 || value[1] is ' ' or '\t')))
				return false;
			for (int index = 0; index < value.Length; index++)
			{
				if (value[index] == ':' && (index + 1 == value.Length || value[index + 1] is ' ' or '\t') ||
					value[index] == '#' && index > 0 && value[index - 1] is ' ' or '\t')
					return false;
			}
			return true;
		}

		private static string DecodeYamlScalar(string token, int line, out ScalarStyle style)
		{
			style = ScalarStyle.YamlPlain;
			if (token.Length == 0) return string.Empty;
			if (token[0] == '\'')
			{
				style = ScalarStyle.YamlSingle;
				if (token.Length < 2 || token[^1] != '\'') throw YamlError(line);
				StringBuilder single = new();
				for (int index = 1; index < token.Length - 1; index++)
				{
					if (token[index] == '\'' &&
						(++index >= token.Length - 1 || token[index] != '\''))
						throw YamlError(line);
					single.Append(token[index]);
				}
				return single.ToString();
			}
			if (token[0] != '"')
			{
				if (!IsPlainYamlText(token)) throw YamlError(line);
				return token;
			}

			style = ScalarStyle.YamlDouble;
			if (token.Length < 2 || token[^1] != '"') throw YamlError(line);
			StringBuilder result = new();
			for (int index = 1; index < token.Length - 1; index++)
			{
				char character = token[index];
				if (character == '"') throw YamlError(line);
				if (character != '\\')
				{
					result.Append(character);
					continue;
				}
				if (++index >= token.Length - 1) throw YamlError(line);
				char escape = token[index];
				if (escape is 'x' or 'u' or 'U')
				{
					int digits = escape == 'x' ? 2 : escape == 'u' ? 4 : 8;
					if (index + digits >= token.Length - 1 ||
						!int.TryParse(token.AsSpan(index + 1, digits), NumberStyles.AllowHexSpecifier,
							CultureInfo.InvariantCulture, out int codePoint) ||
						!Rune.IsValid(codePoint))
						throw YamlError(line);
					result.Append(char.ConvertFromUtf32(codePoint));
					index += digits;
				}
				else
				{
					result.Append(escape switch
					{
						'0' => '\0', 'a' => '\a', 'b' => '\b', 't' or '\t' => '\t',
						'n' => '\n', 'v' => '\v', 'f' => '\f', 'r' => '\r', 'e' => '\u001b',
						' ' => ' ', '"' => '"', '/' => '/', '\\' => '\\',
						'N' => '\u0085', '_' => '\u00a0', 'L' => '\u2028', 'P' => '\u2029',
						_ => throw YamlError(line)
					});
				}
			}
			return result.ToString();
		}

		private static string FormatYamlReplacement(ParsedValue source, string value)
		{
			ValidateYamlCharacters(value, 1);
			string token;
			if (source.Type == ConfigValueType.Boolean)
			{
				if (!TryParseBoolean(value, out bool boolean))
					throw new InvalidDataException(LocalizationManager.Get(
						"Configuration.Editor.Error.Boolean", source.Key));
				token = boolean ? "true" : "false";
			}
			else if (source.Type == ConfigValueType.Number)
			{
				if (!IsYamlNumber(value.Trim()))
					throw new InvalidDataException(LocalizationManager.Get(
						"Configuration.Editor.Error.Number", source.Key));
				token = value.Trim();
			}
			else if (source.Type == ConfigValueType.Null && value == "null")
				token = "null";
			else if (source.Style == ScalarStyle.YamlSingle)
				token = "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
			else
				// Explicit strings also avoid YAML 1.1 implicit booleans/dates in
				// games that use an older reader. Only this scalar's span changes.
				token = EncodeJsonStringToken(value);

			// Empty scalars may have no space after the colon. Supply separators on
			// both sides, including before a following inline comment.
			return source.Length == 0 ? " " + token + " " : token;
		}
	}
}
