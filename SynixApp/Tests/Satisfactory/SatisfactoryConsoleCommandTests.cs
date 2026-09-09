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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;
using Xunit;
using static Synix_Control_Panel.SynixEngine.Core;

namespace Synix_Control_Panel.Tests;

public sealed class SatisfactoryConsoleCommandTests
{
	private static string Token(char signature) =>
		Convert.ToBase64String(Encoding.UTF8.GetBytes("{\r\n\t\"pl\": \"APIToken\"\r\n}")) + "." + new string(signature, 128);
	private static string Line(char signature) => SatisfactoryTokenParser.ConsoleLabel + " " + Token(signature);

	[Theory]
	[InlineData('A')]
	[InlineData('B')]
	public void OneClickCapturesTheFreshLogAndPersistsAnEncryptedConnectionWhenWindowRowsDoNotChange(char signature)
	{
		OnSta(() =>
		{
			using SatisfactoryTokenLogFixture log = new(Line('A') + "\r\n");
			using SatisfactoryTokenLogTail tail = Assert.IsType<SatisfactoryTokenLogTail>(SatisfactoryTokenLogTail.TryOpen(log.Root));
			using ConsoleFixture console = new();
			console.Show();
			console.AddLine(Line('A'));
			console.SetFilter("Errors"); // The game's output log is independent of UI filtering/recycling.
			console.OnCommand = () => log.Append("[2026.09.05-15.54.59:642][803]" + Line(signature) + "\r\n");
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
			string saved = "";
			GameServer server = new() { Game = "Satisfactory", Port = 7777, Status = StatusManager.GetStatus(ServerState.Running) };
			var steps = new SatisfactoryAutoConnect.Steps(_ => { }, (_, ct) => Generate(console, ct, outputLog: tail),
				(_, _) => Task.FromResult(new string('B', 64)),
				(_, token, _, _) =>
				{
					Assert.Equal(Token(signature), token);
					return Task.FromResult(new SatisfactoryServerState("Factory", 3, 10, true, false, 30, 100, 1));
				},
				(value, token, pin) => SatisfactoryIntegration.SaveConnection(value, token, pin, () =>
				{
					saved = SerializeServersForStorage([value]);
					return true;
				}));
			Task<SatisfactoryServerState> operation = SatisfactoryAutoConnect.ConnectAsync(server, timeout.Token, steps);
			Pump(operation, timeout);
			Assert.Equal(3, operation.GetAwaiter().GetResult().NumConnectedPlayers);
			Assert.Equal(1, console.Commands);
			Assert.True(IsProtected(server.AuthenticationToken));
			Assert.DoesNotContain(Token(signature), saved);
			GameServer restored = JsonSerializer.Deserialize<GameServer[]>(saved)![0];
			Assert.Equal(Token(signature), RevealServerPasswords(restored).AuthenticationToken);
			Assert.True(SatisfactoryIntegration.IsConnected(restored));
		});
	}

	[Theory]
	[InlineData(true, 'A')] // Satisfactory may issue the same token again; a fresh row is what matters.
	[InlineData(true, 'B')]
	[InlineData(false, 'A')]
	[InlineData(false, 'B')]
	public void SendsExactlyOneCommandAndCapturesTheNewFullToken(bool listBox, char signature)
	{
		OnSta(() =>
		{
			using ConsoleFixture console = new(listBox);
			console.Show();
			console.AddLine(Line('A'));
			console.AddLine("Existing log boundary");
			console.OnCommand = () => console.BeginInvoke(() => console.AddLine(Line(signature)));
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
			Task<string> operation = Generate(console, timeout.Token);
			Pump(operation, timeout);
			Assert.Equal(Token(signature), operation.GetAwaiter().GetResult());
			Assert.Equal(1, console.Commands);
			Assert.Equal(SatisfactoryApiClient.GenerateTokenCommand, console.LastCommand);
		});
	}

	[Theory]
	[InlineData("old-token")]
	[InlineData("cleared-log")]
	[InlineData("replaced-boundary")]
	[InlineData("command-draft")]
	[InlineData("filter")]
	[InlineData("wrong-owner")]
	[InlineData("disabled-command")]
	[InlineData("canceled")]
	public void DoesNotReuseOldTokensOverwriteDraftsOrSendToAnInvalidConsole(string failure)
	{
		OnSta(() =>
		{
			using ConsoleFixture console = new();
			console.Show();
			console.AddLine(Line('A'));
			console.AddLine("Existing log boundary");
			if (failure == "command-draft") console.SetDraft("save-game");
			if (failure == "filter") console.SetFilter("Errors");
			if (failure == "disabled-command") console.DisableRun();
			if (failure == "cleared-log") console.OnCommand = () => { console.ClearLines(); console.AddLine(Line('B')); };
			if (failure == "replaced-boundary") console.OnCommand = () =>
			{
				console.ClearLines();
				console.AddLine("Replacement row");
				console.AddLine("Different boundary at the same index");
				console.AddLine(Line('B'));
			};
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(1));
			if (failure == "canceled") timeout.Cancel();
			Task<string> operation = Generate(console, timeout.Token, failure != "wrong-owner");
			Pump(operation, timeout);
			Exception? error = Record.Exception(() => operation.GetAwaiter().GetResult());
			Assert.NotNull(error);
			Assert.DoesNotContain(Token('A'), error.ToString());
			Assert.DoesNotContain(Token('B'), error.ToString());
			Assert.Equal(failure is "old-token" or "cleared-log" or "replaced-boundary" ? 1 : 0, console.Commands);
			if (failure == "command-draft") Assert.Equal("save-game", console.Draft);
		});
	}

	private static Task<string> Generate(ConsoleFixture console, CancellationToken cancellationToken, bool matches = true,
		SatisfactoryTokenLogTail? outputLog = null)
	{
		IntPtr window = console.Handle;
		return Task.Run(() => SatisfactoryConsoleTokenReader.GenerateInConsole(window, Environment.ProcessId, () => matches, cancellationToken, outputLog));
	}
	private static void Pump(Task task, CancellationTokenSource timeout)
	{
		DateTime deadline = DateTime.UtcNow.AddSeconds(7);
		while (!task.IsCompleted && DateTime.UtcNow < deadline) { Application.DoEvents(); Thread.Sleep(5); }
		if (!task.IsCompleted) timeout.Cancel();
		Assert.True(task.IsCompleted, "The isolated native console operation did not finish.");
	}
	private static void OnSta(Action action)
	{
		Exception? error = null;
		Thread thread = new(() => { try { action(); } catch (Exception exception) { error = exception; } }) { IsBackground = true };
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		// Allow Windows to create/tear down its native accessibility provider on a
		// busy desktop; each connection itself still has a separate bounded timeout.
		Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "The isolated native UI fixture did not finish.");
		if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
	}

	// Only these test-owned native windows are touched. No live server, token,
	// clipboard or user console is accessed by this fixture.
	private sealed class ConsoleFixture : Form
	{
		private readonly bool _listBox;
		private IntPtr _list, _input, _run, _filter;
		private int _rows;
		internal int Commands { get; private set; }
		internal string LastCommand { get; private set; } = "";
		internal Action? OnCommand;
		internal string Draft { get { StringBuilder value = new(1025); GetWindowText(_input, value, value.Capacity); return value.ToString(); } }
		internal ConsoleFixture(bool listBox = true)
		{
			_listBox = listBox;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-32000, -32000);
		}
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			CommonControls controls = new() { Size = 8, Classes = 1 };
			Assert.True(InitCommonControlsEx(ref controls));
			// 0xD51 matches the installed Unreal console's owner-drawn ListBox:
			// LBS_OWNERDRAWFIXED | HASSTRINGS | NOTIFY | NOINTEGRALHEIGHT |
			// WANTKEYBOARDINPUT | EXTENDEDSEL. This is not a details ListView.
			_list = _listBox ? Child("ListBox", 34817, 0xD51) : Child("SysListView32", 34817, 1);
			_input = Child("Edit", 34820);
			_run = Child("Button", 34821);
			_filter = Child("Edit", 34818);
			if (!_listBox)
			{
				AddColumn(0, "Category");
				AddColumn(1, "Message");
			}
		}
		private IntPtr Child(string className, int id, uint style = 0)
		{
			IntPtr child = CreateWindowEx(0, className, "", 0x50000000 | style, 0, 0, 200, 150, Handle, (IntPtr)id, IntPtr.Zero, IntPtr.Zero);
			Assert.NotEqual(IntPtr.Zero, child);
			return child;
		}
		protected override void WndProc(ref Message message)
		{
			if (message.Msg == 0x0111 && message.WParam == (IntPtr)34821 && message.LParam == _run)
			{
				Commands++;
				LastCommand = Draft;
				OnCommand?.Invoke();
				message.Result = IntPtr.Zero;
				return;
			}
			base.WndProc(ref message);
		}
		internal void SetDraft(string value) => SetWindowText(_input, value);
		internal void SetFilter(string value) => SetWindowText(_filter, value);
		internal void DisableRun() => EnableWindow(_run, false);
		internal void ClearLines() { SendMessage(_list, _listBox ? 0x0184u : 0x1009u, IntPtr.Zero, IntPtr.Zero); _rows = 0; }
		internal void AddLine(string line)
		{
			if (_listBox) WithText("Server\t: " + line, pointer => SendMessage(_list, 0x0180, IntPtr.Zero, pointer));
			else
			{
				WithText("Server", pointer => SendStruct(_list, 0x104D, IntPtr.Zero, new ListItem { Mask = 1, Item = _rows, Text = pointer }));
				WithText(line, pointer => SendStruct(_list, 0x1074, (IntPtr)_rows, new ListItem { SubItem = 1, Text = pointer }));
			}
			_rows++;
		}
		private void AddColumn(int index, string title) => WithText(title, pointer =>
			SendStruct(_list, 0x1061, (IntPtr)index, new ListColumn { Mask = 6, Width = 600, Text = pointer }));
		private static void WithText(string text, Action<IntPtr> action)
		{
			IntPtr pointer = Marshal.StringToHGlobalUni(text);
			try { action(pointer); } finally { Marshal.FreeHGlobal(pointer); }
		}
		private static void SendStruct<T>(IntPtr window, uint message, IntPtr parameter, T value) where T : struct
		{
			IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
			try { Marshal.StructureToPtr(value, buffer, false); SendMessage(window, message, parameter, buffer); }
			finally { Marshal.FreeHGlobal(buffer); }
		}
	}
	[StructLayout(LayoutKind.Sequential)] private struct CommonControls { public uint Size, Classes; }
	[StructLayout(LayoutKind.Sequential)] private struct ListItem
	{
		public uint Mask; public int Item, SubItem; public uint State, StateMask; public IntPtr Text;
		public int TextMax, Image; public IntPtr Param; public int Indent, Group; public uint Columns;
		public IntPtr ColumnPointer, Formats; public int GroupIndex;
	}
	[StructLayout(LayoutKind.Sequential)] private struct ListColumn
	{
		public uint Mask; public int Format, Width; public IntPtr Text; public int TextMax, SubItem, Image, Order, Min, Default, Ideal;
	}
	[DllImport("comctl32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool InitCommonControlsEx(ref CommonControls controls);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateWindowEx(uint extended, string className, string text, uint style,
		int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr parameter, IntPtr value);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetWindowText(IntPtr window, string text);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int length);
	[DllImport("user32.dll")] private static extern bool EnableWindow(IntPtr window, bool enabled);
}
