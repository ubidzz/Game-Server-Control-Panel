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
using Synix_Control_Panel.SynixEngine;
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using System.Globalization;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	internal static class GameServerInputValidator
	{
		internal const int NumericWorldSeedMaximum = int.MaxValue;
		internal static int GetNumericWorldSeedMinimum(string? gameName) =>
			string.Equals(gameName, "Empyrion - Galactic Survival", StringComparison.OrdinalIgnoreCase)
				? 1 : int.MinValue;

		internal static bool CanGenerateWorldSeed(GameInfo? definition) =>
			definition != null &&
			GameFix.GetManagementCapabilities(definition).HasFlag(GameManagementCapability.WorldSeed);

		internal static string GenerateWorldSeed() =>
			// A positive 32-bit seed also fits the wider numeric/text seed fields.
			Random.Shared.NextInt64(1, (long)NumericWorldSeedMaximum + 1)
				.ToString(CultureInfo.InvariantCulture);

		internal static bool RequiresNumericWorldSeed(GameInfo? definition) =>
			definition?.Format == ConfigFormat.YAML &&
			GameFix.GetManagementCapabilities(definition).HasFlag(GameManagementCapability.WorldSeed);

		internal static bool TryValidateWorldSeed(
			GameInfo? definition, string? value, out string error)
		{
			error = string.Empty;
			return !RequiresNumericWorldSeed(definition) ||
				TryParseRequiredNumericWorldSeed(definition!.Game, value, out _, out error);
		}

		internal static bool TryParseRequiredNumericWorldSeed(
			string gameName, string? value, out int seed, out string error)
		{
			seed = default;
			int minimum = GetNumericWorldSeedMinimum(gameName);
			if (value != null && !value.Any(char.IsControl) &&
				int.TryParse(value.Trim(), NumberStyles.AllowLeadingSign,
					CultureInfo.InvariantCulture, out seed) && seed >= minimum)
			{
				error = string.Empty;
				return true;
			}

			error = LocalizationManager.Get(
				"GameInput.WorldSeed.RequiredNumber", gameName,
				minimum.ToString(CultureInfo.InvariantCulture),
				NumericWorldSeedMaximum.ToString(CultureInfo.InvariantCulture));
			return false;
		}

		internal static bool TryValidate(
			GameInfo definition,
			string? serverName,
			SynixServerPasswords passwords,
			out string error)
		{
			ArgumentNullException.ThrowIfNull(definition);

			string password = passwords.ServerPassword ?? string.Empty;
			if (password.Length < definition.MinimumServerPasswordLength)
			{
				error = LocalizationManager.Get(
					"GameInput.Password.MinimumLength",
					definition.Game,
					definition.MinimumServerPasswordLength);
				return false;
			}

			if (definition.ServerPasswordMustNotAppearInName &&
				!string.IsNullOrEmpty(password) &&
				(serverName ?? string.Empty).Contains(
					password,
					StringComparison.OrdinalIgnoreCase))
			{
				error = LocalizationManager.Get(
					"GameInput.Password.NotInServerName",
					definition.Game);
				return false;
			}

			string authenticationToken = passwords.AuthenticationToken ?? string.Empty;
			string authenticationTokenLabel = string.IsNullOrWhiteSpace(
				definition.AuthenticationTokenLabel)
					? LocalizationManager.Get("GameInput.AuthenticationToken")
					: definition.AuthenticationTokenLabel;
			if (definition.RequiresAuthenticationToken &&
				string.IsNullOrWhiteSpace(authenticationToken))
			{
				error = LocalizationManager.Get(
					"GameInput.AuthenticationToken.Required",
					definition.Game,
					authenticationTokenLabel);
				return false;
			}

			if (!string.IsNullOrEmpty(authenticationToken) &&
				(authenticationToken.Length > 4096 ||
				 authenticationToken.Any(character =>
					char.IsControl(character) ||
					char.IsWhiteSpace(character) ||
					character is '"' or '\'' or '&' or '|' or '<' or '>' or '^' or '%' or '!')))
			{
				error = LocalizationManager.Get(
					"GameInput.AuthenticationToken.Unsafe",
					authenticationTokenLabel);
				return false;
			}

			error = string.Empty;
			return true;
		}
	}
}
