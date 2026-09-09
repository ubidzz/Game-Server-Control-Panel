# ============================================================================
# PROJECT: Synix Game Server Control Panel
# AUTHOR: Jason Turner (ubidzz)
# COPYRIGHT: © 2026 All Rights Reserved.
#
# LEGAL NOTICE:
# This source code is proprietary and confidential.
# 1. Permission is granted for PERSONAL, NON-COMMERCIAL use only.
# 2. You may modify this code for your own use, but you may NOT redistribute,
#    rebrand, or sell this code or derivative works without written consent.
# 3. The "Synix" brand and logic remain the property of Jason Turner.
# ============================================================================

param()

Add-Type -AssemblyName System.Windows.Forms

$semanticText = [ordered]@{
    'Language.English' = 'English'
    'Language.French' = 'French'
    'Language.German' = 'German'
    'Language.Spanish' = 'Spanish'
    'Option.DownloadSpeed.Unlimited' = 'Unlimited'
    'Option.DownloadSpeed.Limited' = 'Limited'
    'Message.AlreadyRunning.Body' = 'Synix is already running. Please use the existing Synix window.'
    'Message.AlreadyRunning.Title' = 'Synix Already Running'
    'Settings.VersionLabel' = 'SYNIX CONTROL PANEL  •  v{0}'
    'SettingsPage.General.Heading' = 'General'
    'SettingsPage.General.Subtitle' = 'Configure basic Synix behavior on this computer.'
    'SettingsPage.Backups.Heading' = 'Backups'
    'SettingsPage.Backups.Subtitle' = 'Manage server backups or move Synix to another computer.'
    'SettingsPage.Privacy.Heading' = 'Privacy & Security'
    'SettingsPage.Privacy.Subtitle' = 'Control how sensitive server information is displayed.'
    'SettingsPage.Advanced.Heading' = 'Advanced'
    'SettingsPage.Advanced.Subtitle' = 'Configure elevated operations and advanced system behavior.'
    'SettingsPage.ReportProblem.Heading' = 'Report a Problem'
    'SettingsPage.ReportProblem.Subtitle' = 'Create a privacy-filtered compatibility report for Synix support.'
    'SettingsPage.Development.Heading' = 'Development'
    'SettingsPage.Development.Subtitle' = 'Manage configuration capture and release testing tools.'
    'Menu.ModPluginManager' = 'Mod && Plugin Manager'
    'Menu.PlayerManagementCenter' = 'Player Management Center'
    'Menu.MinecraftServerConsole' = 'Minecraft Server Console'
    'Menu.ConnectionInformation' = 'Connection Information'
    'Menu.LiveProcessDetails' = 'Live Process Details'
    'Option.Status.All' = 'All Statuses'
    'Option.Status.Running' = 'Running'
    'Option.Status.Stopped' = 'Stopped'
    'Option.Status.InProgress' = 'In Progress'
    'Option.Status.NeedsAttention' = 'Needs Attention'
    'Option.Discord.AllEvents' = 'All events'
    'Option.Discord.ServerStatus' = 'Server status'
    'Option.Discord.Maintenance' = 'Maintenance'
    'Option.Discord.ProblemsOnly' = 'Problems only'
    'Option.Discord.Custom' = 'Custom'
    'Option.ConfigType.All' = 'All types'
    'Option.ConfigType.Text' = 'TEXT'
    'Option.ConfigType.Number' = 'NUMBER'
    'Option.ConfigType.Boolean' = 'BOOLEAN'
    'Option.ConfigType.Secret' = 'SECRET'
    'Option.ConfigType.Null' = 'NULL'
    'Option.VerificationFilter.NeedsWork' = 'Needs work'
    'Option.VerificationFilter.UnknownConfiguration' = 'Unknown configuration'
    'Option.VerificationFilter.PartiallyVerified' = 'Partially verified'
    'Option.VerificationFilter.FullyVerified' = 'Fully verified'
    'Option.VerificationFilter.AllGames' = 'All games'
    'VerificationStep.Install' = 'Install'
    'VerificationStep.Start' = 'Start'
    'VerificationStep.Stop' = 'Stop'
    'VerificationStep.Monitoring' = 'Monitoring'
    'VerificationStep.Arguments' = 'Arguments'
    'VerificationStep.Configuration' = 'Configuration'
    'Status.Stopped' = 'Stopped'
    'Status.Running' = 'Running'
    'Status.Starting' = 'Starting'
    'Status.Crashed' = 'Crashed'
    'Status.Stopping' = 'Stopping'
    'Status.Installing' = 'Installing'
    'Status.Updating' = 'Updating'
    'Status.BackingUp' = 'Backing Up'
    'Status.Validating' = 'Validating'
    'Status.Exporting' = 'Exporting'
    'Status.Restoring' = 'Restoring'
    'Status.Deleting' = 'Deleting'
    'Status.Unknown' = 'Unknown'
    'Dashboard.ServerCount.One' = '{0} server'
    'Dashboard.ServerCount.Many' = '{0} servers'
    'Dashboard.ServerCount.Filtered' = '{0} of {1} servers'
    'Dashboard.Network.PublicFetching' = 'Public IP: Fetching...'
    'Dashboard.Network.LocalFetching' = 'LAN IP: Fetching...'
    'Dashboard.Network.PublicAddress' = 'Public IP: {0}'
    'Dashboard.Network.LocalAddress' = 'LAN IP: {0}'
    'Dashboard.Network.PublicHidden' = 'Public IP: [HIDDEN]'
    'Dashboard.Network.LocalHidden' = 'LAN IP: [HIDDEN]'
    'Dashboard.CpuGaugeLabel' = 'CPU %'
    'Dashboard.RamGaugeLabel' = 'RAM GB'
    'Dashboard.CpuValue' = '{0:0.0}%'
    'Dashboard.RamValue' = '{0:0.00} GB'
    'ServerSetup.Status.Ready' = '●  Ready to save'
    'ServerSetup.Status.ActionRequired' = '●  Action required'
    'ServerSetup.Status.AllChecksPassed' = 'All required checks passed'
    'ServerSetup.Status.SeeValidationMessage' = 'See the exact validation message below'
    'ServerSetup.Completion' = 'Setup completion: {0}%'
    'ServerSetup.Window.Title' = 'Server Setup'
    'ServerSetup.Window.EditTitle' = 'Edit Server'
    'ServerSetup.ModeBadge.New' = 'NEW SERVER'
    'ServerSetup.ModeBadge.Edit' = 'EDIT SERVER'
    'ServerSetup.Button.SaveServer' = 'Save Server'
    'ServerSetup.Button.SaveChanges' = 'Save Changes'
    'ServerSetup.Page.General.Title' = 'General'
    'ServerSetup.Page.General.Description' = 'Choose the game and define the server identity.'
    'ServerSetup.Page.Security.Title' = 'Security'
    'ServerSetup.Page.Security.Description' = 'Manage server passwords and online-service credentials.'
    'ServerSetup.Page.World.Title' = 'World Generation'
    'ServerSetup.Page.World.Description' = 'Configure world seed, size, and game-specific world options.'
    'ServerSetup.Page.Network.Title' = 'Network & RCON'
    'ServerSetup.Page.Network.Description' = 'Assign service ports and secure remote administration.'
    'ServerSetup.Page.Network.BeginnerDescription' = 'Use the recommended game and query ports. Advanced mode adds RCON controls.'
    'ServerSetup.Page.Automation.Title' = 'Automation'
    'ServerSetup.Page.Automation.Description' = 'Control startup tasks, scheduled restarts, backups, and alerts.'
    'ServerSetup.Page.Discord.Title' = 'Discord Notifications'
    'ServerSetup.Page.Discord.Description' = 'Use one master webhook or route different Synix events to multiple Discord channels.'
    'ServerSetup.Page.Install.Title' = 'Install & Launch'
    'ServerSetup.Page.Install.Description' = 'Choose server storage and customize launch arguments.'
    'ServerSetup.Page.Install.BeginnerDescription' = 'Choose where the server will be installed. Synix supplies the recommended launch settings.'
    'ServerSetup.Mode.Advanced' = 'Mode: Advanced'
    'ServerSetup.Mode.Beginner' = 'Mode: Beginner'
    'ServerSetup.Mode.Advanced.AccessibleName' = 'Advanced server setup mode. Click to use Beginner mode.'
    'ServerSetup.Mode.Beginner.AccessibleName' = 'Beginner server setup mode. Click to show advanced settings.'
    'ServerSetup.Navigation.AttentionRequired' = '{0} contains settings that require attention before saving.'
    'ServerSetup.Navigation.NoAttentionRequired' = '{0} has no settings that require attention.'
    'ServerSetup.Validation.Waiting' = 'Validation is waiting for the required server information.'
    'ServerSetup.Validation.ServerNameAndGameRequired' = '  🔒 [REQUIRED] Enter a Server Name and select a Game Template.'
    'ServerSetup.Validation.ServerNameRequired' = '  🔒 [REQUIRED] Enter a Server Name before this server can be saved.'
    'ServerSetup.Validation.GameRequired' = '  🔒 [REQUIRED] Select a Game Template before this server can be saved.'
    'ServerSetup.Validation.MinecraftLoading' = '  ◌ [MINECRAFT] Loading compatible versions and Java requirements...'
    'ServerSetup.Validation.MinecraftDetail' = '  ⚠️ [MINECRAFT] {0}'
    'ServerSetup.Validation.MinecraftVersionRequired' = '  🔒 [MINECRAFT] Select a Minecraft game version.'
    'ServerSetup.Validation.AdminPasswordRequired' = '  🔒 [REQUIRED] Enter an Admin Password to protect the server administrator role.'
    'ServerSetup.Validation.AuthenticationTokenRequired' = '  🔒 [REQUIRED] Enter the required {0} before this server can be saved.'
    'ServerSetup.Validation.RequiredDetail' = '  🔒 [REQUIRED] {0}'
    'ServerSetup.Validation.MinecraftLoaderRequired' = '  🔒 [MINECRAFT] No compatible loader build is selected.'
    'ServerSetup.Validation.RequirementDetail' = '  ⚠️ [REQUIREMENT] {0}'
    'ServerSetup.Validation.NameConflict' = "  ⚠️ [CONFLICT] Name '{0}' is already used for {1}."
    'ServerSetup.Validation.ScheduleDayRequired' = '  🔒 [REQUIRED] Select at least one day for the automatic restart schedule.'
    'ServerSetup.Validation.InstallFolderRequired' = '  🔒 [REQUIRED] Select an install folder or enable the default install path.'
    'ServerSetup.Validation.LaunchDetail' = '  ⚠️ [LAUNCH] {0}'
    'ServerSetup.Validation.DiscordDetail' = '  🔒 [DISCORD] {0}'
    'ServerSetup.Validation.ReadyNote' = '  ✔ [READY] NOTE: {0}'
    'ServerSetup.Validation.Updating' = '  ✔ [READY] Updating: {0}'
    'ServerSetup.Validation.Ready' = '  ✔ [READY] Configuration is valid and safe.'
    'ServerSetup.Validation.Error' = '  ⚠️ [VALIDATION ERROR] Validation could not complete: {0}'
    'ServerSetup.Validation.DuplicatePort' = '  ⚠️ [CONFLICT] {0} cannot use the same port {1}.'
    'ServerSetup.Validation.PortBlocked' = '  ⚠️ [CONFLICT] {0} {1} is blocked by: {2}'
    'ServerSetup.ConfigurationSupport' = '◇  CONFIGURATION SUPPORT: {0}  •  {1}'
    'ServerSetup.PortMapping.SelectGame' = 'Select a game to see its managed port mappings.'
    'ServerSetup.PortMapping.AllMapped' = 'All declared ports are mapped by arguments or configuration.'
    'ServerSetup.PortMapping.NeedsMapping' = 'Needs mapping: {0} (arguments or configuration template).'
    'ServerSetup.Port.Game' = 'Game Port'
    'ServerSetup.Port.Query' = 'Query Port'
    'ServerSetup.Port.Ipv6' = 'IPv6 Port'
    'ServerSetup.Port.Rcon' = 'RCON Port'
    'ServerSetup.Port.App' = 'App Port'
    'ServerSetup.Port.SystemProcess' = 'System Process'
    'ServerSetup.List.AndSeparator' = ' and '
    'ServerSetup.Credentials.UnlockFailed.Title' = 'Re-enter Server Credentials'
    'ServerSetup.Credentials.UnlockFailed.Body' = "Synix could not unlock this server's saved passwords, authentication token, or Discord webhooks. They may have come from another Windows user or computer.`n`nEnter the credentials again and press Save Changes to protect them for this Windows user."
    'ServerSetup.Dialog.SettingsAttention.Title' = 'Server Settings Need Attention'
    'ServerSetup.Dialog.ExtraArgumentsBlocked.Title' = 'Extra Arguments Blocked'
    'ServerSetup.Dialog.DiscordAttention.Title' = 'Discord Settings Need Attention'
    'ServerSetup.Dialog.IllegalInput.Title' = 'Input Blocked'
    'ServerSetup.Dialog.IllegalInput.Body' = 'Security Alert: One of your inputs contains illegal characters.'
    'ServerSetup.ErrorAction.SaveMode' = 'save the setup mode'
    'ServerSetup.ErrorAction.SaveSettings' = 'save the server settings'
    'ServerSetup.ErrorAction.OpenTokenPage' = 'open the authentication-token page'
    'ServerSetup.GamePicker.Placeholder' = '-- Pick a Game --'
    'ServerSetup.Placeholder.SelectGame' = 'Select a game...'
    'ServerSetup.Placeholder.NotRequired' = 'Not Required'
    'ServerSetup.Security.AuthenticationToken' = 'Authentication Token'
    'ServerSetup.Network.RconToggle.AccessibleName' = 'Enable RCON'
    'ServerSetup.Automation.EnableSchedule.AccessibleName' = 'Activate Scheduler'
    'ServerSetup.Automation.UpdateOnStart.AccessibleName' = 'Update on Start'
    'ServerSetup.Automation.BackupOnStart.AccessibleName' = 'Backup on Start'
    'ServerSetup.Install.DefaultFolder.AccessibleName' = 'Default Folder'
    'ServerSetup.Verification.LastTested.Unverified' = 'Last-tested Synix version: Not verified yet'
    'ServerSetup.Verification.LastTested.Verified' = 'Last-tested Synix version: v{0}  •  {1:d}'
    'ServerSetup.Verification.Verified' = '{0}  ✓ Verified'
    'ServerSetup.Verification.Unverified' = '{0}  — Not verified yet'
    'ServerSetup.Minecraft.MetadataLoadFailed' = 'Metadata could not be loaded: {0}'
    'ServerSetup.Minecraft.MojangVersionsLoadFailed' = 'Mojang versions could not be loaded: {0}'
    'ServerSetup.Minecraft.LoadingBuilds' = 'Loading compatible builds...'
    'ServerSetup.Minecraft.Resolving' = 'Resolving...'
    'ServerSetup.Minecraft.Unavailable' = 'Unavailable'
    'ServerSetup.Minecraft.JavaVersion' = 'Java {0}'
    'ServerSetup.Minecraft.Helper.Vanilla' = 'Synix installs the official server and matching portable Java.'
    'ServerSetup.Minecraft.Helper.Loader' = 'Synix installs the compatible {0} server loader. Add your own mods after installation.'
    'ServerSetup.Minecraft.NoCompatibleBuild' = 'No compatible {0} server build exists for Minecraft {1}.'
    'ServerSetup.Minecraft.Helper.ResolvedVanilla' = 'Minecraft {0} uses the official Mojang server and Java {3}.'
    'ServerSetup.Minecraft.Helper.ResolvedLoader' = 'Minecraft {0} + {1} {2} uses Java {3}. Add mods after installation.'
    'ServerSetup.Minecraft.RetryDetail' = '{0} Re-select the version or loader to retry.'
    'ServerSetup.Minecraft.Helper.Unverified' = 'Synix could not verify this loader combination from the official metadata service.'
    'ServerSetup.Minecraft.Helper.Bedrock' = "Synix installs Microsoft's official Bedrock Dedicated Server. Java and Java mod loaders do not apply."
    'ServerSetup.Runtime.Minecraft.Title' = 'Minecraft Runtime'
    'ServerSetup.Runtime.ServerPackage' = 'Server Package'
    'ServerSetup.Runtime.Loader' = 'Loader'
    'ServerSetup.Runtime.OfficialBedrock' = 'Official Bedrock'
    'ServerSetup.Runtime.Framework.Title' = 'Server Framework'
    'ServerSetup.Runtime.Framework' = 'Framework'
    'ServerSetup.Runtime.Framework.Helper' = "Synix installs the official Oxide runtime only. Plugins remain user-managed in the server's oxide\plugins folder."
    'ServerSetup.MaxPlayers.Label' = 'Max Players'
    'ServerSetup.MaxPlayers.Limited' = 'Max Players (maximum {0:0})'
    'ServerSetup.SteamAccount.Restore.WindowTitle' = 'Restore Steam Authorization'
    'ServerSetup.SteamAccount.Required.WindowTitle' = 'Steam Account Required'
    'ServerSetup.SteamAccount.Restore.Title' = 'Restore Steam authorization'
    'ServerSetup.SteamAccount.Required.Title' = 'Steam account required'
    'ServerSetup.SteamAccount.Restore.Description' = '{0} was imported to this PC. Confirm the Steam account name so SteamCMD can restore access before the first start.'
    'ServerSetup.SteamAccount.Required.Description' = '{0} requires a Steam account for installation. Enter the account name that SteamCMD should use.'
    'ServerSetup.SteamAccount.Validation.InvalidName' = 'Enter a valid Steam account name.'
    'ProblemAction.ServerInstallation' = 'Server installation'
    'ProblemAction.UpdateValidation' = 'Server update or file validation'
    'ProblemAction.ServerStartup' = 'Server startup'
    'ProblemAction.ServerShutdown' = 'Server shutdown'
    'ProblemAction.RestartWatchdog' = 'Server restart or watchdog'
    'ProblemAction.IncorrectStatus' = 'Incorrect server status'
    'ProblemAction.ResourceMonitoring' = 'CPU, memory, or player monitoring'
    'ProblemAction.LocalNetwork' = 'Local network connection'
    'ProblemAction.PublicNetwork' = 'Internet or public connection'
    'ProblemAction.PortsFirewallRcon' = 'Ports, firewall, or RCON'
    'ProblemAction.ServerBackups' = 'Server backups'
    'ProblemAction.TransferExport' = 'Transfer export'
    'ProblemAction.TransferImport' = 'Transfer import'
    'ProblemAction.TransferVerification' = 'Transfer package verification'
    'ProblemAction.SettingsPasswords' = 'Server settings or passwords'
    'ProblemAction.DiscordAlerts' = 'Discord alerts'
    'ProblemAction.SynixUpdate' = 'Synix update'
    'ProblemAction.InstallationPackaging' = 'MSI, WinGet, or standalone installation'
    'ProblemAction.WindowDisplay' = 'Window or display problem'
    'ProblemAction.CrashFreeze' = 'Synix crash or freeze'
    'ProblemAction.TemplateLaunch' = 'Server template or launch behavior'
    'ProblemAction.Other' = 'Other'
    'Report.EnglishRequiredWarning' = 'Important: Write the summary and report details in English so the Synix support team can review them.'
    'Advanced.Firewall.ButtonChecking' = 'Checking Firewall...'
    'Advanced.Firewall.CheckingPaths' = 'Checking Windows Firewall program paths...'
    'Advanced.Firewall.Canceled' = 'Cleanup canceled. No firewall rules were changed.'
    'Advanced.Firewall.WaitingForAdmin' = 'Waiting for administrator permission...'
    'Advanced.Firewall.RemovedVerified' = 'Removed and verified {0} orphaned executable path(s).'
    'Advanced.Firewall.NoneFound' = 'No orphaned firewall rules were found in the default Synix Games folder.'
    'Advanced.Background.EnabledCurrent' = 'Enabled for Windows sign-in — Close still exits Synix completely.'
    'Advanced.Background.DisabledCurrent' = 'Disabled — scheduled work runs only while Synix is open.'
    'Advanced.Background.EnabledResult' = 'Enabled for Windows sign-in. Closing Synix still exits every Synix process for the current session.'
    'Advanced.Background.DisabledResult' = 'Disabled. Background monitoring will stop and will not start at sign-in.'
    'AddServer.Title' = 'Add a Server'
    'AddServer.Heading' = 'How would you like to add a server?'
    'AddServer.Subtitle' = 'Synix can install a new server or safely register files that are already on this PC.'
    'AddServer.Create.Title' = 'Create and install a new server'
    'AddServer.Create.Description' = 'Choose the game and settings, then let Synix download the server files.'
    'AddServer.Create.Button' = 'Create New'
    'AddServer.Import.Title' = 'Import an existing server'
    'AddServer.Import.Description' = 'Point Synix to an existing server folder. Your files are not moved or replaced.'
    'AddServer.Import.Button' = 'Import Existing'
    'AddServer.Catalog.Title' = 'Check game support first'
    'AddServer.Catalog.Description' = 'Search the catalog to see executable, configuration, crossplay, and player-query support.'
    'AddServer.Catalog.Button' = 'View Catalog'
    'Connection.Heading' = 'Connect to {0}'
    'Connection.Subtitle' = 'Use the address that matches where the player is connecting from.'
    'Connection.Local.Title' = 'Same computer or home network'
    'Connection.Local.Description' = 'Use this for players connected to the same router.'
    'Connection.Public.Title' = 'Friends connecting over the internet'
    'Connection.Public.Description' = 'Your router and Windows Firewall must allow the game and query ports.'
    'Connection.Public.BedrockDescription' = 'Your router and Windows Firewall must allow Bedrock''s UDP game port.'
    'Connection.Ports.StandardSummary' = 'Configured ports: {0}. Some games appear in a server browser only when the query port is also forwarded.'
    'Connection.Ports.BedrockSummary' = 'Bedrock game port: {0}/UDP. IPv6 port: {1}/UDP. Each Bedrock server needs its own pair of ports.'
    'Connection.Port.Game' = 'game {0}'
    'Connection.Port.Query' = 'query {0}'
    'Connection.Port.Rcon' = 'RCON {0}'
    'Connection.Port.App' = 'app {0}'
    'Connection.Address.Hidden' = 'Hidden by Privacy Mode'
    'Connection.Address.PublicUnavailable' = 'Public address could not be loaded'
    'Connection.Address.Unavailable' = 'Address could not be loaded'
    'PlayerCenter.Summary.One' = '{0} • {1} • 1 named player'
    'PlayerCenter.Summary.Many' = '{0} • {1} • {2} named players'
    'PlayerCenter.Loading' = 'Loading player details…'
    'PlayerCenter.Guidance.Minecraft' = ' Select a player to use Minecraft''s local administration commands.'
    'PlayerCenter.Guidance.UnsupportedActions' = ' Player actions remain disabled unless a game provides a verified administration protocol.'
    'PlayerCenter.Action.Kick' = 'Kick'
    'PlayerCenter.Action.Allowlist' = 'Add to Allowlist'
    'PlayerCenter.Action.Operator' = 'Make Operator'
    'PlayerCenter.SelectValidPlayer' = 'Select a valid Minecraft player first.'
    'PlayerCenter.Confirm.Title' = 'Confirm Minecraft Player Action'
    'PlayerCenter.Confirm.Kick' = 'Do you want to kick this player: {0}?'
    'PlayerCenter.Confirm.Allowlist' = 'Do you want to add this player to the allowlist: {0}?'
    'PlayerCenter.Confirm.Operator' = 'Do you want to make this player an operator: {0}?'
    'PlayerQuery.GameDefinitionUnavailable' = 'The game definition is unavailable.'
    'PlayerQuery.CrossplayUnavailable' = 'Player tracking is unavailable while Crossplay is enabled. Disable Crossplay to use Steam A2S player tracking.'
    'PlayerQuery.ProtocolUnavailable' = 'This game''s current query protocol does not provide a safe, universal player-name list.'
    'PlayerQuery.MinecraftCountOnly' = 'Minecraft reports {0} connected player(s), but this server query does not publish player names.'
    'PlayerQuery.StartServerFirst' = 'Start the server before refreshing player details.'
    'PlayerQuery.InvalidA2sResponse' = 'The server returned an invalid A2S player response.'
    'PlayerQuery.IncompatiblePlayerList' = 'The server query works, but it did not provide a compatible player list.'
    'PlayerQuery.NoNamedPlayers' = 'The server responded and no named players are connected.'
    'PlayerQuery.LoadedPlayers' = 'Loaded {0} connected player(s).'
    'PlayerQuery.Timeout' = 'The player query on UDP port {0} timed out.'
    'PlayerQuery.ConnectionFailed' = 'The player query could not connect: {0}'
    'PlayerQuery.ReadFailed' = 'Player details could not be read: {0}'
    'PlayerQuery.BedrockCountOnly' = 'Minecraft Bedrock reports {0} connected player(s), but its built-in status response does not publish player names.'
    'PlayerQuery.MinecraftManagement.None' = 'Minecraft''s local management service reports no connected players.'
    'PlayerQuery.MinecraftManagement.Loaded' = 'Loaded {0} player(s) through Minecraft''s local management service.'
    'PlayerQuery.MinecraftRcon.None' = 'Minecraft RCON reports no connected players.'
    'PlayerQuery.MinecraftRcon.Loaded' = 'Loaded {0} player(s) through local Minecraft RCON.'
    'PlayerQuery.MinecraftUnavailable' = 'Minecraft player details are not available yet.'
    'PlayerQuery.UnnamedPlayer' = 'Unnamed player'
    'ModManager.Subtitle' = 'Discover what is already installed, safely add local packages, and keep a rollback record without maintaining a list of every mod.'
    'ModManager.Field.Server' = 'SERVER'
    'ModManager.Field.System' = 'ADD-ON SYSTEM'
    'ModManager.Field.InstallArea' = 'INSTALL AREA'
    'ModManager.Support.Checking' = 'Checking support…'
    'ModManager.Step.Detect' = '1  Detect'
    'ModManager.Step.Stop' = '2  Stop server'
    'ModManager.Step.Backup' = '3  Back up files'
    'ModManager.Step.Install' = '4  Install'
    'ModManager.Step.Verify' = '5  Verify'
    'ModManager.Step.Restart' = '6  Restart if needed'
    'ModManager.Column.AddOn' = 'ADD-ON'
    'ModManager.Column.Type' = 'TYPE'
    'ModManager.Column.Version' = 'VERSION'
    'ModManager.Column.Status' = 'STATUS'
    'ModManager.Column.Security' = 'SECURITY'
    'ModManager.Column.Source' = 'SOURCE'
    'ModManager.Column.Location' = 'LOCATION'
    'ModManager.Safety.Title' = 'Automatic Safety Checklist'
    'ModManager.Safety.Subtitle' = 'Synix checks these before it changes anything.'
    'ModManager.Selection.Empty' = 'Select an add-on to see where it was found.'
    'ModManager.Button.InstallFile' = 'Install From File'
    'ModManager.Button.InstallFramework' = 'Install Framework'
    'ModManager.Button.BrowseCatalog' = 'Browse Catalog'
    'ModManager.Button.BrowseCatalogs' = 'Browse Catalogs'
    'ModManager.Button.OpenFolder' = 'Open Add-ons Folder'
    'ModManager.Button.Refresh' = 'Refresh'
    'ModManager.Button.Remove' = 'Remove Selected'
    'ModManager.Button.Close' = 'Close'
    'ModManager.Button.ManageIds' = 'Manage Mod IDs'
    'ModManager.Inventory.Empty' = 'No add-ons were found in the active profile folders.'
    'ModManager.Inventory.One' = '1 add-on found  •  {1} tracked by Synix'
    'ModManager.Inventory.Many' = '{0} add-ons found  •  {1} tracked by Synix'
    'ModManager.Inventory.RefreshFailed' = 'Synix could not refresh the add-on folders.'
    'ModManager.Support.ProviderIds' = 'READY • Synix manages the provider''s ordered mod ID list'
    'ModManager.Support.FileImport' = 'READY • Synix can safely import local add-on files'
    'ModManager.Support.SetupNeeded' = 'SETUP NEEDED • Select or install a compatible framework first'
    'ModManager.Support.DetectionOnly' = 'DETECTION ONLY • The game provider remains responsible for installation'
    'ModManager.Framework.Automatic' = 'The server loader and existing folders choose the install area automatically.'
    'ModManager.Framework.Named' = 'Framework: {0}.'
    'ModManager.Unsupported.Title' = 'NO ADD-ON PROFILE YET'
    'ModManager.Unsupported.Description' = 'Synix will not guess where this game stores mods. A small data profile can add support later without rewriting this window.'
    'ModManager.NoFilesChanged' = 'No files were changed.'
    'ModManager.Safety.ServerStopped' = 'Server is stopped'
    'ModManager.Safety.StopFirst' = 'Stop the server before changes'
    'ModManager.Safety.FrameworkDetected' = 'Framework detected'
    'ModManager.Safety.FrameworkRequired' = 'Framework setup required'
    'ModManager.Safety.FolderAvailable' = 'Server folder available'
    'ModManager.Safety.FolderMissing' = 'Server folder missing'
    'ModManager.Safety.ProviderTrust' = 'Provider download needs manual trust'
    'ModManager.Safety.SecurityScan' = 'Security scan runs before install'
    'ModManager.Safety.StandardPermissions' = 'Standard Windows permissions'
    'ModManager.Safety.RestartWithoutAdmin' = 'Restart without administrator access'
    'ModManager.Safety.RestartRequired' = 'Restart required after changes'
    'ModManager.Safety.LiveReload' = 'Framework supports live reload'
    'ModManager.Profile.Rust.Description' = 'Rust plugins loaded by the Oxide/uMod framework.'
    'ModManager.Profile.Rust.Target' = 'Oxide plugins'
    'ModManager.Profile.Minecraft.Name' = 'Minecraft add-ons'
    'ModManager.Profile.Minecraft.Description' = 'JAR plugins or mods selected from the server loader and folders already on disk.'
    'ModManager.Profile.Minecraft.ModsTarget' = 'Loader mods'
    'ModManager.Profile.Minecraft.PluginsTarget' = 'Server plugins'
    'ModManager.Profile.SevenDays.Name' = '7 Days to Die server mods'
    'ModManager.Profile.SevenDays.Description' = 'Synix installs complete mod ZIP packages into the dedicated server''s Mods folder. Mods with client assets may also need to be installed by every player.'
    'ModManager.Profile.SevenDays.Target' = 'Server Mods folder'
    'ModManager.Profile.ArkEvolved.Name' = 'Steam Workshop mods'
    'ModManager.Profile.ArkEvolved.Description' = 'Synix manages the ordered Steam Workshop IDs; ARK and Steam download and update the actual content.'
    'ModManager.Profile.ArkEvolved.Target' = 'Ordered Steam Workshop IDs'
    'ModManager.Profile.ArkAscended.Name' = 'CurseForge server mods'
    'ModManager.Profile.ArkAscended.Description' = 'Synix manages the ordered mod ID list; ARK downloads and updates the actual CurseForge content when the server starts.'
    'ModManager.Profile.ArkAscended.Target' = 'Ordered CurseForge mod IDs'
    'ModManager.Profile.Discovered.Name' = 'Discovered add-on folders'
    'ModManager.Profile.Discovered.Description' = 'Synix found common add-on folders and can inventory them safely. Installation stays disabled until a maintainer adds a verified data profile.'
    'ModManager.Known.Mod' = 'Mod'
    'ModManager.Known.Plugin' = 'Plugin'
    'ModManager.Known.ModId' = 'Mod ID'
    'ModManager.Known.ProviderManaged' = 'Provider managed'
    'ModManager.Known.ConfiguredNextStart' = 'Configured for next start'
    'ModManager.Known.ProviderNotScanned' = 'Provider download not pre-scanned'
    'ModManager.Known.GameProvider' = 'Game provider'
    'ModManager.Known.Detected' = 'Detected on disk'
    'ModManager.Known.Healthy' = 'Healthy'
    'ModManager.Known.Changed' = 'Changed outside Synix'
    'ModManager.Known.NotReviewed' = 'Not reviewed by Synix'
    'ModManager.Known.LegacyNotReviewed' = 'Legacy install • not reviewed'
    'ModManager.Known.StructuralOnly' = 'Structural checks only'
    'ModManager.Known.ReviewRecorded' = 'Pre-install review recorded'
    'ModManager.Known.External' = 'External'
    'ModManager.Known.ExternalProvider' = 'External provider'
    'ModManager.Known.SynixImport' = 'Synix import'
    'ModManager.Known.LocalPackage' = 'Local package'
    'ModManager.Known.BuiltInLoader' = 'Built-in mod loader'
    'ModManager.Known.ArkBuiltInInstaller' = 'ARK built-in mod installer'
    'ResourceMonitor.WindowTitleFiltered' = 'Live Process Details - {0}'
    'ResourceMonitor.GridTitleFiltered' = 'Live Process Details  •  {0}'
    'ResourceMonitor.FilteredSubtitle' = 'Every launcher, console host, and game process Synix has verified inside this server group.'
    'ResourceMonitor.RowRunning' = '●  Running'
    'ResourceMonitor.CpuCaption' = 'Across all managed server processes'
    'ResourceMonitor.RamValue' = '{0:N2} GB'
    'ResourceMonitor.RamCaption' = '{0:N1}% of {1:N1} GB system memory'
    'ResourceMonitor.Active.None' = 'No running server processes detected'
    'ResourceMonitor.Active.One' = '1 server process is currently online'
    'ResourceMonitor.Active.Many' = '{0} server processes are currently online'
    'ResourceMonitor.ProcessCount.One' = '1 running process'
    'ResourceMonitor.ProcessCount.Many' = '{0} running processes'
    'ResourceMonitor.LastUpdated' = 'Updated {0:T}  •  Auto-refresh every 1 second'
    'ResourceMonitor.Empty' = 'No running game servers detected'
}

$additionalSemanticText = & (Join-Path $PSScriptRoot 'SemanticResources.en.ps1')
foreach ($entry in $additionalSemanticText.GetEnumerator()) {
    if ($semanticText.Contains($entry.Key)) {
        throw "Duplicate English semantic resource key: $($entry.Key)"
    }

    $semanticText[$entry.Key] = [string]$entry.Value
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceFiles = Get-ChildItem -Path $projectRoot -Recurse -Filter '*.cs' -File |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|Tests)[\\/]'
    }

function Get-CSharpLiteralText([string] $Block) {
    foreach ($match in [regex]::Matches(
        $Block,
        '(?<prefix>\$?@?)"(?<value>(?:\\.|[^"\\])*)"')) {
        $value = if ($match.Groups['prefix'].Value.Contains('@')) {
            $match.Groups['value'].Value.Replace('""', '"')
        }
        else {
            [regex]::Unescape($match.Groups['value'].Value)
        }

        if ($match.Groups['prefix'].Value.Contains('$')) {
            foreach ($fragment in [regex]::Split($value, '\{[^{}]*\}')) {
                if ([regex]::IsMatch($fragment, '\p{L}')) {
                    $fragment
                }
            }
        }
        elseif ([regex]::IsMatch($value, '\p{L}')) {
            $value
        }
    }
}

$visibleText = foreach ($file in $sourceFiles |
    Where-Object { $_.Name -notlike 'HelpGUI*.cs' }) {
    $content = Get-Content -Raw -Path $file.FullName
    foreach ($match in [regex]::Matches(
        $content,
        '(?<![A-Za-z0-9_])(?:Text|AccessibleName|HeaderText|PlaceholderText)\s*=\s*"((?:\\.|[^"\\])*)"\s*[,;]')) {
        [regex]::Unescape($match.Groups[1].Value)
    }
}

$visibleText = @($visibleText |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Sort-Object -Unique -CaseSensitive)

# Runtime labels often combine an English phrase with a server name, count,
# version, or exception detail. Store the literal portions separately so the
# application can translate the interface phrase without modifying the data.
$runtimeText = foreach ($file in $sourceFiles |
    Where-Object { $_.Name -ne 'HelpGUI.cs' -and $_.Name -notlike '*.Designer.cs' }) {
    $content = Get-Content -Raw -Path $file.FullName
    foreach ($match in [regex]::Matches(
        $content,
        '(?s)(?<![A-Za-z0-9_])(?:[A-Za-z_][A-Za-z0-9_]*\.)?Text\s*=\s*(?<expression>.*?);')) {
        Get-CSharpLiteralText $match.Groups['expression'].Value
    }
    foreach ($match in [regex]::Matches(
        $content,
        '(?s)(?<![A-Za-z0-9_])Text\s*=\s*(?<expression>\$?"(?:\\.|[^"\\])*")\s*,')) {
        Get-CSharpLiteralText $match.Groups['expression'].Value
    }
}
$runtimeText = @($runtimeText |
    Where-Object {
        -not [string]::IsNullOrWhiteSpace($_) -and
        $_.Length -ge 2 -and
        $_ -notin $visibleText -and
        $_ -notmatch '^(?:(?:Text|DynamicText|MessageText)\.[A-F0-9]{20}|[A-Z][A-Za-z0-9]+(?:\.[A-Za-z0-9]+)+)$' -and
        $_ -notin @(
            'Cascadia Mono',
            'Segoe UI',
            'Segoe UI Semibold',
            'Consolas',
            'latest',
            'Rust') -and
        $_ -notmatch '\{(?:DisplayOrFallback|\(passed \?)' -and
        $_ -notmatch '^\)\}'
    } |
    Sort-Object -Unique -CaseSensitive)

# Message boxes are localized separately from logs and support reports. Literal
# fragments are safe to replace because variable values are never cataloged.
$messageText = foreach ($file in $sourceFiles |
    Where-Object { $_.Name -ne 'HelpGUI.cs' }) {
    $content = Get-Content -Raw -Path $file.FullName
    foreach ($match in [regex]::Matches(
        $content,
        '(?s)(?:System\.Windows\.Forms\.)?(?:MessageBox|LocalizedMessageBox)\.Show\s*\((?<expression>.*?)\)\s*;')) {
        Get-CSharpLiteralText $match.Groups['expression'].Value
    }
}
$messageText = @($messageText |
    Where-Object {
        -not [string]::IsNullOrWhiteSpace($_) -and
        $_.Length -ge 2 -and
        $_ -notin $visibleText -and
        $_ -notin $runtimeText -and
        $_ -notmatch '^(?:[A-Z][A-Za-z0-9]+)(?:\.[A-Za-z0-9]+)+$'
    } |
    Sort-Object -Unique -CaseSensitive)

$targetPath = Join-Path $PSScriptRoot 'Strings.resx'

# Keep hash-based resources that are now referenced by key instead of repeated as
# English literals in runtime code. This lets the localization cleanup remove
# presentation text from C# without making a later resource regeneration discard
# translations that are still in use.
if (Test-Path -LiteralPath $targetPath) {
    $existingResources = @{}
    $reader = [System.Resources.ResXResourceReader]::new($targetPath)
    try {
        foreach ($entry in $reader) {
            $existingResources[[string]$entry.Key] = [string]$entry.Value
        }
    }
    finally {
        $reader.Close()
    }

    $referencedHashKeys = foreach ($file in $sourceFiles) {
        $content = Get-Content -Raw -Path $file.FullName
        foreach ($match in [regex]::Matches(
            $content,
            '(?<key>(?:Text|DynamicText|MessageText)\.[A-F0-9]{20})')) {
            $match.Groups['key'].Value
        }
    }

    foreach ($key in @($referencedHashKeys | Sort-Object -Unique)) {
        if (-not $existingResources.ContainsKey($key)) {
            continue
        }

        $value = $existingResources[$key]
        if ($key.StartsWith('Text.', [StringComparison]::Ordinal)) {
            $visibleText += $value
        }
        elseif ($key.StartsWith('DynamicText.', [StringComparison]::Ordinal)) {
            $runtimeText += $value
        }
        else {
            $messageText += $value
        }
    }

    $visibleText = @($visibleText | Sort-Object -Unique -CaseSensitive)
    $runtimeText = @($runtimeText | Sort-Object -Unique -CaseSensitive)
    $messageText = @($messageText | Sort-Object -Unique -CaseSensitive)
}

# Runtime fragments are used to translate status and diagnostic text assembled
# outside the UI layer. They do not have direct C# resource-key references, so
# keep their English source values explicitly instead of allowing regeneration
# to discard translations that are still used by TranslateRuntimeText or
# TranslateMessageText.
$operationalEnglish = & (Join-Path $PSScriptRoot 'OperationalTranslations.en.ps1')
foreach ($entry in $operationalEnglish.GetEnumerator()) {
    $value = [string]$entry.Value
    $prefix = if ($entry.Key.StartsWith('DynamicText.', [StringComparison]::Ordinal)) {
        'DynamicText.'
    }
    elseif ($entry.Key.StartsWith('MessageText.', [StringComparison]::Ordinal)) {
        'MessageText.'
    }
    else {
        throw "Operational English key '$($entry.Key)' must use DynamicText. or MessageText."
    }

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    $expectedKey = $prefix + [Convert]::ToHexString($hash).Substring(0, 20)
    if ($entry.Key -ne $expectedKey) {
        throw "Operational English key '$($entry.Key)' does not match its source text hash '$expectedKey'."
    }

    if ($prefix -eq 'DynamicText.') {
        $runtimeText += $value
    }
    else {
        $messageText += $value
    }
}
$runtimeText = @($runtimeText | Sort-Object -Unique -CaseSensitive)
$messageText = @($messageText | Sort-Object -Unique -CaseSensitive)

$writer = [System.Resources.ResXResourceWriter]::new($targetPath)
try {
    foreach ($entry in $semanticText.GetEnumerator()) {
        $writer.AddResource($entry.Key, [string]$entry.Value)
    }

    foreach ($value in $visibleText) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
        $key = 'Text.' + [Convert]::ToHexString($hash).Substring(0, 20)
        $writer.AddResource($key, [string]$value)
    }

    foreach ($value in $runtimeText) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
        $key = 'DynamicText.' + [Convert]::ToHexString($hash).Substring(0, 20)
        $writer.AddResource($key, [string]$value)
    }

    foreach ($value in $messageText) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
        $key = 'MessageText.' + [Convert]::ToHexString($hash).Substring(0, 20)
        $writer.AddResource($key, [string]$value)
    }
}
finally {
    $writer.Close()
}

Write-Host "Created Strings.resx with $($visibleText.Count) static texts, $($runtimeText.Count) runtime fragments, $($messageText.Count) message fragments, and $($semanticText.Count) semantic texts."
