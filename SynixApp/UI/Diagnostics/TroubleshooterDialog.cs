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
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.SteamCMDHandler;
using System.Diagnostics;

namespace Synix_Control_Panel.SynixApp.UI.Diagnostics
{
	internal sealed partial class TroubleshooterDialog : Form
	{
		private SynixHealthReport? _report;
		private bool _running;
		private bool _rowHeightRefreshPending;
		private readonly GameServer? _readinessServer;
		private bool IsReadinessMode => _readinessServer != null;

		internal TroubleshooterDialog()
		{
			InitializeComponent();
			GridStyler.DarkTheme(resultsGrid);
			GridStyler.ApplyDashboardTheme(resultsGrid);
			GridStyler.ApplyRoundedCorners(resultsGrid, 10);
			ConfigureResultsGridRendering();
			ThemeManager.Apply(this);
		}

		private void ConfigureResultsGridRendering()
		{
			resultsGrid.AutoSizeRowsMode =
				DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
			resultsGrid.RowTemplate.MinimumHeight = 36;
			resultsGrid.DefaultCellStyle.Padding = new Padding(8, 4, 4, 4);
			detailsColumn.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
			actionColumn.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);

			typeof(DataGridView).InvokeMember(
				"DoubleBuffered",
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.SetProperty,
				null,
				resultsGrid,
				[true]);

			resultsGrid.Scroll += ResultsGrid_Scroll;
			resultsGrid.SizeChanged += ResultsGrid_SizeChanged;
			resultsGrid.ColumnWidthChanged += ResultsGrid_ColumnWidthChanged;
		}

		internal TroubleshooterDialog(GameServer server) : this()
		{
			_readinessServer = server ?? throw new ArgumentNullException(nameof(server));
			Text = LocalizationManager.Get("Text.8C33F13ABED20D6638CD");
			LocalizationManager.BindText(
				titleLabel,
				"Text.8C33F13ABED20D6638CD");
			LocalizationManager.BindText(
				subtitleLabel,
				"Diagnostics.Readiness.Subtitle",
				server.ServerName,
				server.Game);
			resultColumn.HeaderText = LocalizationManager.Get("ModManager.Column.Status");
			subjectColumn.HeaderText = LocalizationManager.Get("Text.2C12824B5F626268A103");
			LocalizationManager.BindText(runButton, "Text.32E60E730D2E6845D352");
			LocalizationManager.BindText(actionButton, "Text.902EA0AF4FAA1C1AC056");
		}

		protected override async void OnShown(EventArgs eventArgs)
		{
			base.OnShown(eventArgs);
			await RunChecksAsync();
		}

		private async void RunButton_Click(object? sender, EventArgs eventArgs) =>
			await RunChecksAsync();

		private async Task RunChecksAsync()
		{
			if (_running)
				return;

			_running = true;
			runButton.Enabled = false;
			actionButton.Enabled = false;
			copyButton.Enabled = false;
			closeButton.Enabled = false;
			statusLabel.ForeColor = SettingsPalette.SecondaryText;
			LocalizationManager.BindText(
				statusLabel,
				IsReadinessMode
					? "Diagnostics.Readiness.CheckingServer"
					: "DynamicText.973CB1044C2D435579D5",
				_readinessServer?.ServerName ?? string.Empty);
			resultsGrid.Rows.Clear();

			try
			{
				Progress<string> progress = new(message =>
					statusLabel.Text = LocalizationManager.TranslateRuntimeText(message));
				GameServer[] servers = IsReadinessMode
					? [_readinessServer!]
					: ServerRegistry.Servers.ToArray();
				_report = await Task.Run(() => SynixTroubleshooter.RunAsync(
					servers,
					checkForUpdates: !IsReadinessMode,
					progress: progress,
					includeUpdateStatus: !IsReadinessMode));
				PopulateReport(_report);
				statusLabel.Text = IsReadinessMode
					? GetReadinessSummary(_report)
					: GetHealthSummary(_report);
				statusLabel.ForeColor = _report.FailedCount > 0
					? SettingsPalette.Danger
					: _report.WarningCount > 0
						? SettingsPalette.Warning
						: SettingsPalette.Success;
				copyButton.Enabled = true;
			}
			catch (Exception exception)
			{
				statusLabel.Text =
					LocalizationManager.Get("DynamicText.46A58E5D0D9BDB34C783") +
					exception.Message;
				statusLabel.ForeColor = SettingsPalette.Danger;
			}
			finally
			{
				_running = false;
				runButton.Enabled = true;
				closeButton.Enabled = true;
				UpdateActionButton();
			}
		}

		internal static string GetHealthSummary(SynixHealthReport report)
		{
			if (report.FailedCount > 0)
				return LocalizationManager.Get("Diagnostics.Health.Summary.Attention",
					report.FailedCount, report.WarningCount, report.PassedCount);
			return LocalizationManager.Get(report.WarningCount > 0
				? "Diagnostics.Health.Summary.Review"
				: "Diagnostics.Health.Summary.Healthy",
				report.WarningCount, report.PassedCount);
		}

		internal static string GetReadinessSummary(SynixHealthReport report)
		{
			int total = report.Items.Count;
			int readyPercent = total == 0
				? 0
				: (int)Math.Round(report.PassedCount * 100d / total);
			if (report.FailedCount > 0)
				return LocalizationManager.Get("Diagnostics.Readiness.Summary.NotReady",
					readyPercent, report.FailedCount, report.WarningCount);
			return LocalizationManager.Get(report.WarningCount > 0
				? "Diagnostics.Readiness.Summary.Review"
				: "Diagnostics.Readiness.Summary.Ready",
				readyPercent, report.WarningCount > 0 ? report.WarningCount : report.PassedCount);
		}

		private void PopulateReport(SynixHealthReport report)
		{
			resultsGrid.SuspendLayout();
			try
			{
				foreach (SynixHealthItem item in report.Items
					.OrderByDescending(item => item.Level)
					.ThenBy(item => item.Area, StringComparer.OrdinalIgnoreCase)
					.ThenBy(item => item.Subject, StringComparer.OrdinalIgnoreCase))
				{
					int index = resultsGrid.Rows.Add(
						IsReadinessMode
							? GetReadinessResultText(item.Level)
							: LocalizationManager.TranslateRuntimeText(item.ResultText),
						LocalizationManager.TranslateRuntimeText(item.Area),
						LocalizationManager.TranslateRuntimeText(item.Subject),
						LocalizationManager.TranslateRuntimeText(item.Details),
						GetActionText(item.Action));
					DataGridViewRow row = resultsGrid.Rows[index];
					row.Tag = item;
					Color color = item.Level switch
					{
						SynixHealthLevel.Passed => SettingsPalette.Success,
						SynixHealthLevel.Warning => SettingsPalette.Warning,
						_ => SettingsPalette.Danger
					};
					row.Cells[0].Style.ForeColor = color;
					row.Cells[0].Style.SelectionForeColor = color;
				}
			}
			finally
			{
				resultsGrid.ResumeLayout();
				RefreshGridRowHeights();
			}
		}

		private void ResultsGrid_Scroll(object? sender, ScrollEventArgs eventArgs)
		{
			resultsGrid.Invalidate(true);
		}

		private void ResultsGrid_SizeChanged(object? sender, EventArgs eventArgs) =>
			QueueGridRowHeightRefresh();

		private void ResultsGrid_ColumnWidthChanged(
			object? sender,
			DataGridViewColumnEventArgs eventArgs) =>
			QueueGridRowHeightRefresh();

		private void QueueGridRowHeightRefresh()
		{
			if (_rowHeightRefreshPending ||
				!IsHandleCreated ||
				IsDisposed ||
				Disposing)
			{
				return;
			}

			_rowHeightRefreshPending = true;
			BeginInvoke(() =>
			{
				_rowHeightRefreshPending = false;
				RefreshGridRowHeights();
			});
		}

		private void RefreshGridRowHeights()
		{
			if (resultsGrid.IsDisposed || resultsGrid.Rows.Count == 0)
				return;

			resultsGrid.AutoResizeRows(
				DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders);
			resultsGrid.Invalidate(true);
		}

		private static string GetReadinessResultText(SynixHealthLevel level) =>
			LocalizationManager.Get(level switch
		{
			SynixHealthLevel.Passed => "Diagnostics.Readiness.Result.Ready",
			SynixHealthLevel.Warning => "Diagnostics.Readiness.Result.Review",
			_ => "Diagnostics.Readiness.Result.Blocked"
		});

		private static string GetActionText(SynixHealthAction action)
		{
			string key = GetActionResourceKey(action);
			return string.IsNullOrEmpty(key)
				? string.Empty
				: LocalizationManager.Get(key);
		}

		private void ResultsGrid_SelectionChanged(object? sender, EventArgs eventArgs) =>
			UpdateActionButton();

		private void UpdateActionButton()
		{
			SynixHealthItem? item = GetSelectedItem();
			actionButton.Enabled = !_running && item is { Action: not SynixHealthAction.None };
			LocalizationManager.BindText(
				actionButton,
				item == null || item.Action == SynixHealthAction.None
					? IsReadinessMode
						? "Text.902EA0AF4FAA1C1AC056"
						: "Text.F19AA7C4D8DA593D73AE"
					: GetActionResourceKey(item.Action));
		}

		private static string GetActionResourceKey(SynixHealthAction action) => action switch
		{
			SynixHealthAction.RepairSteamCmd => "Diagnostics.Action.RepairSteamCmd",
			SynixHealthAction.ValidateServerFiles => "Diagnostics.Action.ValidateFiles",
			SynixHealthAction.FixConfiguration => "Text.4035C78A474280CE7F1E",
			SynixHealthAction.OpenServerFolder => "Diagnostics.Action.OpenFolder",
			SynixHealthAction.OpenFirewallSettings => "Diagnostics.Action.FirewallSettings",
			SynixHealthAction.RecoverProcesses => "Diagnostics.Action.RecoverProcesses",
			SynixHealthAction.OpenLatestLog => "Diagnostics.Action.OpenLog",
			SynixHealthAction.OpenUpdate => "Diagnostics.Action.OpenUpdates",
			_ => string.Empty
		};

		private SynixHealthItem? GetSelectedItem() =>
			resultsGrid.SelectedRows.Count > 0
				? resultsGrid.SelectedRows[0].Tag as SynixHealthItem
				: null;

		private async void ActionButton_Click(object? sender, EventArgs eventArgs)
		{
			SynixHealthItem? item = GetSelectedItem();
			if (item == null || item.Action == SynixHealthAction.None || _running)
				return;

			try
			{
				switch (item.Action)
				{
					case SynixHealthAction.RepairSteamCmd:
						LocalizationManager.BindText(
							statusLabel,
							"Text.777F9DDFDDB4FCF844BE");
						await Task.Run(() => SteamCMD.EnsureSteamCMD((message, color) =>
						{
							if (IsHandleCreated)
								BeginInvoke(() => statusLabel.Text = message);
						}));
						break;

					case SynixHealthAction.ValidateServerFiles when item.Server != null:
						await Core.Instance.UpdateServerAndReport(item.Server, "VALIDATE");
						break;

					case SynixHealthAction.FixConfiguration when item.Server != null:
						await FixConfigurationAsync(item.Server);
						break;

					case SynixHealthAction.OpenServerFolder when item.Server != null:
						Core.Instance.OpenServerFolder(item.Server);
						return;

					case SynixHealthAction.OpenFirewallSettings:
						Process.Start(new ProcessStartInfo("firewall.cpl") { UseShellExecute = true });
						return;

					case SynixHealthAction.RecoverProcesses:
						await Core.Instance.RebindProcesses();
						break;

					case SynixHealthAction.OpenLatestLog when item.Server != null:
						Core.Instance.OpenLatestGameLog(item.Server);
						return;

					case SynixHealthAction.OpenUpdate:
						Process.Start(new ProcessStartInfo(Core.ReleasesUri.AbsoluteUri) { UseShellExecute = true });
						return;
				}

				await RunChecksAsync();
			}
			catch (Exception exception)
			{
				PlainEnglishErrorDialog.ShowError(
					this,
					LocalizationManager.Get("Diagnostics.ErrorAction.FinishRepair"),
					exception.ToString());
			}
		}

		private async Task FixConfigurationAsync(GameServer server)
		{
			if (!string.Equals(
				server.Status,
				Core.StatusManager.GetStatus(Core.ServerState.Stopped),
				StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(LocalizationManager.Get(
					"Diagnostics.Configuration.StopBeforeRebuild"));
			}

			DialogResult confirmation = LocalizedMessageBox.Show(
				this,
				LocalizationManager.Get("MessageText.AA9402AAA45447FCB9A8"),
				LocalizationManager.Get("MessageText.E66DFFEC800B8E0880C2"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning,
				MessageBoxDefaultButton.Button2);
			if (confirmation != DialogResult.Yes)
				return;

			ConfigurationApplyResult result = await GameFix.ResetManagedConfiguration(server);
			if (!result.Succeeded || !result.Complete)
				throw new InvalidOperationException(result.Message);
			FileHandler.SaveServers();
			LocalizedMessageBox.Show(
				this,
				LocalizationManager.TranslateRuntimeText(result.Message),
				LocalizationManager.Get("MessageText.AEBC3D7D14735D41DB99"),
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}

		private void CopyButton_Click(object? sender, EventArgs eventArgs)
		{
			if (_report == null)
				return;
			try
			{
				Clipboard.SetText(_report.ToPlainText(
					IsReadinessMode
						? LocalizationManager.Get(
							"Diagnostics.Health.Report.ReadinessTitle")
						: LocalizationManager.Get(
							"Diagnostics.Health.Report.Title")));
				LocalizationManager.BindText(
					statusLabel,
					IsReadinessMode
						? "DynamicText.DBF742AF53EBF85C9E90"
						: "DynamicText.98343573B4075CAC328B");
			}
			catch
			{
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("MessageText.42714FAA1D98E9331F6D"),
					LocalizationManager.Get("MessageText.2C58B2D4975AADC6042D"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}
	}
}
