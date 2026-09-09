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
    'Language.English' = "Inglés"
    'Language.French' = "Francés"
    'Language.German' = "Alemán"
    'Language.Spanish' = "Español"
    'Option.DownloadSpeed.Unlimited' = "Ilimitada"
    'Option.DownloadSpeed.Limited' = "Limitada"
    'Message.AlreadyRunning.Body' = "Synix ya está en ejecución. Usa la ventana de Synix que ya está abierta."
    'Message.AlreadyRunning.Title' = "Synix ya está en ejecución"
    'Settings.VersionLabel' = "PANEL DE CONTROL SYNIX  •  v{0}"
    'SettingsPage.General.Heading' = "General"
    'SettingsPage.General.Subtitle' = "Configura el comportamiento básico de Synix en este equipo."
    'SettingsPage.Backups.Heading' = "Copias de seguridad"
    'SettingsPage.Backups.Subtitle' = "Administra las copias de seguridad o mueve Synix a otro equipo."
    'SettingsPage.Privacy.Heading' = "Privacidad y seguridad"
    'SettingsPage.Privacy.Subtitle' = "Controla cómo se muestra la información confidencial del servidor."
    'SettingsPage.Advanced.Heading' = "Avanzado"
    'SettingsPage.Advanced.Subtitle' = "Configura las operaciones elevadas y el comportamiento avanzado del sistema."
    'SettingsPage.ReportProblem.Heading' = "Informar de un problema"
    'SettingsPage.ReportProblem.Subtitle' = "Crea un informe de compatibilidad filtrado para proteger la privacidad."
    'SettingsPage.Development.Heading' = "Desarrollo"
    'SettingsPage.Development.Subtitle' = "Administra la captura de configuraciones y las herramientas de prueba de versiones."
    'Menu.ModPluginManager' = "Gestor de mods y complementos"
    'Menu.PlayerManagementCenter' = "Centro de gestión de jugadores"
    'Menu.MinecraftServerConsole' = "Consola del servidor Minecraft"
    'Menu.ConnectionInformation' = "Información de conexión"
    'Menu.LiveProcessDetails' = "Detalles de procesos en directo"
    'Option.Status.All' = "Todos los estados"
    'Option.Status.Running' = "En ejecución"
    'Option.Status.Stopped' = "Detenido"
    'Option.Status.InProgress' = "En curso"
    'Option.Status.NeedsAttention' = "Requiere atención"
    'Option.Discord.AllEvents' = "Todos los eventos"
    'Option.Discord.ServerStatus' = "Estado del servidor"
    'Option.Discord.Maintenance' = "Mantenimiento"
    'Option.Discord.ProblemsOnly' = "Solo problemas"
    'Option.Discord.Custom' = "Personalizado"
    'Option.ConfigType.All' = "Todos los tipos"
    'Option.ConfigType.Text' = "TEXTO"
    'Option.ConfigType.Number' = "NÚMERO"
    'Option.ConfigType.Boolean' = "BOOLEANO"
    'Option.ConfigType.Secret' = "SECRETO"
    'Option.ConfigType.Null' = "NULO"
    'Option.VerificationFilter.NeedsWork' = "Necesita trabajo"
    'Option.VerificationFilter.UnknownConfiguration' = "Configuración desconocida"
    'Option.VerificationFilter.PartiallyVerified' = "Verificado parcialmente"
    'Option.VerificationFilter.FullyVerified' = "Verificado por completo"
    'Option.VerificationFilter.AllGames' = "Todos los juegos"
    'VerificationStep.Install' = "Instalación"
    'VerificationStep.Start' = "Inicio"
    'VerificationStep.Stop' = "Detención"
    'VerificationStep.Monitoring' = "Supervisión"
    'VerificationStep.Arguments' = "Argumentos"
    'VerificationStep.Configuration' = "Configuración"
    'Status.Stopped' = "Detenido"
    'Status.Running' = "En ejecución"
    'Status.Starting' = "Iniciando"
    'Status.Crashed' = "Bloqueado"
    'Status.Stopping' = "Deteniendo"
    'Status.Installing' = "Instalando"
    'Status.Updating' = "Actualizando"
    'Status.BackingUp' = "Creando copia"
    'Status.Validating' = "Validando"
    'Status.Exporting' = "Exportando"
    'Status.Restoring' = "Restaurando"
    'Status.Deleting' = "Eliminando"
    'Status.Unknown' = "Desconocido"
    'Dashboard.ServerCount.One' = "{0} servidor"
    'Dashboard.ServerCount.Many' = "{0} servidores"
    'Dashboard.ServerCount.Filtered' = "{0} de {1} servidores"
    'Dashboard.Network.PublicFetching' = "IP pública: obteniendo…"
    'Dashboard.Network.LocalFetching' = "IP de red local: obteniendo…"
    'Dashboard.Network.PublicAddress' = "IP pública: {0}"
    'Dashboard.Network.LocalAddress' = "IP de red local: {0}"
    'Dashboard.Network.PublicHidden' = "IP pública: [OCULTA]"
    'Dashboard.Network.LocalHidden' = "IP de red local: [OCULTA]"
    'Dashboard.CpuGaugeLabel' = "CPU %"
    'Dashboard.RamGaugeLabel' = "RAM GB"
    'Dashboard.CpuValue' = "{0:0.0} %"
    'Dashboard.RamValue' = "{0:0.00} GB"
    'ServerSetup.Status.Ready' = "●  Listo para guardar"
    'ServerSetup.Status.ActionRequired' = "●  Acción requerida"
    'ServerSetup.Status.AllChecksPassed' = "Se aprobaron todas las comprobaciones obligatorias"
    'ServerSetup.Status.SeeValidationMessage' = "Consulta el mensaje de validación de abajo"
    'ServerSetup.Completion' = "Configuración: {0} %"
    'ServerSetup.Window.EditTitle' = "Editar servidor"
    'ServerSetup.ModeBadge.Edit' = "EDITAR SERVIDOR"
    'ServerSetup.Page.Security.Description' = "Administre las contraseñas del servidor y las credenciales de los servicios en línea."
    'ServerSetup.Page.World.Description' = "Configure la semilla, el tamaño y las opciones del mundo específicas del juego."
    'ServerSetup.Page.Network.Description' = "Asigne puertos de servicio y proteja la administración remota."
    'ServerSetup.Page.Network.BeginnerDescription' = "Use los puertos de juego y consulta recomendados. El modo avanzado añade controles RCON."
    'ServerSetup.Page.Automation.Description' = "Controle las tareas de inicio, los reinicios programados, las copias de seguridad y las alertas."
    'ServerSetup.Page.Discord.Title' = "Notificaciones de Discord"
    'ServerSetup.Page.Discord.Description' = "Use un webhook principal o dirija distintos eventos de Synix a varios canales de Discord."
    'ServerSetup.Page.Install.Description' = "Elija el almacenamiento del servidor y personalice los argumentos de inicio."
    'ServerSetup.Page.Install.BeginnerDescription' = "Elija dónde se instalará el servidor. Synix proporciona la configuración de inicio recomendada."
    'ServerSetup.Mode.Advanced' = "Modo: Avanzado"
    'ServerSetup.Mode.Beginner' = "Modo: Principiante"
    'ServerSetup.Mode.Advanced.AccessibleName' = "Modo avanzado de configuración del servidor. Haga clic para usar el modo principiante."
    'ServerSetup.Mode.Beginner.AccessibleName' = "Modo principiante de configuración del servidor. Haga clic para mostrar la configuración avanzada."
    'ServerSetup.Navigation.AttentionRequired' = "{0} contiene ajustes que requieren atención antes de guardar."
    'ServerSetup.Navigation.NoAttentionRequired' = "{0} no contiene ajustes que requieran atención."
    'ServerSetup.Validation.Waiting' = "La validación está esperando la información obligatoria del servidor."
    'ServerSetup.Validation.ServerNameAndGameRequired' = "  🔒 [OBLIGATORIO] Ingrese un nombre de servidor y seleccione una plantilla de juego."
    'ServerSetup.Validation.ServerNameRequired' = "  🔒 [OBLIGATORIO] Ingrese un nombre antes de guardar este servidor."
    'ServerSetup.Validation.GameRequired' = "  🔒 [OBLIGATORIO] Seleccione una plantilla de juego antes de guardar este servidor."
    'ServerSetup.Validation.MinecraftLoading' = "  ◌ [MINECRAFT] Cargando versiones compatibles y requisitos de Java…"
    'ServerSetup.Validation.MinecraftDetail' = "  ⚠️ [MINECRAFT] {0}"
    'ServerSetup.Validation.MinecraftVersionRequired' = "  🔒 [MINECRAFT] Seleccione una versión del juego Minecraft."
    'ServerSetup.Validation.AdminPasswordRequired' = "  🔒 [OBLIGATORIO] Ingrese una contraseña de administrador para proteger la función de administrador del servidor."
    'ServerSetup.Validation.AuthenticationTokenRequired' = "  🔒 [OBLIGATORIO] Ingrese el campo obligatorio {0} antes de guardar este servidor."
    'ServerSetup.Validation.RequiredDetail' = "  🔒 [OBLIGATORIO] {0}"
    'ServerSetup.Validation.MinecraftLoaderRequired' = "  🔒 [MINECRAFT] No hay una compilación compatible del cargador seleccionada."
    'ServerSetup.Validation.RequirementDetail' = "  ⚠️ [REQUISITO] {0}"
    'ServerSetup.Validation.NameConflict' = "  ⚠️ [CONFLICTO] El nombre «{0}» ya se usa para {1}."
    'ServerSetup.Validation.ScheduleDayRequired' = "  🔒 [OBLIGATORIO] Seleccione al menos un día para la programación de reinicio automático."
    'ServerSetup.Validation.InstallFolderRequired' = "  🔒 [OBLIGATORIO] Seleccione una carpeta de instalación o active la ruta predeterminada."
    'ServerSetup.Validation.LaunchDetail' = "  ⚠️ [INICIO] {0}"
    'ServerSetup.Validation.DiscordDetail' = "  🔒 [DISCORD] {0}"
    'ServerSetup.Validation.ReadyNote' = "  ✔ [LISTO] NOTA: {0}"
    'ServerSetup.Validation.Updating' = "  ✔ [LISTO] Actualizando: {0}"
    'ServerSetup.Validation.Ready' = "  ✔ [LISTO] La configuración es válida y segura."
    'ServerSetup.Validation.Error' = "  ⚠️ [ERROR DE VALIDACIÓN] No se pudo completar la validación: {0}"
    'ServerSetup.Validation.DuplicatePort' = "  ⚠️ [CONFLICTO] {0} no pueden usar el mismo puerto {1}."
    'ServerSetup.Validation.PortBlocked' = "  ⚠️ [CONFLICTO] {0} {1} está bloqueado por: {2}"
    'ServerSetup.ConfigurationSupport' = "◇  COMPATIBILIDAD DE CONFIGURACIÓN: {0}  •  {1}"
    'ServerSetup.PortMapping.SelectGame' = "Seleccione un juego para ver sus asignaciones de puertos administradas."
    'ServerSetup.PortMapping.AllMapped' = "Todos los puertos declarados están asignados mediante argumentos o configuración."
    'ServerSetup.PortMapping.NeedsMapping' = "Asignación necesaria: {0} (argumentos o plantilla de configuración)."
    'ServerSetup.Port.Ipv6' = "Puerto IPv6"
    'ServerSetup.Port.SystemProcess' = "Proceso del sistema"
    'ServerSetup.List.AndSeparator' = " y "
    'ServerSetup.Credentials.UnlockFailed.Title' = "Volver a ingresar las credenciales del servidor"
    'ServerSetup.Credentials.UnlockFailed.Body' = "Synix no pudo desbloquear las contraseñas guardadas, el token de autenticación o los webhooks de Discord de este servidor. Es posible que provengan de otro usuario de Windows u otro equipo.`n`nIngrese de nuevo las credenciales y pulse Guardar cambios para protegerlas para este usuario de Windows."
    'ServerSetup.Dialog.SettingsAttention.Title' = "La configuración del servidor requiere atención"
    'ServerSetup.Dialog.ExtraArgumentsBlocked.Title' = "Argumentos adicionales bloqueados"
    'ServerSetup.Dialog.DiscordAttention.Title' = "La configuración de Discord requiere atención"
    'ServerSetup.Dialog.IllegalInput.Title' = "Entrada bloqueada"
    'ServerSetup.Dialog.IllegalInput.Body' = "Alerta de seguridad: una de sus entradas contiene caracteres no permitidos."
    'ServerSetup.ErrorAction.SaveMode' = "guardar el modo de configuración"
    'ServerSetup.ErrorAction.SaveSettings' = "guardar la configuración del servidor"
    'ServerSetup.ErrorAction.OpenTokenPage' = "abrir la página del token de autenticación"
    'ServerSetup.GamePicker.Placeholder' = "-- Elegir un juego --"
    'ServerSetup.Placeholder.SelectGame' = "Seleccione un juego…"
    'ServerSetup.Automation.EnableSchedule.AccessibleName' = "Activar programador"
    'ServerSetup.Install.DefaultFolder.AccessibleName' = "Carpeta predeterminada"
    'ServerSetup.Verification.LastTested.Verified' = "Última versión de Synix probada: v{0}  •  {1:d}"
    'ServerSetup.Verification.Verified' = "{0}  ✓ Verificado"
    'ServerSetup.Verification.Unverified' = "{0}  — Aún no verificado"
    'ServerSetup.Minecraft.MetadataLoadFailed' = "No se pudieron cargar los metadatos: {0}"
    'ServerSetup.Minecraft.MojangVersionsLoadFailed' = "No se pudieron cargar las versiones de Mojang: {0}"
    'ServerSetup.Minecraft.LoadingBuilds' = "Cargando compilaciones compatibles…"
    'ServerSetup.Minecraft.JavaVersion' = "Java {0}"
    'ServerSetup.Minecraft.Helper.Vanilla' = "Synix instala el servidor oficial y la versión portátil de Java correspondiente."
    'ServerSetup.Minecraft.Helper.Loader' = "Synix instala el cargador de servidor {0} compatible. Añada sus propios mods después de la instalación."
    'ServerSetup.Minecraft.NoCompatibleBuild' = "No existe una compilación de servidor {0} compatible para Minecraft {1}."
    'ServerSetup.Minecraft.Helper.ResolvedVanilla' = "Minecraft {0} usa el servidor oficial de Mojang y Java {3}."
    'ServerSetup.Minecraft.Helper.ResolvedLoader' = "Minecraft {0} + {1} {2} usa Java {3}. Añada mods después de la instalación."
    'ServerSetup.Minecraft.RetryDetail' = "{0} Vuelva a seleccionar la versión o el cargador para intentarlo de nuevo."
    'ServerSetup.Runtime.ServerPackage' = "Paquete del servidor"
    'ServerSetup.Runtime.OfficialBedrock' = "Bedrock oficial"
    'ServerSetup.MaxPlayers.Limited' = "Jugadores máx. (máximo {0:0})"
    'ServerSetup.SteamAccount.Restore.WindowTitle' = "Restaurar autorización de Steam"
    'ServerSetup.SteamAccount.Restore.Title' = "Restaurar autorización de Steam"
    'ServerSetup.SteamAccount.Restore.Description' = "{0} se importó a este equipo. Confirme el nombre de la cuenta de Steam para que SteamCMD pueda restaurar el acceso antes del primer inicio."
    'ServerSetup.SteamAccount.Required.Description' = "{0} requiere una cuenta de Steam para la instalación. Ingrese el nombre de la cuenta que debe usar SteamCMD."
    'ProblemAction.ServerInstallation' = "Instalación del servidor"
    'ProblemAction.UpdateValidation' = "Actualización o validación de archivos"
    'ProblemAction.ServerStartup' = "Inicio del servidor"
    'ProblemAction.ServerShutdown' = "Apagado del servidor"
    'ProblemAction.RestartWatchdog' = "Reinicio del servidor o vigilancia"
    'ProblemAction.IncorrectStatus' = "Estado incorrecto del servidor"
    'ProblemAction.ResourceMonitoring' = "Supervisión de CPU, memoria o jugadores"
    'ProblemAction.LocalNetwork' = "Conexión de red local"
    'ProblemAction.PublicNetwork' = "Conexión pública o a Internet"
    'ProblemAction.PortsFirewallRcon' = "Puertos, firewall o RCON"
    'ProblemAction.ServerBackups' = "Copias de seguridad del servidor"
    'ProblemAction.TransferExport' = "Exportación de transferencia"
    'ProblemAction.TransferImport' = "Importación de transferencia"
    'ProblemAction.TransferVerification' = "Verificación del paquete de transferencia"
    'ProblemAction.SettingsPasswords' = "Ajustes o contraseñas del servidor"
    'ProblemAction.DiscordAlerts' = "Alertas de Discord"
    'ProblemAction.SynixUpdate' = "Actualización de Synix"
    'ProblemAction.InstallationPackaging' = "Instalación MSI, WinGet o independiente"
    'ProblemAction.WindowDisplay' = "Problema de ventana o visualización"
    'ProblemAction.CrashFreeze' = "Bloqueo o cierre de Synix"
    'ProblemAction.TemplateLaunch' = "Plantilla del servidor o comportamiento de inicio"
    'ProblemAction.Other' = "Otro"
    'Report.EnglishRequiredWarning' = "Importante: escribe el resumen y los detalles del informe en inglés para que el equipo de soporte de Synix pueda revisarlos."
    'Advanced.Firewall.ButtonChecking' = "Comprobando el firewall…"
    'Advanced.Firewall.CheckingPaths' = "Comprobando las rutas de programas del Firewall de Windows…"
    'Advanced.Firewall.Canceled' = "Limpieza cancelada. No se modificó ninguna regla del firewall."
    'Advanced.Firewall.WaitingForAdmin' = "Esperando permiso de administrador…"
    'Advanced.Firewall.RemovedVerified' = "Se eliminaron y verificaron {0} rutas de ejecutables huérfanas."
    'Advanced.Firewall.NoneFound' = "No se encontraron reglas huérfanas en la carpeta predeterminada de juegos de Synix."
    'Advanced.Background.EnabledCurrent' = "Activado para el inicio de sesión de Windows; Cerrar sigue saliendo por completo de Synix."
    'Advanced.Background.DisabledCurrent' = "Desactivado; las tareas programadas solo se ejecutan mientras Synix está abierto."
    'Advanced.Background.EnabledResult' = "Activado para el inicio de sesión de Windows. Cerrar Synix sigue finalizando todos sus procesos de la sesión actual."
    'Advanced.Background.DisabledResult' = "Desactivado. La supervisión en segundo plano se detendrá y no se iniciará al iniciar sesión."
    'AddServer.Title' = "Añadir un servidor"
    'AddServer.Heading' = "¿Cómo quieres añadir un servidor?"
    'AddServer.Subtitle' = "Synix puede instalar un servidor nuevo o registrar de forma segura archivos que ya están en este equipo."
    'AddServer.Create.Title' = "Crear e instalar un servidor nuevo"
    'AddServer.Create.Description' = "Elige el juego y los ajustes; después, Synix descargará los archivos del servidor."
    'AddServer.Create.Button' = "Crear nuevo"
    'AddServer.Import.Title' = "Importar un servidor existente"
    'AddServer.Import.Description' = "Selecciona una carpeta de servidor existente. Tus archivos no se moverán ni sustituirán."
    'AddServer.Import.Button' = "Importar existente"
    'AddServer.Catalog.Title' = "Comprobar primero la compatibilidad del juego"
    'AddServer.Catalog.Description' = "Consulta el catálogo para ver la compatibilidad con ejecutables, configuraciones, juego cruzado y consultas de jugadores."
    'AddServer.Catalog.Button' = "Ver catálogo"
    'Connection.Heading' = "Conectarse a {0}"
    'Connection.Subtitle' = "Usa la dirección correspondiente al lugar desde el que se conecta el jugador."
    'Connection.Local.Title' = "Mismo equipo o red doméstica"
    'Connection.Local.Description' = "Usa esta dirección para jugadores conectados al mismo router."
    'Connection.Public.Title' = "Amigos que se conectan por Internet"
    'Connection.Public.Description' = "El router y el Firewall de Windows deben permitir los puertos del juego y de consulta."
    'Connection.Public.BedrockDescription' = "El router y el Firewall de Windows deben permitir el puerto UDP del juego Bedrock."
    'Connection.Ports.StandardSummary' = "Puertos configurados: {0}. Algunos juegos solo aparecen en el navegador de servidores si también se redirige el puerto de consulta."
    'Connection.Ports.BedrockSummary' = "Puerto de juego Bedrock: {0}/UDP. Puerto IPv6: {1}/UDP. Cada servidor Bedrock necesita su propio par de puertos."
    'Connection.Port.Game' = "juego {0}"
    'Connection.Port.Query' = "consulta {0}"
    'Connection.Port.Rcon' = "RCON {0}"
    'Connection.Port.App' = "aplicación {0}"
    'Connection.Address.Hidden' = "Oculta por el modo de privacidad"
    'Connection.Address.PublicUnavailable' = "No se pudo cargar la dirección pública"
    'Connection.Address.Unavailable' = "No se pudo cargar la dirección"
    'PlayerCenter.Summary.One' = "{0} • {1} • 1 jugador identificado"
    'PlayerCenter.Summary.Many' = "{0} • {1} • {2} jugadores identificados"
    'PlayerCenter.Loading' = "Cargando los detalles de los jugadores…"
    'PlayerCenter.Guidance.Minecraft' = " Selecciona un jugador para usar los comandos de administración local de Minecraft."
    'PlayerCenter.Guidance.UnsupportedActions' = " Las acciones de jugador permanecen desactivadas salvo que el juego proporcione un protocolo de administración verificado."
    'PlayerCenter.Action.Kick' = "Expulsar"
    'PlayerCenter.Action.Allowlist' = "Añadir a la lista permitida"
    'PlayerCenter.Action.Operator' = "Hacer operador"
    'PlayerCenter.SelectValidPlayer' = "Selecciona primero un jugador de Minecraft válido."
    'PlayerCenter.Confirm.Title' = "Confirmar acción sobre jugador de Minecraft"
    'PlayerCenter.Confirm.Kick' = "¿Quieres expulsar a este jugador: {0}?"
    'PlayerCenter.Confirm.Allowlist' = "¿Quieres añadir este jugador a la lista permitida: {0}?"
    'PlayerCenter.Confirm.Operator' = "¿Quieres convertir a este jugador en operador: {0}?"
    'PlayerQuery.GameDefinitionUnavailable' = "La definición del juego no está disponible."
    'PlayerQuery.CrossplayUnavailable' = "El seguimiento de jugadores no está disponible con el juego cruzado activado. Desactívalo para usar el seguimiento Steam A2S."
    'PlayerQuery.ProtocolUnavailable' = "El protocolo de consulta actual de este juego no proporciona una lista universal y segura de nombres de jugadores."
    'PlayerQuery.MinecraftCountOnly' = "Minecraft informa de {0} jugador(es) conectado(s), pero esta consulta no publica sus nombres."
    'PlayerQuery.StartServerFirst' = "Inicia el servidor antes de actualizar los detalles de los jugadores."
    'PlayerQuery.InvalidA2sResponse' = "El servidor devolvió una respuesta de jugadores A2S no válida."
    'PlayerQuery.IncompatiblePlayerList' = "La consulta del servidor funciona, pero no proporcionó una lista de jugadores compatible."
    'PlayerQuery.NoNamedPlayers' = "El servidor respondió y no hay jugadores identificados conectados."
    'PlayerQuery.LoadedPlayers' = "Se cargaron {0} jugador(es) conectado(s)."
    'PlayerQuery.Timeout' = "La consulta de jugadores en el puerto UDP {0} agotó el tiempo de espera."
    'PlayerQuery.ConnectionFailed' = "La consulta de jugadores no pudo conectarse: {0}"
    'PlayerQuery.ReadFailed' = "No se pudieron leer los detalles de los jugadores: {0}"
    'PlayerQuery.BedrockCountOnly' = "Minecraft Bedrock informa de {0} jugador(es) conectado(s), pero su respuesta de estado no publica sus nombres."
    'PlayerQuery.MinecraftManagement.None' = "El servicio de administración local de Minecraft no informa de jugadores conectados."
    'PlayerQuery.MinecraftManagement.Loaded' = "Se cargaron {0} jugador(es) mediante el servicio de administración local de Minecraft."
    'PlayerQuery.MinecraftRcon.None' = "Minecraft RCON no informa de jugadores conectados."
    'PlayerQuery.MinecraftRcon.Loaded' = "Se cargaron {0} jugador(es) mediante el RCON local de Minecraft."
    'PlayerQuery.MinecraftUnavailable' = "Los detalles de los jugadores de Minecraft todavía no están disponibles."
    'PlayerQuery.UnnamedPlayer' = "Jugador sin nombre"
    'ModManager.Subtitle' = "Descubre lo que ya está instalado, añade paquetes locales con seguridad y conserva un registro de reversión sin mantener una lista de cada mod."
    'ModManager.Field.Server' = "SERVIDOR"
    'ModManager.Field.System' = "SISTEMA DE COMPLEMENTOS"
    'ModManager.Field.InstallArea' = "ÁREA DE INSTALACIÓN"
    'ModManager.Support.Checking' = "Comprobando compatibilidad…"
    'ModManager.Step.Detect' = "1  Detectar"
    'ModManager.Step.Stop' = "2  Detener servidor"
    'ModManager.Step.Backup' = "3  Copiar archivos"
    'ModManager.Step.Install' = "4  Instalar"
    'ModManager.Step.Verify' = "5  Verificar"
    'ModManager.Step.Restart' = "6  Reiniciar si es necesario"
    'ModManager.Column.AddOn' = "COMPLEMENTO"
    'ModManager.Column.Type' = "TIPO"
    'ModManager.Column.Version' = "VERSIÓN"
    'ModManager.Column.Status' = "ESTADO"
    'ModManager.Column.Security' = "SEGURIDAD"
    'ModManager.Column.Source' = "ORIGEN"
    'ModManager.Column.Location' = "UBICACIÓN"
    'ModManager.Safety.Title' = "Lista de seguridad automática"
    'ModManager.Safety.Subtitle' = "Synix comprueba estos puntos antes de cambiar nada."
    'ModManager.Selection.Empty' = "Selecciona un complemento para ver dónde se encontró."
    'ModManager.Button.InstallFile' = "Instalar archivo"
    'ModManager.Button.InstallFramework' = "Instalar framework"
    'ModManager.Button.BrowseCatalog' = "Ver catálogo"
    'ModManager.Button.BrowseCatalogs' = "Ver catálogos"
    'ModManager.Button.OpenFolder' = "Abrir carpeta de complementos"
    'ModManager.Button.Refresh' = "Actualizar"
    'ModManager.Button.Remove' = "Eliminar selección"
    'ModManager.Button.Close' = "Cerrar"
    'ModManager.Button.ManageIds' = "Gestionar ID de mods"
    'ModManager.Inventory.Empty' = "No se encontraron complementos en las carpetas del perfil activo."
    'ModManager.Inventory.One' = "1 complemento encontrado  •  {1} seguido por Synix"
    'ModManager.Inventory.Many' = "{0} complementos encontrados  •  {1} seguidos por Synix"
    'ModManager.Inventory.RefreshFailed' = "Synix no pudo actualizar las carpetas de complementos."
    'ModManager.Support.ProviderIds' = "LISTO • Synix gestiona la lista ordenada de ID de mods del proveedor"
    'ModManager.Support.FileImport' = "LISTO • Synix puede importar archivos locales de complementos de forma segura"
    'ModManager.Support.SetupNeeded' = "CONFIGURACIÓN NECESARIA • Selecciona o instala primero un framework compatible"
    'ModManager.Support.DetectionOnly' = "SOLO DETECCIÓN • El proveedor del juego sigue siendo responsable de la instalación"
    'ModManager.Framework.Automatic' = "El cargador del servidor y las carpetas existentes eligen automáticamente el área de instalación."
    'ModManager.Framework.Named' = "Framework: {0}."
    'ModManager.Unsupported.Title' = "TODAVÍA NO HAY PERFIL DE COMPLEMENTOS"
    'ModManager.Unsupported.Description' = "Synix no adivinará dónde guarda este juego sus mods. Un pequeño perfil de datos podrá añadir compatibilidad sin reescribir esta ventana."
    'ModManager.NoFilesChanged' = "No se modificó ningún archivo."
    'ModManager.Safety.ServerStopped' = "El servidor está detenido"
    'ModManager.Safety.StopFirst' = "Detén el servidor antes de hacer cambios"
    'ModManager.Safety.FrameworkDetected' = "Framework detectado"
    'ModManager.Safety.FrameworkRequired' = "Se requiere configurar el framework"
    'ModManager.Safety.FolderAvailable' = "Carpeta del servidor disponible"
    'ModManager.Safety.FolderMissing' = "Falta la carpeta del servidor"
    'ModManager.Safety.ProviderTrust' = "La descarga del proveedor requiere confianza manual"
    'ModManager.Safety.SecurityScan' = "El análisis de seguridad se ejecuta antes de instalar"
    'ModManager.Safety.StandardPermissions' = "Permisos estándar de Windows"
    'ModManager.Safety.RestartWithoutAdmin' = "Reinicia sin acceso de administrador"
    'ModManager.Safety.RestartRequired' = "Es necesario reiniciar después de los cambios"
    'ModManager.Safety.LiveReload' = "El framework admite recarga en directo"
    'ModManager.Profile.Rust.Description' = "Complementos de Rust cargados por el framework Oxide/uMod."
    'ModManager.Profile.Rust.Target' = "Complementos de Oxide"
    'ModManager.Profile.Minecraft.Name' = "Complementos de Minecraft"
    'ModManager.Profile.Minecraft.Description' = "Complementos o mods JAR seleccionados según el cargador y las carpetas del servidor."
    'ModManager.Profile.Minecraft.ModsTarget' = "Mods del cargador"
    'ModManager.Profile.Minecraft.PluginsTarget' = "Complementos del servidor"
    'ModManager.Profile.SevenDays.Name' = "Mods del servidor de 7 Days to Die"
    'ModManager.Profile.SevenDays.Description' = "Synix instala paquetes ZIP completos de mods en la carpeta Mods del servidor dedicado. Los mods con recursos de cliente también pueden necesitar instalación en cada jugador."
    'ModManager.Profile.SevenDays.Target' = "Carpeta Mods del servidor"
    'ModManager.Profile.ArkEvolved.Name' = "Mods de Steam Workshop"
    'ModManager.Profile.ArkEvolved.Description' = "Synix gestiona los ID ordenados de Steam Workshop; ARK y Steam descargan y actualizan el contenido real."
    'ModManager.Profile.ArkEvolved.Target' = "ID ordenados de Steam Workshop"
    'ModManager.Profile.ArkAscended.Name' = "Mods de servidor de CurseForge"
    'ModManager.Profile.ArkAscended.Description' = "Synix gestiona la lista ordenada de ID de mods; ARK descarga y actualiza el contenido de CurseForge al iniciar el servidor."
    'ModManager.Profile.ArkAscended.Target' = "ID ordenados de mods de CurseForge"
    'ModManager.Profile.Discovered.Name' = "Carpetas de complementos detectadas"
    'ModManager.Profile.Discovered.Description' = "Synix encontró carpetas comunes de complementos y puede inventariarlas con seguridad. La instalación permanece desactivada hasta que se añada un perfil verificado."
    'ModManager.Known.Mod' = "Mod"
    'ModManager.Known.Plugin' = "Complemento"
    'ModManager.Known.ModId' = "ID de mod"
    'ModManager.Known.ProviderManaged' = "Gestionado por el proveedor"
    'ModManager.Known.ConfiguredNextStart' = "Configurado para el próximo inicio"
    'ModManager.Known.ProviderNotScanned' = "Descarga del proveedor sin análisis previo"
    'ModManager.Known.GameProvider' = "Proveedor del juego"
    'ModManager.Known.Detected' = "Detectado en el disco"
    'ModManager.Known.Healthy' = "Correcto"
    'ModManager.Known.Changed' = "Modificado fuera de Synix"
    'ModManager.Known.NotReviewed' = "No revisado por Synix"
    'ModManager.Known.LegacyNotReviewed' = "Instalación antigua • sin revisar"
    'ModManager.Known.StructuralOnly' = "Solo comprobaciones estructurales"
    'ModManager.Known.ReviewRecorded' = "Revisión previa a la instalación registrada"
    'ModManager.Known.External' = "Externo"
    'ModManager.Known.ExternalProvider' = "Proveedor externo"
    'ModManager.Known.SynixImport' = "Importación de Synix"
    'ModManager.Known.LocalPackage' = "Paquete local"
    'ModManager.Known.BuiltInLoader' = "Cargador de mods integrado"
    'ModManager.Known.ArkBuiltInInstaller' = "Instalador de mods integrado de ARK"
    'ResourceMonitor.WindowTitleFiltered' = "Detalles de procesos en directo - {0}"
    'ResourceMonitor.GridTitleFiltered' = "Detalles de procesos en directo  •  {0}"
    'ResourceMonitor.FilteredSubtitle' = "Todos los lanzadores, hosts de consola y procesos de juego que Synix ha verificado en este grupo."
    'ResourceMonitor.RowRunning' = "●  En ejecución"
    'ResourceMonitor.CpuCaption' = "En todos los procesos de servidor gestionados"
    'ResourceMonitor.RamValue' = "{0:N2} GB"
    'ResourceMonitor.RamCaption' = "{0:N1} % de {1:N1} GB de memoria del sistema"
    'ResourceMonitor.Active.None' = "No se detectaron procesos de servidor en ejecución"
    'ResourceMonitor.Active.One' = "1 proceso de servidor está en línea"
    'ResourceMonitor.Active.Many' = "{0} procesos de servidor están en línea"
    'ResourceMonitor.ProcessCount.One' = "1 proceso en ejecución"
    'ResourceMonitor.ProcessCount.Many' = "{0} procesos en ejecución"
    'ResourceMonitor.LastUpdated' = "Actualizado a las {0:T}  •  Actualización automática cada segundo"
    'ResourceMonitor.Empty' = "No se detectaron servidores de juego en ejecución"
}

$additionalSemanticTranslations = & (Join-Path $PSScriptRoot 'SemanticResources.es.ps1')
foreach ($entry in $additionalSemanticTranslations.GetEnumerator()) {
    if ($semanticTranslations.Contains($entry.Key)) {
        throw "Duplicate Spanish semantic resource key: $($entry.Key)"
    }

    $semanticTranslations[$entry.Key] = [string]$entry.Value
}

$pairs = @(
    @("—", "—"),
    @(":", ":"),
    @(".NET Framework requirement", "Requisito de .NET Framework"),
    @("↺", "↺"),
    @("↻", "↻"),
    @("↻  Restart", "↻  Reiniciar"),
    @("+  Add Server", "+  Añadir servidor"),
    @("×", "×"),
    @(">_", ">_"),
    @("⌕", "⌕"),
    @("⌘", "⌘"),
    @("■  Stop", "■  Detener"),
    @("▤", "▤"),
    @("▶  Start", "▶  Iniciar"),
    @("◆", "◆"),
    @("◇", "◇"),
    @("◇  Sensitive fields follow the Synix Privacy Mode setting.", "◇ Los campos confidenciales siguen la configuración del Modo de privacidad de Synix."),
    @("◇  Template-aware controls: unavailable settings are disabled automatically for the selected game.", "◇ Controles basados en plantillas: las configuraciones no disponibles se desactivan automáticamente para el juego seleccionado."),
    @("◎", "◎"),
    @("●", "●"),
    @("●  Action required", "●  Acción requerida"),
    @("●  Initializing SteamCMD...", "● Inicializando SteamCMD..."),
    @("●  LIVE MONITORING", "● MONITOREO EN VIVO"),
    @("●  LIVE TELEMETRY", "● TELEMETRÍA EN VIVO"),
    @("●  SteamCMD needs attention", "● SteamCMD necesita atención"),
    @("●  SteamCMD ready", "● Listo para SteamCMD"),
    @("◷", "◷"),
    @("⚙", "⚙"),
    @("⚠ Changing this location does not delete backups from the previous folder.", "⚠ Cambiar esta ubicación no elimina las copias de seguridad de la carpeta anterior."),
    @("✓", "✓"),
    @("✓  Readiness", "✓  Preparación"),
    @("✕", "✕"),
    @("➜", "➜"),
    @("⬡", "⬡"),
    @("🔒 [REQUIRED] Enter a Server Name and select a Game Template.", "🔒 [OBLIGATORIO] Ingrese un nombre de servidor y seleccione una plantilla de juego."),
    @("0", "0"),
    @("0 help articles", "0 artículos de ayuda"),
    @("0 running servers", "0 servidores en ejecución"),
    @("0 servers", "0 servidores"),
    @("0 settings", "0 configuraciones"),
    @("0 unsaved changes", "0 cambios no guardados"),
    @("0.0%", "0,0%"),
    @("0.0% of system memory", "0,0% de la memoria del sistema"),
    @("0.00 GB", "0,00GB"),
    @("1   YOUR DATA STAYS SEPARATE`r`nServers, settings, backups, runtimes, and SteamCMD are stored under C:\Synix so application updates do not replace them.", "1 TUS DATOS QUEDAN SEPARADOS`r`nLos servidores, la configuración, las copias de seguridad, los tiempos de ejecución y SteamCMD se almacenan en C:\Synix, por lo que las actualizaciones de la aplicación no los reemplazan."),
    @("1–100 per server", "1–100 por servidor"),
    @("10", "10"),
    @("12345", "12345"),
    @("2   ADD A SERVER`r`nChoose a game, enter the friendly settings, and let Synix install it. Steam login is requested only when that game requires it.", "2 AÑADIR UN SERVIDOR`r`nElige un juego, ingresa a la configuración amigable y deja que Synix lo instale. El inicio de sesión en Steam se solicita solo cuando ese juego lo requiere."),
    @("27015", "27015"),
    @("27016", "27016"),
    @("3   START, STOP, AND VERIFY`r`nSynix shows the exact launch arguments, verifies startup, uses safe stop behavior where supported, and keeps recent logs available.", "3 ARRANCAR, DETENER Y VERIFICAR`r`nSynix muestra los argumentos de inicio exactos, verifica el inicio, utiliza un comportamiento de parada segura cuando sea compatible y mantiene registros recientes disponibles."),
    @("4   NETWORK ACCESS`r`nWindows Firewall permission and router port forwarding are different. Synix checks local conflicts, but never changes your router.", "4 ACCESO A LA RED`r`nEl permiso del Firewall de Windows y el reenvío de puertos del enrutador son diferentes. Synix comprueba los conflictos locales, pero nunca cambia su enrutador."),
    @("5   RECOVERY AND BACKUPS`r`nUse Settings > Advanced > Troubleshooter for safe health checks and repairs. Use Backups before moving Synix or making large changes.", "5 RECUPERACIÓN Y COPIAS DE SEGURIDAD`r`nUtilice Configuración > Avanzado > Solucionador de problemas para realizar comprobaciones de estado y reparaciones seguras. Utilice copias de seguridad antes de mover Synix o realizar cambios importantes."),
    @("7777", "7777"),
    @("A server operation is currently in progress", "Actualmente hay una operación de servidor en curso."),
    @("Access controls, startup behavior, and integrations", "Controles de acceso, comportamiento de inicio e integraciones"),
    @("Access Credentials", "Credenciales de acceso"),
    @("Account name, not your Steam display name", "Nombre de cuenta, no su nombre para mostrar de Steam"),
    @("Across all managed server processes", "En todos los procesos del servidor administrado"),
    @("ACTIVE SERVERS", "SERVIDORES ACTIVOS"),
    @("Activity & Diagnostics", "Actividad y diagnóstico"),
    @("Add Destination", "Agregar destino"),
    @("Add every other complete template the game needs. Edit Installed location so each path is relative to the installed server folder.", "Agrega todas las demás plantillas completas que el juego necesite. Edite la ubicación instalada para que cada ruta sea relativa a la carpeta del servidor instalado."),
    @("Add files", "Agregar archivos"),
    @("Added automatically", "Agregado automáticamente"),
    @("Additional configuration files", "Archivos de configuración adicionales"),
    @("Additional files are required", "Se requieren archivos adicionales"),
    @("Admin Password", "Contraseña de administrador"),
    @("Advanced", "Avanzado"),
    @("Advanced Discord Destinations", "Destinos avanzados de Discord"),
    @("Agreement required", "Se requiere acuerdo"),
    @("Allow launch-file export", "Permitir exportación de archivos de inicio"),
    @("App Port", "Puerto de aplicación"),
    @("AREA", "ZONA"),
    @("Argument Test", "Prueba de argumento"),
    @("ARGUMENTS", "ARGUMENTOS"),
    @("Authentication Token", "Token de autenticación"),
    @("Auto Restart", "Reinicio automático"),
    @("Automatic evidence comes from Synix actions; arguments require the real-server test.", "La evidencia automática proviene de acciones de Synix; Los argumentos requieren la prueba del servidor real."),
    @("Automatic safety checklist and next steps", "Lista de verificación de seguridad automática y próximos pasos"),
    @("Automatically builds a safe game/server folder below the configured Games path.", "Crea automáticamente una carpeta de juego/servidor segura debajo de la ruta de Juegos configurada."),
    @("Automatically collect generated game configurations", "Recopilar automáticamente las configuraciones de juego generadas"),
    @("Automation", "Automatización"),
    @("BACKUP FILE", "ARCHIVO DE COPIA DE SEGURIDAD"),
    @("Backup on Start", "Copia de seguridad al iniciar"),
    @("Backup Server", "Servidor de respaldo"),
    @("Backups", "Copias de seguridad"),
    @("Before you continue", "Antes de continuar"),
    @("Blocks launch with clear Microsoft download guidance when the runtime is missing.", "Bloquea el inicio con una clara guía de descarga de Microsoft cuando falta el tiempo de ejecución."),
    @("Blocks setup with a clear message when the processor does not support AVX2.", "Bloquea la configuración con un mensaje claro cuando el procesador no es compatible con AVX2."),
    @("Browse", "Navegar"),
    @("Browse Folder", "Explorar carpeta"),
    @("Browse topics", "Explorar temas"),
    @("Builder guide and supported tags", "Guía del creador y etiquetas compatibles"),
    @("C:\Synix\Games\Example Server", "C:\Synix\Juegos\Servidor de ejemplo"),
    @("Cancel", "Cancelar"),
    @("Cancel Check", "Cancelar cheque"),
    @("Canceling the release check safely...", "Cancelando el control de liberación de forma segura..."),
    @("Catalog order", "Orden de catálogo"),
    @("CHECK", "COMPROBAR"),
    @("Check Again", "Comprobar de nuevo"),
    @("Check for DDoS", "Comprobar DDoS"),
    @("Check Release", "Verificar lanzamiento"),
    @("Check release readiness", "Comprobar la preparación para el lanzamiento"),
    @("Check shared runtimes, server files, configurations, ports, Windows Firewall, disk space, interrupted processes, recent logs, and Synix update health from one place.", "Verifique tiempos de ejecución compartidos, archivos del servidor, configuraciones, puertos, Firewall de Windows, espacio en disco, procesos interrumpidos, registros recientes y el estado de las actualizaciones de Synix desde un solo lugar."),
    @("Check SteamCMD for updates before launching the server.", "Consulte SteamCMD para obtener actualizaciones antes de iniciar el servidor."),
    @("Check SteamCMD, runtimes, server files, configs, ports, Windows Firewall, disk space, interrupted processes, recent logs, and update health.", "Verifique SteamCMD, tiempos de ejecución, archivos del servidor, configuraciones, puertos, Firewall de Windows, espacio en disco, procesos interrumpidos, registros recientes y estado de las actualizaciones."),
    @("Check Synix Values", "Verifique los valores de Synix"),
    @("Checking for updates...", "Buscando actualizaciones..."),
    @("Checking release files...", "Comprobando archivos de lanzamiento..."),
    @("Checks the actual publish output without rebuilding Synix, starting the release, or accessing C:\Synix.", "Comprueba el resultado de publicación real sin reconstruir Synix, iniciar la versión o acceder a C:\Synix."),
    @("Checks the installed Windows .NET Framework release before the server starts.", "Comprueba la versión de Windows .NET Framework instalada antes de que se inicie el servidor."),
    @("Checks whether virtualization support is enabled and available to Windows.", "Comprueba si el soporte de virtualización está habilitado y disponible para Windows."),
    @("Choose a row to unlock server controls", "Elija una fila para desbloquear los controles del servidor"),
    @("Choose a server type to show its local verification history.", "Elija un tipo de servidor para mostrar su historial de verificación local."),
    @("Choose Folder", "Elegir carpeta"),
    @("Choose only the built-in launch behavior verified for this dedicated server.", "Elija solo el comportamiento de inicio integrado verificado para este servidor dedicado."),
    @("Choose Package", "Elija el paquete"),
    @("Choose the backup that should replace the server's current files.", "Elija la copia de seguridad que debería reemplazar los archivos actuales del servidor."),
    @("Choose the game and define the server identity.", "Elige el juego y define la identidad del servidor."),
    @("Choose the language used by Synix. Game settings and configuration values remain in English.", "Elija el idioma utilizado por Synix. Los ajustes del juego y los valores de configuración permanecen en inglés."),
    @("Choose when Synix should perform the scheduled server restart.", "Elija cuándo Synix debe realizar el reinicio programado del servidor."),
    @("Clean Orphaned Rules", "Limpiar reglas huérfanas"),
    @("Clean orphaned Synix server firewall rules", "Limpiar las reglas de firewall del servidor Synix huérfano"),
    @("CLEAR", "CLARO"),
    @("Clear Filters", "Borrar filtros"),
    @("Clear Mark", "Borrar marca"),
    @("Close", "Cerrar"),
    @("Collect generated game configurations now", "Recopile configuraciones de juegos generadas ahora"),
    @("Collect Now", "Recoger ahora"),
    @("Commands stay on this computer unless you intentionally configure Java RCON for remote access.", "Los comandos permanecen en esta computadora a menos que configure intencionalmente Java RCON para acceso remoto."),
    @("Compatibility Verification", "Verificación de compatibilidad"),
    @("Complete the required file setup before the dedicated server can start.", "Complete la configuración del archivo requerido antes de que pueda iniciarse el servidor dedicado."),
    @("Complete, working configuration template file", "Archivo de plantilla de configuración completo y funcional"),
    @("Config Editor", "Editor de configuración"),
    @("CONFIG SOURCE", "FUENTE DE CONFIGURACIÓN"),
    @("Config unavailable", "Configuración no disponible"),
    @("CONFIGURATION", "CONFIGURACIÓN"),
    @("Configuration & Security", "Configuración y seguridad"),
    @("Configuration Application Check", "Verificación de la aplicación de configuración"),
    @("Configuration behavior", "Comportamiento de configuración"),
    @("Configuration Editor", "Editor de configuración"),
    @("Configuration file", "Archivo de configuración"),
    @("Configuration format", "Formato de configuración"),
    @("Configuration path relative to the installed server folder", "Ruta de configuración relativa a la carpeta del servidor instalado"),
    @("Configuration repair is available", "La reparación de configuración está disponible"),
    @("Configuration report copied to the clipboard.", "Informe de configuración copiado al portapapeles."),
    @("CONFIGURATION STATUS", "ESTADO DE LA CONFIGURACIÓN"),
    @("Configure", "Configurar"),
    @("Configure basic Synix behavior on this computer.", "Configure el comportamiento básico de Synix en esta computadora."),
    @("Configure Schedule", "Configurar horario"),
    @("Confirm password", "Confirmar contraseña"),
    @("Confirm removal of the listed firewall rules", "Confirmar la eliminación de las reglas de firewall enumeradas"),
    @("Connect GitHub", "Conectar GitHub"),
    @("Connect GitHub account", "Conectar cuenta de GitHub"),
    @("CONNECTED", "CONECTADO"),
    @("Connection Information", "Información de conexión"),
    @("Continue only after you have reviewed the required setup steps.", "Continúe solo después de haber revisado los pasos de configuración requeridos."),
    @("Copy Address", "Copiar dirección"),
    @("Copy allowlisted Steam runtime files after install", "Copie los archivos de tiempo de ejecución de Steam incluidos en la lista permitida después de la instalación"),
    @("Copy approved Steam runtime files after installation", "Copie los archivos de tiempo de ejecución de Steam aprobados después de la instalación"),
    @("Copy Details", "Copiar detalles"),
    @("Copy problem report", "Copiar informe de problemas"),
    @("Copy Report", "Copiar informe"),
    @("Covers the unified Microsoft runtime used by current 2015, 2017, 2019, and 2022 servers.", "Cubre el tiempo de ejecución unificado de Microsoft utilizado por los servidores actuales de 2015, 2017, 2019 y 2022."),
    @("CPU Usage", "Uso de CPU"),
    @("CPU USAGE", "USO DE LA CPU"),
    @("Create a protected server backup before each launch.", "Cree una copia de seguridad del servidor protegido antes de cada lanzamiento."),
    @("Create a transfer password", "Crear una contraseña de transferencia"),
    @("Create a validated built-in game definition without plugins or scripts. Definitions are saved into the project and become available only after Synix is rebuilt.", "Cree una definición de juego integrada validada sin complementos ni secuencias de comandos. Las definiciones se guardan en el proyecto y están disponibles solo después de que se reconstruye Synix."),
    @("Create Batch File", "Crear archivo por lotes"),
    @("CREATED", "CREADO"),
    @("Crossplay", "Juego cruzado"),
    @("Ctrl+F  Search     •     Esc  Close     •     Links open in your browser", "Ctrl+F Buscar • Esc Cerrar • Enlaces abiertos en su navegador"),
    @("Current state reported by the Synix engine", "Estado actual informado por el motor Synix"),
    @("Current version and installation type", "Versión actual y tipo de instalación."),
    @("Custom backup location", "Ubicación de copia de seguridad personalizada"),
    @("D", "re"),
    @("Dark Mode", "Modo oscuro"),
    @("Dark mode toggle", "Alternar modo oscuro"),
    @("DDoS Attack Detection", "Detección de ataques DDoS"),
    @("Decline", "Rechazar"),
    @("Default launch arguments", "Argumentos de lanzamiento predeterminados"),
    @("Default Launch Arguments", "Argumentos de lanzamiento predeterminados"),
    @("Default launch arguments (everything after the executable)", "Argumentos de inicio predeterminados (todo después del ejecutable)"),
    @("Default startup arguments", "Argumentos de inicio predeterminados"),
    @("Definition Builder", "Generador de definiciones"),
    @("Definition ID", "ID de definición"),
    @("Definition revision", "Revisión de definición"),
    @("Definition test report copied to the clipboard.", "Informe de prueba de definición copiado al portapapeles."),
    @("Delete Backup", "Eliminar copia de seguridad"),
    @("Delete Server", "Eliminar servidor"),
    @("Describe the problem", "Describe el problema"),
    @("DESTINATION", "DESTINO"),
    @("Destination name", "Nombre de destino"),
    @("DETAILS", "DETALLES"),
    @("Development", "Desarrollo"),
    @("Disabled — scheduled work runs only while Synix is open.", "Deshabilitado: el trabajo programado se ejecuta solo mientras Synix está abierto."),
    @("Disconnect GitHub", "Desconectar GitHub"),
    @("Disconnect GitHub account", "Desconectar la cuenta de GitHub"),
    @("Discord", "Discord"),
    @("Discord Alerts", "Alertas de Discord"),
    @("Discord Destination", "Destino de Discord"),
    @("Discord opened. Select New Post in the bug-reporting forum and paste the copied report.", "Discord se abrió. Selecciona Nueva publicación en el foro de informes de errores y pega el informe copiado."),
    @("Discord webhook URL", "URL del webhook de Discord"),
    @("Discord Webhooks", "Webhooks de Discord"),
    @("Do not paste passwords, webhooks, IP addresses, private configuration, or full launch commands. Synix removes common secrets before sending.", "No pegue contraseñas, webhooks, direcciones IP, configuraciones privadas ni comandos de inicio completos. Synix elimina los secretos comunes antes de enviar."),
    @("Documents source folder for automatic imports (optional)", "Carpeta de origen de documentos para importaciones automáticas (opcional)"),
    @("Each rule points to an executable under C:\Synix\Games\[Game]\[Server], but that individual server folder is gone and no installed Synix server owns the path.", "Cada regla apunta a un ejecutable en C:\Synix\Games\[Juego]\[Servidor], pero esa carpeta del servidor individual desapareció y ningún servidor Synix instalado posee la ruta."),
    @("Edit", "Editar"),
    @("Edit serverconfig.xml safely without changing its XML structure.", "Edite serverconfig.xml de forma segura sin cambiar su estructura XML."),
    @("Edition", "Edición"),
    @("Elevated System Tasks", "Tareas elevadas del sistema"),
    @("Elevated system tasks", "Tareas elevadas del sistema"),
    @("Enable only when anonymous SteamCMD installation fails and a Steam account is required.", "Habilítelo solo cuando falle la instalación anónima de SteamCMD y se requiera una cuenta de Steam."),
    @("Enable only when the server cannot run correctly without Windows elevation.", "Habilítelo solo cuando el servidor no pueda ejecutarse correctamente sin la elevación de Windows."),
    @("Enable RCON", "Habilitar RCON"),
    @("Enable RCON only for game templates that support secure remote commands.", "Habilite RCON solo para plantillas de juegos que admitan comandos remotos seguros."),
    @("Enable server query monitoring", "Habilitar el monitoreo de consultas del servidor"),
    @("Enable when the server has a verified query or network probe that Synix can monitor.", "Habilítelo cuando el servidor tenga una consulta verificada o una sonda de red que Synix pueda monitorear."),
    @("Enabled", "Habilitado"),
    @("Encrypted Export", "Exportación cifrada"),
    @("Enter a valid Steam account name.", "Ingrese un nombre de cuenta de Steam válido."),
    @("Enter IDs in the order they should load. Use commas, spaces, or one ID per line. Synix does not need a database of mod names.", "Ingrese los ID en el orden en que deben cargarse. Utilice comas, espacios o un ID por línea. Synix no necesita una base de datos de nombres de mods."),
    @("Enter the game information, then validate before saving.", "Ingresa la información del juego, luego valida antes de guardar."),
    @("EVENTS", "EVENTOS"),
    @("Example Game", "Juego de ejemplo"),
    @("Example: Server closes a few seconds after Start", "Ejemplo: el servidor se cierra unos segundos después del inicio"),
    @("EXECUTABLE", "EJECUTABLE"),
    @("Expected result", "Resultado esperado"),
    @("EXPERIMENTAL", "EXPERIMENTAL"),
    @("Export", "Exportar"),
    @("Export to Project", "Exportar al proyecto"),
    @("External deployment is for launchers or virtual machines and disables query monitoring.", "La implementación externa es para lanzadores o máquinas virtuales y deshabilita la supervisión de consultas."),
    @("Extra Arguments", "Argumentos adicionales"),
    @("Find setup guidance, command details, and troubleshooting answers.", "Encuentre orientación de configuración, detalles de comandos y respuestas a la solución de problemas."),
    @("Finds rules whose executable was under C:\Synix\Games\[Game]\[Server], but that specific server is no longer saved and its server folder is gone. Ports and custom install folders are not scanned.", "Encuentra reglas cuyo ejecutable estaba en C:\Synix\Games\[Juego]\[Servidor], pero ese servidor específico ya no se guarda y su carpeta de servidor desapareció. Los puertos y las carpetas de instalación personalizada no se analizan."),
    @("Firewall Cleanup Review", "Revisión de limpieza del firewall"),
    @("Firewall executable rules ready for removal", "Reglas ejecutables del firewall listas para ser eliminadas"),
    @("First-launch preparation", "Preparación del primer lanzamiento"),
    @("First-Start Assistant", "Asistente de primer inicio"),
    @("First-start message shown to the user", "Mensaje de primer inicio mostrado al usuario."),
    @("Fix Config", "Reparar configuración"),
    @("Folder", "Carpeta"),
    @("Folder Path", "Ruta de la carpeta"),
    @("FORMAT-AWARE EDITING", "EDICIÓN CONSCIENTE DEL FORMATO"),
    @("Framework", "Marco"),
    @("Full Release Notes", "Notas de la versión completa"),
    @("Game", "Juego"),
    @("GAME", "JUEGO"),
    @("Game Definition Builder", "Creador de definiciones de juegos"),
    @("Game Definition Test Runner", "Corredor de prueba de definición de juego"),
    @("Game icon HTTPS URL (optional)", "Ícono del juego URL HTTPS (opcional)"),
    @("Game Mode", "Modo de juego"),
    @("Game modes (one exact value per line)", "Modos de juego (un valor exacto por línea)"),
    @("Game name", "nombre del juego"),
    @("Game port", "Puerto de juego"),
    @("Game Port", "Puerto del juego"),
    @("Game Server", "Servidor de juego"),
    @("Game Servers", "Servidores de juego"),
    @("Game Support Catalog", "Catálogo de soporte de juegos"),
    @("Game Verification Queue", "Cola de verificación del juego"),
    @("Game Version", "Versión del juego"),
    @("Gameplay Profile", "Perfil de juego"),
    @("General", "General"),
    @("Get Token", "Obtener ficha"),
    @("Getting Started with Synix", "Comenzando con Synix"),
    @("GH", "GH"),
    @("GitHub is not connected. Copy and Discord options still work.", "GitHub no está conectado. Las opciones Copiar y Discord aún funcionan."),
    @("GitHub posts directly without opening a browser after the account is connected.", "GitHub publica directamente sin abrir un navegador después de conectar la cuenta."),
    @("Guide", "Guía"),
    @("Help", "Ayuda"),
    @("HELP & SUPPORT", "AYUDA Y SOPORTE"),
    @("Help Center", "Centro de ayuda"),
    @("Hide IP addresses, passwords, and other sensitive information while screen sharing.", "Oculte direcciones IP, contraseñas y otra información confidencial mientras comparte la pantalla."),
    @("HOUR", "HORA"),
    @("How can we help?", "¿Cómo podemos ayudar?"),
    @("How the user obtains and places required game files", "Cómo el usuario obtiene y coloca los archivos necesarios del juego"),
    @("I Agree", "Estoy de acuerdo"),
    @("I confirmed the displayed server name, ports, player limit, and all other values used by this definition, including passwords, RCON, mode, and map/world where applicable.", "Confirmé el nombre del servidor mostrado, los puertos, el límite de jugadores y todos los demás valores utilizados por esta definición, incluidas contraseñas, RCON, modo y mapa/mundo, cuando corresponda."),
    @("I Understand", "yo entiendo"),
    @("Identity, world, player, and network information", "Información de identidad, mundo, jugador y red."),
    @("IMPORT  No package selected`nChoose a package to calculate space and time.", "IMPORTAR Ningún paquete seleccionado`nElija un paquete para calcular el espacio y el tiempo."),
    @("Import Existing Server", "Importar servidor existente"),
    @("Import Synix", "Importar Synix"),
    @("Important: Write the summary and report details in English so the Synix support team can review them.", "Importante: escriba el resumen y los detalles del informe en inglés para que el equipo de soporte de Synix pueda revisarlos."),
    @("Individual events", "Eventos individuales"),
    @("Insert a supported Synix argument tag", "Insertar una etiqueta de argumento Synix compatible"),
    @("Insert tag", "Insertar etiqueta"),
    @("INSTALL", "INSTALAR"),
    @("Install  — Not verified yet", "Instalar: aún no verificado"),
    @("Install & Launch", "Instalar e iniciar"),
    @("Install Location", "Ubicación de instalación"),
    @("Install this game in Synix before testing its real launch arguments.", "Instala este juego en Synix antes de probar sus argumentos de lanzamiento reales."),
    @("Install Update", "Instalar actualización"),
    @("Install, start, stop, and monitoring checks are recorded automatically. Argument verification uses a real installed server and a sanitized command test; configuration remains a manual file check.", "Las comprobaciones de instalación, inicio, parada y supervisión se registran automáticamente. La verificación de argumentos utiliza un servidor instalado real y una prueba de comando desinfectada; La configuración sigue siendo una verificación manual de archivos."),
    @("Installation canceled. No files were changed.", "Instalación cancelada. No se cambiaron archivos."),
    @("Installed location", "Ubicación instalada"),
    @("Installed server to test", "Servidor instalado para probar."),
    @("Installed Servers", "Servidores instalados"),
    @("INTEGRITY", "INTEGRIDAD"),
    @("Interface language", "Idioma de la interfaz"),
    @("Invite Code", "Código de invitación"),
    @("KNOWLEDGE BASE", "BASE DE CONOCIMIENTOS"),
    @("KNOWLEDGE BASE READY", "BASE DE CONOCIMIENTOS LISTO"),
    @("LAN IP: Fetching...", "IP de LAN: Obteniendo..."),
    @("Language", "Idioma"),
    @("LAST TESTED", "ÚLTIMA PRUEBA"),
    @("LAST VERIFIED", "ÚLTIMO VERIFICADO"),
    @("Last-tested Synix version: Not verified yet", "Última versión de Synix probada: aún no verificada"),
    @("Later", "Más tarde"),
    @("Launch Arguments", "Argumentos de lanzamiento"),
    @("Launch behavior", "Comportamiento de lanzamiento"),
    @("Launch file", "Iniciar archivo"),
    @("Launch Preparation", "Preparación del lanzamiento"),
    @("Launch preparation", "Preparación del lanzamiento"),
    @("Launch with administrator permission", "Iniciar con permiso de administrador"),
    @("Lets the user create a reviewed launch file. Disable for deployment commands that must stay inside Synix.", "Permite al usuario crear un archivo de inicio revisado. Desactívelo para los comandos de implementación que deben permanecer dentro de Synix."),
    @("Limit the number of backups retained per server.", "Limite la cantidad de copias de seguridad retenidas por servidor."),
    @("Live performance across every managed game server process.", "Rendimiento en vivo en todos los procesos del servidor de juegos administrado."),
    @("Live performance and configuration details", "Detalles de configuración y rendimiento en vivo"),
    @("Loader", "Cargador"),
    @("Loader Version", "Versión del cargador"),
    @("Loading the built-in game verification queue...", "Cargando la cola de verificación integrada del juego..."),
    @("Loading...", "Cargando…"),
    @("LOCATION", "UBICACIÓN"),
    @("Logs\*.log`r`nSaved\Logs\**\*.log", "Registros\*.log`r`nGuardado\Registros\**\*.log"),
    @("Long-Duration Reliability Test", "Prueba de confiabilidad de larga duración"),
    @("Main World", "Mundo principal"),
    @("Maintenance schedule", "Programa de mantenimiento"),
    @("Maintenance Schedule", "Programa de mantenimiento"),
    @("Manage Provider Mod IDs", "Administrar ID de mod de proveedor"),
    @("Map", "Mapa"),
    @("Map / World", "Mapa / Mundo"),
    @("Map and mode choices come directly from the selected game template.", "Las opciones de mapa y modo provienen directamente de la plantilla de juego seleccionada."),
    @("Maps or scenarios (one exact value per line)", "Mapas o escenarios (un valor exacto por línea)"),
    @("Mark Verified", "Marca verificada"),
    @("Master Discord Webhook", "Webhook maestro de Discord"),
    @("Max Players", "Máximo de jugadores"),
    @("Max saved backups", "Máximo de copias de seguridad guardadas"),
    @("Mbps", "mbps"),
    @("Message shown after special readiness checks pass (optional)", "Mensaje que se muestra después de pasar las verificaciones de preparación especiales (opcional)"),
    @("MESSAGES SENT", "MENSAJES ENVIADOS"),
    @("Messages to send", "Mensajes para enviar"),
    @("Minecraft Runtime", "Tiempo de ejecución de Minecraft"),
    @("Minecraft Server Console", "Consola del servidor Minecraft"),
    @("Minimum system RAM in GB (0 means no minimum)", "RAM mínima del sistema en GB (0 significa que no hay mínimo)"),
    @("MINUTE", "MINUTO"),
    @("minutes", "minutos"),
    @("Mod & Plugin Manager", "Gestor de mods y complementos"),
    @("MONITOR", "MONITOREAR"),
    @("Monitor active server ports for incoming packet floods and notify on abnormal traffic bursts.", "Supervise los puertos activos del servidor para detectar inundaciones de paquetes entrantes y notifique sobre ráfagas de tráfico anormales."),
    @("Monitor and manage every game server from one workspace.", "Supervise y administre todos los servidores de juegos desde un solo espacio de trabajo."),
    @("Monitoring  — Not verified yet", "Monitoreo: aún no verificado"),
    @("My Dedicated Server", "Mi servidor dedicado"),
    @("N/A", "N/A"),
    @("Name this destination, paste its Discord webhook, and choose exactly which Synix events it receives.", "Nombra este destino, pega su webhook de Discord y elige exactamente qué eventos de Synix recibe."),
    @("Network", "Red"),
    @("Network & RCON", "Red y RCON"),
    @("NEW SERVER", "NUEVO SERVIDOR"),
    @("No Days Scheduled", "No hay días programados"),
    @("No extra arguments", "Sin argumentos adicionales"),
    @("No matching help articles", "No hay artículos de ayuda que coincidan"),
    @("No publish folder was detected.", "No se detectó ninguna carpeta de publicación."),
    @("No reliability test has been run yet.", "Aún no se ha realizado ninguna prueba de fiabilidad."),
    @("NO RESULTS", "SIN RESULTADOS"),
    @("No running server processes detected", "No se detectaron procesos de servidor en ejecución"),
    @("Not changed: game files, saved servers, port-only rules, custom install folders, and firewall rules outside C:\Synix\Games.", "No modificado: archivos de juegos, servidores guardados, reglas de solo puerto, carpetas de instalación personalizadas y reglas de firewall fuera de C:\Synix\Games."),
    @("Not Required", "No requerido"),
    @("Off", "Apagado"),
    @("Online Service Authentication", "Autenticación de servicios en línea"),
    @("Only a masked webhook identifier is shown. Open Server Settings to view or edit the saved destination.", "Solo se muestra un identificador de webhook enmascarado. Abra Configuración del servidor para ver o editar el destino guardado."),
    @("Only the value you change is replaced; comments, sections, nesting, quotes, spacing, and key order remain intact.", "Sólo se reemplaza el valor que cambia; los comentarios, las secciones, el anidamiento, las citas, el espaciado y el orden de las claves permanecen intactos."),
    @("Open Backup Folder", "Abrir carpeta de respaldo"),
    @("Open Config Editor", "Abrir editor de configuración"),
    @("Open Discord", "Abrir Discord"),
    @("Open Discord bug forum", "Abrir foro de errores de Discord"),
    @("Open GitHub", "Abrir GitHub"),
    @("Open Latest Game Log", "Abrir el último registro del juego"),
    @("Open PayPal Donation", "Abrir donación de PayPal"),
    @("Open Server Folder", "Abrir carpeta del servidor"),
    @("Open SteamCMD", "Abrir SteamCMD"),
    @("Open the game definition builder", "Abra el generador de definiciones de juegos"),
    @("Open the game verification queue", "Abre la cola de verificación del juego."),
    @("Open the long-duration reliability test", "Abra la prueba de confiabilidad de larga duración"),
    @("Open the native console when a game server starts. Disable this to run servers silently in the background.", "Abra la consola nativa cuando se inicie un servidor de juegos. Desactívelo para ejecutar servidores de forma silenciosa en segundo plano."),
    @("Open the PayPal donation page on your phone.", "Abra la página de donación de PayPal en su teléfono."),
    @("Open the Synix troubleshooter", "Abra el solucionador de problemas de Synix"),
    @("Optional", "Opcional"),
    @("Optional flags only — for example: -log, -nosteamclient, or -forceupdate", "Solo indicadores opcionales, por ejemplo: -log, -nosteamclient o -forceupdate"),
    @("Optional import files (relative paths, one per line)", "Archivos de importación opcionales (rutas relativas, una por línea)"),
    @("Optional RCON syntax — launch arguments must contain {rcon}", "Sintaxis RCON opcional: los argumentos de lanzamiento deben contener {rcon}"),
    @("ORIGINAL", "ORIGINALES"),
    @("Original formatting is protected", "El formato original está protegido."),
    @("Orphaned Firewall Rule Cleanup", "Limpieza de reglas de firewall huérfanas"),
    @("Overrides Synix's hide-console preference for servers managed through their own window.", "Anula la preferencia de ocultar consola de Synix para servidores administrados a través de su propia ventana."),
    @("Password (at least 8 characters)", "Contraseña (al menos 8 caracteres)"),
    @("Paths & Launch Details", "Rutas y detalles de lanzamiento"),
    @("PID", "PID"),
    @("PLAYER", "JUGADOR"),
    @("Player Management Center", "Centro de gestión de jugadores"),
    @("Players", "Jugadores"),
    @("Port", "Puerto"),
    @("Port availability is checked automatically against running processes and other Synix servers.", "La disponibilidad del puerto se verifica automáticamente frente a los procesos en ejecución y otros servidores Synix."),
    @("Portable Java", "Java portátil"),
    @("Preview", "Vista previa"),
    @("Privacy & Security", "Privacidad y seguridad"),
    @("Privacy mode", "Modo de privacidad"),
    @("Privacy Mode", "Modo de privacidad"),
    @("Privacy Mode masks this access credential. Enter a custom code, or leave it empty on first install to let Windrose generate one.", "El modo de privacidad enmascara esta credencial de acceso. Ingrese un código personalizado o déjelo vacío en la primera instalación para permitir que Windrose genere uno."),
    @("Problem summary", "Resumen del problema"),
    @("Process identity and live resource usage for every active game server.", "Procese la identidad y el uso de recursos en vivo para cada servidor de juegos activo."),
    @("PROGRESS", "PROGRESO"),
    @("Protect Synix Transfer", "Proteger la transferencia Synix"),
    @("Protected in Synix and hidden from its logs. Generated batch files include the usable token in readable text.", "Protegido en Synix y oculto de sus registros. Los archivos por lotes generados incluyen el token utilizable en texto legible."),
    @("Public IP: Fetching...", "IP pública: Obteniendo..."),
    @("Publish folder selected. Run the check when ready.", "Carpeta de publicación seleccionada. Ejecute la verificación cuando esté listo."),
    @("Published Synix folder", "Carpeta Synix publicada"),
    @("PVE", "PvE"),
    @("Query", "Consulta"),
    @("Query port", "Puerto de consulta"),
    @("Query Port", "Puerto de consulta"),
    @("Quick Commands — choose one to prepare it, then review and send it", "Comandos rápidos: elija uno para prepararlo, luego revíselo y envíelo"),
    @("Quick event selection", "Selección rápida de eventos"),
    @("RAM Usage", "Uso de RAM"),
    @("RAM USAGE", "USO DE RAM"),
    @("Raw Preview", "Vista previa sin procesar"),
    @("RCON", "RCON"),
    @("RCON Password", "Contraseña RCON"),
    @("RCON Port", "Puerto RCON"),
    @("Read-only values can be selected and copied for diagnostics", "Se pueden seleccionar y copiar valores de solo lectura para diagnóstico"),
    @("Read-only verification of template structure, revision, and values saved in Server Settings. Password values are never displayed.", "Verificación de solo lectura de la estructura de la plantilla, la revisión y los valores guardados en la Configuración del servidor. Los valores de contraseña nunca se muestran."),
    @("Reading and testing the project game-definition library...", "Leyendo y probando la biblioteca de definición de juegos del proyecto..."),
    @("Ready — Windows requests administrator permission only if rules need removal.", "Listo: Windows solicita permiso de administrador solo si es necesario eliminar las reglas."),
    @("Ready for the first start", "Listo para el primer comienzo"),
    @("Ready to check the published files and the test receipt created during Publish.", "Listo para verificar los archivos publicados y el recibo de prueba creado durante la Publicación."),
    @("Ready to check this computer.", "Listo para revisar esta computadora."),
    @("Ready to manage", "Listo para administrar"),
    @("Ready to test the built-in game-definition library.", "Listo para probar la biblioteca de definiciones de juegos incorporada."),
    @("Ready. A 30-minute run with 30-second samples is recommended for a quick check.", "Listo. Se recomienda un análisis de 30 minutos con muestras de 30 segundos para una comprobación rápida."),
    @("Record Verification", "Verificación de registros"),
    @("Refresh", "Actualizar"),
    @("Refresh Players", "Actualizar jugadores"),
    @("Refresh to load player details directly from the local server.", "Actualice para cargar los detalles del jugador directamente desde el servidor local."),
    @("Register Server", "Registrar servidor"),
    @("Release check canceled.", "Cheque de liberación cancelado."),
    @("Release highlights", "Aspectos destacados del lanzamiento"),
    @("Release notes will appear here.", "Las notas de la versión aparecerán aquí."),
    @("Release Readiness Checker", "Comprobador de preparación para la versión"),
    @("Release report copied to the clipboard.", "Informe de lanzamiento copiado al portapapeles."),
    @("Reliability Test", "Prueba de confiabilidad"),
    @("Reliability test cancelled. No server settings were changed.", "Prueba de fiabilidad cancelada. No se cambió ninguna configuración del servidor."),
    @("Remind Me Later", "Recuérdamelo más tarde"),
    @("Remote Administration", "Administración remota"),
    @("Remove", "Quitar"),
    @("Remove Rules", "Eliminar reglas"),
    @("Remove selected", "Eliminar seleccionado"),
    @("Repair available", "Reparación disponible"),
    @("Repairing SteamCMD...", "Reparando SteamCMD..."),
    @("Repeatedly samples Synix memory, handles, threads, and the read-only server health checks. It does not start, stop, install, update, or alter a server.", "Muestra repetidamente la memoria, los identificadores, los subprocesos y las comprobaciones de estado del servidor de solo lectura de Synix. No inicia, detiene, instala, actualiza ni modifica un servidor."),
    @("Report a Problem", "Informar de un problema"),
    @("Require a visible server manager window", "Requerir una ventana visible del administrador del servidor"),
    @("Require an AVX2-capable processor", "Requiere un procesador compatible con AVX2"),
    @("Require hardware virtualization", "Requerir virtualización de hardware"),
    @("Require Microsoft Hyper-V", "Requerir Microsoft Hyper-V"),
    @("Require the server manager window to remain visible", "Requerir que la ventana del administrador del servidor permanezca visible"),
    @("Require Visual C++ 2013 x64 runtime", "Requiere tiempo de ejecución de Visual C++ 2013 x64"),
    @("Require Visual C++ 2015-2022 x64 runtime", "Requiere tiempo de ejecución de Visual C++ 2015-2022 x64"),
    @("Require Windows Professional or higher", "Requiere Windows Professional o superior"),
    @("Required fields update automatically for the selected game.", "Los campos obligatorios se actualizan automáticamente para el juego seleccionado."),
    @("Required files and Synix-created templates automatically enable a warning.", "Los archivos necesarios y las plantillas creadas por Synix activan automáticamente una advertencia."),
    @("Required for features such as Hyper-V that are unavailable on Windows Home.", "Requerido para funciones como Hyper-V que no están disponibles en Windows Home."),
    @("Required startup arguments are dynamically injected with your specific data before initialization. You may include any additional command-line flags not covered by the default string in the Extra Arguments section.", "Los argumentos de inicio requeridos se inyectan dinámicamente con sus datos específicos antes de la inicialización. Puede incluir cualquier indicador de línea de comando adicional que no esté cubierto por la cadena predeterminada en la sección Argumentos adicionales."),
    @("Required user-supplied files (relative paths, one per line)", "Archivos requeridos proporcionados por el usuario (rutas relativas, una por línea)"),
    @("Resolved automatically", "Resuelto automáticamente"),
    @("Resolving...", "Resolviendo..."),
    @("Resource Monitor", "Monitor de recursos"),
    @("Resource sampling was delayed  •  Retrying automatically", "El muestreo de recursos se retrasó • Reintentar automáticamente"),
    @("Restart days", "Días de reinicio"),
    @("Restart hour using a 24-hour clock", "Reiniciar la hora usando un reloj de 24 horas"),
    @("Restart minute", "Reiniciar minuto"),
    @("Restart selected days at a configured time while preserving the current scheduler data.", "Reinicie los días seleccionados a una hora configurada conservando los datos actuales del programador."),
    @("Restart time", "Hora de reinicio"),
    @("Restore Backup", "Restaurar copia de seguridad"),
    @("Restore Server Backup", "Restaurar copia de seguridad del servidor"),
    @("RESULT", "RESULTADO"),
    @("Review how Synix builds the command used to start this server.", "Revise cómo Synix crea el comando utilizado para iniciar este servidor."),
    @("Review orphaned firewall rules", "Revisar las reglas de firewall huérfanas"),
    @("Review the highlighted requirement", "Revise el requisito resaltado"),
    @("Review the license terms before allowing the first server launch.", "Revise los términos de la licencia antes de permitir el primer inicio del servidor."),
    @("Review these setup requirements before continuing.", "Revise estos requisitos de configuración antes de continuar."),
    @("Run All Checks", "Ejecutar todas las comprobaciones"),
    @("Run Health Check", "Ejecutar verificación de estado"),
    @("Run Release Check", "Ejecutar verificación de liberación"),
    @("Run Tests", "Ejecutar pruebas"),
    @("Running Now", "En ejecución"),
    @("Running package structure, SHA-256, and antivirus checks…", "Ejecutando comprobaciones de estructura de paquetes, SHA-256 y antivirus..."),
    @("Running Servers", "Servidores en ejecución"),
    @("Runtime requirements", "Requisitos de tiempo de ejecución"),
    @("SAFE ACTION", "ACCIÓN SEGURA"),
    @("Sample every", "Muestra cada"),
    @("Sanitized arguments (no secrets)", "Argumentos desinfectados (sin secretos)"),
    @("Save", "Guardar"),
    @("Save Changes", "Guardar cambios"),
    @("Save Destination", "Guardar destino"),
    @("Save Ordered IDs", "Guardar ID ordenados"),
    @("Save Server", "Guardar servidor"),
    @("Save to Project", "Guardar en proyecto"),
    @("SCAN TO SUPPORT SYNIX", "ESCANEAR PARA APOYAR SYNIX"),
    @("Scheduled Restarts", "Reinicios programados"),
    @("SCORE", "PUNTUACIÓN"),
    @("SEARCH", "BUSCAR"),
    @("Search by game, executable, or support status…", "Busque por juego, ejecutable o estado de soporte..."),
    @("Search checks titles and article text", "La búsqueda comprueba títulos y texto de artículos."),
    @("Search game name...", "Buscar nombre del juego..."),
    @("Search game or server name...", "Buscar un juego o servidor…"),
    @("Search settings, paths, or values...", "Buscar configuraciones, rutas o valores..."),
    @("Search the full knowledge base or expand a category below.", "Busque en la base de conocimientos completa o expanda una categoría a continuación."),
    @("seconds", "segundos"),
    @("Security", "Seguridad"),
    @("Security review blocked the package. No files were changed.", "La revisión de seguridad bloqueó el paquete. No se cambiaron archivos."),
    @("See exactly what Synix can install, configure, monitor, and query before creating a server.", "Vea exactamente qué Synix puede instalar, configurar, monitorear y consultar antes de crear un servidor."),
    @("See which Discord destination receives each type of Synix notification for this server.", "Vea qué destino de Discord recibe cada tipo de notificación Synix para este servidor."),
    @("Select a backup to continue.", "Seleccione una copia de seguridad para continuar."),
    @("Select a game server", "Seleccione un servidor de juego"),
    @("Select a game to update its verification evidence.", "Selecciona un juego para actualizar su evidencia de verificación."),
    @("Select a Repair", "Seleccione una reparación"),
    @("Select an Action", "Seleccione una acción"),
    @("Select an installed server and validate its command.", "Seleccione un servidor instalado y valide su comando."),
    @("Selected source file", "Archivo fuente seleccionado"),
    @("Send Command", "Enviar comando"),
    @("Send status, backups, maintenance, and problems to different Discord channels.", "Envía estados, copias de seguridad, mantenimiento y problemas a diferentes canales de Discord."),
    @("Send Test", "Enviar prueba"),
    @("Send your report", "Envía tu informe"),
    @("Sending a safe test message...", "Enviando un mensaje de prueba seguro..."),
    @("Server", "Servidor"),
    @("SERVER / ITEM", "SERVIDOR / ARTÍCULO"),
    @("SERVER CONFIGURATION", "CONFIGURACIÓN DEL SERVIDOR"),
    @("Server Dashboard", "Panel de servidores"),
    @("Server Details", "Detalles del servidor"),
    @("Server executable (relative path)", "Ejecutable del servidor (ruta relativa)"),
    @("Server Folder", "Carpeta del servidor"),
    @("Server Framework", "Marco del servidor"),
    @("Server Identity", "Identidad del servidor"),
    @("Server Info", "Información del servidor"),
    @("Server lifecycle tracking", "Seguimiento del ciclo de vida del servidor"),
    @("Server list changed during sampling  •  Retrying automatically", "La lista de servidores cambió durante el muestreo • Reintentar automáticamente"),
    @("Server log locations (one relative path or wildcard pattern per line)", "Ubicaciones de registros del servidor (una ruta relativa o patrón comodín por línea)"),
    @("Server Name", "Nombre del servidor"),
    @("SERVER NAME", "NOMBRE DEL SERVIDOR"),
    @("Server Options  ▴", "Opciones de servidor ▴"),
    @("Server Overview", "Descripción general del servidor"),
    @("Server Password", "Contraseña del servidor"),
    @("Server RAM (GB)", "RAM del servidor (GB)"),
    @("Server Readiness Center", "Centro de preparación del servidor"),
    @("Server Setup", "Configuración del servidor"),
    @("SERVER STATUS", "ESTADO DEL SERVIDOR"),
    @("Server type", "Tipo de servidor"),
    @("serverconfig.xml", "servidorconfig.xml"),
    @("Servers online", "Servidores en línea"),
    @("Service Ports", "Puertos de servicio"),
    @("SETTING", "AJUSTE"),
    @("Settings", "Ajustes"),
    @("Short summary", "Breve resumen"),
    @("Show a first-start setup warning", "Mostrar una advertencia de configuración de primer inicio"),
    @("Show server console window", "Mostrar la ventana de la consola del servidor"),
    @("Show Server Console Window", "Mostrar ventana de consola del servidor"),
    @("Show Technical Details", "Mostrar detalles técnicos"),
    @("Showing 0 games", "Mostrando 0 juegos"),
    @("Shown for transparency so you can verify the startup command has no hidden arguments.", "Se muestra con fines transparentes para que pueda verificar que el comando de inicio no tenga argumentos ocultos."),
    @("Simple view", "Vista sencilla"),
    @("SIZE", "TAMAÑO"),
    @("START", "COMENZAR"),
    @("Start  — Not verified yet", "Inicio — Aún no verificado"),
    @("Start Server", "Iniciar servidor"),
    @("Start Test", "Iniciar prueba"),
    @("Start Using Synix", "Comience a usar Synix"),
    @("Starting the server through Synix. Waiting for its configured listener to respond...", "Iniciando el servidor a través de Synix. Esperando que su oyente configurado responda..."),
    @("Starts background monitoring when you sign in to Windows. Closing the Synix dashboard always exits Synix completely for the current session.", "Inicia la supervisión en segundo plano cuando inicia sesión en Windows. Al cerrar el panel de Synix, siempre se sale de Synix por completo para la sesión actual."),
    @("Startup argument template", "Plantilla de argumento de inicio"),
    @("Startup Tasks", "Tareas de inicio"),
    @("Status", "Estado"),
    @("STATUS", "ESTADO"),
    @("Steam account login required", "Se requiere iniciar sesión en la cuenta de Steam"),
    @("Steam account name", "Nombre de la cuenta de Steam"),
    @("Steam account required", "Se requiere cuenta de Steam"),
    @("Steam Account Required", "Se requiere cuenta de Steam"),
    @("Steam AppID", "ID de aplicación de Steam"),
    @("Steam runtime target directory (relative path)", "Directorio de destino del tiempo de ejecución de Steam (ruta relativa)"),
    @("SteamCMD app configuration (normally blank)", "Configuración de la aplicación SteamCMD (normalmente en blanco)"),
    @("SteamCMD Download Speed", "Velocidad de descarga de SteamCMD"),
    @("SteamCMD download speed in megabits per second", "Velocidad de descarga de SteamCMD en megabits por segundo"),
    @("SteamCMD download speed mode", "Modo de velocidad de descarga de SteamCMD"),
    @("STOP", "DETENER"),
    @("Stop  — Not verified yet", "Detener: aún no verificado"),
    @("Stop Server", "Detener servidor"),
    @("Stopped", "Detenido"),
    @("Stopping the test server through Synix...", "Deteniendo el servidor de prueba a través de Synix..."),
    @("Store automated and manual server backup archives in a custom folder.", "Almacene archivos de copia de seguridad del servidor automatizados y manuales en una carpeta personalizada."),
    @("Structured View", "Vista estructurada"),
    @("Submit problem report to GitHub", "Enviar informe de problema a GitHub"),
    @("Submit to GitHub", "Enviar a GitHub"),
    @("Switch the Synix dashboard between light and dark visual themes.", "Cambie el panel de Synix entre temas visuales claros y oscuros."),
    @("Synix", "Synix"),
    @("Synix Argument Test", "Prueba de argumento de Synix"),
    @("Synix background service", "Servicio en segundo plano Synix"),
    @("Synix Background Service", "Servicio en segundo plano Synix"),
    @("Synix builds the real command with this server's saved settings, hides every password, starts it normally, and waits for proof that the server accepted the launch.", "Synix crea el comando real con la configuración guardada de este servidor, oculta cada contraseña, lo inicia normalmente y espera pruebas de que el servidor aceptó el inicio."),
    @("Synix Configuration Application Check", "Verificación de la aplicación de configuración de Synix"),
    @("Synix Control Panel", "Panel de control Synix"),
    @("SYNIX CONTROL PANEL  •  version", "PANEL DE CONTROL SYNIX • versión"),
    @("Synix could not verify this loader combination from the official metadata service.", "Synix no pudo verificar esta combinación de cargador desde el servicio de metadatos oficial."),
    @("Synix does not open a public web-control port. Passwords stored by Synix are protected locally, and sensitive values are masked from its activity logs.", "Synix no abre un puerto público de control web. Las contraseñas almacenadas por Synix están protegidas localmente y los valores confidenciales están ocultos en sus registros de actividad."),
    @("Synix Game Definition Builder", "Generador de definiciones de juegos Synix"),
    @("Synix Game Definition Test Runner", "Ejecutor de pruebas de definición de juegos Synix"),
    @("Synix Game Verification Queue", "Cola de verificación de juegos Synix"),
    @("Synix Help Center", "Centro de ayuda de Synix"),
    @("Synix installs Microsoft's official Bedrock Dedicated Server. Java and Java mod loaders do not apply.", "Synix instala el servidor dedicado Bedrock oficial de Microsoft. Los cargadores de mods Java y Java no se aplican."),
    @("Synix installs the official Oxide runtime only. Plugins remain user-managed in the server's oxide\plugins folder.", "Synix instala únicamente el tiempo de ejecución oficial de Oxide. Los complementos permanecen administrados por el usuario en la carpeta oxide\plugins del servidor."),
    @("Synix installs the selected server loader and matching portable Java. Add your own mods after installation.", "Synix instala el cargador de servidor seleccionado y el Java portátil correspondiente. Añade tus propios mods después de la instalación."),
    @("Synix is designed to make personal game-server hosting understandable without hiding what it changes on your computer.", "Synix está diseñado para hacer comprensible el alojamiento de servidores de juegos personales sin ocultar los cambios en su computadora."),
    @("SYNIX KNOWLEDGE BASE", "BASE DE CONOCIMIENTOS SYNIX"),
    @("Synix logo", "Logotipo de Synix"),
    @("Synix Release Readiness Checker", "Comprobador de preparación de lanzamiento de Synix"),
    @("Synix Reliability Test", "Prueba de confiabilidad de Synix"),
    @("Synix Settings", "Configuración de Synix"),
    @("Synix Troubleshooter", "Solucionador de problemas de Synix"),
    @("Synix Update", "Actualización de Synix"),
    @("Synix update is available", "La actualización de Synix está disponible"),
    @("Synix verifies backups with integrity receipts, safely stages the selected archive, and automatically rolls back if restoration fails. The saved Synix server entry and its settings are not changed.", "Synix verifica las copias de seguridad con recibos de integridad, organiza de forma segura el archivo seleccionado y las revierte automáticamente si falla la restauración. La entrada guardada del servidor Synix y su configuración no se modifican."),
    @("Synix verifies each action automatically after it succeeds on this PC.", "Synix verifica cada acción automáticamente después de que se realiza correctamente en esta PC."),
    @("Synix version:", "Versión Synix:"),
    @("System & Server Troubleshooter", "Solucionador de problemas de sistemas y servidores"),
    @("Tell us what you clicked, what Synix displayed, and how to make it happen again.", "Cuéntenos en qué hizo clic, qué mostró Synix y cómo hacer que vuelva a suceder."),
    @("Template revision", "Revisión de plantilla"),
    @("Test built-in game definitions", "Probar definiciones de juegos integradas"),
    @("Test duration", "Duración de la prueba"),
    @("Test LAN Connectivity", "Probar la conectividad LAN"),
    @("Test WAN Connectivity", "Pruebe la conectividad WAN"),
    @("Testing every built-in definition and template safely...", "Probando de forma segura cada definición y plantilla integradas..."),
    @("Tests every built-in game, managed setting binding, full configuration template, revision, path, log location, and allowlisted post-install action. Installed servers are never changed.", "Prueba cada juego integrado, enlace de configuración administrada, plantilla de configuración completa, revisión, ruta, ubicación de registro y acción posterior a la instalación incluida en la lista de permitidas. Los servidores instalados nunca se modifican."),
    @("The add-on was not installed.", "El complemento no se instaló."),
    @("The configuration report will appear here.", "El informe de configuración aparecerá aquí."),
    @("The game server process is not running", "El proceso del servidor del juego no se está ejecutando."),
    @("The game server process is online", "El proceso del servidor del juego está en línea."),
    @("The game-definition tests could not finish.", "Las pruebas de definición del juego no pudieron finalizar."),
    @("The local connection was removed. Revoke Synix on the GitHub page that opened.", "Se eliminó la conexión local. Revoque Synix en la página de GitHub que se abrió."),
    @("The passwords do not match.", "Las contraseñas no coinciden."),
    @("The privacy-filtered report was copied and is ready to paste into the Discord bug forum.", "El informe filtrado por privacidad se copió y está listo para pegarse en el foro de errores de Discord."),
    @("The readiness report will appear here.", "El informe de preparación aparecerá aquí."),
    @("The release check could not finish.", "La verificación de liberación no pudo finalizar."),
    @("The release check was canceled. No release files were changed.", "El control de liberación fue cancelado. No se modificaron archivos de versión."),
    @("The reliability report will appear after the requested run finishes.", "El informe de confiabilidad aparecerá una vez finalizada la ejecución solicitada."),
    @("The schedule is saved with this server's settings.", "La programación se guarda con la configuración de este servidor."),
    @("The selected server requires a Steam account for installation. Enter the account name that SteamCMD should use.", "El servidor seleccionado requiere una cuenta de Steam para su instalación. Ingrese el nombre de cuenta que SteamCMD debería usar."),
    @("The server must remain stopped during restoration", "El servidor debe permanecer detenido durante la restauración."),
    @("The server stopped before startup could be verified. Review its recent logs and definition.", "El servidor se detuvo antes de que se pudiera verificar el inicio. Revise sus registros recientes y su definición."),
    @("The test server is stopped. The completed argument evidence is preserved.", "El servidor de prueba está detenido. Se conserva la evidencia del argumento completo."),
    @("The validation report will appear here.", "El informe de validación aparecerá aquí."),
    @("These values are enabled only when the selected server template supports them.", "Estos valores se habilitan solo cuando la plantilla de servidor seleccionada los admite."),
    @("This informational view shows the default arguments Synix uses when building the start command.", "Esta vista informativa muestra los argumentos predeterminados que utiliza Synix al crear el comando de inicio."),
    @("TOTAL CPU", "CPU TOTAL"),
    @("TOTAL RAM", "RAM TOTAL"),
    @("Total system load", "Carga total del sistema"),
    @("Total system RAM in use", "RAM total del sistema en uso"),
    @("Turn on every day when the scheduled restart should run.", "Actívelo todos los días cuando deba ejecutarse el reinicio programado."),
    @("TYPE", "TIPO"),
    @("Unavailable", "No disponible"),
    @("Undo Edits", "Deshacer ediciones"),
    @("Update did not start  •  Current Synix was not changed", "La actualización no comenzó • Synix actual no fue modificado"),
    @("Update on Start", "Actualización al inicio"),
    @("Update safety information appears here.", "La información de seguridad actualizada aparece aquí."),
    @("Update Server", "Actualizar servidor"),
    @("Uptime", "tiempo de actividad"),
    @("Use at least 8 characters.", "Utilice al menos 8 caracteres."),
    @("Use full speed or limit game-server installs, updates, repairs, and validations.", "Utilice instalaciones, actualizaciones, reparaciones y validaciones de servidores de juegos a máxima velocidad o limite."),
    @("Use one webhook for this server and choose the messages it should receive.", "Utilice un webhook para este servidor y elija los mensajes que debe recibir."),
    @("Use only when testing proves the server needs the approved Steam DLL files. The target must stay inside the server folder.", "Úselo solo cuando las pruebas demuestren que el servidor necesita los archivos DLL de Steam aprobados. El objetivo debe permanecer dentro de la carpeta del servidor."),
    @("Use only when the server is deployed through Hyper-V or Windows containers.", "Úselo solo cuando el servidor se implemente a través de contenedores Hyper-V o Windows."),
    @("Use premade game configurations", "Utilice configuraciones de juego prefabricadas"),
    @("Use Synix default folder", "Usar la carpeta predeterminada de Synix"),
    @("Uses the computer's local time and a 24-hour clock.", "Utiliza la hora local de la computadora y un reloj de 24 horas."),
    @("Validate & Preview", "Validar y obtener vista previa"),
    @("Validate Command", "Validar comando"),
    @("Validate Game Files", "Validar archivos de juego"),
    @("Validated definition preview", "Vista previa de definición validada"),
    @("VALUE", "VALOR"),
    @("Verification queue refreshed from the saved Synix evidence.", "Cola de verificación actualizada a partir de la evidencia Synix guardada."),
    @("Verification step", "Paso de verificación"),
    @("Verified hardware and Windows requirements checked before Synix installs or launches the server.", "Requisitos verificados de hardware y Windows verificados antes de que Synix instale o inicie el servidor."),
    @("Verified update package details", "Detalles del paquete de actualización verificado"),
    @("Verify Backup", "Verificar copia de seguridad"),
    @("Verifying archive paths and SHA-256 integrity...", "Verificando rutas de archivo e integridad SHA-256..."),
    @("View Default Arguments", "Ver argumentos predeterminados"),
    @("View Details", "Ver detalles"),
    @("View Discord Webhooks", "Ver webhooks de Discord"),
    @("VIEWING HELP ARTICLE", "VER EL ARTÍCULO DE AYUDA"),
    @("Waiting for a configuration report.", "Esperando un informe de configuración."),
    @("Waiting for a running server process", "Esperando un proceso de servidor en ejecución"),
    @("Waiting for first sample  •  Auto-refresh every 1 second", "Esperando la primera muestra • Actualización automática cada 1 segundo"),
    @("WEBHOOK", "WEBHOOK"),
    @("Webhook secrets stay protected", "Los secretos del webhook permanecen protegidos"),
    @("WELCOME", "BIENVENIDO"),
    @("Welcome to Synix", "Bienvenido a Synix"),
    @("Welcome to the Synix Engine Knowledge Base. Select a topic from the navigation panel to begin.", "Bienvenido a la base de conocimientos del motor Synix. Seleccione un tema del panel de navegación para comenzar."),
    @("What happened", "que paso"),
    @("What happened?", "¿Qué pasó?"),
    @("WHAT HAPPENS AFTER YOU CONTINUE", "QUÉ PASA DESPUÉS DE CONTINUAR"),
    @("What should have happened?", "¿Qué debería haber pasado?"),
    @("What Synix currently knows how to install, configure, start, monitor, and query for this game.", "Lo que Synix sabe actualmente cómo instalar, configurar, iniciar, monitorear y consultar este juego."),
    @("What Synix was doing when the problem happened", "Qué estaba haciendo Synix cuando ocurrió el problema"),
    @("What were you doing?", "¿Qué estabas haciendo?"),
    @("When enabled, deleting a server requests administrator permission to remove its Windows Firewall rules. Turn this off to skip automatic cleanup during deletion.", "Cuando está habilitado, eliminar un servidor solicita permiso del administrador para eliminar sus reglas de Firewall de Windows. Desactive esta opción para omitir la limpieza automática durante la eliminación."),
    @("WHY SYNIX FLAGGED THESE RULES", "POR QUÉ SYNIX MARCÓ ESTAS REGLAS"),
    @("Window title", "Título de la ventana"),
    @("Windows requests administrator permission. Synix then removes only firewall rules matching the exact executable paths above and scans again to verify the cleanup.", "Windows solicita permiso de administrador. Luego, Synix elimina solo las reglas de firewall que coinciden con las rutas ejecutables exactas anteriores y escanea nuevamente para verificar la limpieza."),
    @("Windows version:", "Versión de Windows:"),
    @("Windrose Invite Access", "Acceso por invitación de Windrose"),
    @("World Generation", "Generación del mundo"),
    @("World Seed", "Semilla mundial"),
    @("World Size", "Tamaño mundial"),
    @("XML", "XML"),
    @("XML structure preserved", "Estructura XML preservada"),
    @("You will need this password when moving Synix to the new PC. It cannot be recovered.", "Necesitará esta contraseña cuando mueva Synix a la nueva PC. No se puede recuperar.")
)

$translations = [System.Collections.Generic.Dictionary[string,string]]::new(
    [System.StringComparer]::Ordinal)
foreach ($pair in $pairs) {
    $translations[$pair[0]] = $pair[1]
}
$operationalTranslations = & (Join-Path $PSScriptRoot 'OperationalTranslations.es.ps1')

$sourcePath = Join-Path $PSScriptRoot 'Strings.resx'
$targetPath = Join-Path $PSScriptRoot 'Strings.es.resx'
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

Write-Host "Created Strings.es.resx with $translatedStaticCount translated static texts and $($semanticTranslations.Count) semantic texts."
