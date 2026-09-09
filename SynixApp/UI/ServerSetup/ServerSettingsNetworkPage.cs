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
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixApp.UI.ServerSetup
{
	public partial class ServerSettingsNetworkPage : UserControl
	{
		private GameInfo? _gameData;
		private bool _editMode;
		private bool _isLoading;
		private bool _isApplyingPortOffset;

		public event EventHandler? SettingsChanged;

		public ServerSettingsNetworkPage()
		{
			InitializeComponent();
			LocalizationManager.BindAccessibleName(
				chkEnableRcon,
				"ServerSetup.Network.RconToggle.AccessibleName");
			numPort.TextChanged += GamePortTextChanged;
			numPort.ValueChanged += SettingsControlChanged;
			numQueryPort.ValueChanged += SettingsControlChanged;
			numAppPort.ValueChanged += SettingsControlChanged;
			numRconPort.ValueChanged += SettingsControlChanged;
			chkEnableRcon.CheckedChanged += RconEnabledChanged;
			txtRconPassword.TextChanged += SettingsControlChanged;
		}

		public int GamePort => (int)numPort.Value;
		public int QueryPort => (int)numQueryPort.Value;
		public int RconPort => (int)numRconPort.Value;
		public int? AppPort => AppPortEnabled ? (int)numAppPort.Value : null;
		public string RconPassword => txtRconPassword.Text;
		public bool RconEnabled => chkEnableRcon.Enabled && chkEnableRcon.Checked;
		public bool GamePortEnabled => numPort.Enabled;
		public bool QueryPortEnabled => numQueryPort.Enabled;
		public bool AppPortEnabled => numAppPort.Enabled;
		public void SetAdvancedMode(bool enabled) => cardRcon.Visible = enabled;

		public void SetPrivacyMode(bool enabled) =>
			txtRconPassword.UseSystemPasswordChar = enabled;

		public void ClearSecret() => txtRconPassword.Clear();

		public void LoadServer(GameServer server, GameInfo? gameData)
		{
			ArgumentNullException.ThrowIfNull(server);
			_isLoading = true;
			try
			{
				numPort.Value = Math.Clamp(server.Port, numPort.Minimum, numPort.Maximum);
				int queryPort = server.QueryPort > 0
					? server.QueryPort
					: gameData?.QueryPort ?? (int)numQueryPort.Minimum;
				numQueryPort.Value = Math.Clamp(
					queryPort,
					numQueryPort.Minimum,
					numQueryPort.Maximum);
				numAppPort.Value = Math.Clamp(
					server.AppPort ?? numAppPort.Minimum,
					numAppPort.Minimum,
					numAppPort.Maximum);
				chkEnableRcon.Checked = server.EnableRcon;
				numRconPort.Value = Math.Clamp(
					server.RconPort,
					numRconPort.Minimum,
					numRconPort.Maximum);
			}
			finally
			{
				_isLoading = false;
			}
		}

		public void SetRconPassword(string password)
		{
			_isLoading = true;
			try
			{
				txtRconPassword.Text = password ?? string.Empty;
			}
			finally
			{
				_isLoading = false;
			}
		}

		public void ConfigureForGame(
			GameInfo? gameData,
			bool isMinecraftBedrock,
			bool editMode)
		{
			_gameData = gameData;
			_editMode = editMode;
			GameManagementCapability capabilities = gameData == null
				? GameManagementCapability.None
				: GameFix.GetManagementCapabilities(gameData);
			if (isMinecraftBedrock)
			{
				capabilities =
					GameManagementCapability.Port |
					GameManagementCapability.QueryPort;
			}

			bool Supports(GameManagementCapability capability) =>
				(capabilities & capability) != 0;
			numPort.Tag = Supports(GameManagementCapability.Port);
			numQueryPort.Tag = Supports(GameManagementCapability.QueryPort);
			numAppPort.Tag = Supports(GameManagementCapability.AppPort);
			chkEnableRcon.Tag = Supports(GameManagementCapability.Rcon);
			LocalizationManager.BindText(
				QueryPortLabel,
				isMinecraftBedrock
					? "ServerSetup.Port.Ipv6"
					: "ServerSetup.Port.Query");
			if (isMinecraftBedrock)
				chkEnableRcon.Checked = false;
		}

		public void ApplyAvailability(bool hasGame)
		{
			numPort.Enabled = hasGame && numPort.Tag is true;
			numQueryPort.Enabled = hasGame && numQueryPort.Tag is true;
			numAppPort.Enabled = hasGame && numAppPort.Tag is true;
			chkEnableRcon.Enabled = hasGame && chkEnableRcon.Tag is true;
			bool rconActive = chkEnableRcon.Enabled && chkEnableRcon.Checked;
			numRconPort.Enabled = rconActive;
			txtRconPassword.Enabled = rconActive;
		}

		public void ApplyDefaultPorts(GameInfo gameData)
		{
			ArgumentNullException.ThrowIfNull(gameData);
			_isLoading = true;
			try
			{
				int gamePort = Math.Clamp(
					gameData.Port,
					(int)numPort.Minimum,
					(int)numPort.Maximum);
				int queryPort = Math.Clamp(
					gameData.QueryPort,
					(int)numQueryPort.Minimum,
					(int)numQueryPort.Maximum);
				if (!_editMode)
				{
					gamePort = ExistingServerImport.FindAvailablePort(
						gamePort,
						ServerRegistry.Servers);
					queryPort = ExistingServerImport.FindAvailablePort(
						queryPort,
						ServerRegistry.Servers.Concat([
							new GameServer { Port = gamePort }
						]));
				}
				numPort.Value = gamePort;
				numQueryPort.Value = queryPort;
				if (gameData.AppPort.HasValue)
				{
					numAppPort.Value = Math.Clamp(
						gameData.AppPort.Value,
						numAppPort.Minimum,
						numAppPort.Maximum);
				}
			}
			finally
			{
				_isLoading = false;
			}
		}

		public void ApplyMinecraftEditionDefaults(bool bedrock)
		{
			LocalizationManager.BindText(
				QueryPortLabel,
				bedrock
					? "ServerSetup.Port.Ipv6"
					: "ServerSetup.Port.Query");
			if (_editMode)
				return;

			int preferredPort = bedrock
				? MinecraftControlProfile.BedrockDefaultPort
				: 25565;
			int preferredSecondaryPort = bedrock
				? MinecraftControlProfile.BedrockDefaultIpv6Port
				: 25565;
			int gamePort = ExistingServerImport.FindAvailablePort(
				preferredPort,
				ServerRegistry.Servers);
			int secondaryPort = ExistingServerImport.FindAvailablePort(
				preferredSecondaryPort,
				ServerRegistry.Servers.Concat([new GameServer { Port = gamePort }]));
			_isLoading = true;
			try
			{
				numPort.Value = Math.Clamp(gamePort, numPort.Minimum, numPort.Maximum);
				numQueryPort.Value = Math.Clamp(
					secondaryPort,
					numQueryPort.Minimum,
					numQueryPort.Maximum);
			}
			finally
			{
				_isLoading = false;
			}
		}

		public PortValidationResult ValidatePorts(GameServer? existingServer)
		{
			List<(int Port, string Name)> selectedPorts = [];
			if (GamePortEnabled)
				selectedPorts.Add((GamePort,
					LocalizationManager.Get("ServerSetup.Port.Game")));
			if (QueryPortEnabled)
				selectedPorts.Add((QueryPort,
					LocalizationManager.Get("ServerSetup.Port.Query")));
			if (RconEnabled)
				selectedPorts.Add((RconPort,
					LocalizationManager.Get("ServerSetup.Port.Rcon")));
			if (AppPortEnabled && AppPort.HasValue)
				selectedPorts.Add((AppPort.Value,
					LocalizationManager.Get("ServerSetup.Port.App")));

			IGrouping<int, (int Port, string Name)>? duplicate = selectedPorts
				.GroupBy(port => port.Port)
				.FirstOrDefault(group => group.Count() > 1);
			if (duplicate != null)
			{
				string roles = string.Join(
					LocalizationManager.Get("ServerSetup.List.AndSeparator"),
					duplicate.Select(port => port.Name));
				return new PortValidationResult(
					true,
					LocalizationManager.Get(
						"ServerSetup.Validation.DuplicatePort",
						roles,
						duplicate.Key));
			}

			foreach ((int port, string name) in selectedPorts)
			{
				string? owner = Core.Instance.GetConfiguredPortCollisionOwner(
					port,
					existingServer);
				bool occupied = Core.Instance.IsPortInUseLocally(port);
				if (owner == null && !occupied)
					continue;

				return new PortValidationResult(
					true,
					LocalizationManager.Get(
						"ServerSetup.Validation.PortBlocked",
						name,
						port,
						owner ?? LocalizationManager.Get(
							"ServerSetup.Port.SystemProcess")));
			}

			return new PortValidationResult(false, string.Empty);
		}

		private void GamePortTextChanged(object? sender, EventArgs eventArgs)
		{
			if (_isLoading ||
				_isApplyingPortOffset ||
				_gameData == null ||
				!numPort.Enabled ||
				!numQueryPort.Enabled ||
				!int.TryParse(numPort.Text, out int gamePort))
			{
				return;
			}

			long defaultOffset = (long)_gameData.QueryPort - _gameData.Port;
			int queryPort = (int)Math.Clamp(
				gamePort + defaultOffset,
				(long)numQueryPort.Minimum,
				(long)numQueryPort.Maximum);
			if (!_editMode)
			{
				queryPort = ExistingServerImport.FindAvailablePort(
					queryPort,
					ServerRegistry.Servers.Concat([new GameServer { Port = gamePort }]));
			}

			try
			{
				_isApplyingPortOffset = true;
				numQueryPort.Value = queryPort;
			}
			finally
			{
				_isApplyingPortOffset = false;
			}
		}

		private void RconEnabledChanged(object? sender, EventArgs eventArgs)
		{
			if (!_isLoading)
			{
				bool active = chkEnableRcon.Enabled && chkEnableRcon.Checked;
				numRconPort.Enabled = active;
				txtRconPassword.Enabled = active;
			}
			SettingsControlChanged(sender, eventArgs);
		}

		private void SettingsControlChanged(object? sender, EventArgs eventArgs)
		{
			if (!_isLoading)
				SettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public readonly record struct PortValidationResult(
		bool HasConflict,
		string ErrorMessage)
	{
	}
}
