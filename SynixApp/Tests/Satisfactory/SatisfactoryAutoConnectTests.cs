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
using System.Net;
using System.Net.Sockets;
using System.Text;
using Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;
using Synix_Control_Panel.SynixEngine;
using Xunit;
using static Synix_Control_Panel.SynixEngine.Core;

namespace Synix_Control_Panel.Tests;

public sealed class SatisfactoryAutoConnectTests
{
	private static string Token => Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"pl\":\"APIToken\"}")) + "." + new string('A', 128);
	private static string Pin => new('B', 64);
	private static GameServer Server() => new() { Game = "Satisfactory", Port = 7777,
		Status = StatusManager.GetStatus(ServerState.Running), StartTime = DateTime.Now, AuthenticationToken = "previous-connection" };
	private static SatisfactoryServerState State => new("Factory", 3, 10, true, false, 30, 100, 1);

	[Fact]
	public async Task OneOperationGeneratesVerifiesAndSavesInOrderWithoutAUserPrompt()
	{
		GameServer server = Server();
		List<string> actions = [];
		var steps = new SatisfactoryAutoConnect.Steps(
			_ => actions.Add("identity"),
			(_, _) => { actions.Add("generate"); return Task.FromResult(Token); },
			(_, _) => { actions.Add("certificate"); return Task.FromResult(Pin); },
			(port, token, pin, _) => { Assert.Equal(7777, port); Assert.Equal(Token, token); Assert.Equal(Pin, pin); actions.Add("verify-token"); return Task.FromResult(State); },
			(_, token, pin) => { Assert.Equal(Token, token); Assert.Equal(Pin, pin); actions.Add("save"); return true; });
		Assert.Equal(State, await SatisfactoryAutoConnect.ConnectAsync(server, default, steps));
		Assert.Equal(new[] { "identity", "generate", "identity", "certificate", "identity", "verify-token", "identity", "save" }, actions);
		Assert.Equal(3, server.CurrentPlayers);
	}

	[Theory]
	[InlineData("identity")]
	[InlineData("generation")]
	[InlineData("certificate")]
	[InlineData("authentication")]
	[InlineData("changed-server")]
	[InlineData("changed-listener")]
	[InlineData("canceled")]
	public async Task FailedConnectionKeepsPreviousTokenAndNeverSavesOrRetries(string failure)
	{
		GameServer server = Server();
		int sends = 0, apiCalls = 0, saves = 0, identities = 0;
		using CancellationTokenSource cancellation = new();
		var steps = new SatisfactoryAutoConnect.Steps(
			_ => { if (failure == "identity" || failure == "changed-listener" && ++identities == 3) throw new SatisfactoryApiException(SatisfactoryApiError.LocalIdentity); },
			(_, _) => { sends++; if (failure == "generation") throw new SatisfactoryApiException(SatisfactoryApiError.ConsoleTokenMissing); return Task.FromResult(Token); },
			(_, _) => Task.FromResult(failure == "certificate" ? "invalid" : Pin),
			(_, _, _, _) =>
			{
				apiCalls++;
				if (failure == "authentication") throw new SatisfactoryApiException(SatisfactoryApiError.Authentication);
				if (failure == "changed-server") server.Port++;
				if (failure == "canceled") cancellation.Cancel();
				return Task.FromResult(State);
			},
			(_, _, _) => { saves++; return true; });
		await Assert.ThrowsAnyAsync<Exception>(() => SatisfactoryAutoConnect.ConnectAsync(server, cancellation.Token, steps));
		Assert.Equal("previous-connection", server.AuthenticationToken);
		Assert.Null(server.SatisfactoryState);
		Assert.Equal(0, saves);
		Assert.InRange(sends, 0, 1);
		if (failure is "identity" or "changed-listener") Assert.Equal(0, apiCalls);
	}

	[Fact]
	public async Task SaveFailureDoesNotReportSuccessfulConnection()
	{
		GameServer server = Server();
		var steps = new SatisfactoryAutoConnect.Steps(_ => { }, (_, _) => Task.FromResult(Token), (_, _) => Task.FromResult(Pin),
			(_, _, _, _) => Task.FromResult(State), (_, _, _) => false);
		Assert.Equal("Satisfactory.Error.SaveConnection",
			(await Assert.ThrowsAsync<SatisfactoryApiException>(() => SatisfactoryAutoConnect.ConnectAsync(server, default, steps))).ResourceKey);
		Assert.Null(server.SatisfactoryState);
		Assert.Equal("previous-connection", server.AuthenticationToken);
	}

	[Fact]
	public void ListenerLookupIdentifiesTheExactLoopbackPortOwner()
	{
		using TcpListener listener = new(IPAddress.Loopback, 0);
		listener.Start();
		int port = ((IPEndPoint)listener.LocalEndpoint).Port;
		Assert.Contains(Environment.ProcessId, SatisfactoryLocalApiIdentity.ListenerOwners(port));
		GameServer server = Server();
		server.Port = port;
		Assert.Equal("Satisfactory.Error.LocalIdentity", Assert.Throws<SatisfactoryApiException>(() => SatisfactoryLocalApiIdentity.Verify(server)).ResourceKey);
	}
}
