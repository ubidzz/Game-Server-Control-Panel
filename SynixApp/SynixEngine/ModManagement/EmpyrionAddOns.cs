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
using System.IO.Compression;

namespace Synix_Control_Panel.SynixEngine.ModManagement;

internal sealed record EmpyrionScenarioState(string Scenario, string SaveName, bool SaveExists);

internal static class EmpyrionAddOns
{
	internal const string GameName = "Empyrion - Galactic Survival";
	internal static bool IsEmpyrion(GameServer server) => server.Game == GameName;
	internal static bool IsScenario(ModInstallTarget target) =>
		target.PackageLayout == ModPackageLayout.EmpyrionScenario;

	internal static string ValidateFolderName(string name, bool scenario = false)
	{
		if (name.Length is < 1 or > 80 || name != name.Trim() ||
			name.IndexOfAny(['/', '\\']) >= 0 || !ModPathSafety.IsSafeRelativePath(name) ||
			(scenario && name.All(char.IsAsciiDigit)))
			throw Error("FolderName");
		return name;
	}

	internal static string SuggestFolderName(string name)
	{
		string cleaned = ModPackageFiles.SuggestPackageName(name);
		if (cleaned.All(char.IsAsciiDigit)) cleaned = "Scenario-" + cleaned;
		return cleaned;
	}

	// Only documented layouts are accepted. Never copy a server archive into Content.
	internal static IReadOnlyDictionary<string, string> MapArchive(
		ZipArchive archive, ModInstallTarget target, string packageName, string? scenarioFolder = null)
	{
		string[] files = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name))
			.Select(e => e.FullName.Replace('\\', '/')).ToArray();
		if (files.Length == 0 || files.Any(file => !ModPathSafety.IsSafeRelativePath(file)))
			throw Error("Layout");
		Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
		if (IsScenario(target))
		{
			string[] markers = files.Where(file => file.Equals("gameoptions.yaml", StringComparison.OrdinalIgnoreCase) ||
				(file.Count(c => c == '/') == 1 && file.EndsWith("/gameoptions.yaml", StringComparison.OrdinalIgnoreCase))).ToArray();
			if (markers.Length != 1) throw Error("ScenarioLayout");
			string prefix = markers[0][..^"gameoptions.yaml".Length];
			if (files.Any(file => !file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ||
				!files.Any(file => new[] { "Content/", "Playfields/", "Sectors/", "Prefabs/" }
					.Any(folder => file[prefix.Length..].StartsWith(folder, StringComparison.OrdinalIgnoreCase))))
				throw Error("ScenarioLayout");
			string folderName = scenarioFolder ?? SuggestFolderName(prefix.Length == 0 ? packageName : prefix.TrimEnd('/'));
			ValidateFolderName(folderName, scenario: true);
			foreach (string file in files)
			{
				if (Path.GetExtension(file).ToLowerInvariant() is ".dll" or ".cs" or ".jar")
					throw Error("ScenarioCode");
				result.Add(file, folderName + "/" + file[prefix.Length..]);
			}
		}
		else
		{
			bool rootMod = files.Any(file => !file.Contains('/') && file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
			if (rootMod)
			{
				string folder = ValidateFolderName(SuggestFolderName(packageName));
				foreach (string file in files) result.Add(file, folder + "/" + file);
			}
			else
			{
				HashSet<string> modFolders = files.Where(file => file.Count(c => c == '/') == 1 &&
					file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
					.Select(file => file[..file.IndexOf('/')]).ToHashSet(StringComparer.OrdinalIgnoreCase);
				if (modFolders.Count == 0 || files.Any(file => !file.Contains('/') || !modFolders.Contains(file[..file.IndexOf('/')])))
					throw Error("ModLayout");
				foreach (string file in files) result.Add(file, file);
			}
		}
		return result;
	}

	internal static IReadOnlyList<string> GetScenarios(GameServer server)
	{
		string root = ModPathSafety.Resolve(server.InstallPath, "Content/Scenarios");
		if (!Directory.Exists(root)) return [];
		return Directory.EnumerateDirectories(root).Where(path =>
			(File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0 &&
			File.Exists(ModPathSafety.Resolve(path, "gameoptions.yaml")))
			.Select(Path.GetFileName).OfType<string>().Order(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	internal static EmpyrionScenarioState ReadSelection(GameServer server)
	{
		List<ConfigLine> lines = ReadConfiguration(server);
		string scenario = Required(lines, "GameConfig.CustomScenario").Value;
		string save = ValidateFolderName(Required(lines, "GameConfig.GameName").Value);
		string path = ModPathSafety.Resolve(server.InstallPath, "Saves/Games/" + save);
		return new(scenario, save, Directory.Exists(path) || File.Exists(path));
	}

	internal static string SelectScenario(GameServer server, string scenario, string saveName,
		Func<bool> persistServer, EmpyrionScenarioState? expected = null)
	{
		if (!IsEmpyrion(server)) throw Error("Profile");
		ModPackageManager.EnsureStopped(server);
		ValidateFolderName(scenario, scenario: true);
		ValidateFolderName(saveName);
		if (!GetScenarios(server).Contains(scenario, StringComparer.Ordinal)) throw Error("MissingScenario");
		EmpyrionScenarioState current = ReadSelection(server);
		if (expected != null && current != expected) throw Error("ConfigurationChanged");
		bool sameSave = saveName.Equals(current.SaveName, StringComparison.OrdinalIgnoreCase);
		bool sameScenario = scenario.Equals(current.Scenario, StringComparison.Ordinal);
		string savePath = ModPathSafety.Resolve(server.InstallPath, "Saves/Games/" + saveName);
		if ((sameSave && current.SaveExists && !sameScenario) ||
			(!sameSave && (Directory.Exists(savePath) || File.Exists(savePath))))
			throw Error("ExistingSave");

		string config = ModPathSafety.Resolve(server.InstallPath, "dedicated.yaml");
		ModPathSafety.EnsureNoLinks(config + ".synix.bak");
		byte[] before = File.ReadAllBytes(config);
		List<ConfigLine> lines = ReadConfiguration(server);
		Required(lines, "GameConfig.CustomScenario").Value = scenario;
		Required(lines, "GameConfig.GameName").Value = saveName;
		ConfigurationTextSnapshot snapshot = ConfigurationTextSnapshot.Read(config);
		byte[] after = snapshot.Encode(ConfigHandler.CreatePreview(config, lines, ConfigFormat.YAML));
		string backup = ModPathSafety.Resolve(ModPackageManager.GetServerDataFolder(server),
			"ScenarioChanges/" + Guid.NewGuid().ToString("N") + "/dedicated.yaml");
		Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
		File.WriteAllBytes(backup, before);
		string previousWorldName = server.WorldName;
		bool written = false;
		try
		{
			ModPackageManager.EnsureStopped(server);
			ModPathSafety.EnsureNoLinks(config);
			if (!File.ReadAllBytes(config).AsSpan().SequenceEqual(before)) throw Error("ConfigurationChanged");
			ConfigurationFileWriter.WriteAtomically(config, after);
			written = true;
			server.WorldName = scenario;
			if (!persistServer()) throw Error("SaveFailed");
		}
		catch
		{
			server.WorldName = previousWorldName;
			ModPathSafety.EnsureNoLinks(config);
			if (written && File.ReadAllBytes(config).AsSpan().SequenceEqual(after))
				ConfigurationFileWriter.WriteAtomically(config, before);
			throw;
		}
		return backup;
	}

	internal static bool CanRemoveScenario(GameServer server, IEnumerable<string> installedPaths)
	{
		if (!IsEmpyrion(server)) return true;
		string[] scenarioPaths = installedPaths.Select(p => p.Replace('\\', '/'))
			.Where(p => p.StartsWith("Content/Scenarios/", StringComparison.OrdinalIgnoreCase)).ToArray();
		if (scenarioPaths.Length == 0) return true;
		// Old saves can depend on scenario assets too. Do not remove those assets automatically.
		string saves = ModPathSafety.Resolve(server.InstallPath, "Saves/Games");
		if (Directory.Exists(saves) && Directory.EnumerateFileSystemEntries(saves).Any()) return false;
		string config = ModPathSafety.Resolve(server.InstallPath, "dedicated.yaml");
		if (!File.Exists(config)) return true;
		string active = ReadSelection(server).Scenario;
		return !scenarioPaths.Any(p => p.StartsWith("Content/Scenarios/" + active + "/", StringComparison.OrdinalIgnoreCase));
	}

	internal static bool CanRollBackScenarioImport(GameServer server, ModInstallationRecord record)
	{
		if (CanRemoveScenario(server, record.Files.Select(file => file.RelativePath))) return true;
		// An update can be rolled back without removing a scenario's original installation.
		// The normal removal engine also verifies every installed hash and required backup.
		var files = record.Files.Select(file => (Path: file.RelativePath.Replace('\\', '/'), File: file)).ToArray();
		string[] folders = files.Where(f => f.Path.StartsWith("Content/Scenarios/", StringComparison.OrdinalIgnoreCase))
			.Select(f => f.Path.Split('/')[2]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		return folders.Length > 0 && folders.All(folder => files.Any(f => f.File.ReplacedExistingFile &&
			f.Path.Equals("Content/Scenarios/" + folder + "/gameoptions.yaml", StringComparison.OrdinalIgnoreCase)));
	}

	private static List<ConfigLine> ReadConfiguration(GameServer server)
	{
		string config = ModPathSafety.Resolve(server.InstallPath, "dedicated.yaml");
		if (!File.Exists(config) || new FileInfo(config).Length > 4 * 1024 * 1024) throw Error("Configuration");
		return ConfigHandler.LoadConfig(config, ConfigFormat.YAML);
	}

	private static ConfigLine Required(IEnumerable<ConfigLine> lines, string path)
	{
		ConfigLine[] matches = lines.Where(line => line.Path == path).ToArray();
		if (matches.Length != 1) throw Error("Configuration");
		return matches[0];
	}

	internal static InvalidDataException Error(string key) =>
		new(LocalizationManager.Get("EmpyrionMods.Error." + key));
}
