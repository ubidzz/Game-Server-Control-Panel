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

namespace Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;

/// <summary>Parses game output, never translated UI text. Never retains or logs its input.</summary>
internal static class SatisfactoryTokenParser
{
	internal const int MaximumInputLength = 65536;
	internal const string ConsoleLabel = "New Server API Authentication Token:";
	private static readonly Regex Candidate = new(
		@"[A-Za-z0-9+/_-]+={0,2}\.[A-Fa-f0-9]+", RegexOptions.NonBacktracking);

	internal static string Extract(string? text)
	{
		if (string.IsNullOrWhiteSpace(text) || text.Length > MaximumInputLength)
			throw new SatisfactoryApiException(SatisfactoryApiError.Token);
		string? result = null;
		foreach (Match match in Candidate.Matches(text))
		{
			// Do not accept a valid-looking substring of a longer malformed credential.
			if (match.Index > 0 && IsTokenCharacter(text[match.Index - 1]) ||
				match.Index + match.Length < text.Length && IsTokenCharacter(text[match.Index + match.Length])) continue;
			string token;
			try { token = SatisfactoryApiClient.NormalizeToken(match.Value); }
			catch (SatisfactoryApiException) { continue; }
			if (result != null && !string.Equals(result, token, StringComparison.Ordinal))
				throw new SatisfactoryApiException(SatisfactoryApiError.AmbiguousToken);
			result = token;
		}
		return result ?? throw new SatisfactoryApiException(SatisfactoryApiError.Token);
	}

	internal static string Redact(string text) => Candidate.Replace(text, match =>
		match.Value.Length >= 40 ? "[secret removed]" : match.Value);

	private static bool IsTokenCharacter(char c) =>
		char.IsAsciiLetterOrDigit(c) || c is '+' or '/' or '=' or '.' or '-' or '_';
}
