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
using Synix_Control_Panel.SynixApp.Database.GameDefinitions;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using System.Text.Json;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class DynamicGameDefinitionTests
{
	[Fact]
	public void EmbeddedDefinitionsAreLoadedFromTheSynixAssembly()
	{
		GameInfo starRupture = GameDatabase.GetGame("StarRupture")!;
		GameInfo beamMp = GameDatabase.GetGame("BeamMP")!;
		IReadOnlyList<GameInfo> games = GameDatabase.GetGameList();

		Assert.True(starRupture.IsEmbeddedDefinition);
		Assert.True(beamMp.IsEmbeddedDefinition);
		Assert.Equal("3809400", starRupture.AppID);
		Assert.Equal(ServerProbeProtocol.Tcp, beamMp.ProbeProtocol);
		Assert.Equal(227, games.Count);
		Assert.Equal(games.Count, TrustedGameDefinitionCatalog.Packages.Count);
		Assert.All(games, game => Assert.True(game.IsEmbeddedDefinition));
		Assert.All(games, game => Assert.True(game.CatalogOrder >= 0));
		Assert.Equal(
			games.Count,
			games.Select(game => game.CatalogOrder).Distinct().Count());
		Assert.Equal(
			games.Select(game => game.CatalogOrder).Order(),
			games.Select(game => game.CatalogOrder));
		Assert.Equal(
			games.Count,
			games.Select(game => game.DefinitionId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.All(
			TrustedGameDefinitionCatalog.Packages,
			package => Assert.EndsWith(".game.json", package.ResourceName));
	}

	[Theory]
	[InlineData("Soulmask", "3017310", 8777, 27016)]
	[InlineData("Rust", "258550", 7777, 28015)]
	[InlineData("Palworld", "2394010", 8211, 8212)]
	[InlineData("Icarus", "2089300", 17777, 27016)]
	public void KnownWorkingGameDefinitionsRemainIntact(
		string gameName,
		string appId,
		int port,
		int queryPort)
	{
		GameInfo game = GameDatabase.GetGame(gameName)!;

		Assert.True(game.IsEmbeddedDefinition);
		Assert.Equal(appId, game.AppID);
		Assert.Equal(port, game.Port);
		Assert.Equal(queryPort, game.QueryPort);
		Assert.False(string.IsNullOrWhiteSpace(game.ExeName));
		Assert.False(string.IsNullOrWhiteSpace(game.RequiredArgs));
	}

	[Theory]
	[InlineData("Automobilista 2", "DedicatedServerCmd.exe", 2)]
	[InlineData("Contagion", "contagionds.exe", 4)]
	[InlineData("Heat", "Server.exe", 2)]
	[InlineData("Hellion", "HELLION_Dedicated.exe", 2)]
	[InlineData("Insurgency: Sandstorm", "InsurgencyServer.exe", 2)]
	[InlineData("Last Oasis", "MistServer.exe", 2)]
	[InlineData("Life is Feudal: Your Own", "ddctd_cm_yo_server.exe", 2)]
	[InlineData("Military Conflict: Vietnam", "srcds_x64.exe", 4)]
	[InlineData("Mordhau", "MordhauServer.exe", 2)]
	[InlineData("Nuclear Dawn", "ndsrv.exe", 4)]
	[InlineData("Project CARS 2", "DedicatedServerCmd.exe", 2)]
	[InlineData("Reign of Kings", "Server.exe", 2)]
	[InlineData("Return to Moria", "MoriaServer.exe", 2)]
	[InlineData("rFactor 2", "Bin64\\rFactor2 Dedicated.exe", 2)]
	[InlineData("Squad", "SquadServer.exe", 2)]
	[InlineData("The Front", "ProjectWar\\Binaries\\Win64\\TheFrontServer.exe", 2)]
	[InlineData("The Isle", "TheIsleServer.exe", 2)]
	public void OfficialWindowsServerLaunchTargetsRemainIntact(
		string gameName,
		string executable,
		int minimumRevision)
	{
		GameInfo game = GameDatabase.GetGame(gameName)!;

		Assert.True(game.IsEmbeddedDefinition);
		Assert.Equal(executable, game.ExeName);
		Assert.True(game.DefinitionRevision >= minimumRevision);
	}

	[Fact]
	public void DysterraSeparatesItsInstallPackageFromItsRuntimeSteamIdentity()
	{
		GameInfo game = GameDatabase.GetGame("Dysterra")!;
		Assert.Equal("2214780", game.AppID);
		Assert.Equal("1527890", game.LaunchBehavior.SteamAppId);
		Assert.True(game.DefinitionRevision >= 3);
		Assert.Contains("-SteamAppId={steamAppID}", game.RequiredArgs);
		Assert.False(game.RequiresSteamLogin);
		Assert.False(game.LaunchBehavior.RunElevated);
		Assert.True(game.LaunchBehavior.AllowLaunchFileExport);
	}

	[Theory]
	[InlineData("0")]
	[InlineData("-1")]
	[InlineData("+1527890")]
	[InlineData(" 1527890")]
	[InlineData("1527890 ")]
	[InlineData("1527890\n")]
	[InlineData("1527890 extra")]
	[InlineData("4294967296")]
	[InlineData("١٥٢٧٨٩٠")]
	public void InvalidRuntimeSteamIdentitiesAreRejected(string steamAppId)
	{
		string json = $$"""
			{
			  "schemaVersion": 1,
			  "id": "runtime-id-test",
			  "game": "Runtime ID Test",
			  "appId": "2214780",
			  "executable": "server.exe",
			  "port": 27015,
			  "queryPort": 27016,
			  "launchBehavior": { "steamAppId": {{JsonSerializer.Serialize(steamAppId)}} }
			}
			""";

		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(json, "runtime-id-test.game.json"));
		Assert.Contains("launchBehavior.steamAppId", exception.Message);
	}

	[Fact]
	public void NuclearDawnUsesTheRequiredSteamLaunchMode()
	{
		GameInfo nuclearDawn = GameDatabase.GetGame("Nuclear Dawn")!;

		Assert.Contains("-steam", nuclearDawn.RequiredArgs, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("-game nucleardawn", nuclearDawn.RequiredArgs, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void PalworldDefinitionRegistersCommunityServersWithoutLegacyPerformanceFlags()
	{
		GameInfo palworld = GameDatabase.GetGame("Palworld")!;

		Assert.Contains("-publiclobby", palworld.RequiredArgs);
		Assert.Contains("-publicip={PublicIP}", palworld.RequiredArgs);
		Assert.Contains("-publicport={port}", palworld.RequiredArgs);
		Assert.DoesNotContain("-useperfthreads", palworld.RequiredArgs, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("-NoAsyncLoadingThread", palworld.RequiredArgs, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("-UseMultithreadForDS", palworld.RequiredArgs, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void EnshroudedDefinitionLimitsServersToSixteenPlayers()
	{
		GameInfo enshrouded = GameDatabase.GetGame("Enshrouded")!;
		GameInfo rust = GameDatabase.GetGame("Rust")!;

		Assert.Equal(16, enshrouded.MaximumPlayers);
		Assert.True(enshrouded.RequiresAdminPassword);
		Assert.True(enshrouded.DefinitionRevision >= 4);
		Assert.Equal(GameDefinition.DefaultMaximumPlayers, rust.MaximumPlayers);
	}

	[Fact]
	public void WindroseDefinitionUsesItsEightPlayerEosProfile()
	{
		GameInfo windrose = GameDatabase.GetGame("Windrose")!;

		Assert.Equal(8, windrose.MaximumPlayers);
		Assert.Equal(ServerProbeProtocol.EpicOnlineServices, windrose.ProbeProtocol);
		Assert.False(windrose.SupportsManualConnectionTesting);
		Assert.Contains(@"R5\Saved\Logs\*.log", windrose.LogPaths);
		Assert.Equal(
			"Server registration finished successfully",
			windrose.LaunchBehavior.ReadyLogText);
		Assert.True(windrose.DefinitionRevision >= 2);
	}

	[Fact]
	public void ValheimDefinitionDeclaresItsRequiredPasswordRules()
	{
		GameInfo valheim = GameDatabase.GetGame("Valheim")!;

		Assert.Equal(5, valheim.MinimumServerPasswordLength);
		Assert.True(valheim.ServerPasswordMustNotAppearInName);
		Assert.True(valheim.DefinitionRevision >= 4);
	}

	[Fact]
	public void EcoDefinitionRequiresOnlineAuthenticationToken()
	{
		GameInfo eco = GameDatabase.GetGame("Eco")!;

		Assert.True(eco.RequiresAuthenticationToken);
		Assert.Equal("Eco User Token", eco.AuthenticationTokenLabel);
		Assert.Equal("https://play.eco/account", eco.AuthenticationTokenHelpUrl);
		Assert.Contains("{auth_token}", eco.RequiredArgs, StringComparison.Ordinal);
		Assert.True(eco.DefinitionRevision >= 3);
	}

	[Fact]
	public void MinecraftControllersAreDeclaredByTheTrustedDefinition()
	{
		GameInfo minecraft = GameDatabase.GetGame("Minecraft Java")!;

		Assert.Equal(
			GameLifecycleControllerKind.Minecraft,
			minecraft.ControlCapabilities.Lifecycle);
		Assert.Equal(
			GameConsoleControllerKind.Minecraft,
			minecraft.ControlCapabilities.Console);
		Assert.Equal(
			GameConfigurationControllerKind.Minecraft,
			minecraft.ControlCapabilities.Configuration);
		Assert.Equal(
			GamePlayerControllerKind.Minecraft,
			minecraft.ControlCapabilities.Players);
		Assert.True(GameDatabase.IsMinecraft(" minecraft bedrock "));
	}

	[Fact]
	public void OrdinaryDefinitionsUseStandardControllersByDefault()
	{
		GameInfo rust = GameDatabase.GetGame("Rust")!;

		Assert.Equal(
			GameLifecycleControllerKind.Standard,
			rust.ControlCapabilities.Lifecycle);
		Assert.Equal(
			GameConsoleControllerKind.None,
			rust.ControlCapabilities.Console);
		Assert.Equal(
			GameConfigurationControllerKind.Generic,
			rust.ControlCapabilities.Configuration);
		Assert.Equal(
			GamePlayerControllerKind.QueryProtocol,
			rust.ControlCapabilities.Players);
		Assert.False(GameDatabase.IsMinecraft("Rust"));
	}

	[Fact]
	public void MinecraftControllerSelectionIsCapabilityDrivenInsteadOfNameDriven()
	{
		EmbeddedGamePackage package = TrustedGameDefinitionCatalog.ParsePackage(
			"""
			{
			  "schemaVersion": 1,
			  "id": "future-java-server",
			  "game": "Future Java Server",
			  "appId": "1",
			  "executable": "server.bat",
			  "port": 25565,
			  "queryPort": 25566,
			  "controlCapabilities": {
			    "lifecycle": "Minecraft",
			    "console": "Minecraft",
			    "configuration": "Minecraft",
			    "players": "Minecraft"
			  }
			}
			""",
			"future-java-server.game.json");

		Assert.True(GameCapabilityResolver.UsesMinecraftLifecycle(package.Definition));
	}

	[Fact]
	public void MinecraftSubControllersRequireMinecraftLifecycleController()
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				"""
				{
				  "schemaVersion": 1,
				  "id": "invalid-controller-test",
				  "game": "Invalid Controller Test",
				  "appId": "1",
				  "executable": "server.exe",
				  "port": 7777,
				  "queryPort": 7778,
				  "controlCapabilities": {
				    "console": "Minecraft"
				  }
				}
				""",
				"invalid-controller-test.game.json"));

		Assert.Contains("without the Minecraft lifecycle", exception.Message);
	}

	[Fact]
	public void EosGamesDoNotAdvertiseUnsupportedPlayerMonitoring()
	{
		GameInfo game = GameDatabase.GetGame("ARK: Survival Ascended")!;
		GameServer server = new()
		{
			Game = game.Game,
			CurrentPlayers = 0,
			MaxPlayers = 70
		};

		Assert.Equal(ServerProbeProtocol.EpicOnlineServices, game.ProbeProtocol);
		Assert.False(GameDatabase.SupportsManualConnectionTesting(game));
		Assert.False(GameDatabase.SupportsPlayerCountMonitoring(game));
		Assert.False(GameDatabase.SupportsPlayerManagement(game));
		Assert.Equal("N/A", server.PlayerCount);
	}

	[Fact]
	public void A2sGamesKeepPlayerMonitoringFeatures()
	{
		GameInfo game = GameDatabase.GetGame("Soulmask")!;
		GameServer server = new()
		{
			Game = game.Game,
			CurrentPlayers = 3,
			MaxPlayers = 16
		};

		Assert.True(GameDatabase.SupportsPlayerCountMonitoring(game));
		Assert.True(GameDatabase.SupportsPlayerManagement(game));
		Assert.Equal("3 / 16", server.PlayerCount);
	}

	[Fact]
	public void ValheimPlayerTrackingFollowsTheCrossplaySetting()
	{
		GameInfo game = GameDatabase.GetGame("Valheim")!;
		Assert.Same(game, GameDatabase.GetGame("Valheim (Crossplay)"));
		Assert.True(game.CrossplayDisablesPlayerTracking);

		GameServer server = new()
		{
			Game = game.Game,
			CurrentPlayers = 2,
			MaxPlayers = 10,
			CrossplayEnabled = true
		};

		Assert.False(GameDatabase.SupportsPlayerCountMonitoring(server));
		Assert.False(GameDatabase.SupportsPlayerManagement(server));
		Assert.Equal("N/A", server.PlayerCount);

		server.CrossplayEnabled = false;
		Assert.True(GameDatabase.SupportsPlayerCountMonitoring(server));
		Assert.True(GameDatabase.SupportsPlayerManagement(server));
		Assert.Equal("2 / 10", server.PlayerCount);
	}

	[Fact]
	public void SevenDaysToDieUsesTheInstalledDedicatedServerLaunchContract()
	{
		GameInfo sevenDays = GameDatabase.GetGame("7 Days to Die")!;

		Assert.Equal("7DaysToDieServer.exe", sevenDays.ExeName);
		Assert.True(sevenDays.DefinitionRevision >= 2);
		Assert.Contains("-configfile=\"serverconfig.xml\"", sevenDays.RequiredArgs);
		Assert.Contains("-logfile \"output_log_dedi_synix.txt\"", sevenDays.RequiredArgs);
		Assert.EndsWith("-dedicated", sevenDays.RequiredArgs);
		Assert.DoesNotContain("-SteamAppId", sevenDays.RequiredArgs, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("-GameWorld", sevenDays.RequiredArgs, StringComparison.OrdinalIgnoreCase);
		Assert.Equal(
			["Navezgane", "Pregen06k01", "Pregen06k02", "Pregen08k01", "Pregen08k02", "RWG"],
			sevenDays.Maps);
	}

	[Fact]
	public void DuneSpecialBehaviorComesFromItsValidatedDefinition()
	{
		GameInfo dune = GameDatabase.GetGame("Dune: Awakening")!;

		Assert.Equal(24, dune.RuntimeRequirements.MinimumSystemMemoryGb);
		Assert.True(dune.RuntimeRequirements.RequiresAvx2);
		Assert.True(dune.RuntimeRequirements.RequiresHardwareVirtualization);
		Assert.True(dune.RuntimeRequirements.RequiresHyperV);
		Assert.True(dune.RuntimeRequirements.RequiresWindowsProfessionalOrHigher);
		Assert.True(dune.LaunchBehavior.RunElevated);
		Assert.Equal(
			GameLifecycleTrackingMode.ExternalDeployment,
			dune.LaunchBehavior.LifecycleTracking);
		Assert.False(dune.LaunchBehavior.AllowLaunchFileExport);
		Assert.Contains("Self-Host Token", dune.LaunchBehavior.ReadyMessage);
	}

	[Fact]
	public void SpaceEngineersUsesItsVisibleOfficialServerManager()
	{
		GameInfo spaceEngineers = GameDatabase.GetGame("Space Engineers")!;

		Assert.Equal("298740", spaceEngineers.AppID);
		Assert.False(spaceEngineers.RequiresSteamLogin);
		Assert.Equal(
			"DedicatedServer64\\SpaceEngineersDedicated.exe",
			spaceEngineers.ExeName);
		Assert.Contains("-path", spaceEngineers.RequiredArgs);
		Assert.Contains("{InstallPath}\\Instance", spaceEngineers.RequiredArgs);
		Assert.DoesNotContain("-console", spaceEngineers.RequiredArgs);
		Assert.DoesNotContain("-noconsole", spaceEngineers.RequiredArgs);
		Assert.Equal(
			ConfigFileCreationMode.GameGenerated,
			spaceEngineers.ConfigFileCreation);
		Assert.Equal(
			"Instance\\SpaceEngineers-Dedicated.cfg",
			spaceEngineers.RelativeConfigPath);
		Assert.Equal(6, spaceEngineers.RuntimeRequirements.MinimumSystemMemoryGb);
		Assert.Equal(
			DotNetFrameworkRequirement.NetFramework48,
			spaceEngineers.RuntimeRequirements.MinimumDotNetFramework);
		Assert.Contains(
			VisualCppRedistributableRequirement.VisualCpp2013X64,
			spaceEngineers.RuntimeRequirements.VisualCppRedistributables);
		Assert.Contains(
			VisualCppRedistributableRequirement.VisualCpp2015To2022X64,
			spaceEngineers.RuntimeRequirements.VisualCppRedistributables);
		Assert.True(spaceEngineers.LaunchBehavior.RequiresVisibleWindow);
		Assert.False(spaceEngineers.LaunchBehavior.RunElevated);
		Assert.Contains("Local/Console", spaceEngineers.WarningMessage);

		Assert.True(TrustedGameDefinitionCatalog.TryGetPackage(
			spaceEngineers.Game,
			out EmbeddedGamePackage? package));
		EmbeddedPostInstallAction action = Assert.Single(package!.PostInstallActions);
		Assert.Equal(TrustedPostInstallActionType.EnsureDirectory, action.Type);
		Assert.Equal("Instance", action.TargetDirectory);
	}

	[Fact]
	public void RustDefinitionOffersOnlyTheTrustedOxideRuntime()
	{
		GameInfo rust = GameDatabase.GetGame("Rust")!;
		GameServer oxideServer = new()
		{
			Game = rust.Game,
			ServerFramework = "Oxide"
		};

		Assert.Equal(["Oxide"], rust.SupportedServerFrameworks);
		Assert.Equal(
			ConfigFileCreationMode.SynixTemplate,
			rust.ConfigFileCreation);
		Assert.True(GameFix.CanResetManagedConfiguration(oxideServer));
		Assert.True(OxideRuntimeManager.IsEnabled(oxideServer, rust));
		oxideServer.ServerFramework = OxideRuntimeManager.VanillaFrameworkName;
		Assert.False(OxideRuntimeManager.IsEnabled(oxideServer, rust));
		oxideServer.ServerFrameworkVersion =
			OxideRuntimeManager.VanillaRestoreRequiredVersion;
		Assert.True(OxideRuntimeManager.RequiresVanillaRestore(oxideServer, rust));
	}

	[Fact]
	public void OxideCannotBeEnabledByAnotherGameDefinition()
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				"""
				{
				  "schemaVersion": 1,
				  "definitionRevision": 1,
				  "id": "not-rust",
				  "catalogOrder": 0,
				  "game": "Not Rust",
				  "appId": "1",
				  "executable": "server.exe",
				  "port": 7777,
				  "queryPort": 7778,
				  "supportedServerFrameworks": ["Oxide"]
				}
				""",
				"not-rust.game.json"));

		Assert.Contains("trusted Rust definition", exception.Message);
	}

	[Theory]
	[InlineData("Atlas", "PVP", "False")]
	[InlineData("Atlas", "PVE", "True")]
	[InlineData("PixARK", "PVP", "False")]
	[InlineData("PixARK", "PVE", "True")]
	[InlineData("Palworld", "PVP", "False")]
	[InlineData("Palworld", "PVE", "True")]
	[InlineData("HumanitZ", "PVP", "false")]
	[InlineData("HumanitZ", "PVE", "true")]
	[InlineData("Longvinter", "PVP", "false")]
	[InlineData("Longvinter", "PVE", "true")]
	[InlineData("Rust", "PVP", "false")]
	[InlineData("Rust", "PVE", "true")]
	[InlineData("Soulmask", "PVP", "pvp")]
	[InlineData("Soulmask", "PVE", "pve")]
	[InlineData("Unturned", "PVP", "PvP")]
	[InlineData("Unturned", "PVE", "PvE")]
	public void FriendlyPvpAndPveChoicesUseEachGamesExactValue(
		string gameName,
		string selectedMode,
		string expectedValue)
	{
		GameInfo game = GameDatabase.GetGame(gameName)!;

		Assert.Equal(expectedValue, GameFix.ResolveGameModeValue(game, selectedMode));
	}

	[Theory]
	[InlineData("HumanitZ")]
	[InlineData("Longvinter")]
	[InlineData("Palworld")]
	public void ManagedPvpGamesKeepFriendlyPvpAndPveChoices(string gameName)
	{
		GameInfo game = GameDatabase.GetGame(gameName)!;

		Assert.Contains("PVP", game.GameModes);
		Assert.Contains("PVE", game.GameModes);
	}

	[Theory]
	[InlineData("Conan Exiles")]
	[InlineData("Dune: Awakening")]
	[InlineData("Myth of Empires")]
	public void UnmanagedModeChoicesAreNotShownAsIfTheyWereApplied(string gameName)
	{
		GameInfo game = GameDatabase.GetGame(gameName)!;

		Assert.Empty(game.GameModes);
		Assert.DoesNotContain("{mode}", game.RequiredArgs);
	}

	[Theory]
	[InlineData("ARK: Survival Ascended", true, "True")]
	[InlineData("ARK: Survival Ascended", false, "False")]
	[InlineData("Rust", true, "true")]
	[InlineData("Rust", false, "false")]
	public void BooleanSettingsUseEachGamesExactValue(
		string gameName,
		bool enabled,
		string expectedValue)
	{
		GameInfo game = GameDatabase.GetGame(gameName)!;

		Assert.Equal(expectedValue, GameFix.ResolveBooleanValue(game, enabled));
	}

	[Fact]
	public void RconDefinitionsUseTheRconPasswordAndCorrectProtocolSyntax()
	{
		IReadOnlyList<EmbeddedGamePackage> packages = TrustedGameDefinitionCatalog.Packages;
		GameInfo rust = packages.Single(package =>
			package.Definition.DefinitionId == "rust").Definition;

		Assert.Equal(
			"+rcon.port {rcon_port} +rcon.password \"{rcon_pass}\" +rcon.web 1",
			rust.RconSyntax);
		Assert.All(
			packages.Where(package =>
				!string.IsNullOrWhiteSpace(package.Definition.RconSyntax)),
			package =>
			{
				Assert.Contains("{rcon}", package.Definition.RequiredArgs);
				Assert.DoesNotContain("{adminpass}", package.Definition.RconSyntax);
			});
		Assert.All(
			packages.Where(package =>
				package.Definition.RconSyntax.StartsWith(
					"+rcon_password",
					StringComparison.Ordinal)),
			package => Assert.Equal(
				"+rcon_password \"{rcon_pass}\"",
				package.Definition.RconSyntax));
	}

	[Fact]
	public void DarkMessiahDefinitionIncludesTransparentFirstStartRequirements()
	{
		GameInfo game = GameDatabase.GetGame(
			"Dark Messiah of Might & Magic Dedicated Server")!;
		GameServer server = new()
		{
			Game = game.Game,
			ServerName = "Test"
		};
		string warning = WarningDatabase.GetWarningText(server);

		Assert.Equal("2145", game.AppID);
		Assert.Equal("srcds.exe", game.ExeName);
		Assert.Equal("ctf_3", game.Maps[0]);
		Assert.Equal(["0", "1", "2", "3", "4"], game.GameModes);
		Assert.True(game.NeedsConfigWarning);
		Assert.Contains("cannot download, copy, or redistribute", warning);
		Assert.Contains("must be completed by the user", warning);
	}

	[Fact]
	public void InstalledServersDoNotInheritExecutableGameDefinitions()
	{
		Assert.False(typeof(GameDefinition).IsAssignableFrom(typeof(GameServer)));

		string json = Core.SerializeServersForStorage(
			[new GameServer
			{
				Game = "BeamMP",
				ServerName = "Test",
				InstallPath = @"C:\Synix\Games\BeamMP\Test",
				Port = 30814,
				QueryPort = 30814
			}]);

		Assert.Contains("\"Game\": \"BeamMP\"", json);
		Assert.DoesNotContain("\"AppID\"", json);
		Assert.DoesNotContain("\"ExeName\"", json);
		Assert.DoesNotContain("\"RequiredArgs\"", json);
	}

	[Fact]
	public void EmbeddedTemplatesAutomaticallyBecomeManagedConfigurations()
	{
		Assert.True(GameFix.TryGetConfiguration(
			"StarRupture",
			out ConfigurationDefinition? definition));
		Assert.IsType<EmbeddedTemplateConfigurationDefinition>(definition);

		GameManagementCapability capabilities =
			GameFix.GetManagementCapabilities(GameDatabase.GetGame("StarRupture"));
		Assert.True((capabilities & GameManagementCapability.Port) != 0);
	}

	[Theory]
	[InlineData("Palworld")]
	[InlineData("ARK: Survival Evolved")]
	[InlineData("ARK: Survival Ascended")]
	[InlineData("Valheim")]
	[InlineData("Arma Reforger")]
	public void DocumentedDedicatedServerCrossplayControlsAreExposed(string game)
	{
		GameManagementCapability capabilities =
			GameFix.GetManagementCapabilities(GameDatabase.GetGame(game));

		Assert.True((capabilities & GameManagementCapability.Crossplay) != 0);
	}

	[Fact]
	public void TemplatePlaceholdersAutomaticallyExposeSettingsCapabilities()
	{
		EmbeddedGamePackage package = TrustedGameDefinitionCatalog.ParsePackage(
			"""
			{
			  "schemaVersion": 1,
			  "id": "capability-test",
			  "game": "Capability Test",
			  "appId": "1",
			  "executable": "server.exe",
			  "arguments": "",
			  "port": 7777,
			  "queryPort": 7778,
			  "configFileCreation": "SynixTemplate",
			  "format": "JSON",
			  "configuration": {
			    "schemaVersion": 1,
			    "templates": [
			      {
			        "relativePath": "config.json",
			        "content": "{ \"maxPlayers\": {MaxPlayers}, \"password\": \"{Password}\" }"
			      }
			    ]
			  }
			}
			""",
			"capability-test.game.json");

		EmbeddedTemplateConfigurationDefinition definition = new(
			package.Definition.Game,
			package.Configuration!);

		Assert.True(
			(definition.SupportedInputs & ManagedConfigurationInput.MaxPlayers) != 0);
		Assert.True(
			(definition.SupportedInputs & ManagedConfigurationInput.ServerPassword) != 0);
	}

	[Fact]
	public void UnsafeDefinitionPathsAreRejected()
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				"""
				{
				  "schemaVersion": 1,
				  "id": "unsafe-test",
				  "game": "Unsafe Test",
				  "appId": "1",
				  "executable": "..\\outside.exe",
				  "port": 7777,
				  "queryPort": 7778
				}
				""",
				"unsafe-test.game.json"));

		Assert.Contains("unsafe executable path", exception.Message);
	}

	[Fact]
	public void UnknownDefinitionPropertiesAreRejected()
	{
		Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				"""
				{
				  "schemaVersion": 1,
				  "id": "unknown-field-test",
				  "game": "Unknown Field Test",
				  "appId": "1",
				  "executable": "server.exe",
				  "port": 7777,
				  "queryPort": 7778,
				  "pluginAssembly": "untrusted.dll"
				}
				""",
				"unknown-field.game.json"));
	}

	[Theory]
	[InlineData("-port {Port}", "")]
	[InlineData("-port {unknown}", "")]
	[InlineData("{rcon}", "+rcon.password {RconPassword}")]
	public void UnsupportedOrIncorrectlyCapitalizedArgumentTagsAreRejected(
		string arguments,
		string rconSyntax)
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				$$"""
				{
				  "schemaVersion": 1,
				  "definitionRevision": 1,
				  "id": "invalid-tag-test",
				  "catalogOrder": 0,
				  "game": "Invalid Tag Test",
				  "appId": "1",
				  "executable": "server.exe",
				  "arguments": "{{arguments}}",
				  "rconSyntax": "{{rconSyntax}}",
				  "port": 7777,
				  "queryPort": 7778
				}
				""",
				"invalid-tag-test.game.json"));

		Assert.Contains("unsupported or incorrectly capitalized", exception.Message);
	}

	[Fact]
	public void DocumentedRconTagsAreAccepted()
	{
		EmbeddedGamePackage package = TrustedGameDefinitionCatalog.ParsePackage(
			"""
			{
			  "schemaVersion": 1,
			  "definitionRevision": 1,
			  "id": "rcon-tag-test",
			  "catalogOrder": 0,
			  "game": "RCON Tag Test",
			  "appId": "1",
			  "executable": "server.exe",
			  "arguments": "{rcon}",
			  "rconSyntax": "+rcon.port {rcon_port} +rcon.password {rcon_pass} +enabled {rcon_enabled} +admin {adminpass} -SteamAppId={steamAppID}",
			  "port": 7777,
			  "queryPort": 7778
			}
			""",
			"rcon-tag-test.game.json");

		Assert.Equal("RCON Tag Test", package.Definition.Game);
	}

	[Theory]
	[InlineData("{rcon}", "")]
	[InlineData("", "+rcon_password {rcon_pass}")]
	public void RconSyntaxAndLaunchTagMustBeDeclaredTogether(
		string arguments,
		string rconSyntax)
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				$$"""
				{
				  "schemaVersion": 1,
				  "definitionRevision": 1,
				  "id": "rcon-pair-test",
				  "catalogOrder": 0,
				  "game": "RCON Pair Test",
				  "appId": "1",
				  "executable": "server.exe",
				  "arguments": "{{arguments}}",
				  "rconSyntax": "{{rconSyntax}}",
				  "port": 7777,
				  "queryPort": 7778
				}
				""",
				"rcon-pair-test.game.json"));

		Assert.Contains("rcon", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void UnsafeDefinitionValueMappingsAreRejected()
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				"""
				{
				  "schemaVersion": 1,
				  "definitionRevision": 1,
				  "id": "unsafe-value-test",
				  "catalogOrder": 0,
				  "game": "Unsafe Value Test",
				  "appId": "1",
				  "executable": "server.exe",
				  "pvpValue": "true&unsafe",
				  "port": 7777,
				  "queryPort": 7778
				}
				""",
				"unsafe-value-test.game.json"));

		Assert.Contains("unsafe pvpValue", exception.Message);
	}

	[Fact]
	public void SharedGoldSrcDefinitionsSelectTheCorrectSteamPackage()
	{
		Dictionary<string, string> expected = new(StringComparer.OrdinalIgnoreCase)
		{
			["Counter-Strike 1.6"] = "90 mod cstrike",
			["Counter-Strike: Condition Zero"] = "90 mod czero",
			["Day of Defeat"] = "90 mod dod",
			["Deathmatch Classic"] = "90 mod dmc",
			["Half-Life: Opposing Force"] = "90 mod gearbox",
			["Ricochet"] = "90 mod ricochet",
			["Team Fortress Classic"] = "90 mod tfc"
		};

		foreach ((string gameName, string appConfig) in expected)
		{
			GameInfo game = GameDatabase.GetGame(gameName)!;
			Assert.Equal("90", game.AppID);
			Assert.Equal("hlds.exe", game.ExeName);
			Assert.Equal(appConfig, game.SteamAppConfig);
			Assert.Equal(ServerProbeProtocol.A2S, game.ProbeProtocol);
		}
	}

	[Fact]
	public void UnsafeSteamAppConfigurationIsRejected()
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				"""
				{
				  "schemaVersion": 1,
				  "definitionRevision": 1,
				  "id": "unsafe-steam-config",
				  "catalogOrder": 0,
				  "game": "Unsafe Steam Config",
				  "appId": "90",
				  "steamAppConfig": "90 mod cstrike +quit",
				  "executable": "hlds.exe",
				  "port": 27015,
				  "queryPort": 27015
				}
				""",
				"unsafe-steam-config.game.json"));

		Assert.Contains("unsafe steamAppConfig", exception.Message);
	}

	[Fact]
	public void CompleteSourceLibraryPassesDefinitionValidation()
	{
		string? projectDirectory =
			Core.FindProjectDirectory(AppContext.BaseDirectory);
		Assert.NotNull(projectDirectory);

		GameDefinitionValidationReport report =
			GameDefinitionValidator.ValidateSourceDirectory(projectDirectory!);

		Assert.True(report.IsValid, report.ToPlainText());
		Assert.Equal(227, report.DefinitionCount);
		Assert.True(report.TemplateCount >= 4);
		Assert.Equal(63, report.PostInstallActionCount);
		Assert.True(report.ManagedSettingBindingCount >= 8);
		Assert.True(
			report.DefinitionTestCount >= report.DefinitionCount + 4);
		Assert.True(
			report.DefinitionTestCount <= report.DefinitionCount + report.TemplateCount);
	}

	[Fact]
	public void UnsafePostInstallTargetIsRejected()
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				"""
				{
				  "schemaVersion": 1,
				  "definitionRevision": 1,
				  "id": "unsafe-action-test",
				  "catalogOrder": 0,
				  "game": "Unsafe Action Test",
				  "appId": "1",
				  "executable": "server.exe",
				  "port": 7777,
				  "queryPort": 7778,
				  "postInstallActions": [
				    {
				      "type": "CopySteamRuntimeFiles",
				      "targetDirectory": "..\\outside"
				    }
				  ]
				}
				""",
				"unsafe-action-test.game.json"));

		Assert.Contains("unsafe postInstallActions.targetDirectory path", exception.Message);
	}

	[Fact]
	public void PostInstallActionTypeMustBeExplicitAndAllowlisted()
	{
		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			TrustedGameDefinitionCatalog.ParsePackage(
				"""
				{
				  "schemaVersion": 1,
				  "definitionRevision": 1,
				  "id": "missing-action-type",
				  "catalogOrder": 0,
				  "game": "Missing Action Type",
				  "appId": "1",
				  "executable": "server.exe",
				  "port": 7777,
				  "queryPort": 7778,
				  "postInstallActions": [
				    {
				      "targetDirectory": "Binaries/Win64"
				    }
				  ]
				}
				""",
				"missing-action-type.game.json"));

		Assert.Contains("unsupported post-install action type", exception.Message);
	}

	[Fact]
	public void DefinitionBuilderCreatesRevisionedValidatedSourceFiles()
	{
		string root = Path.Combine(
			Path.GetTempPath(),
			$"SynixDefinitionBuilderTests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try
		{
			string templateSource = Path.Combine(root, "server.cfg");
			File.WriteAllText(
				templateSource,
				"name={ServerName}\nport={Port}\ncustom=preserved\n");
			GameDefinitionDraft draft = new()
			{
				Id = "builder-test",
				CatalogOrder = GameDefinitionAuthoring.GetNextCatalogOrder(),
				DefinitionRevision = 2,
				Game = "Builder Test",
				AppId = "1",
				Executable = "server.exe",
				Maps = ["Map One", "Map Two"],
				GameModes = ["PVE", "PVP"],
				Port = 7777,
				QueryPort = 7778,
				ConfigFileCreation = ConfigFileCreationMode.SynixTemplate,
				Format = ConfigFormat.StandardINI,
				RelativeConfigPath = "cfg/server.cfg",
				TemplateSourcePath = templateSource,
				ConfigurationRevision = 3,
				ExternalDataFolderName = "Builder Test",
				RequiredLaunchFiles = ["player-data/server.dat"],
				OptionalLaunchFiles = ["player-data/server.cfg"],
				LaunchFileSetupInstructions =
					"Create the files in the normal game, then copy them into the server folder.",
				NeedsConfigWarning = true,
				WarningMessage = "The user must supply files from their own game installation.",
				IconUrl = "https://example.com/game.png",
				CopySteamRuntimeFiles = true,
				SteamRuntimeTargetDirectory = "Binaries/Win64",
				IsQueryable = false,
				RuntimeRequirements = new GameRuntimeRequirements
				{
					MinimumSystemMemoryGb = 24,
					RequiresAvx2 = true,
					RequiresHardwareVirtualization = true,
					RequiresHyperV = true,
					RequiresWindowsProfessionalOrHigher = true,
					MinimumDotNetFramework =
						DotNetFrameworkRequirement.NetFramework481,
					VisualCppRedistributables =
					[
						VisualCppRedistributableRequirement.VisualCpp2013X64,
						VisualCppRedistributableRequirement.VisualCpp2015To2022X64
					]
				},
				LaunchBehavior = new GameLaunchBehavior
				{
					SteamAppId = "1527890",
					RunElevated = true,
					RequiresVisibleWindow = true,
					LifecycleTracking = GameLifecycleTrackingMode.ExternalDeployment,
					AllowLaunchFileExport = false,
					ReadyMessage = "The deployment passed its readiness checks.",
					ReadyLogText = "Deployment registration complete"
				}
			};

			GameDefinitionSaveResult result =
				GameDefinitionAuthoring.SaveDraft(draft, root);
			EmbeddedGamePackage parsed =
				GameDefinitionAuthoring.ValidateDraft(draft);

			Assert.True(File.Exists(result.DefinitionPath));
			Assert.True(File.Exists(result.TemplatePath));
			Assert.Equal(2, parsed.Definition.DefinitionRevision);
			Assert.Equal(3, parsed.Configuration!.Revision);
			Assert.Equal(3, parsed.Configuration.Templates[0].Revision);
			Assert.Equal(["Map One", "Map Two"], parsed.Definition.Maps);
			Assert.Equal(["PVE", "PVP"], parsed.Definition.GameModes);
			Assert.Equal(["player-data/server.dat"], parsed.Definition.RequiredLaunchFiles);
			Assert.Equal(["player-data/server.cfg"], parsed.Definition.OptionalLaunchFiles);
			Assert.Equal("Builder Test", parsed.Definition.ExternalDataFolderName);
			Assert.True(parsed.Definition.NeedsConfigWarning);
			Assert.Equal(
				"The user must supply files from their own game installation.",
				parsed.Definition.WarningMessage);
			Assert.Equal("https://example.com/game.png", parsed.Definition.IconUrl);
			Assert.Equal(24, parsed.Definition.RuntimeRequirements.MinimumSystemMemoryGb);
			Assert.True(parsed.Definition.RuntimeRequirements.RequiresAvx2);
			Assert.True(parsed.Definition.RuntimeRequirements.RequiresHardwareVirtualization);
			Assert.True(parsed.Definition.RuntimeRequirements.RequiresHyperV);
			Assert.True(parsed.Definition.RuntimeRequirements.RequiresWindowsProfessionalOrHigher);
			Assert.Equal(
				DotNetFrameworkRequirement.NetFramework481,
				parsed.Definition.RuntimeRequirements.MinimumDotNetFramework);
			Assert.Equal(
				2,
				parsed.Definition.RuntimeRequirements.VisualCppRedistributables.Count);
			Assert.Equal("1527890", parsed.Definition.LaunchBehavior.SteamAppId);
			Assert.Contains("\"steamAppId\": \"1527890\"", result.Json);
			Assert.True(parsed.Definition.LaunchBehavior.RunElevated);
			Assert.True(parsed.Definition.LaunchBehavior.RequiresVisibleWindow);
			Assert.Equal(
				GameLifecycleTrackingMode.ExternalDeployment,
				parsed.Definition.LaunchBehavior.LifecycleTracking);
			Assert.False(parsed.Definition.LaunchBehavior.AllowLaunchFileExport);
			Assert.Equal(
				"The deployment passed its readiness checks.",
				parsed.Definition.LaunchBehavior.ReadyMessage);
			Assert.Equal(
				"Deployment registration complete",
				parsed.Definition.LaunchBehavior.ReadyLogText);
			Assert.Single(parsed.PostInstallActions);
			Assert.Contains("\"definitionRevision\": 2", result.Json);
			Assert.Contains("\"requiresVisibleWindow\": true", result.Json);
			Assert.Contains("\"readyLogText\": \"Deployment registration complete\"", result.Json);
			Assert.Contains("\"minimumDotNetFramework\": \"NetFramework481\"", result.Json);
			Assert.Contains("\"VisualCpp2015To2022X64\"", result.Json);
			Assert.Contains("\"pvpValue\": \"PVP\"", result.Json);
			Assert.Contains("\"booleanTrueValue\": \"true\"", result.Json);
		}
		finally
		{
			if (Directory.Exists(root))
				Directory.Delete(root, true);
		}
	}

	[Fact]
	public void DefinitionBuilderCanManageACompleteGameGeneratedConfiguration()
	{
		string root = Path.Combine(
			Path.GetTempPath(),
			$"SynixGeneratedTemplateBuilderTests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try
		{
			string templateSource = Path.Combine(root, "serverconfig.xml");
			File.WriteAllText(
				templateSource,
				"<Server Name=\"{ServerName}\" Port=\"{Port}\" Players=\"{MaxPlayers}\" />");
			GameDefinitionDraft draft = new()
			{
				Id = "generated-template-builder-test",
				CatalogOrder = GameDefinitionAuthoring.GetNextCatalogOrder(),
				Game = "Generated Template Builder Test",
				AppId = "3",
				Executable = "server.exe",
				Port = 26900,
				QueryPort = 26901,
				ConfigFileCreation = ConfigFileCreationMode.GameGenerated,
				Format = ConfigFormat.XML,
				RelativeConfigPath = "serverconfig.xml",
				TemplateSourcePath = templateSource
			};

			EmbeddedGamePackage parsed =
				GameDefinitionAuthoring.ValidateDraft(draft);

			Assert.NotNull(parsed.Configuration);
			Assert.Contains(
				ManagedConfigurationInput.ServerName,
				parsed.Configuration!.ManagedInputs);
			Assert.Contains(
				ManagedConfigurationInput.Port,
				parsed.Configuration.ManagedInputs);
			Assert.Contains(
				ManagedConfigurationInput.MaxPlayers,
				parsed.Configuration.ManagedInputs);
			Assert.Single(parsed.Configuration.Templates);
		}
		finally
		{
			if (Directory.Exists(root))
				Directory.Delete(root, true);
		}
	}

	[Fact]
	public void DefinitionBuilderRequiresRevisionIncreaseBeforeOverwrite()
	{
		string root = Path.Combine(
			Path.GetTempPath(),
			$"SynixDefinitionRevisionTests-{Guid.NewGuid():N}");
		string definitionDirectory = Path.Combine(
			root,
			"Database",
			"GameDefinitions",
			"revision-test");
		Directory.CreateDirectory(definitionDirectory);
		try
		{
			File.WriteAllText(
				Path.Combine(definitionDirectory, "revision-test.game.json"),
				"""
				{
				  "schemaVersion": 1,
				  "definitionRevision": 2,
				  "id": "revision-test",
				  "catalogOrder": 220,
				  "game": "Revision Test",
				  "appId": "1",
				  "executable": "server.exe",
				  "port": 7777,
				  "queryPort": 7778
				}
				""");
			GameDefinitionDraft draft = new()
			{
				Id = "revision-test",
				CatalogOrder = GameDefinitionAuthoring.GetNextCatalogOrder(),
				DefinitionRevision = 2,
				Game = "Revision Test",
				AppId = "1",
				Executable = "server.exe",
				Port = 7777,
				QueryPort = 7778
			};

			InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
				GameDefinitionAuthoring.SaveDraft(draft, root));

			Assert.Contains("Increase definitionRevision above 2", exception.Message);
		}
		finally
		{
			if (Directory.Exists(root))
				Directory.Delete(root, true);
		}
	}

	[Fact]
	public void DefinitionBuilderSavesMultipleConfigurationTemplates()
	{
		string root = Path.Combine(
			Path.GetTempPath(),
			$"SynixMultiTemplateBuilderTests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try
		{
			string primarySource = Path.Combine(root, "server.cfg");
			string secondarySource = Path.Combine(root, "mapcycle.txt");
			File.WriteAllText(primarySource, "hostname={ServerName}\nport={Port}\n");
			File.WriteAllText(secondarySource, "map_one\nmap_two\n");
			GameDefinitionDraft draft = new()
			{
				Id = "multi-template-builder-test",
				CatalogOrder = GameDefinitionAuthoring.GetNextCatalogOrder(),
				Game = "Multi Template Builder Test",
				AppId = "2",
				Executable = "server.exe",
				Port = 27015,
				QueryPort = 27016,
				ConfigFileCreation = ConfigFileCreationMode.SynixTemplate,
				RelativeConfigPath = "game/cfg/server.cfg",
				TemplateSourcePath = primarySource,
				AdditionalTemplates =
				[
					new GameDefinitionTemplateDraft(
						"game/cfg/mapcycle.txt",
						secondarySource)
				],
				ConfigurationRevision = 2
			};

			GameDefinitionSaveResult result =
				GameDefinitionAuthoring.SaveDraft(draft, root);
			EmbeddedGamePackage parsed =
				GameDefinitionAuthoring.ValidateDraft(draft);

			Assert.Equal(2, parsed.Configuration!.Templates.Count);
			Assert.Equal(2, result.TemplatePaths.Count);
			Assert.All(result.TemplatePaths, path => Assert.True(File.Exists(path)));
			Assert.Contains(
				parsed.Configuration.Templates,
				template => template.RelativePath == "game/cfg/mapcycle.txt");
		}
		finally
		{
			if (Directory.Exists(root))
				Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TrustedPostInstallActionsCopyOnlyAllowlistedSteamFiles()
	{
		string root = Path.Combine(
			Path.GetTempPath(),
			$"SynixPostInstallTests-{Guid.NewGuid():N}");
		string install = Path.Combine(root, "install");
		string steam = Path.Combine(root, "steamcmd");
		Directory.CreateDirectory(install);
		Directory.CreateDirectory(steam);
		try
		{
			File.WriteAllText(Path.Combine(steam, "steamclient64.dll"), "trusted-test-file");
			File.WriteAllText(Path.Combine(steam, "not-allowlisted.dll"), "must-not-copy");
			GameServer server = new()
			{
				Game = "StarRupture",
				ServerName = "Test",
				InstallPath = install,
				Port = 7777,
				QueryPort = 7778
			};

			TrustedPostInstallExecutionResult result =
				TrustedPostInstallExecutor.Execute(server, steam);
			string target = Path.Combine(install, "StarRupture", "Binaries", "Win64");

			Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Messages));
			Assert.True(result.Changed);
			Assert.True(File.Exists(Path.Combine(target, "steamclient64.dll")));
			Assert.False(File.Exists(Path.Combine(target, "not-allowlisted.dll")));
		}
		finally
		{
			if (Directory.Exists(root))
				Directory.Delete(root, true);
		}
	}

	[Fact]
	public void TemplateRevisionUpgradePreservesBackupAndCustomSettings()
	{
		EmbeddedGamePackage package = TrustedGameDefinitionCatalog.ParsePackage(
			"""
			{
			  "schemaVersion": 1,
			  "definitionRevision": 1,
			  "id": "upgrade-test",
			  "catalogOrder": 0,
			  "game": "Upgrade Test",
			  "appId": "1",
			  "executable": "server.exe",
			  "port": 7777,
			  "queryPort": 7778,
			  "configFileCreation": "SynixTemplate",
			  "relativeConfigPath": "server.cfg",
			  "format": "StandardINI",
			  "configuration": {
			    "schemaVersion": 1,
			    "revision": 2,
			    "templates": [
			      {
			        "revision": 2,
			        "relativePath": "server.cfg",
			        "content": "name={ServerName}\nport={Port}\n"
			      }
			    ]
			  }
			}
			""",
			"upgrade-test.game.json");
		string root = Path.Combine(
			Path.GetTempPath(),
			$"SynixTemplateUpgradeTests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(root);
		try
		{
			EmbeddedTemplateConfigurationDefinition definition = new(
				package.Definition.Game,
				package.Configuration!);
			GameServer server = new()
			{
				Game = "Upgrade Test",
				ServerName = "Before Upgrade",
				InstallPath = root,
				Port = 7777,
				QueryPort = 7778,
				ManagedConfigurationVersion = 0
			};
			ConfigurationContext first = new(
				server,
				default,
				"upgrade-test",
				string.Empty,
				string.Empty);
			Assert.True(definition.Apply(first).Succeeded);
			string configPath = Path.Combine(root, "server.cfg");
			File.AppendAllText(configPath, "custom=user-value\n");
			server.ServerName = "After Upgrade";
			server.ManagedConfigurationVersion = 1;

			ConfigurationApplyResult upgraded = definition.Apply(new ConfigurationContext(
				server,
				default,
				"upgrade-test",
				string.Empty,
				string.Empty));
			string backupPath = configPath + ".synix.before-template-v2.bak";

			Assert.True(upgraded.Succeeded, upgraded.Message);
			Assert.True(File.Exists(backupPath));
			Assert.Contains("name=Before Upgrade", File.ReadAllText(backupPath));
			Assert.Contains("custom=user-value", File.ReadAllText(configPath));
			Assert.Contains("name=After Upgrade", File.ReadAllText(configPath));
		}
		finally
		{
			if (Directory.Exists(root))
				Directory.Delete(root, true);
		}
	}
}
