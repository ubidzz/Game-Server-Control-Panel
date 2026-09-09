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
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.Design;

namespace Synix_Control_Panel.SynixApp.UI.Configuration
{
	internal sealed partial class ConfigurationValidationDialog : Form
	{
		public ConfigurationValidationDialog()
		{
			InitializeComponent();
			ThemeManager.Apply(this);
		}

		internal ConfigurationValidationDialog(
			ConfigurationValidationReport report)
			: this()
		{
			ArgumentNullException.ThrowIfNull(report);
			LocalizationManager.BindText(
				_titleLabel,
				"Configuration.Validation.Title",
				report.GameName);
			if (report.IsCurrent)
				LocalizationManager.BindText(_summaryLabel, "Configuration.Validation.Current",
					report.PassedCount);
			else
				LocalizationManager.BindText(_summaryLabel, "Configuration.Validation.Attention",
					report.FailedCount, report.WarningCount);
			_summaryLabel.ForeColor = report.IsCurrent
				? SettingsPalette.Success
				: report.FailedCount > 0
					? SettingsPalette.Warning
					: SettingsPalette.SecondaryText;
			_reportBox.Text = report.ToPlainText();
			_reportBox.SelectionStart = 0;
			_reportBox.ScrollToCaret();
		}

		private void CopyButton_Click(object? sender, EventArgs eventArgs)
		{
			try
			{
				Clipboard.SetText(_reportBox.Text);
				LocalizationManager.BindText(
					_summaryLabel,
					"Text.773BE7BF4AC974A6FE2E");
				_summaryLabel.ForeColor = SettingsPalette.Success;
			}
			catch (Exception suppressedException)
			{
				ApplicationLogService.WriteSuppressedException(suppressedException);
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("MessageText.C02FB698977B0364EC82"),
					LocalizationManager.Get("MessageText.2C58B2D4975AADC6042D"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}
	}
}
