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
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class ServerProcessDiscoveryTests
{
	[Theory]
	[InlineData("Windrose", "WindroseServer.exe")]
	[InlineData("Minecraft", "cmd.exe")]
	public async Task Stop_DoesNotTerminateAnUnrelatedProcessWithAStaleSavedPid(string game, string executableName)
	{
		string fixture = Path.Combine(Path.GetTempPath(), "SynixStalePidTests", Guid.NewGuid().ToString("N"));
		string unrelatedDirectory = Path.Combine(fixture, "unrelated");
		Directory.CreateDirectory(unrelatedDirectory);
		string executable = Path.Combine(unrelatedDirectory, executableName);
		File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), executable);
		using Process unrelated = Process.Start(new ProcessStartInfo
		{
			FileName = executable,
			Arguments = "/d /c ping 127.0.0.1 -n 120 > nul",
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden
		}) ?? throw new InvalidOperationException("Could not start the isolated process fixture.");
		try
		{
			await Task.Delay(200);
			GameServer staleEntry = new()
			{
				Game = game,
				ServerName = "Stale PID regression test",
				InstallPath = Path.Combine(fixture, "different-server"),
				PID = unrelated.Id
			};
			Assert.True(await Servers.Stop(staleEntry, (_, _) => { }));
			Assert.False(unrelated.HasExited, "A matching executable name does not prove server ownership.");
			Assert.Empty(staleEntry.ServerProcesses);
			Assert.Null(staleEntry.PID);
		}
		finally
		{
			if (!unrelated.HasExited)
			{
				unrelated.Kill(entireProcessTree: true);
				await unrelated.WaitForExitAsync();
			}
			Directory.Delete(fixture, recursive: true);
		}
	}

	[Fact]
	public void ImagePathQuery_ResolvesTheCurrentExecutableWithLimitedAccess()
	{
		Assert.Equal(Environment.ProcessPath, Servers.TryGetProcessImagePath(Environment.ProcessId),
			ignoreCase: true);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(int.MaxValue)]
	public void ImagePathQuery_UnavailableProcessIsANormalMiss(int processId)
	{
		Assert.Null(Servers.TryGetProcessImagePath(processId));
	}

	[Fact]
	public void ImagePathQuery_DoesNotLeakNativeProcessHandles()
	{
		using Process current = Process.GetCurrentProcess();
		for (int i = 0; i < 16; i++)
			Assert.NotNull(Servers.TryGetProcessImagePath(current.Id));
		current.Refresh();
		int handlesBefore = current.HandleCount;
		for (int i = 0; i < 256; i++)
			Assert.NotNull(Servers.TryGetProcessImagePath(current.Id));
		current.Refresh();
		Assert.True(current.HandleCount <= handlesBefore + 8,
			"Repeated process-image queries must dispose their native handles.");
	}

	[Fact]
	public void InstallDirectoryDiscovery_DoesNotThrowForUnrelatedProtectedProcesses()
	{
		GameServer server = new()
		{
			Game = "Windrose",
			ServerName = "Read-only discovery regression test",
			InstallPath = Path.Combine(Path.GetTempPath(), "SynixDiscoveryTests", Guid.NewGuid().ToString("N"))
		};
		int accessFailures = 0;
		int dashboardMessages = 0;
		int threadId = Environment.CurrentManagedThreadId;
		EventHandler<FirstChanceExceptionEventArgs> exceptionHandler = (_, args) =>
		{
			if (Environment.CurrentManagedThreadId == threadId &&
				args.Exception is Win32Exception { NativeErrorCode: 5 })
				accessFailures++;
		};
		EventHandler<ApplicationLogEventArgs> logHandler = (_, _) => dashboardMessages++;
		AppDomain.CurrentDomain.FirstChanceException += exceptionHandler;
		ApplicationUiService.LogRequested += logHandler;
		try
		{
			// This only discovers processes for a unique, nonexistent installation;
			// it cannot bind to or operate on a user's server.
			Assert.Empty(Servers.RefreshServerProcessRegistry(server, forceDiscovery: true));
			Assert.Equal(0, accessFailures);
			Assert.Equal(0, dashboardMessages);
		}
		finally
		{
			AppDomain.CurrentDomain.FirstChanceException -= exceptionHandler;
			ApplicationUiService.LogRequested -= logHandler;
		}
	}
}
