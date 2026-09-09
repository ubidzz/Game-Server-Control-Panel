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
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixApp.SteamCMDHandler;
using System.Diagnostics;
using System.Text;

namespace Synix_Control_Panel.SynixEngine
{
	public partial class Core
	{
		public async Task<bool> StopServerAndReport(GameServer server, bool isManual = true)
		{
			using ServerOperationLease operation =
				ServerOperationCoordinator.TryBegin(server, ServerOperationKind.Stop);
			if (!operation.Acquired)
			{
				LogLocalized("ServerActions.Activity.StopBlocked", Color.Orange, true, operation.FailureReason);
				return false;
			}

			server.Status = StatusManager.GetStatus(ServerState.Stopping);
			Core.Instance.UpdateGridStatus();
			_ = SendDiscordNotification(
				server,
				DiscordNotificationEvent.ServerStopping,
				LocalizationManager.Get(isManual
					? "ServerActions.Notification.Stop.ManualTitle"
					: "ServerActions.Notification.Stop.AutomaticTitle"),
				isManual
					? LocalizationManager.Get("ServerActions.Notification.Stop.ManualBody")
					: LocalizationManager.Get("ServerActions.Notification.Stop.AutomaticBody"),
				Color.Orange);

			bool stopped = await Servers.Stop(server, (msg, logColor) =>
			{
				Log(msg, logColor);
			}, isManual);

			if (!stopped)
			{
				LogLocalized("ServerActions.Activity.StopFailed", Color.Red, true, server.ServerName);
			}
			else
			{
				RecordGameVerification(server.Game, GameVerificationKind.Stop);
				await CollectGeneratedConfigurationAfterStop(server);
				await SynchronizeFirstGeneratedConfiguration(server);
				_ = SendDiscordNotification(
					server,
					DiscordNotificationEvent.ServerStopped,
					LocalizationManager.Get("ServerActions.Notification.Stopped.Title"),
					LocalizationManager.Get("ServerActions.Notification.Stopped.Body", server.ServerName),
					Color.LimeGreen);
			}

			FileHandler.SaveServers();
			Core.Instance.UpdateGridStatus();
			return stopped;
		}

		internal async Task SynchronizeFirstGeneratedConfiguration(GameServer server)
		{
			ConfigurationApplyResult? optionalResult =
				await GameFix.ApplyFirstGeneratedConfiguration(server);
			if (!optionalResult.HasValue)
			{
				return;
			}
			ConfigurationApplyResult result = optionalResult.Value;

			if (!result.Succeeded)
			{
				LogLocalized("ServerActions.Activity.ConfigError", Color.Red, true, result.Message);
				_ = SendDiscordNotification(
					server,
					DiscordNotificationEvent.ConfigurationWarning,
					LocalizationManager.Get("ServerActions.Notification.ConfigurationError.Title"),
					result.Message,
					Color.Red);
				return;
			}

			if (!result.Complete)
			{
				LogLocalized("ServerActions.Activity.ConfigWarning", Color.Orange, true, result.Message);
				_ = SendDiscordNotification(
					server,
					DiscordNotificationEvent.ConfigurationWarning,
					LocalizationManager.Get("ServerActions.Notification.ConfigurationWarning.Title"),
					result.Message,
					Color.Orange);
				return;
			}

			if (result.Changed)
			{
				LogLocalized(
					"ServerActions.Activity.GeneratedConfigApplied",
					Color.LimeGreen,
					true,
					server.Game);
			}
		}

		private async Task CollectGeneratedConfigurationAfterStop(GameServer server)
		{
			if (!GeneratedConfigurationCollector.AutomaticCollectionEnabled)
			{
				return;
			}

			bool includeAllGeneratedFiles = !GameFix.ManagedConfigurationsEnabled;
			if (!includeAllGeneratedFiles &&
				GameFix.GetConfigFileCreationMode(server.Game) is
					ConfigFileCreationMode.SynixTemplate or
					ConfigFileCreationMode.LaunchArgumentsOnly)
			{
				return;
			}

			try
			{
				GeneratedConfigurationCaptureResult result = await Task.Run(() =>
					GeneratedConfigurationCollector.CollectServer(
						server,
						includeAllGeneratedFiles: includeAllGeneratedFiles));
				if (result.CopiedFiles > 0)
				{
					LogLocalized(
						"ServerActions.Activity.ConfigCaptureCopied",
						Color.Cyan,
						false,
						result.CopiedFiles,
						server.ServerName,
						result.DestinationRoot);
				}

				foreach (string error in result.Errors.Take(3))
				{
					LogLocalized("ServerActions.Activity.ConfigCaptureWarning", Color.OrangeRed, false, error);
				}
			}
			catch (Exception exception)
			{
				LogLocalized(
					"ServerActions.Activity.ConfigCaptureFailed",
					Color.OrangeRed,
					false,
					server.ServerName,
					exception.Message);
			}
		}

		public void OpenConfigEditor(GameServer server)
		{
			if (server == null) return;

			if (string.IsNullOrWhiteSpace(server.InstallPath))
			{
				LogLocalized("ServerActions.Activity.InstallPathMissing", Color.Red, true);
				return;
			}

			IReadOnlyList<ConfigurationEditorFile> configurationFiles;
			try
			{
				PrepareConfigurationEditorFiles(server);
				configurationFiles = ResolveConfigurationEditorFiles(server);
			}
			catch (Exception exception)
			{
				LogLocalized("ServerActions.Activity.ConfigPathUnsafe", Color.Red, true, exception.Message);
				return;
			}

			if (configurationFiles.Count == 0)
			{
				LogLocalized("ServerActions.Activity.ConfigPathUndefined", Color.Red, true);
				return;
			}

			if (configurationFiles.Any(file => File.Exists(file.Path)) ||
				GameFix.CanResetManagedConfiguration(server))
			{
				try
				{
					using ServerConfig editor = new(configurationFiles, server);
					editor.ShowDialog(ApplicationUiService.DialogOwner);
				}
				catch (Exception exception)
				{
					LogLocalized(
						"ServerActions.Activity.ConfigEditorFailed",
						Color.Red,
						true,
						exception.Message);
					PlainEnglishErrorDialog.ShowError(
						ApplicationUiService.DialogOwner,
						LocalizationManager.Get(
							"ServerActions.ErrorAction.OpenConfigEditor"),
						exception.ToString());
				}
			}
			else
			{
				string locations = string.Join(
					Environment.NewLine,
					configurationFiles.Select(file => file.Path));
				LogLocalized("ServerActions.Activity.ConfigFilesMissing", Color.Red, true, locations);
			}
		}

		internal static void PrepareConfigurationEditorFiles(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			if (GameFix.TryGetConfiguration(
				server.Game,
				out ConfigurationDefinition? definition) &&
				definition?.UsesConfigurationFile == true)
			{
				definition.PrepareConfigurationFilesForEditing(server);
			}
		}

		internal static IReadOnlyList<ConfigurationEditorFile>
			ResolveConfigurationEditorFiles(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			if (string.IsNullOrWhiteSpace(server.InstallPath))
				return [];

			Dictionary<string, ConfigurationEditorFile> files =
				new(StringComparer.OrdinalIgnoreCase);
			if (GameFix.TryGetConfiguration(
				server.Game,
				out ConfigurationDefinition? definition) &&
				definition?.UsesConfigurationFile == true)
			{
				foreach (string path in definition.ResolveConfigurationPaths(server))
					files[path] = new ConfigurationEditorFile(path, definition.Format);
			}

			GameInfo? blueprint = GameDatabase.GetGame(server.Game);
			if (blueprint != null && !string.IsNullOrWhiteSpace(blueprint.RelativeConfigPath))
			{
				string fullPath = ResolveConfigurationEditorPath(
					server,
					blueprint.RelativeConfigPath);
				files.TryAdd(
					fullPath,
					new ConfigurationEditorFile(fullPath, blueprint.Format));
			}

			AddSiblingConfigurationFiles(server, files);

			return files.Values.ToArray();
		}

		private static void AddSiblingConfigurationFiles(
			GameServer server,
			Dictionary<string, ConfigurationEditorFile> files)
		{
			foreach (ConfigurationEditorFile declaredFile in files.Values.ToArray())
			{
				string? directory = Path.GetDirectoryName(declaredFile.Path);
				if (string.IsNullOrWhiteSpace(directory) ||
					!Directory.Exists(directory) ||
					!IsDeclaredConfigurationDirectory(server, directory))
				{
					continue;
				}

				string declaredExtension = Path.GetExtension(declaredFile.Path);
				foreach (string path in Directory
					.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
					.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
				{
					string fileName = Path.GetFileName(path);
					if (files.ContainsKey(path) || IsEditorFileExcluded(fileName))
						continue;

					ConfigFormat format;
					if (Path.GetExtension(path).Equals(
						declaredExtension,
						StringComparison.OrdinalIgnoreCase))
					{
						format = declaredFile.Format;
					}
					else if (!ConfigHandler.TryGetFormatFromPath(path, out format))
					{
						continue;
					}

					files[path] = new ConfigurationEditorFile(path, format);
				}
			}
		}

		private static bool IsDeclaredConfigurationDirectory(
			GameServer server,
			string directory)
		{
			string installRoot = Path.GetFullPath(server.InstallPath)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string fullDirectory = Path.GetFullPath(directory)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (fullDirectory.Equals(installRoot, StringComparison.OrdinalIgnoreCase))
				return false;

			string relativeDirectory = Path.GetRelativePath(installRoot, fullDirectory);
			if (relativeDirectory.StartsWith("..", StringComparison.Ordinal) ||
				Path.IsPathRooted(relativeDirectory))
			{
				return false;
			}

			return relativeDirectory.Split(
				[Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
				StringSplitOptions.RemoveEmptyEntries)
				.Any(part =>
					part.Equals("cfg", StringComparison.OrdinalIgnoreCase) ||
					part.Contains("config", StringComparison.OrdinalIgnoreCase) ||
					part.Contains("setting", StringComparison.OrdinalIgnoreCase));
		}

		private static bool IsEditorFileExcluded(string fileName)
		{
			return fileName.StartsWith(".", StringComparison.Ordinal) ||
				fileName.Contains(".synix.", StringComparison.OrdinalIgnoreCase) ||
				fileName.EndsWith(".template", StringComparison.OrdinalIgnoreCase) ||
				fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
				fileName.EndsWith(".backup", StringComparison.OrdinalIgnoreCase);
		}

		internal static bool CanOpenConfigurationEditor(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			try
			{
				IReadOnlyList<ConfigurationEditorFile> files =
					ResolveConfigurationEditorFiles(server);
				return files.Count > 0 &&
					(files.Any(file => File.Exists(file.Path)) ||
					 GameFix.CanResetManagedConfiguration(server));
			}
			catch (Exception exception)
			{
				ApplicationLogService.WriteSuppressedException(exception);
				return false;
			}
		}

		private static string ResolveConfigurationEditorPath(
			GameServer server,
			string relativePathTemplate)
		{
			string cleanIdentity = Instance.GetSafeName(server.ServerName);
			string resolvedRelativePath = relativePathTemplate
				.Replace("{Identity}", cleanIdentity, StringComparison.Ordinal)
				.Replace("{ServerName}", cleanIdentity, StringComparison.Ordinal)
				.Replace("{map}", server.WorldName ?? string.Empty, StringComparison.Ordinal)
				.Replace("{port}", server.Port.ToString(), StringComparison.Ordinal)
				.Replace("{query}", server.QueryPort.ToString(), StringComparison.Ordinal)
				.Replace('/', Path.DirectorySeparatorChar)
				.Replace('\\', Path.DirectorySeparatorChar);
			string installRoot = Path.GetFullPath(server.InstallPath)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string fullPath = Path.GetFullPath(Path.Combine(installRoot, resolvedRelativePath));
			if (!fullPath.StartsWith(
				installRoot + Path.DirectorySeparatorChar,
				StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException(
					LocalizationManager.Get("ServerActions.Error.ConfigPathOutsideInstall"));
			}

			return fullPath;
		}

		public void OpenServerFolder(GameServer server)
		{
			if (Directory.Exists(server.InstallPath))
			{
				Process.Start("explorer.exe", $"\"{server.InstallPath}\"");
			}
			else
			{
				LogLocalized("ServerActions.Activity.FolderMissing", Color.Red, true, server.InstallPath);
			}
		}

		public async Task<bool> DeleteServerAndReportAsync(GameServer server)
		{
			using ServerOperationLease operation =
				ServerOperationCoordinator.TryBegin(server, ServerOperationKind.Delete);
			if (!operation.Acquired)
			{
				LogLocalized("ServerActions.Activity.DeleteBlocked", Color.Orange, true, operation.FailureReason);
				return false;
			}

			string status = server.Status ?? string.Empty;
			if (status == StatusManager.GetStatus(ServerState.Installing) ||
				status == StatusManager.GetStatus(ServerState.Updating) ||
				(server.PID.HasValue && server.PID > 0))
			{
				LogLocalized("ServerActions.Activity.DeleteActiveBlocked", Color.Red, true);
				return false;
			}

			var page = new TaskDialogPage()
			{
				Caption = LocalizationManager.Get(
					"ServerActions.Delete.ConfirmTitle"),
				Heading = LocalizationManager.Get(
					"ServerActions.Delete.ConfirmHeading",
					server.ServerName),
				Text = LocalizationManager.Get(
					"ServerActions.Delete.ConfirmBody",
					server.InstallPath),
				Icon = TaskDialogIcon.Warning,
				Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
				Verification = new TaskDialogVerificationCheckBox()
				{
					Text = LocalizationManager.Get(
						"ServerActions.Delete.IncludeBackups")
				}
			};

			IWin32Window? dialogOwner = ApplicationUiService.DialogOwner;
			TaskDialogButton result = dialogOwner == null
				? TaskDialog.ShowDialog(page)
				: TaskDialog.ShowDialog(dialogOwner, page);
			if (result != TaskDialogButton.Yes)
				return false;

			bool deleteBackups = page.Verification.Checked;
			string previousStatus = status;
			server.Status = StatusManager.GetStatus(ServerState.Deleting);
			UpdateGridStatus();

			try
			{
				// Validate before any firewall cleanup or other deletion side effects.
				FolderHandler.ServerFolder.ValidateDeletionTargets(server, deleteBackups);
				if (Properties.Settings.Default.enableRunAsAdmin)
				{
					GameInfo? definition = GameDatabase.GetGame(server.Game);
					string executableName = definition == null
						? string.Empty
						: MinecraftControlProfile.ResolveExecutableName(server, definition);
					string serverExePath = Path.Combine(server.InstallPath, executableName);

					if (File.Exists(serverExePath))
						await CleanFirewallRulesAsync(serverExePath);
				}

				ServerFolderDeletionResult deletion =
					await FolderHandler.ServerFolder.DeleteFilesAsync(server, deleteBackups);

				if (deletion.InstallationDeleted)
				{
					LogLocalized(
						"ServerActions.Activity.ServerDeleted",
						Color.Yellow,
						false,
						server.ServerName,
						deletion.InstallationPath);
				}
				if (deletion.BackupsDeleted)
					LogLocalized("ServerActions.Activity.BackupsDeleted", Color.LimeGreen, false, deletion.BackupPath);

				if (ServerRegistry.Servers.Contains(server))
					ServerRegistry.Servers.Remove(server);

				FileHandler.SaveServers();
				UpdateGridStatus();
				return true;
			}
			catch (Exception ex)
			{
				server.Status = previousStatus;
				LogLocalized("ServerActions.Activity.DeleteFailed", Color.Red, true, ex.Message);
				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"ServerActions.Delete.Error.Body",
						LocalizationManager.TranslateRuntimeText(ex.Message)),
					LocalizationManager.Get(
						"ServerActions.Delete.Error.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				UpdateGridStatus();
				return false;
			}
		}

		public async Task OpenBackFolderAsync(GameServer selectedServer)
		{
			IReadOnlyList<ServerBackupArchive> backups =
				await GetServerBackupsAsync(selectedServer);
			string fullPath = backups.Count > 0
				? Path.GetDirectoryName(backups[0].ArchivePath) ?? GetActiveServerBackupFolder(selectedServer)
				: GetActiveServerBackupFolder(selectedServer);

			if (Directory.Exists(fullPath))
			{
				Process.Start("explorer.exe", fullPath);
				LogLocalized("ServerActions.Activity.VaultOpening", Color.Cyan, false, selectedServer.ServerName);
			}
			else
			{
				LogLocalized("ServerActions.Activity.NoBackups", Color.Yellow, true, fullPath);
			}
		}

		public async Task UpdateServerAndReport(GameServer server, string serverProcess, bool autoRestart = false)
		{
			bool ServerUpdating = false;

			if (serverProcess == "UPDATE")
			{
				if (server.Status == StatusManager.GetStatus(ServerState.Running))
				{
					LocalizedMessageBox.Show(
						LocalizationManager.Get(
							"ServerActions.Update.StopFirst"),
						LocalizationManager.Get(
							"ServerActions.ServerActive.Title"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					return;
				}
				if (server.Status == StatusManager.GetStatus(ServerState.Updating) || server.Status == StatusManager.GetStatus(ServerState.Installing) || server.Status == StatusManager.GetStatus(ServerState.Validating) || isDownloadActive)
				{
					LogLocalized("ServerActions.Activity.DownloadBusy", Color.Orange);
					return;
				}

				ServerUpdating = true;
				if (!autoRestart)
				{
					var confirm = LocalizedMessageBox.Show(
						LocalizationManager.Get(
							"ServerActions.Update.Confirm",
							server.ServerName),
						LocalizationManager.Get(
							"ServerActions.Update.ConfirmTitle"),
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question);
					if (confirm != DialogResult.Yes) return;
				}
			}
			else if (serverProcess == "VALIDATE")
			{
				if (server.Status == StatusManager.GetStatus(ServerState.Running))
				{
					LocalizedMessageBox.Show(
						LocalizationManager.Get(
							"ServerActions.Validate.StopFirst"),
						LocalizationManager.Get(
							"ServerActions.ServerActive.Title"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					return;
				}

				if (server.Status == StatusManager.GetStatus(ServerState.Updating) || server.Status == StatusManager.GetStatus(ServerState.Installing) || server.Status == StatusManager.GetStatus(ServerState.Validating) || isDownloadActive)
				{
					LocalizedMessageBox.Show(
						LocalizationManager.Get(
							"ServerActions.ValidationBusy.Body"),
						LocalizationManager.Get(
							"ServerActions.SystemBusy.Title"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
					return;
				}

				var confirm = LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"ServerActions.Validate.Confirm",
						server.ServerName),
					LocalizationManager.Get(
						"ServerActions.Validate.ConfirmTitle"),
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question);
				if (confirm != DialogResult.Yes) return;
			}
			else
			{ return; }

			var gameData = GameDatabase.GetGame(server.Game);

			if (gameData == null || string.IsNullOrEmpty(gameData.AppID))
			{
				LogLocalized("ServerActions.Activity.GameDefinitionMissing", Color.Red, true, server.Game);
				return;
			}

			if (!EnsureSteamAccountName(server, gameData))
				return;

			ServerOperationKind operationKind = ServerUpdating
				? ServerOperationKind.Update
				: ServerOperationKind.Validate;
			using ServerOperationLease operation =
				ServerOperationCoordinator.TryBegin(server, operationKind);
			if (!operation.Acquired)
			{
				LogLocalized("ServerActions.Activity.SteamCmdBlocked", Color.Orange, true, operation.FailureReason);
				return;
			}
			DiscordNotificationEvent startedEvent = ServerUpdating
				? DiscordNotificationEvent.UpdateStarted
				: DiscordNotificationEvent.VerificationStarted;
			DiscordNotificationEvent completedEvent = ServerUpdating
				? DiscordNotificationEvent.UpdateCompleted
				: DiscordNotificationEvent.VerificationCompleted;
			DiscordNotificationEvent failedEvent = ServerUpdating
				? DiscordNotificationEvent.UpdateFailed
				: DiscordNotificationEvent.VerificationFailed;
			string operationName = LocalizationManager.Get(ServerUpdating
				? "ServerActions.Operation.Update"
				: "ServerActions.Operation.FileVerification");

			try
			{
				LogLocalized("SteamCmd.Activity.CloseDisabled", Color.Orange, true);
				isDownloadActive = true;
				string ManifestMessage = "";

				if (ServerUpdating)
				{
					LogLocalized("ServerActions.Activity.UpdateStarted", Color.White, true, server.Game);
					server.Status = StatusManager.GetStatus(ServerState.Updating);
					LogLocalized("ServerActions.Activity.FetchingManifest", Color.DeepSkyBlue, true);
					LogLocalized("ServerActions.Activity.UpdateWorking", Color.Gray);
					ManifestMessage = LocalizationManager.Get("ServerActions.Operation.UpdateLower");
				}
				else
				{
					LogLocalized("ServerActions.Activity.ValidationStarted", Color.White, true, server.Game);
					server.Status = StatusManager.GetStatus(ServerState.Validating);
					LogLocalized("ServerActions.Activity.AnalyzingFiles", Color.DeepSkyBlue, true);
					LogLocalized("ServerActions.Activity.ValidationWorking", Color.Gray);
					ManifestMessage = LocalizationManager.Get("ServerActions.Operation.ValidationLower");
				}

				Core.Instance.UpdateGridStatus();
				_ = SendDiscordNotification(
					server,
					startedEvent,
					LocalizationManager.Get("ServerActions.Notification.OperationStarted.Title", operationName),
					LocalizationManager.Get("ServerActions.Notification.OperationStarted.Body", server.ServerName),
					Color.Cyan);

				string steamAppsPath = Path.Combine(server.InstallPath, "steamapps");
				string manifestPath = Path.Combine(steamAppsPath, $"appmanifest_{gameData.AppID}.acf");

				if (File.Exists(manifestPath))
				{
					try
					{
						File.Delete(manifestPath);
						LogLocalized("ServerActions.Activity.ManifestCleared", Color.SeaGreen, false, ManifestMessage);
					}
					catch (Exception ex)
					{
						LogLocalized("ServerActions.Activity.ManifestClearFailed", Color.Red, true, ManifestMessage, ex.Message);
					}
				}

				int exitCode = await Task.Run(() =>
				{
					return ServerInstaller.Install(server, gameData,
						msg => Log(msg),
						pid =>
						{
							server.SteamPID = pid;
							FileHandler.SaveServers();
						});
				});

				if (exitCode != 0)
				{
					string errorDetail = ServerInstaller.GetSteamError(exitCode);
					LogLocalized("ServerActions.Activity.SteamFailed", Color.Red, true, errorDetail);
					LogLocalized("ServerActions.Activity.ExitCodeFailed", Color.Red, true, exitCode);
					_ = SendDiscordNotification(
						server,
						failedEvent,
						LocalizationManager.Get("ServerActions.Notification.OperationFailed.Title", operationName),
						errorDetail,
						Color.Red);
					isDownloadActive = false;
					LogLocalized("SteamCmd.Activity.CloseEnabled", Color.Orange, true);
					return;
				}

				bool fixApplied = await GameFix.PostInstall(server);
				if (fixApplied)
					LogLocalized("ServerActions.Activity.RequiredFilesReapplied", Color.Green, false, server.Game);
				if (OxideRuntimeManager.RequiresVanillaRestore(server, gameData))
				{
					server.ServerFrameworkVersion = "Official";
					LogLocalized("ServerActions.Activity.OxideOfficialRestored", Color.LimeGreen, true);
				}
				if (OxideRuntimeManager.IsEnabled(server, gameData))
				{
					try
					{
						await OxideRuntimeManager.InstallOrUpdateAsync(
							server,
							gameData,
							(message, color) => Log(message, color, true));
					}
					catch (Exception exception)
					{
						LogLocalized("ServerActions.Activity.OxideError", Color.Red, true, exception.Message);
						if (!IsBackgroundServiceMode)
							LocalizedMessageBox.Show(
								LocalizationManager.Get(
									"ServerActions.Oxide.ReapplyFailed.Body",
									LocalizationManager.TranslateRuntimeText(
										ManifestMessage),
									LocalizationManager.TranslateRuntimeText(
										exception.Message)),
								LocalizationManager.Get(
									"ServerActions.Oxide.UpdateFailed.Title"),
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
						_ = SendDiscordNotification(
							server,
							failedEvent,
							LocalizationManager.Get("ServerActions.Notification.OperationFailed.Title", operationName),
							LocalizationManager.Get("ServerActions.Notification.OxideReapplyFailed.Body", exception.Message),
							Color.Red);
						return;
					}
				}

				if (ServerUpdating)
				{
					LogLocalized("ServerActions.Activity.UpdateFinished", Color.Green, true, server.Game);
				}
				else
				{
					LogLocalized("ServerActions.Activity.ValidationFinished", Color.Green, true, server.Game);
				}
				_ = SendDiscordNotification(
					server,
					completedEvent,
					LocalizationManager.Get("ServerActions.Notification.OperationCompleted.Title", operationName),
					LocalizationManager.Get("ServerActions.Notification.OperationCompleted.Body", server.ServerName),
					Color.LimeGreen);
				ManifestMessage = "";
			}
			catch (Exception exception)
			{
				LogLocalized("ServerActions.Activity.OperationError", Color.Red, true, operationName, exception.Message);
				_ = SendDiscordNotification(
					server,
					failedEvent,
					LocalizationManager.Get("ServerActions.Notification.OperationFailed.Title", operationName),
					exception.Message,
					Color.Red);
			}
			finally
			{
				server.Status = StatusManager.GetStatus(ServerState.Stopped);
				server.SteamPID = null;
				isDownloadActive = false;
				FileHandler.SaveServers();
				Core.Instance.UpdateGridStatus();
				LogLocalized("SteamCmd.Activity.CloseEnabled", Color.Orange, true);
			}
		}

		public async Task AddServerAndReport()
		{
			using (ServerSettingsGUI settingsForm = new())
			{
				if (settingsForm.ShowDialog() == DialogResult.OK && settingsForm.NewServer != null)
				{
					GameServer newServer = settingsForm.NewServer;
					var gameData = GameDatabase.GetGame(newServer.Game);
					GameFix.ManualConfigWasCreated = false;

					if (gameData == null || string.IsNullOrEmpty(gameData.AppID))
					{
						LogLocalized("ServerActions.Activity.InstallAppIdMissing", Color.Red, true);
						return;
					}

					using ServerOperationLease operation =
						ServerOperationCoordinator.TryBegin(
							newServer,
							ServerOperationKind.Install);
					if (!operation.Acquired)
					{
						LogLocalized("ServerActions.Activity.InstallBlocked", Color.Orange, true, operation.FailureReason);
						return;
					}

					try
					{
						isDownloadActive = true;
						LogLocalized("SteamCmd.Activity.CloseDisabled", Color.Orange, true);

						LogLocalized("ServerActions.Activity.InstallStarted", Color.LightCyan, true, newServer.Game);
						newServer.Status = StatusManager.GetStatus(ServerState.Installing);
						Core.Instance.UpdateGridStatus();
						_ = SendDiscordNotification(
							newServer,
							DiscordNotificationEvent.InstallStarted,
							LocalizationManager.Get("ServerActions.Notification.InstallStarted.Title"),
							LocalizationManager.Get("ServerActions.Notification.InstallStarted.Body", newServer.Game),
							Color.Cyan);

						int exitCode = await Task.Run(() =>
						{
							return ServerInstaller.Install(newServer, gameData,
								msg => Log(msg),
								pid =>
								{
									newServer.SteamPID = pid;
									FileHandler.SaveServers();
								});
						});

						if (exitCode != 0)
						{
							string errorMsg = ServerInstaller.GetSteamError(exitCode);
							LogLocalized("ServerActions.Activity.InstallFailed", Color.Red, true, errorMsg);
							newServer.Status = "Failed";
							_ = SendDiscordNotification(
								newServer,
								DiscordNotificationEvent.InstallFailed,
								LocalizationManager.Get("ServerActions.Notification.InstallFailed.Title"),
								errorMsg,
								Color.Red);
							return;
						}

						bool fixApplied = await GameFix.PostInstall(newServer);
						if (fixApplied)
							LogLocalized("ServerActions.Activity.MissingFilesReapplied", Color.Green, false, newServer.Game);
						if (OxideRuntimeManager.IsEnabled(newServer, gameData))
						{
							try
							{
								await OxideRuntimeManager.InstallOrUpdateAsync(
									newServer,
									gameData,
									(message, color) => Log(message, color, true));
							}
							catch (Exception exception)
							{
								LogLocalized("ServerActions.Activity.OxideError", Color.Red, true, exception.Message);
								LocalizedMessageBox.Show(
									LocalizationManager.Get(
										"ServerActions.Oxide.InstallFailed.Body",
										LocalizationManager.TranslateRuntimeText(
											exception.Message)),
									LocalizationManager.Get(
										"ServerActions.Oxide.InstallFailed.Title"),
									MessageBoxButtons.OK,
									MessageBoxIcon.Error);
								_ = SendDiscordNotification(
									newServer,
									DiscordNotificationEvent.InstallFailed,
									LocalizationManager.Get("ServerActions.Notification.InstallFailed.Title"),
									LocalizationManager.Get("ServerActions.Notification.OxideInstallFailed.Body", exception.Message),
									Color.Red);
								return;
							}
						}
						newServer.IsFirstBoot =
							fixApplied ||
							gameData.NeedsConfigWarning ||
							gameData.RequiredLaunchFiles.Length > 0;
						if (await RefreshServerIconAsync(newServer))
						{
							Core.Instance.UpdateGridStatus();
							LogLocalized("ServerActions.Activity.IconUpdated", Color.Cyan, false, newServer.Game);
						}
						LogLocalized("ServerActions.Activity.InstallFinished", Color.Green, true, newServer.Game);
						_ = SendDiscordNotification(
							newServer,
							DiscordNotificationEvent.InstallCompleted,
							LocalizationManager.Get("ServerActions.Notification.InstallCompleted.Title"),
							LocalizationManager.Get("ServerActions.Notification.InstallCompleted.Body", newServer.Game),
							Color.LimeGreen);
						RecordGameVerification(newServer.Game, GameVerificationKind.Install);
					}
					catch (Exception ex)
					{
						LogLocalized("ServerActions.Activity.InstallUnexpectedError", Color.Red, true, ex.Message);
						_ = SendDiscordNotification(
							newServer,
							DiscordNotificationEvent.InstallFailed,
							LocalizationManager.Get("ServerActions.Notification.InstallFailed.Title"),
							ex.Message,
							Color.Red);
					}
					finally
					{
						newServer.Status = StatusManager.GetStatus(ServerState.Stopped);
						newServer.SteamPID = null;
						isDownloadActive = false;
						FileHandler.SaveServers();
						Core.Instance.UpdateGridStatus();
						LogLocalized("SteamCmd.Activity.CloseEnabled", Color.Orange, true);
					}
				}
			}
		}

		private bool EnsureSteamAccountName(
			GameServer server,
			GameInfo blueprint,
			bool forcePrompt = false,
			bool restoringImportedServer = false)
		{
			if (!blueprint.RequiresSteamLogin ||
				(!forcePrompt &&
				 !string.IsNullOrWhiteSpace(server.SteamAccountName)))
			{
				return true;
			}
			if (IsBackgroundServiceMode)
			{
				LogLocalized("ServerActions.Activity.SteamAccountNeeded", Color.Orange, true, server.ServerName);
				return false;
			}

			using SteamAccountLoginDialog loginDialog = new(
				blueprint.Game,
				server.SteamAccountName,
				restoringImportedServer);
			if (loginDialog.ShowDialog(ApplicationUiService.DialogOwner) != DialogResult.OK)
			{
				LogLocalized(
					restoringImportedServer
						? "ServerActions.Activity.SteamAuthorizationCancelled"
						: "ServerActions.Activity.SteamLoginCancelled",
					Color.Orange,
					true);
				return false;
			}

			server.SteamAccountName = loginDialog.SteamAccountName;
			FileHandler.SaveServers();
			return true;
		}

		private async Task<bool> EnsureSteamAuthenticationAfterImport(
			GameServer server,
			string status)
		{
			if (!server.SteamAuthenticationRequired)
				return true;

			GameInfo? blueprint = GameDatabase.GetGame(server.Game);
			if (blueprint?.RequiresSteamLogin != true)
			{
				server.SteamAuthenticationRequired = false;
				FileHandler.SaveServers();
				return true;
			}

			if (status is "WATCHDOG" or "MAINTENANCE")
			{
				LogLocalized("ServerActions.Activity.ImportSteamAuthorizationNeeded", Color.Orange, true, server.ServerName);
				return false;
			}

			if (!EnsureSteamAccountName(
					server,
					blueprint,
					forcePrompt: true,
					restoringImportedServer: true))
			{
				return false;
			}

			if (!File.Exists(Core.SteamCmdExe))
			{
				await SteamCMD.EnsureSteamCMD((message, color) => Log(message, color));
				if (!File.Exists(Core.SteamCmdExe))
				{
					LogLocalized("ServerActions.Activity.SteamCmdPreparationFailed", Color.Red, true);
					return false;
				}
			}

			try
			{
				isDownloadActive = true;
				LogLocalized("ServerActions.Activity.SteamAuthorizationLocked", Color.Orange, true);

				int exitCode = await Task.Run(() =>
					ServerInstaller.AuthenticateSteamAccount(
						server,
						blueprint,
						message => Log(message),
						pid =>
						{
							server.SteamPID = pid;
							FileHandler.SaveServers();
						}));

				if (exitCode != 0)
				{
					LogLocalized(
						"ServerActions.Activity.SteamAuthorizationFailed",
						Color.Red,
						true,
						ServerInstaller.GetSteamError(exitCode));
					return false;
				}

				server.SteamAuthenticationRequired = false;
				FileHandler.SaveServers();
				LogLocalized("ServerActions.Activity.SteamAuthorizationSucceeded", Color.Green, true, server.ServerName);
				return true;
			}
			finally
			{
				server.SteamPID = null;
				isDownloadActive = false;
				FileHandler.SaveServers();
				Core.Instance.UpdateGridStatus();
				LogLocalized("ServerActions.Activity.SteamAuthorizationUnlocked", Color.Orange, true);
			}
		}

		internal static int MarkImportedSteamAuthenticationRequired(
			IEnumerable<GameServer> servers)
		{
			ArgumentNullException.ThrowIfNull(servers);

			int authenticationCount = 0;
			foreach (GameServer server in servers)
			{
				GameInfo? blueprint = GameDatabase.GetGame(server.Game);
				server.SteamAuthenticationRequired =
					blueprint?.RequiresSteamLogin == true;

				if (server.SteamAuthenticationRequired)
					authenticationCount++;
			}

			return authenticationCount;
		}

		public async Task EditServerAndReport(GameServer server)
		{
			if (server.Status == StatusManager.GetStatus(ServerState.Running) || (server.PID.HasValue && server.PID > 0))
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"ServerActions.Edit.StopFirst"),
					LocalizationManager.Get(
						"ServerActions.ServerActive.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			if (server.Status == StatusManager.GetStatus(ServerState.Installing) || server.Status == StatusManager.GetStatus(ServerState.Updating) || (server.SteamPID.HasValue && server.SteamPID > 0))
			{
				string currentAction = (server.Status == StatusManager.GetStatus(ServerState.Updating)) ? StatusManager.GetStatus(ServerState.Updating) : StatusManager.GetStatus(ServerState.Installing);

				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"ServerActions.Edit.Busy.Body",
						server.ServerName,
						LocalizationManager.TranslateRuntimeText(currentAction)),
					LocalizationManager.Get(
						"ServerActions.SystemBusy.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			string previousFramework = server.ServerFramework ?? "Vanilla";
			using (var editForm = new ServerSettingsGUI(server))
			{
				if (editForm.ShowDialog() == DialogResult.OK && editForm.NewServer != null)
				{
					GameServer updatedServer = editForm.NewServer;
					ConfigurationApplyResult configurationResult =
						await GameFix.ApplyManagedConfiguration(updatedServer);

					if (!configurationResult.Succeeded)
					{
						LogLocalized("ServerActions.Activity.ConfigError", Color.Red, true, configurationResult.Message);
					}
					else if (!configurationResult.Complete)
					{
						LogLocalized("ServerActions.Activity.ConfigWarning", Color.Orange, true, configurationResult.Message);
					}
					else if (configurationResult.Changed)
					{
						LogLocalized("ServerActions.Activity.ConfigChanged", Color.Green, false, configurationResult.Message);
					}

					GameInfo? definition = GameDatabase.GetGame(updatedServer.Game);
					if (definition != null &&
						OxideRuntimeManager.IsEnabled(updatedServer, definition) &&
						!previousFramework.Equals(
							OxideRuntimeManager.FrameworkName,
							StringComparison.OrdinalIgnoreCase))
					{
						try
						{
							isDownloadActive = true;
							await OxideRuntimeManager.InstallOrUpdateAsync(
								updatedServer,
								definition,
								(message, color) => Log(message, color, true));
						}
						catch (Exception exception)
						{
							updatedServer.ServerFramework = OxideRuntimeManager.VanillaFrameworkName;
							updatedServer.ServerFrameworkVersion = "Official";
							LogLocalized("ServerActions.Activity.OxideError", Color.Red, true, exception.Message);
							LocalizedMessageBox.Show(
								LocalizationManager.Get(
									"ServerActions.Oxide.VanillaFallback.Body",
									LocalizationManager.TranslateRuntimeText(
										exception.Message)),
								LocalizationManager.Get(
									"ServerActions.Oxide.InstallFailed.Title"),
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
						}
						finally
						{
							isDownloadActive = false;
						}
					}
					else if (previousFramework.Equals(
						OxideRuntimeManager.FrameworkName,
						StringComparison.OrdinalIgnoreCase) &&
						string.Equals(
							updatedServer.ServerFramework,
							OxideRuntimeManager.VanillaFrameworkName,
							StringComparison.OrdinalIgnoreCase))
					{
						updatedServer.ServerFrameworkVersion =
							OxideRuntimeManager.VanillaRestoreRequiredVersion;
						LogLocalized("ServerActions.Activity.OxideRestoreRequired", Color.Orange, true);
						LocalizedMessageBox.Show(
							LocalizationManager.Get(
								"ServerActions.Oxide.RestoreOfficial.Body"),
							LocalizationManager.Get(
								"ServerActions.Oxide.RestoreOfficial.Title"),
							MessageBoxButtons.OK,
							MessageBoxIcon.Information);
					}

					FileHandler.SaveServers();
					LogLocalized("ServerActions.Activity.SettingsSaved", Color.Green, false, updatedServer.ServerName);
					Core.Instance.UpdateGridStatus();
				}
			}
		}

		public async Task<bool> ExecuteStartSequence(GameServer server, string status = "")
		{
			ServerOperationKind operationKind = string.IsNullOrWhiteSpace(status)
				? ServerOperationKind.Start
				: ServerOperationKind.Restart;
			using ServerOperationLease operation =
				ServerOperationCoordinator.TryBegin(server, operationKind);
			if (!operation.Acquired)
			{
				LogLocalized("ServerActions.Activity.StartBlocked", Color.Orange, true, operation.FailureReason);
				return false;
			}

			bool isRestart = status.Equals("RESTART", StringComparison.OrdinalIgnoreCase);
			bool isMaintenance = status.Equals("MAINTENANCE", StringComparison.OrdinalIgnoreCase);
			bool isWatchdog = status.Equals("WATCHDOG", StringComparison.OrdinalIgnoreCase);
			bool requiresVerifiedStop = RequiresVerifiedStopBeforeStartValidation(status);
			StartContext currentContext = isMaintenance
				? StartContext.Scheduled
				: isWatchdog
					? StartContext.CrashRecovery
					: StartContext.Manual;
			try
			{
				if (!PassResourceGuard(out string guardMsg))
				{
					Log(guardMsg, System.Drawing.Color.Red, true);
					if (!IsBackgroundServiceMode)
						LocalizedMessageBox.Show(
							LocalizationManager.TranslateRuntimeText(guardMsg),
							LocalizationManager.Get(
								"ResourceGuard.Exhaustion.Title"),
							System.Windows.Forms.MessageBoxButtons.OK,
							System.Windows.Forms.MessageBoxIcon.Warning);
					return false;
				}

				if (!await EnsureSteamAuthenticationAfterImport(server, status))
					return false;

				bool showInteractiveErrors = string.IsNullOrWhiteSpace(status) || isRestart;

				if (isRestart)
				{
					LogLocalized("ServerActions.Activity.RestartSequence", Color.Cyan, false, server.ServerName);
				}
				else if (isMaintenance)
				{
					LogLocalized("ServerActions.Activity.MaintenanceRestart", Color.Cyan, true, server.ServerName);
				}
				else if (isWatchdog)
				{
					server.Status = StatusManager.GetStatus(ServerState.Crashed);
					string reason = LocalizationManager.Get(
						!server.RunningProcess?.Responding ?? false
							? "ServerActions.Watchdog.Freeze"
							: "ServerActions.Watchdog.CrashOrClose");
					LogLocalized("ServerActions.Activity.WatchdogRecovery", Color.Orange, false, reason, server.ServerName);

					_ = SendDiscordNotification(
						server,
						DiscordNotificationEvent.ServerCrashed,
						LocalizationManager.Get("ServerActions.Notification.CrashDetected.Title"),
						LocalizationManager.Get("ServerActions.Notification.CrashDetected.Body", server.ServerName),
						Color.Red);

					Core.Instance.UpdateGridStatus();
				}

				if (requiresVerifiedStop)
				{
					LogLocalized("ServerActions.Activity.VerifyingStop", Color.Cyan, true, server.ServerName);

					bool stopped = await StopServerAndReport(server, isManual: isRestart);
					if (!stopped)
					{
						LogLocalized("ServerActions.Activity.RestartBlocked", Color.Red, true, server.ServerName);
						return false;
					}
				}

				if (!ValidateIntegrityAndReport(server, showInteractiveErrors)) return false;
				SafetyChecklistReport safetyReport = UserGuidance.BuildSafetyChecklist(server);
				if (!safetyReport.CanContinue)
				{
					SafetyCheckItem blocked = safetyReport.Items.First(item =>
						item.Level == SafetyCheckLevel.Blocked);
					LogLocalized("ServerActions.Activity.SafetyBlocked", Color.Red, true, blocked.Name, blocked.Details);
					if (showInteractiveErrors)
					{
						PlainEnglishErrorDialog.ShowError(
							ApplicationUiService.DialogOwner,
							LocalizationManager.Get(
								"ServerActions.ErrorAction.StartSafely"),
							blocked.Details);
					}
					return false;
				}
				LogLocalized(
					"ServerActions.Activity.SafetyPassed",
					Color.LimeGreen,
					false,
					safetyReport.CompletionPercentage);
				if (GameFix.NeedsManagedConfiguration(server))
				{
					ConfigurationApplyResult configurationResult =
						await GameFix.ApplyManagedConfiguration(server);

					if (!configurationResult.Succeeded)
					{
						LogLocalized("ServerActions.Activity.ConfigError", Color.Red, true, configurationResult.Message);
						if (showInteractiveErrors)
							LocalizedMessageBox.Show(
								LocalizationManager.TranslateRuntimeText(
									configurationResult.Message),
								LocalizationManager.Get(
									"ServerActions.ConfigurationApplyFailed.Title"),
								MessageBoxButtons.OK,
								MessageBoxIcon.Error);
						return false;
					}

					if (!configurationResult.Complete)
					{
						LogLocalized("ServerActions.Activity.ConfigWarning", Color.Orange, true, configurationResult.Message);
					}
					else if (configurationResult.Changed)
					{
						LogLocalized("ServerActions.Activity.ConfigChanged", Color.Green, false, configurationResult.Message);
					}

					FileHandler.SaveServers();
				}
				bool displayedFirstBootWarning = server.IsFirstBoot;
				if (server.IsFirstBoot && !showInteractiveErrors)
				{
					LogLocalized("ServerActions.Activity.FirstStartRequired", Color.Orange, true, server.ServerName);
					return false;
				}
				if (ShouldBlockForConfig(server)) return false;
				if (!EnsureRequiredLaunchFilesAndReport(
					server,
					showDialog: showInteractiveErrors &&
						!displayedFirstBootWarning &&
						!status.Equals("WATCHDOG", StringComparison.OrdinalIgnoreCase)))
				{
					return false;
				}

				if (server.Status == StatusManager.GetStatus(ServerState.Stopped))
				{
					LogLocalized("ServerActions.Activity.Starting", Color.Cyan, true, server.ServerName);
					if (!PassSpamLock(server, out string lockMsg, "Start")) { Log(lockMsg, System.Drawing.Color.Orange); return false; }

					await Servers.Start(server, (message, color) => Log(message, color), currentContext);
				}
				else
				{
					if (server.Status != StatusManager.GetStatus(ServerState.Starting))
					{
						LogLocalized("ServerActions.Activity.RestartFailed", Color.Red, false, server.ServerName);
					}
				}
				return server.Status == StatusManager.GetStatus(ServerState.Starting) ||
					server.Status == StatusManager.GetStatus(ServerState.Running);
			}
			catch (Exception ex)
			{
				LogLocalized("ServerActions.Activity.SequenceFailed", Color.Red, true, server.ServerName, ex.Message);
				if (currentContext == StartContext.Manual && ApplicationUiService.IsAvailable)
				{
					ApplicationUiService.TryPost(() =>
						PlainEnglishErrorDialog.ShowError(
							ApplicationUiService.DialogOwner,
							LocalizationManager.Get(
								"ServerActions.ErrorAction.CompleteAction"),
							ex.ToString()));
				}
				return false;
			}
		}

		internal static bool RequiresVerifiedStopBeforeStartValidation(string? status) =>
			status?.Equals("RESTART", StringComparison.OrdinalIgnoreCase) == true ||
			status?.Equals("MAINTENANCE", StringComparison.OrdinalIgnoreCase) == true ||
			status?.Equals("WATCHDOG", StringComparison.OrdinalIgnoreCase) == true;

		public void RunUniversalHealthCheck()
		{
			foreach (var server in ServerRegistry.Snapshot())
			{
				if (server.Status == StatusManager.GetStatus(ServerState.Running))
				{
					if (server.RunningProcess == null || server.RunningProcess.HasExited)
					{
						_ = ExecuteStartSequence(server, "WATCHDOG");
						continue;
					}

					try
					{
						server.RunningProcess.Refresh();
						if (!server.RunningProcess.Responding)
						{
							_ = ExecuteStartSequence(server, "WATCHDOG");
						}
					}
					catch (Exception suppressedException)
					{
						Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
					}
				}
			}
		}

		public bool ExportServerToBatch(GameServer server)
		{
			if (server == null || string.IsNullOrWhiteSpace(server.InstallPath))
			{
				LogLocalized("ServerActions.Activity.ExportInvalidServer", Color.Red);
				return false;
			}

			var dbEntry = GameDatabase.GetGame(server.Game);
			if (dbEntry == null)
			{
				LogLocalized("ServerActions.Activity.ExportDefinitionMissing", Color.Red, false, server.Game);
				return false;
			}

			SynixServerPasswords batchPasswords;
			try
			{
				batchPasswords = Core
					.RevealServerPasswords(server);
			}
			catch (SynixPasswordProtectionException)
			{
				LogLocalized("ServerActions.Activity.ExportCredentialsLocked", Color.Red);
				return false;
			}

			if (!dbEntry.LaunchBehavior.AllowLaunchFileExport)
			{
				LogLocalized("ServerActions.Activity.ExportDisabled", Color.Orange, false, server.Game);
				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"ServerActions.Export.Disabled.Body",
						server.Game),
					LocalizationManager.Get(
						"ServerActions.Export.Disabled.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return false;
			}

			if (!GameServerInputValidator.TryValidate(
					dbEntry,
					server.ServerName,
					batchPasswords,
					out string credentialError))
			{
				LogLocalized("ServerActions.Activity.ExportCredentialBlocked", Color.Red, true, credentialError);
				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"ServerActions.Export.CredentialAttention.Body",
						LocalizationManager.TranslateRuntimeText(
							credentialError)),
					LocalizationManager.Get(
						"ServerActions.Export.CredentialAttention.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return false;
			}

			try
			{
				string fullExePath = GameLaunchCommandBuilder.ResolveExecutablePath(server, dbEntry);
				string invokedId = GameLaunchCommandBuilder.ResolveInvokedAppId(
					server,
					dbEntry,
					fullExePath);

				string cleanIdentity = GetSafeName(server.ServerName ?? "Server");
				if (!GameLaunchCommandBuilder.TryBuildArguments(
					server,
					dbEntry,
					invokedId,
					batchPasswords,
					out string args,
					out string argumentError,
					forBatchFile: true))
				{
					LogLocalized("ServerActions.Activity.ExportArgumentBlocked", Color.Red, true, argumentError);
					return false;
				}

				string safeArgs = EscapeWindowsBatchCommandLine(args);

				string binDir = Path.GetDirectoryName(fullExePath) ?? server.InstallPath;
				string exeNameOnly = Path.GetFileName(fullExePath);
				string safeIdentity = EscapeWindowsBatchCommandLine(cleanIdentity);
				string safeBinDir = EscapeWindowsBatchQuotedValue(binDir);
				string safeExeName = EscapeWindowsBatchQuotedValue(exeNameOnly);
				string safeInvokedId = EscapeWindowsBatchQuotedValue(invokedId);

				StringBuilder batchContent = new StringBuilder();
				batchContent.AppendLine("@echo off");
				batchContent.AppendLine("setlocal DisableDelayedExpansion");
				batchContent.AppendLine($"echo :: ===========================================================================");
				batchContent.AppendLine($"echo :: SYNIX AUTOMATICALLY GENERATED LAUNCH SCRIPT");
				batchContent.AppendLine($"echo :: Server: {safeIdentity}");
				batchContent.AppendLine($"echo :: Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				batchContent.AppendLine($"echo :: ===========================================================================");
				batchContent.AppendLine();
				batchContent.AppendLine($":: Move execution context to the actual binaries directory");
				batchContent.AppendLine($"cd /d \"{safeBinDir}\"");
				batchContent.AppendLine();
				batchContent.AppendLine($":: Inject Steam App Variables into Windows Memory");
				batchContent.AppendLine($"set \"SteamAppId={safeInvokedId}\"");
				batchContent.AppendLine($"set \"SteamGameId={safeInvokedId}\"");
				batchContent.AppendLine();
				batchContent.AppendLine($":: Execute the standalone server payload and instantly close this script window");
				batchContent.AppendLine($"start \"\" \"{safeExeName}\" {safeArgs}");
				batchContent.AppendLine("exit");

				string safeFileName = $"Run_{cleanIdentity}_Server.bat";
				string fullOutputPath = Path.Combine(server.InstallPath, safeFileName);

				File.WriteAllText(fullOutputPath, batchContent.ToString());

				LogLocalized("ServerActions.Activity.ExportSucceeded", Color.SpringGreen, false, fullOutputPath);
				return true;
			}
			catch (Exception ex)
			{
				LogLocalized("ServerActions.Activity.ExportFailed", Color.Red, true, ex.Message);
				return false;
			}
		}

		public async Task CleanFirewallRulesAsync(string executablePath)
		{
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo
				{
					FileName = "netsh",
					Arguments = $"advfirewall firewall delete rule name=all program=\"{executablePath}\"",
					UseShellExecute = true,
					Verb = "runas",
					WindowStyle = ProcessWindowStyle.Hidden
				};

				using Process? cleanup = Process.Start(psi);
				if (cleanup == null)
				{
					LogLocalized("ServerActions.Activity.FirewallStartFailed", Color.Orange, true, executablePath);
					return;
				}

				await cleanup.WaitForExitAsync();
				if (cleanup.ExitCode == 0)
					LogLocalized("ServerActions.Activity.FirewallRemoved", Color.LimeGreen, false, executablePath);
				else
					LogLocalized("ServerActions.Activity.FirewallExitCode", Color.Orange, true, cleanup.ExitCode, executablePath);
			}
			catch (System.ComponentModel.Win32Exception)
			{
				LogLocalized("ServerActions.Activity.FirewallAdminDenied", Color.Orange, true);
			}
			catch (Exception ex)
			{
				LogLocalized("ServerActions.Activity.FirewallError", Color.Red, true, ex.Message);
			}
		}
	}
}
