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
using System.Text;

namespace Synix_Control_Panel.SynixApp.Database.GameConfigurations
{
	internal enum ConfigurationValidationState
	{
		Passed,
		Warning,
		Failed,
		Information
	}

	internal readonly record struct ConfigurationValidationItem(
		ConfigurationValidationState State,
		string Setting,
		string Message);

	internal sealed class ConfigurationValidationReport
	{
		public ConfigurationValidationReport(
			string gameName,
			int savedRevision,
			int currentRevision,
			bool fixConfigAvailable,
			IReadOnlyList<ConfigurationValidationItem> items)
		{
			GameName = gameName;
			SavedRevision = savedRevision;
			CurrentRevision = currentRevision;
			FixConfigAvailable = fixConfigAvailable;
			Items = items;
		}

		public string GameName { get; }
		public int SavedRevision { get; }
		public int CurrentRevision { get; }
		public bool FixConfigAvailable { get; }
		public IReadOnlyList<ConfigurationValidationItem> Items { get; }
		public int PassedCount => Items.Count(item =>
			item.State == ConfigurationValidationState.Passed);
		public int WarningCount => Items.Count(item =>
			item.State == ConfigurationValidationState.Warning);
		public int FailedCount => Items.Count(item =>
			item.State == ConfigurationValidationState.Failed);
		public bool IsCurrent => FailedCount == 0 && WarningCount == 0;

		public string ToPlainText()
		{
			StringBuilder report = new();
			report.AppendLine(LocalizationManager.Get(
				"Configuration.Report.Title"));
			report.AppendLine();
			report.AppendLine(LocalizationManager.Get(
				"Configuration.Report.Game",
				GameName));
			report.AppendLine(LocalizationManager.Get(
				"Configuration.Report.Result",
				LocalizationManager.Get(IsCurrent
					? "Configuration.Report.Result.Current"
					: "Configuration.Report.Result.Attention")));
			report.AppendLine(LocalizationManager.Get(
				"Configuration.Report.Counts",
				PassedCount,
				WarningCount,
				FailedCount));
			report.AppendLine(LocalizationManager.Get(
				"Configuration.Report.TemplateRevision",
				SavedRevision,
				CurrentRevision));
			report.AppendLine(LocalizationManager.Get(
				"Configuration.Report.FixConfig",
				LocalizationManager.Get(FixConfigAvailable
					? "Configuration.Report.FixConfig.Available"
					: "Configuration.Report.FixConfig.Unavailable")));
			report.AppendLine();

			foreach (ConfigurationValidationItem item in Items)
			{
				string state = LocalizationManager.Get(item.State switch
				{
					ConfigurationValidationState.Passed =>
						"Configuration.Report.State.Pass",
					ConfigurationValidationState.Warning =>
						"Configuration.Report.State.Warning",
					ConfigurationValidationState.Information =>
						"Configuration.Report.State.Information",
					_ => "Configuration.Report.State.Fail"
				});
				report.AppendLine(LocalizationManager.Get(
					"Configuration.Report.Item",
					state,
					LocalizationManager.TranslateKnownText(item.Setting)));
				report.AppendLine(LocalizationManager.TranslateRuntimeText(
					item.Message));
			}

			if (FixConfigAvailable)
			{
				report.AppendLine();
				report.AppendLine(LocalizationManager.Get(
					"Configuration.Report.FixConfig.Footer"));
			}

			return report.ToString().TrimEnd();
		}
	}
}
