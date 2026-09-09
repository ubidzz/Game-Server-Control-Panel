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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Synix_Control_Panel.SynixApp.Database;

namespace Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;

/// <summary>
/// One on-demand operation against the selected server's native Unreal console.
/// No global keystrokes, clipboard access, process-memory writes or idle polling.
/// </summary>
internal static class SatisfactoryConsoleTokenReader
{
	private const int UnrealLogControlId = 34817;
	private const int UnrealCommandControlId = 34820;
	private const int UnrealRunControlId = 34821;
	private static int _reading;

	internal static async Task<string> GenerateAsync(GameServer server, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (!GameDatabase.IsSatisfactory(server.Game) || !SatisfactoryIntegration.IsLive(server))
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
		string installPath = server.InstallPath;
		ServerProcessIdentity[] identities = Servers.GetServerProcessSnapshot(server);
		if (Interlocked.CompareExchange(ref _reading, 1, 0) != 0)
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleBusy);
		TaskCompletionSource<string> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		// A misbehaving accessibility provider cannot freeze the form or create an
		// unbounded series of abandoned workers. Only one reader may exist at a time.
		Thread worker = new(() =>
		{
			try
			{
				using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeout.CancelAfter(TimeSpan.FromSeconds(15));
				completion.TrySetResult(GenerateInWindow(installPath, identities, timeout.Token));
			}
			catch (OperationCanceledException) { completion.TrySetCanceled(); }
			catch (SatisfactoryApiException exception) { completion.TrySetException(exception); }
			catch { completion.TrySetException(new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable)); }
			finally { Volatile.Write(ref _reading, 0); }
		}) { IsBackground = true, Name = nameof(SatisfactoryConsoleTokenReader) };
		try
		{
			worker.SetApartmentState(ApartmentState.MTA);
			worker.Start();
		}
		catch
		{
			Volatile.Write(ref _reading, 0);
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
		}
		try { return await completion.Task.WaitAsync(TimeSpan.FromSeconds(16), cancellationToken); }
		catch (Exception exception) when (exception is TimeoutException ||
			exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
		{ throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleBusy); }
	}

	private static string GenerateInWindow(string installPath, ServerProcessIdentity[] identities, CancellationToken cancellationToken)
	{
		Dictionary<int, ServerProcessIdentity> owners = identities.Where(identity => MatchesLiveProcess(installPath, identity))
			.GroupBy(identity => identity.ProcessId).ToDictionary(group => group.Key, group => group.First());
		List<(IntPtr Window, ServerProcessIdentity Owner)> windows = [];
		EnumWindows((window, _) =>
		{
			if (cancellationToken.IsCancellationRequested) return false;
			GetWindowThreadProcessId(window, out uint pid);
			if (!owners.TryGetValue((int)pid, out ServerProcessIdentity? owner)) return true;
			IntPtr list = GetDlgItem(window, UnrealLogControlId);
			if (list == IntPtr.Zero) return true;
			if (HasWindowOwner(list, owner.ProcessId) && IsLogList(list)) windows.Add((window, owner));
			return true;
		}, IntPtr.Zero);
		cancellationToken.ThrowIfCancellationRequested();
		if (windows.Count != 1) throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
		var selected = windows[0];
		using SatisfactoryTokenLogTail? outputLog = SatisfactoryTokenLogTail.TryOpen(installPath);
		return GenerateInConsole(selected.Window, selected.Owner.ProcessId,
			() => MatchesLiveProcess(installPath, selected.Owner), cancellationToken, outputLog);
	}

	// Also exercised against an isolated native console fixture; production reaches
	// this only after matching the recorded PID, executable path and start time.
	internal static string GenerateInConsole(IntPtr window, int processId, Func<bool> ownerStillMatches,
		CancellationToken cancellationToken, SatisfactoryTokenLogTail? outputLog = null)
	{
		cancellationToken.ThrowIfCancellationRequested();
		IntPtr list = GetDlgItem(window, UnrealLogControlId);
		IntPtr input = GetDlgItem(window, UnrealCommandControlId);
		IntPtr run = GetDlgItem(window, UnrealRunControlId);
		void Verify()
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!ownerStillMatches() || !HasWindowOwner(window, processId) ||
				!HasWindowOwner(list, processId) || !HasWindowOwner(input, processId) || !HasWindowOwner(run, processId) ||
				GetDlgItem(window, UnrealLogControlId) != list || GetDlgItem(window, UnrealCommandControlId) != input ||
				GetDlgItem(window, UnrealRunControlId) != run || !IsLogList(list) ||
				!HasClass(input, "Edit") || !HasClass(run, "Button") || !IsWindowEnabled(input) || !IsWindowEnabled(run))
				throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
		}
		Verify();
		// Do not overwrite a command the user is preparing, or change their filters.
		string draft = ReadText(input);
		if (draft.Length > 0 && draft != SatisfactoryApiClient.GenerateTokenCommand)
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleInputBusy);
		int baseline = 0;
		string? boundary = null;
		// The game's real log contains the full response even when its owner-drawn
		// window refreshes/recycles rows or exposes incomplete accessibility text.
		// Capture its EOF before the single dispatch, never an existing token line.
		bool watchLog = outputLog?.CaptureStart() == true;
		if (!watchLog)
		{
			foreach (int id in new[] { 34818, 34819 })
			{
				IntPtr filter = GetDlgItem(window, id);
				if (filter != IntPtr.Zero && ReadText(filter).Length > 0)
					throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleFilters);
			}
			bool captured = false;
			for (int attempt = 0; attempt < 3; attempt++)
			{
				Verify();
				baseline = ItemCount(list);
				boundary = baseline == 0 ? null : SatisfactoryConsoleAccessibility.CaptureTail(list, cancellationToken);
				if (baseline == ItemCount(list)) { captured = true; break; }
			}
			if (!captured || baseline > 0 && boundary == null)
				throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleBusy);
		}
		Verify();
		if (ReadText(input) != draft) throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleInputBusy);
		if (SendText(input, 0x000C, UIntPtr.Zero, SatisfactoryApiClient.GenerateTokenCommand, 0x22, 1000, out UIntPtr setResult) == IntPtr.Zero ||
			setResult == UIntPtr.Zero) throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
		if (ReadText(input) != SatisfactoryApiClient.GenerateTokenCommand)
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleInputBusy);
		// Deliver the button's native BN_CLICKED notification directly to its own
		// parent. Unlike BM_CLICK, this does not require an active/foreground dialog.
		// Never retry this dispatch: a timeout can mean the command already ran.
		Send(window, 0x0111, (UIntPtr)UnrealRunControlId, run);
		Stopwatch elapsed = Stopwatch.StartNew();
		while (elapsed.Elapsed < TimeSpan.FromSeconds(10))
		{
			Verify();
			if (watchLog)
			{
				string? token = outputLog!.ReadFreshToken(cancellationToken);
				if (token != null)
				{
					Verify();
					return token;
				}
			}
			else
			{
				int current = ItemCount(list);
				if (current < baseline) throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleTokenMissing);
				if (current > baseline)
				{
					string? line;
					try { line = SatisfactoryConsoleAccessibility.ReadAppendedTokenLine(list, current - baseline, boundary, cancellationToken); }
					catch (SatisfactoryApiException) when (ItemCount(list) != current) { continue; }
					if (current != ItemCount(list)) continue;
					if (line != null)
					{
						Verify();
						return SatisfactoryTokenParser.Extract(line);
					}
				}
			}
			if (cancellationToken.WaitHandle.WaitOne(100)) cancellationToken.ThrowIfCancellationRequested();
		}
		throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleTokenMissing);
	}

	// Satisfactory's FConsoleWindow uses an owner-drawn ListBox, not a details
	// ListView. Their count messages are different despite both appearing as a
	// "list" in accessibility trees. Keep support for either native implementation.
	private static bool IsLogList(IntPtr list) => HasClass(list, "ListBox") || HasClass(list, "SysListView32");
	private static int ItemCount(IntPtr list) => checked((int)Send(list,
		HasClass(list, "ListBox") ? 0x018Bu : 0x1004u, UIntPtr.Zero, IntPtr.Zero).ToUInt64());
	private static UIntPtr Send(IntPtr window, uint message, UIntPtr parameter, IntPtr value)
	{
		if (SendMessageTimeout(window, message, parameter, value, 0x22, 1000, out UIntPtr result) == IntPtr.Zero)
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleBusy);
		return result;
	}
	private static string ReadText(IntPtr input)
	{
		StringBuilder text = new(1025);
		if (ReadMessage(input, 0x000D, (UIntPtr)text.Capacity, text, 0x22, 1000, out _) == IntPtr.Zero)
			throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleUnavailable);
		return text.ToString();
	}
	private static bool HasClass(IntPtr window, string expected)
	{
		StringBuilder name = new(128);
		GetClassName(window, name, name.Capacity);
		return name.ToString() == expected;
	}

	private static bool HasWindowOwner(IntPtr window, int expectedProcessId)
	{
		GetWindowThreadProcessId(window, out uint processId);
		return processId == expectedProcessId;
	}

	internal static string ReadListToken(IntPtr list, CancellationToken cancellationToken)
		=> SatisfactoryTokenParser.Extract(ReadLatestTokenLine(list, cancellationToken));

	internal static string ReadLatestTokenLine(IntPtr list, CancellationToken cancellationToken)
		=> SatisfactoryConsoleAccessibility.ReadLatestTokenLine(list, cancellationToken);

	internal static bool MatchesLiveProcess(string installPath, ServerProcessIdentity identity)
	{
		try
		{
			if (identity.ProcessId <= 0 || !identity.StartTimeUtc.HasValue) return false;
			using Process process = Process.GetProcessById(identity.ProcessId);
			return !process.HasExited && MatchesIdentity(installPath, identity,
				process.MainModule?.FileName ?? "", process.StartTime.ToUniversalTime());
		}
		catch { return false; } // Fail closed. Never include process/window data in a token-reader error.
	}

	internal static bool MatchesIdentity(string installPath, ServerProcessIdentity identity, string actualPath, DateTime actualStartUtc)
	{
		if (identity.ProcessId <= 0 || string.IsNullOrWhiteSpace(installPath) || string.IsNullOrWhiteSpace(actualPath) ||
			string.IsNullOrWhiteSpace(identity.ExecutablePath) || identity.StartTimeUtc != actualStartUtc ||
			!Path.IsPathFullyQualified(installPath) || !Path.IsPathFullyQualified(actualPath) ||
			!Path.IsPathFullyQualified(identity.ExecutablePath)) return false;
		string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installPath)) + Path.DirectorySeparatorChar;
		if (string.Equals(Path.TrimEndingDirectorySeparator(root), Path.TrimEndingDirectorySeparator(Path.GetPathRoot(root) ?? ""),
			StringComparison.OrdinalIgnoreCase)) return false;
		string expected = Path.GetFullPath(identity.ExecutablePath);
		string actual = Path.GetFullPath(actualPath);
		string name = Path.GetFileName(actual);
		return actual.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase) &&
			(name.Equals("FactoryServer-Win64-Shipping-Cmd.exe", StringComparison.OrdinalIgnoreCase) ||
			 name.Equals("FactoryServer-Win64-Shipping.exe", StringComparison.OrdinalIgnoreCase) ||
			 name.Equals("FactoryServer.exe", StringComparison.OrdinalIgnoreCase));
	}

	private delegate bool EnumWindowProc(IntPtr window, IntPtr parameter);
	[DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumWindows(EnumWindowProc callback, IntPtr parameter);
	[DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
	[DllImport("user32.dll")] private static extern IntPtr GetDlgItem(IntPtr window, int controlId);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder className, int maximum);
	[DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowEnabled(IntPtr window);
	[DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
	private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, UIntPtr parameter, IntPtr value, uint flags, uint timeout, out UIntPtr result);
	[DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode)]
	private static extern IntPtr SendText(IntPtr window, uint message, UIntPtr parameter, string value, uint flags, uint timeout, out UIntPtr result);
	[DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode)]
	private static extern IntPtr ReadMessage(IntPtr window, uint message, UIntPtr parameter, StringBuilder value, uint flags, uint timeout, out UIntPtr result);
}
