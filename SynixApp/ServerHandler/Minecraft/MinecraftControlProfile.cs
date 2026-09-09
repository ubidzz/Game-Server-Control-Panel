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
using Synix_Control_Panel.SynixEngine;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	internal sealed record MinecraftManagementSettings(
		bool Enabled,
		string Host,
		int Port,
		string Secret,
		bool TlsEnabled);

	internal static class MinecraftControlProfile
	{
		internal const string JavaEdition = "Java";
		internal const string BedrockEdition = "Bedrock";
		internal const string SurvivalGameMode = "Survival";
		internal const string CreativeGameMode = "Creative";
		internal const string AdventureGameMode = "Adventure";
		internal const string BedrockExecutableName = "bedrock_server.exe";
		internal const int BedrockDefaultPort = 19132;
		internal const int BedrockDefaultIpv6Port = 19133;
		internal static readonly IReadOnlyList<string> GameModes =
			[SurvivalGameMode, CreativeGameMode, AdventureGameMode];
		private const int FirstManagementProtocolMajor = 1;
		private const int FirstManagementProtocolMinor = 21;
		private const int FirstManagementProtocolPatch = 9;
		private const int SecretLength = 40;
		private const string SecretCharacters =
			"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		private static readonly ConcurrentDictionary<string, string> GeneratedSecrets =
			new(StringComparer.OrdinalIgnoreCase);

		internal static string NormalizeEdition(string? edition) =>
			string.Equals(edition, BedrockEdition, StringComparison.OrdinalIgnoreCase)
				? BedrockEdition
				: JavaEdition;

		internal static string NormalizeGameMode(string? gameMode)
		{
			string value = gameMode?.Trim() ?? string.Empty;
			if (value.Equals(CreativeGameMode, StringComparison.OrdinalIgnoreCase))
				return CreativeGameMode;
			if (value.Equals(AdventureGameMode, StringComparison.OrdinalIgnoreCase))
				return AdventureGameMode;

			// PVE and PVP are legacy Synix values, not Minecraft game modes.
			return SurvivalGameMode;
		}

		internal static bool IsBedrock(GameServer server) =>
			GameCapabilityResolver.UsesMinecraftLifecycle(server) &&
			(server.Game.Trim().Equals("Minecraft Bedrock", StringComparison.OrdinalIgnoreCase) ||
			 NormalizeEdition(server.MinecraftEdition) == BedrockEdition);

		internal static bool IsJava(GameServer server) =>
			GameCapabilityResolver.UsesMinecraftLifecycle(server) && !IsBedrock(server);

		internal static string ResolveExecutableName(GameServer server, GameInfo definition) =>
			IsBedrock(server) ? BedrockExecutableName : definition.ExeName;

		internal static bool SupportsManagementProtocol(string? gameVersion)
		{
			string value = gameVersion?.Trim() ?? string.Empty;
			if (value.Equals("latest", StringComparison.OrdinalIgnoreCase))
				return true;

			int separator = value.IndexOfAny(['-', '+', ' ']);
			if (separator >= 0)
				value = value[..separator];

			string[] parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2 ||
				!int.TryParse(parts[0], out int major) ||
				!int.TryParse(parts[1], out int minor))
			{
				return false;
			}

			int patch = parts.Length >= 3 && int.TryParse(parts[2], out int parsedPatch)
				? parsedPatch
				: 0;
			return (major, minor, patch).CompareTo((
				FirstManagementProtocolMajor,
				FirstManagementProtocolMinor,
				FirstManagementProtocolPatch)) >= 0;
		}

		internal static bool ShouldEnableManagementProtocol(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			return IsJava(server) &&
				server.EnableMinecraftManagementProtocol &&
				SupportsManagementProtocol(server.GameVersion);
		}

		internal static bool EnsureDefaults(
			GameServer server,
			IEnumerable<GameServer>? registeredServers = null)
		{
			ArgumentNullException.ThrowIfNull(server);
			if (!ShouldEnableManagementProtocol(server))
				return false;

			List<GameServer> servers = (registeredServers ?? []).ToList();
			if (server.MinecraftManagementPort is >= 1024 and <= 65535 &&
				IsAvailableFor(server, server.MinecraftManagementPort, servers))
			{
				return false;
			}

			int preferred = server.RconPort is >= 1024 and < 65535
				? server.RconPort + 1
				: Math.Clamp(server.QueryPort + 20, 1024, 65535);
			server.MinecraftManagementPort = ExistingServerImport.FindAvailablePort(
				preferred,
				servers.Where(existing => !ReferenceEquals(existing, server))
					.Append(new GameServer
					{
						Port = server.Port,
						QueryPort = server.QueryPort,
						EnableRcon = server.EnableRcon,
						RconPort = server.RconPort,
						AppPort = server.AppPort
					}));
			return true;
		}

		internal static string GetOrCreateManagementSecret(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			string path = GetPropertiesPath(server);
			if (TryReadProperties(path, out Dictionary<string, string>? properties) &&
				properties.TryGetValue("management-server-secret", out string? existing) &&
				IsValidManagementSecret(existing))
			{
				GeneratedSecrets[path] = existing;
				return existing;
			}

			return GeneratedSecrets.GetOrAdd(path, _ => CreateManagementSecret());
		}

		internal static bool TryLoadManagementSettings(
			GameServer server,
			out MinecraftManagementSettings? settings,
			out string problem)
		{
			settings = null;
			problem = string.Empty;
			if (!ShouldEnableManagementProtocol(server))
			{
				problem = LocalizationManager.Get("Minecraft.Management.ProtocolDisabled");
				return false;
			}

			if (!TryReadProperties(
				GetPropertiesPath(server),
				out Dictionary<string, string>? properties))
			{
				problem = LocalizationManager.Get("Minecraft.Management.PropertiesUnavailable");
				return false;
			}

			bool enabled = ReadBoolean(properties, "management-server-enabled");
			string host = ReadValue(properties, "management-server-host", "localhost");
			bool tls = ReadBoolean(properties, "management-server-tls-enabled");
			string secret = ReadValue(properties, "management-server-secret", string.Empty);
			if (!int.TryParse(
				ReadValue(properties, "management-server-port", "0"),
				out int port))
			{
				port = 0;
			}

			if (!enabled || port is < 1 or > 65535 || !IsValidManagementSecret(secret))
			{
				problem = LocalizationManager.Get("Minecraft.Management.EndpointUnavailable");
				return false;
			}

			if (!IsLoopbackHost(host))
			{
				problem = LocalizationManager.Get("Minecraft.Management.LoopbackRequired");
				return false;
			}

			settings = new MinecraftManagementSettings(enabled, host, port, secret, tls);
			return true;
		}

		internal static bool IsValidManagementSecret(string? value) =>
			value?.Length == SecretLength && value.All(char.IsAsciiLetterOrDigit);

		private static bool IsAvailableFor(
			GameServer server,
			int port,
			IEnumerable<GameServer> registeredServers)
		{
			if (server.Port == port || server.QueryPort == port ||
				(server.EnableRcon && server.RconPort == port) || server.AppPort == port)
			{
				return false;
			}

			return registeredServers.All(existing =>
				ReferenceEquals(existing, server) || !Core.HasConfiguredPort(existing, port));
		}

		private static string CreateManagementSecret()
		{
			Span<char> secret = stackalloc char[SecretLength];
			for (int index = 0; index < secret.Length; index++)
			{
				secret[index] = SecretCharacters[
					RandomNumberGenerator.GetInt32(SecretCharacters.Length)];
			}

			return new string(secret);
		}

		private static string GetPropertiesPath(GameServer server)
		{
			string installPath = string.IsNullOrWhiteSpace(server.InstallPath)
				? Environment.CurrentDirectory
				: server.InstallPath;
			return Path.GetFullPath(Path.Combine(installPath, "server.properties"));
		}

		private static bool TryReadProperties(
			string path,
			out Dictionary<string, string> properties)
		{
			properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (!File.Exists(path))
				return false;

			try
			{
				foreach (string rawLine in File.ReadLines(path))
				{
					string line = rawLine.Trim();
					if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('!'))
						continue;

					int separator = line.IndexOf('=');
					if (separator <= 0)
						continue;

					properties[line[..separator].Trim()] = line[(separator + 1)..].Trim();
				}

				return true;
			}
			catch (Exception suppressedException)
			{
				ApplicationLogService.WriteSuppressedException(suppressedException);
				properties.Clear();
				return false;
			}
		}

		private static string ReadValue(
			IReadOnlyDictionary<string, string> properties,
			string key,
			string fallback) =>
			properties.TryGetValue(key, out string? value) ? value : fallback;

		private static bool ReadBoolean(
			IReadOnlyDictionary<string, string> properties,
			string key) =>
			bool.TryParse(ReadValue(properties, key, bool.FalseString), out bool value) && value;

		private static bool IsLoopbackHost(string host) =>
			host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
			host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
			host.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
			host.Equals("[::1]", StringComparison.OrdinalIgnoreCase);
	}
}
