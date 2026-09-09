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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Synix_Control_Panel.SynixApp.UI.Help
{
	public partial class HelpGUI : Form
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
		private const int EmSetCueBanner = 0x1501;
		private readonly ModernSettingsButton btnDonateAction = new();

		private static readonly (
			string Key,
			string DisplayName,
			string Index)[] CategoryDefinitions =
		[
			("Start", "Getting Started & Setup", "01"),
			("Dash", "Dashboard & Settings", "02"),
			("Config", "Server Configuration", "03"),
			("Net", "Networking & Connections", "04"),
			("Maint", "Backups & Maintenance", "05"),
			("Watch", "Monitoring & Safeguards", "06"),
			("Trouble", "Troubleshooting & System", "07"),
			("Games", "Game Guides", "08"),
			("Support", "Support, License & Donate", "09")
		];

		private Dictionary<string, HelpItem> _helpData =
			new(StringComparer.OrdinalIgnoreCase);
		private int _visibleArticleCount;

		public HelpGUI()
		{
			InitializeComponent();

			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
			{
				return;
			}
			ThemeManager.Apply(this);

			InitializeData();
			PopulateTree();
			ShowWelcome();

			_ = SendMessageText(
				txtSearch.Handle,
				EmSetCueBanner,
				IntPtr.Zero,
				"Search topics, guides, or answers...");

			btnDonateAction.Text = "Open PayPal Donation";
			btnDonateAction.UseAccentStyle = true;
			btnDonateAction.Size = new Size(176, 38);

			btnDonateAction.Location = new Point(20, 304);
			btnDonateAction.Click += (s, e) =>
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://www.paypal.com/donate/?hosted_button_id=FAHU6EH6BX9J8",
					UseShellExecute = true
				});
			};
			qrCard.Controls.Add(btnDonateAction);
		}

		private void InitializeData()
		{
			_helpData = CreateHelpArticles();
		}

		internal const string WelcomeText = """
			Choose a topic on the left to read its instructions. All help articles work offline and remain in English.

			NEW TO SYNIX?
			Open First-Time Setup Guide in Getting Started & Setup.

			MANAGING A SERVER?
			Choose the category for the task, such as configuration, networking, backups, or a game-specific control.

			LOOKING FOR AN ERROR?
			Press Ctrl+F and search for a feature, game, or short part of the message. Search checks article titles and full text and brings matching text into view.

			Use the clear-search button to return to all topics. Download and support links open in your browser only when you choose them.
			""";

		// Author one task-focused guide per topic; do not append external documentation.
		// Keep help offline, in English, and independent of Synix release numbers.
		// Game syntax stays exact. Only actionable download/support links belong here.
		// Build without a form so content and navigation can be tested together.
		internal static Dictionary<string, HelpItem> CreateHelpArticles()
		{
			Dictionary<string, HelpItem> articles = new(StringComparer.OrdinalIgnoreCase)
			{
				["First-Time Setup Guide"] = CreateArticle(
					"Start",
					"""
					Use this guide to create your first server. Synix runs it on this PC, so leave the PC on while people are playing. You do not need to write a launch command for a normal supported setup.

					1. Open Synix and let the SteamCMD dependency check finish.
					2. Choose Add Server, then create a new server. If the server files already exist, choose Import Existing Server instead.
					3. On General, enter a unique server name and select the game. Choose the edition, game version, or loader if those choices appear.
					4. Keep the suggested values unless you need something different. Review the enabled setup pages and resolve the exact required-setting message at the bottom.
					5. Choose Save Server. This adds an entry to the dashboard; it does not mean the game has finished installing.
					6. Select that entry and choose Start. Allow the download and first-start preparation to finish, following any prompts Synix shows.
					7. Complete any setup owned by the game, such as an agreement, account sign-in, or creating a session in its in-game server manager.
					8. Join from this PC or another device on your home network. Once that works, create a backup before adding mods or making major changes.

					IF YOU GET STUCK
					Open Readiness for the selected server and read Activity & Diagnostics. Follow the specific error rather than repeatedly clicking Start. For Minecraft or Satisfactory, the Game Guides category explains the additional steps.

					Hosting for people outside your network is a separate networking step. First confirm that the server works locally.
					"""),

				["Installing and Updating Synix"] = CreateArticle(
					"Start",
					"""
					Use an official release package. It includes the .NET runtime needed by Synix; game servers may still need their own prerequisites.

					CHOOSE AN INSTALLATION METHOD
					• MSI: Download SynixSetup.msi, run it, then open Synix Control Panel from the Start menu. The normal per-user application location is %LocalAppData%\Programs\Synix.
					• WinGet: Use the exact package ubidzz.Synix. The WinGet Installation and Updates article contains the commands.
					• Standalone executable: Place the official standalone download in a writable folder and run it there. Moving that executable is not a complete transfer of your servers.

					Official downloads:
					https://github.com/ubidzz/Synix-Control-Panel/releases/latest

					UPDATE THE APPLICATION
					1. Finish active downloads, backups, and maintenance.
					2. Stop your game servers through Synix and close Synix normally.
					3. Use the updater or installer for your chosen installation method.
					4. Reopen Synix and check that your server entries and paths are correct.

					If replacing an older EXE-based installation with the MSI, uninstall the older application first. Keep its server data and custom server folders. Do not delete C:\Synix to update the program.

					An application update does not update the games you host. Use the selected server's Update action for that.

					If Windows reports access denied while replacing the executable, check that the copy being replaced is no longer running. Do not overwrite a running application or remove server data to work around the error.
					"""),

				["WinGet Installation and Updates"] = CreateArticle(
					"Start",
					"""
					Open Windows Terminal or PowerShell and use the exact package identifier below. These commands manage Synix itself, not individual game servers.

					INSTALL
					winget install --exact --id ubidzz.Synix

					CHECK THE INSTALLED PACKAGE
					winget list --exact --id ubidzz.Synix

					UPDATE
					Close Synix and finish its running work before using:
					winget upgrade --exact --id ubidzz.Synix

					UNINSTALL THE APPLICATION
					winget uninstall --exact --id ubidzz.Synix

					KEEP YOUR DATA
					Server installations, saves, and backups are separate from the application package. Do not delete C:\Synix or any custom data folders unless you intentionally want to remove that data.

					IF A COMMAND FAILS
					Read the reported reason. If WinGet reports an installer hash mismatch, stop and report it; do not bypass hash verification. A missing update can mean that the repository has not published a newer package yet.

					WinGet approval and a matching download hash do not code-sign Synix or guarantee that Windows security software will trust it.
					"""),

				["Importing Existing Servers"] = CreateArticle(
					"Start",
					"""
					Import Existing Server registers a supported installation that is already on disk. It is not a reinstall and is different from importing a complete Synix transfer package.

					BEFORE YOU IMPORT
					Make a separate backup, including any saves outside the installation folder. Stop the server and disable another manager or scheduler that could start it while Synix is managing it.

					REGISTER THE INSTALLATION
					1. Choose Add Server → Import Existing Server.
					2. Select the actual server installation folder, not a shortcut, ZIP file, or game-client folder.
					3. Let Synix inspect it, then confirm the detected game, edition, executable, and settings.
					4. Give the entry a unique name and complete any missing values.
					5. Finish the import, open Configure, and review its paths and ports.
					6. Run Readiness, then perform one controlled start. Check that the intended world and configuration were loaded.

					WHAT IMPORT CHANGES
					Registration does not move the installation, redownload it, or replace its configuration files. Later actions you explicitly choose, such as Update, Validate, or configuration repair, can change files.

					Custom launch wrappers, modpacks, passwords, and external saves may not be detected automatically. The same installation path cannot be registered twice.

					IF DETECTION FAILS
					Check that you selected a dedicated-server installation supported by the chosen profile. Do not rename an unrelated executable to force a match. Report the game and a redacted folder layout so its definition can be checked.
					"""),

				["Server Names and Folders"] = CreateArticle(
					"Start",
					"""
					Each dashboard entry needs its own name and installation folder. Synix turns the server name into a Windows-safe identity for supported folder and launch settings. This keeps separate instances from unintentionally sharing files; it is not a virtual machine or security boundary.

					DEFAULT LOCATIONS
					• C:\Synix — main Synix data workspace.
					• C:\Synix\SteamCMD — shared game downloader.
					• C:\Synix\Games\<game>\<server> — normal server installations.
					• C:\Synix\BackupGames — normal backup root.
					• C:\Synix\SynixData\logs — application logs and crash reports.

					The MSI application files are normally under %LocalAppData%\Programs\Synix. Some preferences and Windows-protected values also belong to your Windows account.

					CUSTOM LOCATIONS
					Use Install & Launch when choosing a server folder. For a new installation, choose an empty writable folder with enough space for the game and updates. Do not point two profiles at the same directory.

					Open Server Folder shows the selected server's actual location. Open Backup Folder shows its backup location; do not guess either path from its display name.

					SAVES CAN LIVE ELSEWHERE
					A game may store worlds in AppData, Documents, another drive, or its own deployment system. Confirm the game's real save location before backing up or moving it.

					Renaming or moving folders in File Explorer does not guarantee that every saved path will update. Use Synix's supported import/transfer workflow and review paths before the next start. Separate instances also need nonconflicting network ports.
					"""),

				["Uninstalling Synix Safely"] = CreateArticle(
					"Start",
					"""
					Decide whether you are removing only the control panel or also permanently deleting your hosted servers. Those are different actions.

					REMOVE ONLY THE APPLICATION
					1. Finish any installation, transfer, or backup that is running.
					2. Stop your servers through Synix, wait for them to finish saving, and exit Synix.
					3. Keep a verified copy of important worlds, configuration files, and credentials.
					4. Uninstall Synix through Windows Installed apps or your package manager.

					Keep C:\Synix, custom server folders, external game saves, and backup locations if you intend to use them again. Reinstalling the application does not require deleting these folders.

					REMOVE ONE SERVER ENTRY
					Use the selected server's Delete action in Synix and read the confirmation. Removing a dashboard entry is not the same as agreeing to delete installation files or backups. Confirm exactly which data will be removed.

					Do not use deletion as a troubleshooting shortcut. A backup stored only inside a folder you are deleting is not a recovery copy.

					If you are switching to another control panel, confirm that it can start the correct installation and load the intended world before removing your old data.
					"""),

				["Using the Dashboard"] = CreateArticle(
					"Dash",
					"""
					Click a server row before using the controls along the bottom. The selected row determines which server an action affects.

					FIND A SERVER
					Use the search box to filter by game or server name. The status filter narrows the list further. If an entry seems missing, clear both filters before assuming it was deleted.

					READ THE SUMMARY
					• Installed Servers counts registered server entries.
					• Running Now shows entries currently reported as running.
					• CPU Usage and RAM Usage describe the whole PC, not just Synix.
					• Player data is available only when the selected game exposes a supported source.

					EVERYDAY CONTROLS
					• Start, Stop, and Restart manage the selected server.
					• Configure opens its setup fields.
					• Readiness checks issues that can prevent it from working.
					• Server Options contains file, maintenance, connection, and game-specific tools.
					• Double-click a row to inspect that server in more detail.

					Activity & Diagnostics shows progress and relevant warnings. Read the latest message before repeating an action. CLEAR clears the visible activity area; it is not a command to erase all log files.

					A running process is not necessarily ready for players. Installation, world loading, agreements, and game-owned setup can continue after a process appears. Confirm readiness in the game log and by joining.
					"""),

				["Starting, Stopping, and Restarting"] = CreateArticle(
					"Dash",
					"""
					Use Synix's lifecycle buttons for normal server operation. This lets the watchdog distinguish an intentional stop from a crash.

					START
					Select the entry and choose Start once. Synix checks the applicable prerequisites, paths, ports, and selected pre-start maintenance, prepares missing supported files, and launches the server. Wait for the result before trying again.

					STOP
					Choose Stop and allow the game time to save and close. Synix uses a supported graceful shutdown where available, then a staged process-termination fallback if needed. Forced termination can lose unsaved progress.

					RESTART
					Choose Restart to stop the current process group and start a fresh instance. Do not start another copy manually while this is happening.

					IF A BUTTON IS UNAVAILABLE
					Another operation may already own the server, the entry may be in a transitional state, or that game may use an external deployment. Read the activity message and the profile's capabilities.

					Avoid killing the server in Task Manager or stopping it from an unrelated tool for ordinary maintenance. That can look like an unexpected exit and trigger recovery.

					Games managed through virtual machines or external deployment services need their own lifecycle checks. A launcher closing does not prove those services have stopped.
					"""),

				["Server Options and Inspection"] = CreateArticle(
					"Dash",
					"""
					Select a dashboard entry and open Server Options. Only tools supported by that profile and its current state are offered.

					FILES AND MAINTENANCE
					• Open Server Folder and Open Backup Folder open the active locations.
					• The configuration editor opens supported game configuration files.
					• Update Server downloads the supported game update.
					• Validate Game Files checks or repairs installer-managed files.
					• Backup and Restore preserve or recover the selected installation.
					• Create Batch File exports a launch script when the profile supports it.
					• Delete removes an entry and offers only the file-removal choices shown in its confirmation.

					CONNECTIONS AND GAME TOOLS
					Connection Information shows addresses, ports, and copy actions. Connectivity tests, the Mod & Plugin Manager, Minecraft tools, and the Satisfactory Control Center appear where supported.

					INSPECT A RUNNING SERVER
					Double-click its row for server details. Live Process Details shows verified launchers, console hosts, and worker processes belonging to that entry. A multi-process game may have more than one process ID.

					Use the resource monitor to inspect that server's CPU and memory history. These readings are distinct from the dashboard's whole-PC summary.

					Closing a details or resource-monitor window does not stop the game. Use the dashboard's Stop action when that is your intention.
					"""),

				["Server Readiness and First Start"] = CreateArticle(
					"Dash",
					"""
					Open Readiness for the selected server before its first start or when something fails. It gathers the checks supported by that profile, such as files, runtimes, configuration, disk space, ports, process state, and relevant logs.

					UNDERSTAND THE RESULT
					• Ready: No blocking issue was found by the available checks.
					• Review: Read the finding and decide whether action is needed.
					• Blocked: Resolve the stated issue before continuing.

					Readiness does not automatically apply every suggested repair. Inspect the proposed action and its confirmation.

					AFTER YOU SAVE A NEW PROFILE
					The first Start can download files, install a supported runtime, generate configuration, and wait for game initialization. Follow first-start guidance and Activity & Diagnostics rather than treating the saved dashboard entry as an installed game.

					Some games need an agreement, authentication, or in-game claiming/session setup before anyone can join. Complete that step in the place the game requires.

					COMPATIBILITY VERIFICATION
					Install, Start, Stop, and Monitoring results record operations Synix has observed. Not verified yet means there is no recorded result; it does not mean the game is known to be broken.

					A Ready result or a successful local verification is not a guarantee of internet access, healthy mods, or compatibility with every client. Finish by testing the intended connection and world.
					"""),

				["Game Support Catalog"] = CreateArticle(
					"Dash",
					"""
					Open Add Server → Game Support Catalog to check what Synix knows about a game before creating an entry. The catalog uses the definitions included with your installed application.

					1. Search for the game name.
					2. Use the configuration, player-data, crossplay, compatibility, or verification filters to narrow the list.
					3. Select the game and open its details, or double-click its row.
					4. Read the supported setup, server-program, prerequisite, and verification information.

					Clear the filters if the game is unexpectedly absent. Minecraft's Java and Bedrock choices are part of its setup workflow rather than two interchangeable mod systems.

					READ CAPABILITIES CAREFULLY
					A profile can offer managed configuration, guided setup, launch settings, or limited/basic support. A listed executable or Steam AppID describes how installation works; it does not certify every game release.

					An authenticated SteamCMD requirement means the profile requests account access. Game ownership and the publisher's rules can impose additional requirements.

					DEFAULT PORTS ARE NOT A FIREWALL CHECKLIST
					A stored query-port value does not prove that a query listener exists or that it must be exposed. Use the actual game configuration and protocol requirements.

					The catalog is the place to inspect the installed definitions. Local verification records show what has been observed on this PC, not universal results for every machine or mod combination.
					"""),

				["Settings and Interface Language"] = CreateArticle(
					"Dash",
					"""
					Open the gear icon on the dashboard for application-wide settings. Use a server's Configure button for settings that belong to only that server.

					GENERAL
					• Show Server Console Window controls whether supported servers show their native console. A hidden window does not mean the server is stopped.
					• Dark Mode switches the application theme.
					• SteamCMD Download Speed chooses full speed or the supported download limit.
					• Language changes supported interface labels, dialogs, and status messages.

					Other pages contain backup locations and transfers, privacy/security options, problem reporting, and advanced/background settings. You do not need to enable advanced options to create a normal server.

					LANGUAGE BEHAVIOR
					Choose from the languages offered in the selector. Help articles and technical text logs/support reports intentionally remain in English. An unavailable translation can fall back to English.

					Game-facing text is not translated: commands, configuration keys and values, map names, modes, filenames, launch arguments, identifiers, and output-recognition text must remain in the form the game expects.

					Changing the interface language does not change a game's command syntax or translate its console output. If an ordinary Synix label is unexpectedly untranslated, report its exact wording, window, and selected language.
					"""),

				["Privacy and Saved Credentials"] = CreateArticle(
					"Dash",
					"""
					Open Settings → Privacy & Security before screen sharing or reviewing security options.

					PRIVACY MODE
					Privacy Mode masks supported sensitive displays, such as addresses and credentials. It does not change your public IP, encrypt network traffic, or clean every game log and configuration file.

					Inspect a screenshot before sharing it. A copy action on a masked field may still copy the real value.

					SAVED PASSWORDS AND TOKENS
					Synix protects supported saved passwords, tokens, and webhook values for the current Windows user. The game may still require readable credentials in its own configuration or launch arguments.

					A plain folder copy to another Windows account does not guarantee those protected values can be unlocked. Use the encrypted transfer workflow for supported credential migration.

					WINDOWS PERMISSIONS
					Run Synix and normal game servers with standard Windows permissions. A specific system change or game launcher can request elevation separately; it is not a reason to run everything permanently as administrator.

					Optional firewall cleanup concerns supported Synix-owned stale rules. Review the proposed rules before approving removal. It does not manage every custom folder or authorize deleting unrelated firewall rules.

					If a secret is exposed, replace or revoke it at the service that issued it. Hiding the field or removing Synix's saved copy does not invalidate that credential elsewhere.
					"""),

				["SteamCMD Downloads and Progress"] = CreateArticle(
					"Dash",
					"""
					SteamCMD downloads and updates the dedicated-server files for supported Steam games. Synix prepares its shared copy automatically; an ordinary server setup does not need a separate SteamCMD tab.

					FOLLOW THE ACTIVE OPERATION
					Select the server and read Activity & Diagnostics. The dashboard animation indicates installation work is in progress. SteamCMD can switch between downloading, allocating, unpacking, and validating, so its output does not always provide a steady percentage or a reliable time remaining.

					Allow disk work to finish even when network traffic drops. Read the final success or failure message before starting another operation.

					LIMIT DOWNLOAD SPEED
					Open Settings → General → SteamCMD Download Speed. Choose the supported limited mode and enter the displayed rate, or select full speed.

					This affects SteamCMD downloads, not players' bandwidth, every game's non-Steam downloader, or all traffic on the PC.

					SIGN-IN AND FAILURES
					Some game profiles require an authorized Steam account and Steam Guard confirmation. Use Synix's sign-in prompt; do not place account credentials in public scripts or reports.

					For a failed download, read the exact SteamCMD error and check free disk space, folder access, network access, and any account requirement. Validation may redownload installer-managed files, so preserve custom modifications before using it.
					"""),

				["Server Setup Pages"] = CreateArticle(
					"Config",
					"""
					Choose Configure for an existing server, or use the setup window while creating one. Change only the fields that apply to your game.

					BEGINNER AND ADVANCED MODES
					Beginner mode emphasizes common settings and the recommended launch command. Advanced mode reveals additional technical controls, including supported RCON and raw launch options. Switching modes does not erase saved values. Discord notifications are available in either mode.

					WHAT EACH PAGE CONTROLS
					• General: Server name, game, edition/version, and supported gameplay-profile choices.
					• Security: Passwords and other access settings supported by the template.
					• World Generation: World name, seed, size, and applicable world options.
					• Network & RCON: Supported gameplay, query, companion/API, and administrative endpoints.
					• Automation: Backup/update-on-start and scheduled maintenance.
					• Discord: Notification destinations and event routing.
					• Install & Launch: Installation folder and supported launch options.

					Game-specific disabled fields are intentional. A game may own that setting itself or lack support for it; forcing extra arguments into the command is not a safe substitute.

					THE FOUR CHECKPOINTS
					Server details → Required settings → Review → Save server

					These track configuration readiness, not download or installation progress. Review is optional; required valid settings determine whether saving is available. Read the exact footer message when something needs attention.

					When editing a server, stop it first if the change affects files or options read at launch. Save Changes updates its profile; it does not promise that a running game has applied those values. Restart through Synix when the game requires it.
					"""),

				["Editing Configuration Files"] = CreateArticle(
					"Config",
					"""
					Use Configure for the common fields Synix manages. Use the configuration-file editor in Server Options when you need to inspect or change the game's actual configuration.

					1. Stop the server if it reads the file at startup or rewrites it while running.
					2. Make a backup and confirm the selected server and file.
					3. Read the existing values and comments.
					4. Change only the settings you intend to change.
					5. Resolve the editor's validation errors and review its warnings.
					6. Save, start the server through Synix, and check the latest game log.

					SUPPORTED FORMATS
					Synix provides format-aware handling for supported INI/properties, JSON, XML, YAML, and other profile-specific formats. Not every file or option has a managed editor.
					YAML editing supports nested settings, indented lists, and single-line values while keeping comments and indentation. Complex YAML features such as anchors, aliases, inline collections, and multiline values are refused without saving.

					Keep configuration keys, accepted values, map names, and commands exactly as the game expects them. Do not translate them to match the interface language.

					VALIDATION IS NOT A GAME TEST
					A well-formed file can still contain incompatible settings. Mods or game updates can add options the editor does not recognize. Check that the game loaded the intended file and world after restarting.

					If another editor or the game changes the file while it is open, reload and compare before saving. Game files can contain readable passwords or tokens; review and redact them before sharing.

					Satisfactory's live API options belong in its Control Center and are separate from editing its on-disk configuration files.
					"""),

				["Configuration Repair and Recovery"] = CreateArticle(
					"Config",
					"""
					Use this guide when the configuration editor reports a missing managed field, invalid structure, or a template mismatch.

					UNDERSTAND THE CHOICES
					• Normal save updates supported values in the selected configuration.
					• Repair addresses a reported supported problem.
					• Reset restores template defaults and can remove custom settings.

					A reset is not the first step for every server failure. Missing game runtimes, occupied ports, and broken mods are not fixed by repeatedly replacing configuration files.

					BEFORE REPAIRING
					Stop the game, keep an untouched copy of the affected files, and read the exact proposed change. Some profiles have complete templates; others expect the game to generate files on first start or require manual setup.

					Synix reports missing required tags rather than assuming it can replace every line safely. Applying a managed template revision can preserve a one-time copy named .synix.before-template-v<revision>.bak.

					AFTER A FAILED SAVE OR REPAIR
					Keep any backup or recovery files and read the recovery message. Compare the original and changed configuration before choosing what to restore. Do not erase evidence by repeatedly trying Reset.

					After recovery, start once and check the game's own log. Confirm that it is reading the correct path and that custom settings you need are still present.
					"""),

				["Launch Options and External Scripts"] = CreateArticle(
					"Config",
					"""
					Install & Launch contains the selected server folder and its supported launch options. Keep the recommended command unless you understand the game's argument syntax.

					EDITING ARGUMENTS
					Use Advanced mode when raw arguments are needed. Check quoting, executable paths, and game-required option names. Synix already supplies supported managed arguments; adding another copy can produce conflicting settings.

					Do not translate switches or replace an expected technical value with its translated label. Never add credentials to a command merely to make them easier to copy.

					EXPORTED LAUNCH FILES
					For supported profiles, Server Options → Create Batch File writes the resolved launch command to a script.

					That file may contain readable passwords or service tokens so it can run independently. Keep it private and inspect it before sharing or moving it.

					Running the script outside Synix does not provide all of Synix's operation locks, watchdog coordination, backups, or scheduled maintenance. Do not run it while Synix already has that server running.

					EXTERNAL DEPLOYMENTS
					Some game profiles launch their own deployment tools instead of a single ordinary server process. Those tools may require a separate permission prompt and may not support launch-file export. Follow the profile's game guide rather than substituting a different executable or wrapper.
					"""),

				["Connecting Locally and Over the Internet"] = CreateArticle(
					"Net",
					"""
					First prove that the game works on your own network. Router changes cannot fix a game that has not started or completed its initial setup.

					CHOOSE THE RIGHT ADDRESS
					• On the server PC: Use the game's supported local or loopback connection.
					• On another home-network device: Use the server PC's LAN address.
					• Outside your home network: Use your public address or the game's supported discovery/join method.

					Open Connection Information for the selected entry to inspect its endpoints. A private LAN address cannot be used directly by internet players.

					TEST LOCALLY
					1. Wait for the game to finish loading.
					2. Check the configured port and the server's Readiness results.
					3. Join from the host PC, then from another LAN device.
					4. If LAN access fails, check the game log and Windows Firewall access for the actual server executable.

					ALLOW INTERNET PLAYERS
					1. Give the host a stable LAN address, usually with a router DHCP reservation.
					2. Identify the required ports and TCP/UDP protocols for the actual game configuration.
					3. Forward only those endpoints to the host's LAN address.
					4. Allow the matching game traffic through Windows Firewall on the network profiles you use.
					5. Test from a genuinely separate internet connection.

					Do not enable a router DMZ, disable the firewall, or expose every administrative port as a general fix. Keep router credentials private.

					Synix is a local desktop manager. It does not include a hosted web panel or public relay that makes these network requirements disappear.
					"""),

				["Understanding Server Ports"] = CreateArticle(
					"Net",
					"""
					Open Configure → Network & RCON to review the endpoints supported by the selected game. Use the game's actual configuration, not a generic list of ports.

					PORT ROLES
					• Game port: Player traffic.
					• Query port: Server information or discovery, if that query protocol is supported.
					• RCON/admin port: An optional administration channel.
					• App/API port: A game-specific companion or management service.

					TCP and UDP are different protocols. Two different protocols can use the same port number, but two servers cannot share a conflicting listening endpoint. Synix also checks saved profiles for port conflicts, including stopped entries.

					Keep enabled endpoints unique for separate instances and preserve any game-required relationship between ports. Do not assume all gameplay is UDP or that every query field represents a real listener.

					ADMINISTRATION IS NOT GAMEPLAY
					Players usually do not need access to RCON or another admin endpoint. Keep management local unless you have a specific secured remote-administration plan.

					GAME-SPECIFIC EXAMPLES
					• Minecraft Java and Bedrock use different connection behavior; follow the selected edition.
					• Satisfactory's local API uses its configured game/API port; its legacy query-field value is not an extra requirement for Synix.
					• Dune's query-field metadata represents its RMQ deployment port, not an ordinary Steam A2S query service.
					• Rust+ uses its own companion endpoint in addition to gameplay.

					Template defaults are starting values, not a universal router-opening checklist. Check the actual listener before changing a firewall rule.
					"""),

				["Connection Tests and Player Counts"] = CreateArticle(
					"Net",
					"""
					Select a server and use its connection-test or player tools when they are available. Synix chooses checks according to the game's supported query, TCP/UDP/HTTP, local API, and process capabilities.

					WHAT A RESULT MEANS
					A low-level connection response does not prove that a player can authenticate and join. A live process also does not prove that its query service is ready.

					Player count and player names are separate capabilities. A server can report how many players are connected without exposing a named roster. An empty roster is not a confirmed zero-player count.

					HIDDEN TOOLS OR N/A
					Some EOS-only or crossplay modes do not expose a dependable supported query or player-management channel. Synix may show N/A or omit a test instead of displaying a misleading result.

					Check the selected edition and configured management channel for Minecraft. Satisfactory player counts depend on its optional local API connection. Do not invent developer-only EOS credentials to force a test.

					SERVER-BROWSER LISTINGS
					Many Steam games use A2S queries, but this is not universal. Registration, game settings, and the browser service affect visibility, and there is no guaranteed listing time.

					If a server is absent from a browser, test the game's direct-connect method and review its log. Changing unrelated query ports repeatedly can introduce additional problems.
					"""),

				["NAT, Public Addresses, and Failed WAN Tests"] = CreateArticle(
					"Net",
					"""
					Use this guide when LAN play works but a public-address test does not.

					YOUR OWN PUBLIC ADDRESS FAILS
					Some routers do not support NAT loopback, also called hairpinning. A connection from inside the home to its own public address can fail even while outside players can join.

					Use the LAN address at home and ask someone on another internet connection to test the public address. A failed in-PC WAN probe alone is not proof of an internet outage.

					PORT FORWARDING MAKES NO DIFFERENCE
					Check for another router upstream, double NAT, or carrier-grade NAT (CGNAT). Compare the router's WAN information with the public address and ask the ISP whether inbound connections are supported.

					A forwarding rule on your router cannot open an ISP-controlled upstream NAT. Depending on the connection, a public-address service, suitable VPN/tunnel, or hosted server may be needed. These are external arrangements, not built-in Synix features.

					DO NOT CONFUSE MANAGEMENT WITH PLAYER ACCESS
					A remote-control connection and the game's player connection are separate paths. Enabling a local API or Discord notifications does not automatically make the game reachable from the internet.

					Keep any network test narrowly scoped to the game's real port and protocol. Do not publish passwords, tokens, router screenshots, or private network details when asking for help.
					"""),

				["Creating and Keeping Backups"] = CreateArticle(
					"Maint",
					"""
					A selected-server backup preserves files in that server's installation folder. Check where the game actually saves its world before relying on it.

					CREATE A BACKUP
					1. Stop the server through Synix and wait for its processes to exit.
					2. Select the backup action in Server Options.
					3. Review the source, destination, estimated size, and available disk space.
					4. Confirm and wait for completion.
					5. Choose Open Backup Folder to locate the archive. Keep its integrity receipt with it.

					COVERAGE
					The backup includes the installation folder, not only a small save subfolder. Large games can need substantial time and storage.

					Saves in AppData, Documents, another drive, or a game-owned virtual machine may be outside that folder. Back those up separately using the game's supported method.

					LOCATION AND RETENTION
					The normal backup root is C:\Synix\BackupGames. Settings → Backups lets you choose another root and set the saved-backup limit.

					The destination must be outside the source server folder. Changing the backup root does not move or delete existing archives; check the old location for older backups.

					Retention removes older archives when the configured limit is exceeded during backup creation. Keep permanent recovery points outside that rolling-backup location.

					Store another verified copy of important worlds on separate storage. A failed disk can destroy both the server and backups kept on that same disk. A ZIP of an external deployment folder is not automatically a consistent VM or database backup.
					"""),

				["Restoring a Server Backup"] = CreateArticle(
					"Maint",
					"""
					Restore can replace the selected server's installation contents. Choose the correct server and preserve anything newer before proceeding.

					1. Stop the server and make a separate copy of current files you may need.
					2. Open the Restore backup workflow in Server Options.
					3. Select the intended archive and review its server, date, size, and integrity status.
					4. Verify it before restoring. Synix backups with SHA-256 receipts can be checked against those receipts; older archives may be identified as legacy.
					5. Read the replacement warning and confirm only if this is the recovery point you want.
					6. Let staging and recovery finish without closing Synix or disconnecting storage.
					7. Start the server and confirm that the intended world and settings were restored.

					WHAT VERIFICATION PROVES
					An integrity check detects changes relative to a receipt. It does not prove that a world was healthy when saved or that an archive from another person is trustworthy.

					IF RESTORE IS INTERRUPTED
					Keep the archive and any rollback or recovery files. Read the recovery message before trying another restore or removing files.

					Do not extract a ZIP over a running server. If the game keeps saves outside the installation folder, restore those separately with the game's supported procedure.

					Keep the original backup until you have tested the restored server and a client connection.
					"""),

				["Moving Synix to Another PC"] = CreateArticle(
					"Maint",
					"""
					Use Settings → Backups for a complete Synix export/import. This is different from registering one existing server folder.

					CHECK WHAT MUST MOVE
					The export packages the main C:\Synix tree. Custom server or backup folders outside it and external game saves need separate handling. Record their paths before starting. Ordinary Windows preferences may also need review on the destination.

					EXPORT
					1. Stop servers and finish active maintenance, downloads, and backups.
					2. Choose encrypted or normal export.
					3. Select a destination outside the source tree and review size and temporary-space requirements.
					4. For encrypted export, choose a strong transfer password and store it separately.
					5. Wait for the .synixbackup package to finish, then verify it.

					CHOOSE THE RIGHT PROTECTION
					Encrypted export protects the package and supports portable transfer of supported credentials to the destination Windows account.

					Normal export is not encrypted. Game files and logs inside it may expose secrets. Copying Windows-bound encrypted values does not by itself make them usable under another account.

					IMPORT
					1. Back up the destination's current Synix data first.
					2. Open the import/verification controls and choose the intended package.
					3. Provide the transfer password if requested and verify the package.
					4. Review the destination, disk requirements, and overwrite warnings.
					5. Let staging, validation, and recovery complete.
					6. Check server paths, ports, schedules, backup locations, and credentials before starting anything.

					Reconnect Satisfactory if its local identity or protected connection no longer matches. Keep the original package and old data until the imported servers are tested.

					Supported data migrations preserve a pre-migration database copy. If import or migration fails, keep that copy and the recovery message instead of replacing the active database while Synix is running.
					"""),

				["Automatic Backups and Updates"] = CreateArticle(
					"Maint",
					"""
					Open Configure → Automation for the selected server to choose work that happens before supported launches.

					BACKUP ON START
					Enable this to create a stopped-server backup before applicable starts. Set the backup root and retention in application Settings → Backups.

					Crash-recovery starts deliberately skip the normal backup routine. Automatic startup backups are not a replacement for keeping independent recovery points before major changes.

					UPDATE ON START
					Enable this to perform the supported game update check before launch. It updates game-server files, not the Synix application.

					An update check is not a promise that every start downloads files. SteamCMD and the game's installer decide what is needed.

					VALIDATION AND CUSTOM FILES
					A manual Validate action can repair or redownload installer-managed files. Preserve modifications before updating or validating; do not assume custom files always remain untouched.

					TEST YOUR CHOICES
					Save the settings and perform an attended start. Read the activity results for backup, update, and launch. A completed download does not prove that the game has loaded its world successfully.

					Allow enough free space for archives, downloads, temporary files, and the installed game. If pre-start work reports a failure, investigate the stated reason rather than launching the same server outside Synix to bypass it.
					"""),

				["Scheduling Smart Maintenance"] = CreateArticle(
					"Maint",
					"""
					Configure a server's maintenance schedule only after you have tested its normal backup, update, stop, and start behavior.

					1. Open its automation/schedule settings and enable the schedule.
					2. Choose the days and time.
					3. Enable Smart Maintenance and select the available backup/update options.
					4. Decide whether to wait for players and set the maximum delay.
					5. Save and test during a maintenance window you can supervise.

					Schedules use the PC's local time. Keep its clock and time zone correct.

					WHEN THE SCHEDULE IS DUE
					Synix can wait for connected players, then coordinate stopping the verified process group, backing up while stopped, updating, and restarting according to the selected options.

					The maximum delay is a limit, not a promise to wait forever. Maintenance can proceed when it expires even if players remain. A zero delay gives no waiting period.

					PLAYER INFORMATION HAS LIMITS
					Waiting depends on the information the selected game can provide. An API-linked Satisfactory server treats stale or unavailable counts conservatively during the configured waiting period. That does not make unsupported query data reliable.

					Notify players in advance and read the resulting activity messages. Discord notifications can report supported maintenance events when their routes are configured.

					The PC must be awake and the relevant Synix process or background agent must be running. A shut-down or sleeping computer cannot perform normal scheduled downloads and restarts.
					"""),

				["Discord Notifications"] = CreateArticle(
					"Maint",
					"""
					Discord webhooks send selected server events to a channel. They are notifications, not a Discord bot for remotely controlling Synix.

					SET UP A DESTINATION
					1. Create or obtain a webhook for a channel you are authorized to manage.
					2. Open the selected server's Configure → Discord page.
					3. Enter the webhook and choose the available event-routing options.
					4. Save the configuration.
					5. Use a test action if offered, or confirm delivery during a planned event.

					Startup, shutdown, recovery, backup, and maintenance messages depend on the routes enabled and whether the operation actually occurred.

					PROTECT THE URL
					A webhook URL contains a posting credential. Anyone who obtains it may be able to send messages to that channel. Keep it out of public screenshots, issues, scripts, and chats.

					Synix protects its saved webhook for the Windows user. If the URL is exposed, rotate or delete it in Discord, then update Synix's destination. Review the server names and event details sent to a shared channel.

					IF MESSAGES DO NOT ARRIVE
					Check the selected event route, the URL, channel/webhook availability, and whether the PC can reach Discord. Inspect the reported error without publishing the full URL.

					Notifications use an outbound connection to Discord. They do not require opening an inbound router control port.
					"""),

				["Watchdog and Crash Recovery"] = CreateArticle(
					"Watch",
					"""
					The watchdog watches recognized server processes and can initiate configured recovery after an unexpected exit or detected problem.

					NORMAL OPERATION
					Use Synix's Stop and Restart controls for intentional changes. Synix coordinates those actions with the watchdog so a requested shutdown is not treated as a crash.

					Closing a game's window, killing it in Task Manager, or stopping it through another manager may look unexpected and trigger recovery.

					REPEATED RESTARTS
					Stop the server through Synix, then inspect the latest game log. Fix the underlying runtime, configuration, port, memory, or mod issue before starting it again. Recovery cannot repair an incompatible mod or an unfinished game-owned setup.

					A process failure and an unavailable query are different problems. Recovery timing depends on the game and current state, so do not diagnose every delay using one assumed timeout.

					LIMITATIONS
					Synix tracks supported launchers and child processes belonging to the server. Externally managed virtual machines or deployment services can outlive the launcher and need game-owned controls.

					The watchdog is not a substitute for verified backups or watching a newly changed server during its first successful start.
					"""),

				["Resource Checks and Network Guard"] = CreateArticle(
					"Watch",
					"""
					Synix checks resource pressure before certain launches to reduce the chance of overloading the host. If a start is delayed or blocked, read the stated resource issue.

					KEEP ROOM FOR THE HOST
					Allow memory for Windows and background applications as well as the game. Keep disk space available for updates, temporary files, saves, and backups.

					A configured Minecraft memory limit affects its game/runtime. It is not a universal hard limit for every process launched by every game.

					Resource checks do not put a server in a CPU/RAM container or guarantee that Windows can never run out of resources. Use the resource monitor and the whole-PC dashboard summaries to understand the actual load.

					NETWORK GUARD
					The experimental network-warning feature watches unusual bandwidth/resource conditions and accounts for known SteamCMD activity to reduce false warnings.

					It is not a firewall, traffic filter, or DDoS protection service. An alert does not identify an attacker or prove an attack occurred.

					When warned, compare the activity with known downloads, backups, and other applications. Inspect the relevant logs before changing security or network settings.
					"""),

				["Background Operation and Reopening Synix"] = CreateArticle(
					"Watch",
					"""
					Closing a child window, minimizing Synix, and explicitly exiting the application are different actions. Review your configured behavior and any exit prompt.

					REOPENING THE CONTROL PANEL
					Synix can recognize supported server processes that are still running and recover ownership of them. Allow the initial process check to finish before starting another instance.

					Process recognition must match the selected installation, not merely a familiar executable name. Custom wrappers or externally managed services may need additional attention.

					BACKGROUND SETTINGS
					Optional startup/background-agent settings support monitoring at Windows sign-in. They are not required to create your first server.

					Scheduled work needs the relevant Synix process or agent, an available Windows session, and an awake PC. Sign-in startup is not a promise that work will run while the machine is shut down.

					BEFORE RESTARTING WINDOWS
					Finish backups, downloads, and transfers. Stop games through Synix so they can save normally, then exit or restart Windows.

					Do not assume a hidden game console has stopped or that closing a monitoring window shuts down its game. Check the dashboard and use intentional lifecycle controls.
					"""),

				["Installation and Windows Security Problems"] = CreateArticle(
					"Trouble",
					"""
					Read the exact Windows message before deciding what to change. A blocked executable, a file-access error, and a missing game runtime are different problems.

					SYNIX WILL NOT INSTALL OR OPEN
					Use an official release or the exact WinGet package ubidzz.Synix. If replacing a file reports access denied, close the Synix instance using that path normally and retry. Check folder access and security-software messages if the problem continues.

					Do not delete server data or disable protection as a general repair.

					SMARTSCREEN
					A reputation warning can offer More info → Run anyway, depending on Windows policy. The presence of that option is not proof that a file is safe. Verify the official source and investigate the warning before deciding whether to proceed.

					SMART APP CONTROL
					This can block an app when Windows cannot establish enough trust. It has no individual-app bypass comparable to SmartScreen's Run anyway. Do not disable Windows security features solely to run Synix.

					ANTIVIRUS OR HASH FAILURE
					Investigate the exact detection and file. Do not assume an alert is a false positive because the file is named Synix. A matching SHA-256 hash identifies the downloaded file; it does not establish trust or code signing.

					Report the exact warning and installation method with private details removed. Avoid generic exclusions or bypassing installer hash verification.
					"""),

				["Missing Game Requirements"] = CreateArticle(
					"Trouble",
					"""
					Open the selected server's Readiness result to identify the missing prerequisite. The runtime included with Synix does not replace a game's separate requirements.

					MISSING DLL MESSAGES
					Errors mentioning MSVCP140.dll or VCRUNTIME140.dll commonly point to a Visual C++ runtime requirement. Use the game's instructions and the matching supported Microsoft redistributable.

					Microsoft's redistributable information:
					https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist

					Do not download individual DLL files from random websites or place unrelated DLLs beside the server executable.

					OTHER REQUIREMENTS
					A game can require an account, server entitlement, particular Java runtime, Windows feature, or supported hardware. Complete those requirements before changing unrelated ports or resetting configuration.

					For Minecraft Java, use a supported version/loader combination from Synix's selectors. Bedrock does not use Java.

					For a deployment requiring Hyper-V or hardware virtualization, inspect its game-specific readiness checks. Do not enable system features or run every server as administrator simply to bypass a warning.

					After fixing the requirement, perform one new start and review the first meaningful game-log error if it still fails.
					"""),

				["Server Startup and Shutdown Problems"] = CreateArticle(
					"Trouble",
					"""
					Read the selected server's latest activity and game log. Look for the first actual failure rather than only the final shutdown message.

					STUCK ON STARTING OR EXITING EARLY
					1. Confirm the selected installation folder, executable, game edition, and loader.
					2. Check required files, free RAM/disk space, and occupied ports.
					3. Complete any agreement, account sign-in, token requirement, or in-game session setup.
					4. For mods, compare the game, loader, dependencies, and client requirements.
					5. Stop through Synix before changing files, then test one start.

					A live launcher can remain after the real server failed. A healthy server can also be waiting for world loading or have an unavailable query/API. Inspect those states separately.

					STOP OR RESTART TAKES TIME
					Games need time to save and close worker processes. Wait for Synix's result and read its shutdown message. A forced fallback can lose unsaved progress.

					Do not simultaneously kill processes, start another copy, or use another manager against the same installation.

					A TOOL OR SETTING IS MISSING
					Check the profile's capabilities and current state. Some options are disabled because the game owns them or no supported management channel exists. N/A player data is not automatically a server failure.

					If the problem persists, record the action, exact error, and a short redacted log excerpt before making further unrelated changes.
					"""),

				["Reading Logs and Crash Reports"] = CreateArticle(
					"Trouble",
					"""
					Use the log that belongs to the part that failed.

					SYNIX ACTIVITY
					Activity & Diagnostics shows progress and user-relevant warnings on the dashboard. Clearing that area only clears the visible messages; it does not erase all saved diagnostics.

					APPLICATION LOGS
					Synix text logs and fatal-crash reports are normally under:
					C:\Synix\SynixData\logs

					A crash dialog may name a specific file. Use that path and the time of the failure to find the relevant entry.

					Handled diagnostic exceptions can be written to the text log without appearing on the dashboard. A logged handled exception is not automatically evidence of an application crash.

					GAME LOGS
					Use the selected server's actual game log for world-loading, configuration, mod, and game-runtime errors. Its location depends on the game and can be outside the installation root.

					READ IN CONTEXT
					Note the action and time, then inspect the messages around the first meaningful failure. A shutdown line at the end can be a consequence rather than the cause.

					Before sharing any excerpt, remove passwords, tokens, webhooks, private addresses, and personal paths. Satisfactory's own console/log can contain an administrator token. Keep the original private log for your own investigation.
					"""),

				["Minecraft Setup and Mod Loaders"] = CreateArticle(
					"Games",
					"""
					Choose the Minecraft edition before choosing mods or connection settings. Java and Bedrock are separate server types; Bedrock is not a Java loader.

					SET UP THE SERVER
					1. Create a Minecraft entry and select Java or Bedrock.
					2. For Java, choose the Minecraft version, loader, and compatible loader version offered by the selectors.
					3. Review the world, native game mode, player limit, memory, ports, and enabled security fields.
					4. Save and Start through Synix.
					5. Read any agreement or first-start prompt before accepting it.
					6. Wait for the world to finish loading, then join with the matching client edition and version.
					7. Back up the working world before adding mods.

					JAVA AND BEDROCK
					Java choices include Vanilla, Fabric, Forge, and supported NeoForge combinations. Use the choices actually offered; do not invent a version string to bypass compatibility checks. Synix prepares the supported Java runtime/loader combination.

					Bedrock uses the official Bedrock Dedicated Server package and does not use Java.

					MATCH THE MODS
					Choose the loader required by your mods. Match the Minecraft version, loader, dependencies, and any client-side requirements.

					Fabric, Forge, NeoForge, and plugin-server packages are not interchangeable. Creating a mods or plugins folder does not install the required loader or server framework. Browse Catalog is not automatic modpack or dependency installation.

					CONFIGURATION
					Configure manages supported server.properties and edition-specific settings. Native modes include Survival, Creative, and Adventure; technical values and commands keep the form Minecraft expects.

					Stop before changing files that require a restart. Do not replace an existing world with a fresh template as a repair shortcut.

					IF PLAYERS CANNOT JOIN
					Check edition, game/loader versions, required mods, and the selected edition's actual port/protocol. For an early exit, inspect the game log for agreement, runtime, memory, or dependency errors.
					"""),

				["Minecraft Console and Player Tools"] = CreateArticle(
					"Games",
					"""
					Select the Minecraft server and open its console or player tools from Server Options.

					COMMAND CHANNELS
					Supported configurations can use a Synix-managed console, a supported localhost management channel, or optional RCON. The available channel depends on the edition and server configuration.

					A hidden native window can still leave a supported local command channel available. If controls are disabled, check that the server is running and that its management settings are valid.

					PREPARED ACTIONS
					Where supported, Synix provides announcements, player lists, moderation, operator and allowlist changes, time/weather controls, and saving.

					Select the intended player and read the action before confirming it. Giving operator access grants powerful game permissions. Do not grant it merely to solve a connection problem.

					RAW COMMANDS
					Use the syntax expected by that edition. Commands such as list and save-all are game input and are not translated with the Synix interface. Check the game's command rules before sending a command that changes worlds or permissions.

					COUNTS AND NAMES
					A query can provide a player count without an authenticated named roster. An empty names table does not by itself prove nobody is connected.

					Use the dashboard Stop or Restart action for lifecycle changes so the watchdog knows the shutdown is intentional.

					Keep optional RCON or administration endpoints local unless you have a secured remote-management arrangement. Public gameplay does not require exposing those credentials or endpoints.
					"""),

				["Mod and Plugin Manager"] = CreateArticle(
					"Games",
					"""
					Open Server Options → Mod & Plugin Manager for a supported profile. Synix detects installed content and supported destinations; it is not a catalog containing every mod.

					INSTALL A LOCAL PACKAGE
					1. Back up the world, existing add-ons, and affected configuration.
					2. Stop the server through Synix and wait for its process group to exit.
					3. Check the detected add-on system, install area, and safety checklist.
					4. Choose Install From File or Import Package for a supported file, ZIP, or complete mod folder. The available choices depend on the game's add-on system.
					5. Review the destination, scan result, warnings, and confirmation.
					6. Let staging, backup, and installation finish.
					7. Refresh the list, restart if required, and check the game log and a client connection.

					Simplified view hides some technical detail without bypassing the checks.

					WORKFLOW TYPES
					Supported workflows include Minecraft JAR packages, Rust Oxide/uMod plugins, local 7 Days to Die packages, Empyrion scenarios and server mods, ARK: Survival Evolved Workshop IDs, and ARK: Survival Ascended provider IDs. Some systems are detection-only or require their framework first.

					For provider IDs, Synix manages supported configuration while the game/provider delivers content. Content not yet downloaded cannot be pre-scanned locally.

					WHAT CHECKS CAN AND CANNOT DO
					Imports check package layout and destinations and reject unsafe paths, links, disallowed executable content, oversized packages, duplicate destinations, and confirmed malware detections.

					An unavailable or inconclusive Defender scan is a warning, not proof of malware. A clean scan and recorded hash also do not prove a third-party package is trustworthy. Mods run with the server's Windows permissions.

					CATALOGS, REMOVAL, AND RECOVERY
					Browse Catalog opens the provider website. It does not automatically resolve dependencies or install a complete modpack.

					Stop before Remove Selected and review its target and rollback information. Synix records changes it performs, not every manual edit. Keep independent backups: removing a mod can make a world unloadable or remove mod-owned content.

					An empty table means no supported add-ons were found in the active locations. Check the profile, folder, and framework before assuming all files on disk were searched.
					"""),

				["Empyrion: Scenarios and Server Mods"] = CreateArticle(
					"Games",
					"""
					Empyrion scenarios and server-code mods are different. In Server Options → Mod & Plugin Manager, select Empyrion scenarios or Empyrion server mods. Stop the server and keep a full server backup before making changes.

					INSTALL A SCENARIO
					1. Browse Catalog opens Steam Workshop. Subscribe to the scenario and wait for Steam to finish downloading it. Workshop blueprints and collections are not server scenarios.
					2. Choose Import Package, then Choose Folder. Select the individual scenario folder inside your Steam library's steamapps\workshop\content\383120 folder. You can also choose a complete scenario ZIP.
					3. Enter a recognizable scenario folder name. Do not use only the numeric Workshop ID. Keep that same folder name for later updates.
					4. Review the package checks and destination, then confirm the import. Synix copies the complete scenario into Content\Scenarios without activating it or changing any world.
					5. Choose Scenario lists installed scenarios. Select one and review the separate save name. Changing to a different scenario requires a new, unused save name; Synix suggests one and leaves existing saves untouched.
					6. Apply the selection, start the server, and check its log and a client connection. Some scenarios have additional settings or requirements documented by their author.

					The scenario name is GameConfig.CustomScenario; the saved-world name is GameConfig.GameName. Synix backs up dedicated.yaml before changing these fields and keeps the server entry in sync. It does not reset the seed, passwords, or unrelated settings.

					INSTALL SERVER MODS
					Use the Empyrion server mods profile and Import Package to select a compiled mod ZIP or complete mod folder. Include its DLLs, supporting assets, dependencies, and _Info.yaml when supplied. Synix installs these under Content\Mods; the game loads the code when started. Source-code downloads and standalone installers are not supported packages.

					Synix does not install a mod loader or enable a disabled mod automatically. Follow the author's requirements, including any additional loader, configuration, and client setup. A successful import does not prove that a mod is compatible with the current game or other installed mods.

					UPDATES AND RECOVERY
					Steam updates its Workshop copy, not the server copy imported by Synix. To update, stop the server and import the updated package using the same scenario folder name. Check whether the author requires a fresh save. Replaced files receive rollback copies; importing an update is not a clean reinstall and does not delete files missing from the new package.

					Refresh lists complete packages rather than every individual asset. Installed (not re-verified) means the import was recorded, not that every installed file has just been checked again. Roll Back Import reverses the entire selected import, including other mods in the same package, and refuses to replace files changed outside Synix. The selected scenario and scenario assets needed by saved worlds are protected from removal; a recorded scenario update can still be rolled back to its previous files.
					"""),

				["Satisfactory: Connect Automatically"] = CreateArticle(
					"Games",
					"""
					Satisfactory requires initial setup in its own in-game Server Manager. Synix can install and launch the dedicated server, but it does not replace claiming the server or creating its session.

					FINISH THE GAME'S SETUP
					1. Create the Satisfactory entry and Start it in Synix.
					2. Wait for the dedicated server to finish loading.
					3. Open Satisfactory's in-game Server Manager and add or select the server.
					4. Claim it, set the administrator password, and sign in as administrator.
					5. Create or load the session you want to use.

					Synix manages supported launch/configuration values, including the launch port and MaxPlayers. Applicable files are under FactoryGame\Saved\Config\WindowsServer.

					CONNECT SYNIX
					1. Keep the server running with -NewConsole. Check imported or older entries; if you add this argument, restart through Synix.
					2. Use the same Windows permission level for Synix and the server, preferably normal user permissions.
					3. Select that server and open Server Options → Satisfactory Control Center → Connect API.
					4. Choose Connect automatically once and wait.

					Synix sends server.GenerateAPIToken, reads the complete fresh output, verifies the local API, and saves the token encrypted for your Windows account. The token grants administrator access, not just monitoring.

					Success reports Token verified and saved securely and opens Overview. You do not need to copy a console line or paste a token manually.

					If the attempt fails, Synix keeps any previously saved connection and does not automatically retry the command. Read the specific result before trying again.

					This connects to the selected server on this PC; it does not require a new router rule for Synix. Normal Start, Stop, and Restart still work without an API token.
					"""),

				["Satisfactory: Settings, Saves, and Commands"] = CreateArticle(
					"Games",
					"""
					Open the Satisfactory Control Center after connecting the local API. Select the tab for the operation you need.

					OVERVIEW
					Choose Refresh to read session name, connected-player count/limit, game state, average tick rate, play time, and technology tier.

					Dashboard player counts update automatically while the connection is healthy; the Overview table refreshes on request. This API provides counts, not a named-player roster. A paused session or a server waiting for a session is not a stopped process.

					SERVER SETTINGS
					Choose Refresh, edit supported values, then Apply changes. Recheck pending values after refreshing. Some options require a restart; applying changes does not automatically restart the game.

					Setting keys and accepted values remain in the game's technical language.

					SAVED GAMES
					• Save now: Enter a name or use a dated Synix name. Reusing a name may overwrite an existing save.
					• Load selected: Review the confirmation. Synix saves current progress first when a session is active; loading can disconnect players.
					• Upload save: Adds the selected file and may replace a same-named save. It does not automatically load it.
					• Download selected: Saves a copy to the local path you choose.

					Keep an independent backup before replacing or loading an important world. Save transfers have a 512 MB safety limit. The API can be temporarily unavailable while a session loads.

					ADVANCED CONSOLE
					Send one supported game command at a time and read the confirmation. Scripts, token/password commands, and insecure-access changes are blocked here.

					Supported stop commands are routed through Synix's stop workflow. Command output stays in this window rather than being posted to the dashboard or Synix text logs; the game may still write its own logs.
					"""),

				["Satisfactory: Connection Problems and Token Safety"] = CreateArticle(
					"Games",
					"""
					Read the result of Connect automatically before retrying. A command appearing in the game console does not prove that token capture, verification, and saving all succeeded.

					CHECK THE SPECIFIC FAILURE
					• Console unavailable: Confirm -NewConsole, the selected server, and matching Windows permission levels.
					• Unfinished command in the input: Clear it yourself. Synix will not overwrite it.
					• Command runs but no token is captured: Let startup finish, check writable game logs, and allow complete fresh output to arrive.
					• Filters interfere: Clear include/exclude filters and let the console respond.
					• API port belongs to another process: Correct the selected server's configured game port. Do not send its token to an unrelated listener.
					• Token rejected or saved connection cannot be unlocked: Complete game-owned setup, then reconnect.
					• API unavailable: Wait for startup or save loading and inspect the game log.

					A fresh response can contain the same token value as an earlier response. Capture uses output produced after the command, not an arbitrary old token in a log.

					PROTECT OR REMOVE ACCESS
					Satisfactory prints administrator tokens in its own console and logs. Keep them private and remove token lines from screenshots and reports.

					Synix verifies that the local endpoint belongs to the selected server and pins its certificate. Do not disable certificate checks to resolve a failure.

					Remove saved token deletes Synix's stored connection; it does not revoke the token at the server.

					To revoke existing application tokens, use server.InvalidateAPITokens in Satisfactory's own server console or Server Manager console. This also affects other integrations using those tokens.

					For another Windows account or moved installation, reconnect if the protected saved connection cannot be used.
					"""),

				["Rust and Rust+ Companion Setup"] = CreateArticle(
					"Games",
					"""
					Choose the Rust profile and review its world, identity, gameplay, query, and companion settings before the first start.

					SERVER IDENTITY
					Synix supplies the supported +server.identity value from the server's identity. Keep separate installations and identities for separate instances so they do not unintentionally share worlds or configuration.

					RUST+
					Configure a unique supported App Port for the companion service, then follow Rust's pairing and network requirements. Its endpoint is separate from player gameplay and optional RCON administration.

					Check the actual configured ports rather than copying a rule from another Rust server. Protect RCON credentials and do not publish them to help people join.

					VANILLA AND OXIDE
					Vanilla uses the official server files. Selecting Oxide adds the supported official framework after the Steam files. Framework installation does not select or vet individual plugins.

					Use the Mod & Plugin Manager's supported local workflow for a detected Oxide/uMod installation. Plugins still need compatible versions and trusted sources.

					Synix reapplies the supported Oxide runtime after Rust updates or validation when that framework is selected. To return to official Vanilla files, change the framework selection and run the supported Update or Validate workflow.

					Before a framework or game change, preserve the world, configuration, and plugin data. Inspect both the game and Oxide logs after restarting; a successful framework download does not prove that every plugin loaded correctly.
					"""),

				["Dune: Awakening Deployment"] = CreateArticle(
					"Games",
					"""
					Dune uses an external deployment system with game-owned virtual machines and battlegroup state. It is not an ordinary single-process game server.

					PREREQUISITES
					Read Synix's readiness checks for system memory, AVX2, hardware virtualization, Hyper-V, and a supported Professional-or-higher Windows edition. Memory needs depend on the battlegroup and maps you run.

					Synix does not silently enable virtualization or install Hyper-V. Complete the game's official prerequisites before deploying.

					INSTALL AND INITIALIZE
					1. Create a Dune: Awakening entry and review its requirements and installation folder.
					2. Save and Start it.
					3. Synix launches the official battlegroup.bat tool with the administrator prompt required by that deployment.
					4. Choose initial-setup in the game's tool and supply your Self-Host Token.
					5. Complete the VM, network, and battlegroup setup in that tool.

					Obtain the Self-Host Token through the official Funcom account portal using its eligibility/setup process:
					https://account.duneawakening.com/

					Keep the token private. Elevation for this deployment tool does not mean every Synix server should run as administrator.

					NETWORKING AND CONTROL LIMITS
					Use the deployment's actual host/VM network layout and required ports. The query-field metadata in Synix represents RMQ, not a normal A2S query listener.

					A closed launcher window does not prove that its VMs stopped. Confirm running state and perform battlegroup operations with the game's deployment controls.

					Synix can install, update, launch, and back up the selected installation folder. Do not assume ordinary process termination, player queries, or watchdog recovery fully controls the external deployment. Launch-file export is not offered for this profile.

					Use game-supported shutdown and backup procedures for persistent VM/database data. A deployment-folder ZIP alone is not a verified consistent backup of it.
					"""),

				["Getting Support and Reporting a Problem"] = CreateArticle(
					"Support",
					"""
					Open Settings → Report a Problem to prepare a report, or use the official support destinations below.

					MAKE THE PROBLEM REPRODUCIBLE
					Include the installation method, the Synix and Windows versions you are actually using, the game/profile, and relevant edition/loader details.

					Describe:
					• What you clicked or changed.
					• What you expected and what happened.
					• The exact error and a short log excerpt near its time.
					• Whether the setup worked before and what changed.

					REVIEW BEFORE SHARING
					Remove passwords, tokens, webhook URLs, personal paths, and network information you do not want public. Privacy Mode does not sanitize every attached file. Do not attach complete logs or game configurations without reviewing them.

					For a translation issue, include the selected language, window name, and exact visible wording. For a game-definition problem, include a redacted installation layout and the relevant game documentation.

					OFFICIAL DESTINATIONS
					Issue reports:
					https://github.com/ubidzz/Synix-Control-Panel/issues

					Community Discord:
					https://discord.gg/WduKEU3j8s

					Project repository:
					https://github.com/ubidzz/Synix-Control-Panel

					A successful application install or build cannot test every game, mod, and network environment. Clear reproduction steps help distinguish a Synix problem from a game or host-setup problem.
					"""),

				["Game Definitions and Contributions"] = CreateArticle(
					"Support",
					"""
					This topic is for advanced users and contributors. You do not need developer tools to create a server from a supported profile.

					HOW DEFINITIONS WORK
					Built-in game definitions describe downloads, executables, Steam AppIDs and login requirements, launch arguments, ports, configuration behavior, prerequisites, and controller capabilities.

					Definitions select supported built-in behavior. They do not dynamically load arbitrary handlers, assemblies, or downloaded scripts. Dropping a JSON file into an installed application folder does not add a new compiled game profile.

					CORRECT OR ADD A PROFILE
					1. Start with official dedicated-server instructions and real generated configuration files.
					2. Use server-root-relative paths where required and exact game-facing identifiers.
					3. Include complete supported templates at their correct installed locations.
					4. Update the appropriate definition/configuration/template revisions.
					5. Validate the library, build with the SDK required by the project, and run relevant tests.
					6. Test installation, start, stop, and monitoring with the actual game before recording verification.

					The Definition Builder is a development-build tool, not a normal release-setup requirement.

					SOURCE ORGANIZATION
					• UI: Feature windows.
					• ServerHandler: Game controllers and server integrations.
					• SynixEngine: Application lifecycle, maintenance, networking, backups, and security.
					• Design: Shared controls, themes, and layout helpers.
					• Database/GameDefinitions: Game profiles and templates.
					• Database/ModSystems: Supported add-on workflows.
					• Localization: Interface catalogs and generators.
					• Tests: Feature-focused checks.

					For translations, update user-facing resources together and preserve placeholders. Do not translate commands, keys, tokens, or output-recognition markers. Help and technical reports remain English.

					Read the License & Proprietary Terms before redistributing code or builds. Keep real credentials and private logs out of contributions and fixtures.
					"""),

				["License & Proprietary Terms"] = new HelpItem("Support",
					"SYNIX CONTROL PANEL — LIMITED PROPRIETARY SOURCE-AVAILABLE LICENSE\n" +
					"\n" +
					"Version 1.0 — August 2, 2026\n" +
					"Copyright © 2026 Jason Turner. All Rights Reserved.\n" +
					"U.S. Copyright Registration Application: Pending\n" +
					"\n" +
					"This license applies to the Synix Control Panel software, including its source code, compiled binaries, user interface, documentation, artwork, logos, and other included materials (collectively, the \"Software\").\n" +
					"\n" +
					"1. OWNERSHIP\n" +
					"\n" +
					"The Software is licensed, not sold.\n" +
					"\n" +
					"Jason Turner retains all right, title, and interest in and to the Software, including all copyrights and other intellectual-property rights. No ownership rights are transferred to you under this license.\n" +
					"\n" +
					"2. LIMITED LICENSE GRANT\n" +
					"\n" +
					"Subject to all terms of this license, you are granted a limited, non-exclusive, non-transferable, non-sublicensable, and revocable license to:\n" +
					"\n" +
					"• Download and use the Software for your own personal, non-commercial purposes.\n" +
					"• View and study the source code.\n" +
					"• Make backup copies for your own personal use.\n" +
					"• Modify the source code for your own personal, non-commercial use.\n" +
					"• Compile and run your personal modifications on devices you own or control.\n" +
					"\n" +
					"Any right not expressly granted by this license is reserved by the copyright holder.\n" +
					"\n" +
					"3. PERSONAL MODIFICATIONS\n" +
					"\n" +
					"You may modify the Software to meet your own personal requirements.\n" +
					"\n" +
					"Personal modifications must remain private unless Jason Turner gives you prior written permission to distribute them.\n" +
					"\n" +
					"You may not publish, upload, share, release, distribute, or make modified source code or modified compiled binaries available to the public as a separate version of Synix.\n" +
					"\n" +
					"4. RESTRICTIONS\n" +
					"\n" +
					"You may not, without prior written permission from Jason Turner:\n" +
					"\n" +
					"4.1 COMMERCIAL USE\n" +
					"\n" +
					"• Sell, rent, lease, license, sublicense, or charge for the Software.\n" +
					"• Sell or distribute modified or unmodified compiled binaries.\n" +
					"• Use the Software or its source code as part of a paid product or commercial service.\n" +
					"• Directly or indirectly profit from distributing, licensing, rebranding, or providing access to the Software.\n" +
					"\n" +
					"4.2 REDISTRIBUTION AND PUBLIC HOSTING\n" +
					"\n" +
					"• Upload, mirror, or host the Software on another website, repository, download service, file-sharing platform, or application store.\n" +
					"• Redistribute the source code or compiled binaries, whether modified or unmodified.\n" +
					"• Create or publish unofficial installers, portable packages, mirrors, or download links.\n" +
					"• Publish releases from a fork or modified copy of the Software.\n" +
					"• Present a fork, modification, or derivative version as an independent or competing product.\n" +
					"\n" +
					"4.3 REBRANDING AND ATTRIBUTION\n" +
					"\n" +
					"• Remove, hide, or alter the Synix name, copyright notices, author credits, license notices, logos, or attribution.\n" +
					"• Rename or rebrand the Software for public release or distribution.\n" +
					"• Claim that you created the original Software.\n" +
					"• Use the Synix name or branding in a way that falsely suggests sponsorship, approval, partnership, or official status.\n" +
					"\n" +
					"4.4 REVERSE ENGINEERING AND PROTECTION BYPASS\n" +
					"\n" +
					"To the maximum extent permitted by applicable law, you may not reverse engineer, decompile, disassemble, or otherwise attempt to bypass licensing notices, attribution, security features, update checks, or branding contained in an official compiled release.\n" +
					"\n" +
					"This restriction does not limit any non-waivable right that applicable law expressly gives you.\n" +
					"\n" +
					"5. GITHUB FORKS AND CONTRIBUTIONS\n" +
					"\n" +
					"The official Synix repository may be publicly visible on GitHub. GitHub's platform may allow users to view and fork a public repository through GitHub's built-in functionality.\n" +
					"\n" +
					"A GitHub fork does not give you permission to:\n" +
					"\n" +
					"• Publish an unofficial Synix release.\n" +
					"• Distribute modified or unmodified binaries.\n" +
					"• Rebrand Synix.\n" +
					"• Operate a separate public download page or mirror.\n" +
					"• Present the fork as your own software.\n" +
					"• Redistribute the Software outside the limited functionality provided by GitHub.\n" +
					"\n" +
					"GitHub forks may be used only to review the code, prepare a contribution, or submit a pull request to the official Synix repository. Personal modifications that are not being submitted as a contribution must remain local and private.\n" +
					"\n" +
					"By submitting source code, documentation, artwork, or another contribution to the official Synix repository, you represent that you have the right to submit it and grant Jason Turner a perpetual, worldwide, non-exclusive, irrevocable, royalty-free license to use, reproduce, modify, publish, distribute, sublicense, and incorporate that contribution into Synix.\n" +
					"\n" +
					"6. SYNIX NAME AND BRANDING\n" +
					"\n" +
					"The Synix name, logo, visual identity, and related branding remain the property of Jason Turner.\n" +
					"\n" +
					"No trademark or branding license is granted by this license except for displaying the unmodified Synix name and branding while personally using the Software.\n" +
					"\n" +
					"Unauthorized use of Synix branding may violate applicable copyright, trademark, unfair-competition, or other laws.\n" +
					"\n" +
					"7. TERMINATION\n" +
					"\n" +
					"Your rights under this license terminate automatically if you violate any term of this license.\n" +
					"\n" +
					"After termination, you must:\n" +
					"\n" +
					"• Stop using and modifying the Software.\n" +
					"• Stop distributing or displaying any unauthorized copy.\n" +
					"• Remove unauthorized public copies, releases, mirrors, or downloads under your control.\n" +
					"• Delete personal copies when legally required to do so.\n" +
					"\n" +
					"Sections concerning ownership, restrictions, contributions, disclaimers, and enforcement survive termination.\n" +
					"\n" +
					"8. ENFORCEMENT\n" +
					"\n" +
					"The copyright holder may pursue any remedies available under applicable law for unauthorized copying, distribution, public hosting, sale, rebranding, or other infringement.\n" +
					"\n" +
					"Available remedies may include removal requests, platform takedown procedures, injunctive relief, damages, and other civil remedies where legally appropriate.\n" +
					"\n" +
					"Nothing in this license guarantees that any particular enforcement action or remedy will be available in every jurisdiction.\n" +
					"\n" +
					"9. DISCLAIMER AND LIMITATION OF LIABILITY\n" +
					"\n" +
					"THE SOFTWARE IS PROVIDED \"AS IS\" AND \"AS AVAILABLE,\" WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, TITLE, AND NON-INFRINGEMENT.\n" +
					"\n" +
					"TO THE MAXIMUM EXTENT PERMITTED BY LAW, JASON TURNER SHALL NOT BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, CONSEQUENTIAL, OR EXEMPLARY DAMAGES, OR FOR ANY LOSS OF DATA, PROFITS, REVENUE, SERVER FILES, GAME WORLDS, BUSINESS, OR SERVICE AVAILABILITY ARISING FROM OR RELATED TO THE SOFTWARE OR ITS USE.\n" +
					"\n" +
					"You are responsible for maintaining backups and verifying that the Software is suitable for your system and game-server environment.\n" +
					"\n" +
					"10. THIRD-PARTY COMPONENTS\n" +
					"\n" +
					"Third-party software, libraries, game-server files, SteamCMD components, trademarks, and other materials are owned by their respective owners and remain subject to their own licenses and terms.\n" +
					"\n" +
					"This license applies only to the portions of Synix owned by Jason Turner.\n" +
					"\n" +
					"11. WRITTEN PERMISSION\n" +
					"\n" +
					"Exceptions to this license are valid only when provided in writing by Jason Turner.\n" +
					"\n" +
					"Questions or permission requests should be submitted through the official Synix GitHub repository or another official Synix contact method."),

				["Donate & Support Development"] = new HelpItem("Support",
					"SUPPORT THE PROJECT:\n\n" +
					"Synix Control Panel is developed with passion for the server hosting community. \nYour support keeps updates frequent and features growing!\n\n" +
					"Click the button below or scan the QR code to open the official PayPal \ndonation page securely in your browser.")
			};

			return articles;
		}

		private static HelpItem CreateArticle(string category, string answer)
		{
			return new HelpItem(category,
				answer.ReplaceLineEndings("\n").Trim());
		}

		private void PopulateTree(string filter = "")
		{
			string normalizedFilter = filter.Trim();
			string? selectedTopic = treeNavigation.SelectedNode?.Tag as string;
			TreeNode? nodeToReselect = null;
			TreeNode? firstTopicNode = null;
			_visibleArticleCount = 0;

			treeNavigation.BeginUpdate();
			try
			{
				treeNavigation.Nodes.Clear();

				foreach ((string categoryKey, string displayName, string index)
					in CategoryDefinitions)
				{
					TreeNode categoryNode = new(displayName)
					{
						Name = categoryKey,
						ToolTipText = displayName
					};

					foreach (KeyValuePair<string, HelpItem> entry in _helpData)
					{
						if (!string.Equals(
							entry.Value.Category,
							categoryKey,
							StringComparison.OrdinalIgnoreCase) ||
							!MatchesFilter(entry, normalizedFilter))
						{
							continue;
						}

						TreeNode topicNode = new(CreateNavigationCaption(entry.Key))
						{
							Tag = entry.Key,
							ToolTipText = entry.Key
						};
						categoryNode.Nodes.Add(topicNode);
						firstTopicNode ??= topicNode;
						_visibleArticleCount++;

						if (string.Equals(
							entry.Key,
							selectedTopic,
							StringComparison.OrdinalIgnoreCase))
						{
							nodeToReselect = topicNode;
						}
					}

					if (categoryNode.Nodes.Count == 0)
					{
						continue;
					}

					treeNavigation.Nodes.Add(categoryNode);
					categoryNode.Expand();
				}

				if (nodeToReselect != null)
				{
					treeNavigation.SelectedNode = nodeToReselect;
				}
				else if (!string.IsNullOrEmpty(normalizedFilter) &&
					firstTopicNode != null)
				{
					treeNavigation.SelectedNode = firstTopicNode;
					firstTopicNode.EnsureVisible();
				}
			}
			finally
			{
				treeNavigation.EndUpdate();
			}

			UpdateSearchStatus(normalizedFilter);

			if (_visibleArticleCount == 0)
			{
				ShowNoResults(normalizedFilter);
			}
			else if (string.IsNullOrEmpty(normalizedFilter) &&
				selectedTopic == null)
			{
				ShowWelcome();
			}
		}

		internal static bool MatchesFilter(
			KeyValuePair<string, HelpItem> entry,
			string filter)
		{
			return string.IsNullOrEmpty(filter) ||
				entry.Key.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
				entry.Value.Answer.Contains(filter, StringComparison.OrdinalIgnoreCase);
		}

		private static string CreateNavigationCaption(string topicTitle)
		{
			const int MaximumCaptionLength = 34;
			return topicTitle.Length <= MaximumCaptionLength
				? topicTitle
				: topicTitle[..(MaximumCaptionLength - 1)] + "…";
		}

		private void UpdateSearchStatus(string filter)
		{
			lblArticleCount.Text = string.IsNullOrEmpty(filter)
				? $"{_visibleArticleCount} help articles"
				: $"{_visibleArticleCount} matching article" +
					(_visibleArticleCount == 1 ? string.Empty : "s");
			lblFooterStatus.Text = string.IsNullOrEmpty(filter)
				? "KNOWLEDGE BASE READY"
				: $"{_visibleArticleCount} SEARCH RESULT" +
					(_visibleArticleCount == 1 ? string.Empty : "S");
			btnClearSearch.Visible = txtSearch.TextLength > 0;
		}

		private void ShowWelcome()
		{
			qrCard.Visible = false;
			lblTopicCategory.Text = "SYNIX KNOWLEDGE BASE";
			lblTopicTitle.Text = "How can we help?";
			lblArticleBadge.Text = "WELCOME";
			lblAnswer.Text = WelcomeText;
			ResetAnswerScroll();
		}

		private void ShowNoResults(string filter)
		{
			qrCard.Visible = false;
			lblTopicCategory.Text = "SEARCH";
			lblTopicTitle.Text = "No matching help articles";
			lblArticleBadge.Text = "NO RESULTS";
			lblAnswer.Text = string.IsNullOrWhiteSpace(filter)
				? "No help articles are currently available."
				: $"Synix could not find a topic containing \"{filter}\".\n\n" +
					"Try a shorter phrase or search for a related term such as " +
					"Satisfactory, token, Minecraft, import, transfer, backup, ports, or SteamCMD.";
			ResetAnswerScroll();
		}

		private void ShowTopic(string topicKey, HelpItem item)
		{
			bool isDonationTopic = string.Equals(
				topicKey,
				"Donate & Support Development",
				StringComparison.OrdinalIgnoreCase) ||
				string.Equals(
					topicKey,
					"Donate",
					StringComparison.OrdinalIgnoreCase);

			lblTopicCategory.Text =
				GetCategoryDisplayName(item.Category).ToUpperInvariant();
			lblTopicTitle.Text = topicKey;
			lblArticleBadge.Text = isDonationTopic ? "SUPPORT" : "ARTICLE";
			lblAnswer.Text = item.Answer;
			qrCard.Visible = isDonationTopic;
			btnDonateAction.Visible = isDonationTopic;
			if (isDonationTopic)
			{
				qrCard.BringToFront();
				btnDonateAction.BringToFront();
			}

			lblFooterStatus.Text = "VIEWING HELP ARTICLE";
			ResetAnswerScroll();
			RevealAnswerText(txtSearch.Text.Trim());
		}

		private void RevealAnswerText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return;

			int position = lblAnswer.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase);
			if (position < 0)
				return;

			lblAnswer.Select(position, text.Length);
			lblAnswer.ScrollToCaret();
		}


		private void ResetAnswerScroll()
		{
			lblAnswer.SelectionStart = 0;
			lblAnswer.SelectionLength = 0;
			lblAnswer.ScrollToCaret();
		}

		private static string GetCategoryDisplayName(string categoryKey)
		{
			foreach ((string key, string displayName, string index)
				in CategoryDefinitions)
			{
				if (string.Equals(
					key,
					categoryKey,
					StringComparison.OrdinalIgnoreCase))
				{
					return displayName;
				}
			}

			return "Help & Support";
		}

		private static string GetCategoryIndex(string categoryKey)
		{
			foreach ((string key, string displayName, string index)
				in CategoryDefinitions)
			{
				if (string.Equals(
					key,
					categoryKey,
					StringComparison.OrdinalIgnoreCase))
				{
					return index;
				}
			}

			return "•";
		}

		private void treeNavigation_AfterSelect(
			object? sender,
			TreeViewEventArgs eventArgs)
		{
			if (eventArgs.Node?.Tag is not string topicKey ||
				!_helpData.TryGetValue(topicKey, out HelpItem? item) ||
				item == null)
			{
				return;
			}

			ShowTopic(topicKey, item);
			treeNavigation.Invalidate();
		}

		private void treeNavigation_NodeMouseClick(
			object? sender,
			TreeNodeMouseClickEventArgs eventArgs)
		{
			TreeNode? node = eventArgs.Node;
			if (node == null || node.Level != 0)
			{
				return;
			}

			if (node.IsExpanded)
			{
				node.Collapse();
			}
			else
			{
				node.Expand();
			}

			treeNavigation.Invalidate();
		}

		private void treeNavigation_DrawNode(
			object? sender,
			DrawTreeNodeEventArgs eventArgs)
		{
			TreeNode? node = eventArgs.Node;
			Graphics? graphics = eventArgs.Graphics;
			if (node == null || graphics == null)
				return;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;

			Rectangle rowBounds = new(
				4,
				eventArgs.Bounds.Y + 2,
				Math.Max(1, treeNavigation.ClientSize.Width - 8),
				Math.Max(1, eventArgs.Bounds.Height - 4));
			using SolidBrush sidebarBrush = new(SettingsPalette.Sidebar);
			graphics.FillRectangle(sidebarBrush, rowBounds);

			if (node.Level == 0)
			{
				DrawCategoryNode(graphics, node, rowBounds);
				return;
			}

			bool selected = ReferenceEquals(treeNavigation.SelectedNode, node);
			if (selected)
			{
				using GraphicsPath selectedPath = CreateRoundedRectangle(rowBounds, 8);
				using SolidBrush selectedBrush = new(SettingsPalette.AccentSoft);
				graphics.FillPath(selectedBrush, selectedPath);

				using SolidBrush accentBrush = new(SettingsPalette.Accent);
				graphics.FillRectangle(
					accentBrush,
					rowBounds.Left,
					rowBounds.Top + 7,
					3,
					Math.Max(4, rowBounds.Height - 14));
			}

			Rectangle textBounds = new(
				rowBounds.Left + 34,
				rowBounds.Top,
				Math.Max(0, rowBounds.Width - 44),
				rowBounds.Height);
			string topicTitle = node.Tag as string ?? node.Text;
			TextRenderer.DrawText(
				graphics,
				topicTitle,
				treeNavigation.Font,
				textBounds,
				selected ? SettingsPalette.PrimaryText : SettingsPalette.SecondaryText,
				TextFormatFlags.Left |
				TextFormatFlags.VerticalCenter |
				TextFormatFlags.EndEllipsis |
				TextFormatFlags.NoPrefix);
		}

		private void DrawCategoryNode(
			Graphics graphics,
			TreeNode node,
			Rectangle rowBounds)
		{
			string categoryIndex = GetCategoryIndex(node.Name);
			Rectangle indexBounds = new(
				rowBounds.Left + 10,
				rowBounds.Top,
				24,
				rowBounds.Height);
			TextRenderer.DrawText(
				graphics,
				categoryIndex,
				lblSidebarEyebrow.Font,
				indexBounds,
				SettingsPalette.Accent,
				TextFormatFlags.Left |
				TextFormatFlags.VerticalCenter |
				TextFormatFlags.NoPrefix);

			Rectangle textBounds = new(
				rowBounds.Left + 40,
				rowBounds.Top,
				Math.Max(0, rowBounds.Width - 70),
				rowBounds.Height);
			TextRenderer.DrawText(
				graphics,
				node.Text,
				lblSidebarEyebrow.Font,
				textBounds,
				SettingsPalette.PrimaryText,
				TextFormatFlags.Left |
				TextFormatFlags.VerticalCenter |
				TextFormatFlags.EndEllipsis |
				TextFormatFlags.NoPrefix);

			int centerX = rowBounds.Right - 17;
			int centerY = rowBounds.Top + (rowBounds.Height / 2);
			using Pen arrowPen = new(SettingsPalette.MutedText, 1.5F)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round
			};
			if (node.IsExpanded)
			{
				graphics.DrawLine(arrowPen, centerX - 4, centerY - 2, centerX, centerY + 2);
				graphics.DrawLine(arrowPen, centerX, centerY + 2, centerX + 4, centerY - 2);
			}
			else
			{
				graphics.DrawLine(arrowPen, centerX - 2, centerY - 4, centerX + 2, centerY);
				graphics.DrawLine(arrowPen, centerX + 2, centerY, centerX - 2, centerY + 4);
			}
		}

		private static GraphicsPath CreateRoundedRectangle(
			Rectangle bounds,
			int radius)
		{
			GraphicsPath path = new();
			int diameter = Math.Min(
				radius * 2,
				Math.Min(bounds.Width, bounds.Height));
			if (diameter <= 1)
			{
				path.AddRectangle(bounds);
				return path;
			}

			Rectangle arc = new(bounds.X, bounds.Y, diameter, diameter);
			path.AddArc(arc, 180, 90);
			arc.X = bounds.Right - diameter;
			path.AddArc(arc, 270, 90);
			arc.Y = bounds.Bottom - diameter;
			path.AddArc(arc, 0, 90);
			arc.X = bounds.Left;
			path.AddArc(arc, 90, 90);
			path.CloseFigure();
			return path;
		}

		private void txtSearch_TextChanged(object? sender, EventArgs eventArgs)
		{
			PopulateTree(txtSearch.Text);
		}

		private void btnClearSearch_Click(object? sender, EventArgs eventArgs)
		{
			txtSearch.Clear();
			txtSearch.Focus();
		}

		private void lblAnswer_LinkClicked(
			object? sender,
			LinkClickedEventArgs eventArgs)
		{
			try
			{
				if (!Uri.TryCreate(
					eventArgs.LinkText,
					UriKind.Absolute,
					out Uri? linkUri) ||
					(linkUri.Scheme != Uri.UriSchemeHttp &&
						linkUri.Scheme != Uri.UriSchemeHttps))
				{
					throw new InvalidOperationException(
						"Only web links can be opened from the help center.");
				}


				Process.Start(new ProcessStartInfo
				{
					FileName = linkUri.AbsoluteUri,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					this,
					"Could not launch external web link: " + ex.Message,
					"Synix Link Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		private void btnMinimize_Click(object? sender, EventArgs eventArgs)
		{
			WindowState = FormWindowState.Minimized;
		}

		private void btnClose_Click(object? sender, EventArgs eventArgs)
		{
			Close();
		}

		private void TitleBar_MouseDown(
			object? sender,
			MouseEventArgs eventArgs)
		{
			if (eventArgs.Button != MouseButtons.Left)
			{
				return;
			}

			_ = ReleaseCapture();
			_ = SendMessage(Handle, WmNcLeftButtonDown, HtCaption, 0);
		}

		protected override void OnHandleCreated(EventArgs eventArgs)
		{
			base.OnHandleCreated(eventArgs);

			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
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

		protected override bool ProcessCmdKey(
			ref Message message,
			Keys keyData)
		{
			if (keyData == (Keys.Control | Keys.F))
			{
				txtSearch.Focus();
				txtSearch.SelectAll();
				return true;
			}

			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}

			return base.ProcessCmdKey(ref message, keyData);
		}

		[DllImport("user32.dll")]
		private static extern uint SetWindowDisplayAffinity(
			IntPtr windowHandle,
			uint affinity);

		[DllImport("user32.dll")]
		private static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(
			IntPtr windowHandle,
			int message,
			int wordParameter,
			int longParameter);

		[DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
		private static extern IntPtr SendMessageText(
			IntPtr windowHandle,
			int message,
			IntPtr wordParameter,
			string text);

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(
			IntPtr windowHandle,
			int attribute,
			ref int attributeValue,
			int attributeSize);
	}

	public class HelpItem
	{
		public string Category { get; set; }
		public string Answer { get; set; }

		public HelpItem(string category, string answer)
		{
			Category = category;
			Answer = answer;
		}
	}
}
