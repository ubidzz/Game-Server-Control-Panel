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
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using Synix_Control_Panel.SynixEngine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Synix_Control_Panel.SynixEngine.Core;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	public static partial class Servers
	{
		public static async Task Start(GameServer server, Action<string, Color> logCallback, StartContext context = StartContext.Manual)
		{
			ArgumentNullException.ThrowIfNull(logCallback);
			try
			{
				GameInfo? selectedDefinition = GameDatabase.GetGame(server.Game);
				if (selectedDefinition == null)
				{
					logCallback?.Invoke(
						LocalizationManager.Get("ServerStart.Activity.DefinitionMissing", server.Game),
						Color.Red);
					_ = Core.Instance.SendDiscordNotification(
						server,
						DiscordNotificationEvent.ConfigurationWarning,
						LocalizationManager.Get("ServerStart.Notification.ConfigurationError.Title"),
						LocalizationManager.Get("ServerStart.Notification.DefinitionMissing.Body", server.Game),
						Color.Red);
					return;
				}

				GamePrerequisiteReport prerequisites = await Task.Run(() =>
					GamePrerequisiteChecker.CheckCurrentSystem(
						selectedDefinition,
						server,
						port => Core.Instance.GetPortCollisionOwner(
							port,
							server,
							activeOnly: true),
						Core.Instance.IsPortInUseLocally));
				foreach (GamePrerequisiteItem warning in prerequisites.Items.Where(item =>
					item.State == GamePrerequisiteState.Warning))
				{
					logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.RequirementWarning", warning.Message), Color.Orange);
				}
				if (!prerequisites.CanStart)
				{
					string message = prerequisites.ToDisplayText();
					logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.Blocked", message), Color.Red);
					_ = Core.Instance.SendDiscordNotification(
						server,
						DiscordNotificationEvent.MonitoringWarning,
						LocalizationManager.Get("ServerStart.Notification.Blocked.Title"),
						message,
						Color.Red);
					if (context == StartContext.Manual)
					{
						LocalizedMessageBox.Show(
							LocalizationManager.TranslateRuntimeText(message),
							LocalizationManager.Get(
								"ServerStart.RequirementsNotMet.Title"),
							MessageBoxButtons.OK,
							MessageBoxIcon.Warning);
					}
					return;
				}

				SynixServerPasswords launchPasswords;
				try
				{
					launchPasswords = Core
						.RevealServerPasswords(server);
				}
				catch (SynixPasswordProtectionException)
				{
					logCallback?.Invoke(
						LocalizationManager.Get("ServerStart.Activity.CredentialsLocked"),
						Color.Red);
					return;
				}
				if (!GameServerInputValidator.TryValidate(
					selectedDefinition,
					server.ServerName,
					launchPasswords,
					out string serverInputError))
				{
					string message = LocalizationManager.Get(
						"ServerStart.CredentialAttention.Body",
						LocalizationManager.TranslateRuntimeText(serverInputError));
					logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.Blocked", message), Color.Red);
					if (context == StartContext.Manual && !Core.IsBackgroundServiceMode)
					{
						LocalizedMessageBox.Show(
							message,
							LocalizationManager.Get(
								"ServerStart.SettingsAttention.Title"),
							MessageBoxButtons.OK,
							MessageBoxIcon.Warning);
					}
					return;
				}

				bool isSystemSafe = await Task.Run(() => IsSystemSafeToStart());
				if (!isSystemSafe) return;

				if (!Core.Instance.PassResourceGuard(out string guardMsg))
				{
					logCallback?.Invoke(guardMsg, Color.Orange);
					if (context == StartContext.Manual && !Core.IsBackgroundServiceMode)
						LocalizedMessageBox.Show(
							LocalizationManager.TranslateRuntimeText(guardMsg),
							LocalizationManager.Get(
								"ResourceGuard.Exhaustion.Title"),
							MessageBoxButtons.OK,
							MessageBoxIcon.Warning);
					return;
				}

				bool maintenanceBackup = context == StartContext.Scheduled &&
					server.SmartMaintenanceEnabled &&
					server.MaintenanceBackupBeforeRestart;
				if ((server.BackupOnStart || maintenanceBackup) &&
					context != StartContext.CrashRecovery)
				{
					await Task.Run(() => Core.Instance.ExecuteBackup(server, context));
				}

				bool maintenanceUpdate = context == StartContext.Scheduled &&
					server.SmartMaintenanceEnabled &&
					server.MaintenanceUpdateBeforeRestart;
				if (server.UpdateOnStart || maintenanceUpdate)
				{
					await Task.Run(() => Core.Instance.UpdateServerAndReport(server, "UPDATE", true));
				}
				if (OxideRuntimeManager.IsEnabled(server, selectedDefinition) &&
					string.Equals(
						server.ServerFrameworkVersion,
						OxideRuntimeManager.FailedVersion,
						StringComparison.OrdinalIgnoreCase))
				{
					logCallback?.Invoke(
						LocalizationManager.Get("ServerStart.Activity.OxideInstallFailed"),
						Color.Red);
					return;
				}
				if (OxideRuntimeManager.RequiresVanillaRestore(server, selectedDefinition))
				{
					logCallback?.Invoke(
						LocalizationManager.Get("ServerStart.Activity.OxideRestoreRequired"),
						Color.Orange);
					return;
				}

				string launchPublicIp = string.Empty;
				if (selectedDefinition.RequiredArgs.Contains(
					"{PublicIP}",
					StringComparison.Ordinal))
				{
					launchPublicIp = (await Core.Instance.GetPublicIP()).Trim();
					if (string.IsNullOrWhiteSpace(launchPublicIp))
					{
						logCallback?.Invoke(
							LocalizationManager.Get("ServerStart.Activity.PublicAddressUnavailable"),
							Color.Orange);
					}
				}

				server.HasAnnouncedOnline = false;
				server.IsProbing = false;
				server.LastProbeTime = null;
				server.Status = StatusManager.GetStatus(ServerState.Starting);
				Core.Instance.UpdateGridStatus();

				ProcessStartInfo? psi = null;
				string finalArgs = "";
				bool isMinecraft = false;
				bool hideWindow = selectedDefinition != null &&
					GameLaunchCommandBuilder.ShouldHideServerWindow(
						selectedDefinition,
						Properties.Settings.Default.ShowServerWindow);

				await Task.Run(() =>
				{
					var dbEntry = GameDatabase.GetGame(server.Game);
					if (dbEntry == null)
					{
						logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.TemplateMissing"), Color.Red);
						return;
					}

					string fullExePath = GameLaunchCommandBuilder.ResolveExecutablePath(server, dbEntry);
					string binDir = Path.GetDirectoryName(fullExePath) ?? "";

					if (!File.Exists(fullExePath))
					{
						logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.ExecutableMissing", fullExePath), Color.Red);
						_ = Core.Instance.SendDiscordNotification(
							server,
							DiscordNotificationEvent.ConfigurationWarning,
							LocalizationManager.Get("ServerStart.Notification.FileMissing.Title"),
							LocalizationManager.Get("ServerStart.Notification.FileMissing.Body"),
							Color.Red);
						server.Status = StatusManager.GetStatus(ServerState.Stopped);
						Core.Instance.UpdateGridStatus();
						return;
					}

					isMinecraft = GameCapabilityResolver.UsesMinecraftLifecycle(server);
					if (MinecraftControlProfile.IsJava(server))
					{
						PrepareMinecraftLauncher(fullExePath, logCallback!);
					}

					string invokedId = GameLaunchCommandBuilder.ResolveInvokedAppId(
						server,
						dbEntry,
						fullExePath);

					string cleanIdentity = Core.Instance.GetSafeName(server.ServerName);
					if (!GameLaunchCommandBuilder.TryBuildArguments(
						server,
						dbEntry,
						invokedId,
						launchPasswords,
						launchPublicIp,
						out string args,
						out string argumentError))
					{
						logCallback?.Invoke(
							LocalizationManager.Get("ServerStart.Activity.SecurityBlocked", argumentError),
							Color.Red);
						_ = Core.Instance.SendDiscordNotification(
							server,
							DiscordNotificationEvent.SecurityWarning,
							LocalizationManager.Get("ServerStart.Notification.Unsafe.Title"),
							argumentError,
							Color.Red);
						server.Status = StatusManager.GetStatus(ServerState.Stopped);
						Core.Instance.UpdateGridStatus();
						return;
					}

					finalArgs = args;
					if (server.Game.Equals("Arma Reforger", StringComparison.OrdinalIgnoreCase))
					{
						string profilePath = Path.Combine(
							server.InstallPath,
							"profiles",
							cleanIdentity);
						Directory.CreateDirectory(profilePath);
						logCallback?.Invoke(
							LocalizationManager.Get("ServerStart.Activity.ArmaProfile", profilePath),
							Color.Cyan);
					}

					psi = GameLaunchCommandBuilder.CreateProcessStartInfo(
						fullExePath,
						finalArgs,
						binDir,
						dbEntry.LaunchBehavior.RunElevated,
						hideWindow,
						isMinecraft && hideWindow,
						isMinecraft && hideWindow);

					if (!psi.UseShellExecute)
					{
						psi.EnvironmentVariables["SteamAppId"] = invokedId;
						psi.EnvironmentVariables["SteamGameId"] = invokedId;
					}
				});

				if (psi == null) return;

				string safeLogArgs = LocalizationManager.Get("ServerStart.Arguments.Unavailable");
				if (selectedDefinition != null)
				{
					string fullExePath = GameLaunchCommandBuilder.ResolveExecutablePath(
						server,
						selectedDefinition);
					string invokedId = GameLaunchCommandBuilder.ResolveInvokedAppId(
						server,
						selectedDefinition,
						fullExePath);
					if (!GameLaunchCommandBuilder.TryBuildLogArguments(
						server,
						selectedDefinition,
						invokedId,
						launchPasswords,
						launchPublicIp,
						out safeLogArgs,
						out _))
					{
						safeLogArgs = LocalizationManager.Get("ServerStart.Arguments.SecurePreviewUnavailable");
					}
					else if (!string.IsNullOrWhiteSpace(launchPublicIp))
					{
						safeLogArgs = safeLogArgs.Replace(
							launchPublicIp,
							LocalizationManager.Get("ServerStart.Arguments.PublicIpRedacted"),
							StringComparison.Ordinal);
					}
				}

				logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.Arguments", safeLogArgs), Color.Cyan);

				Process? proc = Process.Start(psi);
				if (proc != null)
				{
					if (isMinecraft && psi.RedirectStandardInput)
					{
						proc.StandardInput.AutoFlush = true;
					}
					if (isMinecraft && psi.RedirectStandardOutput)
					{
						MinecraftConsoleHub.Attach(server, proc);
					}

					server.RunningProcess = proc;
					server.PID = proc.Id;
					server.LastProcessDiscoveryUtc = DateTime.MinValue;
					RefreshServerProcessRegistry(server, forceDiscovery: true);
					_ = CaptureSpawnedServerProcesses(server, logCallback!);
					if (hideWindow && !psi.UseShellExecute)
					{
						_ = HideServerWindowAfterLaunch(proc);
					}

					server.StartTime = DateTime.Now;

					_ = Core.Instance.SendDiscordNotification(
						server,
						DiscordNotificationEvent.ServerStarting,
						LocalizationManager.Get("ServerStart.Notification.Starting.Title"),
						LocalizationManager.Get("ServerStart.Notification.Starting.Body", server.ServerName),
						Color.Cyan);

					proc.EnableRaisingEvents = true;
					proc.Exited += async (s, e) =>
					{
						try
						{
							if (IsStoppingStatus(server.Status))
							{
								return;
							}

							if (ReconcileActiveServerProcesses(server, forceDiscovery: true))
							{
								logCallback?.Invoke(
									LocalizationManager.Get(
										"ServerStart.Activity.LauncherExitedProcessesRemain",
										server.ServerProcesses.Count,
										FormatProcessRegistry(server.ServerProcesses)),
									Color.Cyan);
								FileHandler.SaveServers();
								return;
							}

							if (server.Status == StatusManager.GetStatus(ServerState.Running))
							{
								await Core.Instance.ExecuteStartSequence(server, "WATCHDOG");
							}
							else
							{
								FinalizeStoppedState(server);
								await Core.Instance.SynchronizeFirstGeneratedConfiguration(server);
								FileHandler.SaveServers();
							}
						}
						catch (Exception ex)
						{
							logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.CrashHandlerError", ex.Message), Color.Red);
							FinalizeStoppedState(server);
						}
					};
					FileHandler.SaveServers();
				}
			}
			catch (Exception ex)
			{
			logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.CriticalError", ex.Message), Color.Red);
				_ = Core.Instance.SendDiscordNotification(
					server,
					DiscordNotificationEvent.MonitoringWarning,
					LocalizationManager.Get("ServerStart.Notification.Failed.Title"),
					ex.Message,
					Color.Red);
			}
		}

		private static void PrepareMinecraftLauncher(string launcherPath, Action<string, Color> logCallback)
		{
			try
			{
				string original = File.ReadAllText(launcherPath);
				string updated = original
					.Replace(" %* <NUL", " %*", StringComparison.OrdinalIgnoreCase)
					.Replace("if %errorlevel% neq 0 pause", "exit /b %errorlevel%", StringComparison.OrdinalIgnoreCase);

				if (!string.Equals(original, updated, StringComparison.Ordinal))
				{
					File.WriteAllText(launcherPath, updated);
					logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.MinecraftLauncherUpdated"), Color.Cyan);
				}
			}
			catch (Exception ex)
			{

				logCallback?.Invoke(LocalizationManager.Get("ServerStart.Activity.MinecraftLauncherUpdateFailed", ex.Message), Color.OrangeRed);
			}
		}


		private static Task HideServerWindowAfterLaunch(Process process)
		{
			return Task.Run(async () =>
			{
				DateTime? firstHiddenAt = null;
				for (int attempt = 0; attempt < 60; attempt++)
				{
					try
					{
						if (Properties.Settings.Default.ShowServerWindow || process.HasExited)
							return;

						if (await TryHideServerWindow(process).ConfigureAwait(false))
						{
							firstHiddenAt ??= DateTime.UtcNow;
						}

						if (firstHiddenAt.HasValue &&
							DateTime.UtcNow - firstHiddenAt.Value >= TimeSpan.FromSeconds(3))
						{
							return;
						}
					}
					catch (Exception exception)
					{
						ApplicationLogService.WriteSuppressedException(exception);
						return;
					}

					await Task.Delay(250).ConfigureAwait(false);
				}
			});
		}

		private static async Task<bool> TryHideServerWindow(Process process)
		{
			bool hidden = false;
			try
			{
				process.Refresh();
				IntPtr mainWindow = process.MainWindowHandle;
				if (mainWindow != IntPtr.Zero)
				{
					ShowWindowAsync(mainWindow, SW_HIDE);
					hidden = true;
				}
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}

			await _consoleLock.WaitAsync().ConfigureAwait(false);
			bool attached = false;
			try
			{
				attached = AttachConsole((uint)process.Id);
				if (!attached)
					return hidden;

				IntPtr consoleWindow = GetConsoleWindow();
				if (consoleWindow == IntPtr.Zero)
					return hidden;

				ShowWindowAsync(consoleWindow, SW_HIDE);
				return true;
			}
			finally
			{
				if (attached)
					FreeConsole();
				_consoleLock.Release();
			}
		}
	}
}
