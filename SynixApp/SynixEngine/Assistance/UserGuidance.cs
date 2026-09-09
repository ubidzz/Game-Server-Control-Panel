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
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixEngine
{
	internal sealed record ConfigurationSupportPresentation(
		string Status,
		string Summary,
		Color Color);

	internal enum SafetyCheckLevel
	{
		Ready,
		Review,
		Blocked
	}

	internal sealed record SafetyCheckItem(
		SafetyCheckLevel Level,
		string Name,
		string Details);

	internal sealed class SafetyChecklistReport
	{
		internal SafetyChecklistReport(IReadOnlyList<SafetyCheckItem> items) =>
			Items = items;

		internal IReadOnlyList<SafetyCheckItem> Items { get; }
		internal bool CanContinue => Items.All(item => item.Level != SafetyCheckLevel.Blocked);
		internal int CompletionPercentage
		{
			get
			{
				if (Items.Count == 0)
					return 0;
				double points = Items.Sum(item => item.Level switch
				{
					SafetyCheckLevel.Ready => 1d,
					SafetyCheckLevel.Review => 0.5d,
					_ => 0d
				});
				return (int)Math.Round(points * 100d / Items.Count);
			}
		}
	}

	internal sealed record SetupCompletionState(
		bool HasServerName,
		bool HasGame,
		bool HasInstallFolder,
		bool PortsAreValid,
		bool RequirementsAreMet,
		bool ReadyToSave);

	internal sealed record PlainEnglishError(
		string Heading,
		string Explanation,
		string NextStep,
		string TechnicalDetails);

	internal static class UserGuidance
	{
		internal static ConfigurationSupportPresentation GetConfigurationSupport(
			GameInfo? game)
		{
			if (game == null)
			{
				return new(
					LocalizationManager.Get(
						"Guidance.Configuration.SelectGame.Status"),
					LocalizationManager.Get(
						"Guidance.Configuration.SelectGame.Summary"),
					SettingsPalette.MutedText);
			}

			return game.ConfigFileCreation switch
			{
				ConfigFileCreationMode.SynixTemplate => new(
					LocalizationManager.Get(
						"Guidance.Configuration.Full.Status"),
					LocalizationManager.Get(
						"Guidance.Configuration.Full.Summary"),
					SettingsPalette.Success),
				ConfigFileCreationMode.GameGenerated => new(
					LocalizationManager.Get(
						"Guidance.Configuration.Guided.Status"),
					LocalizationManager.Get(
						"Guidance.Configuration.Guided.Summary"),
					SettingsPalette.Accent),
				ConfigFileCreationMode.LaunchArgumentsOnly => new(
					LocalizationManager.Get(
						"Guidance.Configuration.Launch.Status"),
					LocalizationManager.Get(
						"Guidance.Configuration.Launch.Summary"),
					SettingsPalette.Accent),
				_ => new(
					LocalizationManager.Get(
						"Guidance.Configuration.Basic.Status"),
					LocalizationManager.Get(
						"Guidance.Configuration.Basic.Summary"),
					SettingsPalette.Warning)
			};
		}

		internal static int CalculateSetupCompletion(SetupCompletionState state)
		{
			ArgumentNullException.ThrowIfNull(state);
			int points = 0;
			if (state.HasServerName) points += 15;
			if (state.HasGame) points += 20;
			if (state.HasInstallFolder) points += 20;
			if (state.PortsAreValid) points += 20;
			if (state.RequirementsAreMet) points += 15;
			if (state.ReadyToSave) points += 10;
			return Math.Clamp(points, 0, 100);
		}

		internal static SafetyChecklistReport BuildSafetyChecklist(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			List<SafetyCheckItem> items = [];
			GameInfo? game = GameDatabase.GetGame(server.Game);

			string serverIdentity = LocalizationManager.Get(
				"Guidance.Safety.ServerIdentity.Name");
			items.Add(string.IsNullOrWhiteSpace(server.ServerName)
				? new(
					SafetyCheckLevel.Blocked,
					serverIdentity,
					LocalizationManager.Get(
						"Guidance.Safety.ServerIdentity.Missing"))
				: new(
					SafetyCheckLevel.Ready,
					serverIdentity,
					LocalizationManager.Get(
						"Guidance.Safety.ServerIdentity.Saved",
						server.ServerName,
						server.Game)));

			string gameDefinition = LocalizationManager.Get(
				"Guidance.Safety.GameDefinition.Name");
			items.Add(game == null
				? new(
					SafetyCheckLevel.Blocked,
					gameDefinition,
					LocalizationManager.Get(
						"Guidance.Safety.GameDefinition.Missing"))
				: new(
					SafetyCheckLevel.Ready,
					gameDefinition,
					LocalizationManager.Get(
						"Guidance.Safety.GameDefinition.Loaded",
						game.Game)));

			string installationFolder = LocalizationManager.Get(
				"Guidance.Safety.InstallFolder.Name");
			items.Add(string.IsNullOrWhiteSpace(server.InstallPath)
				? new(
					SafetyCheckLevel.Blocked,
					installationFolder,
					LocalizationManager.Get(
						"Guidance.Safety.InstallFolder.Missing"))
				: Directory.Exists(server.InstallPath)
					? new(
						SafetyCheckLevel.Ready,
						installationFolder,
						LocalizationManager.Get(
							"Guidance.Safety.InstallFolder.Available"))
					: new(
						SafetyCheckLevel.Review,
						installationFolder,
						LocalizationManager.Get(
							"Guidance.Safety.InstallFolder.WillCreate")));

			bool portsValid = server.Port is >= 1 and <= 65535 &&
				(server.QueryPort == 0 || server.QueryPort is >= 1 and <= 65535) &&
				(!server.EnableRcon || server.RconPort is >= 1 and <= 65535);
			string networkPorts = LocalizationManager.Get(
				"Guidance.Safety.NetworkPorts.Name");
			items.Add(portsValid
				? new(SafetyCheckLevel.Ready, networkPorts, GetPortSummary(server))
				: new(
					SafetyCheckLevel.Blocked,
					networkPorts,
					LocalizationManager.Get(
						"Guidance.Safety.NetworkPorts.Invalid")));

			ConfigurationSupportPresentation support = GetConfigurationSupport(game);
			string configurationSupport = LocalizationManager.Get(
				"Guidance.Safety.ConfigurationSupport.Name");
			items.Add(game?.ConfigFileCreation == ConfigFileCreationMode.Unknown
				? new(SafetyCheckLevel.Review, configurationSupport, support.Summary)
				: new(SafetyCheckLevel.Ready, configurationSupport, support.Summary));

			if (game != null)
			{
				GamePrerequisiteItem? failure = GamePrerequisiteChecker
					.CheckCurrentSystem(game)
					.FirstFailure;
				string computerRequirements = LocalizationManager.Get(
					"Guidance.Safety.ComputerRequirements.Name");
				items.Add(failure == null
					? new(
						SafetyCheckLevel.Ready,
						computerRequirements,
						LocalizationManager.Get(
							"Guidance.Safety.ComputerRequirements.Met"))
					: new(
						SafetyCheckLevel.Blocked,
						computerRequirements,
						LocalizationManager.TranslateRuntimeText(
							failure.Message)));
			}

			return new SafetyChecklistReport(items);
		}

		internal static string GetPortSummary(GameServer server)
		{
			List<string> ports =
			[
				LocalizationManager.Get(
					"Guidance.Port.Game",
					server.Port)
			];
			if (server.QueryPort > 0 && server.QueryPort != server.Port)
				ports.Add(LocalizationManager.Get(
					"Guidance.Port.Query",
					server.QueryPort));
			if (server.EnableRcon && server.RconPort > 0)
				ports.Add(LocalizationManager.Get(
					"Guidance.Port.Rcon",
					server.RconPort));
			if (server.AppPort is > 0)
				ports.Add(LocalizationManager.Get(
					"Guidance.Port.App",
					server.AppPort.Value));
			return LocalizationManager.Get(
				"Guidance.Port.Summary",
				string.Join(", ", ports));
		}

		internal static PlainEnglishError TranslateError(
			string operation,
			string? technicalDetails)
		{
			string details = string.IsNullOrWhiteSpace(technicalDetails)
				? LocalizationManager.Get(
					"Guidance.Error.NoTechnicalDetails")
				: technicalDetails.Trim();
			string searchable = details.ToLowerInvariant();

			if (searchable.Contains("game not found") || searchable.Contains("game definition"))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.GameDefinition.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.GameDefinition.NextStep"),
					details);
			}

			if (searchable.Contains("blocked this package") ||
				searchable.Contains("security review") ||
				searchable.Contains("antivirus:"))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.Security.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.Security.NextStep"),
					details);
			}

			if (searchable.Contains("port") &&
				(searchable.Contains("use") || searchable.Contains("occup") || searchable.Contains("bind")))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.Port.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.Port.NextStep"),
					details);
			}

			if (searchable.Contains("not found") || searchable.Contains("missing") || searchable.Contains("could not find"))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.File.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.File.NextStep"),
					details);
			}

			if (searchable.Contains("access") || searchable.Contains("permission") || searchable.Contains("unauthorized"))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.Permission.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.Permission.NextStep"),
					details);
			}

			if (searchable.Contains("configuration") || searchable.Contains("config file"))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.Configuration.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.Configuration.NextStep"),
					details);
			}

			if (searchable.Contains(" requires ") || searchable.Contains("requirement"))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.Requirement.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.Requirement.NextStep"),
					details);
			}

			if (searchable.Contains("network") || searchable.Contains("http") || searchable.Contains("github") || searchable.Contains("timed out"))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.Network.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.Network.NextStep"),
					details);
			}

			if (searchable.Contains("process") || searchable.Contains("shutdown") || searchable.Contains("still running"))
			{
				return new(
					ErrorHeading(operation),
					LocalizationManager.Get(
						"Guidance.Error.Process.Explanation"),
					LocalizationManager.Get(
						"Guidance.Error.Process.NextStep"),
					details);
			}

			return new(
				ErrorHeading(operation),
				LocalizationManager.Get(
					"Guidance.Error.Generic.Explanation"),
				LocalizationManager.Get(
					"Guidance.Error.Generic.NextStep"),
				details);
		}

		private static string ErrorHeading(string operation) =>
			LocalizationManager.Get("Guidance.Error.Heading", operation);
	}
}
