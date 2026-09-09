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
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using static Synix_Control_Panel.SynixEngine.Core;

namespace Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;

internal static class SatisfactoryIntegration
{
	internal static bool IsConnected(GameServer server) =>
		GameDatabase.IsSatisfactory(server.Game) &&
		!string.IsNullOrWhiteSpace(server.AuthenticationToken) &&
		SatisfactoryApiClient.IsFingerprint(server.SatisfactoryCertificateFingerprint);

	internal static bool IsLive(GameServer server) => server.Status == StatusManager.GetStatus(ServerState.Running) ||
		server.Status == StatusManager.GetStatus(ServerState.Starting);

	internal static bool HasFreshPlayerCount(GameServer server) => IsConnected(server) &&
		IsLive(server) && server.SatisfactoryLastSuccessUtc is DateTime success &&
		DateTime.UtcNow - success < TimeSpan.FromSeconds(45) &&
		(!server.StartTime.HasValue || success >= server.StartTime.Value.ToUniversalTime());

	internal static SatisfactoryApiClient CreateClient(GameServer server) =>
		new(server.Port, RevealServerPasswords(server).AuthenticationToken,
			server.SatisfactoryCertificateFingerprint);

	internal static void RecordState(GameServer server, SatisfactoryServerState state)
	{
		server.SatisfactoryState = state;
		server.CurrentPlayers = state.NumConnectedPlayers;
		server.MaxPlayersFromQuery = state.PlayerLimit;
		server.SatisfactoryApiErrorKey = string.Empty;
		server.SatisfactoryLastSuccessUtc = DateTime.UtcNow;
	}

	internal static void ClearState(GameServer server)
	{
		server.SatisfactoryState = null;
		server.SatisfactoryLastAttemptUtc = null;
		server.SatisfactoryLastSuccessUtc = null;
		server.SatisfactoryApiErrorKey = string.Empty;
	}

	internal static async Task PollAsync(GameServer server)
	{
		if (!IsConnected(server) || !IsLive(server)) return;
		int interval = string.IsNullOrEmpty(server.SatisfactoryApiErrorKey) ? 15 : 60;
		if (server.SatisfactoryLastAttemptUtc is DateTime last &&
			DateTime.UtcNow - last < TimeSpan.FromSeconds(interval)) return;
		server.SatisfactoryLastAttemptUtc = DateTime.UtcNow;
		string storedToken = server.AuthenticationToken;
		int port = server.Port;
		DateTime? started = server.StartTime;
		try
		{
			using SatisfactoryApiClient client = CreateClient(server);
			SatisfactoryServerState state = await client.QueryStateAsync();
			if (IsLive(server) && storedToken == server.AuthenticationToken && port == server.Port && started == server.StartTime)
				RecordState(server, state);
		}
		catch (Exception exception)
		{
			string key = SafeErrorKey(exception);
			server.SatisfactoryLastSuccessUtc = null;
			if (server.SatisfactoryApiErrorKey != key)
				LogFailure(key);
			server.SatisfactoryApiErrorKey = key;
			// An API/authentication failure is not evidence that the game crashed.
			// Keep the last count internally for conservative maintenance decisions.
		}
	}

	internal static async Task<bool> TryShutdownAsync(GameServer server)
	{
		if (!IsConnected(server)) return false;
		try
		{
			// Only the existing stop pipeline may call this after marking Stopping.
			if (server.Status != StatusManager.GetStatus(ServerState.Stopping)) return false;
			using SatisfactoryApiClient client = CreateClient(server);
			await client.CallAsync("Shutdown");
			ClearState(server);
			return true;
		}
		catch (Exception exception)
		{
			LogFailure(SafeErrorKey(exception));
			return false;
		}
	}

	internal static string SafeErrorKey(Exception exception) => exception is SatisfactoryApiException api
		? api.ResourceKey : exception is SynixPasswordProtectionException
			? "Satisfactory.Error.ProtectedToken" : "Satisfactory.Error.Unavailable";

	internal static void LogFailure(string resourceKey) =>
		ApplicationLogService.WriteSuppressedException(new InvalidOperationException(
			LocalizationManager.GetEnglish(resourceKey)));

	internal static bool SaveConnection(GameServer server, string token, string fingerprint,
		Func<bool>? persist = null)
	{
		if (!GameDatabase.IsSatisfactory(server.Game)) throw new ArgumentException(nameof(server));
		if (token.Length > 0)
		{
			token = SatisfactoryApiClient.NormalizeToken(token);
			if (!SatisfactoryApiClient.IsFingerprint(fingerprint))
				throw new SatisfactoryApiException(SatisfactoryApiError.Certificate);
		}
		else fingerprint = string.Empty;
		string previousToken = server.AuthenticationToken;
		string previousFingerprint = server.SatisfactoryCertificateFingerprint;
		var previousSecrets = (server.Password, server.AdminPassword, server.RconPassword,
			server.DiscordWebhook, server.PasswordStorageVersion);
		void RollBack()
		{
			server.AuthenticationToken = previousToken;
			server.SatisfactoryCertificateFingerprint = previousFingerprint;
			(server.Password, server.AdminPassword, server.RconPassword,
				server.DiscordWebhook, server.PasswordStorageVersion) = previousSecrets;
		}
		// The existing storage format already encrypts AuthenticationToken with DPAPI.
		// No token is written into game configs or command-line arguments.
		bool saved;
		try
		{
			if (server.PasswordStorageVersion < 4)
				SetServerPasswords(server, RevealServerPasswords(server));
			server.AuthenticationToken = Protect(token);
			server.SatisfactoryCertificateFingerprint = fingerprint;
			saved = (persist ?? FileHandler.SaveServers)();
		}
		catch
		{
			RollBack();
			throw;
		}
		if (!saved)
		{
			RollBack();
			return false;
		}
		ClearState(server);
		return true;
	}
}
