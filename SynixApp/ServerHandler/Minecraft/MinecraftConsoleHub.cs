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
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	internal sealed record MinecraftConsoleLine(DateTime Timestamp, string Text, bool IsError);

	internal static class MinecraftConsoleHub
	{
		private const int MaximumBufferedLines = 1_500;
		private static readonly ConcurrentDictionary<string, ConcurrentQueue<MinecraftConsoleLine>> Buffers =
			new(StringComparer.OrdinalIgnoreCase);

		internal static event Action<GameServer, MinecraftConsoleLine>? LineReceived;

		internal static void Attach(GameServer server, Process process)
		{
			ArgumentNullException.ThrowIfNull(server);
			ArgumentNullException.ThrowIfNull(process);
			if (!process.StartInfo.RedirectStandardOutput)
				return;

			process.OutputDataReceived += (_, eventArgs) =>
			{
				if (eventArgs.Data != null)
					Publish(server, eventArgs.Data, isError: false);
			};
			process.ErrorDataReceived += (_, eventArgs) =>
			{
				if (eventArgs.Data != null)
					Publish(server, eventArgs.Data, isError: true);
			};
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			Publish(server, LocalizationManager.Get("Minecraft.Console.Connected"), false);
		}

		internal static void NotifyStopped(GameServer server)
		{
			if (GameCapabilityResolver.UsesMinecraftConsole(server))
				Publish(server, LocalizationManager.Get("Minecraft.Console.Stopped"), false);
		}

		internal static IReadOnlyList<MinecraftConsoleLine> GetSnapshot(GameServer server)
		{
			string key = GetKey(server);
			return Buffers.TryGetValue(key, out ConcurrentQueue<MinecraftConsoleLine>? buffer)
				? buffer.ToArray()
				: LoadRecentMinecraftLog(server);
		}

		internal static void Publish(GameServer server, string text, bool isError)
		{
			if (!GameCapabilityResolver.UsesMinecraftConsole(server) || string.IsNullOrWhiteSpace(text))
				return;

			MinecraftConsoleLine line = new(DateTime.Now, text, isError);
			ConcurrentQueue<MinecraftConsoleLine> buffer = Buffers.GetOrAdd(
				GetKey(server),
				_ => new ConcurrentQueue<MinecraftConsoleLine>());
			buffer.Enqueue(line);
			while (buffer.Count > MaximumBufferedLines)
				buffer.TryDequeue(out _);

			LineReceived?.Invoke(server, line);
		}

		internal static bool IsSameServer(GameServer first, GameServer second) =>
			GetKey(first).Equals(GetKey(second), StringComparison.OrdinalIgnoreCase);

		private static string GetKey(GameServer server)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(server.InstallPath))
					return Path.GetFullPath(server.InstallPath).TrimEnd(Path.DirectorySeparatorChar);
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}

			return $"{server.Game}|{server.ServerName}";
		}

		private static IReadOnlyList<MinecraftConsoleLine> LoadRecentMinecraftLog(
			GameServer server)
		{
			try
			{
				string path = Path.Combine(server.InstallPath, "logs", "latest.log");
				if (!File.Exists(path))
					return [];

				return File.ReadLines(path)
					.TakeLast(500)
					.Select(line => new MinecraftConsoleLine(
						File.GetLastWriteTime(path),
						line,
						line.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
							line.Contains("FATAL", StringComparison.OrdinalIgnoreCase)))
					.ToArray();
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
				return [];
			}
		}
	}
}
