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
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	internal sealed record MinecraftManagedPlayer(string Id, string Name);

	internal sealed record MinecraftManagementResult<T>(
		bool Succeeded,
		T? Value,
		string Problem);

	internal static class MinecraftManagementClient
	{
		private const int MaximumResponseBytes = 1024 * 1024;

		internal static async Task<MinecraftManagementResult<IReadOnlyList<MinecraftManagedPlayer>>>
			QueryPlayersAsync(
				GameServer server,
				CancellationToken cancellationToken = default)
		{
			MinecraftManagementResult<JsonElement> response = await CallAsync(
				server,
				"minecraft:players",
				parameters: null,
				cancellationToken);
			if (!response.Succeeded)
				return new(false, null, response.Problem);

			try
			{
				JsonElement players = response.Value;
				if (players.ValueKind == JsonValueKind.Object &&
					players.TryGetProperty("players", out JsonElement nestedPlayers))
				{
					players = nestedPlayers;
				}

				if (players.ValueKind != JsonValueKind.Array)
					return new(false, null, LocalizationManager.Get("Minecraft.Management.PlayerListFormat"));

				List<MinecraftManagedPlayer> result = [];
				foreach (JsonElement player in players.EnumerateArray())
				{
					if (player.ValueKind != JsonValueKind.Object ||
						!player.TryGetProperty("name", out JsonElement nameElement))
					{
						continue;
					}

					string name = nameElement.GetString()?.Trim() ?? string.Empty;
					if (name.Length == 0)
						continue;

					string id = player.TryGetProperty("id", out JsonElement idElement)
						? idElement.GetString() ?? string.Empty
						: string.Empty;
					result.Add(new MinecraftManagedPlayer(id, name));
				}

				return new(true, result, string.Empty);
			}
			catch (Exception exception) when (
				exception is InvalidOperationException or JsonException)
			{
				return new(false, null, LocalizationManager.Get("Minecraft.Management.PlayerDataInvalid", exception.Message));
			}
		}

		internal static async Task<MinecraftManagementResult<bool>> StopAsync(
			GameServer server,
			CancellationToken cancellationToken = default)
		{
			MinecraftManagementResult<JsonElement> response = await CallAsync(
				server,
				"minecraft:server/stop",
				parameters: null,
				cancellationToken);
			return response.Succeeded
				? new(true, true, string.Empty)
				: new(false, false, response.Problem);
		}

		private static async Task<MinecraftManagementResult<JsonElement>> CallAsync(
			GameServer server,
			string method,
			object? parameters,
			CancellationToken cancellationToken)
		{
			if (!MinecraftControlProfile.TryLoadManagementSettings(
				server,
				out MinecraftManagementSettings? settings,
				out string settingsProblem) || settings == null)
			{
				return new(false, default, settingsProblem);
			}

			using CancellationTokenSource timeout =
				CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			timeout.CancelAfter(TimeSpan.FromSeconds(5));
			try
			{
				using ClientWebSocket socket = new();
				socket.Options.SetRequestHeader(
					"Authorization",
					$"Bearer {settings.Secret}");
				socket.Options.AddSubProtocol("minecraft-v1");
				string scheme = settings.TlsEnabled ? "wss" : "ws";
				Uri endpoint = new($"{scheme}://{settings.Host}:{settings.Port}/");
				await socket.ConnectAsync(endpoint, timeout.Token);

				const int requestId = 1;
				Dictionary<string, object?> request = new()
				{
					["jsonrpc"] = "2.0",
					["id"] = requestId,
					["method"] = method
				};
				if (parameters != null)
					request["params"] = parameters;

				byte[] json = JsonSerializer.SerializeToUtf8Bytes(request);
				await socket.SendAsync(
					json,
					WebSocketMessageType.Text,
					endOfMessage: true,
					timeout.Token);

				while (socket.State == WebSocketState.Open)
				{
					byte[] responseBytes = await ReceiveMessageAsync(socket, timeout.Token);
					using JsonDocument document = JsonDocument.Parse(responseBytes);
					JsonElement root = document.RootElement;
					if (!root.TryGetProperty("id", out JsonElement idElement) ||
						idElement.ValueKind != JsonValueKind.Number ||
						idElement.GetInt32() != requestId)
					{
						continue;
					}

					if (root.TryGetProperty("error", out JsonElement error))
					{
						string message = error.TryGetProperty("message", out JsonElement messageElement)
							? messageElement.GetString() ?? string.Empty
							: string.Empty;
						return new(
							false,
							default,
							message.Length > 0
								? LocalizationManager.Get("Minecraft.Management.RequestRejectedDetail", message)
								: LocalizationManager.Get("Minecraft.Management.RequestRejected"));
					}

					if (!root.TryGetProperty("result", out JsonElement result))
						return new(false, default, LocalizationManager.Get("Minecraft.Management.ResultMissing"));

					return new(true, result.Clone(), string.Empty);
				}

				return new(false, default, LocalizationManager.Get("Minecraft.Management.ConnectionClosed"));
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return new(false, default, LocalizationManager.Get("Minecraft.Management.Timeout"));
			}
			catch (Exception exception) when (
				exception is WebSocketException or IOException or JsonException or UriFormatException)
			{
				return new(false, default, LocalizationManager.Get("Minecraft.Management.ConnectionUnavailable", exception.Message));
			}
		}

		private static async Task<byte[]> ReceiveMessageAsync(
			ClientWebSocket socket,
			CancellationToken cancellationToken)
		{
			using MemoryStream content = new();
			byte[] buffer = new byte[8192];
			while (true)
			{
				WebSocketReceiveResult received = await socket.ReceiveAsync(
					buffer,
					cancellationToken);
				if (received.MessageType == WebSocketMessageType.Close)
					throw new IOException(LocalizationManager.Get("Minecraft.Management.ConnectionClosed"));
				if (received.MessageType != WebSocketMessageType.Text)
					continue;

				content.Write(buffer, 0, received.Count);
				if (content.Length > MaximumResponseBytes)
					throw new InvalidDataException(LocalizationManager.Get("Minecraft.Management.ResponseTooLarge"));
				if (received.EndOfMessage)
					return content.ToArray();
			}
		}
	}
}
