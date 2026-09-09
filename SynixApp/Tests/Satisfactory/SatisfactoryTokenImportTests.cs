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
using System.Text;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;
using Synix_Control_Panel.SynixApp.UI.ServerManagement;
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class SatisfactoryTokenImportTests
{
	// Deliberately fabricated signatures. Never use a real server credential in fixtures.
	private static string Token(char signature = 'A', string privilege = "APIToken") =>
		Convert.ToBase64String(Encoding.UTF8.GetBytes("{\r\n\t\"pl\": \"" + privilege + "\"\r\n}")) + "." + new string(signature, 128);
	private static string Line(string token) => SatisfactoryTokenParser.ConsoleLabel + " " + token;

	[Theory]
	[InlineData("{0}")]
	[InlineData("New Server API Authentication Token: {0}\r\n")]
	[InlineData("[2026.09.05-10.00.00:000][1]LogServer: New Server API Authentication Token: {0}")]
	[InlineData("Server\tNew Server API Authentication Token: {0}")]
	[InlineData("\"{0}\"")]
	[InlineData("Other log line\r\nNew Server API Authentication Token: {0}\r\nAnother log line")]
	public void ExtractsOnlyTokenFromCompleteCopiedOutput(string template)
	{
		string token = Token();
		Assert.Equal(token, SatisfactoryTokenParser.Extract(string.Format(template, token)));
	}

	[Fact]
	public void RepeatedSameTokenIsAllowedButDifferentTokensAreAmbiguous()
	{
		Assert.Equal(Token(), SatisfactoryTokenParser.Extract(Line(Token()) + "\n" + Line(Token())));
		var error = Assert.Throws<SatisfactoryApiException>(() => SatisfactoryTokenParser.Extract(Line(Token()) + "\n" + Line(Token('B'))));
		Assert.Equal("Satisfactory.Error.AmbiguousToken", error.ResourceKey);
		Assert.DoesNotContain(Token(), error.ToString());
	}

	[Fact]
	public void InvalidCredentialsAreRejectedWithoutLeakingTheirInput()
	{
		foreach (string input in new[] { "", "admin-password", Token(privilege: "Administrator"), Token() + "G",
			Token() + ".extra", Token().Split('.')[0], new string('x', SatisfactoryTokenParser.MaximumInputLength + 1) })
		{
			var error = Assert.Throws<SatisfactoryApiException>(() => SatisfactoryTokenParser.Extract(input));
			Assert.Equal("Satisfactory.Error.Token", error.ResourceKey);
			Assert.DoesNotContain(Token(), error.ToString());
		}
	}

	[Fact]
	public void ProtocolValidationStaysStrictWhenImportAcceptsConsoleLines()
	{
		Assert.Equal(Token(), SatisfactoryApiClient.NormalizeToken(Token()));
		Assert.Throws<SatisfactoryApiException>(() => SatisfactoryApiClient.NormalizeToken(Line(Token())));
		Assert.Throws<SatisfactoryApiException>(() => SatisfactoryApiClient.NormalizeToken(Token().Split('.')[0] + ".not-hex"));
	}

	[Fact]
	public void SupportReportsRedactCopiedConsoleLinesAndBareTokens()
	{
		string sanitized = Core.SanitizeProblemReportText(Line(Token()) + "\n" + Token('B'));
		Assert.DoesNotContain(Token(), sanitized);
		Assert.DoesNotContain(Token('B'), sanitized);
		Assert.Contains("[secret removed]", sanitized);
	}

	[Theory]
	[InlineData("C:\\Servers\\One", "C:\\Servers\\One\\Engine\\Binaries\\Win64\\FactoryServer-Win64-Shipping-Cmd.exe", true)]
	[InlineData("C:\\Servers\\One", "C:\\Servers\\One-other\\FactoryServer-Win64-Shipping-Cmd.exe", false)]
	[InlineData("C:\\Servers\\One", "C:\\Servers\\Two\\FactoryServer-Win64-Shipping-Cmd.exe", false)]
	[InlineData("C:\\Servers\\One", "C:\\Servers\\One\\Unrelated.exe", false)]
	[InlineData("C:\\", "C:\\Servers\\One\\FactoryServer.exe", false)]
	[InlineData("Servers\\One", "C:\\Servers\\One\\FactoryServer.exe", false)]
	public void WindowReaderRequiresTheSelectedInstallAndAnExpectedExecutable(string install, string exe, bool matches)
	{
		DateTime started = DateTime.UtcNow;
		ServerProcessIdentity identity = new() { ProcessId = 123, ExecutablePath = exe, StartTimeUtc = started };
		Assert.Equal(matches, SatisfactoryConsoleTokenReader.MatchesIdentity(install, identity, exe, started));
		Assert.False(SatisfactoryConsoleTokenReader.MatchesIdentity(install, identity, exe, started.AddSeconds(1)));
		Assert.False(SatisfactoryConsoleTokenReader.MatchesIdentity(install, identity, exe + ".other", started));
	}

	[Fact]
	public async Task StoppedOrNonSatisfactoryServersCannotReadAnyWindow()
	{
		foreach (var server in new[] { new GameServer { Game = "Satisfactory" }, new GameServer { Game = "Minecraft" } })
			Assert.Equal("Satisfactory.Error.ConsoleUnavailable",
				(await Assert.ThrowsAsync<SatisfactoryApiException>(() => SatisfactoryConsoleTokenReader.GenerateAsync(server, default))).ResourceKey);
	}

	[Fact]
	public void NativeDetailsListReadsNewestTokenFromMessageColumnWithoutChangingSelection()
	{
		OnSta(() =>
		{
			using Form form = new() { StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000) };
			using NativeDetailsList list = new() { View = View.Details, Dock = DockStyle.Fill };
			list.Columns.Add("Category", 80);
			list.Columns.Add("Message", 600);
			list.Items.Add(new ListViewItem(new[] { "Server", Line(Token()) }));
			list.Items.Add(new ListViewItem(new[] { "Other", "Ordinary log output" }));
			list.Items.Add(new ListViewItem(new[] { "Server", Line(Token('B')) }));
			form.Controls.Add(list);
			form.Show();
			Application.DoEvents();
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
			IntPtr handle = list.Handle;
			Task<string> read = Task.Run(() => SatisfactoryConsoleTokenReader.ReadListToken(handle, timeout.Token));
			while (!read.IsCompleted && !timeout.IsCancellationRequested) { Application.DoEvents(); Thread.Sleep(5); }
			Assert.Equal(Token('B'), read.GetAwaiter().GetResult());
			Assert.Empty(list.SelectedIndices.Cast<int>());
			Assert.Equal(3, list.Items.Count);
			list.Items.Clear();
			Task<string> empty = Task.Run(() => SatisfactoryConsoleTokenReader.ReadListToken(handle, timeout.Token));
			while (!empty.IsCompleted && !timeout.IsCancellationRequested) { Application.DoEvents(); Thread.Sleep(5); }
			Assert.Equal("Satisfactory.Error.ConsoleTokenMissing",
				Assert.Throws<SatisfactoryApiException>(() => empty.GetAwaiter().GetResult()).ResourceKey);
		});
	}

	[Fact]
	public void CanceledReadDoesNotTouchAWindow()
	{
		using CancellationTokenSource source = new();
		source.Cancel();
		Assert.Throws<OperationCanceledException>(() => SatisfactoryConsoleTokenReader.ReadListToken(IntPtr.Zero, source.Token));
	}

	[Fact]
	public void StoppedServerCannotAutomaticallyConnectAndThereIsNoManualTokenField()
	{
		OnSta(() =>
		{
			using SatisfactoryControlDialog dialog = new(new GameServer { Game = "Satisfactory" });
			Assert.Empty(dialog.Controls.Find("satisfactoryToken", true));
			Assert.False(Assert.IsAssignableFrom<Button>(Assert.Single(dialog.Controls.Find("satisfactoryConnectAutomatically", true))).Enabled);
		});
	}

	private static void OnSta(Action action)
	{
		Exception? error = null;
		Thread thread = new(() => { try { action(); } catch (Exception exception) { error = exception; } }) { IsBackground = true };
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "The isolated UI test did not complete in time.");
		if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
	}

	// Exercise the additional details-list format with the OS provider, not the
	// WinForms-specific provider. The real Unreal ListBox is covered separately.
	private sealed class NativeDetailsList : ListView
	{
		protected override void WndProc(ref Message message)
		{
			if (message.Msg == 0x003D) DefWndProc(ref message);
			else base.WndProc(ref message);
		}
	}
}
