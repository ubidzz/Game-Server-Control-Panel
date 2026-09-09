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
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class GameLaunchCommandBuilderTests
{
	private static readonly Regex UnresolvedLaunchPlaceholder = new(
		"\\{(?:app_port|seed|map|steamAppID|appid|port|query|MaxPlayers|pass|adminpass|auth_token|ServerName|InstallPath|world_size|Identity|crossplay|crossplay_public_ip|ram|rcon|mode|PublicIP)\\}",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	[Fact]
	public void EveryBuiltInGameUsesASupportedLauncherAndBuildsCompleteArguments()
	{
		SynixServerPasswords passwords = new("server-pass", "admin-pass", "rcon-pass");
		foreach (GameInfo definition in GameDatabase.GetGameList())
		{
			Assert.True(
				GameLaunchCommandBuilder.TryGetLauncherKind(
					definition.ExeName,
					out GameLauncherKind launcherKind),
				$"{definition.Game} uses unsupported launch file '{definition.ExeName}'.");

			GameServer server = CreateServer(definition.Game);
			Assert.True(
				GameLaunchCommandBuilder.TryBuildArguments(
					server,
					definition,
					definition.AppID,
					passwords,
					out string arguments,
					out string error),
				$"{definition.Game} did not build its launch arguments: {error}");
			Assert.False(
				UnresolvedLaunchPlaceholder.IsMatch(arguments),
				$"{definition.Game} left a launch placeholder unresolved: {arguments}");
			if (definition.RequiredArgs.TrimEnd().EndsWith(
				"-dedicated",
				StringComparison.OrdinalIgnoreCase))
			{
				Assert.Contains("-synixCompatibilityTest 1", arguments);
				Assert.EndsWith("-dedicated", arguments);
				Assert.True(
					arguments.IndexOf("-synixCompatibilityTest 1", StringComparison.Ordinal) <
					arguments.LastIndexOf("-dedicated", StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				Assert.EndsWith("-synixCompatibilityTest 1", arguments);
			}

			string executablePath = Path.Combine(server.InstallPath, definition.ExeName);
			string workingDirectory = Path.GetDirectoryName(executablePath)!;
			bool redirectInput = GameDatabase.IsMinecraft(definition.Game);
			var startInfo = GameLaunchCommandBuilder.CreateProcessStartInfo(
				executablePath,
				arguments,
				workingDirectory,
				definition.LaunchBehavior.RunElevated,
				createNoWindow: true,
				redirectStandardInput: redirectInput);

			Assert.Equal(workingDirectory, startInfo.WorkingDirectory);
			Assert.Equal(definition.LaunchBehavior.RunElevated, startInfo.UseShellExecute);
			if (launcherKind == GameLauncherKind.NativeExecutable)
			{
				Assert.Equal(executablePath, startInfo.FileName);
				Assert.Equal(arguments, startInfo.Arguments);
			}
			else
			{
				Assert.Equal("cmd.exe", Path.GetFileName(startInfo.FileName), ignoreCase: true);
				Assert.Contains("/d /s /v:off /c", startInfo.Arguments);
				Assert.Contains($"\"{executablePath}\"", startInfo.Arguments);
				Assert.Contains(arguments, startInfo.Arguments);
			}
		}
	}

	[Fact]
	public void EcoLaunchUsesTheSavedTokenAndRedactsItFromPreviews()
	{
		GameInfo definition = GameDatabase.GetGame("Eco")!;
		GameServer server = CreateServer(definition.Game);
		const string authenticationToken = "eco-user-token_123.test";
		SynixServerPasswords passwords = new(
			string.Empty,
			string.Empty,
			string.Empty,
			authenticationToken);

		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			passwords,
			out string launchArguments,
			out string launchError), launchError);
		Assert.Contains($"-userToken=\"{authenticationToken}\"", launchArguments);

		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			GameLaunchCommandBuilder.CreateRedactedPasswords(passwords),
			out string redactedArguments,
			out string redactionError), redactionError);
		Assert.DoesNotContain(authenticationToken, redactedArguments);
		Assert.Contains("-userToken=\"********\"", redactedArguments);
	}

	[Fact]
	public void NativeExecutableLaunchKeepsTheExistingDirectProcessBehavior()
	{
		string executablePath = @"C:\Synix\Games\Rust\RustDedicated.exe";
		var startInfo = GameLaunchCommandBuilder.CreateProcessStartInfo(
			executablePath,
			"-batchmode +server.port 28015",
			@"C:\Synix\Games\Rust",
			runElevated: false,
			createNoWindow: true,
			redirectStandardInput: false);

		Assert.Equal(executablePath, startInfo.FileName);
		Assert.Equal("-batchmode +server.port 28015", startInfo.Arguments);
		Assert.False(startInfo.UseShellExecute);
		Assert.True(startInfo.CreateNoWindow);
		Assert.Equal(ProcessWindowStyle.Hidden, startInfo.WindowStyle);
		Assert.False(startInfo.RedirectStandardInput);
	}

	[Fact]
	public void PalworldCommunityListingUsesCurrentPublicAddressAndSelectedPort()
	{
		GameInfo definition = GameDatabase.GetGame("Palworld")!;
		GameServer server = CreateServer(definition.Game);
		server.Port = 8777;

		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords("server-pass", "admin-pass", string.Empty),
			"203.0.113.25",
			out string arguments,
			out string error), error);
		Assert.Contains("-publiclobby", arguments);
		Assert.Contains("-publicip=203.0.113.25", arguments);
		Assert.Contains("-port=8777", arguments);
		Assert.Contains("-publicport=8777", arguments);
		Assert.DoesNotContain("-useperfthreads", arguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("-NoAsyncLoadingThread", arguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("-UseMultithreadForDS", arguments, StringComparison.OrdinalIgnoreCase);

		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords("server-pass", "admin-pass", string.Empty),
			string.Empty,
			out string automaticArguments,
			out error), error);
		Assert.Contains("-publiclobby", automaticArguments);
		Assert.DoesNotContain("-publicip=", automaticArguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("{PublicIP}", automaticArguments, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("ARK: Survival Ascended", "-ServerPlatform=ALL", "-ServerPlatform=PC")]
	[InlineData("ARK: Survival Evolved", "-crossplay", "")]
	[InlineData("Valheim", "-crossplay", "")]
	public void CrossplayFlagIsIncludedOnlyWhenSelected(
		string game,
		string enabledValue,
		string disabledValue)
	{
		GameInfo definition = GameDatabase.GetGame(game)!;
		GameServer server = CreateServer(definition.Game);

		server.CrossplayEnabled = true;
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords("server-pass", "admin-pass", string.Empty),
			out string enabledArguments,
			out string error), error);
		Assert.Contains(enabledValue, enabledArguments, StringComparison.OrdinalIgnoreCase);

		server.CrossplayEnabled = false;
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords("server-pass", "admin-pass", string.Empty),
			out string disabledArguments,
			out error), error);
		if (string.IsNullOrEmpty(disabledValue))
			Assert.DoesNotContain(enabledValue, disabledArguments, StringComparison.OrdinalIgnoreCase);
		else
			Assert.Contains(disabledValue, disabledArguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("{crossplay}", disabledArguments, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ArkSurvivalEvolvedCrossplayAddsEpicPublicIpOnlyWhenAvailable()
	{
		GameInfo definition = GameDatabase.GetGame("ARK: Survival Evolved")!;
		GameServer server = CreateServer(definition.Game);
		server.CrossplayEnabled = true;

		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords("server-pass", "admin-pass", string.Empty),
			"203.0.113.25",
			out string arguments,
			out string error), error);
		Assert.Contains("-crossplay", arguments, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("-PublicIPForEpic=203.0.113.25", arguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("{PublicIP}", arguments, StringComparison.Ordinal);

		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords("server-pass", "admin-pass", string.Empty),
			string.Empty,
			out arguments,
			out error), error);
		Assert.DoesNotContain("PublicIPForEpic", arguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("{PublicIP}", arguments, StringComparison.Ordinal);

		server.CrossplayEnabled = false;
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords("server-pass", "admin-pass", string.Empty),
			"203.0.113.25",
			out arguments,
			out error), error);
		Assert.DoesNotContain("-crossplay", arguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("PublicIPForEpic", arguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("{crossplay_public_ip}", arguments, StringComparison.Ordinal);
	}

	[Fact]
	public void RequiredManagerWindowOverridesTheGlobalHideSetting()
	{
		GameInfo spaceEngineers = GameDatabase.GetGame("Space Engineers")!;
		GameInfo rust = GameDatabase.GetGame("Rust")!;

		Assert.False(GameLaunchCommandBuilder.ShouldHideServerWindow(
			spaceEngineers,
			showServerWindowSetting: false));
		Assert.False(GameLaunchCommandBuilder.ShouldHideServerWindow(
			spaceEngineers,
			showServerWindowSetting: true));
		Assert.True(GameLaunchCommandBuilder.ShouldHideServerWindow(
			rust,
			showServerWindowSetting: false));
		Assert.False(GameLaunchCommandBuilder.ShouldHideServerWindow(
			rust,
			showServerWindowSetting: true));
	}

	[Fact]
	public void MinecraftBatchLaunchUsesHardenedCmdAndKeepsItsInputChannel()
	{
		string scriptPath = @"C:\Synix\Games\Minecraft Java\Start.bat";
		var startInfo = GameLaunchCommandBuilder.CreateProcessStartInfo(
			scriptPath,
			"-Xmx4096M -Xms4096M -jar server.jar nogui",
			@"C:\Synix\Games\Minecraft Java",
			runElevated: false,
			createNoWindow: true,
			redirectStandardInput: true);

		Assert.Equal("cmd.exe", Path.GetFileName(startInfo.FileName), ignoreCase: true);
		Assert.Equal(
			"/d /s /v:off /c \"\"C:\\Synix\\Games\\Minecraft Java\\Start.bat\" -Xmx4096M -Xms4096M -jar server.jar nogui\"",
			startInfo.Arguments);
		Assert.False(startInfo.UseShellExecute);
		Assert.True(startInfo.CreateNoWindow);
		Assert.Equal(ProcessWindowStyle.Hidden, startInfo.WindowStyle);
		Assert.True(startInfo.RedirectStandardInput);
	}

	[Fact]
	public void ElevatedBatchLaunchUsesCmdThroughTheWindowsElevationPrompt()
	{
		var startInfo = GameLaunchCommandBuilder.CreateProcessStartInfo(
			@"C:\Synix\Games\Dune Awakening\battlegroup.bat",
			string.Empty,
			@"C:\Synix\Games\Dune Awakening",
			runElevated: true,
			createNoWindow: true,
			redirectStandardInput: true);

		Assert.Equal("cmd.exe", Path.GetFileName(startInfo.FileName), ignoreCase: true);
		Assert.True(startInfo.UseShellExecute);
		Assert.Equal("runas", startInfo.Verb);
		Assert.False(startInfo.CreateNoWindow);
		Assert.Equal(ProcessWindowStyle.Normal, startInfo.WindowStyle);
		Assert.False(startInfo.RedirectStandardInput);
	}

	[Fact]
	public void WindowsBatchLauncherForwardsTheCompleteArgumentListThroughPercentStar()
	{
		string testDirectory = Path.Combine(
			Path.GetTempPath(),
			"Synix Launch Test " + Guid.NewGuid().ToString("N"));
		string scriptPath = Path.Combine(testDirectory, "Start Server.bat");
		string resultPath = Path.Combine(testDirectory, "arguments.txt");
		Directory.CreateDirectory(testDirectory);
		File.WriteAllText(
			scriptPath,
			"@echo off\r\n> \"%~dp0arguments.txt\" echo %*\r\nexit /b %errorlevel%\r\n");

		try
		{
			var startInfo = GameLaunchCommandBuilder.CreateProcessStartInfo(
				scriptPath,
				"-Xmx4096M -Dmod.name=Test -message \"Two Words\"",
				testDirectory,
				runElevated: false,
				createNoWindow: true,
				redirectStandardInput: false);

			using Process process = Process.Start(startInfo)!;
			Assert.True(process.WaitForExit(10_000));
			Assert.Equal(0, process.ExitCode);
			Assert.Equal(
				"-Xmx4096M -Dmod.name=Test -message \"Two Words\"",
				File.ReadAllText(resultPath).Trim());
		}
		finally
		{
			Directory.Delete(testDirectory, recursive: true);
		}
	}

	[Fact]
	public void ForgeUsesTheGeneratedLauncherInsteadOfTheVanillaJarArguments()
	{
		GameInfo definition = GameDatabase.GetGame("Minecraft")!;
		GameServer server = CreateServer(definition.Game);
		server.MinecraftLoader = MinecraftMetadataService.ForgeLoader;
		server.ExtraArgs = "-Dfml.readTimeout=180";

		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords(string.Empty, string.Empty, string.Empty),
			out string arguments,
			out string error), error);
		Assert.Equal("-Xmx4096M -Xms4096M -Dfml.readTimeout=180", arguments);
		Assert.DoesNotContain("server.jar", arguments, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("server.jar")]
	[InlineData("server.dll")]
	[InlineData("launcher.ps1")]
	public void UnsupportedLaunchFilesAreRejected(string executable)
	{
		Assert.False(GameLaunchCommandBuilder.TryGetLauncherKind(executable, out _));
	}

	[Fact]
	public void ArgumentPreviewUsesTheInstalledAppIdAndNeverDisplaysPasswords()
	{
		string directory = Path.Combine(
			Path.GetTempPath(),
			"Synix.Tests",
			Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			GameInfo definition = GameDatabase.GetGame("Rust")!;
			string executablePath = Path.Combine(directory, definition.ExeName);
			File.WriteAllBytes(executablePath, []);
			File.WriteAllText(Path.Combine(directory, "steam_appid.txt"), "123456\r\n");

			GameServer server = CreateServer(definition.Game);
			server.InstallPath = directory;
			server.ExtraArgs = "-logfile \"synix test.log\"";
			Core.SetServerPasswords(
				server,
				new SynixServerPasswords(
					"x",
					"PrivateAdmin-9f84",
					"PrivateRcon-31bd"));

			GameArgumentTestPreview preview =
				Core.BuildGameArgumentTestPreview(server);

			Assert.True(
				preview.IsValid,
				string.Join(
					Environment.NewLine,
					preview.Checks.Where(check => !check.Passed)
						.Select(check => $"{check.Name}: {check.Details}")));
			Assert.Equal("123456", preview.InvokedAppId);
			Assert.Contains("-SteamAppId=123456", preview.SanitizedArguments);
			Assert.Contains("-logfile \"synix test.log\"", preview.SanitizedArguments);
			Assert.DoesNotContain("PrivateAdmin-9f84", preview.SanitizedCommand);
			Assert.DoesNotContain("PrivateRcon-31bd", preview.SanitizedCommand);
			Assert.DoesNotContain("+server.password \"x\"", preview.SanitizedCommand);
			Assert.Contains("********", preview.SanitizedCommand);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void DysterraLaunchAndPreviewUseTheClientAppIdWithoutAnInstalledAppIdFile()
	{
		string directory = Path.Combine(Path.GetTempPath(), "Synix.Tests", Guid.NewGuid().ToString("N"));
		GameInfo definition = GameDatabase.GetGame("Dysterra")!;
		GameServer server = CreateServer(definition.Game);
		server.InstallPath = directory;
		server.WorldName = "MyServer";
		server.QueryPort = 27016;
		string executablePath = GameLaunchCommandBuilder.ResolveExecutablePath(server, definition);
		Directory.CreateDirectory(Path.GetDirectoryName(executablePath)!);
		File.WriteAllBytes(executablePath, []);
		try
		{
			string appId = GameLaunchCommandBuilder.ResolveInvokedAppId(server, definition, executablePath);
			Assert.Equal("1527890", appId);
			Assert.Equal("2214780", definition.AppID);
			Assert.Empty(Directory.GetFiles(directory, "steam_appid.txt", SearchOption.AllDirectories));
			SynixServerPasswords passwords = new("test-join-password", "test-admin-password", string.Empty);
			Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
				server, definition, appId, passwords, out string arguments, out string error), error);
			Assert.Contains("-SteamAppId=1527890", arguments);
			Assert.DoesNotContain("-SteamAppId=2214780", arguments);
			Assert.Contains("-QueryPort=27016", arguments);
			Assert.Contains("-worldsettings=\"MyServer.json\"", arguments);

			Core.SetServerPasswords(server, passwords);
			GameArgumentTestPreview preview = Core.BuildGameArgumentTestPreview(server);
			Assert.True(preview.IsValid, string.Join("; ", preview.Checks.Where(check => !check.Passed).Select(check => check.Details)));
			Assert.Equal("1527890", preview.InvokedAppId);
			Assert.Contains("-SteamAppId=1527890", preview.SanitizedArguments);
			Assert.DoesNotContain("test-admin-password", preview.SanitizedCommand);
			Assert.DoesNotContain("test-join-password", preview.SanitizedCommand);
			Assert.True(GameLaunchCommandBuilder.TryBuildLogArguments(
				server, definition, appId, passwords, string.Empty, out string logArguments, out string logError), logError);
			Assert.Contains("-SteamAppId=1527890", logArguments);
			Assert.DoesNotContain("test-admin-password", logArguments);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Theory]
	[InlineData("steam_appid.txt", "2214780")]
	[InlineData("Dysterra/Binaries/Win64/steam_appid.txt", "480")]
	[InlineData("steam_appid.txt", "invalid-id")]
	public void DysterraRuntimeIdentityTakesPrecedenceWithoutChangingInstalledFiles(string relativePath, string fileContent)
	{
		string directory = Path.Combine(Path.GetTempPath(), "Synix.Tests", Guid.NewGuid().ToString("N"));
		GameInfo definition = GameDatabase.GetGame("Dysterra")!;
		GameServer server = CreateServer(definition.Game);
		server.InstallPath = directory;
		string appIdPath = Path.Combine(directory, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(appIdPath)!);
		File.WriteAllText(appIdPath, fileContent);
		try
		{
			Assert.Equal("1527890", GameLaunchCommandBuilder.ResolveInvokedAppId(
				server, definition, GameLaunchCommandBuilder.ResolveExecutablePath(server, definition)));
			Assert.Equal("2214780", definition.AppID);
			Assert.Equal(fileContent, File.ReadAllText(appIdPath));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Theory]
	[InlineData("Dysterra", "2214780", "1527890")]
	[InlineData("Rust", "123456", "123456")]
	public void BatchExportUsesTheSameSteamIdentityAsNormalLaunch(string gameName, string installedAppId, string expectedAppId)
	{
		string directory = Path.Combine(Path.GetTempPath(), "Synix.Tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		EventHandler<ApplicationLogEventArgs> captureLog = (_, _) => { };
		ApplicationUiService.LogRequested += captureLog;
		try
		{
			GameInfo definition = GameDatabase.GetGame(gameName)!;
			GameServer server = CreateServer(gameName);
			server.InstallPath = directory;
			server.WorldName = "MyServer";
			File.WriteAllText(Path.Combine(directory, "steam_appid.txt"), installedAppId);
			Core.SetServerPasswords(server, new SynixServerPasswords(
				"test-join-password", "test-admin-password", "test-rcon-password"));

			Assert.True(Core.Instance.ExportServerToBatch(server));

			string batch = File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "Run_*_Server.bat")));
			Assert.Contains($"set \"SteamAppId={expectedAppId}\"", batch);
			Assert.Contains($"set \"SteamGameId={expectedAppId}\"", batch);
			Assert.Contains($"-SteamAppId={expectedAppId}", batch);
			Assert.Equal(expectedAppId, GameLaunchCommandBuilder.ResolveInvokedAppId(
				server, definition, GameLaunchCommandBuilder.ResolveExecutablePath(server, definition)));
			Assert.Equal(installedAppId, File.ReadAllText(Path.Combine(directory, "steam_appid.txt")));
		}
		finally
		{
			ApplicationUiService.LogRequested -= captureLog;
			Directory.Delete(directory, recursive: true);
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("not-an-app-id")]
	[InlineData("١٥٢٧٨٩٠")]
	public void GamesWithoutRuntimeOverrideKeepTheInstallIdFallback(string? fileContent)
	{
		string directory = Path.Combine(Path.GetTempPath(), "Synix.Tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			GameInfo definition = GameDatabase.GetGame("Rust")!;
			GameServer server = CreateServer(definition.Game);
			server.InstallPath = directory;
			if (fileContent != null)
				File.WriteAllText(Path.Combine(directory, "steam_appid.txt"), fileContent);
			Assert.Equal(string.Empty, definition.LaunchBehavior.SteamAppId);
			Assert.Equal(definition.AppID, GameLaunchCommandBuilder.ResolveInvokedAppId(
				server, definition, GameLaunchCommandBuilder.ResolveExecutablePath(server, definition)));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void InvokedAppIdIgnoresDamagedSteamAppIdFiles()
	{
		string directory = Path.Combine(
			Path.GetTempPath(),
			"Synix.Tests",
			Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			GameInfo definition = GameDatabase.GetGame("Rust")!;
			GameServer server = CreateServer(definition.Game);
			server.InstallPath = directory;
			File.WriteAllText(Path.Combine(directory, "steam_appid.txt"), "not-an-app-id");

			Assert.Equal(
				definition.AppID,
				GameLaunchCommandBuilder.ResolveInvokedAppId(
					server,
					definition,
					Path.Combine(directory, definition.ExeName)));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static GameServer CreateServer(string game) => new()
	{
		Game = game,
		ServerName = "Compatibility Test",
		InstallPath = @"C:\Synix\Games\Compatibility Test",
		WorldName = "TestWorld",
		WorldSeed = "12345",
		WorldSize = 4000,
		Port = 28015,
		QueryPort = 28016,
		AppPort = 28017,
		MaxPlayers = 24,
		MaxRam = 4,
		EnableRcon = true,
		RconPort = 28018,
		GameMode = "PVE",
		ExtraArgs = "-synixCompatibilityTest 1"
	};
}
