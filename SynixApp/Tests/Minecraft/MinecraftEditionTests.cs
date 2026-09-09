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
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using System.Windows.Forms;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class MinecraftEditionTests : IDisposable
{
	private readonly string _testRoot = Path.Combine(
		Path.GetTempPath(),
		"SynixMinecraftEditionTests",
		Guid.NewGuid().ToString("N"));

	public MinecraftEditionTests() => Directory.CreateDirectory(_testRoot);

	[Fact]
	public void BedrockUsesItsOwnExecutableAndDoesNotInheritJavaArguments()
	{
		GameInfo definition = GameDatabase.GetGame("Minecraft")!;
		GameServer server = CreateMinecraft(MinecraftControlProfile.BedrockEdition);
		server.ExtraArgs = "--server-thread-count 4";

		Assert.EndsWith(
			MinecraftControlProfile.BedrockExecutableName,
			GameLaunchCommandBuilder.ResolveExecutablePath(server, definition),
			StringComparison.OrdinalIgnoreCase);
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords(string.Empty, string.Empty, string.Empty),
			out string arguments,
			out string problem), problem);
		Assert.Equal("--server-thread-count 4", arguments);
		Assert.DoesNotContain("server.jar", arguments, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("-Xmx", arguments, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void BedrockConfigurationContainsOnlyBedrockManagedSettings()
	{
		GameServer server = CreateMinecraft(MinecraftControlProfile.BedrockEdition);
		server.ServerName = "Bedrock Home";
		server.Port = 19132;
		server.QueryPort = 19133;
		server.MaxPlayers = 18;
		server.WorldName = "Family World";
		server.WorldSeed = "safe-seed";
		server.GameMode = "Creative";
		MinecraftConfiguration configuration = new();

		ConfigurationApplyResult result = configuration.Apply(new ConfigurationContext(
			server,
			new SynixServerPasswords(string.Empty, string.Empty, string.Empty),
			"bedrock-home",
			"127.0.0.1",
			"127.0.0.1"));
		string content = File.ReadAllText(Path.Combine(server.InstallPath, "server.properties"));

		Assert.True(result.Succeeded, result.Message);
		Assert.Contains("server-name=Bedrock Home", content, StringComparison.Ordinal);
		Assert.Contains("server-port=19132", content, StringComparison.Ordinal);
		Assert.Contains("server-portv6=19133", content, StringComparison.Ordinal);
		Assert.Contains("gamemode=creative", content, StringComparison.Ordinal);
		Assert.DoesNotContain("enable-rcon", content, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("management-server", content, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("1.20.6", false)]
	[InlineData("1.21", true)]
	[InlineData("1.21.1", true)]
	[InlineData("26.1", true)]
	[InlineData("latest", true)]
	public void NeoForgeIsOfferedOnlyForMinecraft121AndNewer(
		string version,
		bool expected)
	{
		Assert.Equal(expected, MinecraftMetadataService.IsNeoForgeCompatibleVersion(version));
	}

	[Fact]
	public void JavaOnlyManagementFeaturesStayDisabledForBedrock()
	{
		GameServer bedrock = CreateMinecraft(MinecraftControlProfile.BedrockEdition);
		bedrock.GameVersion = "1.21.9";
		bedrock.EnableMinecraftManagementProtocol = true;
		GameServer java = CreateMinecraft(MinecraftControlProfile.JavaEdition);
		java.GameVersion = "1.21.9";
		java.EnableMinecraftManagementProtocol = true;

		Assert.False(MinecraftControlProfile.ShouldEnableManagementProtocol(bedrock));
		Assert.False(GameDatabase.SupportsPlayerManagement(bedrock));
		Assert.Equal("Minecraft Bedrock", bedrock.DisplayGameName);
		Assert.True(MinecraftControlProfile.ShouldEnableManagementProtocol(java));
		Assert.True(GameDatabase.SupportsPlayerManagement(java));
		Assert.Equal("Minecraft Java", java.DisplayGameName);
	}

	[Fact]
	public void ConsoleQuickCommandsUseEditionAppropriateServerCommands()
	{
		GameServer java = CreateMinecraft(MinecraftControlProfile.JavaEdition);
		GameServer bedrock = CreateMinecraft(MinecraftControlProfile.BedrockEdition);

		IReadOnlyList<MinecraftQuickCommand> javaCommands =
			MinecraftConsoleDialog.GetQuickCommands(java);
		IReadOnlyList<MinecraftQuickCommand> bedrockCommands =
			MinecraftConsoleDialog.GetQuickCommands(bedrock);

		Assert.Contains(javaCommands, command => command.Command == "whitelist list");
		Assert.Contains(javaCommands, command => command.Command == "save-all");
		Assert.DoesNotContain(javaCommands, command => command.Command == "allowlist list");
		Assert.Contains(bedrockCommands, command => command.Command == "allowlist list");
		Assert.DoesNotContain(bedrockCommands, command => command.Command == "whitelist list");
		Assert.DoesNotContain(bedrockCommands, command => command.Command == "save-all");
		Assert.All(
			javaCommands.Concat(bedrockCommands).Where(command => command.Command == "stop"),
			command => Assert.True(command.IsDangerous));
	}

	[Fact]
	public void ConsoleWindowBuildsQuickCommandsWithoutCoveringTheCommandInput()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				GameServer server = CreateMinecraft(MinecraftControlProfile.JavaEdition);
				using MinecraftConsoleDialog dialog = new(server);
				FlowLayoutPanel quickCommands = Assert.IsType<FlowLayoutPanel>(
					dialog.Controls.Find("minecraftQuickCommands", true).Single());
				Control commandInput = dialog.Controls.Find("minecraftCommandInput", true).Single();

				Assert.Equal(
					MinecraftConsoleDialog.GetQuickCommands(server).Count,
					quickCommands.Controls.Count);
				Assert.True(quickCommands.Bottom <= commandInput.Top);
				Assert.All(
					quickCommands.Controls.Cast<Control>(),
					button => Assert.False(string.IsNullOrWhiteSpace(button.AccessibleDescription)));
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

	[Fact]
	[Trait("Category", "Regression")]
	public void PlayerManagementStatusDoesNotCoverTheActionButtons()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				GameServer server = CreateMinecraft(MinecraftControlProfile.JavaEdition);
				using PlayerManagementCenter dialog = new(server);
				AssertPlayerManagementFooter(dialog);

				dialog.Size = dialog.MinimumSize;
				dialog.PerformLayout();
				AssertPlayerManagementFooter(dialog);
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

	[Fact]
	public void FabricGeneratedJavaTemplateKeepsTheCompleteVerifiedKeySet()
	{
		GameServer server = CreateMinecraft(MinecraftControlProfile.JavaEdition);
		server.MinecraftLoader = MinecraftMetadataService.FabricLoader;
		server.ServerName = "Fabric Home";
		server.Port = 25570;
		server.QueryPort = 25571;
		server.MaxPlayers = 24;
		server.GameMode = MinecraftControlProfile.CreativeGameMode;
		server.EnableRcon = true;
		server.RconPort = 25572;
		MinecraftConfiguration configuration = new();

		ConfigurationApplyResult result = configuration.Apply(new ConfigurationContext(
			server,
			new SynixServerPasswords(string.Empty, string.Empty, "local-rcon-secret"),
			"fabric-home",
			"127.0.0.1",
			"127.0.0.1"));
		string content = File.ReadAllText(Path.Combine(server.InstallPath, "server.properties"));

		Assert.True(result.Succeeded, result.Message);
		Assert.Contains("Verified from a Fabric-generated server.properties file", content);
		Assert.Contains("chat-spam-threshold-seconds=10", content);
		Assert.Contains("enable-code-of-conduct=false", content);
		Assert.Contains("region-file-compression=deflate", content);
		Assert.Contains("server-port=25570", content);
		Assert.Contains("query.port=25571", content);
		Assert.Contains("max-players=24", content);
		Assert.Contains("gamemode=creative", content);
		Assert.Contains("motd=Fabric Home", content);
		Assert.Contains("rcon.password=local-rcon-secret", content);
		Assert.DoesNotContain("{Secret}", content);
		Assert.DoesNotContain("{Password}", content);
	}

	[Theory]
	[Trait("Category", "Regression")]
	[InlineData("Survival", "Survival")]
	[InlineData("survival", "Survival")]
	[InlineData("Creative", "Creative")]
	[InlineData("creative", "Creative")]
	[InlineData("Adventure", "Adventure")]
	[InlineData("PVE", "Survival")]
	[InlineData("PVP", "Survival")]
	[InlineData("", "Survival")]
	public void MinecraftGameModesReplaceLegacyPveAndPvpValues(
		string savedValue,
		string expected)
	{
		Assert.Equal(expected, MinecraftControlProfile.NormalizeGameMode(savedValue));
	}

	[Fact]
	public void MinecraftDefinitionOffersOnlyNativeGameModes()
	{
		GameInfo definition = GameDatabase.GetGame("Minecraft")!;

		Assert.Equal(
			MinecraftControlProfile.GameModes,
			definition.GameModes);
		Assert.DoesNotContain("PVE", definition.GameModes);
		Assert.DoesNotContain("PVP", definition.GameModes);
		Assert.True(
			(GameFix.GetManagedConfigurationInputs("Minecraft") &
			 ManagedConfigurationInput.GameMode) != ManagedConfigurationInput.None);
	}

	[Theory]
	[InlineData(MinecraftMetadataService.VanillaLoader)]
	[InlineData(MinecraftMetadataService.FabricLoader)]
	[InlineData(MinecraftMetadataService.ForgeLoader)]
	[InlineData(MinecraftMetadataService.NeoForgeLoader)]
	public void EveryJavaLoaderUsesTheSharedMinecraftServerProperties(string loader)
	{
		GameServer server = CreateMinecraft(MinecraftControlProfile.JavaEdition);
		server.MinecraftLoader = loader;
		server.GameMode = MinecraftControlProfile.AdventureGameMode;
		MinecraftConfiguration configuration = new();

		ConfigurationApplyResult result = configuration.Apply(new ConfigurationContext(
			server,
			new SynixServerPasswords(string.Empty, string.Empty, string.Empty),
			"loader-test",
			"127.0.0.1",
			"127.0.0.1"));
		string content = File.ReadAllText(Path.Combine(server.InstallPath, "server.properties"));

		Assert.True(result.Succeeded, result.Message);
		Assert.Contains("gamemode=adventure", content);
	}

	[Fact]
	public void ExistingBedrockServerIsDetectedAndImportedAsBedrock()
	{
		string folder = Path.Combine(_testRoot, "existing-bedrock");
		Directory.CreateDirectory(folder);
		File.WriteAllText(
			Path.Combine(folder, MinecraftControlProfile.BedrockExecutableName),
			"placeholder");

		ExistingServerDetection detection = Assert.Single(
			ExistingServerImport.Detect(folder),
			candidate => candidate.MinecraftEdition == MinecraftControlProfile.BedrockEdition);
		GameServer imported = ExistingServerImport.Create(
			folder,
			detection.Game,
			"Imported Bedrock",
			19132,
			19133,
			[],
			detection.MinecraftEdition);

		Assert.Equal(MinecraftControlProfile.BedrockEdition, imported.MinecraftEdition);
		Assert.True(imported.PreserveImportedConfiguration);
		Assert.EndsWith(
			MinecraftControlProfile.BedrockExecutableName,
			detection.ExecutablePath,
			StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void UnrelatedGameKeepsItsOriginalExecutableAndArguments()
	{
		GameInfo definition = GameDatabase.GetGame("Rust")!;
		GameServer server = new()
		{
			Game = definition.Game,
			InstallPath = @"C:\Synix\Rust",
			ServerName = "Rust One",
			Port = definition.Port,
			QueryPort = definition.QueryPort,
			MaxPlayers = 25,
			MaxRam = 8,
			WorldName = "Procedural Map",
			WorldSeed = "12345"
		};

		Assert.Equal(
			Path.Combine(server.InstallPath, definition.ExeName),
			GameLaunchCommandBuilder.ResolveExecutablePath(server, definition));
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			server,
			definition,
			definition.AppID,
			new SynixServerPasswords(string.Empty, string.Empty, string.Empty),
			out string arguments,
			out string problem), problem);
		Assert.Contains("+server.port", arguments, StringComparison.OrdinalIgnoreCase);
	}

	public void Dispose()
	{
		try { Directory.Delete(_testRoot, recursive: true); }
		catch (Exception suppressedException) { Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException); }
	}

	private GameServer CreateMinecraft(string edition) => new()
	{
		Game = "Minecraft",
		MinecraftEdition = edition,
		MinecraftLoader = MinecraftMetadataService.VanillaLoader,
		MinecraftLoaderVersion = "Official",
		GameVersion = "latest",
		InstallPath = Path.Combine(_testRoot, Guid.NewGuid().ToString("N")),
		ServerName = "Minecraft Test",
		Port = edition == MinecraftControlProfile.BedrockEdition ? 19132 : 25565,
		QueryPort = edition == MinecraftControlProfile.BedrockEdition ? 19133 : 25566,
		RconPort = 25575,
		MaxPlayers = 10,
		MaxRam = 4,
		WorldName = "world",
		WorldSeed = "12345"
	};

	private static void AssertPlayerManagementFooter(PlayerManagementCenter dialog)
	{
		Control grid = dialog.Controls.Find("playerManagementGrid", true).Single();
		Control status = dialog.Controls.Find("playerManagementStatus", true).Single();
		Control refresh = dialog.Controls.Find("playerManagementRefresh", true).Single();
		Control close = dialog.Controls.Find("playerManagementClose", true).Single();
		Control kick = dialog.Controls.Find("playerManagementKick", true).Single();
		Control allowlist = dialog.Controls.Find("playerManagementAddtoAllowlist", true).Single();
		Control makeOperator = dialog.Controls.Find("playerManagementMakeOperator", true).Single();

		Assert.True(grid.Bottom < status.Top);
		Assert.All(
			new[] { kick, allowlist, makeOperator, refresh, close },
			button =>
			{
				Assert.True(status.Bottom <= button.Top);
				Assert.True(button.Bottom <= dialog.ClientSize.Height);
			});
		Assert.True(makeOperator.Right < refresh.Left);
	}
}
