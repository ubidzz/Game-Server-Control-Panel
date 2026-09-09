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
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixApp.ServerHandler;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Synix_Control_Panel.SynixEngine
{
	internal sealed record GamePlayerInfo(
		string Name,
		int Score,
		TimeSpan ConnectedFor);

	internal sealed record PlayerQueryResult(
		bool IsSupported,
		bool IsSuccessful,
		string Message,
		IReadOnlyList<GamePlayerInfo> Players);

	internal static class PlayerQueryService
	{
		private static readonly byte[] PlayerChallengeRequest =
			[0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF];

		internal static async Task<PlayerQueryResult> QueryAsync(
			GameServer server,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(server);
			GameInfo? game = GameDatabase.GetGame(server.Game);
			if (game == null)
				return Unsupported(LocalizationManager.Get(
					"PlayerQuery.GameDefinitionUnavailable"));

			if (GameCapabilityResolver.UsesMinecraftPlayers(server))
				return await QueryMinecraftAsync(server, cancellationToken);

			if (!GameDatabase.SupportsPlayerManagement(server))
			{
				return Unsupported(GameDatabase.IsPlayerTrackingDisabledByCrossplay(server)
					? LocalizationManager.Get("PlayerQuery.CrossplayUnavailable")
					: LocalizationManager.Get("PlayerQuery.ProtocolUnavailable"));
			}

			ServerProbeProtocol protocol = GameDatabase.GetProbeProtocol(game);
			if (protocol != ServerProbeProtocol.A2S)
			{
				string message = GameCapabilityResolver.UsesMinecraftPlayers(server)
					? LocalizationManager.Get(
						"PlayerQuery.MinecraftCountOnly",
						server.CurrentPlayers)
					: LocalizationManager.Get("PlayerQuery.ProtocolUnavailable");
				return Unsupported(message);
			}

			if (server.Status != Core.StatusManager.GetStatus(Core.ServerState.Running))
				return new(true, false, LocalizationManager.Get(
					"PlayerQuery.StartServerFirst"), []);

			int port = server.QueryPort > 0 ? server.QueryPort : server.Port;
			try
			{
				using CancellationTokenSource timeout =
					CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeout.CancelAfter(TimeSpan.FromSeconds(3));
				using UdpClient client = new(AddressFamily.InterNetwork);
				client.Connect(IPAddress.Loopback, port);

				await client.SendAsync(PlayerChallengeRequest, timeout.Token);
				UdpReceiveResult received = await client.ReceiveAsync(timeout.Token);
				byte[] response = received.Buffer;
				for (int challengeAttempt = 0; challengeAttempt < 2; challengeAttempt++)
				{
					if (!TryReadPayloadHeader(response, out byte responseType, out int payloadOffset))
						return new(true, false, LocalizationManager.Get(
							"PlayerQuery.InvalidA2sResponse"), []);

					// Some compatible servers accept the legacy -1 challenge and return
					// A2S_PLAYER data immediately. Others require the challenge exchange.
					if (responseType == 0x44)
						break;

					if (responseType != 0x41 || response.Length < payloadOffset + 4)
						return new(true, false, LocalizationManager.Get(
							"PlayerQuery.IncompatiblePlayerList"), []);

					byte[] request = (byte[])PlayerChallengeRequest.Clone();
					response.AsSpan(payloadOffset, 4).CopyTo(request.AsSpan(5, 4));
					await client.SendAsync(request, timeout.Token);
					response = (await client.ReceiveAsync(timeout.Token)).Buffer;
				}

				IReadOnlyList<GamePlayerInfo> players = ParsePlayerResponse(response);
				return new(
					true,
					true,
					players.Count == 0
						? LocalizationManager.Get("PlayerQuery.NoNamedPlayers")
						: LocalizationManager.Get(
							"PlayerQuery.LoadedPlayers",
							players.Count),
					players);
			}
			catch (OperationCanceledException)
			{
				return new(true, false, LocalizationManager.Get(
					"PlayerQuery.Timeout",
					port), []);
			}
			catch (SocketException exception)
			{
				return new(true, false, LocalizationManager.Get(
					"PlayerQuery.ConnectionFailed",
					exception.Message), []);
			}
			catch (Exception exception)
			{
				return new(true, false, LocalizationManager.Get(
					"PlayerQuery.ReadFailed",
					exception.Message), []);
			}
		}

		private static async Task<PlayerQueryResult> QueryMinecraftAsync(
			GameServer server,
			CancellationToken cancellationToken)
		{
			if (MinecraftControlProfile.IsBedrock(server))
			{
				return Unsupported(
					LocalizationManager.Get(
						"PlayerQuery.BedrockCountOnly",
						server.CurrentPlayers));
			}

			if (server.Status != Core.StatusManager.GetStatus(Core.ServerState.Running))
				return new(true, false, LocalizationManager.Get(
					"PlayerQuery.StartServerFirst"), []);

			MinecraftManagementResult<IReadOnlyList<MinecraftManagedPlayer>> managed =
				await MinecraftManagementClient.QueryPlayersAsync(server, cancellationToken);
			if (managed.Succeeded && managed.Value != null)
			{
				IReadOnlyList<GamePlayerInfo> players = managed.Value
					.Select(player => new GamePlayerInfo(player.Name, 0, TimeSpan.Zero))
					.ToArray();
				return new(
					true,
					true,
					players.Count == 0
						? LocalizationManager.Get(
							"PlayerQuery.MinecraftManagement.None")
						: LocalizationManager.Get(
							"PlayerQuery.MinecraftManagement.Loaded",
							players.Count),
					players);
			}

			MinecraftRconResult rcon = await MinecraftRconClient.ExecuteCommandAsync(
				server,
				"list",
				cancellationToken);
			if (rcon.Succeeded)
			{
				IReadOnlyList<GamePlayerInfo> players = MinecraftRconClient
					.ParsePlayerNames(rcon.Response)
					.Select(name => new GamePlayerInfo(name, 0, TimeSpan.Zero))
					.ToArray();
				return new(
					true,
					true,
					players.Count == 0
						? LocalizationManager.Get("PlayerQuery.MinecraftRcon.None")
						: LocalizationManager.Get(
							"PlayerQuery.MinecraftRcon.Loaded",
							players.Count),
					players);
			}

			string problem = string.Join(
				" ",
				new[] { managed.Problem, rcon.Problem }
					.Where(value => !string.IsNullOrWhiteSpace(value)));
			return new(
				true,
				false,
				string.IsNullOrWhiteSpace(problem)
					? LocalizationManager.Get("PlayerQuery.MinecraftUnavailable")
					: problem,
				[]);
		}

		internal static IReadOnlyList<GamePlayerInfo> ParsePlayerResponse(
			ReadOnlySpan<byte> response)
		{
			if (!TryReadPayloadHeader(response, out byte responseType, out int offset) ||
				responseType != 0x44 ||
				offset >= response.Length)
			{
				throw new InvalidDataException(LocalizationManager.Get(
					"PlayerQuery.InvalidA2sResponse"));
			}

			int count = response[offset++];
			List<GamePlayerInfo> players = new(count);
			for (int index = 0; index < count; index++)
			{
				if (offset >= response.Length)
					throw new InvalidDataException(LocalizationManager.Get(
						"PlayerQuery.ResponseUnexpectedEnd"));

				offset++; // Player index supplied by the server.
				int nameEnd = response[offset..].IndexOf((byte)0);
				if (nameEnd < 0)
					throw new InvalidDataException(LocalizationManager.Get(
						"PlayerQuery.NameNotTerminated"));

				string name = Encoding.UTF8.GetString(response.Slice(offset, nameEnd));
				offset += nameEnd + 1;
				if (response.Length < offset + 8)
					throw new InvalidDataException(LocalizationManager.Get(
						"PlayerQuery.RecordIncomplete"));

				int score = BinaryPrimitives.ReadInt32LittleEndian(response.Slice(offset, 4));
				offset += 4;
				int durationBits = BinaryPrimitives.ReadInt32LittleEndian(response.Slice(offset, 4));
				offset += 4;
				float seconds = BitConverter.Int32BitsToSingle(durationBits);
				players.Add(new(
					string.IsNullOrWhiteSpace(name)
						? LocalizationManager.Get("PlayerQuery.UnnamedPlayer")
						: name,
					score,
					TimeSpan.FromSeconds(Math.Max(0, seconds))));
			}

			return players;
		}

		private static PlayerQueryResult Unsupported(string message) =>
			new(false, false, message, []);

		private static bool TryReadPayloadHeader(
			ReadOnlySpan<byte> response,
			out byte responseType,
			out int payloadOffset)
		{
			responseType = 0;
			payloadOffset = 0;
			if (response.Length < 5 ||
				response[0] != 0xFF || response[1] != 0xFF ||
				response[2] != 0xFF || response[3] != 0xFF)
			{
				return false;
			}

			responseType = response[4];
			payloadOffset = 5;
			return true;
		}
	}
}
