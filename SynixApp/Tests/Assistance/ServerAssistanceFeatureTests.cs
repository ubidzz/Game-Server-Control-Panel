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
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class ServerAssistanceFeatureTests
{
	[Fact]
	public void ExistingServerImportDetectsTheExactDefinitionExecutable()
	{
		GameInfo definition = GameDatabase.GetGameList()
			.First(game => !string.IsNullOrWhiteSpace(game.ExeName));
		string folder = CreateTemporaryServerFolder(definition);
		try
		{
			IReadOnlyList<ExistingServerDetection> detections =
				ExistingServerImport.Detect(folder);

			Assert.Contains(detections, detection =>
				detection.Game.Game.Equals(definition.Game, StringComparison.OrdinalIgnoreCase) &&
				detection.ExecutablePath.Equals(
					Path.GetFullPath(Path.Combine(folder, definition.ExeName)),
					StringComparison.OrdinalIgnoreCase));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Fact]
	public void ExistingServerImportPreservesConfigurationAndRejectsDuplicateFolders()
	{
		GameInfo definition = GameDatabase.GetGameList()
			.First(game => !string.IsNullOrWhiteSpace(game.ExeName));
		string folder = CreateTemporaryServerFolder(definition);
		try
		{
			GameServer imported = ExistingServerImport.Create(
				folder,
				definition,
				"Imported Test",
				20000,
				20001,
				[]);

			Assert.False(imported.IsFirstBoot);
			Assert.False(imported.IsDefaultPath);
			Assert.True(imported.PreserveImportedConfiguration);
			Assert.Equal(Path.GetFullPath(folder), imported.InstallPath);
			Assert.Throws<InvalidOperationException>(() => ExistingServerImport.Create(
				folder,
				definition,
				"Another Name",
				20010,
				20011,
				[imported]));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Fact]
	[Trait("Category", "Regression")]
	public void ExistingServerImportRejectsAQueryPortUsedByAStoppedServer()
	{
		GameInfo definition = GameDatabase.GetGameList()
			.First(game => !string.IsNullOrWhiteSpace(game.ExeName));
		string folder = CreateTemporaryServerFolder(definition);
		GameServer existing = new()
		{
			ServerName = "Existing Stopped Server",
			Port = 21000,
			QueryPort = 21001,
			Status = Core.StatusManager.GetStatus(Core.ServerState.Stopped)
		};
		try
		{
			InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
				ExistingServerImport.Create(
					folder,
					definition,
					"Imported Test",
					22000,
					21001,
					[existing]));

			Assert.Contains("unique query port", error.Message);
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Fact]
	public void SmartMaintenanceWaitsForPlayersThenRunsAtTheDeadline()
	{
		GameServer server = CreateScheduledServer();
		server.CurrentPlayers = 3;

		SmartMaintenancePlan waiting = SmartMaintenancePlanner.Evaluate(
			server,
			new DateTime(2026, 8, 31, 4, 10, 0));
		SmartMaintenancePlan deadline = SmartMaintenancePlanner.Evaluate(
			server,
			new DateTime(2026, 8, 31, 4, 30, 0));

		Assert.Equal(SmartMaintenanceDecision.DeferForPlayers, waiting.Decision);
		Assert.Equal(SmartMaintenanceDecision.RunNow, deadline.Decision);
	}

	[Fact]
	public void StandardMaintenanceDoesNotRunAfterItsScheduledMinute()
	{
		GameServer server = CreateScheduledServer();
		server.SmartMaintenanceEnabled = false;

		SmartMaintenancePlan plan = SmartMaintenancePlanner.Evaluate(
			server,
			new DateTime(2026, 8, 31, 4, 1, 0));

		Assert.Equal(SmartMaintenanceDecision.NotDue, plan.Decision);
	}

	[Fact]
	public void A2SPlayerResponseParsesNamesScoresAndConnectionTime()
	{
		using MemoryStream stream = new();
		using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
		writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x44, 0x01, 0x00 });
		writer.Write(Encoding.UTF8.GetBytes("Player One"));
		writer.Write((byte)0);
		writer.Write(42);
		writer.Write(125.5F);

		GamePlayerInfo player = Assert.Single(
			PlayerQueryService.ParsePlayerResponse(stream.ToArray()));

		Assert.Equal("Player One", player.Name);
		Assert.Equal(42, player.Score);
		Assert.InRange(player.ConnectedFor.TotalSeconds, 125.49, 125.51);
	}

	[Fact]
	public async Task PlayerQueryAcceptsDirectPlayerResponseWithoutChallenge()
	{
		using UdpClient queryServer = new(
			new IPEndPoint(IPAddress.Loopback, 0));
		int queryPort = ((IPEndPoint)queryServer.Client.LocalEndPoint!).Port;
		using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));

		Task responder = Task.Run(async () =>
		{
			UdpReceiveResult request = await queryServer.ReceiveAsync(timeout.Token);
			Assert.Equal(0x55, request.Buffer[4]);

			using MemoryStream response = new();
			using BinaryWriter writer = new(response, Encoding.UTF8, leaveOpen: true);
			writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x44, 0x01, 0x00 });
			writer.Write(Encoding.UTF8.GetBytes("LiveNoobTV"));
			writer.Write((byte)0);
			writer.Write(0);
			writer.Write(0F);
			await queryServer.SendAsync(
				response.ToArray(),
				request.RemoteEndPoint,
				timeout.Token);
		}, timeout.Token);

		GameServer server = new()
		{
			Game = "Soulmask",
			ServerName = "Direct Response Test",
			Port = 8777,
			QueryPort = queryPort,
			Status = Core.StatusManager.GetStatus(Core.ServerState.Running)
		};

		PlayerQueryResult result = await PlayerQueryService.QueryAsync(
			server,
			timeout.Token);
		await responder;

		Assert.True(result.IsSupported);
		Assert.True(result.IsSuccessful);
		Assert.Equal("LiveNoobTV", Assert.Single(result.Players).Name);
	}

	[Fact]
	public void BackgroundServiceCommandQuotesThePublishedExecutablePath()
	{
		string command = BackgroundServiceManager.BuildLaunchCommand(
			@"C:\Program Files\Synix\Synix Control Panel.exe");

		Assert.Equal(
			"\"C:\\Program Files\\Synix\\Synix Control Panel.exe\" --synix-background-agent",
			command);
	}

	[Fact]
	[Trait("Category", "Regression")]
	public void ExplicitDashboardCloseDoesNotRestartBackgroundAgent()
	{
		Assert.False(BackgroundServiceManager.ShouldStartAgent(
			startSuppressed: true,
			enabled: true,
			agentRunning: false));
		Assert.True(BackgroundServiceManager.ShouldStartAgent(
			startSuppressed: false,
			enabled: true,
			agentRunning: false));
	}

	[Fact]
	public void ConfigEditorFindsEveryFileInAMultiFileGameDefinition()
	{
		GameServer server = new()
		{
			Game = "Subsistence",
			ServerName = "Multi Config Test",
			InstallPath = Path.Combine(
				Path.GetTempPath(),
				$"synix-multi-config-{Guid.NewGuid():N}")
		};

		IReadOnlyList<ConfigurationEditorFile> files =
			Core.ResolveConfigurationEditorFiles(server);

		Assert.Equal(2, files.Count);
		Assert.Contains(files, file => file.Path.EndsWith(
			@"UDKGame\Config\UDKDedServerSettings.ini",
			StringComparison.OrdinalIgnoreCase));
		Assert.Contains(files, file => file.Path.EndsWith(
			@"UDKGame\Config\UDKEngine.ini",
			StringComparison.OrdinalIgnoreCase));
		Assert.All(files, file => Assert.Equal(ConfigFormat.StandardINI, file.Format));
	}

	[Fact]
	[Trait("Category", "Regression")]
	public void EcoConfigEditorCreatesVersionMatchedGameplayFilesFromInstalledTemplates()
	{
		string root = Path.Combine(
			Path.GetTempPath(),
			$"synix-eco-config-editor-{Guid.NewGuid():N}");
		string configDirectory = Path.Combine(root, "Configs");
		Directory.CreateDirectory(configDirectory);
		try
		{
			string networkPath = Path.Combine(configDirectory, "Network.eco");
			string existingNetwork = "{\"Name\":\"Existing Eco Server\"}";
			File.WriteAllText(networkPath, existingNetwork);
			File.WriteAllText(
				Path.Combine(configDirectory, "Network.eco.template"),
				"{\"Name\":\"Template Eco Server\"}");
			string difficultyTemplate = "{\"CollaborationPreset\":\"LowCollaboration\"}";
			File.WriteAllText(
				Path.Combine(configDirectory, "Difficulty.eco.template"),
				difficultyTemplate);
			string usersTemplate = "{\"Admins\":[],\"Blacklist\":[]}";
			File.WriteAllText(
				Path.Combine(configDirectory, "Users.eco.template"),
				usersTemplate);
			File.WriteAllText(
				Path.Combine(root, "server.properties"),
				"motd=Not an Eco configuration");

			GameServer server = new()
			{
				Game = "Eco",
				ServerName = "Eco Multi Config",
				InstallPath = root
			};
			Core.PrepareConfigurationEditorFiles(server);
			IReadOnlyList<ConfigurationEditorFile> files =
				Core.ResolveConfigurationEditorFiles(server);

			Assert.Equal(3, files.Count);
			Assert.Equal(networkPath, files[0].Path, ignoreCase: true);
			Assert.All(files, file => Assert.Equal(ConfigFormat.JSON, file.Format));
			Assert.Contains(files, file => file.Path.EndsWith(
				@"Configs\Difficulty.eco",
				StringComparison.OrdinalIgnoreCase));
			Assert.Contains(files, file => file.Path.EndsWith(
				@"Configs\Users.eco",
				StringComparison.OrdinalIgnoreCase));
			Assert.DoesNotContain(files, file => file.Path.EndsWith(
				"server.properties",
				StringComparison.OrdinalIgnoreCase));
			Assert.Equal(existingNetwork, File.ReadAllText(networkPath));
			Assert.Equal(
				difficultyTemplate,
				File.ReadAllText(Path.Combine(configDirectory, "Difficulty.eco")));
			Assert.Equal(
				usersTemplate,
				File.ReadAllText(Path.Combine(configDirectory, "Users.eco")));
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	[Trait("Category", "Regression")]
	public void ConfigEditorAddsSupportedSiblingFilesFromADeclaredConfigDirectory()
	{
		string root = Path.Combine(
			Path.GetTempPath(),
			$"synix-sibling-config-editor-{Guid.NewGuid():N}");
		string configDirectory = Path.Combine(
			root,
			@"ShooterGame\Saved\Config\WindowsServer");
		Directory.CreateDirectory(configDirectory);
		try
		{
			File.WriteAllText(
				Path.Combine(configDirectory, "GameUserSettings.ini"),
				"[ServerSettings]\nServerName=Configured");
			File.WriteAllText(
				Path.Combine(configDirectory, "Game.ini"),
				"[/Script/ShooterGame.ShooterGameMode]\nXPMultiplier=2.0");
			File.WriteAllText(
				Path.Combine(configDirectory, "Engine.ini"),
				"[URL]\nPort=7777");
			File.WriteAllText(
				Path.Combine(configDirectory, "Engine.ini.synix.bak"),
				"backup");
			File.WriteAllText(
				Path.Combine(configDirectory, "Notes.md"),
				"not a configuration file");

			GameServer server = new()
			{
				Game = "ARK: Survival Ascended",
				ServerName = "Sibling Config Test",
				InstallPath = root
			};
			IReadOnlyList<ConfigurationEditorFile> files =
				Core.ResolveConfigurationEditorFiles(server);

			Assert.Equal(3, files.Count);
			Assert.Contains(files, file => file.Path.EndsWith(
				"GameUserSettings.ini",
				StringComparison.OrdinalIgnoreCase));
			Assert.Contains(files, file => file.Path.EndsWith(
				"Game.ini",
				StringComparison.OrdinalIgnoreCase));
			Assert.Contains(files, file => file.Path.EndsWith(
				"Engine.ini",
				StringComparison.OrdinalIgnoreCase));
			Assert.DoesNotContain(files, file => file.Path.EndsWith(
				".bak",
				StringComparison.OrdinalIgnoreCase));
			Assert.DoesNotContain(files, file => file.Path.EndsWith(
				"Notes.md",
				StringComparison.OrdinalIgnoreCase));
			Assert.All(files, file => Assert.Equal(ConfigFormat.StandardINI, file.Format));
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	[Trait("Category", "Regression")]
	public void ConfigEditorAvailabilityHidesLaunchArgumentOnlyGames()
	{
		string root = Path.Combine(
			Path.GetTempPath(),
			$"synix-config-availability-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try
		{
			GameServer valheim = new()
			{
				Game = "Valheim",
				ServerName = "Valheim Test",
				InstallPath = root
			};
			GameServer sevenDays = new()
			{
				Game = "7 Days to Die",
				ServerName = "7D2D Test",
				InstallPath = root
			};

			Assert.False(Core.CanOpenConfigurationEditor(valheim));
			Assert.True(Core.CanOpenConfigurationEditor(sevenDays));
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void ConfigEditorEmbedsItsFormResourceUnderTheFormTypeName()
	{
		Assert.Contains(
			"Synix_Control_Panel.SynixApp.UI.Configuration.ServerConfig.resources",
			typeof(ServerConfig).Assembly.GetManifestResourceNames());
	}

	[Fact]
	public void ConfigEditorRuntimeConstructorLoadsWithoutOpeningAWindow()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ServerConfig editor = new(
					Path.Combine(Path.GetTempPath(), "synix-config-editor-test.ini"),
					ConfigFormat.StandardINI);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();

		Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
		Assert.Null(failure);
	}

	private static GameServer CreateScheduledServer() => new()
	{
		ServerName = "Scheduled Test",
		IsScheduledRestartEnabled = true,
		RestartDays = [true, true, true, true, true, true, true],
		RestartTime = "04:00",
		SmartMaintenanceEnabled = true,
		MaintenanceWaitForPlayers = true,
		MaintenanceMaximumDelayMinutes = 30
	};

	private static string CreateTemporaryServerFolder(GameInfo definition)
	{
		string folder = Path.Combine(
			Path.GetTempPath(),
			$"synix-import-test-{Guid.NewGuid():N}");
		string executable = Path.Combine(folder, definition.ExeName);
		Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
		File.WriteAllBytes(executable, [0x4D, 0x5A]);
		return folder;
	}
}
