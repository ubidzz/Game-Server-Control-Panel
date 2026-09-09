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
using System.Text.RegularExpressions;
using Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;

namespace Synix_Control_Panel.SynixEngine;

/// <summary>Best-effort masking of output copies only. Never use for game input or storage.</summary>
internal static class SecretRedactor
{
	internal const string Removed = "[secret removed]";
	internal const string Omitted = "[Content omitted because the privacy check could not finish safely.]";
	private const int MaximumTextLength = 256 * 1024;
	private const string Key = @"(?:[a-z][a-z0-9_-]*\.)*(?:(?:server|admin|rcon|user|authentication|access|refresh)[._ -]?)?(?:password|passwd|pwd|token|secret|api[._-]?key|webhook(?:url)?|authorization)";
	private const string Value = """
		(?:(?:Bearer|Basic)[ \t]+(?:\[secret removed\]|\S+)|\[secret removed\]|\[Discord webhook removed\]|"(?:\\[^\r\n]|[^"\\\r\n])*(?:"|(?=\r?\n|$))|'(?:\\[^\r\n]|[^'\\\r\n])*(?:'|(?=\r?\n|$))|[^\s,;&\r\n]+)
		""";
	private static readonly Regex Webhook = Pattern(@"https://(?:canary\.|ptb\.)?discord(?:app)?\.com/api/webhooks/[^\s""'<>]+");
	private static readonly Regex Authorization = Pattern(@"(?<key>\b(?:proxy-)?authorization\s*[:=]\s*(?:Bearer|Basic)\s+)(?<value>\[secret removed\]|\S+)");
	private static readonly Regex Assigned = Pattern($"(?<![a-z0-9_])(?<key>(?:\"{Key}\"|'{Key}'|{Key})\\s*[:=]\\s*)(?<value>{Value})");
	private static readonly Regex Command = Pattern($"(?<key>(?:--?|\\+){Key})(?<gap>[ \\t]+)(?<value>{Value})");

	internal static string Redact(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;
		if (text.Length > MaximumTextLength)
			return Omitted;
		try
		{
			string result = text.Length >= 40 && text.Contains('.') ? SatisfactoryTokenParser.Redact(text) : text;
			if (!MightContainSecret(result))
				return result;
			result = Webhook.Replace(result, "[Discord webhook removed]");
			result = Authorization.Replace(result, match => match.Groups["key"].Value + Removed);
			result = Assigned.Replace(result, Mask);
			return Command.Replace(result, Mask);
		}
		catch (RegexMatchTimeoutException)
		{
			// Logging must neither throw nor fall back to publishing an unchecked secret.
			return Omitted;
		}
	}

	private static string Mask(Match match)
	{
		string value = match.Groups["value"].Value;
		string replacement = value.StartsWith('"') ? $"\"{Removed}\"" :
			value.StartsWith('\'') ? $"'{Removed}'" : Removed;
		if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
			value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
			replacement = value[..value.IndexOf(' ')] + " " + Removed;
		return match.Groups["key"].Value + match.Groups["gap"].Value + replacement;
	}

	private static bool MightContainSecret(string text) =>
		text.Contains("pass", StringComparison.OrdinalIgnoreCase) ||
		text.Contains("pwd", StringComparison.OrdinalIgnoreCase) ||
		text.Contains("token", StringComparison.OrdinalIgnoreCase) ||
		text.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
		text.Contains("key", StringComparison.OrdinalIgnoreCase) ||
		text.Contains("webhook", StringComparison.OrdinalIgnoreCase) ||
		text.Contains("authorization", StringComparison.OrdinalIgnoreCase);

	private static Regex Pattern(string pattern) => new(pattern,
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
		TimeSpan.FromMilliseconds(250));
}
