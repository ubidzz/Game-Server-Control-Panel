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
using System.Text.Json;
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;
using Xunit;
using static Synix_Control_Panel.SynixEngine.Core;

namespace Synix_Control_Panel.Tests;

public sealed class SatisfactoryLiveConnectionTests
{
	// Explicitly opt in on a developer's machine. No window automation, token
	// generation, server changes, or real settings writes. Secrets stay in memory.
	[LocalSatisfactoryFact]
	public async Task ExistingLocalResponseAuthenticatesAndRoundTripsThroughEncryptedStorage()
	{
		string serversPath = Environment.GetEnvironmentVariable("SYNIX_SATISFACTORY_READONLY_SERVERS") ?? throw new InvalidOperationException("Select a server file.");
		string install = Environment.GetEnvironmentVariable("SYNIX_SATISFACTORY_READONLY_INSTALL") ?? throw new InvalidOperationException("Select a server installation.");
		GameServer[] servers = JsonSerializer.Deserialize<GameServer[]>(await File.ReadAllTextAsync(serversPath))!;
		GameServer server = Assert.Single(servers, value => GameDatabase.IsSatisfactory(value.Game) &&
			string.Equals(Path.GetFullPath(value.InstallPath), Path.GetFullPath(install), StringComparison.OrdinalIgnoreCase));
		SatisfactoryLocalApiIdentity.Verify(server);
		using SatisfactoryTokenLogTail? tail = SatisfactoryTokenLogTail.TryOpen(server.InstallPath);
		Assert.True(tail != null && tail.CaptureStart(), "The selected server's output log must be readable.");
		Assert.True(tail!.ReadFreshToken(default) is null); // Existing responses must not count as new.
		string logPath = Path.Combine(server.InstallPath, "FactoryGame", "Saved", "Logs", "FactoryGame.log");
		using FileStream stream = new(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		stream.Position = Math.Max(0, stream.Length - 65536);
		using StreamReader reader = new(stream);
		string output = await reader.ReadToEndAsync();
		string line = output.Split('\n').Last(value => value.Contains(SatisfactoryTokenParser.ConsoleLabel, StringComparison.Ordinal));
		string token = SatisfactoryTokenParser.Extract(line);
		output = line = string.Empty;
		using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
		string fingerprint = await SatisfactoryApiClient.InspectCertificateAsync(server.Port, timeout.Token);
		SatisfactoryLocalApiIdentity.Verify(server);
		using SatisfactoryApiClient client = new(server.Port, token, fingerprint);
		SatisfactoryServerState state = await client.QueryStateAsync(timeout.Token);
		Assert.True(state.NumConnectedPlayers >= 0);
		SatisfactoryLocalApiIdentity.Verify(server);
		string savedInMemory = "";
		Assert.True(SatisfactoryIntegration.SaveConnection(server, token, fingerprint, () =>
		{
			savedInMemory = SerializeServersForStorage([server]);
			return true;
		}));
		Assert.True(IsProtected(server.AuthenticationToken));
		// Boolean assertions intentionally prevent xUnit from printing a credential
		// if this manually enabled check ever fails.
		Assert.False(savedInMemory.Contains(token, StringComparison.Ordinal));
		GameServer restored = JsonSerializer.Deserialize<GameServer[]>(savedInMemory)![0];
		Assert.True(RevealServerPasswords(restored).AuthenticationToken == token);
		Assert.True(SatisfactoryIntegration.IsConnected(restored));
		token = savedInMemory = string.Empty;
	}
}

public sealed class LocalSatisfactoryFactAttribute : FactAttribute
{
	public LocalSatisfactoryFactAttribute()
	{
		if (Environment.GetEnvironmentVariable("SYNIX_SATISFACTORY_READONLY_CHECK") != "1")
			Skip = "Opt-in read-only check against a selected local server; never run by default or in CI.";
	}
}
