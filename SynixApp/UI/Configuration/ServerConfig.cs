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
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixEngine;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Synix_Control_Panel.SynixApp.UI.Configuration
{
	public partial class ServerConfig : Form
	{
		private const uint WdaExcludeFromCapture = 0x00000011;
		private const int WmNcHitTest = 0x0084;
		private const int WmNcLeftButtonDown = 0x00A1;
		private const int HtCaption = 0x0002;
		private const int HtLeft = 10;
		private const int HtRight = 11;
		private const int HtTop = 12;
		private const int HtTopLeft = 13;
		private const int HtTopRight = 14;
		private const int HtBottom = 15;
		private const int HtBottomLeft = 16;
		private const int HtBottomRight = 17;
		private const int DwmWindowCornerPreference = 33;
		private const int DwmRound = 2;
		private const int ResizeBorder = 7;

		private static Color TextTypeColor => ThemeManager.IsDarkMode
			? Color.FromArgb(96, 165, 250)
			: Color.FromArgb(37, 99, 168);
		private static Color NumberTypeColor => ThemeManager.IsDarkMode
			? Color.FromArgb(167, 139, 250)
			: Color.FromArgb(109, 72, 184);
		private static Color BooleanTypeColor => SettingsPalette.Accent;
		private static Color SecretTypeColor => SettingsPalette.Warning;
		private static Color NullTypeColor => SettingsPalette.MutedText;

		private string _path = string.Empty;
		private ConfigFormat _format = ConfigFormat.StandardINI;
		private readonly GameServer? _server;
		private readonly bool _isRuntimeInstance;
		private readonly IReadOnlyList<ConfigurationEditorFile> _configurationFiles = [];
		private ModernSettingsComboBox? _fileSelector;
		private bool _fileSelectionIsUpdating;
		private int _selectedFileIndex;
		private List<ConfigLine> _fileData = new();
		private bool _rowsAreLoading;
		private bool _dataLoaded;
		private bool _allowClose;
		private bool _openBooleanDropDownOnEdit;
		private bool _booleanDropDownOpenQueued;
		private bool _templateResetInProgress;
		private int _booleanDropDownRowIndex = -1;

		public ServerConfig()
		{
			InitializeComponent();
			InitializeLocalizedControls();
			ConfigureBooleanGridEditing();
			if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
				ThemeManager.Apply(this);
		}

		public ServerConfig(string filePath, ConfigFormat format)
			: this(filePath, format, null)
		{
		}

		public ServerConfig(
			string filePath,
			ConfigFormat format,
			GameServer? server)
			: this([new ConfigurationEditorFile(filePath, format)], server)
		{
		}

		internal ServerConfig(
			IReadOnlyList<ConfigurationEditorFile> configurationFiles,
			GameServer? server)
		{
			InitializeComponent();
			InitializeLocalizedControls();
			ConfigureBooleanGridEditing();

			if (configurationFiles == null || configurationFiles.Count == 0)
			{
				throw new ArgumentException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.PathRequired"),
					nameof(configurationFiles));
			}

			_configurationFiles = configurationFiles
				.Where(file => !string.IsNullOrWhiteSpace(file.Path))
				.Select(file => file with { Path = Path.GetFullPath(file.Path) })
				.GroupBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.First())
				.ToArray();
			if (_configurationFiles.Count == 0)
			{
				throw new ArgumentException(
					LocalizationManager.Get(
						"Configuration.Editor.Error.ValidPathRequired"),
					nameof(configurationFiles));
			}

			_server = server;
			_isRuntimeInstance = true;
			_selectedFileIndex = FindInitialFileIndex(_configurationFiles);
			ApplySelectedFile();
			ConfigureFileSelector();
			ConfigureFilePresentation();
			ThemeManager.Apply(this);
		}

		private void InitializeLocalizedControls()
		{
			PopulateTypeFilterOptions();
			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
			{
				return;
			}

			LocalizationManager.LanguageChanged += InterfaceLanguageChanged;
			Disposed += (_, _) =>
				LocalizationManager.LanguageChanged -= InterfaceLanguageChanged;
		}

		private void InterfaceLanguageChanged(
			object? sender,
			EventArgs eventArgs)
		{
			PopulateTypeFilterOptions();
		}

		private void PopulateTypeFilterOptions()
		{
			int selectedIndex = Math.Max(0, cmbTypeFilter.SelectedIndex);
			cmbTypeFilter.Items.Clear();
			cmbTypeFilter.Items.AddRange(
			[
				LocalizationManager.Get("Option.ConfigType.All"),
				LocalizationManager.Get("Option.ConfigType.Text"),
				LocalizationManager.Get("Option.ConfigType.Number"),
				LocalizationManager.Get("Option.ConfigType.Boolean"),
				LocalizationManager.Get("Option.ConfigType.Secret"),
				LocalizationManager.Get("Option.ConfigType.Null")
			]);
			cmbTypeFilter.SelectedIndex = Math.Min(
				selectedIndex,
				cmbTypeFilter.Items.Count - 1);
		}

		private void ConfigureBooleanGridEditing()
		{
			dgvConfig.EditMode = DataGridViewEditMode.EditOnEnter;
			colSetting.ReadOnly = true;
			colType.ReadOnly = true;
			colSetting.CellTemplate = new ModernSettingsDataGridViewInformationalCell();
			colType.CellTemplate = new ModernSettingsDataGridViewInformationalCell();

			dgvConfig.CellMouseDown -= dgvConfig_CellMouseDown;
			dgvConfig.CellMouseDown += dgvConfig_CellMouseDown;
			dgvConfig.CellEnter -= dgvConfig_CellEnter;
			dgvConfig.CellEnter += dgvConfig_CellEnter;
			dgvConfig.EditingControlShowing -= dgvConfig_EditingControlShowing;
			dgvConfig.EditingControlShowing += dgvConfig_EditingControlShowing;
			dgvConfig.Scroll -= dgvConfig_Scroll;
			dgvConfig.Scroll += dgvConfig_Scroll;
		}

		protected override void OnShown(EventArgs eventArgs)
		{
			base.OnShown(eventArgs);
			EnableGridDoubleBuffering();
			if (_isRuntimeInstance && !_dataLoaded)
			{
				LoadConfiguration();
			}
		}

		protected override void OnHandleCreated(EventArgs eventArgs)
		{
			base.OnHandleCreated(eventArgs);

			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime ||
				DesignMode ||
				Site?.DesignMode == true)
			{
				return;
			}

			if (Properties.Settings.Default.PrivacyMode)
			{
				_ = SetWindowDisplayAffinity(Handle, WdaExcludeFromCapture);
			}

			try
			{
				int preference = DwmRound;
				_ = DwmSetWindowAttribute(
					Handle,
					DwmWindowCornerPreference,
					ref preference,
					sizeof(int));
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
		}

		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);

			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime ||
				DesignMode ||
				Site?.DesignMode == true ||
				message.Msg != WmNcHitTest ||
				WindowState == FormWindowState.Maximized)
			{
				return;
			}

			Point cursor = PointToClient(Cursor.Position);
			bool left = cursor.X <= ResizeBorder;
			bool right = cursor.X >= ClientSize.Width - ResizeBorder;
			bool top = cursor.Y <= ResizeBorder;
			bool bottom = cursor.Y >= ClientSize.Height - ResizeBorder;

			if (left && top) message.Result = (IntPtr)HtTopLeft;
			else if (right && top) message.Result = (IntPtr)HtTopRight;
			else if (left && bottom) message.Result = (IntPtr)HtBottomLeft;
			else if (right && bottom) message.Result = (IntPtr)HtBottomRight;
			else if (left) message.Result = (IntPtr)HtLeft;
			else if (right) message.Result = (IntPtr)HtRight;
			else if (top) message.Result = (IntPtr)HtTop;
			else if (bottom) message.Result = (IntPtr)HtBottom;
		}

		protected override bool ProcessCmdKey(ref Message message, Keys keyData)
		{
			if (keyData == (Keys.Control | Keys.F) && dgvConfig.Visible)
			{
				txtSearch.Focus();
				txtSearch.SelectAll();
				return true;
			}

			if (keyData == (Keys.Control | Keys.S) && btnSave.Enabled)
			{
				SaveConfiguration();
				return true;
			}

			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}

			return base.ProcessCmdKey(ref message, keyData);
		}

		protected override void OnFormClosing(FormClosingEventArgs eventArgs)
		{
			if (_templateResetInProgress)
			{
				eventArgs.Cancel = true;
				return;
			}

			dgvConfig.EndEdit();

			if (_isRuntimeInstance && !_allowClose && HasUnsavedChanges())
			{
				DialogResult result = LocalizedMessageBox.Show(
					LocalizationManager.Get("MessageText.663E3E2D227DB51428B7"),
					LocalizationManager.Get("MessageText.CD26D6F5A2FFE405025E"),
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning,
					MessageBoxDefaultButton.Button2);

				if (result != DialogResult.Yes)
				{
					eventArgs.Cancel = true;
				}
				else
				{
					_allowClose = true;
				}
			}

			base.OnFormClosing(eventArgs);
		}

		private void ConfigureFilePresentation()
		{
			string fileName = Path.GetFileName(_path);
			string formatName = ConfigHandler.GetFormatDisplayName(_format);

			Text = LocalizationManager.Get("Configuration.Editor.Title", fileName);
			lblFileName.Text = fileName;
			lblFormatBadge.Text = formatName;
			if (_configurationFiles.Count > 1)
				LocalizationManager.BindText(lblPageSubtitle, "Configuration.Editor.Subtitle.Multiple",
					fileName, _selectedFileIndex + 1, _configurationFiles.Count, formatName);
			else
				LocalizationManager.BindText(lblPageSubtitle, "Configuration.Editor.Subtitle.Single",
					fileName, formatName);
			LocalizationManager.BindText(
				lblFormatState,
				"Configuration.Editor.StructurePreserved",
				formatName);
			if (_fileSelector != null &&
				_fileSelector.SelectedIndex != _selectedFileIndex)
			{
				_fileSelectionIsUpdating = true;
				_fileSelector.SelectedIndex = _selectedFileIndex;
				_fileSelectionIsUpdating = false;
			}
			btnFixConfig.Visible = _server != null &&
				GameFix.CanResetManagedConfiguration(_server);
			btnRestoreBackup.Visible = _server != null;
			btnValidateConfig.Visible = _server != null;
			btnValidateConfig.Enabled = _server != null;
			UpdateFixConfigAvailability();
			UpdateRestoreBackupAvailability();
		}

		private void ConfigureFileSelector()
		{
			if (_configurationFiles.Count <= 1)
				return;

			lblFileName.Visible = false;
			_fileSelector = new ModernSettingsComboBox
			{
				Location = new Point(184, 12),
				Size = new Size(360, 34),
				DropDownWidth = 620,
				MaxDropDownItems = 10,
				AccessibleName = LocalizationManager.Get("Text.F1C216DDF2B88463BCA7"),
				AccessibleDescription = LocalizationManager.Get("Configuration.Editor.FileSelector.Description")
			};
			foreach ((ConfigurationEditorFile file, int index) in
				_configurationFiles.Select((file, index) => (file, index)))
			{
				_fileSelector.Items.Add(new ConfigurationFileChoice(
					index,
					GetConfigurationFileDisplayName(file.Path)));
			}

			_fileSelector.SelectedIndex = _selectedFileIndex;
			_fileSelector.SelectedIndexChanged += FileSelectorSelectedIndexChanged;
			titleBar.Controls.Add(_fileSelector);
			_fileSelector.BringToFront();
			lblFormatBadge.Location = new Point(554, 14);
			lblFormatBadge.BringToFront();
		}

		private void FileSelectorSelectedIndexChanged(object? sender, EventArgs eventArgs)
		{
			if (_fileSelectionIsUpdating ||
				_fileSelector?.SelectedItem is not ConfigurationFileChoice choice ||
				choice.Index == _selectedFileIndex)
			{
				return;
			}

			SwitchConfigurationFile(choice.Index);
		}

		private void SwitchConfigurationFile(int newIndex)
		{
			if (newIndex < 0 || newIndex >= _configurationFiles.Count)
				return;

			dgvConfig.EndEdit();
			if (HasUnsavedChanges())
			{
				DialogResult result = LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get(
						"Configuration.Editor.SwitchFilePrompt",
						Path.GetFileName(_path)),
					LocalizationManager.Get("MessageText.47D6D9A26235C64A173D"),
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Warning,
					MessageBoxDefaultButton.Button1);
				if (result == DialogResult.Cancel ||
					(result == DialogResult.Yes && !TrySaveCurrentConfiguration()))
				{
					RestoreFileSelectorSelection();
					return;
				}
			}

			_selectedFileIndex = newIndex;
			ApplySelectedFile();
			_fileData = [];
			_dataLoaded = false;
			ConfigureFilePresentation();
			LoadConfiguration();
		}

		private void RestoreFileSelectorSelection()
		{
			if (_fileSelector == null)
				return;

			_fileSelectionIsUpdating = true;
			_fileSelector.SelectedIndex = _selectedFileIndex;
			_fileSelectionIsUpdating = false;
		}

		private void ApplySelectedFile()
		{
			ConfigurationEditorFile selected = _configurationFiles[_selectedFileIndex];
			_path = selected.Path;
			_format = selected.Format;
		}

		private string GetConfigurationFileDisplayName(string path)
		{
			if (_server == null || string.IsNullOrWhiteSpace(_server.InstallPath))
				return Path.GetFileName(path);

			try
			{
				string relativePath = Path.GetRelativePath(
					Path.GetFullPath(_server.InstallPath),
					path);
				return relativePath.StartsWith("..", StringComparison.Ordinal)
					? Path.GetFileName(path)
					: relativePath;
			}
			catch (Exception suppressedException)
			{
				ApplicationLogService.WriteSuppressedException(suppressedException);
				return Path.GetFileName(path);
			}
		}

		private static int FindInitialFileIndex(
			IReadOnlyList<ConfigurationEditorFile> configurationFiles)
		{
			for (int index = 0; index < configurationFiles.Count; index++)
			{
				if (File.Exists(configurationFiles[index].Path))
					return index;
			}

			return 0;
		}

		private void LoadConfiguration()
		{
			_dataLoaded = true;

			try
			{
				if (!File.Exists(_path))
				{
					if (CanFixConfiguration())
					{
						ShowConfigurationRepairState(
							LocalizationManager.Get("Configuration.Editor.Repair.Missing"));
						return;
					}

					LocalizedMessageBox.Show(
						LocalizationManager.Get("Configuration.Editor.FileNotFound", _path),
						LocalizationManager.Get("MessageText.BF599881101CA656921C"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
					_allowClose = true;
					Close();
					return;
				}

				_fileData = ConfigHandler.LoadConfig(_path, _format);
				dgvConfig.Enabled = true;
				btnStructured.Enabled = true;
				btnRawPreview.Enabled = true;
				LocalizationManager.BindText(
					lblPreservationTitle,
					"Text.BF239CF13522B7D2F15A");
				LocalizationManager.BindText(
					lblPreservationText,
					"Text.A8796566765C1117B49C");
				PopulateGrid();
				ShowStructuredView();
				UpdateFixConfigAvailability();
			}
			catch (Exception exception)
			{
				if (CanFixConfiguration())
				{
					ShowConfigurationRepairState(
						LocalizationManager.Get(
							"Configuration.Editor.Repair.ReadFailed",
							exception.Message));
					return;
				}

				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"Configuration.Editor.LoadFailed",
						exception.Message),
					LocalizationManager.Get("MessageText.E7516E586A2D8DACDF5E"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				_allowClose = true;
				Close();
			}
		}

		private bool CanFixConfiguration()
		{
			return _server != null && GameFix.CanResetManagedConfiguration(_server);
		}

		private void UpdateFixConfigAvailability()
		{
			btnFixConfig.Enabled = btnFixConfig.Visible &&
				_server != null &&
				GameFix.CanManuallyResetManagedConfiguration(
					_server,
					IsServerBusy(_server));
		}

		private void UpdateRestoreBackupAvailability()
		{
			btnRestoreBackup.Enabled = btnRestoreBackup.Visible &&
				_server != null &&
				!IsServerBusy(_server) &&
				GameFix.HasManagedConfigurationBackup(_server);
		}

		private void ShowConfigurationRepairState(string message)
		{
			_fileData = [];
			PopulateGrid();
			ShowStructuredView();
			dgvConfig.Enabled = false;
			txtSearch.Enabled = false;
			cmbTypeFilter.Enabled = false;
			btnStructured.Enabled = false;
			btnRawPreview.Enabled = false;
			btnSave.Enabled = false;
			btnReset.Enabled = false;
			btnFixConfig.Enabled = _server != null &&
				GameFix.CanManuallyResetManagedConfiguration(
					_server,
					IsServerBusy(_server));
			btnValidateConfig.Enabled = _server != null;
			LocalizationManager.BindText(lblSettingCount, "Text.ADD1C4B4E0694244F0AE");
			LocalizationManager.BindText(lblFormatState, "Text.1142B190BA89EF85AF20");
			LocalizationManager.BindText(lblPreservationTitle, "Text.7ED4EB126CA3A3051286");
			lblPreservationText.Text = message;
		}

		private void PopulateGrid()
		{
			_rowsAreLoading = true;
			dgvConfig.SuspendLayout();

			try
			{
				dgvConfig.Rows.Clear();
				foreach (ConfigLine line in _fileData)
				{
					int rowIndex = dgvConfig.Rows.Add();
					DataGridViewRow row = dgvConfig.Rows[rowIndex];
					row.Tag = line;
					row.Cells[colSetting.Index].Value = GetCleanSettingName(line);
					row.Cells[colSetting.Index].ToolTipText = line.Path;
					row.Cells[colType.Index].Value = GetTypeDisplayName(line.Type);

					if (line.Type == ConfigValueType.Boolean)
					{
						ModernSettingsDataGridViewComboBoxCell booleanCell = CreateBooleanCell();
						booleanCell.Value = NormalizeBooleanDisplay(line.Value);
						row.Cells[colValue.Index] = booleanCell;
					}
					else
					{
						row.Cells[colValue.Index].Value = line.Value;
					}
				}

				ApplyFilters();
				UpdateChangePresentation();
				btnSave.Enabled = _fileData.Count > 0;
			}
			finally
			{
				dgvConfig.ResumeLayout();
				_rowsAreLoading = false;
			}

			UpdateChangePresentation();
		}

		private static ModernSettingsDataGridViewComboBoxCell CreateBooleanCell()
		{
			ModernSettingsDataGridViewComboBoxCell cell = new()
			{
				DropDownWidth = 180
			};
			cell.Items.AddRange("True", "False");
			return cell;
		}

		private static string GetCleanSettingName(ConfigLine line)
		{
			string key = line.Key.Trim();
			if (!string.IsNullOrWhiteSpace(key) &&
				!string.Equals(key, line.Path, StringComparison.Ordinal))
			{
				return key;
			}

			string path = string.IsNullOrWhiteSpace(line.Path)
				? key
				: line.Path.Trim();
			int sectionSeparator = path.LastIndexOf(" / ", StringComparison.Ordinal);
			if (sectionSeparator >= 0)
			{
				path = path[(sectionSeparator + 3)..];
			}

			int leafSeparator = path.LastIndexOf('.');
			return leafSeparator >= 0 && leafSeparator + 1 < path.Length
				? path[(leafSeparator + 1)..]
				: path;
		}

		private static string NormalizeBooleanDisplay(string value)
		{
			return bool.TryParse(value, out bool parsed) && parsed ? "True" : "False";
		}

		private static string GetTypeDisplayName(ConfigValueType type)
		{
			return type switch
			{
				ConfigValueType.Boolean => LocalizationManager.Get("Option.ConfigType.Boolean"),
				ConfigValueType.Number => LocalizationManager.Get("Option.ConfigType.Number"),
				ConfigValueType.Secret => LocalizationManager.Get("Option.ConfigType.Secret"),
				ConfigValueType.Null => LocalizationManager.Get("Option.ConfigType.Null"),
				_ => LocalizationManager.Get("Option.ConfigType.Text")
			};
		}

		private void ApplyFilters()
		{
			if (_rowsAreLoading && dgvConfig.Rows.Count == 0)
			{
				return;
			}

			string searchText = txtSearch.Text.Trim();
			ConfigValueType? selectedType = cmbTypeFilter.SelectedIndex switch
			{
				1 => ConfigValueType.Text,
				2 => ConfigValueType.Number,
				3 => ConfigValueType.Boolean,
				4 => ConfigValueType.Secret,
				5 => ConfigValueType.Null,
				_ => null
			};
			int visibleCount = 0;

			dgvConfig.CurrentCell = null;
			foreach (DataGridViewRow row in dgvConfig.Rows)
			{
				if (row.Tag is not ConfigLine line)
				{
					continue;
				}

				bool searchMatches = string.IsNullOrWhiteSpace(searchText) ||
					line.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
					line.Path.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
					line.Section.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
					GetRowValue(row).Contains(searchText, StringComparison.OrdinalIgnoreCase);
				bool typeMatches = selectedType == null || line.Type == selectedType;

				row.Visible = searchMatches && typeMatches;
				if (row.Visible)
				{
					visibleCount++;
				}
			}

			LocalizationManager.BindText(
				lblSettingCount,
				"Configuration.Editor.SettingCount",
				visibleCount,
				_fileData.Count);
		}

		private List<ConfigLine> CollectUpdatedData()
		{
			dgvConfig.EndEdit();
			List<ConfigLine> updatedData = new(dgvConfig.Rows.Count);

			foreach (DataGridViewRow row in dgvConfig.Rows)
			{
				if (row.Tag is not ConfigLine source)
				{
					continue;
				}

				updatedData.Add(new ConfigLine
				{
					Id = source.Id,
					Key = source.Key,
					Path = source.Path,
					Section = source.Section,
					OriginalValue = source.OriginalValue,
					HasOriginalValue = source.HasOriginalValue,
					Type = source.Type,
					Value = GetRowValue(row)
				});
			}

			return updatedData;
		}

		private bool HasUnsavedChanges()
		{
			if (!_dataLoaded || dgvConfig.Rows.Count == 0)
			{
				return false;
			}

			return dgvConfig.Rows.Cast<DataGridViewRow>().Any(RowHasChanged);
		}

		private static bool ValuesMatch(ConfigLine source, string currentValue)
		{
			if (source.Type == ConfigValueType.Boolean &&
				bool.TryParse(source.OriginalValue, out bool originalBoolean) &&
				bool.TryParse(currentValue, out bool currentBoolean))
			{
				return originalBoolean == currentBoolean;
			}

			return string.Equals(source.OriginalValue, currentValue, StringComparison.Ordinal);
		}

		private static string GetRowValue(DataGridViewRow row)
		{
			return row.Cells[2].Value?.ToString() ?? string.Empty;
		}

		private static bool RowHasChanged(DataGridViewRow row)
		{
			return row.Tag is ConfigLine source && !ValuesMatch(source, GetRowValue(row));
		}

		private void UpdateChangePresentation()
		{
			if (_rowsAreLoading)
			{
				return;
			}

			int changedCount = dgvConfig.Rows
				.Cast<DataGridViewRow>()
				.Count(RowHasChanged);
			LocalizationManager.BindText(
				lblModifiedCount,
				changedCount == 1
					? "DynamicText.841A820BC0CD109C0B37"
					: "Configuration.Editor.UnsavedChanges.Many",
				changedCount);
			lblModifiedCount.ForeColor = changedCount > 0
				? SettingsPalette.Warning
				: SettingsPalette.MutedText;
			btnReset.Enabled = changedCount > 0;
		}

		private void ResetChanges()
		{
			_rowsAreLoading = true;
			try
			{
				foreach (DataGridViewRow row in dgvConfig.Rows)
				{
					if (row.Tag is ConfigLine source)
					{
						row.Cells[colValue.Index].Value = source.Type == ConfigValueType.Boolean
							? NormalizeBooleanDisplay(source.OriginalValue)
							: source.OriginalValue;
					}
				}
			}
			finally
			{
				_rowsAreLoading = false;
			}

			UpdateChangePresentation();
			ApplyFilters();
		}

		private void ShowStructuredView()
		{
			txtRawPreview.Visible = false;
			dgvConfig.Visible = true;
			dgvConfig.BringToFront();
			txtSearch.Enabled = true;
			cmbTypeFilter.Enabled = true;
			btnStructured.ForeColor = SettingsPalette.Accent;
			btnRawPreview.ForeColor = SettingsPalette.SecondaryText;
		}

		private void ShowRawPreview()
		{
			try
			{
				txtRawPreview.Text = ConfigHandler.CreatePreview(
					_path,
					CollectUpdatedData(),
					_format);
				dgvConfig.Visible = false;
				txtRawPreview.Visible = true;
				txtRawPreview.BringToFront();
				txtSearch.Enabled = false;
				cmbTypeFilter.Enabled = false;
				btnStructured.ForeColor = SettingsPalette.SecondaryText;
				btnRawPreview.ForeColor = SettingsPalette.Accent;
			}
			catch (Exception exception)
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get("Configuration.Editor.PreviewFailed", exception.Message),
					LocalizationManager.Get("MessageText.B3084806B31E356F5612"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		private void SaveConfiguration()
		{
			if (!TrySaveCurrentConfiguration())
				return;

			_allowClose = true;
			DialogResult = DialogResult.OK;
			Close();
		}

		private bool TrySaveCurrentConfiguration()
		{
			try
			{
				if (_server != null && HasUnsavedChanges())
					_ = GameFix.BackupManagedConfiguration(
						_server,
						LocalizationManager.Get(
							"Configuration.Editor.BackupReason"));
				ConfigHandler.SaveConfig(_path, CollectUpdatedData(), _format);
				return true;
			}
			catch (Exception exception)
			{
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("Configuration.Editor.SaveFailed", exception.Message),
					LocalizationManager.Get("MessageText.9169BD1E6835B2752A61"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return false;
			}
		}

		private async Task ResetConfigurationFromTemplate()
		{
			if (_server == null || !CanFixConfiguration())
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get("MessageText.1D7E8CD37BFCD097E6DA"),
					LocalizationManager.Get("MessageText.08E9E5F519C0E3E112DC"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}

			if (IsServerBusy(_server))
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get("MessageText.37D35536B29E5D04935F"),
					LocalizationManager.Get("MessageText.88F9321A50E97A2552C0"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			bool fileExists = File.Exists(_path);
			if (!GameServerInputValidator.TryValidateWorldSeed(
				GameDatabase.GetGame(_server.Game), _server.WorldSeed, out string worldSeedError))
			{
				LocalizedMessageBox.Show(this, worldSeedError,
					LocalizationManager.Get("ServerSetup.Dialog.SettingsAttention.Title"),
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			string developmentModeText = GameFix.ManagedConfigurationsEnabled
				? string.Empty
				: LocalizationManager.Get("Configuration.Editor.Reset.DevelopmentMode");
			string backupText = fileExists
				? LocalizationManager.Get("Configuration.Editor.Reset.BackupNotice")
				: string.Empty;
			DialogResult confirmation = LocalizedMessageBox.Show(
				LocalizationManager.Get("MessageText.AC3B07BB575D129E2A12") +
				developmentModeText +
				backupText +
				LocalizationManager.Get("MessageText.B2F06E7B3A4E957880A3"),
				LocalizationManager.Get("MessageText.8A7D73F831BD8606CF92"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning,
				MessageBoxDefaultButton.Button2);
			if (confirmation != DialogResult.Yes)
			{
				return;
			}

			_templateResetInProgress = true;
			UseWaitCursor = true;
			btnFixConfig.Enabled = false;
			btnReset.Enabled = false;
			btnCancel.Enabled = false;
			btnSave.Enabled = false;
			if (_fileSelector != null)
				_fileSelector.Enabled = false;

			try
			{
				ConfigurationApplyResult result =
					await GameFix.ResetManagedConfiguration(_server);
				if (!result.Succeeded || !result.Complete)
				{
					LocalizedMessageBox.Show(
						LocalizationManager.TranslateRuntimeText(result.Message),
						LocalizationManager.Get("MessageText.071AC0FA89C3FD9DD0F3"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
					return;
				}

				_ = FileHandler.SaveServers();
				LoadConfiguration();
				LocalizedMessageBox.Show(
					LocalizationManager.TranslateRuntimeText(result.Message) +
					(fileExists
						? LocalizationManager.Get("MessageText.ECD30B96D30BE40030DF")
						: string.Empty),
					LocalizationManager.Get("MessageText.4C6913DA26CA421FD296"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
			}
			catch (Exception exception)
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get("Configuration.Editor.ResetFailed", exception.Message),
					LocalizationManager.Get("MessageText.071AC0FA89C3FD9DD0F3"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
			finally
			{
				_templateResetInProgress = false;
				UseWaitCursor = false;
				btnCancel.Enabled = true;
				if (_fileSelector != null)
					_fileSelector.Enabled = true;
				UpdateFixConfigAvailability();
				UpdateRestoreBackupAvailability();
				if (File.Exists(_path) && _fileData.Count > 0)
				{
					btnSave.Enabled = true;
					UpdateChangePresentation();
				}
			}
		}

		private async Task ValidateSynixConfiguration()
		{
			if (_server == null)
				return;

			if (HasUnsavedChanges())
			{
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("MessageText.A36A8AE6C94D6549F743"),
					LocalizationManager.Get("MessageText.F507179BD454425EEE38"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}

			UseWaitCursor = true;
			btnValidateConfig.Enabled = false;
			try
			{
				ConfigurationValidationReport report =
					await GameFix.ValidateManagedConfiguration(_server);
				using ConfigurationValidationDialog dialog = new(report);
				dialog.ShowDialog(this);
			}
			catch (Exception exception)
			{
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("Configuration.Editor.ValidationFailed", exception.Message),
					LocalizationManager.Get("MessageText.68F42FF97AF1C59CF5EA"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
			finally
			{
				UseWaitCursor = false;
				btnValidateConfig.Enabled = _server != null;
			}
		}

		private void RestorePreviousConfiguration()
		{
			if (_server == null || IsServerBusy(_server))
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get("MessageText.96B14861829C994EDBFF"),
					LocalizationManager.Get("MessageText.88F9321A50E97A2552C0"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			DialogResult confirmation = LocalizedMessageBox.Show(
				LocalizationManager.Get("MessageText.D955BE724373ACFC3AD2"),
				LocalizationManager.Get("MessageText.9A66A7E6057B206C0F72"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning,
				MessageBoxDefaultButton.Button2);
			if (confirmation != DialogResult.Yes)
				return;

			ConfigurationRestoreResult result =
				GameFix.RestorePreviousManagedConfiguration(_server);
			if (!result.Succeeded)
			{
				LocalizedMessageBox.Show(
					LocalizationManager.TranslateRuntimeText(result.Message),
					LocalizationManager.Get("MessageText.8C800FC47C25572C6AD2"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}

			LoadConfiguration();
			UpdateRestoreBackupAvailability();
			LocalizedMessageBox.Show(
				LocalizationManager.TranslateRuntimeText(result.Message),
				LocalizationManager.Get("MessageText.5B867C348CF06A5F841C"),
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}

		private static bool IsServerBusy(GameServer server)
		{
			string status = server.Status ?? string.Empty;
			return (server.PID.HasValue && server.PID.Value > 0) ||
				(server.SteamPID.HasValue && server.SteamPID.Value > 0) ||
				status == Core.StatusManager.GetStatus(Core.ServerState.Running) ||
				status == Core.StatusManager.GetStatus(Core.ServerState.Starting) ||
				status == Core.StatusManager.GetStatus(Core.ServerState.Stopping) ||
				status == Core.StatusManager.GetStatus(Core.ServerState.Installing) ||
				status == Core.StatusManager.GetStatus(Core.ServerState.Updating) ||
				status == Core.StatusManager.GetStatus(Core.ServerState.Validating) ||
				status == Core.StatusManager.GetStatus(Core.ServerState.Deleting);
		}

		private void EnableGridDoubleBuffering()
		{
			PropertyInfo? doubleBufferedProperty = typeof(DataGridView).GetProperty(
				"DoubleBuffered",
				BindingFlags.NonPublic | BindingFlags.Instance);
			doubleBufferedProperty?.SetValue(dgvConfig, true, null);
		}

		private static Color GetTypeColor(ConfigValueType type)
		{
			return type switch
			{
				ConfigValueType.Boolean => BooleanTypeColor,
				ConfigValueType.Number => NumberTypeColor,
				ConfigValueType.Secret => SecretTypeColor,
				ConfigValueType.Null => NullTypeColor,
				_ => TextTypeColor
			};
		}

		private static Color GetTypeBadgeColor(ConfigValueType type)
		{
			if (!ThemeManager.IsDarkMode)
			{
				return type switch
				{
					ConfigValueType.Boolean => Color.FromArgb(215, 240, 237),
					ConfigValueType.Number => Color.FromArgb(236, 228, 251),
					ConfigValueType.Secret => Color.FromArgb(248, 236, 208),
					ConfigValueType.Null => Color.FromArgb(227, 232, 239),
					_ => Color.FromArgb(221, 234, 248)
				};
			}

			return type switch
			{
				ConfigValueType.Boolean => Color.FromArgb(15, 61, 66),
				ConfigValueType.Number => Color.FromArgb(48, 39, 77),
				ConfigValueType.Secret => Color.FromArgb(68, 52, 24),
				ConfigValueType.Null => Color.FromArgb(40, 48, 61),
				_ => Color.FromArgb(24, 48, 72)
			};
		}

		private sealed record ConfigurationFileChoice(int Index, string DisplayName)
		{
			public override string ToString() => DisplayName;
		}

		private void TitleBar_MouseDown(object? sender, MouseEventArgs eventArgs)
		{
			if (eventArgs.Button != MouseButtons.Left)
			{
				return;
			}

			_ = ReleaseCapture();
			_ = SendMessage(Handle, WmNcLeftButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
		}

		private void btnMinimize_Click(object? sender, EventArgs eventArgs)
		{
			WindowState = FormWindowState.Minimized;
		}

		private void btnClose_Click(object? sender, EventArgs eventArgs)
		{
			Close();
		}

		private void txtSearch_TextChanged(object? sender, EventArgs eventArgs)
		{
			ApplyFilters();
		}

		private void cmbTypeFilter_SelectedIndexChanged(object? sender, EventArgs eventArgs)
		{
			ApplyFilters();
		}

		private void btnStructured_Click(object? sender, EventArgs eventArgs)
		{
			ShowStructuredView();
		}

		private void btnRawPreview_Click(object? sender, EventArgs eventArgs)
		{
			ShowRawPreview();
		}

		private void dgvConfig_CurrentCellDirtyStateChanged(object? sender, EventArgs eventArgs)
		{
			if (dgvConfig.IsCurrentCellDirty &&
				dgvConfig.CurrentCell is DataGridViewComboBoxCell)
			{
				dgvConfig.CommitEdit(DataGridViewDataErrorContexts.Commit);
			}
		}

		private void dgvConfig_CellMouseDown(
			object? sender,
			DataGridViewCellMouseEventArgs eventArgs)
		{
			if (eventArgs.RowIndex >= 0 &&
				IsInformationalColumn(eventArgs.ColumnIndex))
			{
				dgvConfig.ClearSelection();
				dgvConfig.InvalidateCell(eventArgs.ColumnIndex, eventArgs.RowIndex);
				return;
			}

			if (eventArgs.Button != MouseButtons.Left ||
				!IsBooleanValueCell(eventArgs.ColumnIndex, eventArgs.RowIndex))
			{
				return;
			}

			_openBooleanDropDownOnEdit = true;
			_booleanDropDownRowIndex = eventArgs.RowIndex;

			DataGridViewCell targetCell =
				dgvConfig.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex];
			if (!ReferenceEquals(dgvConfig.CurrentCell, targetCell))
			{
				dgvConfig.CurrentCell = targetCell;
			}

			dgvConfig.Focus();
			if (!dgvConfig.IsCurrentCellInEditMode)
			{
				_ = dgvConfig.BeginEdit(true);
			}

			QueueBooleanDropDownOpen();
		}

		private void dgvConfig_CellEnter(
			object? sender,
			DataGridViewCellEventArgs eventArgs)
		{
			if (eventArgs.RowIndex < 0 ||
				!IsInformationalColumn(eventArgs.ColumnIndex))
			{
				return;
			}

			dgvConfig.ClearSelection();
			dgvConfig.InvalidateCell(eventArgs.ColumnIndex, eventArgs.RowIndex);
		}

		private void dgvConfig_Scroll(object? sender, ScrollEventArgs eventArgs)
		{

			_openBooleanDropDownOnEdit = false;
			_booleanDropDownRowIndex = -1;

			if (dgvConfig.EditingControl is not ComboBox comboBox)
			{
				return;
			}

			comboBox.DroppedDown = false;
			if (dgvConfig.IsCurrentCellDirty)
			{
				dgvConfig.CommitEdit(DataGridViewDataErrorContexts.Commit);
			}

			if (dgvConfig.IsCurrentCellInEditMode && !dgvConfig.EndEdit())
			{
				dgvConfig.CancelEdit();
			}

			dgvConfig.Invalidate();
		}

		private void dgvConfig_EditingControlShowing(
			object? sender,
			DataGridViewEditingControlShowingEventArgs eventArgs)
		{
			if (!IsCurrentBooleanValueCell() ||
				eventArgs.Control is not ComboBox comboBox)
			{
				return;
			}

			ApplyBooleanEditingStyle(comboBox, eventArgs.CellStyle);
			if (_openBooleanDropDownOnEdit)
			{
				QueueBooleanDropDownOpen();
			}
		}

		private bool IsCurrentBooleanValueCell()
		{
			DataGridViewCell? currentCell = dgvConfig.CurrentCell;
			return currentCell != null &&
				IsBooleanValueCell(currentCell.ColumnIndex, currentCell.RowIndex);
		}

		private bool IsInformationalColumn(int columnIndex)
		{
			return columnIndex == colSetting.Index ||
				columnIndex == colType.Index;
		}

		private bool IsBooleanValueCell(int columnIndex, int rowIndex)
		{
			return rowIndex >= 0 &&
				rowIndex < dgvConfig.Rows.Count &&
				columnIndex == colValue.Index &&
				dgvConfig.Rows[rowIndex].Tag is ConfigLine line &&
				line.Type == ConfigValueType.Boolean &&
				!dgvConfig.Rows[rowIndex].Cells[columnIndex].ReadOnly;
		}

		private void ApplyBooleanEditingStyle(
			ComboBox comboBox,
			DataGridViewCellStyle cellStyle)
		{
			cellStyle.BackColor = SettingsPalette.Input;
			cellStyle.ForeColor = SettingsPalette.PrimaryText;
			cellStyle.SelectionBackColor = SettingsPalette.Input;
			cellStyle.SelectionForeColor = SettingsPalette.PrimaryText;
			cellStyle.Padding = Padding.Empty;

			comboBox.BackColor = SettingsPalette.Input;
			comboBox.ForeColor = SettingsPalette.PrimaryText;
			comboBox.FlatStyle = FlatStyle.Flat;
			comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			comboBox.Dock = DockStyle.None;
			comboBox.Margin = Padding.Empty;
			comboBox.Padding = Padding.Empty;
			comboBox.Cursor = Cursors.Hand;

			Panel editingPanel = dgvConfig.EditingPanel;
			editingPanel.BackColor = SettingsPalette.Input;
			editingPanel.BorderStyle = BorderStyle.None;
			editingPanel.Margin = Padding.Empty;
			editingPanel.Padding = Padding.Empty;
			if (comboBox.Parent is Panel parentPanel &&
				!ReferenceEquals(parentPanel, editingPanel))
			{
				parentPanel.BackColor = SettingsPalette.Input;
				parentPanel.BorderStyle = BorderStyle.None;
				parentPanel.Margin = Padding.Empty;
				parentPanel.Padding = Padding.Empty;
			}

			if (comboBox is ModernSettingsComboBox modernComboBox)
			{
				modernComboBox.BorderColor = SettingsPalette.Border;
				modernComboBox.FocusBorderColor = SettingsPalette.Border;
				modernComboBox.ArrowColor = SettingsPalette.SecondaryText;
				modernComboBox.SelectedItemBackColor = SettingsPalette.Selection;
				modernComboBox.Invalidate();
			}

			if (comboBox is ModernSettingsDataGridViewComboBoxEditingControl gridEditor)
			{
				gridEditor.PrepareForFullCellHeight(
					dgvConfig.CurrentCell?.Size.Height ?? dgvConfig.RowTemplate.Height);
			}
		}

		private void QueueBooleanDropDownOpen()
		{
			if (_booleanDropDownOpenQueued ||
				!_openBooleanDropDownOnEdit ||
				!dgvConfig.IsHandleCreated)
			{
				return;
			}

			_booleanDropDownOpenQueued = true;
			dgvConfig.BeginInvoke(new Action(OpenPendingBooleanDropDown));
		}

		private void OpenPendingBooleanDropDown()
		{
			_booleanDropDownOpenQueued = false;
			int targetRowIndex = _booleanDropDownRowIndex;
			_openBooleanDropDownOnEdit = false;
			_booleanDropDownRowIndex = -1;

			if (IsDisposed || Disposing)
			{
				return;
			}

			DataGridViewCell? currentCell = dgvConfig.CurrentCell;
			if (targetRowIndex < 0 ||
				currentCell == null ||
				currentCell.RowIndex != targetRowIndex ||
				!IsCurrentBooleanValueCell())
			{
				return;
			}

			if (!dgvConfig.IsCurrentCellInEditMode && !dgvConfig.BeginEdit(true))
			{
				return;
			}

			if (dgvConfig.EditingControl is ComboBox comboBox)
			{
				ApplyBooleanEditingStyle(comboBox, currentCell.InheritedStyle);
				comboBox.Focus();
				comboBox.DroppedDown = true;
			}
		}

		private void dgvConfig_CellValueChanged(
			object? sender,
			DataGridViewCellEventArgs eventArgs)
		{
			if (!_rowsAreLoading && eventArgs.RowIndex >= 0)
			{
				UpdateChangePresentation();
			}
		}

		private void dgvConfig_CellPainting(
			object? sender,
			DataGridViewCellPaintingEventArgs eventArgs)
		{
			if (eventArgs.RowIndex < 0)
			{
				return;
			}

			if (eventArgs.ColumnIndex == colSetting.Index)
			{
				eventArgs.PaintBackground(eventArgs.ClipBounds, false);
				eventArgs.PaintContent(eventArgs.ClipBounds);
				eventArgs.Handled = true;
				return;
			}

			if (eventArgs.ColumnIndex != colType.Index ||
				dgvConfig.Rows[eventArgs.RowIndex].Tag is not ConfigLine line)
			{
				return;
			}

			eventArgs.PaintBackground(eventArgs.ClipBounds, false);
			Color typeColor = GetTypeColor(line.Type);
			Rectangle badgeBounds = new(
				eventArgs.CellBounds.X + 12,
				eventArgs.CellBounds.Y + 13,
				Math.Min(92, eventArgs.CellBounds.Width - 24),
				26);
			using SolidBrush badgeBrush = new(GetTypeBadgeColor(line.Type));
			if (eventArgs.Graphics == null)
				return;
			eventArgs.Graphics.FillRectangle(badgeBrush, badgeBounds);
			TextRenderer.DrawText(
				eventArgs.Graphics,
				GetTypeDisplayName(line.Type),
				colType.DefaultCellStyle.Font ?? dgvConfig.Font,
				badgeBounds,
				typeColor,
				TextFormatFlags.HorizontalCenter |
				TextFormatFlags.VerticalCenter |
				TextFormatFlags.EndEllipsis);
			eventArgs.Handled = true;
		}

		private void dgvConfig_DataError(
			object? sender,
			DataGridViewDataErrorEventArgs eventArgs)
		{
			eventArgs.ThrowException = false;
		}

		private void btnReset_Click(object? sender, EventArgs eventArgs)
		{
			ResetChanges();
		}

		private async void btnFixConfig_Click(object? sender, EventArgs eventArgs)
		{
			await ResetConfigurationFromTemplate();
		}

		private async void btnValidateConfig_Click(object? sender, EventArgs eventArgs)
		{
			await ValidateSynixConfiguration();
		}

		private void btnRestoreBackup_Click(object? sender, EventArgs eventArgs)
		{
			RestorePreviousConfiguration();
		}

		private void btnCancel_Click(object? sender, EventArgs eventArgs)
		{
			Close();
		}

		private void btnSave_Click(object? sender, EventArgs eventArgs)
		{
			SaveConfiguration();
		}

		[DllImport("user32.dll")]
		private static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(
			IntPtr windowHandle,
			uint message,
			IntPtr wordParameter,
			IntPtr longParameter);

		[DllImport("user32.dll")]
		private static extern bool SetWindowDisplayAffinity(
			IntPtr windowHandle,
			uint affinity);

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(
			IntPtr windowHandle,
			int attribute,
			ref int attributeValue,
			int attributeSize);
	}
}
