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
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json.Nodes;
using Synix_Control_Panel.SynixEngine;
using Synix_Control_Panel.SynixEngine.ModManagement;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class ModPathSafetyTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("../outside.cs")]
	[InlineData("nested/../../outside.cs")]
	[InlineData(@"C:\outside.cs")]
	[InlineData("C:outside.cs")]
	[InlineData("/outside.cs")]
	[InlineData("file.cs:stream")]
	[InlineData("folder./file.cs")]
	[InlineData("folder /file.cs")]
	[InlineData("folder//file.cs")]
	[InlineData("NUL.cs")]
	[InlineData("folder/COM1.txt")]
	[InlineData("folder/CONOUT$")]
	public void RelativePathsRejectWindowsAliasesAndEscapes(string? path)
	{
		Assert.False(ModPathSafety.IsSafeRelativePath(path));
	}

	[Theory]
	[InlineData("Welcome.cs")]
	[InlineData("My Mod/config file.json")]
	[InlineData(@"mods\version.1\Example.dll")]
	[InlineData("Données/配置.json")]
	public void NormalNestedPathsRemainSupported(string path)
	{
		Assert.True(ModPathSafety.IsSafeRelativePath(path));
	}

	[Fact]
	public void ArchivePreflightRejectsNestedJunctionBeforeChangingAnyInstalledFile()
	{
		using Fixture fixture = new();
		fixture.CreateJunction("server/plugins/linked", "outside");
		string sentinel = fixture.PathInRoot("outside/Welcome.cs");
		File.WriteAllText(sentinel, "outside unchanged");
		string archivePath = fixture.PathInRoot("input/test.zip");
		using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
		{
			using (StreamWriter first = new(archive.CreateEntry("First.cs").Open())) first.Write("// first file");
			using (StreamWriter second = new(archive.CreateEntry("linked/Welcome.cs").Open())) second.Write("// second file");
		}
		Assert.Throws<InvalidDataException>(() => fixture.Import(archivePath));
		Assert.False(File.Exists(fixture.PathInRoot("server/plugins/First.cs")));
		Assert.Equal("outside unchanged", File.ReadAllText(sentinel));
	}

	[Theory]
	[InlineData("server")]
	[InlineData("server/plugins")]
	[InlineData("data")]
	public void ImportRejectsLinkedRootsAndTargetFolders(string linkedPath)
	{
		using Fixture fixture = new();
		string original = fixture.PathInRoot(linkedPath);
		if (Directory.Exists(original)) Directory.Delete(original, true);
		fixture.CreateJunction(linkedPath, "outside");
		Assert.Throws<InvalidDataException>(() => fixture.Import());
		Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.PathInRoot("outside")));
	}

	[Theory]
	[InlineData("backupRelativePath", "../outside.cs")]
	[InlineData("backupRelativePath", "C:\\outside.cs")]
	[InlineData("backupRelativePath", "Welcome.cs:stream")]
	[InlineData("relativePath", "../outside.cs")]
	[InlineData("relativePath", "C:\\outside.cs")]
	[InlineData("transactionFolder", "../outside")]
	[InlineData("transactionFolder", "Transactions/00000000000000000000000000000000")]
	public void ChangedHistoryCannotRedirectRemovalOrRollback(string field, string value)
	{
		using Fixture fixture = new();
		ModImportResult imported = fixture.Import();
		string historyPath = Path.Combine(ModPackageManager.GetServerDataFolder(fixture.Server), "installed.json");
		JsonNode history = JsonNode.Parse(File.ReadAllText(historyPath))!;
		JsonNode record = history["installations"]![0]!;
		if (field == "transactionFolder") record[field] = value;
		else record["files"]![0]![field] = value;
		string changedHistory = history.ToJsonString();
		File.WriteAllText(historyPath, changedHistory);
		Assert.Throws<InvalidDataException>(() => ModPackageManager.Remove(fixture.Server, imported.InstallationId));
		Assert.Equal("// harmless imported file", File.ReadAllText(fixture.PathInRoot("server/plugins/Welcome.cs")));
		Assert.Equal(changedHistory, File.ReadAllText(historyPath));
		Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.PathInRoot("outside")));
	}

	[Theory]
	[InlineData("PreviousFiles")]
	[InlineData("RemovalRollback")]
	public void RemovalRejectsLinkedBackupFoldersBeforeReplacingInstalledFiles(string folder)
	{
		using Fixture fixture = new();
		File.WriteAllText(fixture.PathInRoot("server/plugins/Welcome.cs"), "// previous version");
		ModImportResult imported = fixture.Import();
		string transactionRoot = Path.GetDirectoryName(imported.BackupFolder)!;
		string linkPath = Path.Combine(transactionRoot, folder);
		fixture.CheckInsideRoot(linkPath);
		fixture.CheckInsideRoot(linkPath + ".saved");
		if (Directory.Exists(linkPath)) Directory.Move(linkPath, linkPath + ".saved");
		fixture.CreateJunction(Path.GetRelativePath(fixture.Root, linkPath), "outside");
		Assert.Throws<InvalidDataException>(() => ModPackageManager.Remove(fixture.Server, imported.InstallationId));
		Assert.Equal("// harmless imported file", File.ReadAllText(fixture.PathInRoot("server/plugins/Welcome.cs")));
		Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.PathInRoot("outside")));
	}

	[Fact]
	public void MissingPreviousFileBlocksRemovalWithoutDeletingTheInstalledMod()
	{
		using Fixture fixture = new();
		string installed = fixture.PathInRoot("server/plugins/Welcome.cs");
		File.WriteAllText(installed, "// previous version");
		ModImportResult imported = fixture.Import();
		string previous = Path.Combine(imported.BackupFolder, "Welcome.cs");
		fixture.CheckInsideRoot(previous);
		File.Delete(previous);
		Assert.Throws<InvalidDataException>(() => ModPackageManager.Remove(fixture.Server, imported.InstallationId));
		Assert.Equal("// harmless imported file", File.ReadAllText(installed));
		Assert.Contains(imported.InstallationId, File.ReadAllText(Path.Combine(ModPackageManager.GetServerDataFolder(fixture.Server), "installed.json")));
	}

	[Fact]
	public void ImportAndRemovalReplaceHardLinksWithoutWritingThroughThem()
	{
		using Fixture fixture = new();
		string sentinel = fixture.PathInRoot("outside/Welcome.cs");
		File.WriteAllText(sentinel, "// previous version");
		fixture.CreateHardLink("server/plugins/Welcome.cs", "outside/Welcome.cs");
		ModImportResult imported = fixture.Import();
		Assert.Equal("// previous version", File.ReadAllText(sentinel));
		Assert.Equal("// harmless imported file", File.ReadAllText(fixture.PathInRoot("server/plugins/Welcome.cs")));
		ModPackageManager.Remove(fixture.Server, imported.InstallationId);
		Assert.Equal("// previous version", File.ReadAllText(sentinel));
		Assert.Equal("// previous version", File.ReadAllText(fixture.PathInRoot("server/plugins/Welcome.cs")));
	}

	[Fact]
	public void FailedImportRestoresAnEarlierFileInTheSamePackage()
	{
		using Fixture fixture = new();
		string installed = fixture.PathInRoot("server/plugins/Welcome.cs");
		File.WriteAllText(installed, "// previous version");
		Directory.CreateDirectory(fixture.PathInRoot("server/plugins/Blocked.cs"));
		string archivePath = fixture.PathInRoot("input/test.zip");
		using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
		{
			using (StreamWriter first = new(archive.CreateEntry("Welcome.cs").Open())) first.Write("// new version");
			using (StreamWriter second = new(archive.CreateEntry("Blocked.cs").Open())) second.Write("// blocked destination");
		}
		Exception? error = Record.Exception(() => fixture.Import(archivePath));
		Assert.True(error is IOException or UnauthorizedAccessException, error?.ToString() ?? "Import should have failed.");
		Assert.Equal("// previous version", File.ReadAllText(installed));
		Assert.False(File.Exists(Path.Combine(ModPackageManager.GetServerDataFolder(fixture.Server), "installed.json")));
	}

	private sealed class Fixture : IDisposable
	{
		private readonly string? _previousDataRoot = ModPackageManager.DataRootOverride;
		private readonly List<string> _junctions = [];
		internal string Root { get; } = Path.Combine(Path.GetTempPath(), "SynixModBoundaryTests-" + Guid.NewGuid().ToString("N"));
		internal GameServer Server { get; }
		private readonly ModInstallTarget _target = new()
		{
			Id = "plugins", DisplayName = "Plugins", Kind = ModContentKind.Plugin,
			Mode = ModTargetMode.FileImport, RelativePath = "plugins", AllowedExtensions = [".cs"], AllowArchives = true
		};

		internal Fixture()
		{
			foreach (string directory in new[] { "server/plugins", "data", "input", "outside" })
				Directory.CreateDirectory(PathInRoot(directory));
			File.WriteAllText(PathInRoot("input/Welcome.cs"), "// harmless imported file");
			Server = new() { Game = "Boundary Fixture", ServerName = "Test", InstallPath = PathInRoot("server"), Status = "Stopped" };
			ModPackageManager.DataRootOverride = PathInRoot("data");
		}

		internal ModImportResult Import(string? packagePath = null) => ModPackageManager.Import(Server,
			new ModSystemProfile { Id = "fixture", DisplayName = "Fixture", SupportLevel = ModSystemSupportLevel.Managed, Targets = [_target] },
			_target, packagePath ?? PathInRoot("input/Welcome.cs"), securityContext: new(false));

		internal string PathInRoot(string relative)
		{
			string path = Path.GetFullPath(Path.Combine(Root, relative));
			CheckInsideRoot(path);
			return path;
		}

		internal void CheckInsideRoot(string path) => Assert.StartsWith(Path.GetFullPath(Root) + Path.DirectorySeparatorChar,
			Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);

		internal void CreateJunction(string relativeLink, string relativeTarget)
		{
			CreateLink(relativeLink, relativeTarget, directory: true);
		}

		internal void CreateHardLink(string relativeLink, string relativeTarget)
		{
			CreateLink(relativeLink, relativeTarget, directory: false);
		}

		private void CreateLink(string relativeLink, string relativeTarget, bool directory)
		{
			string link = PathInRoot(relativeLink);
			string target = PathInRoot(relativeTarget);
			Directory.CreateDirectory(Path.GetDirectoryName(link)!);
			if (directory) Directory.CreateDirectory(target);
			ProcessStartInfo info = new(Path.Combine(Environment.SystemDirectory, "cmd.exe"))
			{
				UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
				RedirectStandardOutput = true, RedirectStandardError = true
			};
			foreach (string argument in new[] { "/d", "/c", "mklink", directory ? "/J" : "/H", link, target }) info.ArgumentList.Add(argument);
			using Process process = Process.Start(info)!;
			if (!process.WaitForExit(10_000)) { process.Kill(); throw new TimeoutException("Fixture junction creation timed out."); }
			Assert.True(process.ExitCode == 0, process.StandardError.ReadToEnd());
			if (directory) _junctions.Add(link);
		}

		public void Dispose()
		{
			ModPackageManager.DataRootOverride = _previousDataRoot;
			foreach (string link in _junctions.AsEnumerable().Reverse())
			{
				CheckInsideRoot(link);
				if (Directory.Exists(link)) Directory.Delete(link); // Remove only the link, never its target.
			}
			string resolved = Path.GetFullPath(Root);
			Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())), Path.GetDirectoryName(resolved));
			Assert.StartsWith("SynixModBoundaryTests-", Path.GetFileName(resolved));
			if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
		}
	}
}
