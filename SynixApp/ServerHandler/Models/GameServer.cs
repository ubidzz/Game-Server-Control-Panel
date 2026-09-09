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
using Synix_Control_Panel.SynixApp.ServerHandler;
using System.Diagnostics;
using System.Text.Json.Serialization;
using static Synix_Control_Panel.SynixApp.Database.GameDatabase;
using static Synix_Control_Panel.SynixEngine.Core;

public enum ServerProbeProtocol
{
	Auto,
	A2S,
	EpicOnlineServices,
	RestApi,
	Tcp
}

public enum ConfigFileCreationMode
{
	Unknown,
	GameGenerated,
	SynixTemplate,
	LaunchArgumentsOnly
}

public enum GameLifecycleTrackingMode
{
	Process,
	ExternalDeployment
}

public enum GameLifecycleControllerKind
{
	Standard,
	Minecraft
}

public enum GameConsoleControllerKind
{
	None,
	Minecraft
}

public enum GameConfigurationControllerKind
{
	Generic,
	Minecraft
}

public enum GamePlayerControllerKind
{
	QueryProtocol,
	Minecraft
}

public sealed class GameControlCapabilities
{
	public GameLifecycleControllerKind Lifecycle { get; init; } =
		GameLifecycleControllerKind.Standard;
	public GameConsoleControllerKind Console { get; init; } =
		GameConsoleControllerKind.None;
	public GameConfigurationControllerKind Configuration { get; init; } =
		GameConfigurationControllerKind.Generic;
	public GamePlayerControllerKind Players { get; init; } =
		GamePlayerControllerKind.QueryProtocol;
}

public enum GameCompatibilityStatus
{
	NeedsCommunityTesting,
	NeedsConfigurationTemplate,
	InstallationVerifiedOnly,
	PartiallyVerified,
	FullyVerified
}

public enum DotNetFrameworkRequirement
{
	None,
	NetFramework48,
	NetFramework481
}

public enum VisualCppRedistributableRequirement
{
	VisualCpp2013X64,
	VisualCpp2015To2022X64
}

public sealed class GameRuntimeRequirements
{
	public int MinimumSystemMemoryGb { get; init; }
	public bool RequiresAvx2 { get; init; }
	public bool RequiresHardwareVirtualization { get; init; }
	public bool RequiresHyperV { get; init; }
	public bool RequiresWindowsProfessionalOrHigher { get; init; }
	public DotNetFrameworkRequirement MinimumDotNetFramework { get; init; }
	public IReadOnlyList<VisualCppRedistributableRequirement>
		VisualCppRedistributables { get; init; } = [];
}

public sealed class GameLaunchBehavior
{
	// Some dedicated servers register with the client app ID, not their SteamCMD package ID.
	public string SteamAppId { get; init; } = string.Empty;
	public bool RunElevated { get; init; }
	public bool RequiresVisibleWindow { get; init; }
	public GameLifecycleTrackingMode LifecycleTracking { get; init; } =
		GameLifecycleTrackingMode.Process;
	public bool AllowLaunchFileExport { get; init; } = true;
	public string ReadyMessage { get; init; } = string.Empty;
	public string ReadyLogText { get; init; } = string.Empty;
}

public class GameDefinition
{
	public const int DefaultMaximumPlayers = 1000;

	[JsonIgnore]
	public string DefinitionId { get; init; } = string.Empty;
	[JsonIgnore]
	public int CatalogOrder { get; init; } = int.MaxValue;
	public string Game { get; set; } = string.Empty;
	[JsonIgnore]
	public bool NeedsConfigWarning { get; internal set; }
	[JsonIgnore]
	public string WarningMessage { get; set; } = LocalizationManager.GetEnglish(
		"GameDefinition.Default.ConfigurationWarning");
	public ConfigFormat Format { get; set; }
	[JsonIgnore]
	public ConfigFileCreationMode ConfigFileCreation { get; init; } =
		ConfigFileCreationMode.Unknown;
	[JsonIgnore]
	public string RelativeConfigPath { get; init; } = string.Empty;
	[JsonIgnore]
	public string ExternalDataFolderName { get; init; } = string.Empty;
	[JsonIgnore]
	public string[] RequiredLaunchFiles { get; init; } = [];
	[JsonIgnore]
	public string[] OptionalLaunchFiles { get; init; } = [];
	[JsonIgnore]
	public string LaunchFileSetupInstructions { get; init; } = string.Empty;
	[JsonIgnore]
	public string AppID { get; set; } = string.Empty;
	[JsonIgnore]
	public bool RequiresSteamLogin { get; init; }
	[JsonIgnore]
	public string SteamAppConfig { get; init; } = string.Empty;
	[JsonIgnore]
	public string ExeName { get; set; } = string.Empty;
	[JsonIgnore]
	public string DownloadUrl { get; init; } = string.Empty;
	public int WorldSize { get; set; }
	public string WorldSeed { get; set; } = "12345";
	[JsonIgnore]
	public string RequiredArgs { get; set; } = string.Empty;
	[JsonIgnore]
	public List<string> Maps { get; set; } = [];
	public int Port { get; set; }
	public int QueryPort { get; set; }
	public int? AppPort { get; set; }
	[JsonIgnore]
	public int MaximumPlayers { get; init; } = DefaultMaximumPlayers;
	[JsonIgnore]
	public bool RequiresAdminPassword { get; init; }
	[JsonIgnore]
	public bool RequiresAuthenticationToken { get; init; }
	[JsonIgnore]
	public string AuthenticationTokenLabel { get; init; } =
		LocalizationManager.GetEnglish("GameInput.AuthenticationToken.Label");
	[JsonIgnore]
	public string AuthenticationTokenHelpUrl { get; init; } = string.Empty;
	[JsonIgnore]
	public int MinimumServerPasswordLength { get; init; }
	[JsonIgnore]
	public bool ServerPasswordMustNotAppearInName { get; init; }

	public List<string> GameModes { get; set; } = [];
	[JsonIgnore]
	public string PvpValue { get; init; } = "PVP";
	[JsonIgnore]
	public string PveValue { get; init; } = "PVE";
	[JsonIgnore]
	public string BooleanTrueValue { get; init; } = "true";
	[JsonIgnore]
	public string BooleanFalseValue { get; init; } = "false";
	[JsonIgnore]
	public string CrossplayEnabledValue { get; init; } = "true";
	[JsonIgnore]
	public string CrossplayDisabledValue { get; init; } = "false";
	public string RconSyntax { get; init; } = "";
	public string IconUrl { get; init; } = string.Empty;
	[JsonIgnore]
	public bool IsQueryable { get; init; } = true;
	[JsonIgnore]
	public bool CrossplayDisablesPlayerTracking { get; init; }
	[JsonIgnore]
	public ServerProbeProtocol ProbeProtocol { get; init; } = ServerProbeProtocol.Auto;
	[JsonIgnore]
	public bool SupportsManualConnectionTesting { get; init; } = true;
	[JsonIgnore]
	public string ProbePath { get; init; } = string.Empty;
	[JsonIgnore]
	public string EosDeploymentId { get; init; } = string.Empty;
	[JsonIgnore]
	public IReadOnlyList<string> Aliases { get; init; } = [];
	[JsonIgnore]
	public int DefinitionSchemaVersion { get; init; } = 1;
	[JsonIgnore]
	public int DefinitionRevision { get; init; } = 1;
	[JsonIgnore]
	public bool IsEmbeddedDefinition { get; internal set; }
	[JsonIgnore]
	public GameRuntimeRequirements RuntimeRequirements { get; init; } = new();
	[JsonIgnore]
	public GameLaunchBehavior LaunchBehavior { get; init; } = new();
	[JsonIgnore]
	public GameControlCapabilities ControlCapabilities { get; init; } = new();
	[JsonIgnore]
	public IReadOnlyList<string> SupportedServerFrameworks { get; init; } = [];
	[JsonIgnore]
	public IReadOnlyList<string> LogPaths { get; init; } = [];
}

public sealed class GameInfo : GameDefinition
{
}

[Flags]
public enum DiscordNotificationEvent : long
{
	None = 0,
	ServerStarting = 1L << 0,
	ServerOnline = 1L << 1,
	ServerStopping = 1L << 2,
	ServerStopped = 1L << 3,
	ServerRestarting = 1L << 4,
	ServerCrashed = 1L << 5,
	InstallStarted = 1L << 6,
	InstallCompleted = 1L << 7,
	InstallFailed = 1L << 8,
	UpdateStarted = 1L << 9,
	UpdateCompleted = 1L << 10,
	UpdateFailed = 1L << 11,
	VerificationStarted = 1L << 12,
	VerificationCompleted = 1L << 13,
	VerificationFailed = 1L << 14,
	BackupStarted = 1L << 15,
	BackupCompleted = 1L << 16,
	BackupFailed = 1L << 17,
	RestoreStarted = 1L << 18,
	RestoreCompleted = 1L << 19,
	RestoreFailed = 1L << 20,
	ResourceWarning = 1L << 21,
	MonitoringWarning = 1L << 22,
	ConfigurationWarning = 1L << 23,
	SecurityWarning = 1L << 24,
	All = (1L << 25) - 1
}

public sealed class DiscordWebhookRoute
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public string Name { get; set; } =
		LocalizationManager.Get("Discord.Destination.DefaultName");
	public bool Enabled { get; set; } = true;
	public string WebhookUrl { get; set; } = string.Empty;
	public DiscordNotificationEvent Events { get; set; } =
		DiscordNotificationEvent.All;
}

public sealed class ServerProcessIdentity
{
	public int ProcessId { get; set; }
	public string ExecutablePath { get; set; } = string.Empty;
	public DateTime? StartTimeUtc { get; set; }
}

public class GameServer
{
	public int DataSchemaVersion { get; set; }
	public string Game { get; set; } = string.Empty;
	[JsonIgnore]
	public string DisplayGameName => IsMinecraft(Game)
		? $"Minecraft {MinecraftControlProfile.NormalizeEdition(MinecraftEdition)}"
		: Game;
	[JsonIgnore]
	public System.Drawing.Image DisplayIcon { get; set; } = null!;
	[JsonIgnore]
	public bool HasAnnouncedOnline { get; set; }
	public int WorldSize { get; set; }
	public string WorldSeed { get; set; } = "12345";
	public int Port { get; set; }
	public int QueryPort { get; set; }
	public int? AppPort { get; set; }
	public string ExtraArgs { get; set; } = string.Empty;
	[JsonIgnore]
	public int CurrentPlayers { get; set; }
	public bool IsScheduledRestartEnabled { get; set; }
	public string RestartTime { get; set; } = "04:00";
	public bool[] RestartDays { get; set; } = [true, true, true, true, true, true, true];
	public string LastMaintenanceDate { get; set; } = string.Empty;
	public bool SmartMaintenanceEnabled { get; set; } = true;
	public bool MaintenanceWaitForPlayers { get; set; } = true;
	public int MaintenanceMaximumDelayMinutes { get; set; } = 30;
	public bool MaintenanceBackupBeforeRestart { get; set; } = true;
	public bool MaintenanceUpdateBeforeRestart { get; set; } = false;
	[JsonIgnore]
	public DateTime? LastMaintenanceDeferralNoticeUtc { get; set; }
	[JsonIgnore]
	public DateTime? MaintenanceRetryAfterUtc { get; set; }
	[JsonIgnore]
	public int MaxPlayersFromQuery { get; set; }
	[JsonIgnore]
	public DateTime? LastProbeTime { get; set; }
	public int PasswordStorageVersion { get; set; }
	public string SteamAccountName { get; set; } = string.Empty;
	public bool SteamAuthenticationRequired { get; set; }
	public string InstallPath { get; set; } = string.Empty;
	public string ServerName { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
	public string AdminPassword { get; set; } = string.Empty;
	public string AuthenticationToken { get; set; } = string.Empty;
	public string SatisfactoryCertificateFingerprint { get; set; } = string.Empty;
	[JsonIgnore]
	public DateTime? SatisfactoryLastAttemptUtc { get; set; }
	[JsonIgnore]
	public DateTime? SatisfactoryLastSuccessUtc { get; set; }
	[JsonIgnore]
	public string SatisfactoryApiErrorKey { get; set; } = string.Empty;
	[JsonIgnore]
	internal Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory.SatisfactoryServerState? SatisfactoryState { get; set; }
	public string InviteCode { get; set; } = string.Empty;
	public string Status { get; set; } = StatusManager.GetStatus(ServerState.Stopped);
	public int MaxPlayers { get; set; } = 10;
	public string WorldName { get; set; } = "NewWorld";
	public bool IsDefaultPath { get; set; } = true;
	public int? PID { get; set; }
	public List<ServerProcessIdentity> ServerProcesses { get; set; } = [];
	public int? SteamPID { get; set; }
	[JsonIgnore]
	public Process? RunningProcess { get; set; }
	[JsonIgnore]
	public DateTime LastProcessDiscoveryUtc { get; set; } = DateTime.MinValue;
	public string GameMode { get; set; } = "PVE";
	public bool CrossplayEnabled { get; set; } = true;
	[JsonIgnore]
	public double LastCpuMillis { get; set; } = 0;
	[JsonIgnore]
	public DateTime LastSampleTime { get; set; } = DateTime.Now;
	public bool EnableRcon { get; set; } = false;
	public int RconPort { get; set; }
	public string RconPassword { get; set; } = "";
	public bool IsFirstBoot { get; set; } = true;
	[JsonIgnore]
	public string PlayerCount
	{
		get
		{
			if (IsSatisfactory(Game))
				return Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory.SatisfactoryIntegration.HasFreshPlayerCount(this)
					? $"{CurrentPlayers} / {MaxPlayersFromQuery}"
					: "N/A";
			return SupportsPlayerCountMonitoring(this)
				? $"{CurrentPlayers} / {MaxPlayers}"
				: "N/A";
		}
	}
	public bool UpdateOnStart { get; set; } = false;
	public bool BackupOnStart { get; set; } = false;
	public bool PreserveImportedConfiguration { get; set; } = false;
	public int ManagedConfigurationVersion { get; set; }
	public bool IsDiscordAlertEnabled { get; set; } = false;
	public string DiscordWebhook { get; set; } = string.Empty;
	public DiscordNotificationEvent DiscordEvents { get; set; } =
		DiscordNotificationEvent.All;
	public List<DiscordWebhookRoute> DiscordWebhookRoutes { get; set; } = [];
	public DateTime? StartTime { get; set; }
	public double RamUsage { get; set; }
	[JsonIgnore]
	public bool IsProbing { get; set; } = false;
	public string GameVersion { get; set; } = "Latest";
	public string MinecraftEdition { get; set; } = "Java";
	public string MinecraftLoader { get; set; } = "Vanilla";
	public string MinecraftLoaderVersion { get; set; } = "Official";
	public bool EnableMinecraftManagementProtocol { get; set; } = true;
	public int MinecraftManagementPort { get; set; }
	public string ServerFramework { get; set; } = "Vanilla";
	public string ServerFrameworkVersion { get; set; } = "Official";
	public int RequiredJavaVersion { get; set; } = 0;
	public int MaxRam { get; set; } = 4;

	[JsonIgnore]
	public string Uptime
	{
		get
		{
			if (Status != StatusManager.GetStatus(ServerState.Running) || !StartTime.HasValue)
				return "--:--:--";

			TimeSpan duration = DateTime.Now - StartTime.Value;

			if (duration.TotalDays >= 1)
				return LocalizationManager.Get(
					"Dashboard.Uptime.Days",
					(int)duration.TotalDays,
					duration.Hours,
					duration.Minutes);

			return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
		}
	}
}
