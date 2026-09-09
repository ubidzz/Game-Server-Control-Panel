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
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	internal enum GameLauncherKind
	{
		NativeExecutable,
		WindowsCommandScript
	}

	internal static class GameLaunchCommandBuilder
	{
		internal static string ResolveExecutablePath(GameServer server, GameInfo definition)
		{
			ArgumentNullException.ThrowIfNull(server);
			ArgumentNullException.ThrowIfNull(definition);
			return Path.Combine(
				server.InstallPath,
				MinecraftControlProfile.ResolveExecutableName(server, definition));
		}

		internal static string ResolveInvokedAppId(
			GameServer server,
			GameInfo definition,
			string executablePath)
		{
			ArgumentNullException.ThrowIfNull(server);
			ArgumentNullException.ThrowIfNull(definition);

			// A trusted runtime identity takes precedence over installed files and the download ID.
			if (!string.IsNullOrEmpty(definition.LaunchBehavior.SteamAppId))
				return definition.LaunchBehavior.SteamAppId;

			string invokedAppId = definition.AppID;
			string executableDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty;
			string rootAppIdPath = Path.Combine(server.InstallPath, "steam_appid.txt");
			string executableAppIdPath = Path.Combine(executableDirectory, "steam_appid.txt");
			string appIdPath = rootAppIdPath;

			if (File.Exists(rootAppIdPath))
			{
				appIdPath = rootAppIdPath;
			}
			else if (File.Exists(executableAppIdPath))
			{
				appIdPath = executableAppIdPath;
			}
			else
			{
				try
				{
					appIdPath = Directory.EnumerateFiles(
						server.InstallPath,
						"steam_appid.txt",
						new EnumerationOptions
						{
							RecurseSubdirectories = true,
							IgnoreInaccessible = true,
							MaxRecursionDepth = 15,
							AttributesToSkip = FileAttributes.ReparsePoint
						})
						.FirstOrDefault() ?? rootAppIdPath;
				}
				catch (Exception exception) when (exception is IOException or
					UnauthorizedAccessException or
					DirectoryNotFoundException)
				{
					appIdPath = rootAppIdPath;
				}
			}

			if (!File.Exists(appIdPath))
				return invokedAppId;

			try
			{
				string fileContent = File.ReadLines(appIdPath)
					.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))
					?.Trim() ?? string.Empty;
				if (fileContent.All(char.IsAsciiDigit) && fileContent.Length > 0)
					invokedAppId = fileContent;
			}
			catch (Exception exception) when (exception is IOException or
				UnauthorizedAccessException)
			{
			}

			return invokedAppId;
		}

		internal static SynixServerPasswords CreateRedactedPasswords(
			SynixServerPasswords passwords)
		{
			return new SynixServerPasswords(
				string.IsNullOrEmpty(passwords.ServerPassword) ? string.Empty : "********",
				string.IsNullOrEmpty(passwords.AdminPassword) ? string.Empty : "********",
				string.IsNullOrEmpty(passwords.RconPassword) ? string.Empty : "********",
				string.IsNullOrEmpty(passwords.AuthenticationToken) ? string.Empty : "********");
		}

		internal static bool ShouldHideServerWindow(
			GameInfo definition,
			bool showServerWindowSetting)
		{
			ArgumentNullException.ThrowIfNull(definition);
			return !showServerWindowSetting &&
				!definition.LaunchBehavior.RequiresVisibleWindow;
		}

		internal static bool TryGetLauncherKind(
			string? executablePath,
			out GameLauncherKind launcherKind)
		{
			string extension = Path.GetExtension(executablePath ?? string.Empty);
			if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
			{
				launcherKind = GameLauncherKind.NativeExecutable;
				return true;
			}

			if (extension.Equals(".bat", StringComparison.OrdinalIgnoreCase) ||
				extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase))
			{
				launcherKind = GameLauncherKind.WindowsCommandScript;
				return true;
			}

			launcherKind = default;
			return false;
		}

		internal static bool TryBuildArguments(
			GameServer server,
			GameInfo definition,
			string invokedAppId,
			SynixServerPasswords passwords,
			out string arguments,
			out string errorMessage,
			bool forBatchFile = false)
		{
			return TryBuildArguments(
				server,
				definition,
				invokedAppId,
				passwords,
				string.Empty,
				out arguments,
				out errorMessage,
				forBatchFile);
		}

		internal static bool TryBuildArguments(
			GameServer server,
			GameInfo definition,
			string invokedAppId,
			SynixServerPasswords passwords,
			string? publicIp,
			out string arguments,
			out string errorMessage,
			bool forBatchFile = false,
			bool includeExtraArguments = true)
		{
			ArgumentNullException.ThrowIfNull(server);
			ArgumentNullException.ThrowIfNull(definition);

			arguments = string.Empty;
			errorMessage = string.Empty;
			if (includeExtraArguments && !Core.TryValidateExtraArguments(server.ExtraArgs, out errorMessage))
				return false;

			bool isMinecraft = GameCapabilityResolver.UsesMinecraftLifecycle(server);
			bool isBedrock = MinecraftControlProfile.IsBedrock(server);
			int ramToUse = isMinecraft ? server.MaxRam * 1024 : server.MaxRam;
			string targetAppId = definition.AppID ?? string.Empty;
			string cleanIdentity = Core.Instance.GetSafeName(server.ServerName ?? string.Empty);

			bool commandScript = TryGetLauncherKind(ResolveExecutablePath(server, definition), out GameLauncherKind kind) &&
				kind == GameLauncherKind.WindowsCommandScript;
			string template = isBedrock ? string.Empty : PreparePublicIpArgument(definition.RequiredArgs ?? string.Empty, publicIp);
			Dictionary<string, string> values = new(StringComparer.Ordinal)
			{
				["{app_port}"] = server.AppPort?.ToString() ?? "0",
				["{seed}"] = string.IsNullOrWhiteSpace(server.WorldSeed) ? "12345" : server.WorldSeed,
				["{map}"] = server.WorldName ?? string.Empty,
				["{steamAppID}"] = invokedAppId ?? string.Empty,
				["{appid}"] = targetAppId,
				["{port}"] = server.Port.ToString(),
				["{query}"] = server.QueryPort.ToString(),
				["{MaxPlayers}"] = server.MaxPlayers.ToString(),
				["{pass}"] = passwords.ServerPassword ?? string.Empty,
				["{adminpass}"] = passwords.AdminPassword ?? string.Empty,
				["{auth_token}"] = passwords.AuthenticationToken ?? string.Empty,
				["{ServerName}"] = server.ServerName ?? string.Empty,
				["{InstallPath}"] = server.InstallPath ?? string.Empty,
				["{world_size}"] = server.WorldSize.ToString(),
				["{Identity}"] = cleanIdentity,
				["{crossplay}"] = GameFix.ResolveCrossplayValue(definition, server.CrossplayEnabled),
				["{crossplay_public_ip}"] = ResolveCrossplayPublicIp(server.CrossplayEnabled, publicIp),
				["{ram}"] = ramToUse.ToString(),
				["{rcon_port}"] = server.RconPort.ToString(),
				["{rcon_pass}"] = passwords.RconPassword ?? string.Empty,
				["{rcon_enabled}"] = GameFix.ResolveBooleanValue(definition, true)
			};

			string minecraftLoader = MinecraftMetadataService.NormalizeLoader(server.MinecraftLoader);
			if (MinecraftControlProfile.IsJava(server) &&
				(minecraftLoader.Equals(MinecraftMetadataService.ForgeLoader, StringComparison.OrdinalIgnoreCase) ||
				 minecraftLoader.Equals(MinecraftMetadataService.NeoForgeLoader, StringComparison.OrdinalIgnoreCase)))
			{
				template = $"-Xmx{ramToUse}M -Xms{ramToUse}M";
			}

			if (template.Contains("{rcon}", StringComparison.Ordinal))
			{
				string formattedRcon = string.Empty;
				if (server.EnableRcon && !string.IsNullOrWhiteSpace(definition.RconSyntax))
				{
					if (!TryExpandTemplate(definition.RconSyntax, values, commandScript, forBatchFile,
						out formattedRcon, out errorMessage))
						return false;
				}

				values["{rcon}"] = formattedRcon;
			}

			if (!string.IsNullOrWhiteSpace(server.GameMode))
			{
				values["{mode}"] = GameFix.ResolveGameModeValue(definition, server.GameMode);
			}

			if (!TryExpandTemplate(template, values, commandScript, forBatchFile, out arguments, out errorMessage))
				return false;
			if (includeExtraArguments && !string.IsNullOrWhiteSpace(server.ExtraArgs))
				arguments = AppendExtraArguments(arguments, server.ExtraArgs);

			arguments = arguments.Trim();
			if (commandScript && !TryValidateCommandScriptArguments(arguments, out errorMessage))
			{
				arguments = string.Empty;
				return false;
			}
			return true;
		}

		internal static bool TryBuildLogArguments(GameServer server, GameInfo definition, string invokedAppId,
			SynixServerPasswords passwords, string? publicIp, out string arguments, out string errorMessage)
		{
			if (!TryBuildArguments(server, definition, invokedAppId, CreateRedactedPasswords(passwords), publicIp,
				out arguments, out errorMessage, includeExtraArguments: false))
				return false;
			if (!string.IsNullOrWhiteSpace(server.ExtraArgs))
				arguments = $"{arguments} {LocalizationManager.Get("ServerStart.Arguments.CustomHidden")}".Trim();
			return true;
		}

		private static bool TryExpandTemplate(string template, IReadOnlyDictionary<string, string> values,
			bool commandScript, bool forBatchFile, out string arguments, out string errorMessage)
		{
			StringBuilder result = new(template.Length);
			bool insideQuotes = false;
			arguments = string.Empty;
			errorMessage = string.Empty;
			// Expand only the trusted template. A value containing another placeholder stays literal.
			for (int index = 0; index < template.Length; index++)
			{
				char character = template[index];
				if (character == '{' && template.IndexOf('}', index) is int end && end > index)
				{
					string key = template[index..(end + 1)];
					if (values.TryGetValue(key, out string? value))
					{
						bool fragment = key is "{rcon}" or "{crossplay_public_ip}";
						if ((commandScript || forBatchFile) && !fragment)
						{
							if (value.IndexOfAny(['\0', '\r', '\n', '"']) >= 0 ||
								(commandScript && !Core.TryValidateExtraArguments($"\"{value}\"", out _)))
							{
								errorMessage = LocalizationManager.Get("LaunchCommand.Error.UnsafeBatchValue", key);
								return false;
							}
							if (!insideQuotes && value.Any(c => char.IsWhiteSpace(c) || "&|<>()^".Contains(c)))
								value = $"\"{value}\"";
						}
						result.Append(value);
						index = end;
						continue;
					}
				}
				if (character == '"')
					insideQuotes = !insideQuotes;
				result.Append(character);
			}
			arguments = result.ToString();
			return true;
		}

		private static bool TryValidateCommandScriptArguments(string arguments, out string errorMessage)
		{
			// cmd accepts environment-variable names containing spaces and other punctuation.
			// Check the whole command, since a pair can span two otherwise-safe fields.
			bool pairedPercent = HasDelimiterPair(arguments, '%');
			bool pairedExclamation = HasDelimiterPair(arguments, '!');
			if (pairedPercent || pairedExclamation || arguments.Contains("%~", StringComparison.Ordinal) ||
				!Core.TryValidateExtraArguments(arguments, out _))
			{
				errorMessage = LocalizationManager.Get("LaunchCommand.Error.UnsafeBatchArguments");
				return false;
			}
			bool insideQuotes = false;
			foreach (char character in arguments)
			{
				if (character == '"') insideQuotes = !insideQuotes;
				if (!insideQuotes && character is '(' or ')')
				{
					errorMessage = LocalizationManager.Get("LaunchCommand.Error.UnsafeBatchArguments");
					return false;
				}
			}
			errorMessage = string.Empty;
			return true;
		}

		private static bool HasDelimiterPair(string value, char delimiter)
		{
			int opening = value.IndexOf(delimiter);
			return opening >= 0 && value.IndexOf(delimiter, opening + 1) >= 0;
		}

		private static string AppendExtraArguments(
			string baseArguments,
			string extraArguments)
		{
			const string terminalDedicatedArgument = "-dedicated";
			string normalizedBase = baseArguments.Trim();
			string normalizedExtra = extraArguments.Trim();
			if (normalizedBase.EndsWith(
				terminalDedicatedArgument,
				StringComparison.OrdinalIgnoreCase))
			{
				string prefix = normalizedBase[..^terminalDedicatedArgument.Length]
					.TrimEnd();
				return $"{prefix} {normalizedExtra} {terminalDedicatedArgument}";
			}

			return $"{normalizedBase} {normalizedExtra}";
		}

		private static string PreparePublicIpArgument(
			string arguments,
			string? publicIp)
		{
			if (!arguments.Contains("{PublicIP}", StringComparison.Ordinal))
				return arguments;

			string normalized = publicIp?.Trim() ?? string.Empty;
			if (!IPAddress.TryParse(normalized, out IPAddress? address) ||
				address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
			{
				return arguments
					.Replace("-PublicIPForEpic={PublicIP}", string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("-publicip={PublicIP}", string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("{PublicIP}", string.Empty, StringComparison.Ordinal);
			}

			return arguments.Replace(
				"{PublicIP}",
				address.ToString(),
				StringComparison.Ordinal);
		}

		private static string ResolveCrossplayPublicIp(
			bool crossplayEnabled,
			string? publicIp)
		{
			if (!crossplayEnabled)
				return string.Empty;

			string normalized = publicIp?.Trim() ?? string.Empty;
			if (!IPAddress.TryParse(normalized, out IPAddress? address) ||
				address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
			{
				return string.Empty;
			}

			return $"-PublicIPForEpic={address}";
		}

		internal static ProcessStartInfo CreateProcessStartInfo(
			string executablePath,
			string arguments,
			string workingDirectory,
			bool runElevated,
			bool createNoWindow,
			bool redirectStandardInput,
			bool redirectStandardOutput = false)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
			ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

			if (!TryGetLauncherKind(executablePath, out GameLauncherKind launcherKind))
			{
				throw new NotSupportedException(
					LocalizationManager.Get(
						"LaunchCommand.Error.UnsupportedFileType",
						Path.GetExtension(executablePath)));
			}

			ProcessStartInfo startInfo;
			if (launcherKind == GameLauncherKind.WindowsCommandScript)
			{
				startInfo = new ProcessStartInfo
				{
					FileName = GetWindowsCommandProcessorPath(),
					Arguments = BuildCommandProcessorArguments(executablePath, arguments)
				};
			}
			else
			{
				startInfo = new ProcessStartInfo
				{
					FileName = executablePath,
					Arguments = arguments ?? string.Empty
				};
			}

			startInfo.WorkingDirectory = workingDirectory;
			startInfo.UseShellExecute = runElevated;
			startInfo.CreateNoWindow = !runElevated && createNoWindow;
			startInfo.WindowStyle = !runElevated && createNoWindow
				? ProcessWindowStyle.Hidden
				: ProcessWindowStyle.Normal;
			startInfo.RedirectStandardInput = !runElevated && redirectStandardInput;
			startInfo.RedirectStandardOutput = !runElevated && redirectStandardOutput;
			startInfo.RedirectStandardError = !runElevated && redirectStandardOutput;
			if (runElevated)
				startInfo.Verb = "runas";

			return startInfo;
		}

		internal static string BuildCommandProcessorArguments(
			string scriptPath,
			string? arguments)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(scriptPath);
			if (scriptPath.IndexOfAny(['\0', '\r', '\n', '"']) >= 0)
				throw new ArgumentException(
					LocalizationManager.Get(
						"LaunchCommand.Error.UnsafeScriptPath"),
					nameof(scriptPath));

			string command = $"\"{scriptPath}\"";
			if (!string.IsNullOrWhiteSpace(arguments))
				command = $"{command} {arguments.Trim()}";
			if (!TryValidateCommandScriptArguments(command, out string errorMessage))
				throw new ArgumentException(errorMessage, nameof(arguments));

			return $"/d /s /v:off /c \"{command}\"";
		}

		private static string GetWindowsCommandProcessorPath()
		{
			return string.IsNullOrWhiteSpace(Environment.SystemDirectory)
				? "cmd.exe"
				: Path.Combine(Environment.SystemDirectory, "cmd.exe");
		}
	}
}
