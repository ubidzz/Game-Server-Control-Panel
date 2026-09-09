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
using Synix_Control_Panel.SynixApp.MonitoringHandler;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixApp.SteamCMDHandler;
using Synix_Control_Panel.SynixEngine;
using Synix_Control_Panel.SynixEngine.ModManagement;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Synix_Control_Panel.SynixEngine.Core;
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Localization;

namespace Synix_Control_Panel.SynixApp.UI.Dashboard
{
	public partial class MainGUI : Form
	{
		private static BindingList<GameServer> serverList { get; } = ServerRegistry.Servers;
		private readonly BindingList<GameServer> _visibleServers = [];
		private static bool isInitializing = false;
		private int chartTickCounter = 0;
		private const int maxGraphPoints = 60;
		private static Font boldFont = new Font("Segoe UI", 9, FontStyle.Bold);
		private static Font regularFont = new Font("Segoe UI", 9, FontStyle.Regular);
		private bool isPrivacyLoading = false;
		private System.Windows.Forms.Timer? versionTimer;
		private System.Windows.Forms.Timer? _busyStatusTimer;
		private int _busyStatusFrame;
		private readonly SemaphoreSlim _versionCheckGate = new(1, 1);
		private SynixUpdateCheckResult? _updateCheckResult;
		private bool _updateShutdownRequested;
		private ToolStripMenuItem? _modPluginManagerMenuItem;
		private ToolStripMenuItem? _playerManagementMenuItem;
		private ToolStripMenuItem? _minecraftConsoleMenuItem;
		private ToolStripMenuItem? _satisfactoryControlMenuItem;
		private ToolStripMenuItem? _liveProcessDetailsMenuItem;
		private ToolStripMenuItem? _connectionInformationMenuItem;
		private string? _localIpAddress;
		private string? _publicIpAddress;
		public const int WM_NCLBUTTONDOWN = 0xA1;
		public const int HT_CAPTION = 0x2;

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImport("user32.dll")]
		public static extern bool ReleaseCapture();

		public MainGUI()
		{
			InitializeComponent();
			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
				return;
			ApplicationUiService.LogRequested += ApplicationLogRequested;
			ApplicationUiService.GridRefreshRequested += ApplicationGridRefreshRequested;
			ApplicationUiService.RegisterMainWindow(this, UpdatePrivacyMode);
			Disposed += MainGUI_Disposed;
			ThemeManager.Apply(this);

			FileHandler.LoadServers();
			AddGuidanceMenuItems();
			PopulateStatusFilters();
			LocalizationManager.LanguageChanged += InterfaceLanguageChanged;

			SynixMenuStyler.Apply(contextMenuStrip);

			dataGridView1.AutoGenerateColumns = false;
			dataGridView1.DataSource = _visibleServers;
			if (!dataGridView1.Columns.Contains("IconCol"))
			{
				DataGridViewImageColumn iconCol = new DataGridViewImageColumn();
				iconCol.Name = "IconCol";
				iconCol.HeaderText = "";
				iconCol.DataPropertyName = "DisplayIcon";
				iconCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
				iconCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
				iconCol.Width = 46;
				iconCol.MinimumWidth = 46;
				iconCol.DefaultCellStyle.Padding = new Padding(8);

				dataGridView1.Columns.Insert(0, iconCol);
				dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
				dataGridView1.RowTemplate.Height = 44;
				foreach (DataGridViewRow row in dataGridView1.Rows)
				{
					row.Height = 44;
				}
			}

			GridStyler.DarkTheme(dataGridView1);
			GridStyler.ApplyRoundedCorners(dataGridView1, 10);
			typeof(DataGridView).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty, null, dataGridView1, new object[] { true });
			GridStyler.ApplyDashboardTheme(dataGridView1);
			GridStyler.EnableServerDetailsInteraction(dataGridView1);
			GridStyler.StyleCloseButton(btnClose);
			GridStyler.StyleMinimizeButton(btnMinimize);
			GridStyler.StyleIconButton(btnDiscord, Properties.Resources.discord_icon, Color.FromArgb(200, 200, 200));
			GridStyler.StyleIconButton(btnGithub, Properties.Resources.github_icon, Color.FromArgb(200, 200, 200));
			GridStyler.StyleIconButton(btnSettings, Properties.Resources.gear_icon, Color.FromArgb(200, 200, 200));
			GridStyler.StyleIconButton(btnHelp, Properties.Resources.help, Color.FromArgb(200, 200, 200));
			InitializeBusyStatusAnimation();
			ApplyServerFilter();

			IntPtr roundedRegionHandle = CreateRoundRectRgn(0, 0, Width, Height, 15, 15);
			if (roundedRegionHandle != IntPtr.Zero)
			{
				Region = System.Drawing.Region.FromHrgn(roundedRegionHandle);
				DeleteObject(roundedRegionHandle);
			}
			UpdateDashboardSummary();
			UpdateSelectedServerCard();
			_ = Core.Instance;
			_ = VersionCheck();
			InitializeVersionCheckTimer();

		}

		private void MainGUI_Disposed(object? sender, EventArgs eventArgs)
		{
			LocalizationManager.LanguageChanged -= InterfaceLanguageChanged;
			ApplicationUiService.LogRequested -= ApplicationLogRequested;
			ApplicationUiService.GridRefreshRequested -= ApplicationGridRefreshRequested;
			ApplicationUiService.UnregisterMainWindow(this);
		}

		private void ApplicationLogRequested(
			object? sender,
			ApplicationLogEventArgs eventArgs)
		{
			if (eventArgs.LocalizedMessage == null)
			{
				AppendLog(
					eventArgs.TechnicalMessage,
					eventArgs.Color,
					eventArgs.Bold);
			}
			else
			{
				AppendLogCore(
					eventArgs.TechnicalMessage,
					eventArgs.LocalizedMessage,
					eventArgs.Color,
					eventArgs.Bold);
			}
		}

		private void ApplicationGridRefreshRequested(object? sender, EventArgs eventArgs)
		{
			UpdateGrid();
		}

		private void AddGuidanceMenuItems()
		{
			_satisfactoryControlMenuItem = new ToolStripMenuItem(LocalizationManager.Get("Satisfactory.Title"));
			_satisfactoryControlMenuItem.Click += (_, _) =>
			{
				GameServer? server = GetSelectedServer();
				if (server == null || !GameDatabase.IsSatisfactory(server.Game)) return;
				using SatisfactoryControlDialog dialog = new(server);
				dialog.ShowDialog(this);
			};
			contextMenuStrip.Items.Add(_satisfactoryControlMenuItem);
			_modPluginManagerMenuItem = new ToolStripMenuItem(
				LocalizationManager.Get("Menu.ModPluginManager"))
			{
				Visible = false,
				Enabled = false
			};
			_modPluginManagerMenuItem.Click += (_, _) =>
			{
				GameServer? server = GetSelectedServer();
				if (server == null || !ModSystemCatalog.CanManageAddOns(server))
					return;
				using ModPluginManager dialog = new(server);
				dialog.ShowDialog(this);
			};

			_playerManagementMenuItem = new ToolStripMenuItem(
				LocalizationManager.Get("Menu.PlayerManagementCenter"));
			_playerManagementMenuItem.Click += (_, _) =>
			{
				GameServer? server = GetSelectedServer();
				if (server == null ||
					!CanShowLiveServerActions(server))
					return;
				if (!GameDatabase.SupportsPlayerManagement(server))
				{
					return;
				}
				using PlayerManagementCenter dialog = new(server);
				dialog.ShowDialog(this);
			};

			_minecraftConsoleMenuItem = new ToolStripMenuItem(
				LocalizationManager.Get("Menu.MinecraftServerConsole"));
			_minecraftConsoleMenuItem.Click += (_, _) =>
			{
				GameServer? server = GetSelectedServer();
				if (server == null || !GameDatabase.IsMinecraft(server.Game))
					return;
				using MinecraftConsoleDialog dialog = new(server);
				dialog.ShowDialog(this);
			};

			_connectionInformationMenuItem = new ToolStripMenuItem(
				LocalizationManager.Get("Menu.ConnectionInformation"));
			_connectionInformationMenuItem.Click += (_, _) =>
			{
				GameServer? server = GetSelectedServer();
				if (server == null)
					return;
				using ConnectionInformationDialog dialog = new(server);
				dialog.ShowDialog(this);
			};

			_liveProcessDetailsMenuItem = new ToolStripMenuItem(
				LocalizationManager.Get("Menu.LiveProcessDetails"));
			_liveProcessDetailsMenuItem.Click += (_, _) =>
			{
				GameServer? server = GetSelectedServer();
				if (server == null ||
					!CanShowLiveServerActions(server))
					return;
				ResourceMonitorGUI monitor = new(server);
				monitor.Show(this);
			};

			int insertAt = contextMenuStrip.Items.IndexOf(toolStripSeparator3);
			if (insertAt < 0)
				insertAt = contextMenuStrip.Items.Count;
			contextMenuStrip.Items.Insert(insertAt++, _modPluginManagerMenuItem);
			contextMenuStrip.Items.Insert(insertAt++, _playerManagementMenuItem);
			contextMenuStrip.Items.Insert(insertAt++, _minecraftConsoleMenuItem);
			contextMenuStrip.Items.Insert(insertAt++, _liveProcessDetailsMenuItem);
			contextMenuStrip.Items.Insert(insertAt, _connectionInformationMenuItem);
		}

		private void InterfaceLanguageChanged(
			object? sender,
			EventArgs eventArgs)
		{
			PopulateStatusFilters();
			if (_satisfactoryControlMenuItem != null)
				_satisfactoryControlMenuItem.Text = LocalizationManager.Get("Satisfactory.Title");
			if (_modPluginManagerMenuItem != null)
				_modPluginManagerMenuItem.Text =
					LocalizationManager.Get("Menu.ModPluginManager");
			if (_playerManagementMenuItem != null)
				_playerManagementMenuItem.Text =
					LocalizationManager.Get("Menu.PlayerManagementCenter");
			if (_minecraftConsoleMenuItem != null)
				_minecraftConsoleMenuItem.Text =
					LocalizationManager.Get("Menu.MinecraftServerConsole");
			if (_liveProcessDetailsMenuItem != null)
				_liveProcessDetailsMenuItem.Text =
					LocalizationManager.Get("Menu.LiveProcessDetails");
			if (_connectionInformationMenuItem != null)
				_connectionInformationMenuItem.Text =
					LocalizationManager.Get("Menu.ConnectionInformation");
			dataGridView1.Refresh();
			UpdateDashboardSummary();
			UpdateSelectedServerCard();
			UpdateNetworkLabels();
		}

		private void PopulateStatusFilters()
		{
			string selectedValue =
				(cmbStatusFilter.SelectedItem as LocalizedOption)?.Value
				?? "all";
			cmbStatusFilter.Items.Clear();
			cmbStatusFilter.Items.AddRange(
			[
				new LocalizedOption("all", "Option.Status.All"),
				new LocalizedOption("running", "Option.Status.Running"),
				new LocalizedOption("stopped", "Option.Status.Stopped"),
				new LocalizedOption("progress", "Option.Status.InProgress"),
				new LocalizedOption("attention", "Option.Status.NeedsAttention")
			]);

			int selectedIndex = cmbStatusFilter.Items
				.Cast<LocalizedOption>()
				.Select((option, index) => (option, index))
				.FirstOrDefault(item => string.Equals(
					item.option.Value,
					selectedValue,
					StringComparison.Ordinal))
				.index;
			cmbStatusFilter.SelectedIndex = selectedIndex;
		}

		internal static bool CanShowLiveServerActions(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			return string.Equals(
				server.Status,
				StatusManager.GetStatus(ServerState.Running),
				StringComparison.OrdinalIgnoreCase);
		}

		private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
			e.ThrowException = false;
		}

		private void tmrResourceUpdates_Tick(object sender, EventArgs e)
		{
			double cpu = Core.Instance.TotalCpuUsage;
			double ram = Core.Instance.TotalRamUsageGb;

			cpuGauge.UpdateGauge(
				(float)cpu,
				LocalizationManager.Get("Dashboard.CpuGaugeLabel"));
			ramGauge.MaxValue = (float)GetUsableSystemRamGb();
			ramGauge.UpdateGauge(
				(float)ram,
				LocalizationManager.Get("Dashboard.RamGaugeLabel"));
			lblCpuValue.Text = LocalizationManager.Get(
				"Dashboard.CpuValue",
				cpu);
			lblRamValue.Text = LocalizationManager.Get(
				"Dashboard.RamValue",
				ram);
			UpdateDashboardSummary();

			chartTickCounter++;
		}

		private static double GetUsableSystemRamGb()
		{
			return Core.TotalRamGb > 0 ? Core.TotalRamGb : 128.0;
		}

		[DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
		private static extern IntPtr CreateRoundRectRgn
		(
			int nLeftRect,
			int nTopRect,
			int nRightRect,
			int nBottomRect,
			int nWidthEllipse,
			int nHeightEllipse
		);

		[DllImport("Gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject(IntPtr hObject);

		private void Form_Drag_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
		}

		private void InitializeBusyStatusAnimation()
		{
			_busyStatusTimer = new System.Windows.Forms.Timer(components)
			{
				Interval = 160
			};
			_busyStatusTimer.Tick += BusyStatusTimer_Tick;
			_busyStatusTimer.Start();
			dataGridView1.CellPainting += dataGridView1_CellPainting;
		}

		private void BusyStatusTimer_Tick(object? sender, EventArgs eventArgs)
		{
			if (!serverList.Any(server =>
				BusyStatusPresentation.TryGetBusyState(server.Status, out _)))
			{
				return;
			}

			_busyStatusFrame =
				(_busyStatusFrame + 1) % BusyStatusPresentation.FrameCount;
			if (colStatus.Index >= 0)
			{
				dataGridView1.InvalidateColumn(colStatus.Index);
			}
		}

		private void StreamerModeCheck()
		{
			if (isPrivacyLoading)
			{
				AppendLocalizedLog("Dashboard.Activity.StreamerMode", Color.Red);
				return;
			}
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (_updateShutdownRequested)
				return;

			bool deletionActive = serverList.Any(server =>
				(server.Status ?? string.Empty).StartsWith(
					StatusManager.GetStatus(ServerState.Deleting),
					StringComparison.OrdinalIgnoreCase));
			if (Core.Instance.isDownloadActive || deletionActive)
			{
				e.Cancel = true;
				LocalizedMessageBox.Show(
					LocalizationManager.Get("Dashboard.CloseBlocked.Body"),
					LocalizationManager.Get("Dashboard.CloseBlocked.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			BackgroundServiceManager.PrepareForDashboardExit();
		}

		private async Task LoadNetworkInfo()
		{
			if (isPrivacyLoading)
			{
				UpdateNetworkLabels();
				return;
			}

			_localIpAddress = null;
			_publicIpAddress = null;
			UpdateNetworkLabels();
			_localIpAddress = await Core.Instance.GetLocalIP();
			UpdateNetworkLabels();
			_publicIpAddress = await Core.Instance.GetPublicIP();
			UpdateNetworkLabels();
		}

		private void UpdateNetworkLabels()
		{
			if (isPrivacyLoading)
			{
				lblPublicIP.Text = LocalizationManager.Get(
					"Dashboard.Network.PublicHidden");
				lblLocalIP1.Text = LocalizationManager.Get(
					"Dashboard.Network.LocalHidden");
				return;
			}

			lblLocalIP1.Text = string.IsNullOrWhiteSpace(_localIpAddress)
				? LocalizationManager.Get("Dashboard.Network.LocalFetching")
				: LocalizationManager.Get(
					"Dashboard.Network.LocalAddress",
					_localIpAddress);
			lblPublicIP.Text = string.IsNullOrWhiteSpace(_publicIpAddress)
				? LocalizationManager.Get("Dashboard.Network.PublicFetching")
				: LocalizationManager.Get(
					"Dashboard.Network.PublicAddress",
					_publicIpAddress);
		}

		private async void lblPublicIP_Click(object sender, EventArgs e)
		{
			string publicIP = await Core.Instance.GetPublicIP();
			Clipboard.SetText(publicIP);
			if (!isPrivacyLoading)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.PublicIpCopied",
					Color.Cyan,
					arguments: publicIP);
			}
			else
			{
				AppendLocalizedLog(
					"Dashboard.Activity.PublicIpCopiedHidden",
					Color.Cyan);
			}
		}

		private async void lblLocalIP_Click(object sender, EventArgs e)
		{
			string localIP = await Core.Instance.GetLocalIP();
			Clipboard.SetText(localIP);
			if (!isPrivacyLoading)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.LocalIpCopied",
					Color.Cyan,
					arguments: localIP);
			}
			else
			{
				AppendLocalizedLog(
					"Dashboard.Activity.LocalIpCopiedHidden",
					Color.Cyan);
			}
		}

		public void AppendLog(string message, Color? textColor = null, bool isBold = false)
		{
			AppendLogCore(
				message,
				LocalizationManager.TranslateRuntimeText(message),
				textColor,
				isBold);
		}

		private void AppendLocalizedLog(
			string resourceKey,
			Color? textColor = null,
			bool isBold = false,
			params object?[] arguments)
		{
			AppendLogCore(
				LocalizationManager.GetEnglish(resourceKey, arguments),
				LocalizationManager.Get(resourceKey, arguments),
				textColor,
				isBold);
		}

		private void AppendLogCore(
			string technicalMessage,
			string displayedMessage,
			Color? textColor,
			bool isBold)
		{
			technicalMessage = SecretRedactor.Redact(technicalMessage);
			displayedMessage = SecretRedactor.Redact(displayedMessage);
			FileHandler.QueueLog(
				"Synix_Log",
				$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {technicalMessage}");

			if (!IsHandleCreated || IsDisposed)
				return;

			if (rtbLog.InvokeRequired)
			{
				rtbLog.BeginInvoke(
					new Action(() => AppendLogToUi(
						displayedMessage,
						textColor,
						isBold)));
				return;
			}

			AppendLogToUi(displayedMessage, textColor, isBold);
		}

		private void AppendLogToUi(
			string displayedMessage,
			Color? textColor,
			bool isBold)
		{
			if (rtbLog.IsDisposed)
				return;

			string timeStamp = $"[{DateTime.Now:HH:mm:ss}] ";
			rtbLog.SelectionStart = rtbLog.TextLength;
			rtbLog.SelectionLength = 0;
			rtbLog.SelectionColor = ResolveLogColor(textColor);

			if (rtbLog.Lines.Length > 500)
			{
				rtbLog.ReadOnly = false;
				rtbLog.Select(0, rtbLog.GetFirstCharIndexFromLine(100));
				rtbLog.SelectedText = "";
				rtbLog.ClearUndo();
				rtbLog.ReadOnly = true;
			}

			rtbLog.SelectionFont = isBold ? boldFont : regularFont;
			rtbLog.AppendText(
				timeStamp + displayedMessage + Environment.NewLine);
			rtbLog.SelectionStart = rtbLog.Text.Length;
			rtbLog.ScrollToCaret();
		}

		private Color ResolveLogColor(Color? requestedColor)
		{
			if (!requestedColor.HasValue)
				return rtbLog.ForeColor;
			if (ThemeManager.IsDarkMode)
				return requestedColor.Value;

			Color color = requestedColor.Value;
			if (color.ToArgb() == Color.White.ToArgb() ||
				color.ToArgb() == Color.WhiteSmoke.ToArgb())
			{
				return SettingsPalette.PrimaryText;
			}
			if (color.ToArgb() == Color.Cyan.ToArgb())
				return SettingsPalette.Accent;
			if (color.ToArgb() == Color.Green.ToArgb())
				return SettingsPalette.Success;
			if (color.ToArgb() == Color.Red.ToArgb())
				return SettingsPalette.Danger;
			if (color.ToArgb() == Color.Yellow.ToArgb() ||
				color.ToArgb() == Color.Orange.ToArgb())
			{
				return SettingsPalette.Warning;
			}

			return color;
		}

		private async void MainGUI_Shown(object sender, EventArgs e)
		{
			if (SynixSessionRecovery.PreviousSessionWasInterrupted)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.CrashRecovery",
					Color.Orange,
					true);
			}
			try
			{
				await UpdatePrivacyMode(Properties.Settings.Default.PrivacyMode);
				await Core.Instance.RebindProcesses();
			}
			catch (Exception ex)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.RebindError",
					Color.Red,
					true,
					ex.Message);
			}

			try
			{
				double physicalRam = await Task.Run(ResourceMonitor.GetTotalSystemRamGB);
				double reserved = Math.Max(physicalRam * 0.10, 5.0);
				Core.TotalRamGb = Math.Max(1.0, physicalRam - reserved);
				ramGauge.MaxValue = (float)GetUsableSystemRamGb();
			}
			catch (Exception ex)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.ResourceError",
					Color.Orange,
					arguments: ex.Message);
			}

			UpdateDashboardSummary();
			chartTickCounter++;
			tmrResourceUpdates.Start();

			try
			{
				LocalizationManager.BindText(
					lblSteamStatus,
					"Text.06114F07909C7C52F41D");
				lblSteamStatus.ForeColor = SettingsPalette.Warning;
				await Task.Run(() => SteamCMD.EnsureSteamCMD((msg, color) => AppendLog(msg, color)));
				LocalizationManager.BindText(
					lblSteamStatus,
					"Text.427A3D1D34F4C09963DA");
				lblSteamStatus.ForeColor = SettingsPalette.Accent;
			}
			catch (Exception ex)
			{
				LocalizationManager.BindText(
					lblSteamStatus,
					"Text.F5C77CBF1B197C912BCA");
				lblSteamStatus.ForeColor = SettingsPalette.Danger;
				AppendLocalizedLog(
					"Dashboard.Activity.SteamCmdError",
					Color.Red,
					true,
					ex.Message);
			}

			if (SynixSessionRecovery.ShouldShowFirstRunGuide())
			{
				using FirstRunGuideDialog guide = new();
				if (guide.ShowDialog(this) == DialogResult.OK)
				{
					try
					{
						SynixSessionRecovery.CompleteFirstRunGuide();
					}
					catch (Exception exception)
					{
						AppendLocalizedLog(
							"Dashboard.Activity.FirstRunMarkerFailed",
							Color.Orange,
							arguments: exception.Message);
					}
				}
			}
		}

		public void UpdateGrid()
		{
			if (this.InvokeRequired)
			{
				this.BeginInvoke(new Action(UpdateGrid));
				return;
			}
			dataGridView1.Refresh();
			ApplyServerFilter();
			UpdateDashboardSummary();
			UpdateSelectedServerCard();
		}

		private void UpdateDashboardSummary()
		{
			int installedCount = serverList.Count;
			int runningCount = serverList.Count(server =>
				string.Equals(
					server.Status,
					StatusManager.GetStatus(ServerState.Running),
					StringComparison.OrdinalIgnoreCase));

			lblInstalledValue.Text = installedCount.ToString();
			lblRunningValue.Text = runningCount.ToString();
			bool isFiltered = !string.IsNullOrWhiteSpace(txtServerSearch.Text) ||
				!string.Equals(
					(cmbStatusFilter.SelectedItem as LocalizedOption)?.Value,
					"all",
					StringComparison.Ordinal);
			lblServersCount.Text = isFiltered
				? LocalizationManager.Get(
					"Dashboard.ServerCount.Filtered",
					_visibleServers.Count,
					installedCount)
				: installedCount == 1
					? LocalizationManager.Get(
						"Dashboard.ServerCount.One",
						installedCount)
					: LocalizationManager.Get(
						"Dashboard.ServerCount.Many",
						installedCount);
		}

		private void dataGridView1_SelectionChanged(object sender, EventArgs e)
		{
			UpdateSelectedServerCard();
		}

		private void dataGridView1_DataBindingComplete(
			object sender,
			DataGridViewBindingCompleteEventArgs e)
		{
			foreach (DataGridViewRow row in dataGridView1.Rows)
			{
				row.Height = 44;
			}

			ApplyServerFilter();
			UpdateDashboardSummary();
			UpdateSelectedServerCard();
		}

		private void UpdateSelectedServerCard()
		{
			DataGridViewRow? currentRow = dataGridView1.CurrentRow;
			GameServer? server = currentRow?.DataBoundItem as GameServer;
			bool hasSelection = server != null && currentRow != null;

			if (btnReadiness != null) btnReadiness.Enabled = hasSelection;
			if (btnServerOptions != null) btnServerOptions.Enabled = hasSelection;
			if (btnConfigure != null) btnConfigure.Enabled = hasSelection;
			if (btnStart != null) btnStart.Enabled = hasSelection;
			if (btnRestart != null) btnRestart.Enabled = hasSelection;
			if (btnStop != null) btnStop.Enabled = hasSelection;

			if (!hasSelection || server == null)
			{
				picSelectedServer.Image = null;
				LocalizationManager.BindText(
					lblSelectedGame,
					"Text.00EF301D2AB0505FDDA2");
				LocalizationManager.BindText(
					lblSelectedServerName,
					"Text.B28F2358A90FE65BFCC8");
				return;
			}

			picSelectedServer.Image = server.DisplayIcon;
			lblSelectedGame.Text = server.DisplayGameName;
			LocalizationManager.BindText(
				lblSelectedServerName,
				"Dashboard.SelectedServer.Summary",
				server.ServerName,
				BusyStatusPresentation.GetDisplayStatus(server.Status));
		}

		private void ServerFilterChanged(object sender, EventArgs e)
		{
			ApplyServerFilter();
			UpdateDashboardSummary();
		}

		private void ApplyServerFilter()
		{
			if (dataGridView1.DataSource == null)
				return;

			string searchText = txtServerSearch.Text.Trim();
			string statusFilter =
				(cmbStatusFilter?.SelectedItem as LocalizedOption)?.Value ?? "all";
			GameServer? selectedServer = dataGridView1.CurrentRow?.DataBoundItem as GameServer;
			List<GameServer> matchingServers = serverList
				.Where(server =>
				{
					bool searchMatch = string.IsNullOrWhiteSpace(searchText) ||
					(server.Game ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
					(server.ServerName ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase);
					return searchMatch && MatchesStatusFilter(server.Status, statusFilter);
				})
				.ToList();

			bool viewChanged = _visibleServers.Count != matchingServers.Count ||
				!_visibleServers.SequenceEqual(matchingServers);
			if (viewChanged)
			{
				_visibleServers.RaiseListChangedEvents = false;
				_visibleServers.Clear();
				foreach (GameServer server in matchingServers)
				{
					_visibleServers.Add(server);
				}
				_visibleServers.RaiseListChangedEvents = true;
				_visibleServers.ResetBindings();
			}

			if (selectedServer != null)
			{
				DataGridViewRow? restoredRow = dataGridView1.Rows
					.Cast<DataGridViewRow>()
					.FirstOrDefault(row => ReferenceEquals(row.DataBoundItem, selectedServer));

				if (restoredRow != null && restoredRow.Cells.Count > 0)
					dataGridView1.CurrentCell = restoredRow.Cells[0];
			}
			else if (dataGridView1.Rows.Count > 0 && dataGridView1.Rows[0].Cells.Count > 0)
			{
				dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
			}

			UpdateSelectedServerCard();
		}

		private static bool MatchesStatusFilter(string? status, string filter)
		{
			string currentStatus = status ?? string.Empty;
			return filter switch
			{
				"running" => currentStatus.Equals(
					StatusManager.GetStatus(ServerState.Running),
					StringComparison.OrdinalIgnoreCase),
				"stopped" => currentStatus.Equals(
					StatusManager.GetStatus(ServerState.Stopped),
					StringComparison.OrdinalIgnoreCase),
				"progress" =>
					currentStatus.StartsWith("Starting", StringComparison.OrdinalIgnoreCase) ||
					currentStatus.StartsWith("Stopping", StringComparison.OrdinalIgnoreCase) ||
					currentStatus.StartsWith("Installing", StringComparison.OrdinalIgnoreCase) ||
					currentStatus.StartsWith("Updating", StringComparison.OrdinalIgnoreCase) ||
					currentStatus.StartsWith(
						StatusManager.GetStatus(ServerState.BackingUp),
						StringComparison.OrdinalIgnoreCase) ||
					currentStatus.StartsWith("Restoring", StringComparison.OrdinalIgnoreCase) ||
					currentStatus.StartsWith("Validating", StringComparison.OrdinalIgnoreCase) ||
					currentStatus.StartsWith("Exporting", StringComparison.OrdinalIgnoreCase) ||
					currentStatus.StartsWith("Deleting", StringComparison.OrdinalIgnoreCase),
				"attention" => currentStatus.Equals(
					StatusManager.GetStatus(ServerState.Crashed),
					StringComparison.OrdinalIgnoreCase),
				_ => true
			};
		}

		private void btnClearLog_Click(object sender, EventArgs e)
		{
			rtbLog.Clear();
		}

		private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			GridStyler.SetStatusColor(dataGridView1, e);
			if (e.ColumnIndex == colStatus.Index && e.Value is string status)
			{
				e.Value = BusyStatusPresentation.GetDisplayStatus(status);
				e.FormattingApplied = true;
			}
		}

		private void dataGridView1_CellPainting(
			object? sender,
			DataGridViewCellPaintingEventArgs eventArgs)
		{
			if (eventArgs.RowIndex < 0 ||
				eventArgs.Graphics == null ||
				eventArgs.ColumnIndex != colStatus.Index ||
				dataGridView1.Rows[eventArgs.RowIndex].DataBoundItem is not GameServer server ||
				!BusyStatusPresentation.TryGetBusyState(server.Status, out string busyState))
			{
				return;
			}

			eventArgs.PaintBackground(eventArgs.CellBounds, true);
			eventArgs.Paint(
				eventArgs.CellBounds,
				DataGridViewPaintParts.Border);

			Rectangle indicatorBounds = new(
				eventArgs.CellBounds.Left + 10,
				eventArgs.CellBounds.Top + (eventArgs.CellBounds.Height - 18) / 2,
				18,
				18);
			BusyStatusPresentation.DrawIndicator(
				eventArgs.Graphics,
				indicatorBounds,
				SettingsPalette.Warning,
				true,
				_busyStatusFrame);

			Rectangle textBounds = new(
				indicatorBounds.Right + 7,
				eventArgs.CellBounds.Top,
				Math.Max(0, eventArgs.CellBounds.Right - indicatorBounds.Right - 11),
				eventArgs.CellBounds.Height);
			TextRenderer.DrawText(
				eventArgs.Graphics,
				BusyStatusPresentation.GetDisplayStatus(busyState),
				eventArgs.CellStyle?.Font ?? dataGridView1.Font,
				textBounds,
				SettingsPalette.Warning,
				TextFormatFlags.Left |
				TextFormatFlags.VerticalCenter |
				TextFormatFlags.EndEllipsis |
				TextFormatFlags.NoPadding);
			eventArgs.Handled = true;
		}

		private void ResourceGraph_Click(object sender, EventArgs e)
		{
			ResourceMonitorGUI monitor = new ResourceMonitorGUI();
			monitor.Show();
		}

		private GameServer? GetSelectedServer()
		{
			if (dataGridView1.CurrentRow == null)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.NoRowSelected",
					Color.Red);
				LocalizedMessageBox.Show(
					LocalizationManager.Get("Dashboard.NoServerSelected.Body"),
					LocalizationManager.Get("Dashboard.NoServerSelected.Title"));
				return null;
			}

			if (dataGridView1.CurrentRow.DataBoundItem is not GameServer selectedServer)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.InvalidServerObject",
					Color.Red);
				return null;
			}

			return selectedServer;
		}

		private async void btnAddServer_Click(object sender, EventArgs e)
		{
			if (isInitializing)
				return;

			using AddServerChoiceDialog choice = new();
			if (choice.ShowDialog(this) != DialogResult.OK)
				return;

			switch (choice.SelectedChoice)
			{
				case AddServerChoice.CreateNew:
					await Core.Instance.AddServerAndReport();
					break;
				case AddServerChoice.ImportExisting:
					using (ExistingServerImportWizard import = new())
						import.ShowDialog(this);
					break;
				case AddServerChoice.BrowseCatalog:
					using (GameSupportCatalog catalog = new())
						catalog.ShowDialog(this);
					break;
			}
		}

		private async void btnEdit_Click(object sender, EventArgs e)
		{
			if (isInitializing) return;
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;
			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "EditConfig"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}
			await Core.Instance.EditServerAndReport(selectedServer);
		}

		private async void btnUpdate_Click(object sender, EventArgs e)
		{
			if (isInitializing) return;
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;

			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Update"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}

			await Core.Instance.UpdateServerAndReport(selectedServer, "UPDATE");
		}

		private async void btnFileValidation_Click(object sender, EventArgs e)
		{
			if (isInitializing) return;
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;

			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Validate"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}

			await Core.Instance.UpdateServerAndReport(selectedServer, "VALIDATE");
		}

		private async void btnDelete_Click(object sender, EventArgs e)
		{
			if (isInitializing) return;
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;
			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Delete"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}
			if (await Core.Instance.DeleteServerAndReportAsync(selectedServer))
				ApplyServerFilter();
		}

		private async void btnBackup_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null)
				return;

			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Backup"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}

			ServerBackupPreflight preflight =
				await Core.Instance.CreateServerBackupPreflightAsync(selectedServer);
			if (!preflight.Succeeded)
			{
				LocalizedMessageBox.Show(
					this,
					preflight.Message,
					LocalizationManager.Get(
						"Dashboard.Backup.CheckFailed.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}
			if (!preflight.HasEnoughSpace)
			{
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get(
						"Dashboard.Backup.NotEnoughSpace.Body",
						Core.FormatBytes(preflight.SourceBytes),
						Core.FormatBytes(preflight.RequiredBytes),
						Core.FormatBytes(preflight.AvailableBytes),
						preflight.BackupFolder),
					LocalizationManager.Get(
						"Dashboard.Backup.NotEnoughSpace.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			DialogResult confirmation = LocalizedMessageBox.Show(
				this,
				LocalizationManager.Get(
					"Dashboard.Backup.Confirm.Body",
					selectedServer.ServerName,
					preflight.FileCount,
					Core.FormatBytes(preflight.SourceBytes),
					Core.FormatBytes(preflight.RequiredBytes),
					Core.FormatBytes(preflight.AvailableBytes)),
				LocalizationManager.Get("Dashboard.Backup.Confirm.Title"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Information,
				MessageBoxDefaultButton.Button2);
			if (confirmation == DialogResult.Yes)
				await Core.Instance.ExecuteBackup(selectedServer, StartContext.Manual);
		}

		private async void btnRestoreServerBackup_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null)
				return;
			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMessage, "Restore"))
			{
				AppendLog(lockMessage, Color.Orange);
				return;
			}

			IReadOnlyList<ServerBackupArchive> backups =
				await Core.Instance.GetServerBackupsAsync(selectedServer);
			if (backups.Count == 0)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.NoBackups",
					Color.Yellow,
					arguments: selectedServer.ServerName);
				return;
			}

			using ServerBackupRestoreDialog dialog = new(selectedServer, backups);
			if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedBackup == null)
				return;

			ServerBackupArchive selectedBackup = dialog.SelectedBackup;
			DialogResult confirmation = LocalizedMessageBox.Show(
				this,
				LocalizationManager.Get(
					"Dashboard.Restore.Confirm.Body",
					selectedServer.ServerName,
					selectedBackup.CreatedLocal,
					selectedBackup.FileName),
				LocalizationManager.Get("Dashboard.Restore.Confirm.Title"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning,
				MessageBoxDefaultButton.Button2);
			if (confirmation != DialogResult.Yes)
				return;

			Progress<string> progress = new(message =>
			{
				if (!message.StartsWith(
					LocalizationManager.Get("Backup.Progress.UnpackingPrefix"),
					StringComparison.OrdinalIgnoreCase))
					AppendLog(
						LocalizationManager.Get(
							"Dashboard.Restore.Activity",
							message),
						Color.Cyan);
			});
			ServerBackupRestoreResult result = await Core.Instance.RestoreServerBackupAsync(
				selectedServer,
				selectedBackup,
				progress);

			LocalizedMessageBox.Show(
				this,
				result.Message,
				LocalizationManager.Get(result.Succeeded
					? "Dashboard.Restore.Succeeded.Title"
					: "Dashboard.Restore.Failed.Title"),
				MessageBoxButtons.OK,
				result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Error);
		}

		private async void btnStart_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;

			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Start"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}
			if (selectedServer.Status == StatusManager.GetStatus(ServerState.Stopped))
			{
				await Core.Instance.ExecuteStartSequence(selectedServer);
			}
		}

		private async void btnStop_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;

			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Stop"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}
			if (selectedServer.Status == StatusManager.GetStatus(ServerState.Running))
			{
				try
				{
					await Core.Instance.StopServerAndReport(selectedServer);
				}
				catch (Exception ex)
				{

					AppendLocalizedLog(
						"Dashboard.Activity.StopError",
						Color.Red,
						arguments:
						[
							selectedServer.ServerName,
							ex.Message
						]);
				}
			}
		}

		private void btnOpenConfig_Click(object sender, EventArgs e)
		{
			StreamerModeCheck();
			if (isPrivacyLoading) return;
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;
			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Config"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}
			Core.Instance.OpenConfigEditor(selectedServer);
		}

		private void btnOpenFolder_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;
			Core.Instance.OpenServerFolder(selectedServer);
		}

		private async void btnOpenBackup_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;
			await Core.Instance.OpenBackFolderAsync(selectedServer);
		}

		private async void btnPublicConnection_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;
			if (selectedServer == null) return;

			GameInfo? gameData = GameDatabase.GetGame(selectedServer.Game);
			if (!GameDatabase.SupportsManualConnectionTesting(gameData))
			{
				AppendLocalizedLog(
					"Dashboard.Activity.WanUnsupported",
					Color.Yellow,
					arguments: selectedServer.Game);
				return;
			}

			AppendLocalizedLog(
				"Dashboard.Activity.WanTesting",
				Color.White,
				arguments: selectedServer.ServerName);

			try
			{
				string publicIp = await Core.Instance.GetPublicIP();
				string ipText = isPrivacyLoading ? "[HIDDEN]" : publicIp;

				bool isReachable = await Core.Instance.ExecuteDynamicProbes(selectedServer, publicIp);

				if (isReachable)
				{
					AppendLocalizedLog(
						"Dashboard.Activity.WanReachable",
						Color.Green,
						arguments:
						[
							selectedServer.ServerName,
							ipText
						]);
				}
				else
				{
					AppendLocalizedLog(
						"Dashboard.Activity.WanProbeFailed",
						Color.Red,
						arguments:
						[
							selectedServer.ServerName,
							ipText,
							selectedServer.Port,
							selectedServer.QueryPort
						]);
				}
			}
			catch (Exception ex)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.WanTestFailed",
					Color.Yellow,
					arguments: ex.Message);
				PlainEnglishErrorDialog.ShowError(
					this,
					LocalizationManager.Get("Dashboard.ErrorAction.TestInternet"),
					ex.Message);
			}
		}

		private async void btnLocalConnection_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;
			if (selectedServer == null) return;

			GameInfo? gameData = GameDatabase.GetGame(selectedServer.Game);
			if (!GameDatabase.SupportsManualConnectionTesting(gameData))
			{
				AppendLocalizedLog(
					"Dashboard.Activity.LanUnsupported",
					Color.Yellow,
					arguments: selectedServer.Game);
				return;
			}

			AppendLocalizedLog(
				"Dashboard.Activity.LanTesting",
				Color.White,
				arguments: selectedServer.ServerName);

			try
			{
				string localIp = await Core.Instance.GetLocalIP();
				string ipText = isPrivacyLoading ? "[HIDDEN]" : localIp;

				bool isReachable = await Core.Instance.ExecuteDynamicProbes(selectedServer, localIp);

				if (isReachable)
				{
					AppendLocalizedLog(
						"Dashboard.Activity.LanReachable",
						Color.Green,
						arguments:
						[
							selectedServer.ServerName,
							ipText
						]);
				}
				else
				{
					AppendLocalizedLog(
						"Dashboard.Activity.LanProbeFailed",
						Color.Red,
						arguments:
						[
							selectedServer.ServerName,
							ipText,
							selectedServer.Port,
							selectedServer.QueryPort
						]);
				}
			}
			catch (Exception ex)
			{
				AppendLocalizedLog(
					"Dashboard.Activity.LanTestFailed",
					Color.Yellow,
					arguments: ex.Message);
				PlainEnglishErrorDialog.ShowError(
					this,
					LocalizationManager.Get("Dashboard.ErrorAction.TestHomeNetwork"),
					ex.Message);
			}
		}

		internal static void UpdateModPluginManagerMenuItem(ToolStripMenuItem item, GameServer? server)
		{
			bool supported = ModSystemCatalog.CanManageAddOns(server);
			item.Visible = supported;
			item.Enabled = supported;
		}

		private async void btnServerOptionsMenu_Click(object sender, EventArgs e)
		{
			if (_modPluginManagerMenuItem != null)
				UpdateModPluginManagerMenuItem(_modPluginManagerMenuItem, GetSelectedServer());
			if (dataGridView1.CurrentRow != null && dataGridView1.CurrentRow.DataBoundItem is GameServer selectedServer)
			{
				bool isMinecraft = GameDatabase.IsMinecraft(selectedServer.Game);
				if (_satisfactoryControlMenuItem != null)
					_satisfactoryControlMenuItem.Visible = GameDatabase.IsSatisfactory(selectedServer.Game);
				GameInfo? selectedGameData = GameDatabase.GetGame(selectedServer.Game);
				bool supportsConnectionTesting =
					GameDatabase.SupportsManualConnectionTesting(selectedGameData);
				bool supportsPlayerManagement =
					GameDatabase.SupportsPlayerManagement(selectedServer);
				bool isRunning = CanShowLiveServerActions(selectedServer);
				if (_playerManagementMenuItem != null)
				{
					bool showPlayerManagement = isRunning && supportsPlayerManagement;
					_playerManagementMenuItem.Visible = showPlayerManagement;
					_playerManagementMenuItem.Enabled = showPlayerManagement;
				}
				if (_liveProcessDetailsMenuItem != null)
				{
					_liveProcessDetailsMenuItem.Visible = isRunning;
					_liveProcessDetailsMenuItem.Enabled = isRunning;
				}
				if (_minecraftConsoleMenuItem != null)
				{
					_minecraftConsoleMenuItem.Visible = isMinecraft;
					_minecraftConsoleMenuItem.Enabled = isMinecraft;
				}

				updateServerToolStripMenuItem.Enabled = !isMinecraft;
				updateServerToolStripMenuItem.Visible = !isMinecraft;
				fileValidationToolStripMenuItem.Enabled = !isMinecraft;
				fileValidationToolStripMenuItem.Visible = !isMinecraft;
				btnExportBatch.Enabled = !isMinecraft;
				btnExportBatch.Visible = !isMinecraft;
				bool canOpenConfigurationEditor =
					Core.CanOpenConfigurationEditor(selectedServer);
				openServerConfigFileToolStripMenuItem.Visible =
					canOpenConfigurationEditor;
				openServerConfigFileToolStripMenuItem.Enabled =
					canOpenConfigurationEditor;
				bool hasDetectedLogs = GameLogDiscovery.HasDetectedLogs(selectedServer);
				openLatestGameLogToolStripMenuItem.Visible = hasDetectedLogs;
				openLatestGameLogToolStripMenuItem.Enabled = hasDetectedLogs;
				bool hasBackups = await Core.Instance.HasServerBackupsAsync(selectedServer);
				if (!ReferenceEquals(selectedServer, GetSelectedServer()))
					return;
				restoreServerBackupToolStripMenuItem.Visible = hasBackups;
				restoreServerBackupToolStripMenuItem.Enabled = hasBackups;

				if (isRunning)
				{
					connectionTestToolStripMenuItem.Visible = supportsConnectionTesting;
					connectionTestToolStripMenuItem.Enabled = supportsConnectionTesting;
					connectionLocalTestToolStripMenuItem.Visible = supportsConnectionTesting;
					connectionLocalTestToolStripMenuItem.Enabled = supportsConnectionTesting;
					toolStripSeparator3.Visible = supportsConnectionTesting;
				}
				else
				{
					connectionTestToolStripMenuItem.Visible = false;
					connectionTestToolStripMenuItem.Enabled = false;
					connectionLocalTestToolStripMenuItem.Visible = false;
					connectionLocalTestToolStripMenuItem.Enabled = false;
					toolStripSeparator3.Visible = false;
				}
			}

			contextMenuStrip.Show(btnServerOptions, new System.Drawing.Point(0, 0), ToolStripDropDownDirection.AboveRight);
		}

		private void btnReadiness_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null)
				return;

			using TroubleshooterDialog dialog = new(selectedServer);
			dialog.ShowDialog(this);
		}

		private async void btnRestart_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null) return;
			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Restart"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}
			if (selectedServer.Status == StatusManager.GetStatus(ServerState.Running))
			{
				try
				{
					await Core.Instance.ExecuteStartSequence(selectedServer, "RESTART");
				}
				catch (Exception ex)
				{
					AppendLocalizedLog(
						"Dashboard.Activity.RestartError",
						Color.Red,
						arguments:
						[
							selectedServer.ServerName,
							ex.Message
						]);
					PlainEnglishErrorDialog.ShowError(
						this,
						LocalizationManager.Get("Dashboard.ErrorAction.RestartServer"),
						ex.Message);
				}
			}
		}

		private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0)
			{
				GameServer? selectedServer = GetSelectedServer();
				if (selectedServer == null) return;
				Help.ServerInfo infoForm = new Help.ServerInfo(selectedServer);
				infoForm.Show();
			}
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			using (HelpGUI helpWindow = new HelpGUI())
			{
				helpWindow.ShowDialog();
			}
		}

		private void InitializeVersionCheckTimer()
		{
			versionTimer = new System.Windows.Forms.Timer(components);
			versionTimer.Interval = 20 * 60 * 1000;
			versionTimer.Tick += async (sender, e) =>
			{
				await VersionCheck();
			};

			versionTimer.Start();
		}

		private async Task VersionCheck()
		{
			Version currentVersion = Core.GetCurrentVersion();
			if (!await _versionCheckGate.WaitAsync(0))
				return;

			btnDownloadUpdate.Visible = false;
			btnDownloadUpdate.Enabled = false;
			LocalizationManager.BindText(
				lblUpdateStatus,
				"Text.8BCAEE3C780FC5A57E1C");
			lblUpdateStatus.ForeColor = SettingsPalette.MutedText;
			lblUpdateStatus.BackColor = SettingsPalette.TitleBar;

			try
			{
				using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(25));
				_updateCheckResult = await Core.CheckForUpdatesAsync(
					currentVersion,
					timeout.Token);

				if (_updateCheckResult.UpdateAvailable)
				{
					string latestVersion =
						_updateCheckResult.AdvertisedVersion?.ToString(3) ??
						LocalizationManager.Get("Updates.Version.New");
					LocalizationManager.BindText(
						lblUpdateStatus,
						_updateCheckResult.Release is not null
							? "Dashboard.Update.Available"
							: "Dashboard.Update.DetailsUnavailable",
						latestVersion,
						currentVersion.ToString(3));
					lblUpdateStatus.ForeColor = SettingsPalette.Warning;
					LocalizationManager.BindText(
						btnDownloadUpdate,
						_updateCheckResult.CanInstall
							? "Text.7E08C50FB774DD673401"
							: "Dashboard.Update.Button.Details");
					btnDownloadUpdate.Visible = true;
					btnDownloadUpdate.Enabled = true;
				}
				else
				{
					LocalizationManager.BindText(
						lblUpdateStatus,
						"Dashboard.Update.Latest",
						currentVersion.ToString(3));
					lblUpdateStatus.ForeColor = SettingsPalette.Accent;
				}
			}
			catch (Exception exception)
			{
				_updateCheckResult = null;
				LocalizationManager.BindText(
					lblUpdateStatus,
					"Dashboard.Update.CheckUnavailable",
					currentVersion.ToString(3));
				lblUpdateStatus.ForeColor = SettingsPalette.MutedText;
				AppendLocalizedLog(
					"Dashboard.Activity.UpdateCheckFailed",
					Color.Orange,
					arguments: exception.Message);
			}
			finally
			{
				_versionCheckGate.Release();
			}
		}

		private async void btnDownloadUpdate_Click(object sender, EventArgs e)
		{
			if (_updateCheckResult?.Release is null)
			{
				await VersionCheck();
				if (_updateCheckResult?.Release is null)
				{
					LocalizedMessageBox.Show(
						this,
						_updateCheckResult?.Problem ??
						LocalizationManager.Get("Dashboard.Update.LoadDetailsFailed"),
						LocalizationManager.Get("Dashboard.Update.DetailsUnavailable.Title"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					return;
				}
			}

			using SynixUpdateDialog updateDialog = new(_updateCheckResult);
			if (updateDialog.ShowDialog(this) != DialogResult.OK)
				return;

			if (!CanInstallSynixUpdate())
				return;

			Core.Instance.isDownloadActive = true;
			versionTimer?.Stop();
			btnDownloadUpdate.Enabled = false;
			try
			{
				Progress<SynixUpdateDownloadProgress> progress = new(download =>
				{
					LocalizationManager.BindText(
						lblUpdateStatus,
						"Dashboard.Update.Downloading",
						download.Percent);
					btnDownloadUpdate.Text = $"{download.Percent}%";
				});

				SynixPreparedUpdate prepared = await Core.PrepareUpdateAsync(
					_updateCheckResult,
					progress);

				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get(
						"Dashboard.Update.Ready.Body",
						prepared.NewVersion.ToString(3)),
					LocalizationManager.Get("Dashboard.Update.Ready.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);

				await FileHandler.FlushLogsAsync();
				BackgroundServiceManager.SuppressStartForCurrentProcess();
				Core.LaunchPreparedUpdate(prepared);
				_updateShutdownRequested = true;
				Core.Instance.isDownloadActive = false;
				Application.Exit();
			}
			catch (Exception exception)
			{
				LocalizationManager.BindText(
					lblUpdateStatus,
					"Text.09872897779CD02DAC70");
				LocalizationManager.BindText(
					btnDownloadUpdate,
					"Text.7E08C50FB774DD673401");
				btnDownloadUpdate.Enabled = true;
				LocalizedMessageBox.Show(
					this,
					exception.Message,
					LocalizationManager.Get("Dashboard.Update.Failed.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
			finally
			{
				if (!_updateShutdownRequested)
				{
					Core.Instance.isDownloadActive = false;
					versionTimer?.Start();
				}
			}
		}

		private bool CanInstallSynixUpdate()
		{
			bool serverBusy = serverList.Any(server =>
				server.Status != Core.StatusManager.GetStatus(
					Core.ServerState.Stopped));
			bool maintenanceBusy = Core.Instance.isDownloadActive;
			if (serverBusy || maintenanceBusy)
			{
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("Dashboard.Update.Busy.Body"),
					LocalizationManager.Get("Dashboard.Update.Busy.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return false;
			}

			if (!FileHandler.SaveServers())
			{
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("Dashboard.Update.SaveFailed.Body"),
					LocalizationManager.Get("Dashboard.Update.SaveFailed.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return false;
			}

			return true;
		}
		public async Task UpdatePrivacyMode(bool isEnabled)
		{
			isPrivacyLoading = isEnabled;
			UpdateNetworkLabels();
			await LoadNetworkInfo();
		}

		private void btnExportBatch_Click(object sender, EventArgs e)
		{
			if (isInitializing) return;

			var selectedServer = GetSelectedServer();
			if (selectedServer == null) return;

			if (!Core.Instance.PassSpamLock(selectedServer, out string lockMsg, "Export"))
			{
				AppendLog(lockMsg, Color.Orange);
				return;
			}

			DialogResult exportConfirmation = LocalizedMessageBox.Show(
				this,
				LocalizationManager.Get("Dashboard.Export.CredentialsWarning.Body"),
				LocalizationManager.Get("Dashboard.Export.CredentialsWarning.Title"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning,
				MessageBoxDefaultButton.Button2);
			if (exportConfirmation != DialogResult.Yes)
				return;

			bool success = Core.Instance.ExportServerToBatch(selectedServer);

			if (success)
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"Dashboard.Export.Complete.Body",
						selectedServer.InstallPath),
					LocalizationManager.Get("Dashboard.Export.Complete.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
			}
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void btnMinimize_Click(object sender, EventArgs e)
		{
			this.WindowState = FormWindowState.Minimized;
		}

		private void btnDiscord_Click(object sender, EventArgs e)
		{
			OpenUrl("https://discord.gg/WduKEU3j8s");
		}

		private void btnGithub_Click(object sender, EventArgs e)
		{
			OpenUrl("https://github.com/ubidzz/Synix-Control-Panel");
		}

		private void OpenUrl(string url)
		{
			try
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = url,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get("Dashboard.Link.OpenFailed.Body", ex.Message),
					LocalizationManager.Get("Dashboard.Link.OpenFailed.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}

		private void btnSettings_Click(object sender, EventArgs e)
		{
			using (AppSettings SynixSettings = new AppSettings())
			{
				SynixSettings.ShowDialog(this);
			}
		}

		private void btnOpenLatestGameLog_Click(object sender, EventArgs e)
		{
			GameServer? selectedServer = GetSelectedServer();
			if (selectedServer == null)
				return;
			Core.Instance.OpenLatestGameLog(selectedServer);
		}
	}
}
