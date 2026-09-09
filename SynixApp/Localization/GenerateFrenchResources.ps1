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
    'Language.English' = "Anglais"
    'Language.French' = "Français"
    'Language.German' = "Allemand"
    'Language.Spanish' = "Espagnol"
    'Option.DownloadSpeed.Unlimited' = "Illimitée"
    'Option.DownloadSpeed.Limited' = "Limitée"
    'Message.AlreadyRunning.Body' = "Synix est déjà en cours d’exécution. Utilisez la fenêtre Synix existante."
    'Message.AlreadyRunning.Title' = "Synix est déjà ouvert"
    'Settings.VersionLabel' = "PANNEAU DE CONTRÔLE SYNIX  •  v{0}"
    'SettingsPage.General.Heading' = "Général"
    'SettingsPage.General.Subtitle' = "Configurez le comportement général de Synix sur cet ordinateur."
    'SettingsPage.Backups.Heading' = "Sauvegardes"
    'SettingsPage.Backups.Subtitle' = "Gérez les sauvegardes des serveurs ou déplacez Synix vers un autre ordinateur."
    'SettingsPage.Privacy.Heading' = "Confidentialité et sécurité"
    'SettingsPage.Privacy.Subtitle' = "Contrôlez l’affichage des informations sensibles des serveurs."
    'SettingsPage.Advanced.Heading' = "Avancé"
    'SettingsPage.Advanced.Subtitle' = "Configurez les opérations avec élévation et le comportement avancé du système."
    'SettingsPage.ReportProblem.Heading' = "Signaler un problème"
    'SettingsPage.ReportProblem.Subtitle' = "Créez un rapport de compatibilité filtré pour préserver la confidentialité."
    'SettingsPage.Development.Heading' = "Développement"
    'SettingsPage.Development.Subtitle' = "Gérez la collecte des configurations et les outils de test des versions."
    'Menu.ModPluginManager' = "Gestionnaire de mods et de modules"
    'Menu.PlayerManagementCenter' = "Centre de gestion des joueurs"
    'Menu.MinecraftServerConsole' = "Console du serveur Minecraft"
    'Menu.ConnectionInformation' = "Informations de connexion"
    'Menu.LiveProcessDetails' = "Détails des processus en direct"
    'Option.Status.All' = "Tous les états"
    'Option.Status.Running' = "En cours"
    'Option.Status.Stopped' = "Arrêté"
    'Option.Status.InProgress' = "Opération en cours"
    'Option.Status.NeedsAttention' = "Attention requise"
    'Option.Discord.AllEvents' = "Tous les événements"
    'Option.Discord.ServerStatus' = "État du serveur"
    'Option.Discord.Maintenance' = "Maintenance"
    'Option.Discord.ProblemsOnly' = "Problèmes uniquement"
    'Option.Discord.Custom' = "Personnalisé"
    'Option.ConfigType.All' = "Tous les types"
    'Option.ConfigType.Text' = "TEXTE"
    'Option.ConfigType.Number' = "NOMBRE"
    'Option.ConfigType.Boolean' = "BOOLÉEN"
    'Option.ConfigType.Secret' = "SECRET"
    'Option.ConfigType.Null' = "NUL"
    'Option.VerificationFilter.NeedsWork' = "Travail requis"
    'Option.VerificationFilter.UnknownConfiguration' = "Configuration inconnue"
    'Option.VerificationFilter.PartiallyVerified' = "Partiellement vérifié"
    'Option.VerificationFilter.FullyVerified' = "Entièrement vérifié"
    'Option.VerificationFilter.AllGames' = "Tous les jeux"
    'VerificationStep.Install' = "Installation"
    'VerificationStep.Start' = "Démarrage"
    'VerificationStep.Stop' = "Arrêt"
    'VerificationStep.Monitoring' = "Surveillance"
    'VerificationStep.Arguments' = "Arguments"
    'VerificationStep.Configuration' = "Configuration"
    'Status.Stopped' = "Arrêté"
    'Status.Running' = "En cours"
    'Status.Starting' = "Démarrage"
    'Status.Crashed' = "En panne"
    'Status.Stopping' = "Arrêt en cours"
    'Status.Installing' = "Installation"
    'Status.Updating' = "Mise à jour"
    'Status.BackingUp' = "Sauvegarde"
    'Status.Validating' = "Validation"
    'Status.Exporting' = "Exportation"
    'Status.Restoring' = "Restauration"
    'Status.Deleting' = "Suppression"
    'Status.Unknown' = "Inconnu"
    'Dashboard.ServerCount.One' = "{0} serveur"
    'Dashboard.ServerCount.Many' = "{0} serveurs"
    'Dashboard.ServerCount.Filtered' = "{0} sur {1} serveurs"
    'Dashboard.Network.PublicFetching' = "IP publique : récupération…"
    'Dashboard.Network.LocalFetching' = "IP du réseau local : récupération…"
    'Dashboard.Network.PublicAddress' = "IP publique : {0}"
    'Dashboard.Network.LocalAddress' = "IP du réseau local : {0}"
    'Dashboard.Network.PublicHidden' = "IP publique : [MASQUÉE]"
    'Dashboard.Network.LocalHidden' = "IP du réseau local : [MASQUÉE]"
    'Dashboard.CpuGaugeLabel' = "CPU %"
    'Dashboard.RamGaugeLabel' = "RAM Go"
    'Dashboard.CpuValue' = "{0:0.0} %"
    'Dashboard.RamValue' = "{0:0.00} Go"
    'ServerSetup.Status.Ready' = "●  Prêt à enregistrer"
    'ServerSetup.Status.ActionRequired' = "●  Action requise"
    'ServerSetup.Status.AllChecksPassed' = "Toutes les vérifications requises ont réussi"
    'ServerSetup.Status.SeeValidationMessage' = "Consultez le message de validation ci-dessous"
    'ServerSetup.Completion' = "Configuration : {0} %"
    'ServerSetup.Window.EditTitle' = "Modifier le serveur"
    'ServerSetup.ModeBadge.Edit' = "MODIFIER LE SERVEUR"
    'ServerSetup.Page.Security.Description' = "Gérez les mots de passe du serveur et les identifiants des services en ligne."
    'ServerSetup.Page.World.Description' = "Configurez la graine, la taille et les options du monde propres au jeu."
    'ServerSetup.Page.Network.Description' = "Attribuez les ports de service et sécurisez l’administration à distance."
    'ServerSetup.Page.Network.BeginnerDescription' = "Utilisez les ports de jeu et de requête recommandés. Le mode avancé ajoute les commandes RCON."
    'ServerSetup.Page.Automation.Description' = "Contrôlez les tâches de démarrage, les redémarrages planifiés, les sauvegardes et les alertes."
    'ServerSetup.Page.Discord.Title' = "Notifications Discord"
    'ServerSetup.Page.Discord.Description' = "Utilisez un webhook principal ou acheminez les événements Synix vers plusieurs salons Discord."
    'ServerSetup.Page.Install.Description' = "Choisissez l’emplacement du serveur et personnalisez les arguments de lancement."
    'ServerSetup.Page.Install.BeginnerDescription' = "Choisissez où installer le serveur. Synix fournit les paramètres de lancement recommandés."
    'ServerSetup.Mode.Advanced' = "Mode : Avancé"
    'ServerSetup.Mode.Beginner' = "Mode : Débutant"
    'ServerSetup.Mode.Advanced.AccessibleName' = "Mode de configuration avancé. Cliquez pour utiliser le mode débutant."
    'ServerSetup.Mode.Beginner.AccessibleName' = "Mode de configuration débutant. Cliquez pour afficher les paramètres avancés."
    'ServerSetup.Navigation.AttentionRequired' = "{0} contient des paramètres à corriger avant l’enregistrement."
    'ServerSetup.Navigation.NoAttentionRequired' = "{0} ne contient aucun paramètre nécessitant votre attention."
    'ServerSetup.Validation.Waiting' = "La validation attend les informations requises du serveur."
    'ServerSetup.Validation.ServerNameAndGameRequired' = "  🔒 [REQUIS] Saisissez un nom de serveur et sélectionnez un modèle de jeu."
    'ServerSetup.Validation.ServerNameRequired' = "  🔒 [REQUIS] Saisissez un nom avant d’enregistrer ce serveur."
    'ServerSetup.Validation.GameRequired' = "  🔒 [REQUIS] Sélectionnez un modèle de jeu avant d’enregistrer ce serveur."
    'ServerSetup.Validation.MinecraftLoading' = "  ◌ [MINECRAFT] Chargement des versions compatibles et des prérequis Java…"
    'ServerSetup.Validation.MinecraftDetail' = "  ⚠️ [MINECRAFT] {0}"
    'ServerSetup.Validation.MinecraftVersionRequired' = "  🔒 [MINECRAFT] Sélectionnez une version du jeu Minecraft."
    'ServerSetup.Validation.AdminPasswordRequired' = "  🔒 [REQUIS] Saisissez un mot de passe administrateur pour protéger le rôle d’administration du serveur."
    'ServerSetup.Validation.AuthenticationTokenRequired' = "  🔒 [REQUIS] Saisissez le champ obligatoire {0} avant d’enregistrer ce serveur."
    'ServerSetup.Validation.RequiredDetail' = "  🔒 [REQUIS] {0}"
    'ServerSetup.Validation.MinecraftLoaderRequired' = "  🔒 [MINECRAFT] Aucune version compatible du chargeur n’est sélectionnée."
    'ServerSetup.Validation.RequirementDetail' = "  ⚠️ [PRÉREQUIS] {0}"
    'ServerSetup.Validation.NameConflict' = "  ⚠️ [CONFLIT] Le nom « {0} » est déjà utilisé pour {1}."
    'ServerSetup.Validation.ScheduleDayRequired' = "  🔒 [REQUIS] Sélectionnez au moins un jour pour le redémarrage automatique."
    'ServerSetup.Validation.InstallFolderRequired' = "  🔒 [REQUIS] Sélectionnez un dossier d’installation ou activez le chemin par défaut."
    'ServerSetup.Validation.LaunchDetail' = "  ⚠️ [LANCEMENT] {0}"
    'ServerSetup.Validation.DiscordDetail' = "  🔒 [DISCORD] {0}"
    'ServerSetup.Validation.ReadyNote' = "  ✔ [PRÊT] REMARQUE : {0}"
    'ServerSetup.Validation.Updating' = "  ✔ [PRÊT] Mise à jour : {0}"
    'ServerSetup.Validation.Ready' = "  ✔ [PRÊT] La configuration est valide et sûre."
    'ServerSetup.Validation.Error' = "  ⚠️ [ERREUR DE VALIDATION] La validation n’a pas pu aboutir : {0}"
    'ServerSetup.Validation.DuplicatePort' = "  ⚠️ [CONFLIT] {0} ne peuvent pas utiliser le même port {1}."
    'ServerSetup.Validation.PortBlocked' = "  ⚠️ [CONFLIT] {0} {1} est bloqué par : {2}"
    'ServerSetup.ConfigurationSupport' = "◇  PRISE EN CHARGE DE LA CONFIGURATION : {0}  •  {1}"
    'ServerSetup.PortMapping.SelectGame' = "Sélectionnez un jeu pour afficher ses ports gérés."
    'ServerSetup.PortMapping.AllMapped' = "Tous les ports déclarés sont définis par les arguments ou la configuration."
    'ServerSetup.PortMapping.NeedsMapping' = "Mappage requis : {0} (arguments ou modèle de configuration)."
    'ServerSetup.Port.Ipv6' = "Port IPv6"
    'ServerSetup.Port.SystemProcess' = "Processus système"
    'ServerSetup.List.AndSeparator' = " et "
    'ServerSetup.Credentials.UnlockFailed.Title' = "Saisir à nouveau les identifiants du serveur"
    'ServerSetup.Credentials.UnlockFailed.Body' = "Synix n’a pas pu déverrouiller les mots de passe, le jeton d’authentification ou les webhooks Discord enregistrés pour ce serveur. Ils peuvent provenir d’un autre utilisateur Windows ou d’un autre ordinateur.`n`nSaisissez à nouveau les identifiants, puis cliquez sur Enregistrer les modifications pour les protéger pour cet utilisateur Windows."
    'ServerSetup.Dialog.SettingsAttention.Title' = "Paramètres du serveur à vérifier"
    'ServerSetup.Dialog.ExtraArgumentsBlocked.Title' = "Arguments supplémentaires bloqués"
    'ServerSetup.Dialog.DiscordAttention.Title' = "Paramètres Discord à vérifier"
    'ServerSetup.Dialog.IllegalInput.Title' = "Saisie bloquée"
    'ServerSetup.Dialog.IllegalInput.Body' = "Alerte de sécurité : une de vos saisies contient des caractères interdits."
    'ServerSetup.ErrorAction.SaveMode' = "enregistrer le mode de configuration"
    'ServerSetup.ErrorAction.SaveSettings' = "enregistrer les paramètres du serveur"
    'ServerSetup.ErrorAction.OpenTokenPage' = "ouvrir la page du jeton d’authentification"
    'ServerSetup.GamePicker.Placeholder' = "-- Choisir un jeu --"
    'ServerSetup.Placeholder.SelectGame' = "Sélectionnez un jeu…"
    'ServerSetup.Automation.EnableSchedule.AccessibleName' = "Activer le planificateur"
    'ServerSetup.Install.DefaultFolder.AccessibleName' = "Dossier par défaut"
    'ServerSetup.Verification.LastTested.Verified' = "Dernière version Synix testée : v{0}  •  {1:d}"
    'ServerSetup.Verification.Verified' = "{0}  ✓ Vérifié"
    'ServerSetup.Verification.Unverified' = "{0}  — Pas encore vérifié"
    'ServerSetup.Minecraft.MetadataLoadFailed' = "Impossible de charger les métadonnées : {0}"
    'ServerSetup.Minecraft.MojangVersionsLoadFailed' = "Impossible de charger les versions Mojang : {0}"
    'ServerSetup.Minecraft.LoadingBuilds' = "Chargement des versions compatibles…"
    'ServerSetup.Minecraft.JavaVersion' = "Java {0}"
    'ServerSetup.Minecraft.Helper.Vanilla' = "Synix installe le serveur officiel et la version portable de Java correspondante."
    'ServerSetup.Minecraft.Helper.Loader' = "Synix installe le chargeur de serveur {0} compatible. Ajoutez vos propres mods après l’installation."
    'ServerSetup.Minecraft.NoCompatibleBuild' = "Aucune version de serveur {0} compatible n’existe pour Minecraft {1}."
    'ServerSetup.Minecraft.Helper.ResolvedVanilla' = "Minecraft {0} utilise le serveur Mojang officiel et Java {3}."
    'ServerSetup.Minecraft.Helper.ResolvedLoader' = "Minecraft {0} + {1} {2} utilise Java {3}. Ajoutez les mods après l’installation."
    'ServerSetup.Minecraft.RetryDetail' = "{0} Sélectionnez à nouveau la version ou le chargeur pour réessayer."
    'ServerSetup.Runtime.ServerPackage' = "Paquet du serveur"
    'ServerSetup.Runtime.OfficialBedrock' = "Bedrock officiel"
    'ServerSetup.MaxPlayers.Limited' = "Joueurs max. (maximum {0:0})"
    'ServerSetup.SteamAccount.Restore.WindowTitle' = "Restaurer l’autorisation Steam"
    'ServerSetup.SteamAccount.Restore.Title' = "Restaurer l’autorisation Steam"
    'ServerSetup.SteamAccount.Restore.Description' = "{0} a été importé sur cet ordinateur. Confirmez le nom du compte Steam afin que SteamCMD puisse restaurer l’accès avant le premier démarrage."
    'ServerSetup.SteamAccount.Required.Description' = "{0} nécessite un compte Steam pour l’installation. Saisissez le nom du compte que SteamCMD doit utiliser."
    'ProblemAction.ServerInstallation' = "Installation du serveur"
    'ProblemAction.UpdateValidation' = "Mise à jour ou validation des fichiers du serveur"
    'ProblemAction.ServerStartup' = "Démarrage du serveur"
    'ProblemAction.ServerShutdown' = "Arrêt du serveur"
    'ProblemAction.RestartWatchdog' = "Redémarrage du serveur ou surveillance"
    'ProblemAction.IncorrectStatus' = "État incorrect du serveur"
    'ProblemAction.ResourceMonitoring' = "Surveillance du processeur, de la mémoire ou des joueurs"
    'ProblemAction.LocalNetwork' = "Connexion au réseau local"
    'ProblemAction.PublicNetwork' = "Connexion Internet ou publique"
    'ProblemAction.PortsFirewallRcon' = "Ports, pare-feu ou RCON"
    'ProblemAction.ServerBackups' = "Sauvegardes du serveur"
    'ProblemAction.TransferExport' = "Exportation de transfert"
    'ProblemAction.TransferImport' = "Importation de transfert"
    'ProblemAction.TransferVerification' = "Vérification du paquet de transfert"
    'ProblemAction.SettingsPasswords' = "Paramètres ou mots de passe du serveur"
    'ProblemAction.DiscordAlerts' = "Alertes Discord"
    'ProblemAction.SynixUpdate' = "Mise à jour de Synix"
    'ProblemAction.InstallationPackaging' = "Installation MSI, WinGet ou autonome"
    'ProblemAction.WindowDisplay' = "Problème de fenêtre ou d’affichage"
    'ProblemAction.CrashFreeze' = "Blocage ou plantage de Synix"
    'ProblemAction.TemplateLaunch' = "Modèle de serveur ou comportement de lancement"
    'ProblemAction.Other' = "Autre"
    'Report.EnglishRequiredWarning' = "Important : rédigez le résumé et les détails du rapport en anglais afin que l’équipe d’assistance Synix puisse les examiner."
    'Advanced.Firewall.ButtonChecking' = "Vérification du pare-feu…"
    'Advanced.Firewall.CheckingPaths' = "Vérification des chemins de programmes du Pare-feu Windows…"
    'Advanced.Firewall.Canceled' = "Nettoyage annulé. Aucune règle de pare-feu n’a été modifiée."
    'Advanced.Firewall.WaitingForAdmin' = "En attente de l’autorisation d’administrateur…"
    'Advanced.Firewall.RemovedVerified' = "{0} chemin(s) d’exécutable orphelin(s) supprimé(s) et vérifié(s)."
    'Advanced.Firewall.NoneFound' = "Aucune règle de pare-feu orpheline n’a été trouvée dans le dossier Synix Games par défaut."
    'Advanced.Background.EnabledCurrent' = "Activé pour la connexion Windows — Fermer Synix le quitte toujours complètement."
    'Advanced.Background.DisabledCurrent' = "Désactivé — les tâches planifiées s’exécutent uniquement lorsque Synix est ouvert."
    'Advanced.Background.EnabledResult' = "Activé pour la connexion Windows. Fermer Synix arrête toujours tous les processus Synix de la session actuelle."
    'Advanced.Background.DisabledResult' = "Désactivé. La surveillance en arrière-plan s’arrêtera et ne démarrera pas à la connexion."
    'AddServer.Title' = "Ajouter un serveur"
    'AddServer.Heading' = "Comment souhaitez-vous ajouter un serveur ?"
    'AddServer.Subtitle' = "Synix peut installer un nouveau serveur ou enregistrer en toute sécurité des fichiers déjà présents sur cet ordinateur."
    'AddServer.Create.Title' = "Créer et installer un nouveau serveur"
    'AddServer.Create.Description' = "Choisissez le jeu et les paramètres, puis laissez Synix télécharger les fichiers du serveur."
    'AddServer.Create.Button' = "Créer"
    'AddServer.Import.Title' = "Importer un serveur existant"
    'AddServer.Import.Description' = "Indiquez un dossier de serveur existant. Vos fichiers ne seront ni déplacés ni remplacés."
    'AddServer.Import.Button' = "Importer l’existant"
    'AddServer.Catalog.Title' = "Vérifier d’abord la prise en charge du jeu"
    'AddServer.Catalog.Description' = "Consultez le catalogue pour vérifier la prise en charge de l’exécutable, de la configuration, du jeu multiplateforme et des requêtes de joueurs."
    'AddServer.Catalog.Button' = "Voir le catalogue"
    'Connection.Heading' = "Se connecter à {0}"
    'Connection.Subtitle' = "Utilisez l’adresse correspondant à l’emplacement depuis lequel le joueur se connecte."
    'Connection.Local.Title' = "Même ordinateur ou réseau domestique"
    'Connection.Local.Description' = "Utilisez cette adresse pour les joueurs connectés au même routeur."
    'Connection.Public.Title' = "Amis se connectant par Internet"
    'Connection.Public.Description' = "Votre routeur et le Pare-feu Windows doivent autoriser les ports du jeu et de requête."
    'Connection.Public.BedrockDescription' = "Votre routeur et le Pare-feu Windows doivent autoriser le port de jeu UDP de Bedrock."
    'Connection.Ports.StandardSummary' = "Ports configurés : {0}. Certains jeux apparaissent dans le navigateur de serveurs uniquement lorsque le port de requête est également redirigé."
    'Connection.Ports.BedrockSummary' = "Port de jeu Bedrock : {0}/UDP. Port IPv6 : {1}/UDP. Chaque serveur Bedrock nécessite sa propre paire de ports."
    'Connection.Port.Game' = "jeu {0}"
    'Connection.Port.Query' = "requête {0}"
    'Connection.Port.Rcon' = "RCON {0}"
    'Connection.Port.App' = "application {0}"
    'Connection.Address.Hidden' = "Masquée par le mode de confidentialité"
    'Connection.Address.PublicUnavailable' = "Impossible de charger l’adresse publique"
    'Connection.Address.Unavailable' = "Impossible de charger l’adresse"
    'PlayerCenter.Summary.One' = "{0} • {1} • 1 joueur nommé"
    'PlayerCenter.Summary.Many' = "{0} • {1} • {2} joueurs nommés"
    'PlayerCenter.Loading' = "Chargement des détails des joueurs…"
    'PlayerCenter.Guidance.Minecraft' = " Sélectionnez un joueur pour utiliser les commandes d’administration locale de Minecraft."
    'PlayerCenter.Guidance.UnsupportedActions' = " Les actions sur les joueurs restent désactivées sauf si le jeu fournit un protocole d’administration vérifié."
    'PlayerCenter.Action.Kick' = "Expulser"
    'PlayerCenter.Action.Allowlist' = "Ajouter à la liste blanche"
    'PlayerCenter.Action.Operator' = "Nommer opérateur"
    'PlayerCenter.SelectValidPlayer' = "Sélectionnez d’abord un joueur Minecraft valide."
    'PlayerCenter.Confirm.Title' = "Confirmer l’action sur le joueur Minecraft"
    'PlayerCenter.Confirm.Kick' = "Voulez-vous expulser ce joueur : {0} ?"
    'PlayerCenter.Confirm.Allowlist' = "Voulez-vous ajouter ce joueur à la liste blanche : {0} ?"
    'PlayerCenter.Confirm.Operator' = "Voulez-vous nommer ce joueur opérateur : {0} ?"
    'PlayerQuery.GameDefinitionUnavailable' = "La définition du jeu n’est pas disponible."
    'PlayerQuery.CrossplayUnavailable' = "Le suivi des joueurs n’est pas disponible lorsque le jeu multiplateforme est activé. Désactivez-le pour utiliser le suivi des joueurs Steam A2S."
    'PlayerQuery.ProtocolUnavailable' = "Le protocole de requête actuel de ce jeu ne fournit pas une liste universelle et sûre des noms de joueurs."
    'PlayerQuery.MinecraftCountOnly' = "Minecraft signale {0} joueur(s) connecté(s), mais cette requête serveur ne publie pas leurs noms."
    'PlayerQuery.StartServerFirst' = "Démarrez le serveur avant d’actualiser les détails des joueurs."
    'PlayerQuery.InvalidA2sResponse' = "Le serveur a renvoyé une réponse de joueurs A2S non valide."
    'PlayerQuery.IncompatiblePlayerList' = "La requête serveur fonctionne, mais elle n’a pas fourni de liste de joueurs compatible."
    'PlayerQuery.NoNamedPlayers' = "Le serveur a répondu et aucun joueur nommé n’est connecté."
    'PlayerQuery.LoadedPlayers' = "{0} joueur(s) connecté(s) chargé(s)."
    'PlayerQuery.Timeout' = "La requête de joueurs sur le port UDP {0} a expiré."
    'PlayerQuery.ConnectionFailed' = "La requête de joueurs n’a pas pu se connecter : {0}"
    'PlayerQuery.ReadFailed' = "Impossible de lire les détails des joueurs : {0}"
    'PlayerQuery.BedrockCountOnly' = "Minecraft Bedrock signale {0} joueur(s) connecté(s), mais sa réponse d’état intégrée ne publie pas leurs noms."
    'PlayerQuery.MinecraftManagement.None' = "Le service de gestion locale de Minecraft ne signale aucun joueur connecté."
    'PlayerQuery.MinecraftManagement.Loaded' = "{0} joueur(s) chargé(s) par le service de gestion locale de Minecraft."
    'PlayerQuery.MinecraftRcon.None' = "Minecraft RCON ne signale aucun joueur connecté."
    'PlayerQuery.MinecraftRcon.Loaded' = "{0} joueur(s) chargé(s) par le RCON Minecraft local."
    'PlayerQuery.MinecraftUnavailable' = "Les détails des joueurs Minecraft ne sont pas encore disponibles."
    'PlayerQuery.UnnamedPlayer' = "Joueur sans nom"
    'ModManager.Subtitle' = "Découvrez les éléments déjà installés, ajoutez des paquets locaux en toute sécurité et conservez un point de restauration sans gérer une liste de chaque mod."
    'ModManager.Field.Server' = "SERVEUR"
    'ModManager.Field.System' = "SYSTÈME DE MODULES"
    'ModManager.Field.InstallArea' = "ZONE D’INSTALLATION"
    'ModManager.Support.Checking' = "Vérification de la prise en charge…"
    'ModManager.Step.Detect' = "1  Détecter"
    'ModManager.Step.Stop' = "2  Arrêter le serveur"
    'ModManager.Step.Backup' = "3  Sauvegarder les fichiers"
    'ModManager.Step.Install' = "4  Installer"
    'ModManager.Step.Verify' = "5  Vérifier"
    'ModManager.Step.Restart' = "6  Redémarrer si nécessaire"
    'ModManager.Column.AddOn' = "MODULE"
    'ModManager.Column.Type' = "TYPE"
    'ModManager.Column.Version' = "VERSION"
    'ModManager.Column.Status' = "ÉTAT"
    'ModManager.Column.Security' = "SÉCURITÉ"
    'ModManager.Column.Source' = "SOURCE"
    'ModManager.Column.Location' = "EMPLACEMENT"
    'ModManager.Safety.Title' = "Liste de contrôle de sécurité automatique"
    'ModManager.Safety.Subtitle' = "Synix vérifie ces éléments avant toute modification."
    'ModManager.Selection.Empty' = "Sélectionnez un module pour voir où il a été trouvé."
    'ModManager.Button.InstallFile' = "Installer un fichier"
    'ModManager.Button.InstallFramework' = "Installer l’infrastructure"
    'ModManager.Button.BrowseCatalog' = "Parcourir le catalogue"
    'ModManager.Button.BrowseCatalogs' = "Parcourir les catalogues"
    'ModManager.Button.OpenFolder' = "Ouvrir le dossier des modules"
    'ModManager.Button.Refresh' = "Actualiser"
    'ModManager.Button.Remove' = "Supprimer la sélection"
    'ModManager.Button.Close' = "Fermer"
    'ModManager.Button.ManageIds' = "Gérer les identifiants de mods"
    'ModManager.Inventory.Empty' = "Aucun module n’a été trouvé dans les dossiers du profil actif."
    'ModManager.Inventory.One' = "1 module trouvé  •  {1} suivi par Synix"
    'ModManager.Inventory.Many' = "{0} modules trouvés  •  {1} suivis par Synix"
    'ModManager.Inventory.RefreshFailed' = "Synix n’a pas pu actualiser les dossiers de modules."
    'ModManager.Support.ProviderIds' = "PRÊT • Synix gère la liste ordonnée des identifiants de mods du fournisseur"
    'ModManager.Support.FileImport' = "PRÊT • Synix peut importer les fichiers de modules locaux en toute sécurité"
    'ModManager.Support.SetupNeeded' = "CONFIGURATION REQUISE • Sélectionnez ou installez d’abord une infrastructure compatible"
    'ModManager.Support.DetectionOnly' = "DÉTECTION UNIQUEMENT • Le fournisseur du jeu reste responsable de l’installation"
    'ModManager.Framework.Automatic' = "Le chargeur du serveur et les dossiers existants choisissent automatiquement la zone d’installation."
    'ModManager.Framework.Named' = "Infrastructure : {0}."
    'ModManager.Unsupported.Title' = "AUCUN PROFIL DE MODULE POUR LE MOMENT"
    'ModManager.Unsupported.Description' = "Synix ne devinera pas où ce jeu stocke ses mods. Un petit profil de données pourra ajouter la prise en charge sans réécrire cette fenêtre."
    'ModManager.NoFilesChanged' = "Aucun fichier n’a été modifié."
    'ModManager.Safety.ServerStopped' = "Le serveur est arrêté"
    'ModManager.Safety.StopFirst' = "Arrêtez le serveur avant les modifications"
    'ModManager.Safety.FrameworkDetected' = "Infrastructure détectée"
    'ModManager.Safety.FrameworkRequired' = "Configuration de l’infrastructure requise"
    'ModManager.Safety.FolderAvailable' = "Dossier du serveur disponible"
    'ModManager.Safety.FolderMissing' = "Dossier du serveur introuvable"
    'ModManager.Safety.ProviderTrust' = "Le téléchargement du fournisseur nécessite une confiance manuelle"
    'ModManager.Safety.SecurityScan' = "L’analyse de sécurité s’exécute avant l’installation"
    'ModManager.Safety.StandardPermissions' = "Autorisations Windows standard"
    'ModManager.Safety.RestartWithoutAdmin' = "Redémarrez sans droits d’administrateur"
    'ModManager.Safety.RestartRequired' = "Redémarrage requis après les modifications"
    'ModManager.Safety.LiveReload' = "L’infrastructure prend en charge le rechargement à chaud"
    'ModManager.Profile.Rust.Description' = "Modules Rust chargés par l’infrastructure Oxide/uMod."
    'ModManager.Profile.Rust.Target' = "Modules Oxide"
    'ModManager.Profile.Minecraft.Name' = "Modules Minecraft"
    'ModManager.Profile.Minecraft.Description' = "Modules JAR sélectionnés selon le chargeur du serveur et les dossiers déjà présents sur le disque."
    'ModManager.Profile.Minecraft.ModsTarget' = "Mods du chargeur"
    'ModManager.Profile.Minecraft.PluginsTarget' = "Modules du serveur"
    'ModManager.Profile.SevenDays.Name' = "Mods du serveur 7 Days to Die"
    'ModManager.Profile.SevenDays.Description' = "Synix installe les paquets ZIP complets des mods dans le dossier Mods du serveur dédié. Les mods contenant des ressources client peuvent aussi devoir être installés par chaque joueur."
    'ModManager.Profile.SevenDays.Target' = "Dossier Mods du serveur"
    'ModManager.Profile.ArkEvolved.Name' = "Mods Steam Workshop"
    'ModManager.Profile.ArkEvolved.Description' = "Synix gère les identifiants Steam Workshop ordonnés ; ARK et Steam téléchargent et mettent à jour le contenu réel."
    'ModManager.Profile.ArkEvolved.Target' = "Identifiants Steam Workshop ordonnés"
    'ModManager.Profile.ArkAscended.Name' = "Mods serveur CurseForge"
    'ModManager.Profile.ArkAscended.Description' = "Synix gère la liste ordonnée des identifiants de mods ; ARK télécharge et met à jour le contenu CurseForge réel au démarrage du serveur."
    'ModManager.Profile.ArkAscended.Target' = "Identifiants de mods CurseForge ordonnés"
    'ModManager.Profile.Discovered.Name' = "Dossiers de modules détectés"
    'ModManager.Profile.Discovered.Description' = "Synix a trouvé des dossiers de modules courants et peut les inventorier en toute sécurité. L’installation reste désactivée jusqu’à l’ajout d’un profil de données vérifié."
    'ModManager.Known.Mod' = "Mod"
    'ModManager.Known.Plugin' = "Module"
    'ModManager.Known.ModId' = "Identifiant de mod"
    'ModManager.Known.ProviderManaged' = "Géré par le fournisseur"
    'ModManager.Known.ConfiguredNextStart' = "Configuré pour le prochain démarrage"
    'ModManager.Known.ProviderNotScanned' = "Téléchargement du fournisseur non analysé à l’avance"
    'ModManager.Known.GameProvider' = "Fournisseur du jeu"
    'ModManager.Known.Detected' = "Détecté sur le disque"
    'ModManager.Known.Healthy' = "Sain"
    'ModManager.Known.Changed' = "Modifié hors de Synix"
    'ModManager.Known.NotReviewed' = "Non vérifié par Synix"
    'ModManager.Known.LegacyNotReviewed' = "Ancienne installation • non vérifiée"
    'ModManager.Known.StructuralOnly' = "Vérifications structurelles uniquement"
    'ModManager.Known.ReviewRecorded' = "Vérification avant installation enregistrée"
    'ModManager.Known.External' = "Externe"
    'ModManager.Known.ExternalProvider' = "Fournisseur externe"
    'ModManager.Known.SynixImport' = "Importation Synix"
    'ModManager.Known.LocalPackage' = "Paquet local"
    'ModManager.Known.BuiltInLoader' = "Chargeur de mods intégré"
    'ModManager.Known.ArkBuiltInInstaller' = "Programme d’installation de mods intégré d’ARK"
    'ResourceMonitor.WindowTitleFiltered' = "Détails des processus en direct - {0}"
    'ResourceMonitor.GridTitleFiltered' = "Détails des processus en direct  •  {0}"
    'ResourceMonitor.FilteredSubtitle' = "Chaque lanceur, hôte de console et processus de jeu vérifié par Synix dans ce groupe de serveurs."
    'ResourceMonitor.RowRunning' = "●  En cours"
    'ResourceMonitor.CpuCaption' = "Pour tous les processus de serveur gérés"
    'ResourceMonitor.RamValue' = "{0:N2} Go"
    'ResourceMonitor.RamCaption' = "{0:N1} % des {1:N1} Go de mémoire système"
    'ResourceMonitor.Active.None' = "Aucun processus de serveur en cours détecté"
    'ResourceMonitor.Active.One' = "1 processus de serveur est actuellement en ligne"
    'ResourceMonitor.Active.Many' = "{0} processus de serveur sont actuellement en ligne"
    'ResourceMonitor.ProcessCount.One' = "1 processus en cours"
    'ResourceMonitor.ProcessCount.Many' = "{0} processus en cours"
    'ResourceMonitor.LastUpdated' = "Mis à jour à {0:T}  •  Actualisation automatique chaque seconde"
    'ResourceMonitor.Empty' = "Aucun serveur de jeu en cours détecté"
}

$additionalSemanticTranslations = & (Join-Path $PSScriptRoot 'SemanticResources.fr.ps1')
foreach ($entry in $additionalSemanticTranslations.GetEnumerator()) {
    if ($semanticTranslations.Contains($entry.Key)) {
        throw "Duplicate French semantic resource key: $($entry.Key)"
    }

    $semanticTranslations[$entry.Key] = [string]$entry.Value
}

$pairs = @(
    @('.NET Framework requirement', "Prérequis .NET Framework"),
    @('↻  Restart', "↻  Redémarrer"),
    @('+  Add Server', "+  Ajouter un serveur"),
    @('■  Stop', "■  Arrêter"),
    @('▶  Start', "▶  Démarrer"),
    @('◇  Sensitive fields follow the Synix Privacy Mode setting.', "◇  Les champs sensibles suivent le réglage du mode de confidentialité de Synix."),
    @('◇  Template-aware controls: unavailable settings are disabled automatically for the selected game.', "◇  Commandes adaptées au modèle : les réglages indisponibles sont automatiquement désactivés pour le jeu sélectionné."),
    @('●  Action required', "●  Action requise"),
    @('●  LIVE MONITORING', "●  SURVEILLANCE EN DIRECT"),
    @('●  LIVE TELEMETRY', "●  TÉLÉMÉTRIE EN DIRECT"),
    @('●  SteamCMD ready', "●  SteamCMD prêt"),
    @('⚠ Changing this location does not delete backups from the previous folder.', "⚠ Changer cet emplacement ne supprime pas les sauvegardes de l’ancien dossier."),
    @('✓  Readiness', "✓  Disponibilité"),
    @('🔒 [REQUIRED] Enter a Server Name and select a Game Template.', "🔒 [REQUIS] Saisissez un nom de serveur et sélectionnez un modèle de jeu."),
    @('0 help articles', "0 article d’aide"),
    @('0 running servers', "0 serveur en cours"),
    @('0 servers', "0 serveur"),
    @('0 settings', "0 paramètre"),
    @('0 unsaved changes', "0 modification non enregistrée"),
    @('0.0% of system memory', "0,0 % de la mémoire système"),
    @('1–100 per server', "1 à 100 par serveur"),
    @('Access controls, startup behavior, and integrations', "Contrôles d’accès, comportement au démarrage et intégrations"),
    @('Access Credentials', "Identifiants d’accès"),
    @('Across all managed server processes', "Pour tous les processus de serveur gérés"),
    @('ACTIVE SERVERS', "SERVEURS ACTIFS"),
    @('Activity & Diagnostics', "Activité et diagnostics"),
    @('Add Destination', "Ajouter une destination"),
    @('Add every other complete template the game needs. Edit Installed location so each path is relative to the installed server folder.', "Ajoutez tous les autres modèles complets nécessaires au jeu. Modifiez l’emplacement installé afin que chaque chemin soit relatif au dossier du serveur installé."),
    @('Add files', "Ajouter des fichiers"),
    @('Added automatically', "Ajouté automatiquement"),
    @('Additional configuration files', "Fichiers de configuration supplémentaires"),
    @('Admin Password', "Mot de passe administrateur"),
    @('Advanced', "Avancé"),
    @('Advanced Discord Destinations', "Destinations Discord avancées"),
    @('Allow launch-file export', "Autoriser l’exportation du fichier de lancement"),
    @('App Port', "Port de l’application"),
    @('Argument Test', "Test des arguments"),
    @('ARGUMENTS', "ARGUMENTS"),
    @('Authentication Token', "Jeton d’authentification"),
    @('Auto Restart', "Redémarrage automatique"),
    @('Automatic evidence comes from Synix actions; arguments require the real-server test.', "Les preuves automatiques proviennent des actions Synix ; les arguments nécessitent un test sur un serveur réel."),
    @('Automatically builds a safe game/server folder below the configured Games path.', "Crée automatiquement un dossier de jeu/serveur sûr sous le chemin Jeux configuré."),
    @('Automation', "Automatisation"),
    @('BACKUP FILE', "FICHIER DE SAUVEGARDE"),
    @('Backup on Start', "Sauvegarder au démarrage"),
    @('Backup Server', "Sauvegarder le serveur"),
    @('Backups', "Sauvegardes"),
    @('Before you continue', "Avant de continuer"),
    @('Blocks launch with clear Microsoft download guidance when the runtime is missing.', "Bloque le lancement avec des instructions claires de téléchargement Microsoft lorsque l’environnement d’exécution manque."),
    @('Blocks setup with a clear message when the processor does not support AVX2.', "Bloque l’installation avec un message clair lorsque le processeur ne prend pas en charge AVX2."),
    @('Browse', "Parcourir"),
    @('Browse Folder', "Parcourir le dossier"),
    @('Browse topics', "Parcourir les rubriques"),
    @('Builder guide and supported tags', "Guide du générateur et balises prises en charge"),
    @('Cancel', "Annuler"),
    @('Catalog order', "Ordre du catalogue"),
    @('CHECK', "VÉRIFIER"),
    @('Check for DDoS', "Détecter les attaques DDoS"),
    @('Check SteamCMD for updates before launching the server.', "Rechercher les mises à jour SteamCMD avant de lancer le serveur."),
    @('Check Synix Values', "Vérifier les valeurs Synix"),
    @('Checking for updates...', "Recherche de mises à jour…"),
    @('Choose a row to unlock server controls', "Choisissez une ligne pour activer les commandes du serveur"),
    @('Choose a server type to show its local verification history.', "Choisissez un type de serveur pour afficher son historique de vérification locale."),
    @('Choose Folder', "Choisir un dossier"),
    @('Choose only the built-in launch behavior verified for this dedicated server.', "Choisissez uniquement le comportement de lancement intégré vérifié pour ce serveur dédié."),
    @('Choose the backup that should replace the server''s current files.', "Choisissez la sauvegarde qui doit remplacer les fichiers actuels du serveur."),
    @('Choose the game and define the server identity.', "Choisissez le jeu et définissez l’identité du serveur."),
    @('Choose the language used by Synix. Game settings and configuration values remain in English.', "Choisissez la langue utilisée par Synix. Les paramètres de jeu et les valeurs de configuration restent en anglais."),
    @('Choose when Synix should perform the scheduled server restart.', "Choisissez quand Synix doit effectuer le redémarrage planifié du serveur."),
    @('CLEAR', "EFFACER"),
    @('Clear Mark', "Effacer la marque"),
    @('Close', "Fermer"),
    @('Compatibility Verification', "Vérification de compatibilité"),
    @('Complete, working configuration template file', "Fichier de modèle de configuration complet et fonctionnel"),
    @('Config Editor', "Éditeur de configuration"),
    @('CONFIG SOURCE', "SOURCE DE CONFIGURATION"),
    @('CONFIGURATION', "CONFIGURATION"),
    @('Configuration & Security', "Configuration et sécurité"),
    @('Configuration Application Check', "Vérification de l’application de la configuration"),
    @('Configuration behavior', "Comportement de la configuration"),
    @('Configuration Editor', "Éditeur de configuration"),
    @('Configuration format', "Format de configuration"),
    @('Configuration path relative to the installed server folder', "Chemin de configuration relatif au dossier du serveur installé"),
    @('CONFIGURATION STATUS', "ÉTAT DE LA CONFIGURATION"),
    @('Configure', "Configurer"),
    @('Configure basic Synix behavior on this computer.', "Configurez le comportement général de Synix sur cet ordinateur."),
    @('Configure Schedule', "Configurer le calendrier"),
    @('Connect GitHub', "Connecter GitHub"),
    @('Connect GitHub account', "Connecter le compte GitHub"),
    @('Continue only after you have reviewed the required setup steps.', "Continuez uniquement après avoir vérifié les étapes d’installation requises."),
    @('Copy allowlisted Steam runtime files after install', "Copier les fichiers d’exécution Steam autorisés après l’installation"),
    @('Copy approved Steam runtime files after installation', "Copier les fichiers d’exécution Steam approuvés après l’installation"),
    @('Copy problem report', "Copier le rapport du problème"),
    @('Copy Report', "Copier le rapport"),
    @('CPU Usage', "Utilisation du processeur"),
    @('Create a protected server backup before each launch.', "Créer une sauvegarde protégée du serveur avant chaque lancement."),
    @('Create a transfer password', "Créer un mot de passe de transfert"),
    @('Create Batch File', "Créer un fichier batch"),
    @('CREATED', "CRÉÉ"),
    @('Crossplay', "Jeu multiplateforme"),
    @('Current version and installation type', "Version actuelle et type d’installation"),
    @('Custom backup location', "Emplacement de sauvegarde personnalisé"),
    @('Dark Mode', "Mode sombre"),
    @('Dark mode toggle', "Bouton du mode sombre"),
    @('DDoS Attack Detection', "Détection des attaques DDoS"),
    @('Default launch arguments', "Arguments de lancement par défaut"),
    @('Default launch arguments (everything after the executable)', "Arguments de lancement par défaut (tout ce qui suit l’exécutable)"),
    @('Default startup arguments', "Arguments de démarrage par défaut"),
    @('Definition ID', "Identifiant de définition"),
    @('Definition revision', "Révision de la définition"),
    @('Delete Backup', "Supprimer la sauvegarde"),
    @('Delete Server', "Supprimer le serveur"),
    @('Describe the problem', "Décrivez le problème"),
    @('DESTINATION', "DESTINATION"),
    @('Destination name', "Nom de la destination"),
    @('DETAILS', "DÉTAILS"),
    @('Development', "Développement"),
    @('Disconnect GitHub', "Déconnecter GitHub"),
    @('Disconnect GitHub account', "Déconnecter le compte GitHub"),
    @('Discord Alerts', "Alertes Discord"),
    @('Discord Destination', "Destination Discord"),
    @('Discord webhook URL', "URL du webhook Discord"),
    @('Discord Webhooks', "Webhooks Discord"),
    @('Do not paste passwords, webhooks, IP addresses, private configuration, or full launch commands. Synix removes common secrets before sending.', "Ne collez pas de mots de passe, webhooks, adresses IP, configurations privées ni commandes de lancement complètes. Synix supprime les secrets courants avant l’envoi."),
    @('Documents source folder for automatic imports (optional)', "Dossier source Documents pour les importations automatiques (facultatif)"),
    @('Edit', "Modifier"),
    @('Edit serverconfig.xml safely without changing its XML structure.', "Modifiez serverconfig.xml en toute sécurité sans changer sa structure XML."),
    @('Edition', "Édition"),
    @('Elevated system tasks', "Tâches système avec élévation"),
    @('Enable only when anonymous SteamCMD installation fails and a Steam account is required.', "Activez uniquement si l’installation anonyme SteamCMD échoue et qu’un compte Steam est requis."),
    @('Enable only when the server cannot run correctly without Windows elevation.', "Activez uniquement si le serveur ne peut pas fonctionner correctement sans élévation Windows."),
    @('Enable RCON', "Activer RCON"),
    @('Enable RCON only for game templates that support secure remote commands.', "Activez RCON uniquement pour les modèles de jeu prenant en charge les commandes distantes sécurisées."),
    @('Enable server query monitoring', "Activer la surveillance des requêtes du serveur"),
    @('Enable when the server has a verified query or network probe that Synix can monitor.', "Activez lorsque le serveur possède une requête ou une sonde réseau vérifiée que Synix peut surveiller."),
    @('Enabled', "Activé"),
    @('Enter the game information, then validate before saving.', "Saisissez les informations du jeu, puis validez avant d’enregistrer."),
    @('Expected result', "Résultat attendu"),
    @('EXPERIMENTAL', "EXPÉRIMENTAL"),
    @('Export', "Exporter"),
    @('Export to Project', "Exporter vers le projet"),
    @('Extra Arguments', "Arguments supplémentaires"),
    @('Find setup guidance, command details, and troubleshooting answers.', "Trouvez des instructions d’installation, des détails sur les commandes et des réponses de dépannage."),
    @('First-launch preparation', "Préparation du premier lancement"),
    @('First-start message shown to the user', "Message de premier démarrage affiché à l’utilisateur"),
    @('Fix Config', "Réparer la configuration"),
    @('Folder', "Dossier"),
    @('Folder Path', "Chemin du dossier"),
    @('FORMAT-AWARE EDITING', "ÉDITION ADAPTÉE AU FORMAT"),
    @('Full Release Notes', "Notes de version complètes"),
    @('GAME', "JEU"),
    @('Game Definition Builder', "Générateur de définitions de jeu"),
    @('Game Definition Test Runner', "Outil de test des définitions de jeu"),
    @('Game icon HTTPS URL (optional)', "URL HTTPS de l’icône du jeu (facultatif)"),
    @('Game Mode', "Mode de jeu"),
    @('Game modes (one exact value per line)', "Modes de jeu (une valeur exacte par ligne)"),
    @('Game name', "Nom du jeu"),
    @('Game Port', "Port du jeu"),
    @('Game Server', "Serveur de jeu"),
    @('Game Servers', "Serveurs de jeu"),
    @('Game Verification Queue', "File de vérification des jeux"),
    @('Game Version', "Version du jeu"),
    @('Gameplay Profile', "Profil de jeu"),
    @('General', "Général"),
    @('Get Token', "Obtenir un jeton"),
    @('GitHub is not connected. Copy and Discord options still work.', "GitHub n’est pas connecté. Les options de copie et Discord fonctionnent toujours."),
    @('GitHub posts directly without opening a browser after the account is connected.', "GitHub publie directement sans ouvrir de navigateur une fois le compte connecté."),
    @('Guide', "Guide"),
    @('Help', "Aide"),
    @('HELP & SUPPORT', "AIDE ET ASSISTANCE"),
    @('Help Center', "Centre d’aide"),
    @('Hide IP addresses, passwords, and other sensitive information while screen sharing.', "Masquez les adresses IP, mots de passe et autres informations sensibles pendant le partage d’écran."),
    @('How can we help?', "Comment pouvons-nous vous aider ?"),
    @('How the user obtains and places required game files', "Comment l’utilisateur obtient et place les fichiers de jeu requis"),
    @('Identity, world, player, and network information', "Informations sur l’identité, le monde, les joueurs et le réseau"),
    @('Individual events', "Événements individuels"),
    @('Insert a supported Synix argument tag', "Insérer une balise d’argument Synix prise en charge"),
    @('Insert tag', "Insérer une balise"),
    @('INSTALL', "INSTALLER"),
    @('Install  — Not verified yet', "Installation  — Pas encore vérifiée"),
    @('Install & Launch', "Installation et lancement"),
    @('Install Location', "Emplacement d’installation"),
    @('Install Update', "Installer la mise à jour"),
    @('Installed location', "Emplacement installé"),
    @('Installed server to test', "Serveur installé à tester"),
    @('Installed Servers', "Serveurs installés"),
    @('INTEGRITY', "INTÉGRITÉ"),
    @('Interface language', "Langue de l’interface"),
    @('Invite Code', "Code d’invitation"),
    @('KNOWLEDGE BASE', "BASE DE CONNAISSANCES"),
    @('KNOWLEDGE BASE READY', "BASE DE CONNAISSANCES PRÊTE"),
    @('LAN IP: Fetching...', "IP du réseau local : récupération…"),
    @('LAST TESTED', "DERNIER TEST"),
    @('LAST VERIFIED', "DERNIÈRE VÉRIFICATION"),
    @('Last-tested Synix version: Not verified yet', "Dernière version Synix testée : pas encore vérifiée"),
    @('Later', "Plus tard"),
    @('Language', "Langue"),
    @('Launch Arguments', "Arguments de lancement"),
    @('Launch behavior', "Comportement au lancement"),
    @('Launch file', "Fichier de lancement"),
    @('Launch preparation', "Préparation du lancement"),
    @('Launch with administrator permission', "Lancer avec les droits d’administrateur"),
    @('Limit the number of backups retained per server.', "Limitez le nombre de sauvegardes conservées par serveur."),
    @('Live performance across every managed game server process.', "Performances en direct de tous les processus de serveur de jeu gérés."),
    @('Live performance and configuration details', "Performances en direct et détails de configuration"),
    @('Loader', "Chargeur"),
    @('Loader Version', "Version du chargeur"),
    @('Loading the built-in game verification queue...', "Chargement de la file de vérification intégrée des jeux…"),
    @('LOCATION', "EMPLACEMENT"),
    @('Long-Duration Reliability Test', "Test de fiabilité longue durée"),
    @('Main World', "Monde principal"),
    @('Maintenance schedule', "Calendrier de maintenance"),
    @('Map', "Carte"),
    @('Map / World', "Carte / Monde"),
    @('Map and mode choices come directly from the selected game template.', "Les choix de carte et de mode proviennent directement du modèle de jeu sélectionné."),
    @('Maps or scenarios (one exact value per line)', "Cartes ou scénarios (une valeur exacte par ligne)"),
    @('Mark Verified', "Marquer comme vérifié"),
    @('Master Discord Webhook', "Webhook Discord principal"),
    @('Max Players', "Nombre maximal de joueurs"),
    @('Max saved backups', "Nombre maximal de sauvegardes conservées"),
    @('Message shown after special readiness checks pass (optional)', "Message affiché après la réussite des contrôles spéciaux (facultatif)"),
    @('MESSAGES SENT', "MESSAGES ENVOYÉS"),
    @('Messages to send', "Messages à envoyer"),
    @('Minecraft Runtime', "Environnement Minecraft"),
    @('Minimum system RAM in GB (0 means no minimum)', "RAM système minimale en Go (0 signifie aucun minimum)"),
    @('minutes', "minutes"),
    @('MONITOR', "SURVEILLER"),
    @('Monitor active server ports for incoming packet floods and notify on abnormal traffic bursts.', "Surveillez les ports actifs des serveurs pour détecter les afflux de paquets entrants et signaler les pics de trafic anormaux."),
    @('Monitor and manage every game server from one workspace.', "Surveillez et gérez tous les serveurs de jeu depuis un seul espace de travail."),
    @('Monitoring  — Not verified yet', "Surveillance  — Pas encore vérifiée"),
    @('Name this destination, paste its Discord webhook, and choose exactly which Synix events it receives.', "Nommez cette destination, collez son webhook Discord et choisissez précisément les événements Synix qu’elle reçoit."),
    @('Network', "Réseau"),
    @('Network & RCON', "Réseau et RCON"),
    @('NEW SERVER', "NOUVEAU SERVEUR"),
    @('No Days Scheduled', "Aucun jour planifié"),
    @('No extra arguments', "Aucun argument supplémentaire"),
    @('No publish folder was detected.', "Aucun dossier de publication n’a été détecté."),
    @('No reliability test has been run yet.', "Aucun test de fiabilité n’a encore été exécuté."),
    @('No running server processes detected', "Aucun processus de serveur en cours détecté"),
    @('Not Required', "Non requis"),
    @('Off', "Désactivé"),
    @('Online Service Authentication', "Authentification au service en ligne"),
    @('Only a masked webhook identifier is shown. Open Server Settings to view or edit the saved destination.', "Seul un identifiant de webhook masqué est affiché. Ouvrez les paramètres du serveur pour voir ou modifier la destination enregistrée."),
    @('Only the value you change is replaced; comments, sections, nesting, quotes, spacing, and key order remain intact.', "Seule la valeur modifiée est remplacée ; les commentaires, sections, niveaux, guillemets, espaces et l’ordre des clés restent intacts."),
    @('Open Backup Folder', "Ouvrir le dossier des sauvegardes"),
    @('Open Config Editor', "Ouvrir l’éditeur de configuration"),
    @('Open Discord', "Ouvrir Discord"),
    @('Open Discord bug forum', "Ouvrir le forum de bogues Discord"),
    @('Open GitHub', "Ouvrir GitHub"),
    @('Open Latest Game Log', "Ouvrir le dernier journal du jeu"),
    @('Open Server Folder', "Ouvrir le dossier du serveur"),
    @('Open SteamCMD', "Ouvrir SteamCMD"),
    @('Open the native console when a game server starts. Disable this to run servers silently in the background.', "Ouvrez la console native au démarrage d’un serveur de jeu. Désactivez cette option pour exécuter les serveurs silencieusement en arrière-plan."),
    @('Open the PayPal donation page on your phone.', "Ouvrez la page de don PayPal sur votre téléphone."),
    @('Open the Synix troubleshooter', "Ouvrir l’outil de dépannage Synix"),
    @('Optional flags only — for example: -log, -nosteamclient, or -forceupdate', "Indicateurs facultatifs uniquement — par exemple : -log, -nosteamclient ou -forceupdate"),
    @('Optional import files (relative paths, one per line)', "Fichiers d’importation facultatifs (chemins relatifs, un par ligne)"),
    @('Optional RCON syntax — launch arguments must contain {rcon}', "Syntaxe RCON facultative — les arguments de lancement doivent contenir {rcon}"),
    @('ORIGINAL', "ORIGINAL"),
    @('Original formatting is protected', "La mise en forme d’origine est protégée"),
    @('Overrides Synix''s hide-console preference for servers managed through their own window.', "Remplace la préférence de masquage de la console de Synix pour les serveurs gérés dans leur propre fenêtre."),
    @('Paths & Launch Details', "Chemins et détails de lancement"),
    @('Players', "Joueurs"),
    @('Port availability is checked automatically against running processes and other Synix servers.', "La disponibilité des ports est automatiquement vérifiée par rapport aux processus en cours et aux autres serveurs Synix."),
    @('Portable Java', "Java portable"),
    @('Preview', "Aperçu"),
    @('Privacy & Security', "Confidentialité et sécurité"),
    @('Privacy mode', "Mode de confidentialité"),
    @('Problem summary', "Résumé du problème"),
    @('Process identity and live resource usage for every active game server.', "Identité des processus et utilisation des ressources en direct pour chaque serveur de jeu actif."),
    @('PROGRESS', "PROGRESSION"),
    @('Protected in Synix and hidden from its logs. Generated batch files include the usable token in readable text.', "Protégé dans Synix et masqué dans ses journaux. Les fichiers batch générés incluent le jeton utilisable en texte lisible."),
    @('Public IP: Fetching...', "IP publique : récupération…"),
    @('Published Synix folder', "Dossier Synix publié"),
    @('Query', "Requête"),
    @('Query Port', "Port de requête"),
    @('Quick event selection', "Sélection rapide des événements"),
    @('RAM USAGE', "UTILISATION DE LA RAM"),
    @('Raw Preview', "Aperçu brut"),
    @('RCON Password', "Mot de passe RCON"),
    @('RCON Port', "Port RCON"),
    @('Read-only values can be selected and copied for diagnostics', "Les valeurs en lecture seule peuvent être sélectionnées et copiées pour le diagnostic"),
    @('Read-only verification of template structure, revision, and values saved in Server Settings. Password values are never displayed.', "Vérification en lecture seule de la structure du modèle, de sa révision et des valeurs enregistrées dans les paramètres du serveur. Les mots de passe ne sont jamais affichés."),
    @('Ready to check the published files and the test receipt created during Publish.', "Prêt à vérifier les fichiers publiés et le reçu de test créé pendant la publication."),
    @('Ready to check this computer.', "Prêt à vérifier cet ordinateur."),
    @('Ready to manage', "Prêt à gérer"),
    @('Ready to test the built-in game-definition library.', "Prêt à tester la bibliothèque intégrée de définitions de jeu."),
    @('Record Verification', "Enregistrer la vérification"),
    @('Refresh', "Actualiser"),
    @('Release highlights', "Points forts de la version"),
    @('Release notes will appear here.', "Les notes de version apparaîtront ici."),
    @('Release Readiness Checker', "Vérificateur de préparation de version"),
    @('Remind Me Later', "Me le rappeler plus tard"),
    @('Remote Administration', "Administration à distance"),
    @('Remove', "Supprimer"),
    @('Remove selected', "Supprimer la sélection"),
    @('Report a Problem', "Signaler un problème"),
    @('Require a visible server manager window', "Exiger une fenêtre visible du gestionnaire de serveur"),
    @('Require an AVX2-capable processor', "Exiger un processeur compatible AVX2"),
    @('Require hardware virtualization', "Exiger la virtualisation matérielle"),
    @('Require Microsoft Hyper-V', "Exiger Microsoft Hyper-V"),
    @('Require the server manager window to remain visible', "Exiger que la fenêtre du gestionnaire de serveur reste visible"),
    @('Require Visual C++ 2013 x64 runtime', "Exiger l’environnement Visual C++ 2013 x64"),
    @('Require Visual C++ 2015-2022 x64 runtime', "Exiger l’environnement Visual C++ 2015-2022 x64"),
    @('Require Windows Professional or higher', "Exiger Windows Professionnel ou une version supérieure"),
    @('Required fields update automatically for the selected game.', "Les champs requis sont automatiquement mis à jour pour le jeu sélectionné."),
    @('Required files and Synix-created templates automatically enable a warning.', "Les fichiers requis et les modèles créés par Synix activent automatiquement un avertissement."),
    @('Resolved automatically', "Résolu automatiquement"),
    @('Resource Monitor', "Moniteur de ressources"),
    @('Restart days', "Jours de redémarrage"),
    @('Restart hour using a 24-hour clock', "Heure de redémarrage au format 24 heures"),
    @('Restart minute', "Minute de redémarrage"),
    @('Restart selected days at a configured time while preserving the current scheduler data.', "Redémarrez les jours sélectionnés à l’heure configurée tout en conservant les données actuelles du planificateur."),
    @('Restart time', "Heure de redémarrage"),
    @('Restore Backup', "Restaurer la sauvegarde"),
    @('Restore Server Backup', "Restaurer la sauvegarde du serveur"),
    @('RESULT', "RÉSULTAT"),
    @('Review how Synix builds the command used to start this server.', "Vérifiez comment Synix construit la commande utilisée pour démarrer ce serveur."),
    @('Review the highlighted requirement', "Vérifiez l’exigence mise en évidence"),
    @('Review these setup requirements before continuing.', "Vérifiez ces exigences d’installation avant de continuer."),
    @('Run All Checks', "Exécuter toutes les vérifications"),
    @('Run Health Check', "Exécuter le contrôle de santé"),
    @('Run Release Check', "Vérifier la version"),
    @('Run Tests', "Exécuter les tests"),
    @('Running Now', "En cours"),
    @('Running Servers', "Serveurs en cours"),
    @('Runtime requirements', "Prérequis d’exécution"),
    @('SAFE ACTION', "ACTION SÛRE"),
    @('Sample every', "Échantillonner toutes les"),
    @('Sanitized arguments (no secrets)', "Arguments nettoyés (aucun secret)"),
    @('Save', "Enregistrer"),
    @('Save Changes', "Enregistrer les modifications"),
    @('Save Destination', "Enregistrer la destination"),
    @('Save Server', "Enregistrer le serveur"),
    @('Save to Project', "Enregistrer dans le projet"),
    @('SCAN TO SUPPORT SYNIX', "SCANNER POUR SOUTENIR SYNIX"),
    @('Scheduled Restarts', "Redémarrages planifiés"),
    @('Search checks titles and article text', "Rechercher dans les titres et le texte des articles"),
    @('Search the full knowledge base or expand a category below.', "Recherchez dans toute la base de connaissances ou développez une catégorie ci-dessous."),
    @('seconds', "secondes"),
    @('Security', "Sécurité"),
    @('See which Discord destination receives each type of Synix notification for this server.', "Consultez la destination Discord qui reçoit chaque type de notification Synix pour ce serveur."),
    @('Select a backup to continue.', "Sélectionnez une sauvegarde pour continuer."),
    @('Select a game server', "Sélectionnez un serveur de jeu"),
    @('Select a game to update its verification evidence.', "Sélectionnez un jeu pour mettre à jour ses preuves de vérification."),
    @('Select a Repair', "Sélectionner une réparation"),
    @('Select an installed server and validate its command.', "Sélectionnez un serveur installé et validez sa commande."),
    @('Selected source file', "Fichier source sélectionné"),
    @('Send status, backups, maintenance, and problems to different Discord channels.', "Envoyez l’état, les sauvegardes, la maintenance et les problèmes vers différents canaux Discord."),
    @('Send Test', "Envoyer un test"),
    @('Send your report', "Envoyer votre rapport"),
    @('Server', "Serveur"),
    @('SERVER / ITEM', "SERVEUR / ÉLÉMENT"),
    @('SERVER CONFIGURATION', "CONFIGURATION DU SERVEUR"),
    @('Server Dashboard', "Tableau de bord des serveurs"),
    @('Server Details', "Détails du serveur"),
    @('Server executable (relative path)', "Exécutable du serveur (chemin relatif)"),
    @('Server Folder', "Dossier du serveur"),
    @('Server Identity', "Identité du serveur"),
    @('Server Info', "Informations du serveur"),
    @('Server lifecycle tracking', "Suivi du cycle de vie du serveur"),
    @('Server log locations (one relative path or wildcard pattern per line)', "Emplacements des journaux du serveur (un chemin relatif ou motif générique par ligne)"),
    @('Server Name', "Nom du serveur"),
    @('Server Options  ▴', "Options du serveur  ▴"),
    @('Server Overview', "Vue d’ensemble du serveur"),
    @('Server Password', "Mot de passe du serveur"),
    @('Server RAM (GB)', "RAM du serveur (Go)"),
    @('Server Setup', "Configuration du serveur"),
    @('SERVER STATUS', "ÉTAT DU SERVEUR"),
    @('Server type', "Type de serveur"),
    @('Servers online', "Serveurs en ligne"),
    @('Service Ports', "Ports de service"),
    @('SETTING', "PARAMÈTRE"),
    @('Settings', "Paramètres"),
    @('Short summary', "Résumé court"),
    @('Show a first-start setup warning', "Afficher un avertissement de configuration au premier démarrage"),
    @('Show server console window', "Afficher la fenêtre de console du serveur"),
    @('Showing 0 games', "0 jeu affiché"),
    @('SIZE', "TAILLE"),
    @('START', "DÉMARRER"),
    @('Start  — Not verified yet', "Démarrage  — Pas encore vérifié"),
    @('Start Server', "Démarrer le serveur"),
    @('Start Test', "Démarrer le test"),
    @('Start Using Synix', "Commencer à utiliser Synix"),
    @('Startup argument template', "Modèle d’arguments de démarrage"),
    @('Startup Tasks', "Tâches de démarrage"),
    @('STATUS', "ÉTAT"),
    @('Steam account login required', "Connexion à un compte Steam requise"),
    @('Steam account name', "Nom du compte Steam"),
    @('Steam account required', "Compte Steam requis"),
    @('Steam AppID', "AppID Steam"),
    @('Steam runtime target directory (relative path)', "Dossier cible de l’environnement Steam (chemin relatif)"),
    @('SteamCMD app configuration (normally blank)', "Configuration de l’application SteamCMD (normalement vide)"),
    @('SteamCMD Download Speed', "Vitesse de téléchargement SteamCMD"),
    @('SteamCMD download speed in megabits per second', "Vitesse de téléchargement SteamCMD en mégabits par seconde"),
    @('SteamCMD download speed mode', "Mode de vitesse de téléchargement SteamCMD"),
    @('STOP', "ARRÊTER"),
    @('Stop  — Not verified yet', "Arrêt  — Pas encore vérifié"),
    @('Stop Server', "Arrêter le serveur"),
    @('Stopped', "Arrêté"),
    @('Store automated and manual server backup archives in a custom folder.', "Stockez les archives de sauvegarde automatiques et manuelles des serveurs dans un dossier personnalisé."),
    @('Structured View', "Vue structurée"),
    @('Submit problem report to GitHub', "Envoyer le rapport du problème à GitHub"),
    @('Submit to GitHub', "Envoyer à GitHub"),
    @('Switch the Synix dashboard between light and dark visual themes.', "Basculez le tableau de bord Synix entre les thèmes visuels clair et sombre."),
    @('Synix Control Panel', "Panneau de contrôle Synix"),
    @('SYNIX CONTROL PANEL  •  version', "PANNEAU DE CONTRÔLE SYNIX  •  version"),
    @('Synix does not open a public web-control port. Passwords stored by Synix are protected locally, and sensitive values are masked from its activity logs.', "Synix n’ouvre aucun port public de contrôle Web. Les mots de passe enregistrés par Synix sont protégés localement et les valeurs sensibles sont masquées dans ses journaux d’activité."),
    @('Synix installs the selected server loader and matching portable Java. Add your own mods after installation.', "Synix installe le chargeur de serveur sélectionné et la version Java portable correspondante. Ajoutez vos propres mods après l’installation."),
    @('Synix is designed to make personal game-server hosting understandable without hiding what it changes on your computer.', "Synix rend l’hébergement personnel de serveurs de jeu compréhensible sans masquer les modifications apportées à votre ordinateur."),
    @('SYNIX KNOWLEDGE BASE', "BASE DE CONNAISSANCES SYNIX"),
    @('Synix Troubleshooter', "Outil de dépannage Synix"),
    @('Synix update is available', "Une mise à jour de Synix est disponible"),
    @('Synix verifies each action automatically after it succeeds on this PC.', "Synix vérifie automatiquement chaque action après sa réussite sur cet ordinateur."),
    @('Synix version:', "Version de Synix :"),
    @('System & Server Troubleshooter', "Dépannage du système et des serveurs"),
    @('Template revision', "Révision du modèle"),
    @('Test duration', "Durée du test"),
    @('Test LAN Connectivity', "Tester la connexion au réseau local"),
    @('Test WAN Connectivity', "Tester la connexion Internet"),
    @('The configuration report will appear here.', "Le rapport de configuration apparaîtra ici."),
    @('The game server process is not running', "Le processus du serveur de jeu n’est pas en cours"),
    @('The readiness report will appear here.', "Le rapport de préparation apparaîtra ici."),
    @('The schedule is saved with this server''s settings.', "Le calendrier est enregistré avec les paramètres de ce serveur."),
    @('The selected server requires a Steam account for installation. Enter the account name that SteamCMD should use.', "Le serveur sélectionné nécessite un compte Steam pour l’installation. Saisissez le nom du compte que SteamCMD doit utiliser."),
    @('The server must remain stopped during restoration', "Le serveur doit rester arrêté pendant la restauration"),
    @('The validation report will appear here.', "Le rapport de validation apparaîtra ici."),
    @('These values are enabled only when the selected server template supports them.', "Ces valeurs sont activées uniquement lorsque le modèle du serveur sélectionné les prend en charge."),
    @('This informational view shows the default arguments Synix uses when building the start command.', "Cette vue informative présente les arguments par défaut utilisés par Synix pour créer la commande de démarrage."),
    @('TOTAL CPU', "PROCESSEUR TOTAL"),
    @('TOTAL RAM', "RAM TOTALE"),
    @('Total system load', "Charge totale du système"),
    @('Total system RAM in use', "RAM système totale utilisée"),
    @('Turn on every day when the scheduled restart should run.', "Activez chaque jour où le redémarrage planifié doit s’exécuter."),
    @('TYPE', "TYPE"),
    @('Undo Edits', "Annuler les modifications"),
    @('Update on Start', "Mettre à jour au démarrage"),
    @('Update safety information appears here.', "Les informations de sécurité de la mise à jour apparaîtront ici."),
    @('Update Server', "Mettre à jour le serveur"),
    @('Uptime', "Durée de fonctionnement"),
    @('Use full speed or limit game-server installs, updates, repairs, and validations.', "Utilisez la vitesse maximale ou limitez les installations, mises à jour, réparations et validations des serveurs de jeu."),
    @('Use one webhook for this server and choose the messages it should receive.', "Utilisez un seul webhook pour ce serveur et choisissez les messages qu’il doit recevoir."),
    @('Use Synix default folder', "Utiliser le dossier Synix par défaut"),
    @('Uses the computer''s local time and a 24-hour clock.', "Utilise l’heure locale de l’ordinateur au format 24 heures."),
    @('Validate & Preview', "Valider et prévisualiser"),
    @('Validate Command', "Valider la commande"),
    @('Validate Game Files', "Valider les fichiers du jeu"),
    @('VALUE', "VALEUR"),
    @('Verification step', "Étape de vérification"),
    @('Verified hardware and Windows requirements checked before Synix installs or launches the server.', "Exigences matérielles et Windows vérifiées avant que Synix installe ou lance le serveur."),
    @('Verified update package details', "Détails vérifiés du paquet de mise à jour"),
    @('Verify Backup', "Vérifier la sauvegarde"),
    @('View Default Arguments', "Afficher les arguments par défaut"),
    @('View Discord Webhooks', "Afficher les webhooks Discord"),
    @('Waiting for a configuration report.', "En attente d’un rapport de configuration."),
    @('Waiting for a running server process', "En attente d’un processus de serveur en cours"),
    @('Waiting for first sample  •  Auto-refresh every 1 second', "En attente du premier échantillon  •  Actualisation automatique chaque seconde"),
    @('WEBHOOK', "WEBHOOK"),
    @('Webhook secrets stay protected', "Les secrets des webhooks restent protégés"),
    @('WELCOME', "BIENVENUE"),
    @('Welcome to Synix', "Bienvenue dans Synix"),
    @('Welcome to the Synix Engine Knowledge Base. Select a topic from the navigation panel to begin.', "Bienvenue dans la base de connaissances du moteur Synix. Sélectionnez une rubrique dans le panneau de navigation pour commencer."),
    @('What happened', "Que s’est-il passé"),
    @('What happened?', "Que s’est-il passé ?"),
    @('What should have happened?', "Qu’aurait-il dû se passer ?"),
    @('What Synix was doing when the problem happened', "Ce que Synix faisait lorsque le problème est survenu"),
    @('What were you doing?', "Que faisiez-vous ?"),
    @('Windows version:', "Version de Windows :"),
    @('Windrose Invite Access', "Accès par invitation Windrose"),
    @('World Generation', "Génération du monde"),
    @('World Seed', "Graine du monde"),
    @('World Size', "Taille du monde"),
    @('XML structure preserved', "Structure XML préservée"),
    @('You will need this password when moving Synix to the new PC. It cannot be recovered.', "Vous aurez besoin de ce mot de passe pour déplacer Synix vers le nouvel ordinateur. Il ne peut pas être récupéré."),
    @('0.00 GB', "0,00 Go"),
    @("1   YOUR DATA STAYS SEPARATE`r`nServers, settings, backups, runtimes, and SteamCMD are stored under C:\Synix so application updates do not replace them.", "1   VOS DONNÉES RESTENT SÉPARÉES`r`nLes serveurs, paramètres, sauvegardes, environnements d’exécution et SteamCMD sont stockés sous C:\Synix afin que les mises à jour de l’application ne les remplacent pas."),
    @("2   ADD A SERVER`r`nChoose a game, enter the friendly settings, and let Synix install it. Steam login is requested only when that game requires it.", "2   AJOUTER UN SERVEUR`r`nChoisissez un jeu, saisissez les paramètres simples et laissez Synix l’installer. La connexion Steam est demandée uniquement lorsque le jeu l’exige."),
    @("3   START, STOP, AND VERIFY`r`nSynix shows the exact launch arguments, verifies startup, uses safe stop behavior where supported, and keeps recent logs available.", "3   DÉMARRER, ARRÊTER ET VÉRIFIER`r`nSynix affiche les arguments de lancement exacts, vérifie le démarrage, utilise un arrêt sûr lorsqu’il est pris en charge et conserve les journaux récents."),
    @("4   NETWORK ACCESS`r`nWindows Firewall permission and router port forwarding are different. Synix checks local conflicts, but never changes your router.", "4   ACCÈS RÉSEAU`r`nL’autorisation du pare-feu Windows et la redirection des ports du routeur sont différentes. Synix vérifie les conflits locaux, mais ne modifie jamais votre routeur."),
    @("5   RECOVERY AND BACKUPS`r`nUse Settings > Advanced > Troubleshooter for safe health checks and repairs. Use Backups before moving Synix or making large changes.", "5   RÉCUPÉRATION ET SAUVEGARDES`r`nUtilisez Paramètres > Avancé > Dépannage pour effectuer des contrôles et réparations sûrs. Créez une sauvegarde avant de déplacer Synix ou d’apporter des modifications importantes."),
    @('AREA', "ZONE"),
    @('Check shared runtimes, server files, configurations, ports, Windows Firewall, disk space, interrupted processes, recent logs, and Synix update health from one place.', "Vérifiez depuis un seul endroit les environnements partagés, les fichiers et configurations des serveurs, les ports, le pare-feu Windows, l’espace disque, les processus interrompus, les journaux récents et l’état des mises à jour Synix."),
    @('Check SteamCMD, runtimes, server files, configs, ports, Windows Firewall, disk space, interrupted processes, recent logs, and update health.', "Vérifiez SteamCMD, les environnements d’exécution, les fichiers et configurations des serveurs, les ports, le pare-feu Windows, l’espace disque, les processus interrompus, les journaux récents et l’état des mises à jour."),
    @('Checks the actual publish output without rebuilding Synix, starting the release, or accessing C:\Synix.', "Vérifie le résultat réel de la publication sans reconstruire Synix, démarrer la version ni accéder à C:\Synix."),
    @('Checks the installed Windows .NET Framework release before the server starts.', "Vérifie la version de .NET Framework installée dans Windows avant le démarrage du serveur."),
    @('Checks whether virtualization support is enabled and available to Windows.', "Vérifie si la virtualisation est activée et disponible pour Windows."),
    @('Covers the unified Microsoft runtime used by current 2015, 2017, 2019, and 2022 servers.', "Couvre l’environnement d’exécution Microsoft unifié utilisé par les serveurs actuels de 2015, 2017, 2019 et 2022."),
    @('Create a validated built-in game definition without plugins or scripts. Definitions are saved into the project and become available only after Synix is rebuilt.', "Créez une définition de jeu intégrée et validée sans module externe ni script. Les définitions sont enregistrées dans le projet et deviennent disponibles uniquement après la reconstruction de Synix."),
    @('Ctrl+F  Search     •     Esc  Close     •     Links open in your browser', "Ctrl+F  Rechercher     •     Échap  Fermer     •     Les liens s’ouvrent dans votre navigateur"),
    @('EXECUTABLE', "EXÉCUTABLE"),
    @('External deployment is for launchers or virtual machines and disables query monitoring.', "Le déploiement externe est destiné aux lanceurs ou aux machines virtuelles et désactive la surveillance des requêtes."),
    @('HOUR', "HEURE"),
    @('I confirmed the displayed server name, ports, player limit, and all other values used by this definition, including passwords, RCON, mode, and map/world where applicable.', "Je confirme le nom du serveur, les ports, la limite de joueurs et toutes les autres valeurs affichées utilisées par cette définition, notamment les mots de passe, RCON, le mode et la carte ou le monde, le cas échéant."),
    @('Install, start, stop, and monitoring checks are recorded automatically. Argument verification uses a real installed server and a sanitized command test; configuration remains a manual file check.', "Les vérifications d’installation, de démarrage, d’arrêt et de surveillance sont enregistrées automatiquement. La vérification des arguments utilise un serveur réellement installé et un test de commande nettoyé ; la configuration reste une vérification manuelle du fichier."),
    @('Lets the user create a reviewed launch file. Disable for deployment commands that must stay inside Synix.', "Permet à l’utilisateur de créer un fichier de lancement vérifié. Désactivez cette option pour les commandes de déploiement qui doivent rester dans Synix."),
    @('MINUTE', "MINUTE"),
    @('My Dedicated Server', "Mon serveur dédié"),
    @('Port', "Port"),
    @('Privacy Mode', "Mode de confidentialité"),
    @('Privacy Mode masks this access credential. Enter a custom code, or leave it empty on first install to let Windrose generate one.', "Le mode de confidentialité masque cet identifiant d’accès. Saisissez un code personnalisé ou laissez le champ vide lors de la première installation pour que Windrose en génère un."),
    @('RAM Usage', "Utilisation de la RAM"),
    @('Ready. A 30-minute run with 30-second samples is recommended for a quick check.', "Prêt. Une exécution de 30 minutes avec des échantillons toutes les 30 secondes est recommandée pour un contrôle rapide."),
    @('Repeatedly samples Synix memory, handles, threads, and the read-only server health checks. It does not start, stop, install, update, or alter a server.', "Échantillonne régulièrement la mémoire, les descripteurs et les threads de Synix ainsi que les contrôles de santé des serveurs en lecture seule. Aucun serveur n’est démarré, arrêté, installé, mis à jour ou modifié."),
    @('Required for features such as Hyper-V that are unavailable on Windows Home.', "Requis pour les fonctions telles que Hyper-V qui ne sont pas disponibles dans Windows Famille."),
    @('Required startup arguments are dynamically injected with your specific data before initialization. You may include any additional command-line flags not covered by the default string in the Extra Arguments section.', "Les arguments de démarrage requis sont injectés dynamiquement avec vos données avant l’initialisation. Vous pouvez ajouter dans la section Arguments supplémentaires tout indicateur de ligne de commande non inclus dans la chaîne par défaut."),
    @('Required user-supplied files (relative paths, one per line)', "Fichiers requis fournis par l’utilisateur (chemins relatifs, un par ligne)"),
    @('SERVER NAME', "NOM DU SERVEUR"),
    @('Show Server Console Window', "Afficher la fenêtre de console du serveur"),
    @('Shown for transparency so you can verify the startup command has no hidden arguments.', "Affiché par souci de transparence afin que vous puissiez vérifier que la commande de démarrage ne contient aucun argument caché."),
    @('Status', "État"),
    @('Synix builds the real command with this server''s saved settings, hides every password, starts it normally, and waits for proof that the server accepted the launch.', "Synix construit la commande réelle à partir des paramètres enregistrés de ce serveur, masque tous les mots de passe, le démarre normalement et attend la preuve que le lancement a été accepté."),
    @('Synix verifies backups with integrity receipts, safely stages the selected archive, and automatically rolls back if restoration fails. The saved Synix server entry and its settings are not changed.', "Synix vérifie les sauvegardes avec des reçus d’intégrité, prépare l’archive sélectionnée en toute sécurité et restaure automatiquement l’état précédent en cas d’échec. L’entrée du serveur Synix et ses paramètres enregistrés ne sont pas modifiés."),
    @('Tests every built-in game, managed setting binding, full configuration template, revision, path, log location, and allowlisted post-install action. Installed servers are never changed.', "Teste chaque jeu intégré, la liaison des paramètres gérés, les modèles de configuration complets, les révisions, les chemins, les emplacements des journaux et les actions autorisées après l’installation. Les serveurs installés ne sont jamais modifiés."),
    @('Use only when testing proves the server needs the approved Steam DLL files. The target must stay inside the server folder.', "Utilisez uniquement lorsque les tests prouvent que le serveur nécessite les fichiers DLL Steam approuvés. La cible doit rester dans le dossier du serveur."),
    @('Use only when the server is deployed through Hyper-V or Windows containers.', "Utilisez uniquement lorsque le serveur est déployé avec Hyper-V ou des conteneurs Windows."),
    @('When enabled, deleting a server requests administrator permission to remove its Windows Firewall rules. Turn this off to skip automatic cleanup during deletion.', "Lorsque cette option est activée, la suppression d’un serveur demande les droits d’administrateur pour retirer ses règles du pare-feu Windows. Désactivez-la pour ignorer le nettoyage automatique lors de la suppression."),
    @('●  Initializing SteamCMD...', "●  Initialisation de SteamCMD…"),
    @('●  SteamCMD needs attention', "●  SteamCMD nécessite votre attention"),
    @('A server operation is currently in progress', "Une opération de serveur est en cours"),
    @('Active Processes', "Processus actifs"),
    @('Additional files are required', "Des fichiers supplémentaires sont requis"),
    @('Address could not be loaded', "L’adresse n’a pas pu être chargée"),
    @('Agreement required', "Accord requis"),
    @('Automatic safety checklist and next steps', "Liste de contrôle de sécurité automatique et prochaines étapes"),
    @('Cancel Check', "Annuler la vérification"),
    @('Canceling the release check safely...', "Annulation sécurisée de la vérification de la version…"),
    @('Check Again', "Vérifier à nouveau"),
    @('Checking release files...', "Vérification des fichiers de la version…"),
    @('Choose Package', "Choisir un paquet"),
    @('Complete the required file setup before the dedicated server can start.', "Terminez la configuration des fichiers requis avant de démarrer le serveur dédié."),
    @('Config unavailable', "Configuration indisponible"),
    @('Configuration repair is available', "Une réparation de la configuration est disponible"),
    @('Configuration report copied to the clipboard.', "Rapport de configuration copié dans le presse-papiers."),
    @('CPU USAGE', "UTILISATION DU PROCESSEUR"),
    @('Current state reported by the Synix engine', "État actuel signalé par le moteur Synix"),
    @('Decline', "Refuser"),
    @('Definition test report copied to the clipboard.', "Rapport de test de la définition copié dans le presse-papiers."),
    @('Discord opened. Select New Post in the bug-reporting forum and paste the copied report.', "Discord est ouvert. Sélectionnez Nouvelle publication dans le forum de signalement des bogues et collez le rapport copié."),
    @('Elevated System Tasks', "Tâches système avec élévation"),
    @('Enter a valid Steam account name.', "Saisissez un nom de compte Steam valide."),
    @('Every launcher, console host, and game process Synix has verified inside this server group.', "Chaque lanceur, hôte de console et processus de jeu que Synix a vérifié dans ce groupe de serveurs."),
    @('First-Start Assistant', "Assistant de premier démarrage"),
    @('Framework', "Infrastructure"),
    @('I Agree', "J’accepte"),
    @('I Understand', "J’ai compris"),
    @("IMPORT  No package selected`nChoose a package to calculate space and time.", "IMPORTATION  Aucun paquet sélectionné`nChoisissez un paquet pour calculer l’espace et le temps nécessaires."),
    @('Import Synix', "Importer Synix"),
    @('Install this game in Synix before testing its real launch arguments.', "Installez ce jeu dans Synix avant de tester ses véritables arguments de lancement."),
    @('Installation canceled. No files were changed.', "Installation annulée. Aucun fichier n’a été modifié."),
    @('LAN IP: [HIDDEN]', "IP du réseau local : [MASQUÉE]"),
    @('Loading player details…', "Chargement des détails des joueurs…"),
    @('NO ADD-ON PROFILE YET', "AUCUN PROFIL D’EXTENSION POUR LE MOMENT"),
    @('No files were changed.', "Aucun fichier n’a été modifié."),
    @('No matching help articles', "Aucun article d’aide correspondant"),
    @('NO RESULTS', "AUCUN RÉSULTAT"),
    @('Open PayPal Donation', "Ouvrir la page de don PayPal"),
    @('Public IP: [HIDDEN]', "IP publique : [MASQUÉE]"),
    @('Publish folder selected. Run the check when ready.', "Dossier de publication sélectionné. Lancez la vérification lorsque vous êtes prêt."),
    @('RAM USAGE', "UTILISATION DE LA RAM"),
    @('Reading and testing the project game-definition library...', "Lecture et test de la bibliothèque de définitions de jeu du projet…"),
    @('Ready for the first start', "Prêt pour le premier démarrage"),
    @('Release check canceled.', "Vérification de la version annulée."),
    @('Release report copied to the clipboard.', "Rapport de version copié dans le presse-papiers."),
    @('Reliability test cancelled. No server settings were changed.', "Test de fiabilité annulé. Aucun paramètre de serveur n’a été modifié."),
    @('Repair available', "Réparation disponible"),
    @('Repairing SteamCMD...', "Réparation de SteamCMD…"),
    @('Resolving...', "Résolution…"),
    @('Resource sampling was delayed  •  Retrying automatically', "L’échantillonnage des ressources a été retardé  •  Nouvelle tentative automatique"),
    @('Review the license terms before allowing the first server launch.', "Consultez les conditions de licence avant d’autoriser le premier lancement du serveur."),
    @('Running package structure, SHA-256, and antivirus checks…', "Vérification de la structure du paquet, du SHA-256 et de l’antivirus…"),
    @('SEARCH', "RECHERCHER"),
    @('Security review blocked the package. No files were changed.', "Le contrôle de sécurité a bloqué le paquet. Aucun fichier n’a été modifié."),
    @('Select a valid Minecraft player first.', "Sélectionnez d’abord un joueur Minecraft valide."),
    @('Select an Action', "Sélectionnez une action"),
    @('Sending a safe test message...', "Envoi d’un message de test sécurisé…"),
    @('Server Framework', "Infrastructure du serveur"),
    @('Server list changed during sampling  •  Retrying automatically', "La liste des serveurs a changé pendant l’échantillonnage  •  Nouvelle tentative automatique"),
    @('Server Readiness Center', "Centre de préparation du serveur"),
    @('Starting the server through Synix. Waiting for its configured listener to respond...', "Démarrage du serveur avec Synix. En attente de la réponse de son écouteur configuré…"),
    @('Stopping the test server through Synix...', "Arrêt du serveur de test avec Synix…"),
    @('Synix could not refresh the add-on folders.', "Synix n’a pas pu actualiser les dossiers d’extensions."),
    @('Synix could not verify this loader combination from the official metadata service.', "Synix n’a pas pu vérifier cette combinaison de chargeurs auprès du service officiel de métadonnées."),
    @('Synix installs Microsoft''s official Bedrock Dedicated Server. Java and Java mod loaders do not apply.', "Synix installe le serveur dédié Bedrock officiel de Microsoft. Java et les chargeurs de mods Java ne s’appliquent pas."),
    @('Synix installs the official Oxide runtime only. Plugins remain user-managed in the server''s oxide\plugins folder.', "Synix installe uniquement l’environnement d’exécution officiel Oxide. Les modules restent gérés par l’utilisateur dans le dossier oxide\plugins du serveur."),
    @('Synix will not guess where this game stores mods. A small data profile can add support later without rewriting this window.', "Synix ne devine pas où ce jeu stocke les mods. Un petit profil de données pourra ajouter la prise en charge ultérieurement sans réécrire cette fenêtre."),
    @('Testing every built-in definition and template safely...', "Test sécurisé de toutes les définitions et de tous les modèles intégrés…"),
    @('The add-on was not installed.', "L’extension n’a pas été installée."),
    @('The game server process is online', "Le processus du serveur de jeu est en ligne"),
    @('The game-definition tests could not finish.', "Les tests des définitions de jeu n’ont pas pu se terminer."),
    @('The local connection was removed. Revoke Synix on the GitHub page that opened.', "La connexion locale a été supprimée. Révoquez Synix sur la page GitHub qui s’est ouverte."),
    @('The passwords do not match.', "Les mots de passe ne correspondent pas."),
    @('The privacy-filtered report was copied and is ready to paste into the Discord bug forum.', "Le rapport filtré pour préserver la confidentialité a été copié et peut être collé dans le forum de bogues Discord."),
    @('The release check could not finish.', "La vérification de la version n’a pas pu se terminer."),
    @('The release check was canceled. No release files were changed.', "La vérification de la version a été annulée. Aucun fichier de version n’a été modifié."),
    @('The reliability report will appear after the requested run finishes.', "Le rapport de fiabilité apparaîtra à la fin de l’exécution demandée."),
    @('The server stopped before startup could be verified. Review its recent logs and definition.', "Le serveur s’est arrêté avant que son démarrage puisse être vérifié. Consultez ses journaux récents et sa définition."),
    @('The test server is stopped. The completed argument evidence is preserved.', "Le serveur de test est arrêté. Les preuves d’arguments terminées sont conservées."),
    @('Unavailable', "Indisponible"),
    @('Update did not start  •  Current Synix was not changed', "La mise à jour n’a pas démarré  •  La version actuelle de Synix n’a pas été modifiée"),
    @('Use at least 8 characters.', "Utilisez au moins 8 caractères."),
    @('Validated definition preview', "Aperçu de la définition validée"),
    @('Verification queue refreshed from the saved Synix evidence.', "File de vérification actualisée à partir des preuves Synix enregistrées."),
    @('Verifying archive paths and SHA-256 integrity...', "Vérification des chemins de l’archive et de l’intégrité SHA-256…"),
    @('VIEWING HELP ARTICLE', "AFFICHAGE DE L’ARTICLE D’AIDE"),
    @('Account name, not your Steam display name', "Nom du compte, et non votre nom d’affichage Steam"),
    @('Add a Server', "Ajouter un serveur"),
    @('Automatically collect generated game configurations', "Collecter automatiquement les configurations de jeu générées"),
    @('CONNECTED', "CONNECTÉ"),
    @('Check Release', "Vérifier la version"),
    @('Check release readiness', "Vérifier si la version est prête"),
    @('Clean Orphaned Rules', "Nettoyer les règles orphelines"),
    @('Clean orphaned Synix server firewall rules', "Nettoyer les règles de pare-feu orphelines des serveurs Synix"),
    @('Clear Filters', "Effacer les filtres"),
    @('Collect Now', "Collecter maintenant"),
    @('Collect generated game configurations now', "Collecter maintenant les configurations de jeu générées"),
    @('Commands stay on this computer unless you intentionally configure Java RCON for remote access.', "Les commandes restent sur cet ordinateur, sauf si vous configurez volontairement Java RCON pour un accès distant."),
    @('Configuration file', "Fichier de configuration"),
    @('Confirm password', "Confirmer le mot de passe"),
    @('Confirm removal of the listed firewall rules', "Confirmer la suppression des règles de pare-feu répertoriées"),
    @('Connection Information', "Informations de connexion"),
    @('Copy Address', "Copier l’adresse"),
    @('Copy Details', "Copier les détails"),
    @('Definition Builder', "Créateur de définitions"),
    @('Default Launch Arguments', "Arguments de lancement par défaut"),
    @('Disabled — scheduled work runs only while Synix is open.', "Désactivé — les tâches planifiées s’exécutent uniquement lorsque Synix est ouvert."),
    @('EVENTS', "ÉVÉNEMENTS"),
    @('Each rule points to an executable under C:\Synix\Games\[Game]\[Server], but that individual server folder is gone and no installed Synix server owns the path.', "Chaque règle pointe vers un exécutable sous C:\Synix\Games\[Game]\[Server], mais le dossier de ce serveur n’existe plus et aucun serveur Synix installé ne possède ce chemin."),
    @('Encrypted Export', "Exportation chiffrée"),
    @('Enter IDs in the order they should load. Use commas, spaces, or one ID per line. Synix does not need a database of mod names.', "Saisissez les identifiants dans leur ordre de chargement. Utilisez des virgules, des espaces ou un identifiant par ligne. Synix n’a pas besoin d’une base de données de noms de mods."),
    @('Example: Server closes a few seconds after Start', "Exemple : le serveur se ferme quelques secondes après son démarrage"),
    @('Finds rules whose executable was under C:\Synix\Games\[Game]\[Server], but that specific server is no longer saved and its server folder is gone. Ports and custom install folders are not scanned.', "Recherche les règles dont l’exécutable se trouvait sous C:\Synix\Games\[Game]\[Server], alors que ce serveur n’est plus enregistré et que son dossier a disparu. Les ports et les dossiers d’installation personnalisés ne sont pas analysés."),
    @('Firewall Cleanup Review', "Vérification du nettoyage du pare-feu"),
    @('Firewall executable rules ready for removal', "Règles d’exécutables du pare-feu prêtes à être supprimées"),
    @('Game Support Catalog', "Catalogue de prise en charge des jeux"),
    @('Game', "Jeu"),
    @('Game port', "Port de jeu"),
    @('Getting Started with Synix', "Bien démarrer avec Synix"),
    @('How would you like to add a server?', "Comment souhaitez-vous ajouter un serveur ?"),
    @('Important: Write the summary and report details in English so the Synix support team can review them.', "Important : rédigez le résumé et les détails du rapport en anglais afin que l’équipe d’assistance Synix puisse les examiner."),
    @('Import Existing Server', "Importer un serveur existant"),
    @('Loading...', "Chargement…"),
    @('Launch Preparation', "Préparation du lancement"),
    @("Logs\*.log`r`nSaved\Logs\**\*.log", "Logs\*.log`r`nSaved\Logs\**\*.log"),
    @('Manage Provider Mod IDs', "Gérer les identifiants de mods du fournisseur"),
    @('Maintenance Schedule', "Calendrier de maintenance"),
    @('Minecraft Server Console', "Console du serveur Minecraft"),
    @('Mod & Plugin Manager', "Gestionnaire de mods et de modules"),
    @('Not changed: game files, saved servers, port-only rules, custom install folders, and firewall rules outside C:\Synix\Games.', "Non modifiés : fichiers de jeu, serveurs enregistrés, règles portant uniquement sur des ports, dossiers d’installation personnalisés et règles de pare-feu hors de C:\Synix\Games."),
    @('Open the game definition builder', "Ouvrir le créateur de définitions de jeu"),
    @('Open the game verification queue', "Ouvrir la file de vérification des jeux"),
    @('Open the long-duration reliability test', "Ouvrir le test de fiabilité de longue durée"),
    @('Optional', "Facultatif"),
    @('Orphaned Firewall Rule Cleanup', "Nettoyage des règles de pare-feu orphelines"),
    @('PLAYER', "JOUEUR"),
    @('Password (at least 8 characters)', "Mot de passe (au moins 8 caractères)"),
    @('Player Management Center', "Centre de gestion des joueurs"),
    @('Protect Synix Transfer', "Protéger le transfert Synix"),
    @('Quick Commands — choose one to prepare it, then review and send it', "Commandes rapides — choisissez-en une à préparer, puis vérifiez-la et envoyez-la"),
    @('Query port', "Port de requête"),
    @('Ready — Windows requests administrator permission only if rules need removal.', "Prêt — Windows demande l’autorisation d’administrateur uniquement si des règles doivent être supprimées."),
    @('Refresh Players', "Actualiser les joueurs"),
    @('Refresh to load player details directly from the local server.', "Actualisez pour charger les détails des joueurs directement depuis le serveur local."),
    @('Register Server', "Enregistrer le serveur"),
    @('Reliability Test', "Test de fiabilité"),
    @('Remove Rules', "Supprimer les règles"),
    @('Review orphaned firewall rules', "Vérifier les règles de pare-feu orphelines"),
    @('SCORE', "SCORE"),
    @('Save Ordered IDs', "Enregistrer les identifiants ordonnés"),
    @('Search by game, executable, or support status…', "Rechercher par jeu, exécutable ou état de prise en charge…"),
    @('Search game name...', "Rechercher un nom de jeu…"),
    @('Search game or server name...', "Rechercher un jeu ou un serveur…"),
    @('Search settings, paths, or values...', "Rechercher des paramètres, chemins ou valeurs…"),
    @('See exactly what Synix can install, configure, monitor, and query before creating a server.', "Consultez précisément ce que Synix peut installer, configurer, surveiller et interroger avant de créer un serveur."),
    @('Send Command', "Envoyer la commande"),
    @('Setup completion: 0%', "Progression de la configuration : 0 %"),
    @('Show Technical Details', "Afficher les détails techniques"),
    @('Simple view', "Vue simplifiée"),
    @('Starts background monitoring when you sign in to Windows. Closing the Synix dashboard always exits Synix completely for the current session.', "Démarre la surveillance en arrière-plan lors de votre connexion à Windows. La fermeture du tableau de bord Synix quitte toujours complètement Synix pour la session actuelle."),
    @('Steam Account Required', "Compte Steam requis"),
    @('Synix Argument Test', "Test des arguments Synix"),
    @('Synix Background Service', "Service d’arrière-plan Synix"),
    @('Synix Configuration Application Check', "Vérification de l’application de la configuration Synix"),
    @('Synix Game Definition Builder', "Créateur de définitions de jeu Synix"),
    @('Synix Game Definition Test Runner', "Outil de test des définitions de jeu Synix"),
    @('Synix Game Verification Queue', "File de vérification des jeux Synix"),
    @('Synix Help Center', "Centre d’aide Synix"),
    @('Synix Release Readiness Checker', "Vérificateur de préparation des versions Synix"),
    @('Synix Reliability Test', "Test de fiabilité Synix"),
    @('Synix Settings', "Paramètres Synix"),
    @('Synix Update', "Mise à jour de Synix"),
    @('Synix background service', "Service d’arrière-plan Synix"),
    @('Synix can install a new server or safely register files that are already on this PC.', "Synix peut installer un nouveau serveur ou enregistrer en toute sécurité des fichiers déjà présents sur cet ordinateur."),
    @('Synix logo', "Logo Synix"),
    @('Tell us what you clicked, what Synix displayed, and how to make it happen again.', "Indiquez ce sur quoi vous avez cliqué, ce que Synix a affiché et comment reproduire le problème."),
    @('Test built-in game definitions', "Tester les définitions de jeu intégrées"),
    @('Use premade game configurations', "Utiliser les configurations de jeu prédéfinies"),
    @('Use the address that matches where the player is connecting from.', "Utilisez l’adresse correspondant à l’emplacement depuis lequel le joueur se connecte."),
    @('View Details', "Afficher les détails"),
    @('WHAT HAPPENS AFTER YOU CONTINUE', "CE QUI SE PASSE APRÈS AVOIR CONTINUÉ"),
    @('WHY SYNIX FLAGGED THESE RULES', "POURQUOI SYNIX A SIGNALÉ CES RÈGLES"),
    @('What Synix currently knows how to install, configure, start, monitor, and query for this game.', "Ce que Synix sait actuellement installer, configurer, démarrer, surveiller et interroger pour ce jeu."),
    @('Window title', "Titre de la fenêtre"),
    @('Windows requests administrator permission. Synix then removes only firewall rules matching the exact executable paths above and scans again to verify the cleanup.', "Windows demande l’autorisation d’administrateur. Synix supprime ensuite uniquement les règles de pare-feu correspondant exactement aux chemins d’exécutables ci-dessus, puis effectue une nouvelle analyse pour vérifier le nettoyage.")
)

$translations = [System.Collections.Generic.Dictionary[string,string]]::new([System.StringComparer]::Ordinal)
foreach ($pair in $pairs) {
    $translations[$pair[0]] = $pair[1]
}
$operationalTranslations = & (Join-Path $PSScriptRoot 'OperationalTranslations.fr.ps1')

$sourcePath = Join-Path $PSScriptRoot 'Strings.resx'
$targetPath = Join-Path $PSScriptRoot 'Strings.fr.resx'
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
        $french = $null
		if ($semanticTranslations.Contains($key)) {
			continue
		}
		elseif ($operationalTranslations.Contains($key)) {
            $writer.AddResource($key, [string]$operationalTranslations[$key])
            $translatedStaticCount++
        }
		elseif ($translations.TryGetValue($english, [ref]$french)) {
            $writer.AddResource($key, $french)
            $translatedStaticCount++
        }
    }
}
finally {
    $reader.Close()
    $writer.Close()
}

Write-Host "Created Strings.fr.resx with $translatedStaticCount translated static texts and $($semanticTranslations.Count) semantic texts."
