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
using Synix_Control_Panel.SynixEngine;

namespace Synix_Control_Panel.SynixApp.UI.ServerSetup
{
	public partial class ServerSettingsInstallPage : UserControl
	{
		private bool _isLoading;
		private Func<GameInfo?>? _selectedGameProvider;

		public event EventHandler? SettingsChanged;

		public ServerSettingsInstallPage()
		{
			InitializeComponent();
			LocalizationManager.BindAccessibleName(
				chkDefaultPath,
				"ServerSetup.Install.DefaultFolder.AccessibleName");
			chkDefaultPath.CheckedChanged += SettingsControlChanged;
			txtInstallPath.TextChanged += SettingsControlChanged;
			txtExtraArgs.TextChanged += SettingsControlChanged;
			btnBrowse.Click += BrowseClicked;
			btnViewArgs.Click += ViewArgumentsClicked;
		}

		public bool UseDefaultPath => chkDefaultPath.Checked;
		public string InstallPath => txtInstallPath.Text;
		public string ExtraArguments => txtExtraArgs.Text;
		public void SetAdvancedMode(bool enabled) =>
			cardLaunchArguments.Visible = enabled;

		public void Initialize(Func<GameInfo?> selectedGameProvider)
		{
			_selectedGameProvider = selectedGameProvider ??
				throw new ArgumentNullException(nameof(selectedGameProvider));
			txtInstallPath.ReadOnly = true;
			txtInstallPath.TabStop = false;
			txtInstallPath.ShortcutsEnabled = false;
			txtInstallPath.Cursor = Cursors.Default;
		}

		public void LoadServer(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			_isLoading = true;
			try
			{
				txtInstallPath.Text = server.InstallPath ?? string.Empty;
				chkDefaultPath.Checked = server.IsDefaultPath;
				txtExtraArgs.Text = server.ExtraArgs ?? string.Empty;
			}
			finally
			{
				_isLoading = false;
			}
		}

		public void SetInstallPath(string path)
		{
			txtInstallPath.Text = path ?? string.Empty;
		}

		public void UpdateAvailability(bool baseReady, bool editMode)
		{
			if (editMode)
			{
				chkDefaultPath.Enabled = false;
				btnBrowse.Enabled = false;
			}
			else
			{
				chkDefaultPath.Enabled = baseReady;
				btnBrowse.Enabled = baseReady && !chkDefaultPath.Checked;
			}

			txtInstallPath.Enabled = true;
		}

		public bool TryValidateExtraArguments(out string error) =>
			Core.TryValidateExtraArguments(txtExtraArgs.Text, out error);

		public void FocusExtraArguments() => txtExtraArgs.Focus();

		private void SettingsControlChanged(object? sender, EventArgs eventArgs)
		{
			if (!_isLoading)
				SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void BrowseClicked(object? sender, EventArgs eventArgs)
		{
			using FolderBrowserDialog browser = new();
			if (browser.ShowDialog(FindForm()) != DialogResult.OK)
				return;

			txtInstallPath.Text = browser.SelectedPath;
		}

		private void ViewArgumentsClicked(object? sender, EventArgs eventArgs)
		{
			GameInfo? gameData = _selectedGameProvider?.Invoke();
			if (gameData == null)
				return;

			using DefaultArgumentsDisplay display = new(gameData.RequiredArgs);
			display.ShowDialog(FindForm());
		}
	}
}
