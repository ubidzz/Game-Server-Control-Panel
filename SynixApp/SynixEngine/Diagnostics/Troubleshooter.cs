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
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixEngine
{
	internal enum SynixHealthLevel
	{
		Passed,
		Warning,
		Failed
	}

	internal enum SynixHealthAction
	{
		None,
		RepairSteamCmd,
		ValidateServerFiles,
		FixConfiguration,
		OpenServerFolder,
		OpenFirewallSettings,
		RecoverProcesses,
		OpenLatestLog,
		OpenUpdate
	}

	internal sealed record SynixHealthItem(
		SynixHealthLevel Level,
		string Area,
		string Subject,
		string Details,
		SynixHealthAction Action = SynixHealthAction.None,
		GameServer? Server = null)
	{
		internal string ResultText => Level switch
		{
			SynixHealthLevel.Passed => LocalizationManager.Get(
				"Diagnostics.Health.Result.Passed"),
			SynixHealthLevel.Warning => LocalizationManager.Get(
				"Diagnostics.Health.Result.Warning"),
			_ => LocalizationManager.Get(
				"Diagnostics.Health.Result.Failed")
		};
	}

	internal sealed class SynixHealthReport
	{
		internal SynixHealthReport(
			DateTimeOffset completedAtUtc,
			IReadOnlyList<SynixHealthItem> items)
		{
			CompletedAtUtc = completedAtUtc;
			Items = items;
		}

		internal DateTimeOffset CompletedAtUtc { get; }
		internal IReadOnlyList<SynixHealthItem> Items { get; }
		internal int PassedCount => Items.Count(item => item.Level == SynixHealthLevel.Passed);
		internal int WarningCount => Items.Count(item => item.Level == SynixHealthLevel.Warning);
		internal int FailedCount => Items.Count(item => item.Level == SynixHealthLevel.Failed);
		internal bool IsHealthy => FailedCount == 0;

		internal string ToPlainText(string? title = null)
		{
			StringBuilder text = new();
			text.AppendLine(title ?? LocalizationManager.Get(
				"Diagnostics.Health.Report.Title"));
			text.AppendLine();
			text.AppendLine(LocalizationManager.Get(
				"Diagnostics.Health.Report.Completed",
				CompletedAtUtc.ToLocalTime()));
			text.AppendLine(LocalizationManager.Get(
				"Diagnostics.Health.Report.Version",
				Core.GetCurrentVersion().ToString(3),
				Core.DetectCurrentInstallation().DisplayName));
			text.AppendLine(LocalizationManager.Get(
				"Diagnostics.Health.Report.Counts",
				PassedCount,
				WarningCount,
				FailedCount));
			text.AppendLine();
			foreach (SynixHealthItem item in Items)
			{
				text.AppendLine(LocalizationManager.Get(
					"Diagnostics.Health.Report.Item",
					item.ResultText.ToUpperInvariant(),
					item.Area,
					item.Subject));
				text.AppendLine(item.Details);
			}

			return text.ToString().TrimEnd();
		}
	}

	internal static class SynixTroubleshooter
	{
		private const long OneGibibyte = 1024L * 1024L * 1024L;
		private const int MaximumLogBytes = 512 * 1024;
		private static readonly Regex ErrorLinePattern = new(
			@"(?im)^.*(?:fatal error|unhandled exception|access violation|couldn['’]?t allocate|cannot bind|address already in use|server packages file not found|missing required|failed to load|redis.*(?:failed|missing|connection)|error\s*:).*$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant,
			TimeSpan.FromSeconds(2));

		internal static async Task<SynixHealthReport> RunAsync(
			IReadOnlyList<GameServer> servers,
			bool checkForUpdates,
			IProgress<string>? progress = null,
			CancellationToken cancellationToken = default,
			bool includeUpdateStatus = true)
		{
			ArgumentNullException.ThrowIfNull(servers);
			List<SynixHealthItem> items = [];
			progress?.Report(LocalizationManager.Get(
				"Diagnostics.Health.Progress.SharedRuntimes"));
			CheckSteamCmd(items);
			CheckJavaRuntimes(servers, items);
			CheckDiskSpace(servers, items);

			FirewallSnapshot firewall = WindowsFirewallInspector.Capture();
			if (!firewall.InspectionSucceeded)
			{
				items.Add(new SynixHealthItem(
					SynixHealthLevel.Warning,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Firewall"),
					LocalizationManager.Get(
						"Diagnostics.Health.Subject.FirewallInspection"),
					firewall.Problem ?? LocalizationManager.Get(
						"Diagnostics.Health.Firewall.InspectionFailed"),
					SynixHealthAction.OpenFirewallSettings));
			}
			else if (!firewall.Enabled)
			{
				items.Add(new SynixHealthItem(
					SynixHealthLevel.Warning,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Firewall"),
					LocalizationManager.Get(
						"Diagnostics.Health.Subject.FirewallProtection"),
					LocalizationManager.Get(
						"Diagnostics.Health.Firewall.Disabled"),
					SynixHealthAction.OpenFirewallSettings));
			}

			for (int index = 0; index < servers.Count; index++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				GameServer server = servers[index];
				progress?.Report(LocalizationManager.Get(
					"Diagnostics.Health.Progress.Server",
					server.ServerName,
					index + 1,
					servers.Count));
				GameInfo? game = GameDatabase.GetGame(server.Game);
				if (game == null)
				{
					items.Add(new SynixHealthItem(
						SynixHealthLevel.Failed,
						LocalizationManager.Get(
							"Diagnostics.Health.Area.ServerFiles"),
						server.ServerName,
						LocalizationManager.Get(
							"Diagnostics.Health.DefinitionMissing",
							server.Game),
						SynixHealthAction.OpenServerFolder,
						server));
					continue;
				}

				RunServerCheck(
					items,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.ServerFiles"),
					server,
					() => CheckServerFiles(server, game, items));
				RunServerCheck(
					items,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Runtimes"),
					server,
					() => CheckRuntimeRequirements(server, game, items));
				try
				{
					await CheckConfigurationAsync(server, items);
				}
				catch (Exception exception)
				{
					AddCheckFailure(
						items,
						LocalizationManager.Get(
							"Diagnostics.Health.Area.Configuration"),
						server,
						exception);
				}
				RunServerCheck(
					items,
					LocalizationManager.Get("Diagnostics.Health.Area.Ports"),
					server,
					() => CheckPorts(server, game, servers, items));
				RunServerCheck(
					items,
					LocalizationManager.Get("Diagnostics.Health.Area.Firewall"),
					server,
					() => CheckFirewall(server, game, firewall, items));
				RunServerCheck(
					items,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.ProcessTracking"),
					server,
					() => CheckProcessRecovery(server, items));
				RunServerCheck(
					items,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.RecentLogs"),
					server,
					() => CheckLatestLog(server, items));
			}

			if (includeUpdateStatus && checkForUpdates)
			{
				progress?.Report(LocalizationManager.Get(
					"Diagnostics.Health.Progress.Update"));
				await CheckUpdateAsync(items, cancellationToken);
			}
			else if (includeUpdateStatus)
			{
				items.Add(new SynixHealthItem(
					SynixHealthLevel.Passed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Update"),
					$"v{Core.GetCurrentVersion().ToString(3)}",
					LocalizationManager.Get(
						"Diagnostics.Health.Update.Skipped",
						Core.DetectCurrentInstallation().DisplayName)));
			}

			return new SynixHealthReport(DateTimeOffset.UtcNow, items);
		}

		private static void CheckSteamCmd(ICollection<SynixHealthItem> items)
		{
			bool executableExists = File.Exists(Core.SteamCmdExe);
			bool initialized = Directory.Exists(Path.Combine(Core.SteamCmdPath, "public"));
			items.Add(new SynixHealthItem(
				executableExists && initialized
					? SynixHealthLevel.Passed
					: SynixHealthLevel.Failed,
				LocalizationManager.Get(
					"Diagnostics.Health.Area.Runtimes"),
				"SteamCMD",
				executableExists && initialized
					? LocalizationManager.Get(
						"Diagnostics.Health.SteamCmd.Initialized",
						Core.SteamCmdPath)
					: LocalizationManager.Get(
						"Diagnostics.Health.SteamCmd.Missing"),
				executableExists && initialized
					? SynixHealthAction.None
					: SynixHealthAction.RepairSteamCmd));
		}

		private static void CheckJavaRuntimes(
			IEnumerable<GameServer> servers,
			ICollection<SynixHealthItem> items)
		{
			foreach (IGrouping<int, GameServer> requiredRuntime in servers
				.Where(server => server.RequiredJavaVersion > 0)
				.GroupBy(server => server.RequiredJavaVersion)
				.OrderBy(group => group.Key))
			{
				int version = requiredRuntime.Key;
				GameServer affectedServer = requiredRuntime.First();
				string directory = Path.Combine(Core.RuntimesPath, $"Java{version}");
				bool available = false;
				try
				{
					available = Directory.Exists(directory) &&
						Directory.EnumerateFiles(directory, "java.exe", SearchOption.AllDirectories).Any();
				}
				catch (Exception suppressedException)
				{
					Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
				}
				items.Add(new SynixHealthItem(
					available ? SynixHealthLevel.Passed : SynixHealthLevel.Failed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Runtimes"),
					LocalizationManager.Get(
						"Diagnostics.Health.Java.Subject",
						version),
					available
						? LocalizationManager.Get(
							"Diagnostics.Health.Java.Available",
							version)
						: LocalizationManager.Get(
							"Diagnostics.Health.Java.Missing",
							version),
					available ? SynixHealthAction.None : SynixHealthAction.ValidateServerFiles,
					affectedServer));
			}
		}

		private static void CheckServerFiles(
			GameServer server,
			GameInfo game,
			ICollection<SynixHealthItem> items)
		{
			if (string.IsNullOrWhiteSpace(server.InstallPath) ||
				!Directory.Exists(server.InstallPath))
			{
				items.Add(new SynixHealthItem(
					SynixHealthLevel.Failed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.ServerFiles"),
					server.ServerName,
					LocalizationManager.Get(
						"Diagnostics.Health.Files.FolderMissing"),
					SynixHealthAction.ValidateServerFiles,
					server));
				return;
			}

			string executable;
			try
			{
				executable = Path.GetFullPath(
					GameLaunchCommandBuilder.ResolveExecutablePath(server, game));
			}
			catch (Exception exception)
			{
				items.Add(new SynixHealthItem(
					SynixHealthLevel.Failed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.ServerFiles"),
					server.ServerName,
					LocalizationManager.Get(
						"Diagnostics.Health.Files.PathInvalid",
						exception.Message),
					SynixHealthAction.OpenServerFolder,
					server));
				return;
			}
			List<string> missing = [];
			if (!File.Exists(executable))
				missing.Add(Path.GetFileName(executable));
			foreach (string relativePath in game.RequiredLaunchFiles)
			{
				try
				{
					if (!File.Exists(Path.GetFullPath(Path.Combine(server.InstallPath, relativePath))))
						missing.Add(relativePath);
				}
				catch
				{
					missing.Add(relativePath);
				}
			}

			items.Add(new SynixHealthItem(
				missing.Count == 0 ? SynixHealthLevel.Passed : SynixHealthLevel.Failed,
				LocalizationManager.Get(
					"Diagnostics.Health.Area.ServerFiles"),
				server.ServerName,
				missing.Count == 0
					? LocalizationManager.Get(
						"Diagnostics.Health.Files.Complete")
					: LocalizationManager.Get(
						"Diagnostics.Health.Files.Missing",
						string.Join(", ", missing)),
				SynixHealthAction.ValidateServerFiles,
				server));
		}

		private static void CheckRuntimeRequirements(
			GameServer server,
			GameInfo game,
			ICollection<SynixHealthItem> items)
		{
			GamePrerequisiteReport report = GamePrerequisiteChecker.CheckCurrentSystem(game);
			foreach (GamePrerequisiteItem result in report.Items
				.Where(result => result.State != GamePrerequisiteState.Passed))
			{
				items.Add(new SynixHealthItem(
					result.State == GamePrerequisiteState.Failed
						? SynixHealthLevel.Failed
						: SynixHealthLevel.Warning,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Runtimes"),
					$"{server.ServerName}: {result.Name}",
					result.Message,
					SynixHealthAction.None,
					server));
			}
		}

		internal static async Task CheckConfigurationAsync(
			GameServer server,
			ICollection<SynixHealthItem> items)
		{
			if (server.PreserveImportedConfiguration)
			{
				items.Add(new SynixHealthItem(
					SynixHealthLevel.Passed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Configuration"),
					server.ServerName,
					LocalizationManager.Get(
						"Diagnostics.Health.Configuration.Imported"),
					SynixHealthAction.None,
					server));
				return;
			}

			ConfigurationValidationReport report =
				await GameFix.ValidateManagedConfiguration(server);
			SynixHealthLevel level = report.FailedCount > 0
				? SynixHealthLevel.Failed
				: report.WarningCount > 0
					? SynixHealthLevel.Warning
					: SynixHealthLevel.Passed;
			string details = level == SynixHealthLevel.Passed
				? LocalizationManager.Get(
					"Diagnostics.Health.Configuration.Complete")
				: string.Join(
					" ",
					report.Items
						.Where(item => item.State is ConfigurationValidationState.Warning or ConfigurationValidationState.Failed)
						.OrderByDescending(item => item.State)
						.Take(3)
						.Select(item => item.Message));
			items.Add(new SynixHealthItem(
				level,
				LocalizationManager.Get(
					"Diagnostics.Health.Area.Configuration"),
				server.ServerName,
				details,
				report.FixConfigAvailable && level != SynixHealthLevel.Passed
					? SynixHealthAction.FixConfiguration
					: SynixHealthAction.None,
				server));
		}

		private static void CheckPorts(
			GameServer server,
			GameInfo game,
			IReadOnlyList<GameServer> servers,
			ICollection<SynixHealthItem> items)
		{
			foreach ((int port, string name) in Core.GetRequiredServerPorts(server, game))
			{
				List<GameServer> configuredOwners = servers
					.Where(candidate => candidate != server)
					.Where(candidate =>
					{
						GameInfo? candidateGame = GameDatabase.GetGame(candidate.Game);
						return candidateGame != null &&
							Core.GetRequiredServerPorts(candidate, candidateGame)
								.Any(required => required.Port == port);
					})
					.ToList();
				bool serverActive = Core.IsActivePortReservation(server);
				if (configuredOwners.Count > 0)
				{
					items.Add(new SynixHealthItem(
						SynixHealthLevel.Failed,
						LocalizationManager.Get(
							"Diagnostics.Health.Area.Ports"),
						$"{server.ServerName}: {port}",
						LocalizationManager.Get(
							"Diagnostics.Health.Ports.Duplicate",
							name,
							string.Join(", ", configuredOwners.Select(
								owner => owner.ServerName))),
						SynixHealthAction.None,
						server));
					continue;
				}

				bool occupied = Core.Instance.IsPortInUseLocally(port);
				items.Add(new SynixHealthItem(
					occupied && !serverActive
						? SynixHealthLevel.Failed
						: SynixHealthLevel.Passed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Ports"),
					$"{server.ServerName}: {port}",
					occupied
						? serverActive
							? LocalizationManager.Get(
								"Diagnostics.Health.Ports.Active",
								name)
							: LocalizationManager.Get(
								"Diagnostics.Health.Ports.Occupied",
								name)
						: LocalizationManager.Get(
							"Diagnostics.Health.Ports.Available",
							name),
					SynixHealthAction.None,
					server));
			}
		}

		private static void CheckFirewall(
			GameServer server,
			GameInfo game,
			FirewallSnapshot firewall,
			ICollection<SynixHealthItem> items)
		{
			if (!firewall.InspectionSucceeded || !firewall.Enabled ||
				string.IsNullOrWhiteSpace(server.InstallPath))
			{
				return;
			}

			string launchFile;
			try
			{
				launchFile = Path.GetFullPath(
					GameLaunchCommandBuilder.ResolveExecutablePath(server, game));
			}
			catch
			{
				return;
			}
			if (!File.Exists(launchFile))
				return;

			string? allowedExecutable = WindowsFirewallInspector.FindAllowedExecutable(
				server.InstallPath,
				launchFile,
				firewall.AllowedExecutables);
			bool allowed = allowedExecutable != null;
			string checkedFile = GetInstalledRelativePath(server.InstallPath, launchFile);
			string matchedFile = allowedExecutable == null
				? string.Empty
				: GetInstalledRelativePath(server.InstallPath, allowedExecutable);
			items.Add(new SynixHealthItem(
				allowed ? SynixHealthLevel.Passed : SynixHealthLevel.Warning,
				LocalizationManager.Get(
					"Diagnostics.Health.Area.Firewall"),
				server.ServerName,
				allowed
					? LocalizationManager.Get(
						"Diagnostics.Health.Firewall.Allowed",
						matchedFile)
					: LocalizationManager.Get(
						"Diagnostics.Health.Firewall.RuleMissing",
						checkedFile),
				allowed ? SynixHealthAction.None : SynixHealthAction.OpenFirewallSettings,
				server));
		}

		private static string GetInstalledRelativePath(string installPath, string path)
		{
			try
			{
				string relative = Path.GetRelativePath(
					Path.GetFullPath(installPath),
					Path.GetFullPath(path));
				return relative.StartsWith("..", StringComparison.Ordinal)
					? Path.GetFileName(path)
					: relative;
			}
			catch
			{
				return Path.GetFileName(path);
			}
		}

		private static void CheckProcessRecovery(
			GameServer server,
			ICollection<SynixHealthItem> items)
		{
			IReadOnlyList<ServerProcessIdentity> processes =
				Servers.RefreshServerProcessRegistry(server, forceDiscovery: true);
			bool statusClaimsActive =
				server.Status == Core.StatusManager.GetStatus(Core.ServerState.Running) ||
				server.Status == Core.StatusManager.GetStatus(Core.ServerState.Starting) ||
				server.Status == Core.StatusManager.GetStatus(Core.ServerState.Stopping);
			bool primaryTracked = server.PID.HasValue &&
				processes.Any(process => process.ProcessId == server.PID.Value);

			if (processes.Count > 0)
			{
				string processDetails = string.Join(
					", ",
					processes.Select(process =>
						$"{Path.GetFileName(process.ExecutablePath)} (PID {process.ProcessId})"));
				bool needsRecovery = !statusClaimsActive || !primaryTracked;
				items.Add(new SynixHealthItem(
					needsRecovery ? SynixHealthLevel.Warning : SynixHealthLevel.Passed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.ProcessTracking"),
					server.ServerName,
					needsRecovery
						? LocalizationManager.Get(
							"Diagnostics.Health.Process.RecoveryNeeded",
							processes.Count,
							processDetails)
						: LocalizationManager.Get(
							"Diagnostics.Health.Process.Tracking",
							processes.Count,
							processDetails),
					needsRecovery ? SynixHealthAction.RecoverProcesses : SynixHealthAction.None,
					server));
				return;
			}

			items.Add(new SynixHealthItem(
				statusClaimsActive ? SynixHealthLevel.Warning : SynixHealthLevel.Passed,
				LocalizationManager.Get(
					"Diagnostics.Health.Area.ProcessTracking"),
				server.ServerName,
				statusClaimsActive
					? LocalizationManager.Get(
						"Diagnostics.Health.Process.StatusMismatch")
					: LocalizationManager.Get(
						"Diagnostics.Health.Process.None"),
				statusClaimsActive ? SynixHealthAction.RecoverProcesses : SynixHealthAction.None,
				server));
		}

		private static void CheckLatestLog(
			GameServer server,
			ICollection<SynixHealthItem> items)
		{
			GameLogDiscoveryResult result = GameLogDiscovery.FindLatest(server);
			if (!result.Found)
			{
				items.Add(new SynixHealthItem(
					GameLogDiscovery.HasDeclaredLogs(server.Game)
						? SynixHealthLevel.Warning
						: SynixHealthLevel.Passed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.RecentLogs"),
					server.ServerName,
					GameLogDiscovery.HasDeclaredLogs(server.Game)
						? LocalizationManager.Get(
							"Diagnostics.Health.Logs.NotFound")
						: LocalizationManager.Get(
							"Diagnostics.Health.Logs.NotDeclared"),
					SynixHealthAction.None,
					server));
				return;
			}

			string tail = ReadLogTail(result.LatestLogPath!);
			string? likelyProblem = FindLikelyLogProblem(tail);
			items.Add(new SynixHealthItem(
				likelyProblem == null ? SynixHealthLevel.Passed : SynixHealthLevel.Warning,
				LocalizationManager.Get(
					"Diagnostics.Health.Area.RecentLogs"),
				server.ServerName,
				likelyProblem == null
					? LocalizationManager.Get(
						"Diagnostics.Health.Logs.Healthy",
						Path.GetFileName(result.LatestLogPath))
					: likelyProblem,
				SynixHealthAction.OpenLatestLog,
				server));
		}

		internal static string? FindLikelyLogProblem(string logText)
		{
			if (string.IsNullOrWhiteSpace(logText))
				return null;
			Match match = ErrorLinePattern.Matches(logText).Cast<Match>().LastOrDefault()!;
			if (match == null || !match.Success)
				return null;
			string line = Regex.Replace(match.Value.Trim(), @"\s+", " ");
			if (line.Length > 360)
				line = line[..360] + "…";
			return LocalizationManager.Get(
				"Diagnostics.Health.Logs.Problem",
				line);
		}

		private static string ReadLogTail(string path)
		{
			try
			{
				using FileStream stream = new(
					path,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete);
				if (stream.Length > MaximumLogBytes)
					stream.Seek(-MaximumLogBytes, SeekOrigin.End);
				using StreamReader reader = new(stream, detectEncodingFromByteOrderMarks: true);
				return reader.ReadToEnd();
			}
			catch
			{
				return string.Empty;
			}
		}

		private static void CheckDiskSpace(
			IEnumerable<GameServer> servers,
			ICollection<SynixHealthItem> items)
		{
			HashSet<string> roots = new(StringComparer.OrdinalIgnoreCase);
			foreach (string path in servers.Select(server => server.InstallPath).Append(Core.RootPath))
			{
				try
				{
					if (!string.IsNullOrWhiteSpace(path))
					{
						string? root = Path.GetPathRoot(Path.GetFullPath(path));
						if (!string.IsNullOrWhiteSpace(root))
							roots.Add(root);
					}
				}
				catch (Exception suppressedException)
				{
					Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
				}
			}
			foreach (string root in roots)
			{
				try
				{
					DriveInfo drive = new(root);
					long free = drive.AvailableFreeSpace;
					SynixHealthLevel level = free < OneGibibyte
						? SynixHealthLevel.Failed
						: free < 10 * OneGibibyte
							? SynixHealthLevel.Warning
							: SynixHealthLevel.Passed;
					items.Add(new SynixHealthItem(
						level,
						LocalizationManager.Get(
							"Diagnostics.Health.Area.DiskSpace"),
						root,
						LocalizationManager.Get(
							"Diagnostics.Health.Disk.Available",
							FormatBytes(free))));
				}
				catch (Exception exception)
				{
					items.Add(new SynixHealthItem(
						SynixHealthLevel.Warning,
						LocalizationManager.Get(
							"Diagnostics.Health.Area.DiskSpace"),
						root,
						LocalizationManager.Get(
							"Diagnostics.Health.Disk.Failed",
							exception.Message)));
				}
			}
		}

		private static async Task CheckUpdateAsync(
			ICollection<SynixHealthItem> items,
			CancellationToken cancellationToken)
		{
			try
			{
				Version current = Core.GetCurrentVersion();
				SynixUpdateCheckResult result = await Core.CheckForUpdatesAsync(
					current,
					cancellationToken);
				items.Add(new SynixHealthItem(
					result.UpdateAvailable
						? SynixHealthLevel.Warning
						: SynixHealthLevel.Passed,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Update"),
					$"v{current.ToString(3)} — {result.Installation.DisplayName}",
					result.UpdateAvailable
						? result.ReleaseReady
							? LocalizationManager.Get(
								"Diagnostics.Health.Update.Available",
								result.AdvertisedVersion!.ToString(3))
							: result.Problem ?? LocalizationManager.Get(
								"Diagnostics.Health.Update.Preparing")
						: LocalizationManager.Get(
							"Diagnostics.Health.Update.Current"),
					result.UpdateAvailable
						? SynixHealthAction.OpenUpdate
						: SynixHealthAction.None));
			}
			catch (Exception exception) when (exception is HttpRequestException or
				TaskCanceledException or InvalidDataException)
			{
				items.Add(new SynixHealthItem(
					SynixHealthLevel.Warning,
					LocalizationManager.Get(
						"Diagnostics.Health.Area.Update"),
					$"v{Core.GetCurrentVersion().ToString(3)}",
					LocalizationManager.Get(
						"Diagnostics.Health.Update.Failed",
						exception.Message),
					SynixHealthAction.OpenUpdate));
			}
		}

		private static string FormatBytes(long bytes)
		{
			string[] units = ["B", "KB", "MB", "GB", "TB"];
			double value = bytes;
			int unit = 0;
			while (value >= 1024 && unit < units.Length - 1)
			{
				value /= 1024;
				unit++;
			}
			return $"{value:0.##} {units[unit]}";
		}

		private static void RunServerCheck(
			ICollection<SynixHealthItem> items,
			string area,
			GameServer server,
			Action check)
		{
			try
			{
				check();
			}
			catch (Exception exception)
			{
				AddCheckFailure(items, area, server, exception);
			}
		}

		private static void AddCheckFailure(
			ICollection<SynixHealthItem> items,
			string area,
			GameServer server,
			Exception exception)
		{
			items.Add(new SynixHealthItem(
				SynixHealthLevel.Warning,
				area,
				server.ServerName,
				LocalizationManager.Get(
					"Diagnostics.Health.Check.Failed",
					exception.Message),
				SynixHealthAction.None,
				server));
		}
	}

	internal sealed record FirewallSnapshot(
		bool InspectionSucceeded,
		bool Enabled,
		IReadOnlySet<string> AllowedExecutables,
		IReadOnlySet<string> ProgramExecutables,
		string? Problem);

	internal static class WindowsFirewallInspector
	{
		internal static string? FindAllowedExecutable(
			string installPath,
			string configuredLaunchPath,
			IEnumerable<string> allowedExecutables)
		{
			if (!TryNormalizePath(installPath, out string normalizedInstallPath))
				return null;

			TryNormalizePath(configuredLaunchPath, out string normalizedLaunchPath);
			List<string> normalizedRules = [];
			foreach (string allowedExecutable in allowedExecutables)
			{
				if (TryNormalizePath(allowedExecutable, out string normalizedRule) &&
					normalizedRule.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				{
					normalizedRules.Add(normalizedRule);
				}
			}

			string? exactMatch = normalizedRules.FirstOrDefault(path =>
				!string.IsNullOrWhiteSpace(normalizedLaunchPath) &&
				string.Equals(path, normalizedLaunchPath, StringComparison.OrdinalIgnoreCase));
			if (exactMatch != null)
				return exactMatch;

			return normalizedRules.FirstOrDefault(path =>
				IsPathInsideDirectory(path, normalizedInstallPath));
		}

		private static bool TryNormalizePath(string path, out string normalizedPath)
		{
			normalizedPath = string.Empty;
			if (string.IsNullOrWhiteSpace(path))
				return false;

			try
			{
				string expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
				normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(expanded));
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool IsPathInsideDirectory(string path, string directory)
		{
			if (string.Equals(path, directory, StringComparison.OrdinalIgnoreCase))
				return false;

			string directoryPrefix = Path.EndsInDirectorySeparator(directory)
				? directory
				: directory + Path.DirectorySeparatorChar;
			return path.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
		}

		internal static FirewallSnapshot Capture()
		{
			if (!OperatingSystem.IsWindows())
				return new(
					false,
					false,
					new HashSet<string>(),
					new HashSet<string>(),
					LocalizationManager.Get(
						"Diagnostics.Health.Firewall.WindowsOnly"));

			object? policy = null;
			object? rules = null;
			try
			{
				Type? policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
				if (policyType == null)
					return new(
						false,
						false,
						new HashSet<string>(),
						new HashSet<string>(),
						LocalizationManager.Get(
							"Diagnostics.Health.Firewall.Unavailable"));
				policy = Activator.CreateInstance(policyType);
				if (policy == null)
					return new(
						false,
						false,
						new HashSet<string>(),
						new HashSet<string>(),
						LocalizationManager.Get(
							"Diagnostics.Health.Firewall.OpenFailed"));

				dynamic firewallPolicy = policy;
				int currentProfiles = Convert.ToInt32(firewallPolicy.CurrentProfileTypes);
				bool enabled = false;
				foreach (int profile in new[] { 1, 2, 4 })
				{
					if ((currentProfiles & profile) != 0 &&
						Convert.ToBoolean(firewallPolicy.FirewallEnabled[profile]))
					{
						enabled = true;
					}
				}

				rules = firewallPolicy.Rules;
				HashSet<string> allowed = new(StringComparer.OrdinalIgnoreCase);
				HashSet<string> programExecutables = new(StringComparer.OrdinalIgnoreCase);
				foreach (object ruleObject in (IEnumerable)rules)
				{
					dynamic rule = ruleObject;
					try
					{
						string applicationName = Convert.ToString(rule.ApplicationName) ?? string.Empty;
						if (!TryNormalizePath(applicationName, out string executablePath) ||
							!executablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						programExecutables.Add(executablePath);
						bool ruleEnabled = Convert.ToBoolean(rule.Enabled);
						int direction = Convert.ToInt32(rule.Direction);
						int action = Convert.ToInt32(rule.Action);
						if (ruleEnabled && direction == 1 && action == 1)
							allowed.Add(executablePath);
					}
					catch (Exception suppressedException)
					{
						Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
					}
					finally
					{
						if (Marshal.IsComObject(ruleObject))
							Marshal.FinalReleaseComObject(ruleObject);
					}
				}

				return new(true, enabled, allowed, programExecutables, null);
			}
			catch (Exception exception)
			{
				return new(
					false,
					false,
					new HashSet<string>(),
					new HashSet<string>(),
					exception.Message);
			}
			finally
			{
				if (rules != null && Marshal.IsComObject(rules))
					Marshal.FinalReleaseComObject(rules);
				if (policy != null && Marshal.IsComObject(policy))
					Marshal.FinalReleaseComObject(policy);
			}
		}
	}
}
