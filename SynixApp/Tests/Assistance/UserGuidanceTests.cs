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
using Synix_Control_Panel.SynixApp.Design.Controls;
using Synix_Control_Panel.SynixApp.MonitoringHandler;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using System.Diagnostics;
using System.Windows.Forms;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class UserGuidanceTests
{
	[Theory]
	[InlineData("The game port is already being used", "network port")]
	[InlineData("Required executable is missing", "required server file")]
	[InlineData("Server process is still running after shutdown", "parts of the game server")]
	[InlineData("Access to the path was denied", "Windows would not allow")]
	[InlineData("Game not found in database", "game definition")]
	[InlineData("This game requires AVX2", "known requirement")]
	public void PlainEnglishErrors_ExplainCommonProblems(
		string technicalDetails,
		string expectedText)
	{
		PlainEnglishError error = UserGuidance.TranslateError(
			"start the server",
			technicalDetails);

		Assert.Contains(expectedText, error.Explanation, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(technicalDetails, error.TechnicalDetails);
	}

	[Fact]
	public void AddOnSecurityReview_IsNotMisreportedAsAFilePermissionProblem()
	{
		const string technicalDetails =
			"Synix blocked this package. Microsoft Defender blocked the security review. " +
			"Add-on code inherits the game server's Windows permissions.";

		PlainEnglishError error = UserGuidance.TranslateError(
			"install the selected add-on",
			technicalDetails);

		Assert.Contains("security review", error.Explanation, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("Windows would not allow", error.Explanation, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(technicalDetails, error.TechnicalDetails);
	}

	[Fact]
	public void SetupCompletion_ReachesOneHundredOnlyWhenReady()
	{
		Assert.Equal(90, UserGuidance.CalculateSetupCompletion(new SetupCompletionState(
			true, true, true, true, true, false)));
		Assert.Equal(100, UserGuidance.CalculateSetupCompletion(new SetupCompletionState(
			true, true, true, true, true, true)));
	}

	[Fact]
	public void LiveProcessDetails_KeepEveryRegisteredPid()
	{
		GameServer server = new()
		{
			PID = 101,
			ServerProcesses =
			[
				new ServerProcessIdentity { ProcessId = 101 },
				new ServerProcessIdentity { ProcessId = 202 },
				new ServerProcessIdentity { ProcessId = 303 }
			]
		};

		Assert.Equal([101, 202, 303], ResourceMonitor.GetProcessIds(server).Order().ToArray());
	}

	[Fact]
	public async Task ResourceMonitorSampling_DoesNotBlockTheCallingThread()
	{
		using ManualResetEventSlim discoveryGate = new(false);
		ResourceMonitorSnapshotSampler sampler = new(_ =>
		{
			discoveryGate.Wait(TimeSpan.FromSeconds(5));
			return [];
		});
		GameServer server = new() { ServerName = "Responsiveness Test" };
		Task<ResourceUsageSnapshot>? samplingTask = null;

		try
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			samplingTask = sampler.CaptureAsync([server], 32, CancellationToken.None);
			stopwatch.Stop();

			Assert.False(samplingTask.IsCompleted);
			Assert.True(
				stopwatch.Elapsed < TimeSpan.FromSeconds(1),
				$"Starting a resource sample blocked for {stopwatch.Elapsed.TotalMilliseconds:N0} ms.");
		}
		finally
		{
			discoveryGate.Set();
		}

		Assert.NotNull(samplingTask);
		await samplingTask.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void FirstStartAssistant_IncludesChecklistAndConnectionHelp()
	{
		GameServer server = new()
		{
			Game = "Unknown Test Game",
			ServerName = "My Server",
			InstallPath = Path.Combine(Path.GetTempPath(), "SynixGuidanceTest"),
			Port = 27015,
			QueryPort = 27016
		};

		string text = WarningDatabase.BuildAssistantText(server);

		Assert.Contains("SETUP COMPLETION", text);
		Assert.Contains("AUTOMATIC SAFETY CHECKLIST", text);
		Assert.Contains("CONFIGURATION SUPPORT", text);
		Assert.Contains("CONNECTION INFORMATION", text);
	}

	[Fact]
	public void ConnectionAddress_UsesTheGamePort()
	{
		Assert.Equal(
			"192.168.1.50:8777",
			ConnectionInformationDialog.FormatAddress("192.168.1.50", 8777));
	}

	[Fact]
	public void ServerSettingsExplainPortMappingsFromArgumentsOrConfigurations()
	{
		GameInfo enshrouded = GameDatabase.GetGame("Enshrouded")!;
		GameInfo abioticFactor = GameDatabase.GetGame("Abiotic Factor")!;

		string templateSummary = ServerSettingsGUI.GetPortMappingSummary(enshrouded);
		Assert.Contains("Needs mapping: Game Port", templateSummary);
		Assert.DoesNotContain("Query Port", templateSummary);
		Assert.Equal(
			"All declared ports are mapped by arguments or configuration.",
			ServerSettingsGUI.GetPortMappingSummary(abioticFactor));
	}

	[Fact]
	public void GuidanceWindows_ConstructWithoutRunningExternalChecks()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				GameServer server = new()
				{
					Game = "Unknown Test Game",
					ServerName = "Test",
					InstallPath = Path.GetTempPath(),
					Port = 27015
				};
				using PlainEnglishErrorDialog error = new("start the server", "port is in use");
				using ConnectionInformationDialog connection = new(server);
				using ResourceMonitorGUI processes = new(server);
				using ServerSettingsGUI setup = new();
				using WarningDatabase firstStart = new(new GameServer
				{
					Game = "Palworld",
					ServerName = "Installed Server",
					InstallPath = Path.GetTempPath(),
					Port = 8211,
					QueryPort = 8212
				});
				Control modeButton = setup.Controls.Find("btnExperienceMode", true).Single();
				Control modeBadge = setup.Controls.Find("lblModeBadge", true).Single();
				Control progress = setup.Controls.Find("setupProgress", true).Single();
				Control pageDescription = setup.Controls.Find("lblPageDescription", true).Single();
				Control pageHost = setup.Controls.Find("pnlPageHost", true).Single();
				Assert.False(modeButton.Bounds.IntersectsWith(modeBadge.Bounds));
				Assert.False(progress.Bounds.IntersectsWith(pageDescription.Bounds));
				Assert.False(progress.Bounds.IntersectsWith(pageHost.Bounds));
				Assert.Equal(
					"Start Server",
					firstStart.Controls.Find("btnStart", true).Single().Text);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void EnshroudedServerSettingsCapThePlayerInputAtSixteen()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ServerSettingsGUI setup = new(new GameServer
				{
					Game = "Enshrouded",
					ServerName = "Enshrouded Test",
					InstallPath = Path.GetTempPath(),
					Port = 15636,
					QueryPort = 15637,
					MaxPlayers = 64
				});
				ModernSettingsNumericUpDown playerLimit = Assert.IsType<ModernSettingsNumericUpDown>(
					setup.Controls.Find("numMaxPlayers", true).Single());
				ModernSettingsNumericUpDown gamePort = Assert.IsType<ModernSettingsNumericUpDown>(
					setup.Controls.Find("numPort", true).Single());
				ModernSettingsNumericUpDown queryPort = Assert.IsType<ModernSettingsNumericUpDown>(
					setup.Controls.Find("numQueryPort", true).Single());
				ModernSettingsNumericUpDown appPort = Assert.IsType<ModernSettingsNumericUpDown>(
					setup.Controls.Find("numAppPort", true).Single());
				TextBox adminPassword = Assert.IsType<TextBox>(
					setup.Controls.Find("txtAdminPassword", true).Single());
				Button saveButton = Assert.IsAssignableFrom<Button>(
					setup.Controls.Find("btnSave", true).Single());

				Assert.Equal(16, playerLimit.Maximum);
				Assert.Equal(16, playerLimit.Value);
				Assert.False(gamePort.Enabled);
				Assert.True(queryPort.Enabled);
				Assert.False(appPort.Enabled);
				Assert.Empty(setup.Controls.Find("lblTemplateBehavior", true));
				Assert.True(adminPassword.Enabled);
				Assert.False(saveButton.Enabled);
				Assert.Equal(
					"Max Players (maximum 16)",
					setup.Controls.Find("MaxPlayerLabel", true).Single().Text);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();

		Assert.Null(failure);
	}

	[Fact]
	public void WindroseServerSettingsExposeInviteCodeAndCapPlayersAtEight()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ServerSettingsGUI setup = new(new GameServer
				{
					Game = "Windrose",
					ServerName = "Windrose Test",
					InstallPath = Path.GetTempPath(),
					Port = 65401,
					QueryPort = 65402,
					MaxPlayers = 20,
					InviteCode = "friends8"
				});
				setup.Show();
				Application.DoEvents();
				Assert.IsType<ModernSettingsNavButton>(setup.Controls.Find(
					"btnNavSecurity",
					true).Single()).PerformClick();
				Application.DoEvents();

				ModernSettingsNumericUpDown playerLimit =
					Assert.IsType<ModernSettingsNumericUpDown>(setup.Controls.Find(
						"numMaxPlayers",
						true).Single());
				Control inviteCard = setup.Controls.Find("cardInviteCode", true).Single();
				Control tokenCard = setup.Controls.Find(
					"cardAuthenticationToken",
					true).Single();
				TextBox inviteCode = Assert.IsType<TextBox>(setup.Controls.Find(
					"txtInviteCode",
					true).Single());

				Assert.Equal(8, playerLimit.Maximum);
				Assert.Equal(8, playerLimit.Value);
				Assert.Equal(
					"Max Players (maximum 8)",
					setup.Controls.Find("MaxPlayerLabel", true).Single().Text);
				Assert.True(inviteCard.Visible);
				Assert.False(tokenCard.Visible);
				Assert.True(inviteCode.Enabled);
				Assert.Equal("friends8", inviteCode.Text);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(10)));

		Assert.Null(failure);
	}

	[Fact]
	public void EcoServerSettingsShowOnlyItsOnlineAuthenticationTokenCard()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ServerSettingsGUI setup = new(new GameServer
				{
					Game = "Eco",
					ServerName = "Eco Test",
					InstallPath = Path.GetTempPath(),
					Port = 61466,
					QueryPort = 61467
				});
				setup.Show();
				Application.DoEvents();
				Control securityPage = setup.Controls.Find(
					"pnlPageSecurity",
					true).Single();
				ModernSettingsNavButton securityNavigation = Assert.IsType<ModernSettingsNavButton>(setup.Controls.Find(
					"btnNavSecurity",
					true).Single());
				Control credentialsCard = setup.Controls.Find(
					"cardCredentials",
					true).Single();
				Control tokenCard = setup.Controls.Find(
					"cardAuthenticationToken",
					true).Single();
				Control inviteCard = setup.Controls.Find(
					"cardInviteCode",
					true).Single();
				TextBox token = Assert.IsType<TextBox>(setup.Controls.Find(
					"txtAuthenticationToken",
					true).Single());
				Button getToken = Assert.IsAssignableFrom<Button>(setup.Controls.Find(
					"btnAuthenticationTokenHelp",
					true).Single());
				Button save = Assert.IsAssignableFrom<Button>(setup.Controls.Find(
					"btnSave",
					true).Single());
				Label footer = Assert.IsAssignableFrom<Label>(setup.Controls.Find(
					"lblFooterStatus",
					true).Single());

				Assert.Same(securityPage, credentialsCard.Parent);
				Assert.Same(securityPage, tokenCard.Parent);
				securityNavigation.PerformClick();
				Application.DoEvents();
				Assert.True(securityPage.Visible);
				Assert.True(tokenCard.Visible);
				Assert.False(inviteCard.Visible);
				Assert.True(token.Enabled);
				Assert.True(getToken.Visible);
				Assert.Equal(
					"Eco User Token",
					setup.Controls.Find("lblAuthenticationToken", true).Single().Text);
				Assert.False(save.Enabled);
				Assert.Contains("Eco User Token", footer.Text, StringComparison.Ordinal);
				Assert.True(securityNavigation.AttentionRequired);

				token.Text = "eco-user-token_123.test";
				DateTime timeout = DateTime.UtcNow.AddSeconds(2);
				while (!save.Enabled && DateTime.UtcNow < timeout)
				{
					Application.DoEvents();
					Thread.Sleep(10);
				}

				Assert.True(save.Enabled, footer.Text);
				Assert.False(securityNavigation.AttentionRequired);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(10)));

		Assert.Null(failure);
	}

	[Fact]
	public void ServerSettingsNavigationHighlightsThePageThatNeedsAttention()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ServerSettingsGUI setup = new();
				ModernSettingsNavButton general = Assert.IsType<ModernSettingsNavButton>(
					setup.Controls.Find("btnNavGeneral", true).Single());
				ModernSettingsNavButton security = Assert.IsType<ModernSettingsNavButton>(
					setup.Controls.Find("btnNavSecurity", true).Single());
				ModernSettingsNavButton network = Assert.IsType<ModernSettingsNavButton>(
					setup.Controls.Find("btnNavNetwork", true).Single());
				ModernSettingsNavButton install = Assert.IsType<ModernSettingsNavButton>(
					setup.Controls.Find("btnNavInstall", true).Single());

				Assert.True(general.AttentionRequired);
				Assert.False(security.AttentionRequired);
				Assert.False(network.AttentionRequired);
				Assert.False(install.AttentionRequired);
				Assert.Contains(
					"require attention",
					general.AccessibleDescription,
					StringComparison.OrdinalIgnoreCase);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(10)));

		Assert.Null(failure);
	}

	[Fact]
	public void ServerSettingsSecurityNavigationDoesNotOverlapStatusAtMinimumSize()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ServerSettingsGUI setup = new();
				setup.Size = setup.MinimumSize;
				setup.Show();
				Application.DoEvents();

				Control installNavigation = setup.Controls.Find(
					"btnNavInstall",
					true).Single();
				Control sidebarStatus = setup.Controls.Find(
					"pnlSidebarStatus",
					true).Single();

				Assert.True(
					installNavigation.Bottom < sidebarStatus.Top,
					$"Install navigation ends at {installNavigation.Bottom}, but status begins at {sidebarStatus.Top}.");
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(10)));

		Assert.Null(failure);
	}

	[Fact]
	public void ServerSettingsTabsUseSeparateDesignerBackedPages()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ServerSettingsGUI setup = new();
				ComboBox minecraftEdition = Assert.IsAssignableFrom<ComboBox>(
					setup.Controls.Find("cmbMinecraftEdition", true).Single());

				Assert.IsType<ServerSettingsGeneralPage>(setup.Controls.Find("pnlPageGeneral", true).Single());
				Assert.IsType<ServerSettingsSecurityPage>(setup.Controls.Find("pnlPageSecurity", true).Single());
				Assert.IsType<ServerSettingsWorldPage>(setup.Controls.Find("pnlPageWorld", true).Single());
				Assert.IsType<ServerSettingsNetworkPage>(setup.Controls.Find("pnlPageNetwork", true).Single());
				Assert.IsType<ServerSettingsAutomationPage>(setup.Controls.Find("pnlPageAutomation", true).Single());
				Assert.IsType<DiscordSettingsPage>(setup.Controls.Find("discordSettingsPage", true).Single());
				Assert.IsType<ServerSettingsInstallPage>(setup.Controls.Find("pnlPageInstall", true).Single());
				Assert.Equal(
					[MinecraftControlProfile.JavaEdition, MinecraftControlProfile.BedrockEdition],
					minecraftEdition.Items.Cast<string>());
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(10)));

		Assert.Null(failure);
	}

	[Fact]
	[Trait("Category", "Regression")]
	public void EditingAValheimPasswordRefreshesTheSaveGate()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ServerSettingsGUI setup = new(new GameServer
				{
					Game = "Valheim",
					ServerName = "454",
					Password = "454",
					InstallPath = Path.GetTempPath(),
					Port = 61456,
					QueryPort = 61457,
					MaxPlayers = 10,
					WorldName = "Dedicated"
				});
				TextBox password = Assert.IsType<TextBox>(
					setup.Controls.Find("txtPassword", true).Single());
				Button save = Assert.IsAssignableFrom<Button>(
					setup.Controls.Find("btnSave", true).Single());

				Assert.False(save.Enabled);
				password.Text = "5555555";
				DateTime timeout = DateTime.UtcNow.AddSeconds(2);
				while (!save.Enabled && DateTime.UtcNow < timeout)
				{
					Application.DoEvents();
					Thread.Sleep(10);
				}

				Assert.True(save.Enabled);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(10)));

		Assert.Null(failure);
	}
}
