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

namespace Synix_Control_Panel.SynixApp.UI.ServerSetup
{
	public partial class ServerSettingsAutomationPage : UserControl
	{
		private bool[] _selectedDays = new bool[7];
		private string _selectedTime = "04:00";
		private bool _smartMaintenanceEnabled = true;
		private bool _waitForPlayers = true;
		private int _maximumDelayMinutes = 30;
		private bool _backupBeforeRestart = true;
		private bool _updateBeforeRestart;
		private bool _isLoading;

		public event EventHandler? SettingsChanged;

		public ServerSettingsAutomationPage()
		{
			InitializeComponent();
			LocalizationManager.BindAccessibleName(
				chkEnableSchedule,
				"ServerSetup.Automation.EnableSchedule.AccessibleName");
			LocalizationManager.BindAccessibleName(
				chkUpdateOnStart,
				"ServerSetup.Automation.UpdateOnStart.AccessibleName");
			LocalizationManager.BindAccessibleName(
				chkBackupOnStart,
				"ServerSetup.Automation.BackupOnStart.AccessibleName");
			chkUpdateOnStart.CheckedChanged += SettingsControlChanged;
			chkBackupOnStart.CheckedChanged += SettingsControlChanged;
			chkEnableSchedule.CheckedChanged += ScheduleEnabledChanged;
			btnEditSchedule.Click += EditScheduleClicked;
		}

		public bool UpdateOnStart => chkUpdateOnStart.Checked;
		public bool BackupOnStart => chkBackupOnStart.Checked;
		public bool ScheduleEnabled => chkEnableSchedule.Checked;
		public bool[] SelectedDays => (bool[])_selectedDays.Clone();
		public string SelectedTime => _selectedTime;
		public bool SmartMaintenanceEnabled => _smartMaintenanceEnabled;
		public bool WaitForPlayers => _waitForPlayers;
		public int MaximumDelayMinutes => _maximumDelayMinutes;
		public bool BackupBeforeRestart => _backupBeforeRestart;
		public bool UpdateBeforeRestart => _updateBeforeRestart;
		public bool HasValidSchedule =>
			!ScheduleEnabled || _selectedDays.Any(selected => selected);

		public void LoadServer(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			_isLoading = true;
			try
			{
				chkUpdateOnStart.Checked = server.UpdateOnStart;
				chkBackupOnStart.Checked = server.BackupOnStart;
				chkEnableSchedule.Checked = server.IsScheduledRestartEnabled;
				_selectedDays = server.RestartDays != null
					? (bool[])server.RestartDays.Clone()
					: new bool[7];
				_selectedTime = server.RestartTime ?? "04:00";
				_smartMaintenanceEnabled = server.SmartMaintenanceEnabled;
				_waitForPlayers = server.MaintenanceWaitForPlayers;
				_maximumDelayMinutes = server.MaintenanceMaximumDelayMinutes;
				_backupBeforeRestart = server.MaintenanceBackupBeforeRestart;
				_updateBeforeRestart = server.MaintenanceUpdateBeforeRestart;
			}
			finally
			{
				_isLoading = false;
				UpdateAvailability(chkEnableSchedule.Enabled);
			}
		}

		public void UpdateAvailability(bool baseReady)
		{
			chkUpdateOnStart.Enabled = baseReady;
			chkBackupOnStart.Enabled = baseReady;
			chkEnableSchedule.Enabled = baseReady;
			btnEditSchedule.Enabled = baseReady && chkEnableSchedule.Checked;
		}

		private void SettingsControlChanged(object? sender, EventArgs eventArgs)
		{
			if (!_isLoading)
				SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void ScheduleEnabledChanged(object? sender, EventArgs eventArgs)
		{
			btnEditSchedule.Enabled =
				chkEnableSchedule.Enabled && chkEnableSchedule.Checked;
			SettingsControlChanged(sender, eventArgs);
		}

		private void EditScheduleClicked(object? sender, EventArgs eventArgs)
		{
			using ScheduleSettingsGUI scheduler = new(
				_selectedDays,
				_selectedTime,
				_smartMaintenanceEnabled,
				_waitForPlayers,
				_maximumDelayMinutes,
				_backupBeforeRestart,
				_updateBeforeRestart);
			if (scheduler.ShowDialog(FindForm()) != DialogResult.OK)
				return;

			_selectedDays = scheduler.SelectedDays;
			_selectedTime = scheduler.SelectedTime;
			_smartMaintenanceEnabled = scheduler.SmartMaintenanceEnabled;
			_waitForPlayers = scheduler.WaitForPlayers;
			_maximumDelayMinutes = scheduler.MaximumDelayMinutes;
			_backupBeforeRestart = scheduler.BackupBeforeRestart;
			_updateBeforeRestart = scheduler.UpdateBeforeRestart;
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
