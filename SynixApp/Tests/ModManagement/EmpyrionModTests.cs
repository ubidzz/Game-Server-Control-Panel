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
using Synix_Control_Panel.SynixApp.UI.ServerManagement;
using Synix_Control_Panel.SynixEngine.ModManagement;
using Synix_Control_Panel.SynixApp.Localization;
using System.IO.Compression;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class EmpyrionModTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "SynixEmpyrionMods-" + Guid.NewGuid().ToString("N"));
	private readonly string? _oldData = ModPackageManager.DataRootOverride;
	private readonly string? _oldProfiles = ModSystemCatalog.ExternalProfileRootOverride;
	private readonly GameServer _server;
	private readonly ModSystemProfile _scenarios;
	private readonly ModSystemProfile _mods;
	private string Config => Path.Combine(_server.InstallPath, "dedicated.yaml");
	private const string OriginalConfig = "# Keep comments\r\nServerConfig:\r\n    Srv_Name: 'Test server'\r\n    Srv_Password: 'dummy-test-secret'\r\nGameConfig:\r\n    GameName: ExistingWorld # preserve save\r\n    CustomScenario: 'Default Multiplayer'\r\n    Seed: 12345\r\n";

	public EmpyrionModTests()
	{
		Directory.CreateDirectory(_root);
		ModPackageManager.DataRootOverride = Path.Combine(_root, "history");
		ModSystemCatalog.ExternalProfileRootOverride = Path.Combine(_root, "profiles");
		Directory.CreateDirectory(ModSystemCatalog.ExternalProfileRootOverride);
		_server = new() { Game = EmpyrionAddOns.GameName, ServerName = "Mod test", Status = "Stopped",
			InstallPath = Path.Combine(_root, "server"), WorldName = "Default Multiplayer", WorldSeed = "12345" };
		Directory.CreateDirectory(_server.InstallPath);
		File.WriteAllText(Path.Combine(_server.InstallPath, "EmpyrionLauncher.exe"), "test fixture only");
		File.WriteAllText(Config, OriginalConfig, new UTF8Encoding(true));
		_scenarios = ModSystemCatalog.GetProfiles(_server).Single(p => p.Id == "empyrion-scenarios");
		_mods = ModSystemCatalog.GetProfiles(_server).Single(p => p.Id == "empyrion-server-mods");
	}

	[Fact]
	public void ProfilesExposeTwoRealWorkflowsWithoutProviderIdPretence()
	{
		Assert.True(ModSystemCatalog.CanManageAddOns(_server));
		Assert.Equal(2, ModSystemCatalog.GetProfiles(_server).Count);
		foreach (ModSystemProfile profile in new[] { _scenarios, _mods })
		{
			Assert.True(ModSystemCatalog.Detect(_server, profile)!.FrameworkDetected);
			Assert.True(profile.RestartRequired);
			Assert.False(profile.Targets[0].CanManageIds);
		}
		Assert.Equal(2048, ModPackageLimits.For(_mods.Targets[0]).Entries);
		Assert.Equal(65536, ModPackageLimits.For(_scenarios.Targets[0]).Entries);
	}

	[Theory]
	[InlineData("")]
	[InlineData("3143225812/")]
	[InlineData("Named Scenario/")]
	public void ScenarioImportsNormalizeOneFolderAndDoNotActivateIt(string wrapper)
	{
		byte[] before = File.ReadAllBytes(Config);
		string zip = Zip((wrapper + "gameoptions.yaml", "Options: []"), (wrapper + "Content/Configuration/Test.ecf", "Data"),
			(wrapper + "SharedData/asset.custom", "asset data"));
		ModImportResult result = Import(_scenarios, zip, "User Scenario");
		Assert.Equal(3, result.InstalledFileCount);
		Assert.Equal("asset data", File.ReadAllText(Path.Combine(_server.InstallPath, "Content/Scenarios/User Scenario/SharedData/asset.custom")));
		Assert.Equal(before, File.ReadAllBytes(Config));
		Assert.Equal("Default Multiplayer", _server.WorldName);
		ModInventoryItem item = Assert.Single(ModPackageManager.Scan(_server, _scenarios));
		Assert.Equal("User Scenario", item.Name);
		Assert.Equal(result.InstallationId, item.InstallationId);
		Assert.True(item.CanRemove);
		ModPackageManager.Remove(_server, result.InstallationId);
		Assert.False(File.Exists(item.FullPath + "/gameoptions.yaml"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("ServerMod/")]
	public void CompiledModsPreserveMetadataDependenciesAndAssets(string prefix)
	{
		string zip = Zip((prefix + "_Info.yaml", "Name: ServerMod\n"), (prefix + "Resources/data.asset", "asset"));
		using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Update))
			archive.CreateEntryFromFile(typeof(EmpyrionModTests).Assembly.Location, prefix + "ServerMod.dll");
		IReadOnlyList<ModSecurityFinding> findings = ModSecurityScanner.InspectPackageStructure(zip, _mods.Targets[0]);
		Assert.Contains(findings, f => f.Message.Contains("code", StringComparison.OrdinalIgnoreCase));
		ModImportResult result = Import(_mods, zip);
		Assert.Equal(3, result.InstalledFileCount);
		ModInventoryItem item = Assert.Single(ModPackageManager.Scan(_server, _mods));
		Assert.True(File.Exists(Path.Combine(item.FullPath, "_Info.yaml")));
		Assert.Equal("asset", File.ReadAllText(Path.Combine(item.FullPath, "Resources/data.asset")));
		ModPackageManager.Remove(_server, result.InstallationId);
		Assert.False(File.Exists(Path.Combine(item.FullPath, "ServerMod.dll")));
	}

	[Fact]
	public void ScenarioUpdateBacksUpPreviousFilesAndCanRollbackBeforeWorldCreation()
	{
		string first = Zip(("gameoptions.yaml", "Options: []"), ("Content/Configuration/Test.ecf", "original"));
		Import(_scenarios, first, "Scenario");
		string second = Zip(("gameoptions.yaml", "Options: []"), ("Content/Configuration/Test.ecf", "updated"));
		ModImportResult update = Import(_scenarios, second, "Scenario");
		Assert.Equal("original", File.ReadAllText(Path.Combine(update.BackupFolder, "Scenario/Content/Configuration/Test.ecf")));
		ModPackageManager.Remove(_server, update.InstallationId);
		Assert.Equal("original", File.ReadAllText(Path.Combine(_server.InstallPath, "Content/Scenarios/Scenario/Content/Configuration/Test.ecf")));
	}

	[Theory]
	[InlineData("../outside.txt")]
	[InlineData("Folder/../../outside.txt")]
	[InlineData("payload.cmd")]
	[InlineData("Content/payload.dll")]
	[InlineData("Content/payload.cs")]
	[InlineData("NUL.txt")]
	public void UnsafeScenarioPackagesAreRejectedBeforeAnyServerWrite(string unsafeFile)
	{
		string zip = Zip(("gameoptions.yaml", "Options: []"), ("Content/test.ecf", "data"), (unsafeFile, "unsafe fixture"));
		Assert.ThrowsAny<InvalidDataException>(() => Import(_scenarios, zip, "Scenario"));
		Assert.False(Directory.Exists(Path.Combine(_server.InstallPath, "Content")));
		Assert.Equal(OriginalConfig, File.ReadAllText(Config));
	}

	[Fact]
	public void ActiveScenarioUpdatesCanRollBackWithoutRemovingTheBaselineOrWorld()
	{
		ModImportResult baseline = InstallScenario();
		EmpyrionAddOns.SelectScenario(_server, "Scenario", "NewWorld", () => true);
		string save = Path.Combine(_server.InstallPath, "Saves/Games/NewWorld");
		Directory.CreateDirectory(save);
		File.WriteAllText(Path.Combine(save, "world.dat"), "keep world");
		byte[] config = File.ReadAllBytes(Config);
		ModImportResult update = Import(_scenarios, Zip(("gameoptions.yaml", "Options: [updated]"),
			("Content/test.ecf", "updated"), ("Content/new.ecf", "new asset")), "Scenario");
		Assert.True(Assert.Single(ModPackageManager.Scan(_server, _scenarios)).CanRemove);
		ModPackageManager.Remove(_server, update.InstallationId);
		Assert.Equal("data", File.ReadAllText(Path.Combine(_server.InstallPath, "Content/Scenarios/Scenario/Content/test.ecf")));
		Assert.False(File.Exists(Path.Combine(_server.InstallPath, "Content/Scenarios/Scenario/Content/new.ecf")));
		Assert.Equal("keep world", File.ReadAllText(Path.Combine(save, "world.dat")));
		Assert.Equal(config, File.ReadAllBytes(Config));
		Assert.Throws<InvalidDataException>(() => ModPackageManager.Remove(_server, baseline.InstallationId));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DuplicatePathsAndLinkedDirectoryEntriesAreBlocked(bool linkedDirectory)
	{
		string zip = Zip(("gameoptions.yaml", "Options: []"), ("Content/test.ecf", "data"));
		using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Update))
		{
			ZipArchiveEntry entry = archive.CreateEntry(linkedDirectory ? "Linked/" : "Content/TEST.ecf");
			if (linkedDirectory) entry.ExternalAttributes = (0xA000 << 16);
		}
		Assert.Throws<InvalidDataException>(() => ModSecurityScanner.InspectPackageStructure(zip, _scenarios.Targets[0]));
		Assert.False(Directory.Exists(Path.Combine(_server.InstallPath, "Content")));
	}

	[Fact]
	public void BlueprintsAndSourceArchivesAreNotAcceptedAsScenariosOrCompiledMods()
	{
		string zip = Zip(("Blueprint.ebp", "blueprint"));
		Assert.Throws<InvalidDataException>(() => ModSecurityScanner.InspectPackageStructure(zip, _scenarios.Targets[0]));
		string source = Zip(("Source.cs", "// source, not a compiled mod"));
		Assert.Throws<InvalidDataException>(() => ModSecurityScanner.InspectPackageStructure(source, _mods.Targets[0]));
	}

	[Fact]
	public void FolderImportSnapshotsFilesThenUsesTheSameHashGate()
	{
		string folder = Path.Combine(_root, "3143225812");
		Directory.CreateDirectory(Path.Combine(folder, "Content"));
		File.WriteAllText(Path.Combine(folder, "gameoptions.yaml"), "Options: []");
		File.WriteAllText(Path.Combine(folder, "Content/test.ecf"), "before");
		string preparedPath;
		using (PreparedModPackage prepared = PreparedModPackage.FromFolder(folder, _scenarios.Targets[0]))
		{
			preparedPath = prepared.Path;
			File.WriteAllText(Path.Combine(folder, "Content/test.ecf"), "after");
			Import(_scenarios, prepared.Path, "Local Copy");
		}
		Assert.False(File.Exists(preparedPath));
		Assert.Equal("before", File.ReadAllText(Path.Combine(_server.InstallPath, "Content/Scenarios/Local Copy/Content/test.ecf")));
		Assert.Equal("after", File.ReadAllText(Path.Combine(folder, "Content/test.ecf")));
	}

	[Fact]
	public void ScenarioSelectionProtectsOldWorldAndPersistsOnlyTheTwoYamlFields()
	{
		InstallScenario();
		string save = Path.Combine(_server.InstallPath, "Saves/Games/ExistingWorld");
		Directory.CreateDirectory(save);
		File.WriteAllText(Path.Combine(save, "world.dat"), "original world");
		Assert.Throws<InvalidDataException>(() => EmpyrionAddOns.SelectScenario(_server, "Scenario", "ExistingWorld", () => true));
		byte[] original = File.ReadAllBytes(Config);
		int persisted = 0;
		string backup = EmpyrionAddOns.SelectScenario(_server, "Scenario", "NewWorld", () => { persisted++; return true; });
		Assert.Equal(1, persisted);
		Assert.Equal("Scenario", _server.WorldName);
		Assert.Equal(original, File.ReadAllBytes(backup));
		string expected = OriginalConfig.Replace("ExistingWorld #", "\"NewWorld\" #").Replace("'Default Multiplayer'", "'Scenario'");
		Assert.Equal(expected, File.ReadAllText(Config));
		Assert.True(File.ReadAllBytes(Config).AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
		Assert.Equal("original world", File.ReadAllText(Path.Combine(save, "world.dat")));
		Assert.False(Directory.Exists(Path.Combine(_server.InstallPath, "Saves/Games/NewWorld")));
	}

	[Fact]
	public void FailedServerPersistenceRestoresConfigAndServerEntry()
	{
		InstallScenario();
		byte[] original = File.ReadAllBytes(Config);
		Assert.Throws<InvalidDataException>(() => EmpyrionAddOns.SelectScenario(_server, "Scenario", "NewWorld", () => false));
		Assert.Equal(original, File.ReadAllBytes(Config));
		Assert.Equal("Default Multiplayer", _server.WorldName);
	}

	[Fact]
	public void StaleScenarioDialogCannotOverwriteNewerSelection()
	{
		InstallScenario();
		EmpyrionScenarioState before = EmpyrionAddOns.ReadSelection(_server);
		File.WriteAllText(Config, OriginalConfig.Replace("ExistingWorld", "OtherWorld"));
		Assert.Throws<InvalidDataException>(() => EmpyrionAddOns.SelectScenario(_server, "Scenario", "NewWorld", () => true, before));
		Assert.Contains("OtherWorld", File.ReadAllText(Config));
	}

	[Theory]
	[InlineData("../escape")]
	[InlineData("NUL")]
	[InlineData("trailing.")]
	[InlineData(" name")]
	[InlineData("3143225812")]
	public void InvalidScenarioNamesAreRejected(string name) =>
		Assert.Throws<InvalidDataException>(() => EmpyrionAddOns.ValidateFolderName(name, scenario: true));

	[Fact]
	public void ActiveScenariosAndScenariosWithSavedWorldsCannotBeRemoved()
	{
		ModImportResult import = InstallScenario();
		EmpyrionAddOns.SelectScenario(_server, "Scenario", "NewWorld", () => true);
		Assert.False(Assert.Single(ModPackageManager.Scan(_server, _scenarios)).CanRemove);
		Assert.Throws<InvalidDataException>(() => ModPackageManager.Remove(_server, import.InstallationId));
		File.WriteAllText(Config, OriginalConfig);
		Directory.CreateDirectory(Path.Combine(_server.InstallPath, "Saves/Games/OldWorld"));
		Assert.Throws<InvalidDataException>(() => ModPackageManager.Remove(_server, import.InstallationId));
	}

	[Fact]
	public void RunningServersCannotImportOrChangeScenarios()
	{
		InstallScenario();
		_server.Status = "Running";
		Assert.Throws<InvalidOperationException>(() => Import(_scenarios, Zip(("gameoptions.yaml", "Options: []"), ("Content/test.ecf", "x"))));
		Assert.Throws<InvalidOperationException>(() => EmpyrionAddOns.SelectScenario(_server, "Scenario", "NewWorld", () => true));
	}

	[Fact]
	public void NewDialogsExposeSeparateScenarioAndSaveInputs()
	{
		InstallScenario();
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using ModPackagePicker import = new(_scenarios.Targets[0]);
				Assert.Single(import.Controls.Find("modPackageFolderName", true));
				CapturePreviewIfRequested(import, "scenario-import.png");
				using ModPackagePicker mods = new(_mods.Targets[0]);
				CapturePreviewIfRequested(mods, "mod-import.png");
				using EmpyrionScenarioPicker selection = new(_server);
				Assert.Single(selection.Controls.Find("empyrionScenarioSelector", true));
				Assert.Single(selection.Controls.Find("empyrionSaveName", true));
				Assert.NotEqual("ExistingWorld", selection.SaveName);
				CapturePreviewIfRequested(selection, "scenario-selection.png");
				using ModPluginManager manager = new(_server);
				typeof(ModPluginManager).GetMethod("RefreshInventory", System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.NonPublic)!.Invoke(manager, null);
				Control summary = Assert.Single(manager.Controls.Find("modInventorySummary", true));
				Assert.Equal(LocalizationManager.Get("ModManager.Inventory.One", 1, 1), summary.Text);
				CapturePreviewIfRequested(manager, "shared-manager.png");
			}
			catch (Exception exception) { failure = exception; }
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(20)));
		Assert.Null(failure);
	}

	private ModImportResult InstallScenario() => Import(_scenarios,
		Zip(("gameoptions.yaml", "Options: []"), ("Content/test.ecf", "data")), "Scenario");
	private static void CapturePreviewIfRequested(Control control, string fileName)
	{
		string? folder = Environment.GetEnvironmentVariable("SYNIX_TEST_UI_PREVIEW_DIR");
		if (string.IsNullOrWhiteSpace(folder)) return;
		Directory.CreateDirectory(folder);
		if (control is Form form)
		{
			form.StartPosition = FormStartPosition.Manual;
			form.Location = new Point(-20000, -20000);
			form.ShowInTaskbar = false;
			form.Show();
			Application.DoEvents();
			form.Update();
		}
		using Bitmap image = new(control.Width, control.Height);
		control.DrawToBitmap(image, new Rectangle(Point.Empty, control.Size));
		image.Save(Path.Combine(folder, fileName), ImageFormat.Png);
		control.Hide();
	}
	private ModImportResult Import(ModSystemProfile profile, string package, string? name = null) =>
		ModPackageManager.Import(_server, profile, profile.Targets[0], package, securityContext: new(false), installationFolderName: name);
	private string Zip(params (string Path, string Content)[] files)
	{
		string path = Path.Combine(_root, "Package-" + Guid.NewGuid().ToString("N") + ".zip");
		using ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
		foreach ((string name, string text) in files)
		{
			using StreamWriter writer = new(zip.CreateEntry(name).Open());
			writer.Write(text);
		}
		return path;
	}

	public void Dispose()
	{
		ModPackageManager.DataRootOverride = _oldData;
		ModSystemCatalog.ExternalProfileRootOverride = _oldProfiles;
		ModPathSafety.EnsureTreeHasNoLinks(_root);
		Directory.Delete(_root, recursive: true);
	}
}
