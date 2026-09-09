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
using Synix_Control_Panel.SynixEngine.ModManagement;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class UniversalModPackageTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "SynixUniversalMods-" + Guid.NewGuid().ToString("N"));
	private readonly string? _oldData = ModPackageManager.DataRootOverride;
	private readonly string? _oldProfiles = ModSystemCatalog.ExternalProfileRootOverride;

	public UniversalModPackageTests()
	{
		Directory.CreateDirectory(_root);
		ModPackageManager.DataRootOverride = Path.Combine(_root, "history");
		ModSystemCatalog.ExternalProfileRootOverride = Path.Combine(_root, "profiles");
		Directory.CreateDirectory(ModSystemCatalog.ExternalProfileRootOverride);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SevenDaysUsesSharedFolderPreparationImportAndRollback(bool wrapped)
	{
		GameServer server = new() { Game = "7 Days to Die", ServerName = "Package test", Status = "Stopped", InstallPath = Path.Combine(_root, "server") };
		Directory.CreateDirectory(server.InstallPath);
		File.WriteAllText(Path.Combine(server.InstallPath, "7DaysToDieServer.exe"), "fixture only");
		ModSystemProfile profile = ModSystemCatalog.GetProfiles(server).Single(p => p.Id == "seven-days-to-die-mods");
		ModInstallTarget target = profile.Targets.Single();
		Assert.True(target.AllowFolderImport);
		Assert.Equal(ModPackageLayout.Default, target.PackageLayout);
		string source = Path.Combine(_root, "Example Mod");
		string contents = wrapped ? Path.Combine(source, "Wrapped Mod") : source;
		Directory.CreateDirectory(Path.Combine(contents, "Resources"));
		File.WriteAllText(Path.Combine(contents, "ModInfo.xml"), "<xml><Name value=\"Example\" /></xml>");
		File.WriteAllText(Path.Combine(contents, "Resources/data.bundle"), "complete asset");
		string preparedPath;
		using (PreparedModPackage prepared = PreparedModPackage.FromFolder(source, target))
		{
			preparedPath = prepared.Path;
			_ = ModSecurityScanner.InspectPackageStructure(prepared.Path, target);
			ModImportResult import = ModPackageManager.Import(server, profile, target, prepared.Path, securityContext: new(false));
			Assert.Equal(2, import.InstalledFileCount);
			string destination = Path.Combine(server.InstallPath, "Mods", wrapped ? "Wrapped Mod" : "Example Mod");
			Assert.Equal("complete asset", File.ReadAllText(Path.Combine(destination, "Resources/data.bundle")));
			ModPackageManager.Remove(server, import.InstallationId);
			Assert.False(File.Exists(Path.Combine(destination, "ModInfo.xml")));
		}
		Assert.False(File.Exists(preparedPath));
		Assert.Equal("complete asset", File.ReadAllText(Path.Combine(contents, "Resources/data.bundle")));
	}

	[Fact]
	public void FolderImportRequiresExplicitProfileSupportAndItsPackageMarker()
	{
		string folder = Path.Combine(_root, "not-a-package");
		Directory.CreateDirectory(folder);
		ModSystemProfile minecraft = ModSystemCatalog.GetProfiles("Minecraft").Single(p => p.Id == "minecraft-addons");
		Assert.Throws<InvalidDataException>(() => PreparedModPackage.FromFolder(folder, minecraft.Targets[0]));
		ModSystemProfile sevenDays = ModSystemCatalog.GetProfiles("7 Days to Die").Single(p => p.Id == "seven-days-to-die-mods");
		Assert.Throws<InvalidDataException>(() => PreparedModPackage.FromFolder(folder, sevenDays.Targets[0]));
	}

	[Theory]
	[InlineData(2, 2, false)]
	[InlineData(3, 2, true)]
	[InlineData(1, 2, true)]
	public void SharedExtractionChecksActualLengthNotOnlyArchiveMetadata(int actual, int declared, bool fails)
	{
		using MemoryStream input = new(new byte[actual]);
		using MemoryStream output = new();
		if (fails) Assert.Throws<InvalidDataException>(() => ModPackageFiles.CopyExactBounded(input, output, declared));
		else ModPackageFiles.CopyExactBounded(input, output, declared);
		Assert.True(output.Length <= declared);
	}

	[Fact]
	public void GameSpecificLayoutsCannotBeAssignedToAnUnrelatedGameOrFolder()
	{
		ModSystemProfile scenarios = ModSystemCatalog.GetProfiles(EmpyrionAddOns.GameName).Single(p => p.Id == "empyrion-scenarios");
		ModInstallTarget target = scenarios.Targets.Single();
		IModPackageHandler handler = ModPackageHandlers.For(target);
		Assert.True(handler.AcceptsProfile(scenarios, target));
		Assert.False(handler.AcceptsProfile(new() { GameNames = ["Other game"] }, target));
		Assert.False(handler.AcceptsProfile(scenarios, new() { RelativePath = "Saves/Games", PackageLayout = ModPackageLayout.EmpyrionScenario }));
	}

	[Fact]
	public void LongFolderNameSuggestionsStayValidAfterTruncation()
	{
		string name = new string('a', 59) + " remaining words";
		Assert.True(ModPathSafety.IsSafeRelativePath(ModPackageFiles.SuggestPackageName(name)));
		Assert.Equal(new string('a', 59), EmpyrionAddOns.ValidateFolderName(EmpyrionAddOns.SuggestFolderName(name), scenario: true));
		Assert.Equal("Scenario-12345", EmpyrionAddOns.SuggestFolderName("12345"));
	}

	public void Dispose()
	{
		ModPackageManager.DataRootOverride = _oldData;
		ModSystemCatalog.ExternalProfileRootOverride = _oldProfiles;
		ModPathSafety.EnsureTreeHasNoLinks(_root);
		Directory.Delete(_root, recursive: true);
	}
}
