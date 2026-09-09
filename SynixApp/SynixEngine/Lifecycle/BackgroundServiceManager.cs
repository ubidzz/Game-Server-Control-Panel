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
using Microsoft.Win32;
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using System.Diagnostics;

namespace Synix_Control_Panel.SynixEngine
{
	internal static class BackgroundServiceManager
	{
		private static bool _suppressStartForCurrentProcess;
		internal const string AgentArgument = "--synix-background-agent";
		private const string AgentMutexName = @"Local\SynixControlPanel.BackgroundAgent";
		private const string AgentStopEventName = @"Local\SynixControlPanel.BackgroundAgent.Stop";
		private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private const string RunValueName = "Synix Background Service";

		internal static bool IsAgentCommand(IReadOnlyList<string> arguments) =>
			arguments.Any(argument => argument.Equals(
				AgentArgument,
				StringComparison.OrdinalIgnoreCase));

		internal static string BuildLaunchCommand(string executablePath) =>
			$"\"{Path.GetFullPath(executablePath)}\" {AgentArgument}";

		internal static bool SetEnabled(bool enabled, out string message)
		{
			try
			{
				using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
					?? throw new InvalidOperationException(LocalizationManager.Get(
						"BackgroundService.Error.StartupSettings"));
				if (enabled)
				{
					string executablePath = GetExecutablePath();
					key.SetValue(RunValueName, BuildLaunchCommand(executablePath), RegistryValueKind.String);
					message = LocalizationManager.Get(
						"Advanced.Background.EnabledResult");
				}
				else
				{
					key.DeleteValue(RunValueName, throwOnMissingValue: false);
					RequestStop();
					message = LocalizationManager.Get(
						"Advanced.Background.DisabledResult");
				}
				return true;
			}
			catch (Exception exception)
			{
				message = LocalizationManager.Get(
					"BackgroundService.Error.UpdateSetting",
					exception.Message);
				return false;
			}
		}

		internal static void EnsureRegistrationMatchesSetting()
		{
			if (Properties.Settings.Default.BackgroundServiceEnabled)
				SetEnabled(true, out _);
		}

		internal static void RequestStop()
		{
			try
			{
				using EventWaitHandle stopEvent = EventWaitHandle.OpenExisting(AgentStopEventName);
				stopEvent.Set();
			}
			catch (WaitHandleCannotBeOpenedException suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
		}

		internal static bool WaitForStop(TimeSpan timeout)
		{
			RequestStop();
			DateTime deadline = DateTime.UtcNow + timeout;
			do
			{
				try
				{
					using Mutex mutex = Mutex.OpenExisting(AgentMutexName);
					if (mutex.WaitOne(TimeSpan.FromMilliseconds(100)))
					{
						try { mutex.ReleaseMutex(); }
						catch (ApplicationException suppressedException)
						{
							Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
						}
						return true;
					}
				}
				catch (WaitHandleCannotBeOpenedException suppressedException)
				{
					ApplicationLogService.WriteSuppressedException(suppressedException);
					return true;
				}
				catch (Exception suppressedException)
				{
					ApplicationLogService.WriteSuppressedException(suppressedException);
					return false;
				}
			}
			while (DateTime.UtcNow < deadline);

			return false;
		}

		internal static void StartIfEnabled()
		{
			if (!ShouldStartAgent(
				_suppressStartForCurrentProcess,
				Properties.Settings.Default.BackgroundServiceEnabled,
				IsAgentRunning()))
				return;

			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = GetExecutablePath(),
					Arguments = AgentArgument,
					UseShellExecute = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden
				});
			}
			catch (Exception exception)
			{
				FileHandler.WriteLogImmediate(
					"Synix_Background_Service",
					$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Could not start: {exception}\r\n");
			}
		}

		internal static void SuppressStartForCurrentProcess() =>
			_suppressStartForCurrentProcess = true;

		/// <summary>
		/// Makes an explicit dashboard close a full Synix exit. Game-server
		/// processes are intentionally not touched.
		/// </summary>
		internal static void PrepareForDashboardExit()
		{
			SuppressStartForCurrentProcess();
			RequestStop();
		}

		internal static bool ShouldStartAgent(
			bool startSuppressed,
			bool enabled,
			bool agentRunning) =>
			!startSuppressed && enabled && !agentRunning;

		internal static int RunAgent()
		{
			using Mutex mutex = new(
				initiallyOwned: true,
				AgentMutexName,
				out bool isFirstAgent);
			if (!isFirstAgent)
				return 0;

			using EventWaitHandle stopEvent = new(
				initialState: false,
				EventResetMode.AutoReset,
				AgentStopEventName);
			try
			{
				Core.IsBackgroundServiceMode = true;
				FileHandler.LoadServers();
				Core.Instance.RebindProcesses().GetAwaiter().GetResult();
				FileHandler.WriteLogImmediate(
					"Synix_Background_Service",
					$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Background monitoring started.\r\n");
				stopEvent.WaitOne();
				FileHandler.SaveServers();
				FileHandler.FlushLogsAsync().GetAwaiter().GetResult();
				return 0;
			}
			catch (Exception exception)
			{
				FileHandler.WriteLogImmediate(
					"Synix_Background_Service",
					$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Background monitoring stopped unexpectedly: {exception}\r\n");
				return 1;
			}
			finally
			{
				Core.IsBackgroundServiceMode = false;
				try { mutex.ReleaseMutex(); }
				catch (ApplicationException suppressedException)
				{
					Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
				}
			}
		}

		private static bool IsAgentRunning()
		{
			try
			{
				using Mutex mutex = Mutex.OpenExisting(AgentMutexName);
				return true;
			}
			catch (WaitHandleCannotBeOpenedException suppressedException)
			{
				ApplicationLogService.WriteSuppressedException(suppressedException);
				return false;
			}
			catch (Exception suppressedException)
			{
				ApplicationLogService.WriteSuppressedException(suppressedException);
				return false;
			}
		}

		private static string GetExecutablePath() =>
			Environment.ProcessPath ??
			throw new InvalidOperationException(LocalizationManager.Get(
				"BackgroundService.Error.ProgramPath"));
	}
}
