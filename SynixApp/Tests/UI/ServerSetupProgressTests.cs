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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.Design.Controls;
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class ServerSetupProgressTests
{
	[Theory]
	[InlineData(false, false, false, "Attention,Waiting,Waiting,Waiting")]
	[InlineData(true, false, false, "Complete,Attention,Waiting,Waiting")]
	[InlineData(true, true, false, "Complete,Complete,Next,Ready")]
	[InlineData(true, true, true, "Complete,Complete,Complete,Next")]
	[InlineData(true, false, true, "Complete,Attention,Waiting,Waiting")]
	[InlineData(false, true, true, "Attention,Waiting,Waiting,Waiting")]
	public void CheckpointsFollowValidationNotVisitedTabs(bool details, bool ready, bool reviewed, string expected)
	{
		RunOnSta(() =>
		{
			using ServerSetupProgressStrip strip = new();
			strip.UpdateState(details, ready, reviewed);
			ServerSetupProgressStrip.StepButton[] buttons = Enumerable.Range(1, 4)
				.Select(index => Find<ServerSetupProgressStrip.StepButton>(strip, $"btnSetupStep{index}")).ToArray();
			Assert.Equal(expected, string.Join(",", buttons.Select(button => button.State)));
			Assert.True(buttons[0].Enabled);
			Assert.Equal(details, buttons[1].Enabled);
			Assert.Equal(details && ready, buttons[2].Enabled);
			Assert.Equal(details && ready, buttons[3].Enabled);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ProgressFitsWithoutTheOldBannerGapOrCoveringPagesOrSidebar(bool minimumSize)
	{
		RunOnSta(() =>
		{
			using ServerSettingsGUI setup = new();
			if (minimumSize) setup.Size = setup.MinimumSize;
			ShowOffscreen(setup);
			Control strip = Find<ServerSetupProgressStrip>(setup, "setupProgress");
			Control description = Find<Label>(setup, "lblPageDescription");
			Control pages = Find<Panel>(setup, "pnlPageHost");
			Assert.True(description.Bottom <= strip.Top);
			Assert.Empty(setup.Controls.Find("lblTemplateBehavior", true));
			Assert.InRange(pages.Top - strip.Bottom, 1, (int)Math.Ceiling(20 * setup.DeviceDpi / 96d));
			Assert.True(pages.Height >= 300);
			Assert.True(pages.Bottom <= pages.Parent!.ClientSize.Height);
			Assert.True(strip.Right <= strip.Parent!.ClientSize.Width);
			Assert.Empty(setup.Controls.Find("lblSetupCompletion", true));
			Assert.True(Find<ModernSettingsNavButton>(setup, "btnNavInstall").Bottom < Find<Panel>(setup, "pnlSidebarStatus").Top);
		});
	}

	[Fact]
	public void RequiredCheckpointFindsEcoTokenAndReviewNeverExposesSecretsOrSaves()
	{
		RunOnSta(() =>
		{
			using ServerSettingsGUI setup = CreateEcoSetup();
			ShowOffscreen(setup);
			Step(setup, 2).PerformClick();
			Assert.True(Find<ServerSettingsSecurityPage>(setup, "pnlPageSecurity").Visible);
			Assert.False(Find<Button>(setup, "btnSave").Enabled);
			Assert.Equal(ServerSetupProgressStrip.StepState.Attention, Step(setup, 2).State);

			Find<TextBox>(setup, "txtAuthenticationToken").Text = "eco-private-token_123.test";
			Find<TextBox>(setup, "txtPassword").Text = "private-password_123";
			Navigate(setup, 2);
			Assert.True(Find<Button>(setup, "btnSave").Enabled);
			Assert.True(Find<ServerSettingsReviewPage>(setup, "pnlPageReview").Visible);
			Assert.Equal(ServerSetupProgressStrip.StepState.Complete, Step(setup, 3).State);
			DataGridView grid = Find<DataGridView>(setup, "gridSummary");
			Assert.True(grid.ReadOnly);
			string[] values = grid.Rows.Cast<DataGridViewRow>().SelectMany(row => row.Cells.Cast<DataGridViewCell>())
				.Select(cell => cell.Value?.ToString() ?? string.Empty).ToArray();
			Assert.Contains("Eco Setup Progress Test", values);
			Assert.DoesNotContain("eco-private-token_123.test", values);
			Assert.DoesNotContain("private-password_123", values);

			Step(setup, 4).PerformClick();
			Assert.True(Find<Button>(setup, "btnSave").Focused);
			Assert.Null(setup.NewServer);
			Assert.Equal(DialogResult.None, setup.DialogResult);

			Find<TextBox>(setup, "txtAuthenticationToken").Text = string.Empty;
			Assert.Equal(ServerSetupProgressStrip.StepState.Waiting, Step(setup, 3).State);
			// A click/Enter before the debounce completes must still respect the gate.
			typeof(ServerSettingsGUI).GetMethod("btnSave_Click", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(setup, [Find<Button>(setup, "btnSave"), EventArgs.Empty]);
			Assert.False(Find<Button>(setup, "btnSave").Enabled);
			Assert.True(Find<ServerSettingsSecurityPage>(setup, "pnlPageSecurity").Visible);
			Assert.Null(setup.NewServer);
			Assert.Equal(DialogResult.None, setup.DialogResult);
		});
	}

	[Fact]
	public void EditingAfterReviewResetsTheCheckpointAndRefreshesTheSummary()
	{
		RunOnSta(() =>
		{
			using ServerSettingsGUI setup = CreateEcoSetup();
			ShowOffscreen(setup);
			Find<TextBox>(setup, "txtAuthenticationToken").Text = "eco-user-token_123.test";
			Navigate(setup, 2);
			Find<TextBox>(setup, "txtName").Text = "Updated server name";
			Assert.Equal(ServerSetupProgressStrip.StepState.Waiting, Step(setup, 3).State);
			Navigate(setup, 0);
			Assert.Equal(ServerSetupProgressStrip.StepState.Next, Step(setup, 3).State);
			Assert.True(Find<Button>(setup, "btnSave").Enabled); // Review remains optional.
			Navigate(setup, 2);
			Assert.Contains(Find<DataGridView>(setup, "gridSummary").Rows.Cast<DataGridViewRow>(),
				row => Equals(row.Cells[1].Value, "Updated server name"));
		});
	}

	[Theory]
	[InlineData("fr-FR", "Récapitulatif", "TERMINÉ")]
	[InlineData("de-DE", "Überprüfen", "ABGESCHLOSSEN")]
	[InlineData("es-ES", "Revisar", "COMPLETO")]
	public void OpenSetupUpdatesItsGuideAndReviewLanguageWithoutTranslatingValues(string language, string title, string complete)
	{
		RunOnSta(() =>
		{
			using ServerSettingsGUI setup = CreateEcoSetup();
			ShowOffscreen(setup);
			Find<TextBox>(setup, "txtName").Text = "Save";
			Find<TextBox>(setup, "txtAuthenticationToken").Text = "eco-user-token_123.test";
			Navigate(setup, 2);
			LocalizationManager.SetLanguage(language);
			LocalizationManager.Apply(setup);
			Assert.Equal(title, Step(setup, 3).Text);
			Assert.Equal(complete, Step(setup, 3).StatusText);
			DataGridView grid = Find<DataGridView>(setup, "gridSummary");
			Assert.Equal(LocalizationManager.Get("ServerSetup.Review.ServerName"), grid.Rows[0].Cells[0].Value);
			Assert.Equal("Save", grid.Rows[0].Cells[1].Value);
			Assert.Equal("Eco", grid.Rows[1].Cells[1].Value);
		});
	}

	private static ServerSettingsGUI CreateEcoSetup() => new(new GameServer
	{
		Game = "Eco",
		ServerName = "Eco Setup Progress Test",
		InstallPath = Path.GetTempPath(),
		Port = 61466,
		QueryPort = 61467
	});

	[Theory]
	[InlineData("en-US", true, false)]
	[InlineData("fr-FR", true, true)]
	[InlineData("de-DE", true, true)]
	[InlineData("es-ES", true, true)]
	[InlineData("en-US", false, true)]
	public void ReviewAndCheckpointTextFitInBothThemesAndAllLanguages(string language, bool darkMode, bool minimumSize)
	{
		RunOnSta(() =>
		{
			bool previousTheme = ThemeManager.IsDarkMode;
			try
			{
				ThemeManager.Initialize(darkMode);
				LocalizationManager.Initialize(language);
				using ServerSettingsGUI setup = CreateEcoSetup();
				if (minimumSize)
					setup.Size = setup.MinimumSize;
				ShowOffscreen(setup);
				Find<TextBox>(setup, "txtAuthenticationToken").Text = "eco-user-token_123.test";
				Navigate(setup, 2);
				Assert.True(Find<ServerSettingsReviewPage>(setup, "pnlPageReview").Visible);
				DataGridView grid = Find<DataGridView>(setup, "gridSummary");
				Assert.Equal(SettingsPalette.Input, grid.ColumnHeadersDefaultCellStyle.BackColor);
				Assert.Equal(SettingsPalette.SecondaryText, grid.ColumnHeadersDefaultCellStyle.ForeColor);
				Assert.Equal(SettingsPalette.Selection, grid.DefaultCellStyle.SelectionBackColor);
				Assert.Equal(LocalizationManager.Get("ServerSetup.Button.SaveChanges"), Step(setup, 4).Text);
				foreach (int index in Enumerable.Range(1, 4))
				{
					var button = Step(setup, index);
					Assert.True(TextRenderer.MeasureText(button.Text, button.Font).Width <= button.ClientSize.Width - 8);
					Assert.True(TextRenderer.MeasureText(button.StatusText, button.Font).Width <= button.ClientSize.Width - 8);
				}
				Label notice = Find<Label>(setup, "lblReviewNotice");
				Size measured = TextRenderer.MeasureText(notice.Text, notice.Font,
					new Size(notice.Width - notice.Padding.Horizontal, int.MaxValue), TextFormatFlags.WordBreak);
				Assert.True(measured.Height <= notice.Height - notice.Padding.Vertical);
				using Bitmap rendered = new(setup.Width, setup.Height);
				setup.DrawToBitmap(rendered, setup.ClientRectangle);
				// Optional artifacts stay in the test output folder, never in a live server.
				if (Environment.GetEnvironmentVariable("SYNIX_RENDER_SETUP_PROGRESS") == "1")
					rendered.Save(Path.Combine(AppContext.BaseDirectory, $"setup-review-{language}-{darkMode}.png"));
			}
			finally { ThemeManager.Initialize(previousTheme); }
		});
	}

	private static T Find<T>(Control parent, string name) where T : Control =>
		Assert.IsAssignableFrom<T>(parent.Controls.Find(name, true).Single());
	private static ServerSetupProgressStrip.StepButton Step(Control setup, int index) =>
		Find<ServerSetupProgressStrip.StepButton>(setup, $"btnSetupStep{index}");
	private static void Navigate(ServerSettingsGUI setup, int step) =>
		typeof(ServerSettingsGUI).GetMethod("NavigateSetupStep", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(setup, [step]);

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
		Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Setup progress check did not finish.");
		if (failure is not null)
			ExceptionDispatchInfo.Capture(failure).Throw();
	}
}
