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
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.Design.Controls;
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class WorldSeedSetupTests
{
	[Fact]
	public void GeneratorAndAutomaticDefaultFollowSeedSupportAcrossTheEntireCatalog()
	{
		RunOnSta(() =>
		{
			using Form host = new() { ClientSize = new Size(914, 496) };
			using ServerSettingsWorldPage page = new() { Dock = DockStyle.Fill };
			host.Controls.Add(page);
			ShowOffscreen(host);
			foreach (GameInfo game in GameDatabase.GetGameList())
			{
				bool supported = GameFix.GetManagementCapabilities(game).HasFlag(GameManagementCapability.WorldSeed);
				Assert.Equal(supported, GameServerInputValidator.CanGenerateWorldSeed(game));
				page.ConfigureForGame(game, isMinecraftBedrock: false);
				page.ApplyAvailability(hasGame: true);
				Assert.Equal(supported, page.btnGenerateSeed.Visible);
				Assert.Equal(supported, page.btnGenerateSeed.Enabled);
				if (supported)
				{
					page.txtWorldSeed.Text = string.Empty;
					page.EnsureRandomSeedForNewServer();
					AssertValidGeneratedSeed(page.WorldSeed);
					Assert.True(page.TryValidate(out string error), error);
					string first = page.WorldSeed;
					page.EnsureRandomSeedForNewServer();
					Assert.Equal(first, page.WorldSeed);
					page.txtWorldSeed.Text = "replace-on-click";
					page.btnGenerateSeed.PerformClick();
					AssertValidGeneratedSeed(page.WorldSeed);
					Assert.True(page.TryValidate(out error), error);
				}
				else
				{
					page.EnsureRandomSeedForNewServer();
					Assert.Empty(page.WorldSeed);
				}
			}
			page.ConfigureForGame(GameDatabase.GetGame("Minecraft"), isMinecraftBedrock: true);
			page.ApplyAvailability(hasGame: true);
			page.txtWorldSeed.Text = string.Empty;
			page.EnsureRandomSeedForNewServer();
			Assert.True(page.btnGenerateSeed.Visible);
			AssertValidGeneratedSeed(page.WorldSeed);
		});
	}

	[Fact]
	public void NewServerSelectionAndSaveAttemptFillOnlyEmptySeedFields()
	{
		RunOnSta(() =>
		{
			using ServerSettingsGUI setup = new();
			ShowOffscreen(setup);
			ComboBox games = Find<ComboBox>(setup, "cmbGame");
			games.SelectedIndex = games.FindStringExact("Empyrion - Galactic Survival");
			Application.DoEvents();
			ServerSettingsWorldPage page = Find<ServerSettingsWorldPage>(setup, "pnlPageWorld");
			AssertValidGeneratedSeed(page.WorldSeed);
			page.txtWorldSeed.Text = "1011345";
			Invoke(setup, "NavigateSetupStep", 1);
			Assert.Equal("1011345", page.WorldSeed);
			page.txtWorldSeed.Text = string.Empty;
			// The name is deliberately empty: exercise the save gate, never save to the real registry.
			Find<TextBox>(setup, "txtName").Text = string.Empty;
			Invoke(setup, "btnSave_Click", Find<Button>(setup, "btnSave"), EventArgs.Empty);
			AssertValidGeneratedSeed(page.WorldSeed);
			Assert.Null(setup.NewServer);
			Assert.False(Find<Button>(setup, "btnSave").Enabled);
		});
	}

	[Theory]
	[InlineData("Minecraft", "a custom text seed")]
	[InlineData("Minecraft", "")]
	[InlineData("Empyrion - Galactic Survival", "545322654334")]
	[InlineData("Empyrion - Galactic Survival", "1011345")]
	public void LoadingAnExistingServerNeverRandomizesItsSavedSeed(string game, string seed)
	{
		RunOnSta(() =>
		{
			GameServer saved = new() { Game = game, WorldSeed = seed };
			using ServerSettingsWorldPage page = new();
			page.LoadServer(saved, GameDatabase.GetGame(game));
			page.ConfigureForGame(GameDatabase.GetGame(game), false);
			Assert.Equal(seed, page.WorldSeed);
			Assert.Equal(seed, saved.WorldSeed);
		});
	}

	[Theory]
	[InlineData("")]
	[InlineData("545322654334")]
	public void InvalidExistingEmpyrionSeedPointsToWorldGenerationAndBlocksSave(string seed)
	{
		RunOnSta(() =>
		{
			GameServer saved = ExistingEmpyrion(seed);
			using ServerSettingsGUI setup = new(saved);
			ShowOffscreen(setup);
			Invoke(setup, "NavigateSetupStep", 1);
			ServerSettingsWorldPage page = Find<ServerSettingsWorldPage>(setup, "pnlPageWorld");
			Assert.Equal(seed, page.WorldSeed);
			Assert.True(page.Visible);
			Assert.True(page.txtWorldSeed.Focused);
			Assert.False(Find<Button>(setup, "btnSave").Enabled);
			Assert.True(Find<ModernSettingsNavButton>(setup, "btnNavWorld").AttentionRequired);
			Assert.False(page.TryValidate(out string error));
			Assert.Contains("2147483647", error);
			page.btnGenerateSeed.PerformClick();
			AssertValidGeneratedSeed(page.WorldSeed);
			Invoke(setup, "NavigateSetupStep", 1);
			Assert.True(Find<Button>(setup, "btnSave").Enabled);
			Assert.Equal(seed, saved.WorldSeed);
			Assert.Null(setup.NewServer);
		});
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("fr-FR")]
	[InlineData("de-DE")]
	[InlineData("es-ES")]
	public void SeedControlsFitAndConfigSubtitleUsesTheFormatName(string language)
	{
		RunOnSta(() =>
		{
			LocalizationManager.Initialize(language);
			using ServerSettingsGUI setup = new(ExistingEmpyrion("1011345"));
			setup.Size = setup.MinimumSize;
			ShowOffscreen(setup);
			Find<Button>(setup, "btnNavWorld").PerformClick();
			ServerSettingsWorldPage page = Find<ServerSettingsWorldPage>(setup, "pnlPageWorld");
			Assert.True(page.btnGenerateSeed.Visible);
			Assert.Equal(LocalizationManager.Get("ServerSetup.World.Seed.Generate"), page.btnGenerateSeed.Text);
			Assert.Contains("2147483647", page.lblWorldSeedHint.Text);
			Assert.True(page.txtWorldSeed.Right < page.btnGenerateSeed.Left);
			Assert.True(page.btnGenerateSeed.Right < page.numWorldSize.Left);
			Assert.True(TextRenderer.MeasureText(page.btnGenerateSeed.Text, page.btnGenerateSeed.Font).Width <= page.btnGenerateSeed.Width - 12);
			Size hintSize = TextRenderer.MeasureText(page.lblWorldSeedHint.Text, page.lblWorldSeedHint.Font,
				new Size(page.lblWorldSeedHint.Width, int.MaxValue), TextFormatFlags.WordBreak);
			Assert.True(hintSize.Height <= page.lblWorldSeedHint.Height);
			Assert.True(page.lblWorldSeedHint.Bottom <= page.cardWorldGeneration.Height);
			if (Environment.GetEnvironmentVariable("SYNIX_RENDER_SEED_SETUP") == "1")
			{
				using Bitmap rendered = new(setup.Width, setup.Height);
				setup.DrawToBitmap(rendered, setup.ClientRectangle);
				rendered.Save(Path.Combine(AppContext.BaseDirectory, $"world-seed-setup-{language}.png"));
			}
			using ServerConfig editor = new(Path.Combine(Path.GetTempPath(), "dedicated.yaml"), ConfigFormat.YAML);
			Assert.Equal(LocalizationManager.Get("Configuration.Editor.Subtitle.Single", "dedicated.yaml", "YAML"),
				Find<Label>(editor, "lblPageSubtitle").Text);
		});
	}

	private static GameServer ExistingEmpyrion(string seed) => new()
	{
		Game = "Empyrion - Galactic Survival", ServerName = "Empyrion seed validation test",
		WorldSeed = seed, WorldName = "Default Multiplayer", Port = 61468, QueryPort = 61469,
		InstallPath = Path.Combine(Path.GetTempPath(), "SynixSeedUiTests", Guid.NewGuid().ToString("N"))
	};
	private static void AssertValidGeneratedSeed(string value)
	{
		Assert.True(int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int seed));
		Assert.InRange(seed, 1, GameServerInputValidator.NumericWorldSeedMaximum);
	}
	private static T Find<T>(Control parent, string name) where T : Control =>
		Assert.IsAssignableFrom<T>(parent.Controls.Find(name, true).Single());
	private static void Invoke(object target, string method, params object[] arguments) =>
		target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, arguments);
	private static void ShowOffscreen(Form form)
	{
		form.StartPosition = FormStartPosition.Manual;
		form.Location = new Point(-32000, -32000);
		form.ShowInTaskbar = false;
		form.Show();
		Application.DoEvents();
	}
	private static void RunOnSta(Action action)
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try { LocalizationManager.Initialize("en-US"); action(); }
			catch (Exception exception) { failure = exception; }
			finally { LocalizationManager.Initialize(LocalizationManager.DefaultLanguageCode); }
		}) { IsBackground = true };
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(45)), "World-seed UI check did not finish.");
		if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
	}
}
