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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using Xunit;
using SynixSettings = Synix_Control_Panel.Properties.Settings;

namespace Synix_Control_Panel.Tests;

public sealed class ConfigurationRepairWorkflowTests
{
	public static IEnumerable<object[]> RepairCases()
	{
		foreach (string game in new[] { "Minecraft", "Minecraft Java", "Minecraft Bedrock",
			"Empyrion - Galactic Survival", "Dysterra", "Foundry", "Satisfactory", "Rust", "7 Days to Die" })
			foreach (bool disableAutomaticWrites in new[] { false, true })
				yield return [game, disableAutomaticWrites];
	}

	[Theory]
	[MemberData(nameof(RepairCases))]
	public async Task ExplicitRepairClearsTheHealthIssueAndPreservesTheDevelopmentPreference(
		string game, bool disableAutomaticWrites)
	{
		string root = Path.Combine(Path.GetTempPath(), "SynixRepairWorkflowTests", Guid.NewGuid().ToString("N"));
		bool previousPreference = SynixSettings.Default.DisablePremadeConfigurationsForDevelopment;
		Directory.CreateDirectory(root);
		try
		{
			SynixSettings.Default.DisablePremadeConfigurationsForDevelopment = disableAutomaticWrites;
			bool bedrock = game == "Minecraft Bedrock";
			GameInfo definition = GameDatabase.GetGame(bedrock ? "Minecraft" : game)!;
			GameServer server = new()
			{
				Game = definition.Game, MinecraftEdition = bedrock ? "Bedrock" : "Java",
				ServerName = "Isolated config repair", InstallPath = root,
				Port = definition.Port, QueryPort = definition.QueryPort, RconPort = 25575,
				MaxPlayers = 10, WorldSeed = "1011345", WorldSize = 4000,
				WorldName = definition.Maps.FirstOrDefault() ?? "world",
				GameMode = definition.GameModes.FirstOrDefault() ?? "Survival"
			};
			Assert.True(GameFix.TryGetConfiguration(server.Game, out ConfigurationDefinition? config));
			Assert.NotNull(config);
			Assert.False(config.RequiresNetworkAddresses);
			Assert.True(GameFix.CanManuallyResetManagedConfiguration(server, serverIsBusy: false));
			Assert.False(GameFix.CanManuallyResetManagedConfiguration(server, serverIsBusy: true));

			// The same backend is used by Fix Config in both diagnostic windows.
			SynixHealthItem missing = await ConfigurationHealth(server);
			Assert.Equal(SynixHealthLevel.Failed, missing.Level);
			Assert.Equal(SynixHealthAction.FixConfiguration, missing.Action);
			Assert.DoesNotContain(LocalizationManager.Get("Configuration.Check.DevelopmentSetting.Disabled"), missing.Details);
			ConfigurationApplyResult created = await GameFix.ResetManagedConfiguration(server);
			Assert.True(created.Succeeded && created.Complete, created.Message);
			Assert.Equal(config.SchemaVersion, server.ManagedConfigurationVersion);
			Assert.Equal("1011345", server.WorldSeed);
			await AssertCurrent(server);

			string path = config.ResolveConfigurationPaths(server).First();
			byte[] original = File.ReadAllBytes(path);
			server.MaxPlayers = 14;
			Assert.Equal(SynixHealthAction.FixConfiguration, (await ConfigurationHealth(server)).Action);
			ConfigurationApplyResult repaired = await GameFix.ResetManagedConfiguration(server);
			Assert.True(repaired.Succeeded && repaired.Complete, repaired.Message);
			Assert.True(GameFix.HasManagedConfigurationBackup(server));
			await AssertCurrent(server);
			Assert.Equal(disableAutomaticWrites, SynixSettings.Default.DisablePremadeConfigurationsForDevelopment);

			ConfigurationRestoreResult restored = GameFix.RestorePreviousManagedConfiguration(server);
			Assert.True(restored.Succeeded, restored.Message);
			Assert.Equal(original, File.ReadAllBytes(path));
		}
		finally
		{
			// No settings Save, server registry write, or real server process is used.
			SynixSettings.Default.DisablePremadeConfigurationsForDevelopment = previousPreference;
			Directory.Delete(root, recursive: true);
		}
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("fr-FR")]
	[InlineData("de-DE")]
	[InlineData("es-ES")]
	public void SummariesUseTheCorrectCountsForEveryOutcome(string language)
	{
		string previousLanguage = LocalizationManager.CurrentLanguageCode;
		try
		{
			LocalizationManager.Initialize(language);
			foreach ((int passed, int warnings, int failed) in new[] { (8, 2, 0), (6, 2, 1), (8, 0, 0), (0, 0, 0) })
			{
				SynixHealthReport report = HealthReport(passed, warnings, failed);
				int total = passed + warnings + failed;
				int percent = total == 0 ? 0 : (int)Math.Round(passed * 100d / total);
				string expectedReadiness = failed > 0
					? LocalizationManager.Get("Diagnostics.Readiness.Summary.NotReady", percent, failed, warnings)
					: warnings > 0
						? LocalizationManager.Get("Diagnostics.Readiness.Summary.Review", percent, warnings)
						: LocalizationManager.Get("Diagnostics.Readiness.Summary.Ready", percent, passed);
				Assert.Equal(expectedReadiness, TroubleshooterDialog.GetReadinessSummary(report));
				string expectedHealth = failed > 0
					? LocalizationManager.Get("Diagnostics.Health.Summary.Attention", failed, warnings, passed)
					: LocalizationManager.Get(warnings > 0 ? "Diagnostics.Health.Summary.Review" : "Diagnostics.Health.Summary.Healthy", warnings, passed);
				Assert.Equal(expectedHealth, TroubleshooterDialog.GetHealthSummary(report));
			}
			Assert.DoesNotContain("[Diagnostics.", TroubleshooterDialog.GetHealthSummary(HealthReport(8, 2, 0)));
		}
		finally { LocalizationManager.Initialize(previousLanguage); }
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("fr-FR")]
	[InlineData("de-DE")]
	[InlineData("es-ES")]
	public void ConfigurationDialogCountsAndInformationNotesMatchTheReport(string language)
	{
		RunOnSta(() =>
		{
			LocalizationManager.Initialize(language);
			foreach (bool failed in new[] { false, true })
			{
				List<ConfigurationValidationItem> items = Enumerable.Range(0, 7)
					.Select(index => new ConfigurationValidationItem(ConfigurationValidationState.Passed, $"Check {index}", "Current")).ToList();
				items.Add(new(ConfigurationValidationState.Information, "Development setting", "Automatic writes are disabled."));
				if (failed)
				{
					items.Add(new(ConfigurationValidationState.Failed, "Config", "Missing setting"));
					items.Add(new(ConfigurationValidationState.Warning, "Revision", "Old revision"));
					items.Add(new(ConfigurationValidationState.Warning, "Other", "Review"));
				}
				ConfigurationValidationReport report = new("Isolated test", 1, 1, true, items);
				Assert.Equal(!failed, report.IsCurrent);
				Assert.Equal(7, report.PassedCount);
				using ConfigurationValidationDialog dialog = new(report);
				Label summary = Assert.IsType<Label>(dialog.Controls.Find("_summaryLabel", true).Single());
				Assert.Equal(failed
					? LocalizationManager.Get("Configuration.Validation.Attention", 1, 2)
					: LocalizationManager.Get("Configuration.Validation.Current", 7), summary.Text);
				string note = LocalizationManager.Get("Configuration.Report.State.Information");
				Assert.DoesNotContain("[Configuration.", note);
				Assert.Contains(note, report.ToPlainText());
			}
			using TroubleshooterDialog empty = new();
			typeof(TroubleshooterDialog).GetMethod("UpdateActionButton", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(empty, null);
			Assert.False(empty.Controls.Find("actionButton", true).Single().Enabled);
		});
	}

	private static async Task AssertCurrent(GameServer server)
	{
		ConfigurationValidationReport report = await GameFix.ValidateManagedConfiguration(server);
		Assert.True(report.IsCurrent, report.ToPlainText());
		Assert.Equal(0, report.WarningCount);
		Assert.Equal(0, report.FailedCount);
		if (!GameFix.ManagedConfigurationsEnabled)
			Assert.Contains(report.Items, item => item.State == ConfigurationValidationState.Information);
		SynixHealthItem health = await ConfigurationHealth(server);
		Assert.Equal(SynixHealthLevel.Passed, health.Level);
		Assert.Equal(SynixHealthAction.None, health.Action);
	}

	private static async Task<SynixHealthItem> ConfigurationHealth(GameServer server)
	{
		List<SynixHealthItem> items = [];
		await SynixTroubleshooter.CheckConfigurationAsync(server, items);
		return Assert.Single(items);
	}

	private static SynixHealthReport HealthReport(int passed, int warnings, int failed) => new(DateTimeOffset.UtcNow,
		Enumerable.Repeat(SynixHealthLevel.Passed, passed)
			.Concat(Enumerable.Repeat(SynixHealthLevel.Warning, warnings))
			.Concat(Enumerable.Repeat(SynixHealthLevel.Failed, failed))
			.Select(level => new SynixHealthItem(level, "Test", "Test", "Test")).ToArray());

	private static void RunOnSta(Action action)
	{
		Exception? failure = null;
		string previousLanguage = LocalizationManager.CurrentLanguageCode;
		CultureInfo? previousDefault = CultureInfo.DefaultThreadCurrentUICulture;
		Thread thread = new(() =>
		{
			try { action(); }
			catch (Exception exception) { failure = exception; }
			finally
			{
				LocalizationManager.Initialize(previousLanguage);
				CultureInfo.DefaultThreadCurrentUICulture = previousDefault;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
	}
}
