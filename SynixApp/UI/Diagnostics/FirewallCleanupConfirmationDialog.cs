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
using Synix_Control_Panel.SynixApp.Design;

namespace Synix_Control_Panel.SynixApp.UI.Diagnostics
{
	internal sealed class FirewallCleanupConfirmationDialog : Form
	{
		internal FirewallCleanupConfirmationDialog(
			IReadOnlyList<string> executablePaths)
		{
			ArgumentNullException.ThrowIfNull(executablePaths);
			if (executablePaths.Count == 0)
				throw new ArgumentException(
					LocalizationManager.Get(
						"Diagnostics.FirewallCleanup.Error.PathRequired"),
					nameof(executablePaths));

			Text = LocalizationManager.Get("Text.0CD9F7C4C6770B0CB39E");
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = false;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			ClientSize = new Size(780, 664);
			BackColor = SettingsPalette.Window;
			ForeColor = SettingsPalette.PrimaryText;
			Font = new Font("Segoe UI", 9.5F);

			Controls.Add(new ModernSettingsGlyph
			{
				Glyph = "!",
				ForeColor = SettingsPalette.Warning,
				Location = new Point(28, 24),
				Size = new Size(44, 44)
			});
			Controls.Add(new Label
			{
				Name = "firewallCleanupHeading",
				Text = LocalizationManager.Get("Text.2BD271478D5133521DF1"),
				Font = new Font("Segoe UI", 18F, FontStyle.Bold),
				ForeColor = SettingsPalette.PrimaryText,
				Location = new Point(86, 20),
				Size = new Size(666, 42)
			});
			Controls.Add(new Label
			{
				Name = "firewallCleanupSubtitle",
				Text = BuildSummary(executablePaths.Count),
				ForeColor = SettingsPalette.SecondaryText,
				Location = new Point(88, 62),
				Size = new Size(664, 42)
			});

			ModernSettingsCard reasonCard = new()
			{
				Name = "firewallInspectionReasonCard",
				Location = new Point(28, 112),
				Size = new Size(724, 86),
				FillColor = SettingsPalette.InfoSurface,
				BorderColor = SettingsPalette.Divider,
				CornerRadius = 11
			};
			reasonCard.Controls.Add(new Label
			{
				Text = LocalizationManager.Get("Text.84915063F51DB634577C"),
				Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
				ForeColor = SettingsPalette.Accent,
				Location = new Point(18, 12),
				Size = new Size(688, 22)
			});
			reasonCard.Controls.Add(new Label
			{
				Name = "firewallInspectionReasonText",
				Text = LocalizationManager.Get("Text.89BF7052C30A2A455D98"),
				ForeColor = SettingsPalette.PrimaryText,
				Location = new Point(18, 38),
				Size = new Size(688, 38)
			});
			Controls.Add(reasonCard);

			ModernSettingsCard pathsCard = new()
			{
				Name = "firewallPathsCard",
				Location = new Point(28, 212),
				Size = new Size(724, 260),
				FillColor = SettingsPalette.Card,
				BorderColor = SettingsPalette.Divider,
				CornerRadius = 13
			};
			pathsCard.Controls.Add(new Label
			{
				Text = LocalizationManager.Get(
					"Diagnostics.FirewallCleanup.RuleCount",
					executablePaths.Count),
				Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
				ForeColor = SettingsPalette.Warning,
				Location = new Point(20, 16),
				Size = new Size(684, 24)
			});
			pathsCard.Controls.Add(new TextBox
			{
				Name = "firewallRuleList",
				AccessibleName = LocalizationManager.Get("Text.07E7C9C515328484C872"),
				Text = BuildPathList(executablePaths),
				Multiline = true,
				ReadOnly = true,
				WordWrap = false,
				ScrollBars = ScrollBars.Both,
				BackColor = SettingsPalette.Input,
				ForeColor = SettingsPalette.PrimaryText,
				BorderStyle = BorderStyle.FixedSingle,
				Font = new Font("Consolas", 9.5F),
				Location = new Point(20, 48),
				Size = new Size(684, 188)
			});
			Controls.Add(pathsCard);

			ModernSettingsCard safetyCard = new()
			{
				Name = "firewallSafetyCard",
				Location = new Point(28, 486),
				Size = new Size(724, 108),
				FillColor = SettingsPalette.InfoSurface,
				BorderColor = SettingsPalette.Warning,
				CornerRadius = 11
			};
			safetyCard.Controls.Add(new Label
			{
				Text = LocalizationManager.Get("Text.FF395F5EEE87F691A065"),
				Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
				ForeColor = SettingsPalette.Warning,
				Location = new Point(18, 10),
				Size = new Size(688, 22)
			});
			safetyCard.Controls.Add(new Label
			{
				Name = "firewallCleanupActionText",
				Text = LocalizationManager.Get("Text.9D352A1132C068237394"),
				ForeColor = SettingsPalette.PrimaryText,
				Location = new Point(18, 34),
				Size = new Size(688, 34)
			});
			safetyCard.Controls.Add(new Label
			{
				Name = "firewallCleanupSafetyText",
				Text = LocalizationManager.Get("Text.8DDA097844BB6EDCFA10"),
				ForeColor = SettingsPalette.SecondaryText,
				Location = new Point(18, 73),
				Size = new Size(688, 26)
			});
			Controls.Add(safetyCard);

			ModernSettingsButton cancelButton = new()
			{
				Name = "cancelFirewallCleanupButton",
				Text = LocalizationManager.Get("Text.19766ED6CCB2F4A32778"),
				Location = new Point(418, 608),
				Size = new Size(158, 42),
				DialogResult = DialogResult.Cancel
			};
			ModernSettingsButton removeButton = new()
			{
				Name = "confirmFirewallCleanupButton",
				Text = LocalizationManager.Get("Text.71F1CA24D00C97F88E3B"),
				AccessibleName = LocalizationManager.Get("Text.1315F512F1E9EDAD63BC"),
				Location = new Point(594, 608),
				Size = new Size(158, 42),
				DialogResult = DialogResult.OK,
				UseAccentStyle = true
			};
			Controls.Add(cancelButton);
			Controls.Add(removeButton);
			CancelButton = cancelButton;

			ThemeManager.Apply(this);
		}

		private static string BuildSummary(int count) =>
			LocalizationManager.Get(
				count == 1
					? "Diagnostics.FirewallCleanup.Summary.One"
					: "Diagnostics.FirewallCleanup.Summary.Many",
				count);

		private static string BuildPathList(
			IEnumerable<string> executablePaths) =>
			string.Join(
				Environment.NewLine,
				executablePaths.Select(path => $"• {path}"));
	}
}
