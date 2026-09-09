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
using Synix_Control_Panel.SynixApp.ServerHandler;
using System.Diagnostics;

namespace Synix_Control_Panel.SynixEngine
{
	internal static class ProcessRecovery
	{
		internal static Process? FindInstalledServerProcess(
			GameServer server,
			GameInfo game,
			int? excludedProcessId = null)
		{
			ArgumentNullException.ThrowIfNull(server);
			ArgumentNullException.ThrowIfNull(game);
			string executableName = MinecraftControlProfile.ResolveExecutableName(server, game);
			if (string.IsNullOrWhiteSpace(server.InstallPath) ||
				string.IsNullOrWhiteSpace(executableName) ||
				!executableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			string expectedPath;
			try
			{
				expectedPath = Path.GetFullPath(Path.Combine(
					server.InstallPath,
					executableName));
			}
			catch (Exception exception)
			{
				ApplicationLogService.WriteSuppressedException(exception);
				return null;
			}

			string processName = Path.GetFileNameWithoutExtension(executableName);
			Process[] candidates = Process.GetProcessesByName(processName);
			Process? matched = null;
			try
			{
				foreach (Process process in candidates)
				{
					if (process.Id == Environment.ProcessId || process.Id == excludedProcessId)
						continue;

					try
					{
						string? actualPath = Servers.TryGetProcessImagePath(process.Id);
						if (!string.IsNullOrWhiteSpace(actualPath) &&
							string.Equals(Path.GetFullPath(actualPath), expectedPath, StringComparison.OrdinalIgnoreCase) &&
							!process.HasExited)
						{
							matched = process;
							return matched;
						}
					}
					catch (Exception exception)
					{
						ApplicationLogService.WriteSuppressedException(exception);
					}
				}
			}
			finally
			{
				foreach (Process process in candidates)
					if (!ReferenceEquals(process, matched))
						process.Dispose();
			}

			return null;
		}

		internal static bool IsRecordedProcessValid(
			GameServer server,
			GameInfo game)
		{
			if (!server.PID.HasValue || server.PID.Value <= 0 || server.PID.Value == Environment.ProcessId)
				return false;

			try
			{
				using Process process = Process.GetProcessById(server.PID.Value);
				if (process.HasExited)
					return false;

				string? imagePath = Servers.TryGetProcessImagePath(process.Id);
				if (string.IsNullOrWhiteSpace(imagePath))
					return false;
				string actualPath = Path.GetFullPath(imagePath);
				string executableName = MinecraftControlProfile.ResolveExecutableName(server, game);
				if (executableName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) ||
					executableName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
				{
					// A Windows command interpreter lives outside the server folder.
					// Recover only the exact launcher Synix previously recorded, never
					// an arbitrary cmd.exe that has reused the saved process ID.
					if (!Path.GetFileName(actualPath).Equals("cmd.exe", StringComparison.OrdinalIgnoreCase))
						return false;
					DateTime startTime = process.StartTime.ToUniversalTime();
					return Servers.GetServerProcessSnapshot(server).Any(identity =>
						identity.ProcessId == process.Id && identity.StartTimeUtc == startTime &&
						!string.IsNullOrWhiteSpace(identity.ExecutablePath) &&
						string.Equals(Path.GetFullPath(identity.ExecutablePath), actualPath, StringComparison.OrdinalIgnoreCase));
				}

				string expectedPath = Path.GetFullPath(Path.Combine(server.InstallPath, executableName));
				return string.Equals(expectedPath, actualPath, StringComparison.OrdinalIgnoreCase);
			}
			catch (Exception exception)
			{
				ApplicationLogService.WriteSuppressedException(exception);
				return false;
			}
		}
	}
}
