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
using System.IO.Compression;

namespace Synix_Control_Panel.SynixEngine.ModManagement;

// One package pipeline; adapters describe game-specific layouts, not separate installers.
// All handlers are compiled into Synix. External profiles are data, never executable plugins.
internal interface IModPackageHandler
{
	ModPackageLimits Limits { get; }
	bool GroupInventoryByFolder { get; }
	string PickerTitleKey { get; }
	string PickerHelpKey { get; }
	bool AcceptsProfile(ModSystemProfile profile, ModInstallTarget target);
	string FolderMarker(ModInstallTarget target);
	bool AllowsWrappedSourceFolder { get; }
	bool IsInstalledFolder(string folder);
	IReadOnlyDictionary<string, string>? MapArchive(ZipArchive zip, ModInstallTarget target, string packageName, string? destinationName);
}

internal static class ModPackageHandlers
{
	private static readonly IModPackageHandler Default = new StandardPackageHandler();
	private static readonly IModPackageHandler EmpyrionMods = new EmpyrionPackageHandler(scenario: false);
	private static readonly IModPackageHandler EmpyrionScenarios = new EmpyrionPackageHandler(scenario: true);
	// History must remain readable even if a profile is later renamed or removed.
	internal static int MaximumHistoryEntries => Math.Max(Default.Limits.Entries,
		Math.Max(EmpyrionMods.Limits.Entries, EmpyrionScenarios.Limits.Entries));

	internal static IModPackageHandler For(ModInstallTarget target) => target.PackageLayout switch
	{
		ModPackageLayout.Default => Default,
		ModPackageLayout.EmpyrionMod => EmpyrionMods,
		ModPackageLayout.EmpyrionScenario => EmpyrionScenarios,
		_ => throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.InvalidTarget", target.Id))
	};

	private sealed class StandardPackageHandler : IModPackageHandler
	{
		public ModPackageLimits Limits => new(2048, 256L * 1024 * 1024, 512L * 1024 * 1024);
		public bool GroupInventoryByFolder => false;
		public string PickerTitleKey => "ModPackages.Picker.Title";
		public string PickerHelpKey => "ModPackages.Picker.Help";
		public bool AcceptsProfile(ModSystemProfile profile, ModInstallTarget target) => true;
		public string FolderMarker(ModInstallTarget target) => target.RequiredArchiveFileName;
		public bool AllowsWrappedSourceFolder => true;
		public bool IsInstalledFolder(string folder) => Directory.Exists(folder);
		public IReadOnlyDictionary<string, string>? MapArchive(ZipArchive zip, ModInstallTarget target, string packageName, string? destinationName) => null;
	}

	private sealed class EmpyrionPackageHandler(bool scenario) : IModPackageHandler
	{
		public ModPackageLimits Limits => scenario ? new(65536, 1024L * 1024 * 1024, 4L * 1024 * 1024 * 1024) : Default.Limits;
		public bool GroupInventoryByFolder => true;
		public string PickerTitleKey => scenario ? "EmpyrionMods.Picker.ScenarioTitle" : "EmpyrionMods.Picker.ModTitle";
		public string PickerHelpKey => scenario ? "EmpyrionMods.Picker.ScenarioHelp" : "EmpyrionMods.Picker.ModHelp";
		public bool AcceptsProfile(ModSystemProfile profile, ModInstallTarget target) =>
			profile.GameNames.All(name => name == EmpyrionAddOns.GameName) && target.Mode == ModTargetMode.FileImport &&
			target.ArchiveOnly && target.AllowArchives && target.PreserveArchiveContents &&
			target.RelativePath.Replace('\\', '/') == (scenario ? "Content/Scenarios" : "Content/Mods");
		public string FolderMarker(ModInstallTarget target) => scenario ? "gameoptions.yaml" : "*.dll";
		public bool AllowsWrappedSourceFolder => !scenario;
		public bool IsInstalledFolder(string folder) => scenario ? File.Exists(ModPathSafety.Resolve(folder, "gameoptions.yaml")) :
			Directory.EnumerateFiles(folder, "*.dll").Any();
		public IReadOnlyDictionary<string, string>? MapArchive(ZipArchive zip, ModInstallTarget target, string packageName, string? destinationName) =>
			EmpyrionAddOns.MapArchive(zip, target, packageName, destinationName);
	}
}
