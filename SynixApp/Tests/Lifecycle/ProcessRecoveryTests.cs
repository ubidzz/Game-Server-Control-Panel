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
using Synix_Control_Panel.SynixEngine;
using System.Diagnostics;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class ProcessRecoveryTests
{
	[Theory]
	[InlineData("missing", false)]
	[InlineData("matching", true)]
	[InlineData("different start time", false)]
	[InlineData("different path", false)]
	public void ScriptRecovery_RequiresTheRecordedLauncherIdentity(string identityKind, bool expected)
	{
		using ProcessFixture fixture = new("cmd.exe");
		GameServer server = new()
		{
			Game = "Minecraft",
			ServerName = "Isolated launcher recovery test",
			InstallPath = Path.Combine(fixture.Root, "minecraft-server"),
			PID = fixture.Process.Id
		};
		if (identityKind != "missing")
		{
			server.ServerProcesses.Add(new ServerProcessIdentity
			{
				ProcessId = fixture.Process.Id,
				ExecutablePath = identityKind == "different path"
					? Path.Combine(fixture.Root, "other", "cmd.exe") : fixture.Executable,
				StartTimeUtc = fixture.Process.StartTime.ToUniversalTime()
					.AddTicks(identityKind == "different start time" ? -1 : 0)
			});
		}
		GameInfo game = Assert.IsType<GameInfo>(GameDatabase.GetGame(server.Game));
		Assert.Equal(expected, ProcessRecovery.IsRecordedProcessValid(server, game));
		Assert.False(fixture.Process.HasExited);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void NativeRecovery_RequiresTheExactInstallationPath(bool correctInstallation)
	{
		using ProcessFixture fixture = new("WindroseServer.exe");
		GameServer server = new()
		{
			Game = "Windrose",
			ServerName = "Isolated native recovery test",
			InstallPath = correctInstallation ? fixture.Root : Path.Combine(fixture.Root, "other"),
			PID = fixture.Process.Id
		};
		GameInfo game = Assert.IsType<GameInfo>(GameDatabase.GetGame(server.Game));
		Assert.Equal(correctInstallation, ProcessRecovery.IsRecordedProcessValid(server, game));
		using Process? recovered = ProcessRecovery.FindInstalledServerProcess(server, game);
		Assert.Equal(correctInstallation ? fixture.Process.Id : (int?)null, recovered?.Id);
		using Process? excluded = ProcessRecovery.FindInstalledServerProcess(server, game, fixture.Process.Id);
		Assert.Null(excluded);
		Assert.False(fixture.Process.HasExited);
	}

	private sealed class ProcessFixture : IDisposable
	{
		internal string Root { get; } = Path.Combine(Path.GetTempPath(), "SynixRecoveryTests", Guid.NewGuid().ToString("N"));
		internal string Executable { get; }
		internal Process Process { get; }

		internal ProcessFixture(string executableName)
		{
			Directory.CreateDirectory(Root);
			Executable = Path.Combine(Root, executableName);
			File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), Executable);
			Process = System.Diagnostics.Process.Start(new ProcessStartInfo
			{
				FileName = Executable,
				Arguments = "/d /c ping 127.0.0.1 -n 120 > nul",
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			}) ?? throw new InvalidOperationException("Could not start the isolated recovery fixture.");
		}

		public void Dispose()
		{
			try
			{
				if (!Process.HasExited)
				{
					Process.Kill(entireProcessTree: true);
					Process.WaitForExit(5000);
				}
			}
			finally
			{
				Process.Dispose();
				Directory.Delete(Root, recursive: true);
			}
		}
	}
}
