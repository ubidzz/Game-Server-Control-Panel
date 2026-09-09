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
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixApp.UI.GameDefinitions
{
	internal sealed class GameSupportDetailsDialog : Form
	{
		private readonly Panel _content;

		internal GameSupportDetailsDialog(GameInfo game)
		{
			ArgumentNullException.ThrowIfNull(game);
			Text = LocalizationManager.Get("Catalog.Details.Title", game.Game);
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = false;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			ClientSize = new Size(840, 690);
			BackColor = SettingsPalette.Window;
			ForeColor = SettingsPalette.PrimaryText;
			Font = new Font("Segoe UI", 9.5F);

			Controls.Add(new Label
			{
				Text = game.Game,
				Font = new Font("Segoe UI", 19F, FontStyle.Bold),
				Location = new Point(28, 22),
				Size = new Size(784, 42),
				ForeColor = SettingsPalette.PrimaryText,
				AutoEllipsis = true
			});
			Controls.Add(new Label
			{
				Text = LocalizationManager.Get("Text.9B1DF515009197F7B731"),
				Location = new Point(30, 66),
				Size = new Size(780, 28),
				ForeColor = SettingsPalette.SecondaryText
			});

			_content = new Panel
			{
				Location = new Point(24, 104),
				Size = new Size(792, 516),
				AutoScroll = true,
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
			};
			Controls.Add(_content);

			BuildDetails(game);

			ModernSettingsButton close = new()
			{
				Text = LocalizationManager.Get("ModManager.Button.Close"),
				Location = new Point(654, 634),
				Size = new Size(158, 44),
				DialogResult = DialogResult.OK,
				UseAccentStyle = true,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			};
			Controls.Add(close);
			AcceptButton = close;
			CancelButton = close;
			ThemeManager.Apply(this);
		}

		private void BuildDetails(GameInfo game)
		{
			GameCompatibilitySummary compatibility = Core.GetGameCompatibilitySummary(game.Game);
			ConfigurationSupportPresentation configuration = UserGuidance.GetConfigurationSupport(game);
			GameManagementCapability capabilities = GameFix.GetManagementCapabilities(game);
			string lastVerified = compatibility.Verification.LastTested is GameVerificationEvidence evidence
				? LocalizationManager.Get(
					"Catalog.Details.LastVerifiedValue",
					evidence.VerifiedAtUtc.ToLocalTime().ToString(
						"D",
						System.Globalization.CultureInfo.CurrentUICulture),
					evidence.SynixVersion)
				: LocalizationManager.Get("Catalog.Details.NotVerified");

			AddSection(
				LocalizationManager.Get("Catalog.Details.Section.Support"),
				new[]
				{
					(LocalizationManager.Get("Catalog.Filter.Compatibility"), LocalizationManager.TranslateKnownText(compatibility.DisplayName)),
					(LocalizationManager.Get("Text.74E5C919B46AE61321E3"), lastVerified),
					(LocalizationManager.Get("Text.D5CDE76290BF3E730FE4"), LocalizationManager.TranslateKnownText(configuration.Status))
				},
				LocalizationManager.TranslateRuntimeText(configuration.Summary));

			AddSection(
				LocalizationManager.Get("Catalog.Details.Section.Program"),
				new[]
				{
					(LocalizationManager.Get("Catalog.Column.ServerProgram"), GameDatabase.IsMinecraft(game.Game)
						? LocalizationManager.Get(
							"Catalog.Details.MinecraftPrograms",
							"Start.bat",
							"bedrock_server.exe")
						: game.ExeName),
					(LocalizationManager.Get("Catalog.Details.SteamAppId"), string.IsNullOrWhiteSpace(game.AppID) ? LocalizationManager.Get("Catalog.Details.NotUsed") : game.AppID),
					(LocalizationManager.Get("Catalog.Details.DefaultPorts"), FormatPorts(game)),
					(LocalizationManager.Get("Catalog.Column.PlayerDetails"), GetPlayerDetails(game)),
					(LocalizationManager.Get("Catalog.Details.CrossplayOption"), GameDatabase.IsMinecraft(game.Game)
						? LocalizationManager.Get("Catalog.Details.Crossplay.Bedrock")
						: LocalizationManager.Get(
							capabilities.HasFlag(GameManagementCapability.Crossplay)
								? "Catalog.Details.Crossplay.Available"
								: "Catalog.Details.Crossplay.NotListed")),
					(LocalizationManager.Get("Catalog.Details.StatusCheck"), FormatProbe(game))
				});

			AddSection(
				LocalizationManager.Get("Catalog.Details.Section.Configuration"),
				new[]
				{
					(LocalizationManager.Get("Text.F1C216DDF2B88463BCA7"), FormatConfigurationFile(game)),
					(LocalizationManager.Get("Catalog.Details.ServerWindow"), LocalizationManager.Get(
						game.LaunchBehavior.RequiresVisibleWindow
							? "Catalog.Details.Window.Visible"
							: "Catalog.Details.Window.Background")),
					(LocalizationManager.Get("Catalog.Details.ProcessTracking"), LocalizationManager.Get(
						game.LaunchBehavior.LifecycleTracking == GameLifecycleTrackingMode.ExternalDeployment
							? "Catalog.Details.Process.External"
							: "Catalog.Details.Process.Managed")),
					(LocalizationManager.Get("Catalog.Details.RequiredLaunchFiles"), GameDatabase.IsMinecraft(game.Game)
						? LocalizationManager.Get("Catalog.Details.LaunchFiles.Minecraft")
						: game.RequiredLaunchFiles.Length == 0
							? LocalizationManager.Get("Catalog.Details.None")
							: string.Join(", ", game.RequiredLaunchFiles.Select(Path.GetFileName)))
				},
				LocalizationManager.Get("Catalog.Details.SupportNote"));
		}

		private void AddSection(
			string title,
			IReadOnlyList<(string Label, string Value)> rows,
			string? note = null)
		{
			int cardHeight = 54 + (rows.Count * 32) + (string.IsNullOrWhiteSpace(note) ? 10 : 62);
			int top = _content.Controls.Count == 0
				? 0
				: _content.Controls.Cast<Control>().Max(control => control.Bottom) + 14;
			ModernSettingsCard card = new()
			{
				Location = new Point(4, top),
				Size = new Size(760, cardHeight),
				FillColor = SettingsPalette.Card,
				BorderColor = SettingsPalette.Divider
			};
			card.Controls.Add(new Label
			{
				Text = title,
				Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
				Location = new Point(20, 15),
				Size = new Size(710, 26),
				ForeColor = SettingsPalette.PrimaryText
			});

			int rowTop = 48;
			foreach ((string label, string value) in rows)
			{
				card.Controls.Add(new Label
				{
					Text = label,
					Location = new Point(20, rowTop),
					Size = new Size(190, 25),
					ForeColor = SettingsPalette.SecondaryText
				});
				card.Controls.Add(new Label
				{
					Text = string.IsNullOrWhiteSpace(value)
						? LocalizationManager.Get("DynamicText.DC12BEC5D71F167B495F")
						: value,
					Location = new Point(218, rowTop),
					Size = new Size(516, 25),
					ForeColor = SettingsPalette.PrimaryText,
					AutoEllipsis = true
				});
				rowTop += 32;
			}

			if (!string.IsNullOrWhiteSpace(note))
			{
				card.Controls.Add(new Label
				{
					Text = note,
					Location = new Point(20, rowTop + 4),
					Size = new Size(714, 48),
					ForeColor = SettingsPalette.SecondaryText
				});
			}

			_content.Controls.Add(card);
		}

		private static string FormatPorts(GameInfo game)
		{
			if (GameDatabase.IsMinecraft(game.Game))
				return LocalizationManager.Get("Catalog.Details.Ports.Minecraft");

			List<string> ports = [];
			if (game.Port > 0)
				ports.Add(LocalizationManager.Get("Connection.Port.Game", game.Port));
			if (game.QueryPort > 0)
				ports.Add(LocalizationManager.Get("Connection.Port.Query", game.QueryPort));
			if (game.AppPort > 0)
				ports.Add(LocalizationManager.Get("Connection.Port.App", game.AppPort));
			return ports.Count == 0
				? LocalizationManager.Get("Catalog.Details.Ports.AssignedDuringSetup")
				: string.Join(", ", ports);
		}

		private static string GetPlayerDetails(GameInfo game) =>
			game.CrossplayDisablesPlayerTracking
				? LocalizationManager.Get("Catalog.Details.PlayerData.SteamCrossplay")
				: GameDatabase.GetProbeProtocol(game) == ServerProbeProtocol.A2S
				? LocalizationManager.Get("Catalog.Details.PlayerData.NamedCount")
				: GameDatabase.IsMinecraft(game.Game)
					? LocalizationManager.Get("Catalog.Details.PlayerData.Minecraft")
					: LocalizationManager.Get("Catalog.Details.PlayerData.Unavailable");

		private static string FormatProbe(GameInfo game) =>
			GameDatabase.IsMinecraft(game.Game)
				? LocalizationManager.Get("Catalog.Details.Probe.Minecraft")
				: game.CrossplayDisablesPlayerTracking
					? LocalizationManager.Get("Catalog.Details.Probe.Crossplay")
				: GameDatabase.GetProbeProtocol(game) switch
			{
				ServerProbeProtocol.A2S => LocalizationManager.Get("Catalog.Details.Probe.Steam"),
				ServerProbeProtocol.EpicOnlineServices => LocalizationManager.Get("Catalog.Details.Probe.Eos"),
				ServerProbeProtocol.RestApi => LocalizationManager.Get("Catalog.Details.Probe.WebApi"),
				ServerProbeProtocol.Tcp => LocalizationManager.Get("Catalog.Details.Probe.Port"),
				_ => LocalizationManager.Get("Catalog.Details.Probe.Automatic")
			};

		private static string FormatConfigurationFile(GameInfo game)
		{
			if (!string.IsNullOrWhiteSpace(game.RelativeConfigPath))
				return game.RelativeConfigPath;
			return game.ConfigFileCreation switch
			{
				ConfigFileCreationMode.LaunchArgumentsOnly => LocalizationManager.Get("Catalog.Details.Config.LaunchArguments"),
				ConfigFileCreationMode.GameGenerated => LocalizationManager.Get("Catalog.Details.Config.GameGenerated"),
				ConfigFileCreationMode.SynixTemplate => LocalizationManager.Get("Catalog.Details.Config.SynixTemplate"),
				_ => LocalizationManager.Get("Catalog.Details.Config.Undefined")
			};
		}
	}
}
