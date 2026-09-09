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
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixEngine;
using Synix_Control_Panel.SynixEngine.ModManagement;
using System.IO.Compression;
using System.Windows.Forms;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class ModPluginManagerTests
{
	[Fact]
	public void ServerOptionsOnlyShowTheModManagerForSupportedGamesAcrossTheEntireCatalog()
	{
		string profileRoot = CreateTestDirectory();
		string? previousRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;
			HashSet<string> supportedGames = new(StringComparer.OrdinalIgnoreCase)
			{
				"Minecraft", "Rust", "7 Days to Die",
				"ARK: Survival Evolved", "ARK: Survival Ascended"
			};
			Assert.True(GameDatabase.GetGames.Count > supportedGames.Count);
			Exception? failure = null;
			Thread thread = new(() =>
			{
				try
				{
					using ToolStripMenuItem item = new();
					foreach (GameInfo game in GameDatabase.GetGames)
					{
						foreach (string status in new[] { "Stopped", "Running" })
						{
							GameServer server = new() { Game = game.Game, Status = status };
							bool expected = supportedGames.Contains(game.Game);
							Assert.True(ModSystemCatalog.CanManageAddOns(server) == expected,
								$"Unexpected mod support for {game.Game} ({status}).");
							MainGUI.UpdateModPluginManagerMenuItem(item, server);
							// Available records visibility even while the parent menu is closed.
							Assert.Equal(expected, item.Available);
							Assert.Equal(expected, item.Enabled);
						}
					}

					// Reuse the same item as selection changes, including no selection.
					foreach (GameServer? server in new GameServer?[]
					{
						new() { Game = "Rust" }, new() { Game = "Satisfactory" },
						new() { Game = "Minecraft", MinecraftEdition = "Bedrock" },
						new() { Game = "Minecraft Bedrock" },
						new() { Game = "Minecraft", MinecraftLoader = "Forge" },
						new() { Game = "Unknown game" }, null
					})
					{
						MainGUI.UpdateModPluginManagerMenuItem(item, server);
						bool expected = server?.Game is "Rust" || server?.MinecraftLoader is "Forge";
						Assert.Equal(expected, item.Available);
						Assert.Equal(expected, item.Enabled);
					}
				}
				catch (Exception exception)
				{
					failure = exception;
				}
			}) { IsBackground = true };
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "The menu checks did not finish.");
			Assert.Null(failure);
		}
		finally
		{
			ModSystemCatalog.ExternalProfileRootOverride = previousRoot;
			Directory.Delete(profileRoot, true);
		}
	}

	[Theory]
	[InlineData("DetectedOnly", "FileImport", false)]
	[InlineData("Managed", "DetectionOnly", false)]
	[InlineData("Managed", "FileImport", true)]
	public void MenuSupportRequiresBothAManagedProfileAndAnActionableTarget(
		string supportLevel, string mode, bool expected)
	{
		string profileRoot = CreateTestDirectory();
		string? previousRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;
			File.WriteAllText(Path.Combine(profileRoot, "test.modsystem.json"), $$"""
				{
				  "schemaVersion": 1,
				  "profiles": [{
				    "id": "test-game", "displayName": "Test profile",
				    "gameNames": ["Test Game"], "supportLevel": "{{supportLevel}}",
				    "catalogUrl": "https://example.com/mods",
				    "targets": [{
				      "id": "mods", "displayName": "Mods", "kind": "Mod",
				      "mode": "{{mode}}", "relativePath": "mods", "allowedExtensions": [".jar"]
				    }]
				  }]
				}
				""");
			Assert.Equal(expected, ModSystemCatalog.CanManageAddOns(new() { Game = "Test Game" }));
		}
		finally
		{
			ModSystemCatalog.ExternalProfileRootOverride = previousRoot;
			Directory.Delete(profileRoot, true);
		}
	}

	[Fact]
	public void SharedMenuStylerAppliesTheSynixMenuDesign()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ContextMenuStrip menu = new();
				ToolStripMenuItem item = new("Nexus Mods");
				menu.Items.Add(item);

				SynixMenuStyler.Apply(menu);

				Assert.IsType<SynixMenuRenderer>(menu.Renderer);
				Assert.False(menu.ShowImageMargin);
				Assert.Equal(new Padding(0, 4, 0, 4), item.Padding);
				Assert.Equal(11F, menu.Font.Size);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void SharedGridThemeDoesNotAddTheDashboardTooltip()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using TestGrid grid = new();
				grid.Columns.Add("Name", "NAME");
				grid.Rows.Add("Example");

				GridStyler.DarkTheme(grid);
				grid.RaiseCellMouseEnter(0, 0);
				Assert.NotEqual(
					"Double-click to view server details",
					grid.Rows[0].Cells[0].ToolTipText);

				GridStyler.EnableServerDetailsInteraction(grid);
				grid.RaiseCellMouseEnter(0, 0);
				Assert.Equal(
					"Double-click to view server details",
					grid.Rows[0].Cells[0].ToolTipText);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Theory]
	[InlineData("Scanning package... found 1 threat.", true)]
	[InlineData("Scanning package... found 3 threats.", true)]
	[InlineData("Scan finished. Found 0 threats.", false)]
	[InlineData("MpCmdRun.exe: hr = 0x80070020.", false)]
	[InlineData("Threat detected: Example", true)]
	[InlineData("No threat was remediated. Found 1 threat.", true)]
	public void DefenderOutputOnlyBlocksConfirmedThreats(
		string output,
		bool expected)
	{
		Assert.Equal(expected, ModSecurityScanner.OutputReportsThreat(output));
	}

	[Fact]
	public void EmbeddedProfilesDescribeFrameworksInsteadOfIndividualMods()
	{
		string profileRoot = CreateTestDirectory();
		string? previousRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;
			ModSystemProfile rust = Assert.Single(ModSystemCatalog.GetProfiles("Rust"));
			ModSystemProfile minecraft = Assert.Single(ModSystemCatalog.GetProfiles("Minecraft"));
			ModSystemProfile ascended = Assert.Single(
				ModSystemCatalog.GetProfiles("ARK: Survival Ascended"));
			ModSystemProfile evolved = Assert.Single(
				ModSystemCatalog.GetProfiles("ARK: Survival Evolved"));
			ModSystemProfile sevenDays = Assert.Single(
				ModSystemCatalog.GetProfiles("7 Days to Die"));

			Assert.Equal("Oxide/uMod", rust.DisplayName);
			Assert.All(rust.Targets, target => Assert.True(target.CanImport));
			Assert.Equal(2, minecraft.Targets.Count);
			ModInstallTarget idTarget = Assert.Single(ascended.Targets);
			Assert.True(idTarget.CanManageIds);
			Assert.Equal("-mods", idTarget.ArgumentName);
			Assert.Equal("CurseForge", idTarget.ProviderName);
			ModInstallTarget workshopTarget = Assert.Single(evolved.Targets);
			Assert.Equal(ModTargetMode.ConfigurationIds, workshopTarget.Mode);
			Assert.Equal("Steam Workshop", workshopTarget.ProviderName);
			Assert.Equal(2, workshopTarget.IdStores.Count);
			Assert.Contains("-automanagedmods", workshopTarget.RequiredArguments);
			ModInstallTarget sevenDaysTarget = Assert.Single(sevenDays.Targets);
			Assert.Equal("Server Mods folder", sevenDaysTarget.DisplayName);
			Assert.True(sevenDaysTarget.ArchiveOnly);
			Assert.True(sevenDaysTarget.PreserveArchiveContents);
			Assert.Equal("ModInfo.xml", sevenDaysTarget.RequiredArchiveFileName);
			Assert.Equal(2, sevenDays.Catalogs.Count);
			Assert.Contains(sevenDays.Catalogs, catalog =>
				catalog.Name == "Nexus Mods" && catalog.Url.Contains("nexusmods.com"));
			Assert.Contains(sevenDays.Catalogs, catalog =>
				catalog.Name == "The Fun Pimps" && catalog.Url.Contains("thefunpimps.com"));
		}
		finally
		{
			ModSystemCatalog.ExternalProfileRootOverride = previousRoot;
			Directory.Delete(profileRoot, true);
		}
	}

	[Fact]
	public void SevenDaysManagerOffersBothCatalogsWithoutApiIntegration()
	{
		string root = CreateTestDirectory();
		string profileRoot = CreateTestDirectory();
		string? previousRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;
			File.WriteAllText(Path.Combine(root, "7DaysToDieServer.exe"), "server");
			Exception? failure = null;
			Thread thread = new(() =>
			{
				try
				{
					using ModPluginManager manager = new(new GameServer
					{
						Game = "7 Days to Die",
						ServerName = "catalog-test",
						InstallPath = root,
						Status = "Stopped"
					});
					Button browse = Assert.IsAssignableFrom<Button>(
						manager.Controls.Find("browseAddOnCatalog", true).Single());
					Assert.True(browse.Enabled);
					Assert.Equal("Browse Catalogs", browse.Text);
				}
				catch (Exception exception)
				{
					failure = exception;
				}
			})
			{
				IsBackground = true
			};
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The catalog UI did not finish constructing.");
			Assert.Null(failure);
		}
		finally
		{
			ModSystemCatalog.ExternalProfileRootOverride = previousRoot;
			Directory.Delete(root, true);
			Directory.Delete(profileRoot, true);
		}
	}

	[Fact]
	public void SevenDaysArchiveInstallsCompleteModFolderAndRejectsMissingManifest()
	{
		string root = CreateTestDirectory();
		string dataRoot = CreateTestDirectory();
		string sourceRoot = CreateTestDirectory();
		string profileRoot = CreateTestDirectory();
		string? previousDataRoot = ModPackageManager.DataRootOverride;
		string? previousProfileRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			ModPackageManager.DataRootOverride = dataRoot;
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;
			File.WriteAllText(Path.Combine(root, "7DaysToDieServer.exe"), "server");
			GameServer server = new()
			{
				Game = "7 Days to Die",
				ServerName = "seven-days-mod-test",
				InstallPath = root,
				Status = "Stopped"
			};
			ModSystemProfile profile = Assert.Single(ModSystemCatalog.GetProfiles(server.Game));
			ModInstallTarget target = Assert.Single(profile.Targets);
			Assert.True(ModSystemCatalog.Detect(server, profile)!.FrameworkDetected);

			string package = Path.Combine(sourceRoot, "Example Mod.zip");
			using (ZipArchive archive = ZipFile.Open(package, ZipArchiveMode.Create))
			{
				using (StreamWriter info = new(archive.CreateEntry("ModInfo.xml").Open()))
					info.Write("<xml><Name value=\"Example\" /></xml>");
				using (StreamWriter config = new(archive.CreateEntry("Config/blocks.xml").Open()))
					config.Write("<configs />");
				using (StreamWriter asset = new(archive.CreateEntry("Resources/example.bundledata").Open()))
					asset.Write("game asset");
			}

			ModImportResult result = ModPackageManager.Import(
				server,
				profile,
				target,
				package,
				securityContext: new(false));

			Assert.True(File.Exists(Path.Combine(root, "Mods", "Example Mod", "ModInfo.xml")));
			Assert.True(File.Exists(Path.Combine(root, "Mods", "Example Mod", "Config", "blocks.xml")));
			Assert.True(File.Exists(Path.Combine(root, "Mods", "Example Mod", "Resources", "example.bundledata")));
			Assert.Equal(3, result.InstalledFileCount);

			string invalidPackage = Path.Combine(sourceRoot, "No Manifest.zip");
			using (ZipArchive archive = ZipFile.Open(invalidPackage, ZipArchiveMode.Create))
			{
				using StreamWriter config = new(archive.CreateEntry("Config/blocks.xml").Open());
				config.Write("<configs />");
			}
			InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
				ModSecurityScanner.InspectPackageStructure(invalidPackage, target));
			Assert.Contains("ModInfo.xml", exception.Message, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			ModPackageManager.DataRootOverride = previousDataRoot;
			ModSystemCatalog.ExternalProfileRootOverride = previousProfileRoot;
			Directory.Delete(root, true);
			Directory.Delete(dataRoot, true);
			Directory.Delete(sourceRoot, true);
			Directory.Delete(profileRoot, true);
		}
	}

	[Fact]
	public void ArkEvolvedWorkshopIdsUpdateBothIniFilesAndCanRollback()
	{
		string root = CreateTestDirectory();
		string profileRoot = CreateTestDirectory();
		string? previousRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;
			string settingsPath = Path.Combine(
				root,
				"ShooterGame",
				"Saved",
				"Config",
				"WindowsServer",
				"GameUserSettings.ini");
			string gamePath = Path.Combine(Path.GetDirectoryName(settingsPath)!, "Game.ini");
			Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
			const string originalSettings =
				"[ServerSettings]\r\nServerPassword=test\r\nActiveMods=999\r\n";
			const string originalGame =
				"[/Script/ShooterGame.ShooterGameMode]\r\nMaxTribeLogs=100\r\n\r\n" +
				"[ModInstaller]\r\nModIDS=999\r\n";
			File.WriteAllText(settingsPath, originalSettings);
			File.WriteAllText(gamePath, originalGame);
			GameServer server = new()
			{
				Game = "ARK: Survival Evolved",
				ServerName = "ase-workshop-test",
				InstallPath = root,
				Status = "Stopped",
				ExtraArgs = "-log"
			};
			ModInstallTarget target = Assert.Single(
				Assert.Single(ModSystemCatalog.GetProfiles(server.Game)).Targets);

			ProviderIdConfigurationChange change = ModPackageManager.ConfigureProviderIds(
				server,
				target,
				["333", "111", "333"]);

			string updatedSettings = File.ReadAllText(settingsPath);
			string updatedGame = File.ReadAllText(gamePath);
			Assert.Contains("ServerPassword=test", updatedSettings);
			Assert.Contains("ActiveMods=333,111", updatedSettings);
			Assert.Contains("MaxTribeLogs=100", updatedGame);
			Assert.Contains("ModIDS=333", updatedGame);
			Assert.Contains("ModIDS=111", updatedGame);
			Assert.Equal(2, updatedGame.Split("ModIDS=", StringSplitOptions.None).Length - 1);
			Assert.Equal("-log -automanagedmods", server.ExtraArgs);
			Assert.Equal(["333", "111"], ModPackageManager.GetProviderIds(server, target));

			change.Rollback();

			Assert.Equal("-log", server.ExtraArgs);
			Assert.Equal(originalSettings, File.ReadAllText(settingsPath));
			Assert.Equal(originalGame, File.ReadAllText(gamePath));
		}
		finally
		{
			ModSystemCatalog.ExternalProfileRootOverride = previousRoot;
			Directory.Delete(root, true);
			Directory.Delete(profileRoot, true);
		}
	}

	[Fact]
	public void ArkAscendedModIdsKeepOrderAndPreserveOtherArguments()
	{
		ModInstallTarget target = CreateArgumentTarget();

		string updated = ModPackageManager.BuildExtraArgumentsWithIds(
			"-NoBattlEye -mods=111,222 -log",
			target,
			["333", "111", "333"]);

		Assert.Equal("-NoBattlEye -log -mods=333,111", updated);
		Assert.Equal(["333", "111"], ModPackageManager.ParseArgumentIds(updated, target));
	}

	[Fact]
	public void ProviderIdValidationRejectsNamesAndCommands()
	{
		ModInstallTarget target = CreateArgumentTarget();

		Assert.Throws<InvalidDataException>(() =>
			ModPackageManager.BuildExtraArgumentsWithIds(
				string.Empty,
				target,
				["123", "bad-id"]));
		Assert.Throws<InvalidDataException>(() =>
			ModPackageManager.NormalizeProviderIds("123,456 & calc", 100));
	}

	[Fact]
	public void SecurityReviewFlagsPowerfulSourcePluginCapabilities()
	{
		string root = CreateTestDirectory();
		try
		{
			string package = Path.Combine(root, "RemoteAdmin.cs");
			File.WriteAllText(
				package,
				"using System.Diagnostics; class Plugin { void Run() => Process.Start(\"cmd.exe\"); }");
			ModInstallTarget target = CreateFileTarget(".cs");

			IReadOnlyList<ModSecurityFinding> findings =
				ModSecurityScanner.InspectPackageStructure(package, target);

			Assert.Contains(findings, finding =>
				finding.Severity == ModSecurityFindingSeverity.Warning &&
				finding.Message.Contains("start other programs", StringComparison.OrdinalIgnoreCase));
			Assert.Contains(findings, finding =>
				finding.Message.Contains("Command Prompt", StringComparison.OrdinalIgnoreCase));
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void SecurityReviewBlocksExecutablePayloadInsidePluginArchive()
	{
		string root = CreateTestDirectory();
		try
		{
			string package = Path.Combine(root, "unsafe.zip");
			using (ZipArchive archive = ZipFile.Open(package, ZipArchiveMode.Create))
			{
				using (StreamWriter plugin = new(archive.CreateEntry("Safe.cs").Open()))
					plugin.Write("class Safe {}");
				using (StreamWriter executable = new(archive.CreateEntry("tools/payload.exe").Open()))
					executable.Write("not allowed");
			}
			ModInstallTarget target = CreateFileTarget(".cs", allowArchives: true);

			InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
				ModSecurityScanner.InspectPackageStructure(package, target));

			Assert.Contains("program or script", exception.Message, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			Directory.Delete(root, true);
		}
	}

	[Fact]
	public void ImportStopsWhenPackageNoLongerMatchesApprovedHash()
	{
		string root = CreateTestDirectory();
		string dataRoot = CreateTestDirectory();
		string sourceRoot = CreateTestDirectory();
		string? previousDataRoot = ModPackageManager.DataRootOverride;
		try
		{
			ModPackageManager.DataRootOverride = dataRoot;
			string package = Path.Combine(sourceRoot, "Changed.cs");
			File.WriteAllText(package, "class Changed {}");
			GameServer server = new()
			{
				Game = "Test Game",
				ServerName = "security-hash-test",
				InstallPath = root,
				Status = "Stopped"
			};
			ModInstallTarget target = CreateFileTarget(".cs");
			ModSystemProfile profile = CreateManagedProfile(target);

			InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
				ModPackageManager.Import(
					server,
					profile,
					target,
					package,
					new string('0', 64),
					securityContext: new(false)));

			Assert.Contains("changed after its security review", exception.Message, StringComparison.OrdinalIgnoreCase);
			Assert.False(File.Exists(Path.Combine(root, "plugins", "Changed.cs")));
		}
		finally
		{
			ModPackageManager.DataRootOverride = previousDataRoot;
			Directory.Delete(root, true);
			Directory.Delete(dataRoot, true);
			Directory.Delete(sourceRoot, true);
		}
	}

	[Fact]
	public void ImportRefusesAnElevatedSynixSecurityContext()
	{
		string root = CreateTestDirectory();
		string dataRoot = CreateTestDirectory();
		string sourceRoot = CreateTestDirectory();
		string? previousDataRoot = ModPackageManager.DataRootOverride;
		try
		{
			ModPackageManager.DataRootOverride = dataRoot;
			string package = Path.Combine(sourceRoot, "Example.cs");
			File.WriteAllText(package, "class Example {}");
			GameServer server = new()
			{
				Game = "Test Game",
				ServerName = "elevated-import-test",
				InstallPath = root,
				Status = "Stopped"
			};
			ModInstallTarget target = CreateFileTarget(".cs");

			InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
				ModPackageManager.Import(
					server,
					CreateManagedProfile(target),
					target,
					package,
					securityContext: new(true)));

			Assert.Contains("Run as administrator", exception.Message, StringComparison.OrdinalIgnoreCase);
			Assert.False(File.Exists(Path.Combine(root, "plugins", "Example.cs")));
		}
		finally
		{
			ModPackageManager.DataRootOverride = previousDataRoot;
			Directory.Delete(root, true);
			Directory.Delete(dataRoot, true);
			Directory.Delete(sourceRoot, true);
		}
	}

	[Fact]
	public void DataProfilesCannotEnableExecutableOrScriptInstallers()
	{
		const string profile =
			"""
			{
			  "schemaVersion": 1,
			  "profiles": [
			    {
			      "id": "unsafe-profile",
			      "displayName": "Unsafe profile",
			      "supportLevel": "Managed",
			      "gameNames": [ "Test Game" ],
			      "targets": [
			        {
			          "id": "programs",
			          "displayName": "Programs",
			          "kind": "Plugin",
			          "mode": "FileImport",
			          "relativePath": "plugins",
			          "allowedExtensions": [ ".exe" ]
			        }
			      ]
			    }
			  ]
			}
			""";

		InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
			ModSystemCatalog.Parse(profile, "unsafe.modsystem.json"));

		Assert.Contains("dangerous program or script", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ExternalProfileCanReplaceBuiltInRulesWithoutChangingCode()
	{
		string profileRoot = CreateTestDirectory();
		string? previousRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			File.WriteAllText(
				Path.Combine(profileRoot, "rust-update.modsystem.json"),
				"""
				{
				  "schemaVersion": 1,
				  "profiles": [
				    {
				      "id": "rust-umod",
				      "displayName": "Updated community profile",
				      "description": "Updated without changing C#.",
				      "supportLevel": "DetectedOnly",
				      "gameNames": [ "Rust" ],
				      "frameworkName": "Oxide",
				      "frameworkMarkers": [ "oxide" ],
				      "catalogUrl": "https://umod.org/plugins",
				      "restartRequired": false,
				      "targets": [
				        {
				          "id": "plugins",
				          "displayName": "Plugins",
				          "kind": "Plugin",
				          "mode": "DetectionOnly",
				          "providerName": "uMod",
				          "relativePath": "oxide/plugins",
				          "allowedExtensions": [ ".cs" ],
				          "markerPaths": [ "oxide/plugins" ],
				          "frameworkNames": [ "Oxide" ],
				          "allowArchives": false,
				          "scanDirectories": false,
				          "recursive": true,
				          "argumentName": "",
				          "maximumIds": 100
				        }
				      ]
				    }
				  ]
				}
				""");
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;

			ModSystemProfile profile = Assert.Single(ModSystemCatalog.GetProfiles("Rust"));

			Assert.Equal("Updated community profile", profile.DisplayName);
			Assert.False(profile.Targets[0].CanImport);
			Assert.False(ModSystemCatalog.CanManageAddOns(new() { Game = "Rust" }));
		}
		finally
		{
			ModSystemCatalog.ExternalProfileRootOverride = previousRoot;
			Directory.Delete(profileRoot, true);
		}
	}

	[Fact]
	public void UnknownGamesReceiveReadOnlyFolderDiscovery()
	{
		string root = CreateTestDirectory();
		string profileRoot = CreateTestDirectory();
		string? previousRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			Directory.CreateDirectory(Path.Combine(root, "BepInEx", "plugins"));
			File.WriteAllText(Path.Combine(root, "BepInEx", "plugins", "Example.dll"), "test");
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;
			GameServer server = new()
			{
				Game = "Future Dedicated Server",
				ServerName = "future-test",
				InstallPath = root
			};

			ModSystemProfile profile = Assert.Single(ModSystemCatalog.GetProfiles(server));
			ModInventoryItem item = Assert.Single(ModPackageManager.Scan(server, profile));

			Assert.Equal(ModSystemSupportLevel.DetectedOnly, profile.SupportLevel);
			Assert.False(profile.Targets[0].CanImport);
			Assert.Equal("Example", item.Name);
			Assert.Equal("Detected on disk", item.Status);
			Assert.False(ModSystemCatalog.CanManageAddOns(server));
		}
		finally
		{
			ModSystemCatalog.ExternalProfileRootOverride = previousRoot;
			Directory.Delete(root, true);
			Directory.Delete(profileRoot, true);
		}
	}

	[Fact]
	public void LocalImportTracksFilesAndRestoresThePreviousVersion()
	{
		string root = CreateTestDirectory();
		string dataRoot = CreateTestDirectory();
		string sourceRoot = CreateTestDirectory();
		string? previousDataRoot = ModPackageManager.DataRootOverride;
		try
		{
			ModPackageManager.DataRootOverride = dataRoot;
			string plugins = Path.Combine(root, "plugins");
			Directory.CreateDirectory(plugins);
			string installedFile = Path.Combine(plugins, "Welcome.cs");
			string package = Path.Combine(sourceRoot, "Welcome.cs");
			File.WriteAllText(installedFile, "old-version");
			File.WriteAllText(package, "new-version");
			GameServer server = new()
			{
				Game = "Test Game",
				ServerName = "addon-test",
				InstallPath = root,
				Status = "Stopped"
			};
			ModInstallTarget target = new()
			{
				Id = "plugins",
				DisplayName = "Plugins",
				Kind = ModContentKind.Plugin,
				Mode = ModTargetMode.FileImport,
				ProviderName = "Test",
				RelativePath = "plugins",
				AllowedExtensions = [".cs"]
			};
			ModSystemProfile profile = new()
			{
				Id = "test-profile",
				DisplayName = "Test",
				Description = "Test profile",
				SupportLevel = ModSystemSupportLevel.Managed,
				GameNames = ["Test Game"],
				Targets = [target]
			};

			ModImportResult import = ModPackageManager.Import(
				server,
				profile,
				target,
				package,
				securityContext: new(false));
			ModInventoryItem item = Assert.Single(ModPackageManager.Scan(server, profile));
			Assert.Equal("new-version", File.ReadAllText(installedFile));
			Assert.Equal("Healthy", item.Status);
			Assert.Equal("Structural checks only", item.SecurityStatus);
			Assert.True(item.CanRemove);

			ModPackageManager.Remove(server, import.InstallationId);
			Assert.Equal("old-version", File.ReadAllText(installedFile));
		}
		finally
		{
			ModPackageManager.DataRootOverride = previousDataRoot;
			Directory.Delete(root, true);
			Directory.Delete(dataRoot, true);
			Directory.Delete(sourceRoot, true);
		}
	}

	[Fact]
	public void LocalImportRejectsArchivePathsOutsideTheAddOnFolder()
	{
		string root = CreateTestDirectory();
		string dataRoot = CreateTestDirectory();
		string sourceRoot = CreateTestDirectory();
		string? previousDataRoot = ModPackageManager.DataRootOverride;
		try
		{
			ModPackageManager.DataRootOverride = dataRoot;
			string archivePath = Path.Combine(sourceRoot, "unsafe.zip");
			using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
			{
				using StreamWriter writer = new(archive.CreateEntry("../outside.cs").Open());
				writer.Write("unsafe");
			}
			GameServer server = new()
			{
				Game = "Test Game",
				ServerName = "archive-test",
				InstallPath = root,
				Status = "Stopped"
			};
			ModInstallTarget target = new()
			{
				Id = "plugins",
				DisplayName = "Plugins",
				Kind = ModContentKind.Plugin,
				Mode = ModTargetMode.FileImport,
				RelativePath = "plugins",
				AllowedExtensions = [".cs"],
				AllowArchives = true
			};
			ModSystemProfile profile = new()
			{
				Id = "test-profile",
				DisplayName = "Test",
				SupportLevel = ModSystemSupportLevel.Managed,
				GameNames = ["Test Game"],
				Targets = [target]
			};

			Assert.Throws<InvalidDataException>(() =>
				ModPackageManager.Import(
					server,
					profile,
					target,
					archivePath,
					securityContext: new(false)));
			Assert.False(File.Exists(Path.Combine(root, "outside.cs")));
		}
		finally
		{
			ModPackageManager.DataRootOverride = previousDataRoot;
			Directory.Delete(root, true);
			Directory.Delete(dataRoot, true);
			Directory.Delete(sourceRoot, true);
		}
	}

	[Fact]
	public void ManagerWindowConstructsForAProviderIdProfile()
	{
		string root = CreateTestDirectory();
		string profileRoot = CreateTestDirectory();
		string? previousRoot = ModSystemCatalog.ExternalProfileRootOverride;
		try
		{
			ModSystemCatalog.ExternalProfileRootOverride = profileRoot;
			Exception? failure = null;
			Thread thread = new(() =>
			{
				try
				{
					using ModPluginManager manager = new(new GameServer
					{
						Game = "ARK: Survival Ascended",
						ServerName = "asa-test",
						InstallPath = root,
						Status = "Stopped"
					});
					DataGridView inventory = Assert.IsType<DataGridView>(
						manager.Controls.Find("addOnInventoryGrid", true).Single());
					Control checklist = manager.Controls.Find(
						"automaticSafetyChecklistCard", true).Single();
					Control selection = manager.Controls.Find(
						"selectedAddOnDetailsCard", true).Single();
					Label details = Assert.IsType<Label>(
						manager.Controls.Find("selectedAddOnDetails", true).Single());
					Label heading = Assert.IsType<Label>(
						manager.Controls.Find("modPluginManagerHeading", true).Single());

					Assert.DoesNotContain(
						inventory.Rows.Cast<DataGridViewRow>()
							.SelectMany(row => row.Cells.Cast<DataGridViewCell>()),
						cell => cell.ToolTipText == "Double-click to view server details");
					Assert.False(checklist.Bounds.IntersectsWith(selection.Bounds));
					Assert.True(details.Height >= 50);
					Assert.False(heading.UseMnemonic);
					Assert.DoesNotContain("&&", heading.Text);
					Assert.Equal(heading.Text, manager.Text);
				}
				catch (Exception exception)
				{
					failure = exception;
				}
			})
			{
				IsBackground = true
			};
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The manager UI did not finish constructing.");
			Assert.Null(failure);
		}
		finally
		{
			ModSystemCatalog.ExternalProfileRootOverride = previousRoot;
			Directory.Delete(root, true);
			Directory.Delete(profileRoot, true);
		}
	}

	private static ModInstallTarget CreateArgumentTarget() => new()
	{
		Id = "launch-mod-ids",
		DisplayName = "Ordered mod IDs",
		Kind = ModContentKind.Mod,
		Mode = ModTargetMode.ArgumentIds,
		ProviderName = "CurseForge",
		ArgumentName = "-mods",
		MaximumIds = 100
	};

	private static ModInstallTarget CreateFileTarget(
		string extension,
		bool allowArchives = false) => new()
	{
		Id = "plugins",
		DisplayName = "Plugins",
		Kind = ModContentKind.Plugin,
		Mode = ModTargetMode.FileImport,
		ProviderName = "Test",
		RelativePath = "plugins",
		AllowedExtensions = [extension],
		AllowArchives = allowArchives
	};

	private static ModSystemProfile CreateManagedProfile(ModInstallTarget target) => new()
	{
		Id = "test-profile",
		DisplayName = "Test",
		Description = "Test profile",
		SupportLevel = ModSystemSupportLevel.Managed,
		GameNames = ["Test Game"],
		Targets = [target]
	};

	private static string CreateTestDirectory()
	{
		string path = Path.Combine(
			Path.GetTempPath(),
			"Synix.ModManager.Tests",
			Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	private sealed class TestGrid : DataGridView
	{
		internal void RaiseCellMouseEnter(int columnIndex, int rowIndex) =>
			OnCellMouseEnter(new DataGridViewCellEventArgs(columnIndex, rowIndex));
	}
}
