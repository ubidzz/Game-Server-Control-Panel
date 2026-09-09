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

$semanticTranslations = [ordered]@{
    'Language.English' = "Englisch"
    'Language.French' = "Französisch"
    'Language.German' = "Deutsch"
    'Language.Spanish' = "Spanisch"
    'Option.DownloadSpeed.Unlimited' = "Unbegrenzt"
    'Option.DownloadSpeed.Limited' = "Begrenzt"
    'Message.AlreadyRunning.Body' = "Synix wird bereits ausgeführt. Verwende das vorhandene Synix-Fenster."
    'Message.AlreadyRunning.Title' = "Synix wird bereits ausgeführt"
    'Settings.VersionLabel' = "SYNIX-STEUERUNG  •  v{0}"
    'SettingsPage.General.Heading' = "Allgemein"
    'SettingsPage.General.Subtitle' = "Konfiguriere das grundlegende Verhalten von Synix auf diesem Computer."
    'SettingsPage.Backups.Heading' = "Sicherungen"
    'SettingsPage.Backups.Subtitle' = "Verwalte Serversicherungen oder verschiebe Synix auf einen anderen Computer."
    'SettingsPage.Privacy.Heading' = "Datenschutz und Sicherheit"
    'SettingsPage.Privacy.Subtitle' = "Lege fest, wie vertrauliche Serverinformationen angezeigt werden."
    'SettingsPage.Advanced.Heading' = "Erweitert"
    'SettingsPage.Advanced.Subtitle' = "Konfiguriere erhöhte Vorgänge und erweitertes Systemverhalten."
    'SettingsPage.ReportProblem.Heading' = "Problem melden"
    'SettingsPage.ReportProblem.Subtitle' = "Erstelle einen datenschutzgefilterten Kompatibilitätsbericht für den Synix-Support."
    'SettingsPage.Development.Heading' = "Entwicklung"
    'SettingsPage.Development.Subtitle' = "Verwalte Konfigurationserfassung und Werkzeuge für Veröffentlichungstests."
    'Menu.ModPluginManager' = "Mod- und Plugin-Manager"
    'Menu.PlayerManagementCenter' = "Spielerverwaltung"
    'Menu.MinecraftServerConsole' = "Minecraft-Serverkonsole"
    'Menu.ConnectionInformation' = "Verbindungsinformationen"
    'Menu.LiveProcessDetails' = "Live-Prozessdetails"
    'Option.Status.All' = "Alle Status"
    'Option.Status.Running' = "Wird ausgeführt"
    'Option.Status.Stopped' = "Gestoppt"
    'Option.Status.InProgress' = "In Bearbeitung"
    'Option.Status.NeedsAttention' = "Aktion erforderlich"
    'Option.Discord.AllEvents' = "Alle Ereignisse"
    'Option.Discord.ServerStatus' = "Serverstatus"
    'Option.Discord.Maintenance' = "Wartung"
    'Option.Discord.ProblemsOnly' = "Nur Probleme"
    'Option.Discord.Custom' = "Benutzerdefiniert"
    'Option.ConfigType.All' = "Alle Typen"
    'Option.ConfigType.Text' = "TEXT"
    'Option.ConfigType.Number' = "ZAHL"
    'Option.ConfigType.Boolean' = "BOOLESCH"
    'Option.ConfigType.Secret' = "GEHEIM"
    'Option.ConfigType.Null' = "NULL"
    'Option.VerificationFilter.NeedsWork' = "Überarbeitung nötig"
    'Option.VerificationFilter.UnknownConfiguration' = "Unbekannte Konfiguration"
    'Option.VerificationFilter.PartiallyVerified' = "Teilweise verifiziert"
    'Option.VerificationFilter.FullyVerified' = "Vollständig verifiziert"
    'Option.VerificationFilter.AllGames' = "Alle Spiele"
    'VerificationStep.Install' = "Installation"
    'VerificationStep.Start' = "Start"
    'VerificationStep.Stop' = "Stopp"
    'VerificationStep.Monitoring' = "Überwachung"
    'VerificationStep.Arguments' = "Argumente"
    'VerificationStep.Configuration' = "Konfiguration"
    'Status.Stopped' = "Gestoppt"
    'Status.Running' = "Wird ausgeführt"
    'Status.Starting' = "Wird gestartet"
    'Status.Crashed' = "Abgestürzt"
    'Status.Stopping' = "Wird gestoppt"
    'Status.Installing' = "Wird installiert"
    'Status.Updating' = "Wird aktualisiert"
    'Status.BackingUp' = "Wird gesichert"
    'Status.Validating' = "Wird validiert"
    'Status.Exporting' = "Wird exportiert"
    'Status.Restoring' = "Wird wiederhergestellt"
    'Status.Deleting' = "Wird gelöscht"
    'Status.Unknown' = "Unbekannt"
    'Dashboard.ServerCount.One' = "{0} Server"
    'Dashboard.ServerCount.Many' = "{0} Server"
    'Dashboard.ServerCount.Filtered' = "{0} von {1} Servern"
    'Dashboard.Network.PublicFetching' = "Öffentliche IP: wird abgerufen…"
    'Dashboard.Network.LocalFetching' = "LAN-IP: wird abgerufen…"
    'Dashboard.Network.PublicAddress' = "Öffentliche IP: {0}"
    'Dashboard.Network.LocalAddress' = "LAN-IP: {0}"
    'Dashboard.Network.PublicHidden' = "Öffentliche IP: [AUSGEBLENDET]"
    'Dashboard.Network.LocalHidden' = "LAN-IP: [AUSGEBLENDET]"
    'Dashboard.CpuGaugeLabel' = "CPU %"
    'Dashboard.RamGaugeLabel' = "RAM GB"
    'Dashboard.CpuValue' = "{0:0.0} %"
    'Dashboard.RamValue' = "{0:0.00} GB"
    'ServerSetup.Status.Ready' = "●  Speicherbereit"
    'ServerSetup.Status.ActionRequired' = "●  Aktion erforderlich"
    'ServerSetup.Status.AllChecksPassed' = "Alle erforderlichen Prüfungen bestanden"
    'ServerSetup.Status.SeeValidationMessage' = "Beachte die Validierungsmeldung unten"
    'ServerSetup.Completion' = "Einrichtung: {0} %"
    'ServerSetup.Window.EditTitle' = "Server bearbeiten"
    'ServerSetup.ModeBadge.Edit' = "SERVER BEARBEITEN"
    'ServerSetup.Page.Security.Description' = "Verwalten Sie Serverpasswörter und Zugangsdaten für Onlinedienste."
    'ServerSetup.Page.World.Description' = "Konfigurieren Sie Welt-Seed, Größe und spielspezifische Weltoptionen."
    'ServerSetup.Page.Network.Description' = "Weisen Sie Dienstports zu und sichern Sie die Remoteverwaltung."
    'ServerSetup.Page.Network.BeginnerDescription' = "Verwenden Sie die empfohlenen Spiel- und Abfrageports. Der erweiterte Modus fügt RCON-Steuerelemente hinzu."
    'ServerSetup.Page.Automation.Description' = "Steuern Sie Startaufgaben, geplante Neustarts, Sicherungen und Warnungen."
    'ServerSetup.Page.Discord.Title' = "Discord-Benachrichtigungen"
    'ServerSetup.Page.Discord.Description' = "Verwenden Sie einen Haupt-Webhook oder leiten Sie verschiedene Synix-Ereignisse an mehrere Discord-Kanäle weiter."
    'ServerSetup.Page.Install.Description' = "Wählen Sie den Serverspeicher und passen Sie die Startargumente an."
    'ServerSetup.Page.Install.BeginnerDescription' = "Wählen Sie den Installationsort des Servers. Synix stellt die empfohlenen Starteinstellungen bereit."
    'ServerSetup.Mode.Advanced' = "Modus: Erweitert"
    'ServerSetup.Mode.Beginner' = "Modus: Einsteiger"
    'ServerSetup.Mode.Advanced.AccessibleName' = "Erweiterter Servereinrichtungsmodus. Klicken Sie, um den Einsteigermodus zu verwenden."
    'ServerSetup.Mode.Beginner.AccessibleName' = "Einsteiger-Servereinrichtungsmodus. Klicken Sie, um erweiterte Einstellungen anzuzeigen."
    'ServerSetup.Navigation.AttentionRequired' = "{0} enthält Einstellungen, die vor dem Speichern bearbeitet werden müssen."
    'ServerSetup.Navigation.NoAttentionRequired' = "{0} enthält keine Einstellungen, die bearbeitet werden müssen."
    'ServerSetup.Validation.Waiting' = "Die Validierung wartet auf die erforderlichen Serverinformationen."
    'ServerSetup.Validation.ServerNameAndGameRequired' = "  🔒 [ERFORDERLICH] Geben Sie einen Servernamen ein und wählen Sie eine Spielvorlage."
    'ServerSetup.Validation.ServerNameRequired' = "  🔒 [ERFORDERLICH] Geben Sie einen Servernamen ein, bevor der Server gespeichert werden kann."
    'ServerSetup.Validation.GameRequired' = "  🔒 [ERFORDERLICH] Wählen Sie eine Spielvorlage, bevor der Server gespeichert werden kann."
    'ServerSetup.Validation.MinecraftLoading' = "  ◌ [MINECRAFT] Kompatible Versionen und Java-Anforderungen werden geladen…"
    'ServerSetup.Validation.MinecraftDetail' = "  ⚠️ [MINECRAFT] {0}"
    'ServerSetup.Validation.MinecraftVersionRequired' = "  🔒 [MINECRAFT] Wählen Sie eine Minecraft-Spielversion."
    'ServerSetup.Validation.AdminPasswordRequired' = "  🔒 [ERFORDERLICH] Geben Sie ein Administratorpasswort ein, um die Serveradministratorrolle zu schützen."
    'ServerSetup.Validation.AuthenticationTokenRequired' = "  🔒 [ERFORDERLICH] Geben Sie das erforderliche Feld {0} ein, bevor der Server gespeichert werden kann."
    'ServerSetup.Validation.RequiredDetail' = "  🔒 [ERFORDERLICH] {0}"
    'ServerSetup.Validation.MinecraftLoaderRequired' = "  🔒 [MINECRAFT] Es ist kein kompatibler Loader-Build ausgewählt."
    'ServerSetup.Validation.RequirementDetail' = "  ⚠️ [ANFORDERUNG] {0}"
    'ServerSetup.Validation.NameConflict' = '  ⚠️ [KONFLIKT] Der Name „{0}“ wird bereits für {1} verwendet.'
    'ServerSetup.Validation.ScheduleDayRequired' = "  🔒 [ERFORDERLICH] Wählen Sie mindestens einen Tag für den automatischen Neustartzeitplan."
    'ServerSetup.Validation.InstallFolderRequired' = "  🔒 [ERFORDERLICH] Wählen Sie einen Installationsordner oder aktivieren Sie den Standardinstallationspfad."
    'ServerSetup.Validation.LaunchDetail' = "  ⚠️ [START] {0}"
    'ServerSetup.Validation.DiscordDetail' = "  🔒 [DISCORD] {0}"
    'ServerSetup.Validation.ReadyNote' = "  ✔ [BEREIT] HINWEIS: {0}"
    'ServerSetup.Validation.Updating' = "  ✔ [BEREIT] Aktualisierung: {0}"
    'ServerSetup.Validation.Ready' = "  ✔ [BEREIT] Die Konfiguration ist gültig und sicher."
    'ServerSetup.Validation.Error' = "  ⚠️ [VALIDIERUNGSFEHLER] Die Validierung konnte nicht abgeschlossen werden: {0}"
    'ServerSetup.Validation.DuplicatePort' = "  ⚠️ [KONFLIKT] {0} können nicht denselben Port {1} verwenden."
    'ServerSetup.Validation.PortBlocked' = "  ⚠️ [KONFLIKT] {0} {1} wird blockiert durch: {2}"
    'ServerSetup.ConfigurationSupport' = "◇  KONFIGURATIONSUNTERSTÜTZUNG: {0}  •  {1}"
    'ServerSetup.PortMapping.SelectGame' = "Wählen Sie ein Spiel, um die verwalteten Portzuordnungen anzuzeigen."
    'ServerSetup.PortMapping.AllMapped' = "Alle angegebenen Ports werden durch Argumente oder die Konfiguration zugeordnet."
    'ServerSetup.PortMapping.NeedsMapping' = "Zuordnung erforderlich: {0} (Argumente oder Konfigurationsvorlage)."
    'ServerSetup.Port.Ipv6' = "IPv6-Port"
    'ServerSetup.Port.SystemProcess' = "Systemprozess"
    'ServerSetup.List.AndSeparator' = " und "
    'ServerSetup.Credentials.UnlockFailed.Title' = "Serverzugangsdaten erneut eingeben"
    'ServerSetup.Credentials.UnlockFailed.Body' = "Synix konnte die gespeicherten Passwörter, das Authentifizierungstoken oder die Discord-Webhooks dieses Servers nicht entsperren. Sie stammen möglicherweise von einem anderen Windows-Benutzer oder Computer.`n`nGeben Sie die Zugangsdaten erneut ein und klicken Sie auf Änderungen speichern, um sie für diesen Windows-Benutzer zu schützen."
    'ServerSetup.Dialog.SettingsAttention.Title' = "Servereinstellungen erfordern Aufmerksamkeit"
    'ServerSetup.Dialog.ExtraArgumentsBlocked.Title' = "Zusätzliche Argumente blockiert"
    'ServerSetup.Dialog.DiscordAttention.Title' = "Discord-Einstellungen erfordern Aufmerksamkeit"
    'ServerSetup.Dialog.IllegalInput.Title' = "Eingabe blockiert"
    'ServerSetup.Dialog.IllegalInput.Body' = "Sicherheitswarnung: Eine Ihrer Eingaben enthält unzulässige Zeichen."
    'ServerSetup.ErrorAction.SaveMode' = "den Einrichtungsmodus speichern"
    'ServerSetup.ErrorAction.SaveSettings' = "die Servereinstellungen speichern"
    'ServerSetup.ErrorAction.OpenTokenPage' = "die Seite für das Authentifizierungstoken öffnen"
    'ServerSetup.GamePicker.Placeholder' = "-- Spiel auswählen --"
    'ServerSetup.Placeholder.SelectGame' = "Spiel auswählen…"
    'ServerSetup.Automation.EnableSchedule.AccessibleName' = "Zeitplan aktivieren"
    'ServerSetup.Install.DefaultFolder.AccessibleName' = "Standardordner"
    'ServerSetup.Verification.LastTested.Verified' = "Zuletzt getestete Synix-Version: v{0}  •  {1:d}"
    'ServerSetup.Verification.Verified' = "{0}  ✓ Verifiziert"
    'ServerSetup.Verification.Unverified' = "{0}  — Noch nicht verifiziert"
    'ServerSetup.Minecraft.MetadataLoadFailed' = "Metadaten konnten nicht geladen werden: {0}"
    'ServerSetup.Minecraft.MojangVersionsLoadFailed' = "Mojang-Versionen konnten nicht geladen werden: {0}"
    'ServerSetup.Minecraft.LoadingBuilds' = "Kompatible Builds werden geladen…"
    'ServerSetup.Minecraft.JavaVersion' = "Java {0}"
    'ServerSetup.Minecraft.Helper.Vanilla' = "Synix installiert den offiziellen Server und das passende portable Java."
    'ServerSetup.Minecraft.Helper.Loader' = "Synix installiert den kompatiblen {0}-Serverloader. Fügen Sie nach der Installation eigene Mods hinzu."
    'ServerSetup.Minecraft.NoCompatibleBuild' = "Für Minecraft {1} ist kein kompatibler {0}-Serverbuild verfügbar."
    'ServerSetup.Minecraft.Helper.ResolvedVanilla' = "Minecraft {0} verwendet den offiziellen Mojang-Server und Java {3}."
    'ServerSetup.Minecraft.Helper.ResolvedLoader' = "Minecraft {0} + {1} {2} verwendet Java {3}. Fügen Sie nach der Installation Mods hinzu."
    'ServerSetup.Minecraft.RetryDetail' = "{0} Wählen Sie die Version oder den Loader erneut aus, um es noch einmal zu versuchen."
    'ServerSetup.Runtime.ServerPackage' = "Serverpaket"
    'ServerSetup.Runtime.OfficialBedrock' = "Offizielles Bedrock"
    'ServerSetup.MaxPlayers.Limited' = "Max. Spieler (höchstens {0:0})"
    'ServerSetup.SteamAccount.Restore.WindowTitle' = "Steam-Autorisierung wiederherstellen"
    'ServerSetup.SteamAccount.Restore.Title' = "Steam-Autorisierung wiederherstellen"
    'ServerSetup.SteamAccount.Restore.Description' = "{0} wurde auf diesen PC importiert. Bestätigen Sie den Steam-Kontonamen, damit SteamCMD den Zugriff vor dem ersten Start wiederherstellen kann."
    'ServerSetup.SteamAccount.Required.Description' = "{0} benötigt für die Installation ein Steam-Konto. Geben Sie den Kontonamen ein, den SteamCMD verwenden soll."
    'ProblemAction.ServerInstallation' = "Serverinstallation"
    'ProblemAction.UpdateValidation' = "Serveraktualisierung oder Dateiprüfung"
    'ProblemAction.ServerStartup' = "Serverstart"
    'ProblemAction.ServerShutdown' = "Serverstopp"
    'ProblemAction.RestartWatchdog' = "Serverneustart oder Überwachung"
    'ProblemAction.IncorrectStatus' = "Falscher Serverstatus"
    'ProblemAction.ResourceMonitoring' = "CPU-, Speicher- oder Spielerüberwachung"
    'ProblemAction.LocalNetwork' = "Lokale Netzwerkverbindung"
    'ProblemAction.PublicNetwork' = "Internet- oder öffentliche Verbindung"
    'ProblemAction.PortsFirewallRcon' = "Ports, Firewall oder RCON"
    'ProblemAction.ServerBackups' = "Serversicherungen"
    'ProblemAction.TransferExport' = "Transferexport"
    'ProblemAction.TransferImport' = "Transferimport"
    'ProblemAction.TransferVerification' = "Prüfung des Transferpakets"
    'ProblemAction.SettingsPasswords' = "Servereinstellungen oder Passwörter"
    'ProblemAction.DiscordAlerts' = "Discord-Warnungen"
    'ProblemAction.SynixUpdate' = "Synix-Aktualisierung"
    'ProblemAction.InstallationPackaging' = "MSI-, WinGet- oder eigenständige Installation"
    'ProblemAction.WindowDisplay' = "Fenster- oder Anzeigeproblem"
    'ProblemAction.CrashFreeze' = "Synix-Absturz oder Einfrieren"
    'ProblemAction.TemplateLaunch' = "Servervorlage oder Startverhalten"
    'ProblemAction.Other' = "Sonstiges"
    'Report.EnglishRequiredWarning' = "Wichtig: Verfasse die Zusammenfassung und Berichtsdetails auf Englisch, damit das Synix-Supportteam sie prüfen kann."
    'Advanced.Firewall.ButtonChecking' = "Firewall wird geprüft…"
    'Advanced.Firewall.CheckingPaths' = "Programmpfade der Windows-Firewall werden geprüft…"
    'Advanced.Firewall.Canceled' = "Bereinigung abgebrochen. Es wurden keine Firewallregeln geändert."
    'Advanced.Firewall.WaitingForAdmin' = "Warte auf Administratorberechtigung…"
    'Advanced.Firewall.RemovedVerified' = "{0} verwaiste Programmpfade wurden entfernt und überprüft."
    'Advanced.Firewall.NoneFound' = "Im standardmäßigen Synix-Spieleordner wurden keine verwaisten Firewallregeln gefunden."
    'Advanced.Background.EnabledCurrent' = "Für die Windows-Anmeldung aktiviert — Schließen beendet Synix weiterhin vollständig."
    'Advanced.Background.DisabledCurrent' = "Deaktiviert — geplante Aufgaben laufen nur, solange Synix geöffnet ist."
    'Advanced.Background.EnabledResult' = "Für die Windows-Anmeldung aktiviert. Beim Schließen werden weiterhin alle Synix-Prozesse der aktuellen Sitzung beendet."
    'Advanced.Background.DisabledResult' = "Deaktiviert. Die Hintergrundüberwachung wird beendet und bei der Anmeldung nicht gestartet."
    'AddServer.Title' = "Server hinzufügen"
    'AddServer.Heading' = "Wie möchtest du einen Server hinzufügen?"
    'AddServer.Subtitle' = "Synix kann einen neuen Server installieren oder bereits vorhandene Dateien sicher registrieren."
    'AddServer.Create.Title' = "Neuen Server erstellen und installieren"
    'AddServer.Create.Description' = "Wähle Spiel und Einstellungen aus; Synix lädt anschließend die Serverdateien herunter."
    'AddServer.Create.Button' = "Neu erstellen"
    'AddServer.Import.Title' = "Vorhandenen Server importieren"
    'AddServer.Import.Description' = "Wähle einen vorhandenen Serverordner. Deine Dateien werden weder verschoben noch ersetzt."
    'AddServer.Import.Button' = "Vorhandenen importieren"
    'AddServer.Catalog.Title' = "Spielunterstützung zuerst prüfen"
    'AddServer.Catalog.Description' = "Durchsuche den Katalog nach Unterstützung für Programm, Konfiguration, Crossplay und Spielerabfragen."
    'AddServer.Catalog.Button' = "Katalog anzeigen"
    'Connection.Heading' = "Mit {0} verbinden"
    'Connection.Subtitle' = "Verwende die Adresse, die zum Standort des Spielers passt."
    'Connection.Local.Title' = "Derselbe Computer oder das Heimnetzwerk"
    'Connection.Local.Description' = "Für Spieler verwenden, die mit demselben Router verbunden sind."
    'Connection.Public.Title' = "Freunde verbinden sich über das Internet"
    'Connection.Public.Description' = "Router und Windows-Firewall müssen die Spiel- und Abfrageports zulassen."
    'Connection.Public.BedrockDescription' = "Router und Windows-Firewall müssen den UDP-Spielport von Bedrock zulassen."
    'Connection.Ports.StandardSummary' = "Konfigurierte Ports: {0}. Manche Spiele erscheinen nur im Serverbrowser, wenn auch der Abfrageport weitergeleitet wird."
    'Connection.Ports.BedrockSummary' = "Bedrock-Spielport: {0}/UDP. IPv6-Port: {1}/UDP. Jeder Bedrock-Server benötigt ein eigenes Portpaar."
    'Connection.Port.Game' = "Spiel {0}"
    'Connection.Port.Query' = "Abfrage {0}"
    'Connection.Port.Rcon' = "RCON {0}"
    'Connection.Port.App' = "App {0}"
    'Connection.Address.Hidden' = "Durch Datenschutzmodus ausgeblendet"
    'Connection.Address.PublicUnavailable' = "Öffentliche Adresse konnte nicht geladen werden"
    'Connection.Address.Unavailable' = "Adresse konnte nicht geladen werden"
    'PlayerCenter.Summary.One' = "{0} • {1} • 1 benannter Spieler"
    'PlayerCenter.Summary.Many' = "{0} • {1} • {2} benannte Spieler"
    'PlayerCenter.Loading' = "Spielerdetails werden geladen…"
    'PlayerCenter.Guidance.Minecraft' = " Wähle einen Spieler aus, um die lokalen Minecraft-Verwaltungsbefehle zu verwenden."
    'PlayerCenter.Guidance.UnsupportedActions' = " Spieleraktionen bleiben deaktiviert, solange das Spiel kein verifiziertes Verwaltungsprotokoll bereitstellt."
    'PlayerCenter.Action.Kick' = "Entfernen"
    'PlayerCenter.Action.Allowlist' = "Zur Zulassungsliste hinzufügen"
    'PlayerCenter.Action.Operator' = "Zum Operator machen"
    'PlayerCenter.SelectValidPlayer' = "Wähle zuerst einen gültigen Minecraft-Spieler aus."
    'PlayerCenter.Confirm.Title' = "Minecraft-Spieleraktion bestätigen"
    'PlayerCenter.Confirm.Kick' = "Möchtest du diesen Spieler entfernen: {0}?"
    'PlayerCenter.Confirm.Allowlist' = "Möchtest du diesen Spieler zur Zulassungsliste hinzufügen: {0}?"
    'PlayerCenter.Confirm.Operator' = "Möchtest du diesen Spieler zum Operator machen: {0}?"
    'PlayerQuery.GameDefinitionUnavailable' = "Die Spieldefinition ist nicht verfügbar."
    'PlayerQuery.CrossplayUnavailable' = "Die Spielerverfolgung ist bei aktiviertem Crossplay nicht verfügbar. Deaktiviere Crossplay für die Steam-A2S-Spielerverfolgung."
    'PlayerQuery.ProtocolUnavailable' = "Das aktuelle Abfrageprotokoll dieses Spiels liefert keine sichere, allgemeine Spielernamenliste."
    'PlayerQuery.MinecraftCountOnly' = "Minecraft meldet {0} verbundene Spieler, aber diese Serverabfrage veröffentlicht keine Spielernamen."
    'PlayerQuery.StartServerFirst' = "Starte den Server, bevor du die Spielerdetails aktualisierst."
    'PlayerQuery.InvalidA2sResponse' = "Der Server hat eine ungültige A2S-Spielerantwort zurückgegeben."
    'PlayerQuery.IncompatiblePlayerList' = "Die Serverabfrage funktioniert, hat aber keine kompatible Spielerliste geliefert."
    'PlayerQuery.NoNamedPlayers' = "Der Server hat geantwortet; es sind keine benannten Spieler verbunden."
    'PlayerQuery.LoadedPlayers' = "{0} verbundene Spieler wurden geladen."
    'PlayerQuery.Timeout' = "Zeitüberschreitung bei der Spielerabfrage auf UDP-Port {0}."
    'PlayerQuery.ConnectionFailed' = "Die Spielerabfrage konnte keine Verbindung herstellen: {0}"
    'PlayerQuery.ReadFailed' = "Spielerdetails konnten nicht gelesen werden: {0}"
    'PlayerQuery.BedrockCountOnly' = "Minecraft Bedrock meldet {0} verbundene Spieler, veröffentlicht in der integrierten Statusantwort aber keine Namen."
    'PlayerQuery.MinecraftManagement.None' = "Der lokale Minecraft-Verwaltungsdienst meldet keine verbundenen Spieler."
    'PlayerQuery.MinecraftManagement.Loaded' = "{0} Spieler wurden über den lokalen Minecraft-Verwaltungsdienst geladen."
    'PlayerQuery.MinecraftRcon.None' = "Minecraft RCON meldet keine verbundenen Spieler."
    'PlayerQuery.MinecraftRcon.Loaded' = "{0} Spieler wurden über lokales Minecraft RCON geladen."
    'PlayerQuery.MinecraftUnavailable' = "Minecraft-Spielerdetails sind noch nicht verfügbar."
    'PlayerQuery.UnnamedPlayer' = "Unbenannter Spieler"
    'ModManager.Subtitle' = "Erkenne bereits installierte Inhalte, füge lokale Pakete sicher hinzu und behalte eine Wiederherstellungshistorie, ohne jeden Mod auflisten zu müssen."
    'ModManager.Field.Server' = "SERVER"
    'ModManager.Field.System' = "ADD-ON-SYSTEM"
    'ModManager.Field.InstallArea' = "INSTALLATIONSBEREICH"
    'ModManager.Support.Checking' = "Unterstützung wird geprüft…"
    'ModManager.Step.Detect' = "1  Erkennen"
    'ModManager.Step.Stop' = "2  Server stoppen"
    'ModManager.Step.Backup' = "3  Dateien sichern"
    'ModManager.Step.Install' = "4  Installieren"
    'ModManager.Step.Verify' = "5  Prüfen"
    'ModManager.Step.Restart' = "6  Bei Bedarf neu starten"
    'ModManager.Column.AddOn' = "ADD-ON"
    'ModManager.Column.Type' = "TYP"
    'ModManager.Column.Version' = "VERSION"
    'ModManager.Column.Status' = "STATUS"
    'ModManager.Column.Security' = "SICHERHEIT"
    'ModManager.Column.Source' = "QUELLE"
    'ModManager.Column.Location' = "SPEICHERORT"
    'ModManager.Safety.Title' = "Automatische Sicherheitscheckliste"
    'ModManager.Safety.Subtitle' = "Synix prüft diese Punkte vor jeder Änderung."
    'ModManager.Selection.Empty' = "Wähle ein Add-on aus, um seinen Fundort anzuzeigen."
    'ModManager.Button.InstallFile' = "Datei installieren"
    'ModManager.Button.InstallFramework' = "Framework installieren"
    'ModManager.Button.BrowseCatalog' = "Katalog öffnen"
    'ModManager.Button.BrowseCatalogs' = "Kataloge öffnen"
    'ModManager.Button.OpenFolder' = "Add-on-Ordner öffnen"
    'ModManager.Button.Refresh' = "Aktualisieren"
    'ModManager.Button.Remove' = "Auswahl entfernen"
    'ModManager.Button.Close' = "Schließen"
    'ModManager.Button.ManageIds' = "Mod-IDs verwalten"
    'ModManager.Inventory.Empty' = "In den Ordnern des aktiven Profils wurden keine Add-ons gefunden."
    'ModManager.Inventory.One' = "1 Add-on gefunden  •  {1} von Synix verfolgt"
    'ModManager.Inventory.Many' = "{0} Add-ons gefunden  •  {1} von Synix verfolgt"
    'ModManager.Inventory.RefreshFailed' = "Synix konnte die Add-on-Ordner nicht aktualisieren."
    'ModManager.Support.ProviderIds' = "BEREIT • Synix verwaltet die sortierte Mod-ID-Liste des Anbieters"
    'ModManager.Support.FileImport' = "BEREIT • Synix kann lokale Add-on-Dateien sicher importieren"
    'ModManager.Support.SetupNeeded' = "EINRICHTUNG NÖTIG • Wähle oder installiere zuerst ein kompatibles Framework"
    'ModManager.Support.DetectionOnly' = "NUR ERKENNUNG • Der Spieleanbieter bleibt für die Installation verantwortlich"
    'ModManager.Framework.Automatic' = "Server-Loader und vorhandene Ordner wählen den Installationsbereich automatisch aus."
    'ModManager.Framework.Named' = "Framework: {0}."
    'ModManager.Unsupported.Title' = "NOCH KEIN ADD-ON-PROFIL"
    'ModManager.Unsupported.Description' = "Synix rät nicht, wo dieses Spiel Mods speichert. Ein kleines Datenprofil kann die Unterstützung später ergänzen, ohne dieses Fenster neu zu schreiben."
    'ModManager.NoFilesChanged' = "Es wurden keine Dateien geändert."
    'ModManager.Safety.ServerStopped' = "Server ist gestoppt"
    'ModManager.Safety.StopFirst' = "Server vor Änderungen stoppen"
    'ModManager.Safety.FrameworkDetected' = "Framework erkannt"
    'ModManager.Safety.FrameworkRequired' = "Framework-Einrichtung erforderlich"
    'ModManager.Safety.FolderAvailable' = "Serverordner verfügbar"
    'ModManager.Safety.FolderMissing' = "Serverordner fehlt"
    'ModManager.Safety.ProviderTrust' = "Anbieterdownload benötigt manuelle Vertrauensprüfung"
    'ModManager.Safety.SecurityScan' = "Sicherheitsprüfung läuft vor der Installation"
    'ModManager.Safety.StandardPermissions' = "Standardmäßige Windows-Berechtigungen"
    'ModManager.Safety.RestartWithoutAdmin' = "Ohne Administratorrechte neu starten"
    'ModManager.Safety.RestartRequired' = "Neustart nach Änderungen erforderlich"
    'ModManager.Safety.LiveReload' = "Framework unterstützt Live-Neuladen"
    'ModManager.Profile.Rust.Description' = "Rust-Plugins, die vom Oxide/uMod-Framework geladen werden."
    'ModManager.Profile.Rust.Target' = "Oxide-Plugins"
    'ModManager.Profile.Minecraft.Name' = "Minecraft-Add-ons"
    'ModManager.Profile.Minecraft.Description' = "JAR-Plugins oder Mods, die anhand des Server-Loaders und vorhandener Ordner ausgewählt werden."
    'ModManager.Profile.Minecraft.ModsTarget' = "Loader-Mods"
    'ModManager.Profile.Minecraft.PluginsTarget' = "Server-Plugins"
    'ModManager.Profile.SevenDays.Name' = "7-Days-to-Die-Servermods"
    'ModManager.Profile.SevenDays.Description' = "Synix installiert vollständige Mod-ZIP-Pakete im Mods-Ordner des dedizierten Servers. Mods mit Clientinhalten müssen eventuell auch bei jedem Spieler installiert werden."
    'ModManager.Profile.SevenDays.Target' = "Server-Mods-Ordner"
    'ModManager.Profile.ArkEvolved.Name' = "Steam-Workshop-Mods"
    'ModManager.Profile.ArkEvolved.Description' = "Synix verwaltet die sortierten Steam-Workshop-IDs; ARK und Steam laden die Inhalte herunter und aktualisieren sie."
    'ModManager.Profile.ArkEvolved.Target' = "Sortierte Steam-Workshop-IDs"
    'ModManager.Profile.ArkAscended.Name' = "CurseForge-Servermods"
    'ModManager.Profile.ArkAscended.Description' = "Synix verwaltet die sortierte Mod-ID-Liste; ARK lädt die CurseForge-Inhalte beim Serverstart herunter und aktualisiert sie."
    'ModManager.Profile.ArkAscended.Target' = "Sortierte CurseForge-Mod-IDs"
    'ModManager.Profile.Discovered.Name' = "Erkannte Add-on-Ordner"
    'ModManager.Profile.Discovered.Description' = "Synix hat gängige Add-on-Ordner gefunden und kann sie sicher inventarisieren. Die Installation bleibt deaktiviert, bis ein verifiziertes Datenprofil hinzugefügt wird."
    'ModManager.Known.Mod' = "Mod"
    'ModManager.Known.Plugin' = "Plugin"
    'ModManager.Known.ModId' = "Mod-ID"
    'ModManager.Known.ProviderManaged' = "Vom Anbieter verwaltet"
    'ModManager.Known.ConfiguredNextStart' = "Für den nächsten Start konfiguriert"
    'ModManager.Known.ProviderNotScanned' = "Anbieterdownload wurde nicht vorab geprüft"
    'ModManager.Known.GameProvider' = "Spieleanbieter"
    'ModManager.Known.Detected' = "Auf Datenträger erkannt"
    'ModManager.Known.Healthy' = "In Ordnung"
    'ModManager.Known.Changed' = "Außerhalb von Synix geändert"
    'ModManager.Known.NotReviewed' = "Nicht von Synix geprüft"
    'ModManager.Known.LegacyNotReviewed' = "Ältere Installation • nicht geprüft"
    'ModManager.Known.StructuralOnly' = "Nur Strukturprüfungen"
    'ModManager.Known.ReviewRecorded' = "Prüfung vor Installation aufgezeichnet"
    'ModManager.Known.External' = "Extern"
    'ModManager.Known.ExternalProvider' = "Externer Anbieter"
    'ModManager.Known.SynixImport' = "Synix-Import"
    'ModManager.Known.LocalPackage' = "Lokales Paket"
    'ModManager.Known.BuiltInLoader' = "Integrierter Mod-Loader"
    'ModManager.Known.ArkBuiltInInstaller' = "Integriertes ARK-Mod-Installationsprogramm"
    'ResourceMonitor.WindowTitleFiltered' = "Live-Prozessdetails - {0}"
    'ResourceMonitor.GridTitleFiltered' = "Live-Prozessdetails  •  {0}"
    'ResourceMonitor.FilteredSubtitle' = "Alle Starter, Konsolenhosts und Spielprozesse, die Synix in dieser Servergruppe verifiziert hat."
    'ResourceMonitor.RowRunning' = "●  Wird ausgeführt"
    'ResourceMonitor.CpuCaption' = "Über alle verwalteten Serverprozesse"
    'ResourceMonitor.RamValue' = "{0:N2} GB"
    'ResourceMonitor.RamCaption' = "{0:N1} % von {1:N1} GB Systemspeicher"
    'ResourceMonitor.Active.None' = "Keine laufenden Serverprozesse erkannt"
    'ResourceMonitor.Active.One' = "1 Serverprozess ist derzeit online"
    'ResourceMonitor.Active.Many' = "{0} Serverprozesse sind derzeit online"
    'ResourceMonitor.ProcessCount.One' = "1 laufender Prozess"
    'ResourceMonitor.ProcessCount.Many' = "{0} laufende Prozesse"
    'ResourceMonitor.LastUpdated' = "Aktualisiert um {0:T}  •  Automatische Aktualisierung jede Sekunde"
    'ResourceMonitor.Empty' = "Keine laufenden Spielserver erkannt"
}

$additionalSemanticTranslations = & (Join-Path $PSScriptRoot 'SemanticResources.de.ps1')
foreach ($entry in $additionalSemanticTranslations.GetEnumerator()) {
    if ($semanticTranslations.Contains($entry.Key)) {
        throw "Duplicate German semantic resource key: $($entry.Key)"
    }

    $semanticTranslations[$entry.Key] = [string]$entry.Value
}

$pairs = @(
    @("—", "—"),
    @(":", ":"),
    @(".NET Framework requirement", ".NET Framework-Anforderung"),
    @("↺", "↺"),
    @("↻", "↻"),
    @("↻  Restart", "↻  Neu starten"),
    @("+  Add Server", "+  Server hinzufügen"),
    @("×", "×"),
    @(">_", ">_"),
    @("⌕", "⌕"),
    @("⌘", "⌘"),
    @("■  Stop", "■  Stoppen"),
    @("▤", "▤"),
    @("▶  Start", "▶  Starten"),
    @("◆", "◆"),
    @("◇", "◇"),
    @("◇  Sensitive fields follow the Synix Privacy Mode setting.", "◇ Sensible Felder folgen der Einstellung für den Synix-Datenschutzmodus."),
    @("◇  Template-aware controls: unavailable settings are disabled automatically for the selected game.", "◇ Vorlagenbasierte Steuerung: Nicht verfügbare Einstellungen werden für das ausgewählte Spiel automatisch deaktiviert."),
    @("◎", "◎"),
    @("●", "●"),
    @("●  Action required", "●  Aktion erforderlich"),
    @("●  Initializing SteamCMD...", "● SteamCMD wird initialisiert..."),
    @("●  LIVE MONITORING", "● LIVE-ÜBERWACHUNG"),
    @("●  LIVE TELEMETRY", "● LIVE-TELEMETRIE"),
    @("●  SteamCMD needs attention", "● SteamCMD braucht Aufmerksamkeit"),
    @("●  SteamCMD ready", "● SteamCMD-fähig"),
    @("◷", "◷"),
    @("⚙", "⚙"),
    @("⚠ Changing this location does not delete backups from the previous folder.", "⚠ Durch das Ändern dieses Speicherorts werden keine Backups aus dem vorherigen Ordner gelöscht."),
    @("✓", "✓"),
    @("✓  Readiness", "✓  Bereitschaft"),
    @("✕", "✕"),
    @("➜", "➜"),
    @("⬡", "⬡"),
    @("🔒 [REQUIRED] Enter a Server Name and select a Game Template.", "🔒 [ERFORDERLICH] Geben Sie einen Servernamen ein und wählen Sie eine Spielvorlage aus."),
    @("0", "0"),
    @("0 help articles", "0 Hilfeartikel"),
    @("0 running servers", "0 laufende Server"),
    @("0 servers", "0 Server"),
    @("0 settings", "0 Einstellungen"),
    @("0 unsaved changes", "0 nicht gespeicherte Änderungen"),
    @("0.0%", "0,0 %"),
    @("0.0% of system memory", "0,0 % des Systemspeichers"),
    @("0.00 GB", "0,00 GB"),
    @("1   YOUR DATA STAYS SEPARATE`r`nServers, settings, backups, runtimes, and SteamCMD are stored under C:\Synix so application updates do not replace them.", "1 IHRE DATEN BLEIBEN GETRENNT`r`nServer, Einstellungen, Backups, Laufzeiten und SteamCMD werden unter C:\Synix gespeichert, sodass Anwendungsupdates sie nicht ersetzen."),
    @("1–100 per server", "1–100 pro Server"),
    @("10", "10"),
    @("12345", "12345"),
    @("2   ADD A SERVER`r`nChoose a game, enter the friendly settings, and let Synix install it. Steam login is requested only when that game requires it.", "2 EINEN SERVER HINZUFÜGEN`r`nWählen Sie ein Spiel, geben Sie die benutzerfreundlichen Einstellungen ein und lassen Sie es von Synix installieren. Eine Steam-Anmeldung wird nur dann angefordert, wenn das Spiel dies erfordert."),
    @("27015", "27015"),
    @("27016", "27016"),
    @("3   START, STOP, AND VERIFY`r`nSynix shows the exact launch arguments, verifies startup, uses safe stop behavior where supported, and keeps recent logs available.", "3 STARTEN, STOPPEN UND ÜBERPRÜFEN`r`nSynix zeigt die genauen Startargumente an, überprüft den Start, verwendet sicheres Stoppverhalten, sofern unterstützt, und hält aktuelle Protokolle verfügbar."),
    @("4   NETWORK ACCESS`r`nWindows Firewall permission and router port forwarding are different. Synix checks local conflicts, but never changes your router.", "4 NETZWERKZUGANG`r`nDie Berechtigungen der Windows-Firewall und die Weiterleitung des Router-Ports sind unterschiedlich. Synix prüft lokale Konflikte, ändert jedoch niemals Ihren Router."),
    @("5   RECOVERY AND BACKUPS`r`nUse Settings > Advanced > Troubleshooter for safe health checks and repairs. Use Backups before moving Synix or making large changes.", "5 WIEDERHERSTELLUNG UND BACKUPS`r`nVerwenden Sie Einstellungen > Erweitert > Fehlerbehebung für sichere Zustandsprüfungen und Reparaturen. Verwenden Sie Backups, bevor Sie Synix verschieben oder große Änderungen vornehmen."),
    @("7777", "7777"),
    @("A server operation is currently in progress", "Derzeit läuft ein Servervorgang"),
    @("Access controls, startup behavior, and integrations", "Zugriffskontrollen, Startverhalten und Integrationen"),
    @("Access Credentials", "Zugangsdaten"),
    @("Account name, not your Steam display name", "Kontoname, nicht Ihr Steam-Anzeigename"),
    @("Across all managed server processes", "Über alle verwalteten Serverprozesse hinweg"),
    @("ACTIVE SERVERS", "AKTIVE SERVER"),
    @("Activity & Diagnostics", "Aktivität und Diagnose"),
    @("Add Destination", "Ziel hinzufügen"),
    @("Add every other complete template the game needs. Edit Installed location so each path is relative to the installed server folder.", "Fügen Sie alle weiteren vollständigen Vorlagen hinzu, die das Spiel benötigt. Bearbeiten Sie den Installationsort, sodass jeder Pfad relativ zum installierten Serverordner ist."),
    @("Add files", "Dateien hinzufügen"),
    @("Added automatically", "Automatisch hinzugefügt"),
    @("Additional configuration files", "Zusätzliche Konfigurationsdateien"),
    @("Additional files are required", "Zusätzliche Dateien sind erforderlich"),
    @("Admin Password", "Admin-Passwort"),
    @("Advanced", "Erweitert"),
    @("Advanced Discord Destinations", "Erweiterte Discord-Ziele"),
    @("Agreement required", "Vereinbarung erforderlich"),
    @("Allow launch-file export", "Export der Startdatei zulassen"),
    @("App Port", "App-Port"),
    @("AREA", "BEREICH"),
    @("Argument Test", "Argumenttest"),
    @("ARGUMENTS", "ARGUMENTE"),
    @("Authentication Token", "Authentifizierungstoken"),
    @("Auto Restart", "Automatischer Neustart"),
    @("Automatic evidence comes from Synix actions; arguments require the real-server test.", "Automatische Beweise stammen aus Synix-Aktionen; Argumente erfordern den Real-Server-Test."),
    @("Automatic safety checklist and next steps", "Automatische Sicherheitscheckliste und nächste Schritte"),
    @("Automatically builds a safe game/server folder below the configured Games path.", "Erstellt automatisch einen sicheren Spiel-/Serverordner unterhalb des konfigurierten Spielepfads."),
    @("Automatically collect generated game configurations", "Sammeln Sie generierte Spielkonfigurationen automatisch"),
    @("Automation", "Automatisierung"),
    @("BACKUP FILE", "BACKUP-DATEI"),
    @("Backup on Start", "Sicherung beim Start"),
    @("Backup Server", "Backup-Server"),
    @("Backups", "Sicherungen"),
    @("Before you continue", "Bevor Sie fortfahren"),
    @("Blocks launch with clear Microsoft download guidance when the runtime is missing.", "Blockiert den Start mit klarer Download-Anleitung von Microsoft, wenn die Laufzeit fehlt."),
    @("Blocks setup with a clear message when the processor does not support AVX2.", "Blockiert die Einrichtung mit einer klaren Meldung, wenn der Prozessor AVX2 nicht unterstützt."),
    @("Browse", "Durchsuchen"),
    @("Browse Folder", "Ordner durchsuchen"),
    @("Browse topics", "Themen durchsuchen"),
    @("Builder guide and supported tags", "Builder-Anleitung und unterstützte Tags"),
    @("C:\Synix\Games\Example Server", "C:\Synix\Games\Example Server"),
    @("Cancel", "Abbrechen"),
    @("Cancel Check", "Scheck abbrechen"),
    @("Canceling the release check safely...", "Freigabeprüfung sicher abbrechen..."),
    @("Catalog order", "Katalogbestellung"),
    @("CHECK", "PRÜFEN"),
    @("Check Again", "Überprüfen Sie es erneut"),
    @("Check for DDoS", "Suchen Sie nach DDoS"),
    @("Check Release", "Überprüfen Sie die Freigabe"),
    @("Check release readiness", "Freigabebereitschaft prüfen"),
    @("Check shared runtimes, server files, configurations, ports, Windows Firewall, disk space, interrupted processes, recent logs, and Synix update health from one place.", "Überprüfen Sie freigegebene Laufzeiten, Serverdateien, Konfigurationen, Ports, Windows-Firewall, Speicherplatz, unterbrochene Prozesse, aktuelle Protokolle und den Status von Synix-Updates von einem Ort aus."),
    @("Check SteamCMD for updates before launching the server.", "Überprüfen Sie SteamCMD auf Updates, bevor Sie den Server starten."),
    @("Check SteamCMD, runtimes, server files, configs, ports, Windows Firewall, disk space, interrupted processes, recent logs, and update health.", "Überprüfen Sie SteamCMD, Laufzeiten, Serverdateien, Konfigurationen, Ports, Windows-Firewall, Speicherplatz, unterbrochene Prozesse, aktuelle Protokolle und den Update-Zustand."),
    @("Check Synix Values", "Überprüfen Sie die Synix-Werte"),
    @("Checking for updates...", "Suche nach Updates..."),
    @("Checking release files...", "Release-Dateien werden überprüft..."),
    @("Checks the actual publish output without rebuilding Synix, starting the release, or accessing C:\Synix.", "Überprüft die tatsächliche Veröffentlichungsausgabe, ohne Synix neu zu erstellen, die Veröffentlichung zu starten oder auf C:\Synix zuzugreifen."),
    @("Checks the installed Windows .NET Framework release before the server starts.", "Überprüft die installierte Windows .NET Framework-Version, bevor der Server startet."),
    @("Checks whether virtualization support is enabled and available to Windows.", "Überprüft, ob die Virtualisierungsunterstützung aktiviert und für Windows verfügbar ist."),
    @("Choose a row to unlock server controls", "Wählen Sie eine Zeile aus, um die Serversteuerung freizuschalten"),
    @("Choose a server type to show its local verification history.", "Wählen Sie einen Servertyp aus, um dessen lokalen Verifizierungsverlauf anzuzeigen."),
    @("Choose Folder", "Wählen Sie Ordner"),
    @("Choose only the built-in launch behavior verified for this dedicated server.", "Wählen Sie nur das integrierte Startverhalten aus, das für diesen dedizierten Server überprüft wurde."),
    @("Choose Package", "Wählen Sie Paket"),
    @("Choose the backup that should replace the server's current files.", "Wählen Sie das Backup aus, das die aktuellen Dateien des Servers ersetzen soll."),
    @("Choose the game and define the server identity.", "Wählen Sie das Spiel und definieren Sie die Serveridentität."),
    @("Choose the language used by Synix. Game settings and configuration values remain in English.", "Wählen Sie die von Synix verwendete Sprache. Spieleinstellungen und Konfigurationswerte bleiben auf Englisch."),
    @("Choose when Synix should perform the scheduled server restart.", "Wählen Sie, wann Synix den geplanten Serverneustart durchführen soll."),
    @("Clean Orphaned Rules", "Bereinigen Sie verwaiste Regeln"),
    @("Clean orphaned Synix server firewall rules", "Bereinigen Sie verwaiste Synix-Server-Firewallregeln"),
    @("CLEAR", "KLAR"),
    @("Clear Filters", "Filter löschen"),
    @("Clear Mark", "Klare Markierung"),
    @("Close", "Schließen"),
    @("Collect generated game configurations now", "Sammeln Sie jetzt generierte Spielkonfigurationen"),
    @("Collect Now", "Jetzt sammeln"),
    @("Commands stay on this computer unless you intentionally configure Java RCON for remote access.", "Befehle bleiben auf diesem Computer, es sei denn, Sie konfigurieren Java RCON absichtlich für den Fernzugriff."),
    @("Compatibility Verification", "Kompatibilitätsüberprüfung"),
    @("Complete the required file setup before the dedicated server can start.", "Schließen Sie die erforderliche Dateieinrichtung ab, bevor der dedizierte Server gestartet werden kann."),
    @("Complete, working configuration template file", "Vollständige, funktionierende Konfigurationsvorlagendatei"),
    @("Config Editor", "Konfigurationseditor"),
    @("CONFIG SOURCE", "QUELLE KONFIG"),
    @("Config unavailable", "Konfiguration nicht verfügbar"),
    @("CONFIGURATION", "KONFIGURATION"),
    @("Configuration & Security", "Konfiguration und Sicherheit"),
    @("Configuration Application Check", "Überprüfung der Konfigurationsanwendung"),
    @("Configuration behavior", "Konfigurationsverhalten"),
    @("Configuration Editor", "Konfigurationseditor"),
    @("Configuration file", "Konfigurationsdatei"),
    @("Configuration format", "Konfigurationsformat"),
    @("Configuration path relative to the installed server folder", "Konfigurationspfad relativ zum installierten Serverordner"),
    @("Configuration repair is available", "Konfigurationsreparatur ist verfügbar"),
    @("Configuration report copied to the clipboard.", "Konfigurationsbericht in die Zwischenablage kopiert."),
    @("CONFIGURATION STATUS", "KONFIGURATIONSSTATUS"),
    @("Configure", "Konfigurieren"),
    @("Configure basic Synix behavior on this computer.", "Konfigurieren Sie das grundlegende Synix-Verhalten auf diesem Computer."),
    @("Configure Schedule", "Zeitplan konfigurieren"),
    @("Confirm password", "Passwort bestätigen"),
    @("Confirm removal of the listed firewall rules", "Bestätigen Sie die Entfernung der aufgelisteten Firewall-Regeln"),
    @("Connect GitHub", "GitHub verbinden"),
    @("Connect GitHub account", "GitHub-Konto verbinden"),
    @("CONNECTED", "VERBUNDEN"),
    @("Connection Information", "Verbindungsinformationen"),
    @("Continue only after you have reviewed the required setup steps.", "Fahren Sie erst fort, nachdem Sie die erforderlichen Einrichtungsschritte überprüft haben."),
    @("Copy Address", "Adresse kopieren"),
    @("Copy allowlisted Steam runtime files after install", "Kopieren Sie die Steam-Laufzeitdateien auf der Zulassungsliste nach der Installation"),
    @("Copy approved Steam runtime files after installation", "Kopieren Sie genehmigte Steam-Laufzeitdateien nach der Installation"),
    @("Copy Details", "Details kopieren"),
    @("Copy problem report", "Problembericht kopieren"),
    @("Copy Report", "Bericht kopieren"),
    @("Covers the unified Microsoft runtime used by current 2015, 2017, 2019, and 2022 servers.", "Deckt die einheitliche Microsoft-Laufzeit ab, die von den aktuellen Servern 2015, 2017, 2019 und 2022 verwendet wird."),
    @("CPU Usage", "CPU-Auslastung"),
    @("CPU USAGE", "CPU-NUTZUNG"),
    @("Create a protected server backup before each launch.", "Erstellen Sie vor jedem Start ein geschütztes Server-Backup."),
    @("Create a transfer password", "Erstellen Sie ein Übertragungspasswort"),
    @("Create a validated built-in game definition without plugins or scripts. Definitions are saved into the project and become available only after Synix is rebuilt.", "Erstellen Sie eine validierte integrierte Spieldefinition ohne Plugins oder Skripte. Definitionen werden im Projekt gespeichert und sind erst verfügbar, nachdem Synix neu erstellt wurde."),
    @("Create Batch File", "Batchdatei erstellen"),
    @("CREATED", "ERSTELLT"),
    @("Crossplay", "Crossplay"),
    @("Ctrl+F  Search     •     Esc  Close     •     Links open in your browser", "Strg+F Suchen • Esc Schließen • Links werden in Ihrem Browser geöffnet"),
    @("Current state reported by the Synix engine", "Aktueller Status, der von der Synix-Engine gemeldet wird"),
    @("Current version and installation type", "Aktuelle Version und Installationstyp"),
    @("Custom backup location", "Benutzerdefinierter Backup-Speicherort"),
    @("D", "D"),
    @("Dark Mode", "Dunkler Modus"),
    @("Dark mode toggle", "Umschalten des Dunkelmodus"),
    @("DDoS Attack Detection", "DDoS-Angriffserkennung"),
    @("Decline", "Ablehnen"),
    @("Default launch arguments", "Standard-Startargumente"),
    @("Default Launch Arguments", "Standard-Startargumente"),
    @("Default launch arguments (everything after the executable)", "Standard-Startargumente (alles nach der ausführbaren Datei)"),
    @("Default startup arguments", "Standard-Startargumente"),
    @("Definition Builder", "Definitionsgenerator"),
    @("Definition ID", "Definitions-ID"),
    @("Definition revision", "Überarbeitung der Definition"),
    @("Definition test report copied to the clipboard.", "Definitionstestbericht in die Zwischenablage kopiert."),
    @("Delete Backup", "Backup löschen"),
    @("Delete Server", "Server löschen"),
    @("Describe the problem", "Beschreiben Sie das Problem"),
    @("DESTINATION", "ZIEL"),
    @("Destination name", "Zielname"),
    @("DETAILS", "DETAILS"),
    @("Development", "Entwicklung"),
    @("Disabled — scheduled work runs only while Synix is open.", "Deaktiviert – geplante Arbeiten werden nur ausgeführt, während Synix geöffnet ist."),
    @("Disconnect GitHub", "Trennen Sie GitHub"),
    @("Disconnect GitHub account", "Trennen Sie das GitHub-Konto"),
    @("Discord", "Discord"),
    @("Discord Alerts", "Discord-Warnungen"),
    @("Discord Destination", "Discord-Ziel"),
    @("Discord opened. Select New Post in the bug-reporting forum and paste the copied report.", "Discord wurde geöffnet. Wählen Sie im Forum für Fehlerberichte die Option Neuer Beitrag und fügen Sie den kopierten Bericht ein."),
    @("Discord webhook URL", "Discord-Webhook-URL"),
    @("Discord Webhooks", "Discord-Webhooks"),
    @("Do not paste passwords, webhooks, IP addresses, private configuration, or full launch commands. Synix removes common secrets before sending.", "Fügen Sie keine Passwörter, Webhooks, IP-Adressen, private Konfigurationen oder vollständige Startbefehle ein. Synix entfernt allgemeine Geheimnisse vor dem Senden."),
    @("Documents source folder for automatic imports (optional)", "Quellordner für Dokumente für automatische Importe (optional)"),
    @("Each rule points to an executable under C:\Synix\Games\[Game]\[Server], but that individual server folder is gone and no installed Synix server owns the path.", "Jede Regel verweist auf eine ausführbare Datei unter C:\Synix\Games\[Spiel]\[Server], aber dieser einzelne Serverordner ist nicht mehr vorhanden und kein installierter Synix-Server besitzt den Pfad."),
    @("Edit", "Bearbeiten"),
    @("Edit serverconfig.xml safely without changing its XML structure.", "Bearbeiten Sie serverconfig.xml sicher, ohne die XML-Struktur zu ändern."),
    @("Edition", "Auflage"),
    @("Elevated System Tasks", "Erhöhte Systemaufgaben"),
    @("Elevated system tasks", "Erhöhte Systemaufgaben"),
    @("Enable only when anonymous SteamCMD installation fails and a Steam account is required.", "Nur aktivieren, wenn die anonyme SteamCMD-Installation fehlschlägt und ein Steam-Konto erforderlich ist."),
    @("Enable only when the server cannot run correctly without Windows elevation.", "Aktivieren Sie diese Option nur, wenn der Server ohne Windows-Erhöhung nicht ordnungsgemäß ausgeführt werden kann."),
    @("Enable RCON", "Aktivieren Sie RCON"),
    @("Enable RCON only for game templates that support secure remote commands.", "Aktivieren Sie RCON nur für Spielvorlagen, die sichere Remote-Befehle unterstützen."),
    @("Enable server query monitoring", "Aktivieren Sie die Überwachung von Serverabfragen"),
    @("Enable when the server has a verified query or network probe that Synix can monitor.", "Aktivieren Sie diese Option, wenn der Server über eine verifizierte Abfrage oder Netzwerkprüfung verfügt, die Synix überwachen kann."),
    @("Enabled", "Aktiviert"),
    @("Encrypted Export", "Verschlüsselter Export"),
    @("Enter a valid Steam account name.", "Geben Sie einen gültigen Steam-Kontonamen ein."),
    @("Enter IDs in the order they should load. Use commas, spaces, or one ID per line. Synix does not need a database of mod names.", "Geben Sie die IDs in der Reihenfolge ein, in der sie geladen werden sollen. Verwenden Sie Kommas, Leerzeichen oder eine ID pro Zeile. Synix benötigt keine Datenbank mit Mod-Namen."),
    @("Enter the game information, then validate before saving.", "Geben Sie die Spielinformationen ein und bestätigen Sie sie vor dem Speichern."),
    @("EVENTS", "VERANSTALTUNGEN"),
    @("Example Game", "Beispielspiel"),
    @("Example: Server closes a few seconds after Start", "Beispiel: Der Server wird einige Sekunden nach dem Start geschlossen"),
    @("EXECUTABLE", "AUSFÜHRBAR"),
    @("Expected result", "Erwartetes Ergebnis"),
    @("EXPERIMENTAL", "EXPERIMENTELL"),
    @("Export", "Exportieren"),
    @("Export to Project", "In Projekt exportieren"),
    @("External deployment is for launchers or virtual machines and disables query monitoring.", "Die externe Bereitstellung ist für Starter oder virtuelle Maschinen gedacht und deaktiviert die Abfrageüberwachung."),
    @("Extra Arguments", "Zusätzliche Argumente"),
    @("Find setup guidance, command details, and troubleshooting answers.", "Hier finden Sie Anleitungen zur Einrichtung, Befehlsdetails und Antworten zur Fehlerbehebung."),
    @("Finds rules whose executable was under C:\Synix\Games\[Game]\[Server], but that specific server is no longer saved and its server folder is gone. Ports and custom install folders are not scanned.", "Findet Regeln, deren ausführbare Datei sich unter C:\Synix\Games\[Spiel]\[Server] befand, aber dieser bestimmte Server wird nicht mehr gespeichert und sein Serverordner ist verschwunden. Ports und benutzerdefinierte Installationsordner werden nicht gescannt."),
    @("Firewall Cleanup Review", "Überprüfung der Firewall-Bereinigung"),
    @("Firewall executable rules ready for removal", "Ausführbare Firewall-Regeln zum Entfernen bereit"),
    @("First-launch preparation", "Vorbereitung des ersten Starts"),
    @("First-Start Assistant", "Erststartassistent"),
    @("First-start message shown to the user", "Dem Benutzer wird beim ersten Start eine Meldung angezeigt"),
    @("Fix Config", "Konfiguration korrigieren"),
    @("Folder", "Ordner"),
    @("Folder Path", "Ordnerpfad"),
    @("FORMAT-AWARE EDITING", "FORMATBEWUSSTE BEARBEITUNG"),
    @("Framework", "Rahmen"),
    @("Full Release Notes", "Vollständige Versionshinweise"),
    @("Game", "Spiel"),
    @("GAME", "SPIEL"),
    @("Game Definition Builder", "Spieldefinitions-Builder"),
    @("Game Definition Test Runner", "Testläufer für Spieldefinitionen"),
    @("Game icon HTTPS URL (optional)", "Spielsymbol HTTPS-URL (optional)"),
    @("Game Mode", "Spielmodus"),
    @("Game modes (one exact value per line)", "Spielmodi (ein exakter Wert pro Zeile)"),
    @("Game name", "Spielname"),
    @("Game port", "Game-Port"),
    @("Game Port", "Spielport"),
    @("Game Server", "Spielserver"),
    @("Game Servers", "Spielserver"),
    @("Game Support Catalog", "Spiele-Support-Katalog"),
    @("Game Verification Queue", "Spielverifizierungswarteschlange"),
    @("Game Version", "Spielversion"),
    @("Gameplay Profile", "Gameplay-Profil"),
    @("General", "Allgemein"),
    @("Get Token", "Token erhalten"),
    @("Getting Started with Synix", "Erste Schritte mit Synix"),
    @("GH", "GH"),
    @("GitHub is not connected. Copy and Discord options still work.", "GitHub ist nicht verbunden. Die Kopier- und Discord-Optionen funktionieren weiterhin."),
    @("GitHub posts directly without opening a browser after the account is connected.", "GitHub postet direkt, ohne einen Browser zu öffnen, nachdem das Konto verbunden wurde."),
    @("Guide", "Leitfaden"),
    @("Help", "Hilfe"),
    @("HELP & SUPPORT", "HILFE & UNTERSTÜTZUNG"),
    @("Help Center", "Hilfecenter"),
    @("Hide IP addresses, passwords, and other sensitive information while screen sharing.", "Verbergen Sie IP-Adressen, Passwörter und andere vertrauliche Informationen während der Bildschirmfreigabe."),
    @("HOUR", "STUNDE"),
    @("How can we help?", "Wie können wir helfen?"),
    @("How the user obtains and places required game files", "Wie der Benutzer die erforderlichen Spieldateien erhält und ablegt"),
    @("I Agree", "Ich stimme zu"),
    @("I confirmed the displayed server name, ports, player limit, and all other values used by this definition, including passwords, RCON, mode, and map/world where applicable.", "Ich habe den angezeigten Servernamen, die Ports, das Spielerlimit und alle anderen in dieser Definition verwendeten Werte bestätigt, einschließlich Passwörter, RCON, Modus und Karte/Welt, sofern zutreffend."),
    @("I Understand", "Ich verstehe"),
    @("Identity, world, player, and network information", "Informationen zu Identität, Welt, Spieler und Netzwerk"),
    @("IMPORT  No package selected`nChoose a package to calculate space and time.", "IMPORT Kein Paket ausgewählt`nWählen Sie ein Paket zur Berechnung von Raum und Zeit."),
    @("Import Existing Server", "Vorhandenen Server importieren"),
    @("Import Synix", "Synix importieren"),
    @("Important: Write the summary and report details in English so the Synix support team can review them.", "Wichtig: Schreiben Sie die Zusammenfassung und die Berichtsdetails auf Englisch, damit das Synix-Supportteam sie überprüfen kann."),
    @("Individual events", "Individuelle Veranstaltungen"),
    @("Insert a supported Synix argument tag", "Fügen Sie ein unterstütztes Synix-Argument-Tag ein"),
    @("Insert tag", "Tag einfügen"),
    @("INSTALL", "INSTALLIEREN"),
    @("Install  — Not verified yet", "Installieren – Noch nicht überprüft"),
    @("Install & Launch", "Installieren und starten"),
    @("Install Location", "Installationsort"),
    @("Install this game in Synix before testing its real launch arguments.", "Installieren Sie dieses Spiel in Synix, bevor Sie seine tatsächlichen Startargumente testen."),
    @("Install Update", "Update installieren"),
    @("Install, start, stop, and monitoring checks are recorded automatically. Argument verification uses a real installed server and a sanitized command test; configuration remains a manual file check.", "Installations-, Start-, Stopp- und Überwachungsprüfungen werden automatisch aufgezeichnet. Bei der Argumentüberprüfung werden ein real installierter Server und ein bereinigter Befehlstest verwendet. Konfiguration bleibt eine manuelle Dateiprüfung."),
    @("Installation canceled. No files were changed.", "Installation abgebrochen. Es wurden keine Dateien geändert."),
    @("Installed location", "Installationsort"),
    @("Installed server to test", "Installierter Server zum Testen"),
    @("Installed Servers", "Installierte Server"),
    @("INTEGRITY", "INTEGRITÄT"),
    @("Interface language", "Schnittstellensprache"),
    @("Invite Code", "Einladungscode"),
    @("KNOWLEDGE BASE", "WISSENSBASIS"),
    @("KNOWLEDGE BASE READY", "WISSENSBASIS BEREIT"),
    @("LAN IP: Fetching...", "LAN-IP: Wird abgerufen..."),
    @("Language", "Sprache"),
    @("LAST TESTED", "ZULETZT GETESTET"),
    @("LAST VERIFIED", "ZULETZT VERIFIZIERT"),
    @("Last-tested Synix version: Not verified yet", "Zuletzt getestete Synix-Version: Noch nicht verifiziert"),
    @("Later", "Später"),
    @("Launch Arguments", "Argumente starten"),
    @("Launch behavior", "Startverhalten"),
    @("Launch file", "Datei starten"),
    @("Launch Preparation", "Startvorbereitung"),
    @("Launch preparation", "Startvorbereitung"),
    @("Launch with administrator permission", "Mit Administratorrechten starten"),
    @("Lets the user create a reviewed launch file. Disable for deployment commands that must stay inside Synix.", "Ermöglicht dem Benutzer, eine überprüfte Startdatei zu erstellen. Deaktivieren Sie diese Option für Bereitstellungsbefehle, die in Synix bleiben müssen."),
    @("Limit the number of backups retained per server.", "Begrenzen Sie die Anzahl der pro Server aufbewahrten Backups."),
    @("Live performance across every managed game server process.", "Live-Leistung für jeden verwalteten Spieleserverprozess."),
    @("Live performance and configuration details", "Live-Performance und Konfigurationsdetails"),
    @("Loader", "Lader"),
    @("Loader Version", "Loader-Version"),
    @("Loading the built-in game verification queue...", "Die integrierte Spielverifizierungswarteschlange wird geladen..."),
    @("Loading...", "Wird geladen…"),
    @("LOCATION", "STANDORT"),
    @("Logs\*.log`r`nSaved\Logs\**\*.log", "Protokolle\*.log`r`nGespeichert\Protokolle\**\*.log"),
    @("Long-Duration Reliability Test", "Langzeit-Zuverlässigkeitstest"),
    @("Main World", "Hauptwelt"),
    @("Maintenance schedule", "Wartungsplan"),
    @("Maintenance Schedule", "Wartungsplan"),
    @("Manage Provider Mod IDs", "Verwalten Sie Provider-Mod-IDs"),
    @("Map", "Karte"),
    @("Map / World", "Karte / Welt"),
    @("Map and mode choices come directly from the selected game template.", "Die Karten- und Modusauswahl erfolgt direkt aus der ausgewählten Spielvorlage."),
    @("Maps or scenarios (one exact value per line)", "Karten oder Szenarien (ein exakter Wert pro Zeile)"),
    @("Mark Verified", "Als bestätigt markieren"),
    @("Master Discord Webhook", "Master Discord Webhook"),
    @("Max Players", "Maximale Spieler"),
    @("Max saved backups", "Maximal gespeicherte Backups"),
    @("Mbps", "Mbit/s"),
    @("Message shown after special readiness checks pass (optional)", "Meldung, die angezeigt wird, nachdem die speziellen Bereitschaftsprüfungen bestanden wurden (optional)"),
    @("MESSAGES SENT", "GESENDETE NACHRICHTEN"),
    @("Messages to send", "Nachrichten zum Senden"),
    @("Minecraft Runtime", "Minecraft-Laufzeit"),
    @("Minecraft Server Console", "Minecraft-Serverkonsole"),
    @("Minimum system RAM in GB (0 means no minimum)", "Mindestsystem-RAM in GB (0 bedeutet kein Minimum)"),
    @("MINUTE", "MINUTE"),
    @("minutes", "Minuten"),
    @("Mod & Plugin Manager", "Mod- und Plugin-Manager"),
    @("MONITOR", "ÜBERWACHEN"),
    @("Monitor active server ports for incoming packet floods and notify on abnormal traffic bursts.", "Überwachen Sie aktive Server-Ports auf eingehende Paketfluten und benachrichtigen Sie bei ungewöhnlichen Datenverkehrsspitzen."),
    @("Monitor and manage every game server from one workspace.", "Überwachen und verwalten Sie jeden Spieleserver von einem Arbeitsbereich aus."),
    @("Monitoring  — Not verified yet", "Überwachung – Noch nicht verifiziert"),
    @("My Dedicated Server", "Mein dedizierter Server"),
    @("N/A", "N/A"),
    @("Name this destination, paste its Discord webhook, and choose exactly which Synix events it receives.", "Benennen Sie dieses Ziel, fügen Sie seinen Discord-Webhook ein und wählen Sie genau aus, welche Synix-Ereignisse es empfängt."),
    @("Network", "Netzwerk"),
    @("Network & RCON", "Netzwerk und RCON"),
    @("NEW SERVER", "NEUER SERVER"),
    @("No Days Scheduled", "Keine Tage geplant"),
    @("No extra arguments", "Keine zusätzlichen Argumente"),
    @("No matching help articles", "Keine passenden Hilfeartikel"),
    @("No publish folder was detected.", "Es wurde kein Veröffentlichungsordner erkannt."),
    @("No reliability test has been run yet.", "Es wurde noch kein Zuverlässigkeitstest durchgeführt."),
    @("NO RESULTS", "KEINE ERGEBNISSE"),
    @("No running server processes detected", "Es wurden keine laufenden Serverprozesse erkannt"),
    @("Not changed: game files, saved servers, port-only rules, custom install folders, and firewall rules outside C:\Synix\Games.", "Nicht geändert: Spieledateien, gespeicherte Server, Nur-Port-Regeln, benutzerdefinierte Installationsordner und Firewall-Regeln außerhalb von C:\Synix\Games."),
    @("Not Required", "Nicht erforderlich"),
    @("Off", "Aus"),
    @("Online Service Authentication", "Online-Service-Authentifizierung"),
    @("Only a masked webhook identifier is shown. Open Server Settings to view or edit the saved destination.", "Es wird nur eine maskierte Webhook-ID angezeigt. Öffnen Sie die Servereinstellungen, um das gespeicherte Ziel anzuzeigen oder zu bearbeiten."),
    @("Only the value you change is replaced; comments, sections, nesting, quotes, spacing, and key order remain intact.", "Nur der von Ihnen geänderte Wert wird ersetzt; Kommentare, Abschnitte, Verschachtelung, Anführungszeichen, Abstände und Tastenreihenfolge bleiben erhalten."),
    @("Open Backup Folder", "Öffnen Sie den Sicherungsordner"),
    @("Open Config Editor", "Öffnen Sie den Konfigurationseditor"),
    @("Open Discord", "Öffne Discord"),
    @("Open Discord bug forum", "Öffnen Sie das Discord-Fehlerforum"),
    @("Open GitHub", "Öffnen Sie GitHub"),
    @("Open Latest Game Log", "Öffnen Sie das neueste Spielprotokoll"),
    @("Open PayPal Donation", "Öffnen Sie PayPal-Spende"),
    @("Open Server Folder", "Öffnen Sie den Serverordner"),
    @("Open SteamCMD", "Öffnen Sie SteamCMD"),
    @("Open the game definition builder", "Öffnen Sie den Spieldefinitions-Builder"),
    @("Open the game verification queue", "Öffnen Sie die Spielverifizierungswarteschlange"),
    @("Open the long-duration reliability test", "Öffnen Sie den Langzeitzuverlässigkeitstest"),
    @("Open the native console when a game server starts. Disable this to run servers silently in the background.", "Öffnen Sie die native Konsole, wenn ein Spieleserver startet. Deaktivieren Sie dies, um Server unbeaufsichtigt im Hintergrund laufen zu lassen."),
    @("Open the PayPal donation page on your phone.", "Öffnen Sie die PayPal-Spendenseite auf Ihrem Telefon."),
    @("Open the Synix troubleshooter", "Öffnen Sie die Synix-Fehlerbehebung"),
    @("Optional", "Optional"),
    @("Optional flags only — for example: -log, -nosteamclient, or -forceupdate", "Nur optionale Flags – zum Beispiel: -log, -nosteamclient oder -forceupdate"),
    @("Optional import files (relative paths, one per line)", "Optionale Importdateien (relative Pfade, einer pro Zeile)"),
    @("Optional RCON syntax — launch arguments must contain {rcon}", "Optionale RCON-Syntax – Startargumente müssen {rcon} enthalten"),
    @("ORIGINAL", "ORIGINAL"),
    @("Original formatting is protected", "Die Originalformatierung ist geschützt"),
    @("Orphaned Firewall Rule Cleanup", "Bereinigung verwaister Firewall-Regeln"),
    @("Overrides Synix's hide-console preference for servers managed through their own window.", "Überschreibt die Hide-Console-Einstellung von Synix für Server, die über ein eigenes Fenster verwaltet werden."),
    @("Password (at least 8 characters)", "Passwort (mindestens 8 Zeichen)"),
    @("Paths & Launch Details", "Pfade und Startdetails"),
    @("PID", "PID"),
    @("PLAYER", "SPIELER"),
    @("Player Management Center", "Spielerverwaltung"),
    @("Players", "Spieler"),
    @("Port", "Port"),
    @("Port availability is checked automatically against running processes and other Synix servers.", "Die Portverfügbarkeit wird automatisch anhand laufender Prozesse und anderer Synix-Server überprüft."),
    @("Portable Java", "Tragbares Java"),
    @("Preview", "Vorschau"),
    @("Privacy & Security", "Datenschutz und Sicherheit"),
    @("Privacy mode", "Datenschutzmodus"),
    @("Privacy Mode", "Datenschutzmodus"),
    @("Privacy Mode masks this access credential. Enter a custom code, or leave it empty on first install to let Windrose generate one.", "Der Datenschutzmodus maskiert diese Zugangsdaten. Geben Sie einen benutzerdefinierten Code ein oder lassen Sie ihn bei der ersten Installation leer, damit Windrose einen generieren kann."),
    @("Problem summary", "Problemzusammenfassung"),
    @("Process identity and live resource usage for every active game server.", "Prozessidentität und Live-Ressourcennutzung für jeden aktiven Spieleserver."),
    @("PROGRESS", "FORTSCHRITT"),
    @("Protect Synix Transfer", "Schützen Sie Synix Transfer"),
    @("Protected in Synix and hidden from its logs. Generated batch files include the usable token in readable text.", "In Synix geschützt und vor seinen Protokollen verborgen. Generierte Batchdateien enthalten das verwendbare Token in lesbarem Text."),
    @("Public IP: Fetching...", "Öffentliche IP: Wird abgerufen..."),
    @("Publish folder selected. Run the check when ready.", "Veröffentlichungsordner ausgewählt. Führen Sie die Prüfung durch, wenn Sie bereit sind."),
    @("Published Synix folder", "Veröffentlichter Synix-Ordner"),
    @("PVE", "PVE"),
    @("Query", "Abfrage"),
    @("Query port", "Abfrageport"),
    @("Query Port", "Abfrageport"),
    @("Quick Commands — choose one to prepare it, then review and send it", "Schnellbefehle: Wählen Sie einen aus, um ihn vorzubereiten, überprüfen Sie ihn und senden Sie ihn"),
    @("Quick event selection", "Schnelle Veranstaltungsauswahl"),
    @("RAM Usage", "RAM-Auslastung"),
    @("RAM USAGE", "RAM-NUTZUNG"),
    @("Raw Preview", "Rohvorschau"),
    @("RCON", "RCON"),
    @("RCON Password", "RCON-Passwort"),
    @("RCON Port", "RCON-Port"),
    @("Read-only values can be selected and copied for diagnostics", "Für die Diagnose können schreibgeschützte Werte ausgewählt und kopiert werden"),
    @("Read-only verification of template structure, revision, and values saved in Server Settings. Password values are never displayed.", "Schreibgeschützte Überprüfung der Vorlagenstruktur, Revision und der in den Servereinstellungen gespeicherten Werte. Passwortwerte werden nie angezeigt."),
    @("Reading and testing the project game-definition library...", "Lesen und Testen der Spieldefinitionsbibliothek des Projekts ..."),
    @("Ready — Windows requests administrator permission only if rules need removal.", "Bereit – Windows fordert nur dann die Administratorberechtigung an, wenn Regeln entfernt werden müssen."),
    @("Ready for the first start", "Bereit für den ersten Start"),
    @("Ready to check the published files and the test receipt created during Publish.", "Bereit zur Überprüfung der veröffentlichten Dateien und des während der Veröffentlichung erstellten Testbelegs."),
    @("Ready to check this computer.", "Bereit, diesen Computer zu überprüfen."),
    @("Ready to manage", "Bereit zur Verwaltung"),
    @("Ready to test the built-in game-definition library.", "Bereit zum Testen der integrierten Spieldefinitionsbibliothek."),
    @("Ready. A 30-minute run with 30-second samples is recommended for a quick check.", "Bereit. Zur schnellen Kontrolle empfiehlt sich ein 30-minütiger Lauf mit 30-Sekunden-Proben."),
    @("Record Verification", "Datensatzüberprüfung"),
    @("Refresh", "Aktualisieren"),
    @("Refresh Players", "Spieler aktualisieren"),
    @("Refresh to load player details directly from the local server.", "Aktualisieren Sie, um Spielerdetails direkt vom lokalen Server zu laden."),
    @("Register Server", "Registrieren Sie den Server"),
    @("Release check canceled.", "Freigabeprüfung abgebrochen."),
    @("Release highlights", "Release-Highlights"),
    @("Release notes will appear here.", "Versionshinweise werden hier angezeigt."),
    @("Release Readiness Checker", "Release-Readiness-Checker"),
    @("Release report copied to the clipboard.", "Freigabebericht in die Zwischenablage kopiert."),
    @("Reliability Test", "Zuverlässigkeitstest"),
    @("Reliability test cancelled. No server settings were changed.", "Zuverlässigkeitstest abgebrochen. Es wurden keine Servereinstellungen geändert."),
    @("Remind Me Later", "Erinnere mich später daran"),
    @("Remote Administration", "Fernverwaltung"),
    @("Remove", "Entfernen"),
    @("Remove Rules", "Regeln entfernen"),
    @("Remove selected", "Ausgewählte entfernen"),
    @("Repair available", "Reparatur möglich"),
    @("Repairing SteamCMD...", "SteamCMD reparieren..."),
    @("Repeatedly samples Synix memory, handles, threads, and the read-only server health checks. It does not start, stop, install, update, or alter a server.", "Prüft wiederholt Synix-Speicher, Handles, Threads und die schreibgeschützten Serverzustandsprüfungen. Es startet, stoppt, installiert, aktualisiert oder verändert keinen Server."),
    @("Report a Problem", "Problem melden"),
    @("Require a visible server manager window", "Erfordert ein sichtbares Server-Manager-Fenster"),
    @("Require an AVX2-capable processor", "Erfordert einen AVX2-fähigen Prozessor"),
    @("Require hardware virtualization", "Erfordern Hardwarevirtualisierung"),
    @("Require Microsoft Hyper-V", "Erfordert Microsoft Hyper-V"),
    @("Require the server manager window to remain visible", "Erfordern, dass das Server-Manager-Fenster sichtbar bleibt"),
    @("Require Visual C++ 2013 x64 runtime", "Erfordert Visual C++ 2013 x64-Laufzeit"),
    @("Require Visual C++ 2015-2022 x64 runtime", "Erfordert Visual C++ 2015–2022 x64-Laufzeit"),
    @("Require Windows Professional or higher", "Erfordert Windows Professional oder höher"),
    @("Required fields update automatically for the selected game.", "Erforderliche Felder werden für das ausgewählte Spiel automatisch aktualisiert."),
    @("Required files and Synix-created templates automatically enable a warning.", "Erforderliche Dateien und von Synix erstellte Vorlagen aktivieren automatisch eine Warnung."),
    @("Required for features such as Hyper-V that are unavailable on Windows Home.", "Erforderlich für Funktionen wie Hyper-V, die unter Windows Home nicht verfügbar sind."),
    @("Required startup arguments are dynamically injected with your specific data before initialization. You may include any additional command-line flags not covered by the default string in the Extra Arguments section.", "Erforderliche Startargumente werden vor der Initialisierung dynamisch mit Ihren spezifischen Daten eingefügt. Sie können zusätzliche Befehlszeilenoptionen, die nicht in der Standardzeichenfolge enthalten sind, im Abschnitt Zusätzliche Argumente angeben."),
    @("Required user-supplied files (relative paths, one per line)", "Erforderliche, vom Benutzer bereitgestellte Dateien (relative Pfade, einer pro Zeile)"),
    @("Resolved automatically", "Automatisch gelöst"),
    @("Resolving...", "Lösung..."),
    @("Resource Monitor", "Ressourcenmonitor"),
    @("Resource sampling was delayed  •  Retrying automatically", "Die Ressourcenprobenahme wurde verzögert. • Automatischer Wiederholungsversuch"),
    @("Restart days", "Tage neu starten"),
    @("Restart hour using a 24-hour clock", "Starten Sie die Stunde im 24-Stunden-Format neu"),
    @("Restart minute", "Minute neu starten"),
    @("Restart selected days at a configured time while preserving the current scheduler data.", "Starten Sie ausgewählte Tage zu einer konfigurierten Zeit neu und behalten Sie dabei die aktuellen Planerdaten bei."),
    @("Restart time", "Neustartzeit"),
    @("Restore Backup", "Sicherung wiederherstellen"),
    @("Restore Server Backup", "Serversicherung wiederherstellen"),
    @("RESULT", "ERGEBNIS"),
    @("Review how Synix builds the command used to start this server.", "Sehen Sie sich an, wie Synix den Befehl erstellt, der zum Starten dieses Servers verwendet wird."),
    @("Review orphaned firewall rules", "Überprüfen Sie verwaiste Firewallregeln"),
    @("Review the highlighted requirement", "Überprüfen Sie die hervorgehobene Anforderung"),
    @("Review the license terms before allowing the first server launch.", "Lesen Sie die Lizenzbedingungen, bevor Sie den ersten Serverstart zulassen."),
    @("Review these setup requirements before continuing.", "Überprüfen Sie diese Einrichtungsanforderungen, bevor Sie fortfahren."),
    @("Run All Checks", "Führen Sie alle Prüfungen durch"),
    @("Run Health Check", "Führen Sie den Gesundheitscheck durch"),
    @("Run Release Check", "Führen Sie die Release-Prüfung durch"),
    @("Run Tests", "Führen Sie Tests durch"),
    @("Running Now", "Derzeit aktiv"),
    @("Running package structure, SHA-256, and antivirus checks…", "Ausführen von Paketstruktur-, SHA-256- und Antivirenprüfungen …"),
    @("Running Servers", "Laufende Server"),
    @("Runtime requirements", "Laufzeitanforderungen"),
    @("SAFE ACTION", "SICHERES HANDELN"),
    @("Sample every", "Probieren Sie jeden"),
    @("Sanitized arguments (no secrets)", "Bereinigte Argumente (keine Geheimnisse)"),
    @("Save", "Speichern"),
    @("Save Changes", "Änderungen speichern"),
    @("Save Destination", "Ziel speichern"),
    @("Save Ordered IDs", "Bestellte IDs speichern"),
    @("Save Server", "Server speichern"),
    @("Save to Project", "Als Projekt speichern"),
    @("SCAN TO SUPPORT SYNIX", "SCANNEN, UM SYNIX ZU UNTERSTÜTZEN"),
    @("Scheduled Restarts", "Geplante Neustarts"),
    @("SCORE", "PUNKTE"),
    @("SEARCH", "SUCHE"),
    @("Search by game, executable, or support status…", "Suche nach Spiel, ausführbarer Datei oder Supportstatus ..."),
    @("Search checks titles and article text", "Die Suche prüft Titel und Artikeltext"),
    @("Search game name...", "Spielnamen suchen..."),
    @("Search game or server name...", "Spiel oder Server suchen…"),
    @("Search settings, paths, or values...", "Sucheinstellungen, Pfade oder Werte..."),
    @("Search the full knowledge base or expand a category below.", "Durchsuchen Sie die gesamte Wissensdatenbank oder erweitern Sie unten eine Kategorie."),
    @("seconds", "Sekunden"),
    @("Security", "Sicherheit"),
    @("Security review blocked the package. No files were changed.", "Die Sicherheitsüberprüfung hat das Paket blockiert. Es wurden keine Dateien geändert."),
    @("See exactly what Synix can install, configure, monitor, and query before creating a server.", "Sehen Sie sich genau an, was Synix installieren, konfigurieren, überwachen und abfragen kann, bevor Sie einen Server erstellen."),
    @("See which Discord destination receives each type of Synix notification for this server.", "Sehen Sie, welches Discord-Ziel die einzelnen Synix-Benachrichtigungstypen für diesen Server erhält."),
    @("Select a backup to continue.", "Wählen Sie ein Backup aus, um fortzufahren."),
    @("Select a game server", "Wählen Sie einen Spieleserver aus"),
    @("Select a game to update its verification evidence.", "Wählen Sie ein Spiel aus, um dessen Verifizierungsnachweise zu aktualisieren."),
    @("Select a Repair", "Wählen Sie eine Reparatur aus"),
    @("Select an Action", "Wählen Sie eine Aktion aus"),
    @("Select an installed server and validate its command.", "Wählen Sie einen installierten Server aus und validieren Sie seinen Befehl."),
    @("Selected source file", "Ausgewählte Quelldatei"),
    @("Send Command", "Befehl senden"),
    @("Send status, backups, maintenance, and problems to different Discord channels.", "Senden Sie Status, Backups, Wartung und Probleme an verschiedene Discord-Kanäle."),
    @("Send Test", "Test senden"),
    @("Send your report", "Senden Sie Ihren Bericht"),
    @("Sending a safe test message...", "Eine sichere Testnachricht wird gesendet..."),
    @("Server", "Server"),
    @("SERVER / ITEM", "SERVER / ARTIKEL"),
    @("SERVER CONFIGURATION", "SERVERKONFIGURATION"),
    @("Server Dashboard", "Serverübersicht"),
    @("Server Details", "Serverdetails"),
    @("Server executable (relative path)", "Ausführbare Datei des Servers (relativer Pfad)"),
    @("Server Folder", "Serverordner"),
    @("Server Framework", "Server-Framework"),
    @("Server Identity", "Serveridentität"),
    @("Server Info", "Serverinformationen"),
    @("Server lifecycle tracking", "Verfolgung des Serverlebenszyklus"),
    @("Server list changed during sampling  •  Retrying automatically", "Serverliste wurde während der Probenahme geändert. • Automatischer Wiederholungsversuch"),
    @("Server log locations (one relative path or wildcard pattern per line)", "Speicherorte des Serverprotokolls (ein relativer Pfad oder ein Platzhaltermuster pro Zeile)"),
    @("Server Name", "Servername"),
    @("SERVER NAME", "SERVERNAME"),
    @("Server Options  ▴", "Serveroptionen ▴"),
    @("Server Overview", "Serverübersicht"),
    @("Server Password", "Serverpasswort"),
    @("Server RAM (GB)", "Server-RAM (GB)"),
    @("Server Readiness Center", "Server-Bereitschaftscenter"),
    @("Server Setup", "Servereinrichtung"),
    @("SERVER STATUS", "SERVERSTATUS"),
    @("Server type", "Servertyp"),
    @("serverconfig.xml", "serverconfig.xml"),
    @("Servers online", "Server online"),
    @("Service Ports", "Service-Ports"),
    @("SETTING", "EINSTELLUNG"),
    @("Settings", "Einstellungen"),
    @("Short summary", "Kurze Zusammenfassung"),
    @("Show a first-start setup warning", "Zeigt eine Warnung beim Erststart an"),
    @("Show server console window", "Serverkonsolenfenster anzeigen"),
    @("Show Server Console Window", "Serverkonsolenfenster anzeigen"),
    @("Show Technical Details", "Technische Details anzeigen"),
    @("Showing 0 games", "0 Spiele werden angezeigt"),
    @("Shown for transparency so you can verify the startup command has no hidden arguments.", "Wird aus Gründen der Transparenz angezeigt, damit Sie überprüfen können, ob der Startbefehl keine versteckten Argumente enthält."),
    @("Simple view", "Einfache Ansicht"),
    @("SIZE", "GRÖSSE"),
    @("START", "STARTEN"),
    @("Start  — Not verified yet", "Start – Noch nicht verifiziert"),
    @("Start Server", "Starten Sie den Server"),
    @("Start Test", "Test starten"),
    @("Start Using Synix", "Beginnen Sie mit der Verwendung von Synix"),
    @("Starting the server through Synix. Waiting for its configured listener to respond...", "Starten des Servers über Synix. Warten auf die Antwort des konfigurierten Listeners ..."),
    @("Starts background monitoring when you sign in to Windows. Closing the Synix dashboard always exits Synix completely for the current session.", "Startet die Hintergrundüberwachung, wenn Sie sich bei Windows anmelden. Durch das Schließen des Synix-Dashboards wird Synix für die aktuelle Sitzung immer vollständig beendet."),
    @("Startup argument template", "Vorlage für Startargumente"),
    @("Startup Tasks", "Startaufgaben"),
    @("Status", "Status"),
    @("STATUS", "STATUS"),
    @("Steam account login required", "Anmeldung beim Steam-Konto erforderlich"),
    @("Steam account name", "Name des Steam-Kontos"),
    @("Steam account required", "Steam-Konto erforderlich"),
    @("Steam Account Required", "Steam-Konto erforderlich"),
    @("Steam AppID", "Steam-AppID"),
    @("Steam runtime target directory (relative path)", "Zielverzeichnis der Steam-Laufzeit (relativer Pfad)"),
    @("SteamCMD app configuration (normally blank)", "SteamCMD-App-Konfiguration (normalerweise leer)"),
    @("SteamCMD Download Speed", "SteamCMD-Download-Geschwindigkeit"),
    @("SteamCMD download speed in megabits per second", "SteamCMD-Downloadgeschwindigkeit in Megabit pro Sekunde"),
    @("SteamCMD download speed mode", "SteamCMD-Download-Geschwindigkeitsmodus"),
    @("STOP", "STOP"),
    @("Stop  — Not verified yet", "Stopp – Noch nicht verifiziert"),
    @("Stop Server", "Stoppen Sie den Server"),
    @("Stopped", "Angehalten"),
    @("Stopping the test server through Synix...", "Stoppen des Testservers über Synix..."),
    @("Store automated and manual server backup archives in a custom folder.", "Speichern Sie automatisierte und manuelle Server-Backup-Archive in einem benutzerdefinierten Ordner."),
    @("Structured View", "Strukturierte Ansicht"),
    @("Submit problem report to GitHub", "Problembericht an GitHub senden"),
    @("Submit to GitHub", "An GitHub senden"),
    @("Switch the Synix dashboard between light and dark visual themes.", "Schalten Sie das Synix-Dashboard zwischen hellen und dunklen visuellen Themen um."),
    @("Synix", "Synix"),
    @("Synix Argument Test", "Synix-Argumenttest"),
    @("Synix background service", "Synix-Hintergrunddienst"),
    @("Synix Background Service", "Synix-Hintergrunddienst"),
    @("Synix builds the real command with this server's saved settings, hides every password, starts it normally, and waits for proof that the server accepted the launch.", "Synix erstellt den eigentlichen Befehl mit den gespeicherten Einstellungen dieses Servers, verbirgt jedes Passwort, startet ihn normal und wartet auf den Beweis, dass der Server den Start akzeptiert hat."),
    @("Synix Configuration Application Check", "Überprüfung der Synix-Konfigurationsanwendung"),
    @("Synix Control Panel", "Synix-Steuerung"),
    @("SYNIX CONTROL PANEL  •  version", "SYNIX-BEDIENFELD • Version"),
    @("Synix could not verify this loader combination from the official metadata service.", "Synix konnte diese Loader-Kombination vom offiziellen Metadatendienst nicht überprüfen."),
    @("Synix does not open a public web-control port. Passwords stored by Synix are protected locally, and sensitive values are masked from its activity logs.", "Synix öffnet keinen öffentlichen Web-Control-Port. Von Synix gespeicherte Passwörter sind lokal geschützt und vertrauliche Werte werden aus den Aktivitätsprotokollen maskiert."),
    @("Synix Game Definition Builder", "Synix Game Definition Builder"),
    @("Synix Game Definition Test Runner", "Testläufer für die Synix-Spieldefinition"),
    @("Synix Game Verification Queue", "Synix-Spielverifizierungswarteschlange"),
    @("Synix Help Center", "Synix-Hilfecenter"),
    @("Synix installs Microsoft's official Bedrock Dedicated Server. Java and Java mod loaders do not apply.", "Synix installiert den offiziellen Bedrock Dedicated Server von Microsoft. Java und Java-Mod-Loader gelten nicht."),
    @("Synix installs the official Oxide runtime only. Plugins remain user-managed in the server's oxide\plugins folder.", "Synix installiert nur die offizielle Oxide-Laufzeitumgebung. Plugins werden weiterhin vom Benutzer im Ordner oxide\plugins des Servers verwaltet."),
    @("Synix installs the selected server loader and matching portable Java. Add your own mods after installation.", "Synix installiert den ausgewählten Server-Loader und passendes portables Java. Fügen Sie nach der Installation Ihre eigenen Mods hinzu."),
    @("Synix is designed to make personal game-server hosting understandable without hiding what it changes on your computer.", "Synix wurde entwickelt, um das Hosten persönlicher Spieleserver verständlich zu machen, ohne die Änderungen auf Ihrem Computer zu verbergen."),
    @("SYNIX KNOWLEDGE BASE", "SYNIX-WISSENSBASIS"),
    @("Synix logo", "Synix-Logo"),
    @("Synix Release Readiness Checker", "Synix Release Readiness Checker"),
    @("Synix Reliability Test", "Synix-Zuverlässigkeitstest"),
    @("Synix Settings", "Synix-Einstellungen"),
    @("Synix Troubleshooter", "Synix-Fehlerbehebung"),
    @("Synix Update", "Synix-Update"),
    @("Synix update is available", "Synix-Update ist verfügbar"),
    @("Synix verifies backups with integrity receipts, safely stages the selected archive, and automatically rolls back if restoration fails. The saved Synix server entry and its settings are not changed.", "Synix überprüft Backups anhand von Integritätsbelegen, stellt das ausgewählte Archiv sicher bereit und führt automatisch ein Rollback durch, wenn die Wiederherstellung fehlschlägt. Der gespeicherte Synix-Servereintrag und seine Einstellungen werden nicht geändert."),
    @("Synix verifies each action automatically after it succeeds on this PC.", "Synix überprüft jede Aktion automatisch, nachdem sie auf diesem PC erfolgreich war."),
    @("Synix version:", "Synix-Version:"),
    @("System & Server Troubleshooter", "System- und Server-Fehlerbehebung"),
    @("Tell us what you clicked, what Synix displayed, and how to make it happen again.", "Sagen Sie uns, worauf Sie geklickt haben, was Synix angezeigt hat und wie Sie es erneut ausführen können."),
    @("Template revision", "Überarbeitung der Vorlage"),
    @("Test built-in game definitions", "Testen Sie integrierte Spieldefinitionen"),
    @("Test duration", "Testdauer"),
    @("Test LAN Connectivity", "Testen Sie die LAN-Konnektivität"),
    @("Test WAN Connectivity", "Testen Sie die WAN-Konnektivität"),
    @("Testing every built-in definition and template safely...", "Alle integrierten Definitionen und Vorlagen sicher testen ..."),
    @("Tests every built-in game, managed setting binding, full configuration template, revision, path, log location, and allowlisted post-install action. Installed servers are never changed.", "Testet jedes integrierte Spiel, jede verwaltete Einstellungsbindung, jede vollständige Konfigurationsvorlage, jede Revision, jeden Pfad, jeden Protokollspeicherort und jede zugelassene Nachinstallationsaktion. Installierte Server werden niemals geändert."),
    @("The add-on was not installed.", "Das Add-on wurde nicht installiert."),
    @("The configuration report will appear here.", "Der Konfigurationsbericht wird hier angezeigt."),
    @("The game server process is not running", "Der Spielserverprozess läuft nicht"),
    @("The game server process is online", "Der Gameserver-Prozess ist online"),
    @("The game-definition tests could not finish.", "Die Spieldefinitionstests konnten nicht abgeschlossen werden."),
    @("The local connection was removed. Revoke Synix on the GitHub page that opened.", "Die lokale Verbindung wurde entfernt. Widerrufen Sie Synix auf der geöffneten GitHub-Seite."),
    @("The passwords do not match.", "Die Passwörter stimmen nicht überein."),
    @("The privacy-filtered report was copied and is ready to paste into the Discord bug forum.", "Der datenschutzgefilterte Bericht wurde kopiert und kann nun in das Discord-Fehlerforum eingefügt werden."),
    @("The readiness report will appear here.", "Der Bereitschaftsbericht wird hier angezeigt."),
    @("The release check could not finish.", "Die Freigabeprüfung konnte nicht abgeschlossen werden."),
    @("The release check was canceled. No release files were changed.", "Die Freigabeprüfung wurde abgebrochen. Es wurden keine Release-Dateien geändert."),
    @("The reliability report will appear after the requested run finishes.", "Der Zuverlässigkeitsbericht wird nach Abschluss des angeforderten Laufs angezeigt."),
    @("The schedule is saved with this server's settings.", "Der Zeitplan wird mit den Einstellungen dieses Servers gespeichert."),
    @("The selected server requires a Steam account for installation. Enter the account name that SteamCMD should use.", "Für die Installation des ausgewählten Servers ist ein Steam-Konto erforderlich. Geben Sie den Kontonamen ein, den SteamCMD verwenden soll."),
    @("The server must remain stopped during restoration", "Der Server muss während der Wiederherstellung gestoppt bleiben"),
    @("The server stopped before startup could be verified. Review its recent logs and definition.", "Der Server wurde gestoppt, bevor der Start überprüft werden konnte. Überprüfen Sie die aktuellen Protokolle und Definitionen."),
    @("The test server is stopped. The completed argument evidence is preserved.", "Der Testserver wird gestoppt. Der abgeschlossene Argumentationsbeweis bleibt erhalten."),
    @("The validation report will appear here.", "Der Validierungsbericht wird hier angezeigt."),
    @("These values are enabled only when the selected server template supports them.", "Diese Werte sind nur aktiviert, wenn die ausgewählte Servervorlage sie unterstützt."),
    @("This informational view shows the default arguments Synix uses when building the start command.", "Diese Informationsansicht zeigt die Standardargumente, die Synix beim Erstellen des Startbefehls verwendet."),
    @("TOTAL CPU", "GESAMT-CPU"),
    @("TOTAL RAM", "GESAMT-RAM"),
    @("Total system load", "Gesamtsystemlast"),
    @("Total system RAM in use", "Gesamter genutzter System-RAM"),
    @("Turn on every day when the scheduled restart should run.", "Schalten Sie es jeden Tag ein, wenn der geplante Neustart ausgeführt werden soll."),
    @("TYPE", "TYP"),
    @("Unavailable", "Nicht verfügbar"),
    @("Undo Edits", "Änderungen rückgängig machen"),
    @("Update did not start  •  Current Synix was not changed", "Das Update wurde nicht gestartet. • Die aktuelle Synix wurde nicht geändert"),
    @("Update on Start", "Update beim Start"),
    @("Update safety information appears here.", "Hier werden Sicherheitsinformationen zum Update angezeigt."),
    @("Update Server", "Update-Server"),
    @("Uptime", "Betriebszeit"),
    @("Use at least 8 characters.", "Verwenden Sie mindestens 8 Zeichen."),
    @("Use full speed or limit game-server installs, updates, repairs, and validations.", "Nutzen Sie die volle Geschwindigkeit oder begrenzen Sie die Installation, Aktualisierung, Reparatur und Validierung von Spieleservern."),
    @("Use one webhook for this server and choose the messages it should receive.", "Verwenden Sie einen Webhook für diesen Server und wählen Sie die Nachrichten aus, die er empfangen soll."),
    @("Use only when testing proves the server needs the approved Steam DLL files. The target must stay inside the server folder.", "Nur verwenden, wenn Tests ergeben, dass der Server die genehmigten Steam-DLL-Dateien benötigt. Das Ziel muss im Serverordner bleiben."),
    @("Use only when the server is deployed through Hyper-V or Windows containers.", "Nur verwenden, wenn der Server über Hyper-V- oder Windows-Container bereitgestellt wird."),
    @("Use premade game configurations", "Verwenden Sie vorgefertigte Spielkonfigurationen"),
    @("Use Synix default folder", "Verwenden Sie den Synix-Standardordner"),
    @("Uses the computer's local time and a 24-hour clock.", "Verwendet die Ortszeit des Computers und eine 24-Stunden-Uhr."),
    @("Validate & Preview", "Validieren und Vorschau"),
    @("Validate Command", "Befehl validieren"),
    @("Validate Game Files", "Spieldateien validieren"),
    @("Validated definition preview", "Validierte Definitionsvorschau"),
    @("VALUE", "WERT"),
    @("Verification queue refreshed from the saved Synix evidence.", "Die Überprüfungswarteschlange wurde anhand der gespeicherten Synix-Beweise aktualisiert."),
    @("Verification step", "Verifizierungsschritt"),
    @("Verified hardware and Windows requirements checked before Synix installs or launches the server.", "Verifizierte Hardware- und Windows-Anforderungen werden überprüft, bevor Synix den Server installiert oder startet."),
    @("Verified update package details", "Details zum Update-Paket überprüft"),
    @("Verify Backup", "Sicherung überprüfen"),
    @("Verifying archive paths and SHA-256 integrity...", "Archivpfade und SHA-256-Integrität überprüfen..."),
    @("View Default Arguments", "Standardargumente anzeigen"),
    @("View Details", "Details anzeigen"),
    @("View Discord Webhooks", "Discord-Webhooks anzeigen"),
    @("VIEWING HELP ARTICLE", "HILFEARTIKEL ANSEHEN"),
    @("Waiting for a configuration report.", "Warten auf einen Konfigurationsbericht."),
    @("Waiting for a running server process", "Warten auf einen laufenden Serverprozess"),
    @("Waiting for first sample  •  Auto-refresh every 1 second", "Warten auf die erste Probe. • Automatische Aktualisierung alle 1 Sekunde"),
    @("WEBHOOK", "WEBHOOK"),
    @("Webhook secrets stay protected", "Webhook-Geheimnisse bleiben geschützt"),
    @("WELCOME", "WILLKOMMEN"),
    @("Welcome to Synix", "Willkommen bei Synix"),
    @("Welcome to the Synix Engine Knowledge Base. Select a topic from the navigation panel to begin.", "Willkommen in der Synix Engine-Wissensdatenbank. Wählen Sie im Navigationsbereich ein Thema aus, um zu beginnen."),
    @("What happened", "Was ist passiert?"),
    @("What happened?", "Was ist passiert?"),
    @("WHAT HAPPENS AFTER YOU CONTINUE", "WAS PASSIERT, NACHDEM SIE FORTFAHREN?"),
    @("What should have happened?", "Was hätte passieren sollen?"),
    @("What Synix currently knows how to install, configure, start, monitor, and query for this game.", "Was Synix derzeit weiß, wie man dieses Spiel installiert, konfiguriert, startet, überwacht und abfragt."),
    @("What Synix was doing when the problem happened", "Was Synix tat, als das Problem auftrat"),
    @("What were you doing?", "Was hast du gemacht?"),
    @("When enabled, deleting a server requests administrator permission to remove its Windows Firewall rules. Turn this off to skip automatic cleanup during deletion.", "Wenn diese Option aktiviert ist, erfordert das Löschen eines Servers die Erlaubnis eines Administrators, seine Windows-Firewallregeln zu entfernen. Deaktivieren Sie diese Option, um die automatische Bereinigung während des Löschvorgangs zu überspringen."),
    @("WHY SYNIX FLAGGED THESE RULES", "WARUM SYNIX DIESE REGELN MARKIERT HAT"),
    @("Window title", "Fenstertitel"),
    @("Windows requests administrator permission. Synix then removes only firewall rules matching the exact executable paths above and scans again to verify the cleanup.", "Windows fordert Administratorrechte an. Synix entfernt dann nur die Firewall-Regeln, die genau den oben genannten ausführbaren Pfaden entsprechen, und führt einen erneuten Scan durch, um die Bereinigung zu überprüfen."),
    @("Windows version:", "Windows-Version:"),
    @("Windrose Invite Access", "Windrose-Einladungszugang"),
    @("World Generation", "Welterzeugung"),
    @("World Seed", "Weltsamen"),
    @("World Size", "Weltgröße"),
    @("XML", "XML"),
    @("XML structure preserved", "XML-Struktur bleibt erhalten"),
    @("You will need this password when moving Synix to the new PC. It cannot be recovered.", "Sie benötigen dieses Passwort, wenn Sie Synix auf den neuen PC übertragen. Es kann nicht wiederhergestellt werden.")
)

$translations = [System.Collections.Generic.Dictionary[string,string]]::new(
    [System.StringComparer]::Ordinal)
foreach ($pair in $pairs) {
    $translations[$pair[0]] = $pair[1]
}
$operationalTranslations = & (Join-Path $PSScriptRoot 'OperationalTranslations.de.ps1')

$sourcePath = Join-Path $PSScriptRoot 'Strings.resx'
$targetPath = Join-Path $PSScriptRoot 'Strings.de.resx'
$reader = [System.Resources.ResXResourceReader]::new($sourcePath)
$writer = [System.Resources.ResXResourceWriter]::new($targetPath)
$translatedStaticCount = 0

try {
    foreach ($entry in $semanticTranslations.GetEnumerator()) {
        $writer.AddResource($entry.Key, [string]$entry.Value)
    }

    foreach ($entry in $reader) {
        $key = [string]$entry.Key
        $english = [string]$entry.Value
        $translation = $null
		if ($semanticTranslations.Contains($key)) {
			continue
		}
		elseif ($operationalTranslations.Contains($key)) {
            $writer.AddResource($key, [string]$operationalTranslations[$key])
            $translatedStaticCount++
        }
		elseif ($translations.TryGetValue($english, [ref]$translation)) {
            $writer.AddResource($key, $translation)
            $translatedStaticCount++
        }
    }
}
finally {
    $reader.Close()
    $writer.Close()
}

Write-Host "Created Strings.de.resx with $translatedStaticCount translated static texts and $($semanticTranslations.Count) semantic texts."
