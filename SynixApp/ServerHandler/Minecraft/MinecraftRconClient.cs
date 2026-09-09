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
using Synix_Control_Panel.SynixEngine;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	internal sealed record MinecraftRconResult(
		bool Succeeded,
		string Response,
		string Problem);

	internal static class MinecraftRconClient
	{
		private const int AuthPacketType = 3;
		private const int CommandPacketType = 2;
		private const int ResponsePacketType = 0;
		private const int MaximumPacketLength = 4 * 1024 * 1024;

		internal static async Task<MinecraftRconResult> ExecuteCommandAsync(
			GameServer server,
			string command,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(server);
			if (!server.EnableRcon || server.RconPort is < 1 or > 65535)
				return new(false, string.Empty, LocalizationManager.Get("Minecraft.Rcon.Disabled"));

			string password;
			try
			{
				password = Core.RevealServerPasswords(server).RconPassword;
			}
			catch (SynixPasswordProtectionException exception)
			{
				return new(false, string.Empty, exception.Message);
			}

			if (string.IsNullOrWhiteSpace(password))
				return new(false, string.Empty, LocalizationManager.Get("Minecraft.Rcon.PasswordRequired"));

			using CancellationTokenSource timeout =
				CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			timeout.CancelAfter(TimeSpan.FromSeconds(5));
			try
			{
				using TcpClient client = new(AddressFamily.InterNetwork);
				await client.ConnectAsync(IPAddress.Loopback, server.RconPort, timeout.Token);
				using NetworkStream stream = client.GetStream();

				const int authenticationId = 73_101;
				await WritePacketAsync(
					stream,
					authenticationId,
					AuthPacketType,
					password,
					timeout.Token);

				RconPacket authentication = await ReadAuthenticationResponseAsync(
					stream,
					authenticationId,
					timeout.Token);
				if (authentication.RequestId == -1)
					return new(false, string.Empty, LocalizationManager.Get("Minecraft.Rcon.PasswordRejected"));

				const int commandId = 73_102;
				await WritePacketAsync(
					stream,
					commandId,
					CommandPacketType,
					command,
					timeout.Token);

				try
				{
					RconPacket response = await ReadPacketAsync(stream, timeout.Token);
					if (response.RequestId != commandId)
						return new(false, string.Empty, LocalizationManager.Get("Minecraft.Rcon.UnrelatedResponse"));

					return new(true, response.Body, string.Empty);
				}
				catch (EndOfStreamException) when (
					command.Equals("stop", StringComparison.OrdinalIgnoreCase))
				{
					return new(true, string.Empty, string.Empty);
				}
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return new(false, string.Empty, LocalizationManager.Get("Minecraft.Rcon.Timeout", server.RconPort));
			}
			catch (SocketException exception)
			{
				return new(false, string.Empty, LocalizationManager.Get("Minecraft.Rcon.ConnectionFailed", exception.Message));
			}
			catch (Exception exception) when (
				exception is IOException or InvalidDataException)
			{
				return new(false, string.Empty, LocalizationManager.Get("Minecraft.Rcon.InvalidResponse", exception.Message));
			}
		}

		internal static IReadOnlyList<string> ParsePlayerNames(string response)
		{
			if (string.IsNullOrWhiteSpace(response))
				return [];

			int separator = response.LastIndexOf(':');
			if (separator < 0 || separator == response.Length - 1)
				return [];

			return response[(separator + 1)..]
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(IsSafePlayerName)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		internal static bool IsSafePlayerName(string? value)
		{
			if (string.IsNullOrWhiteSpace(value) || value.Length > 16)
				return false;

			return value.All(character =>
				char.IsAsciiLetterOrDigit(character) || character == '_');
		}

		internal static byte[] EncodePacket(int requestId, int packetType, string body)
		{
			byte[] text = Encoding.UTF8.GetBytes(body ?? string.Empty);
			byte[] packet = new byte[4 + 4 + 4 + text.Length + 2];
			BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), packet.Length - 4);
			BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4, 4), requestId);
			BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), packetType);
			text.CopyTo(packet.AsSpan(12));
			return packet;
		}

		private static async Task<RconPacket> ReadAuthenticationResponseAsync(
			NetworkStream stream,
			int authenticationId,
			CancellationToken cancellationToken)
		{
			for (int attempt = 0; attempt < 3; attempt++)
			{
				RconPacket packet = await ReadPacketAsync(stream, cancellationToken);
				if (packet.RequestId == -1 || packet.RequestId == authenticationId)
					return packet;
			}

			throw new InvalidDataException(LocalizationManager.Get("Minecraft.Rcon.AuthenticationMissing"));
		}

		private static async Task WritePacketAsync(
			NetworkStream stream,
			int requestId,
			int packetType,
			string body,
			CancellationToken cancellationToken)
		{
			byte[] packet = EncodePacket(requestId, packetType, body);
			await stream.WriteAsync(packet, cancellationToken);
			await stream.FlushAsync(cancellationToken);
		}

		private static async Task<RconPacket> ReadPacketAsync(
			NetworkStream stream,
			CancellationToken cancellationToken)
		{
			byte[] lengthBytes = new byte[4];
			await stream.ReadExactlyAsync(lengthBytes, cancellationToken);
			int length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
			if (length is < 10 or > MaximumPacketLength)
				throw new InvalidDataException(LocalizationManager.Get("Minecraft.Rcon.InvalidPacketLength", length));

			byte[] payload = new byte[length];
			await stream.ReadExactlyAsync(payload, cancellationToken);
			if (payload[^1] != 0 || payload[^2] != 0)
				throw new InvalidDataException(LocalizationManager.Get("Minecraft.Rcon.PacketTerminatorMissing"));

			int requestId = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(0, 4));
			int packetType = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(4, 4));
			string body = Encoding.UTF8.GetString(payload, 8, payload.Length - 10);
			return new RconPacket(requestId, packetType, body);
		}

		private sealed record RconPacket(int RequestId, int PacketType, string Body);
	}
}
