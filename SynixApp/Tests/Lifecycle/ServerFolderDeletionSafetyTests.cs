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
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using Synix_Control_Panel.SynixEngine.ModManagement;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using Xunit;
using AppSettings = Synix_Control_Panel.Properties.Settings;

namespace Synix_Control_Panel.Tests;

public sealed class ServerFolderDeletionSafetyTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("relative-server")]
	[InlineData(@"C:relative-server")]
	[InlineData(@"\relative-server")]
	[InlineData(@"C:\")]
	[InlineData(@"G:\")]
	[InlineData(@"\\example.invalid\share\")]
	[InlineData(@"\\?\C:\server")]
	[InlineData(@"\\.\C:\server")]
	[InlineData(@"C:\server\..\other")]
	[InlineData(@"C:\server\.\other")]
	[InlineData(@"C:\server..\other")]
	[InlineData(@"C:\server \other")]
	[InlineData(@"C:\server:stream")]
	[InlineData(@"C:\server\*")]
	public void InvalidOrBroadPaths_AreRejectedWithoutDeletingAnything(string? path)
	{
		// Validation only: never hand actual protected locations to recursive deletion,
		// even in a test expecting rejection.
		Assert.Throws<InvalidOperationException>(() => ServerDeletionSafety.ValidatePath(path!));
	}

	[Fact]
	public void UserAndSharedRoots_AreProtectedIncludingCaseAndTrailingSeparators()
	{
		List<string> roots =
		[
			Core.RootPath, Core.GamesPath, Core.DefaultBackupPath, Path.GetTempPath(),
			Path.Combine(Core.GamesPath, "Example Game"),
			Path.Combine(Core.DefaultBackupPath, "Example Game")
		];
		foreach (Environment.SpecialFolder folder in new[]
		{
			Environment.SpecialFolder.UserProfile, Environment.SpecialFolder.DesktopDirectory,
			Environment.SpecialFolder.MyDocuments, Environment.SpecialFolder.MyMusic,
			Environment.SpecialFolder.MyPictures, Environment.SpecialFolder.MyVideos,
			Environment.SpecialFolder.ApplicationData, Environment.SpecialFolder.LocalApplicationData,
			Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolder.CommonDocuments,
			Environment.SpecialFolder.CommonDesktopDirectory
		})
		{
			string path = Environment.GetFolderPath(folder);
			if (!string.IsNullOrWhiteSpace(path))
				roots.Add(path);
		}
		string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrWhiteSpace(profile))
		{
			roots.Add(Path.Combine(profile, "Downloads"));
			roots.Add(Path.GetDirectoryName(profile)!);
			roots.Add(Path.Combine(Path.GetDirectoryName(profile)!, "AnotherProfile"));
		}

		foreach (string path in roots.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			Assert.Throws<InvalidOperationException>(() => ServerDeletionSafety.ValidatePath(path));
			Assert.Throws<InvalidOperationException>(() =>
				ServerDeletionSafety.ValidatePath(path.ToUpperInvariant().TrimEnd('\\') + "\\"));
			Assert.Throws<InvalidOperationException>(() =>
				ServerDeletionSafety.ValidatePath(path.TrimEnd('\\', '/') + ".\\"));
		}
	}

	[Fact]
	public void WindowsApplicationAndSynixDataTrees_AreProtected()
	{
		List<string> trees = [Core.DataPath, Core.SteamCmdPath, AppContext.BaseDirectory];
		foreach (Environment.SpecialFolder folder in new[]
		{
			Environment.SpecialFolder.Windows, Environment.SpecialFolder.System,
			Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86
		})
		{
			string path = Environment.GetFolderPath(folder);
			if (!string.IsNullOrWhiteSpace(path))
				trees.Add(path);
		}
		foreach (string path in trees)
		{
			Assert.Throws<InvalidOperationException>(() => ServerDeletionSafety.ValidatePath(path));
			Assert.Throws<InvalidOperationException>(() =>
				ServerDeletionSafety.ValidatePath(Path.Combine(path, "DeletionValidationOnly")));
		}
	}

	[Fact]
	public void EveryBuiltInGame_AllowsADedicatedDefaultServerAndBackupFolder()
	{
		IReadOnlyList<GameInfo> games = GameDatabase.GetGameList();
		Assert.NotEmpty(games);
		foreach (GameInfo game in games)
		{
			string name = Core.Instance.GetSafeName(game.Game);
			foreach (string root in new[] { Core.GamesPath, Core.DefaultBackupPath })
			{
				string path = Path.Combine(root, name, "DeletionValidationOnly");
				Assert.Equal(Path.GetFullPath(path), ServerDeletionSafety.ValidatePath(path), ignoreCase: true);
			}
		}
	}

	[Fact]
	public void DedicatedCustomFolders_AreAllowedAndReturnedAsAbsolutePaths()
	{
		using DeletionFixture fixture = new();
		string path = fixture.CreateServer().InstallPath;
		Assert.Equal(path, ServerDeletionSafety.ValidatePath(path + "\\"), ignoreCase: true);
		Assert.Equal(path, ServerDeletionSafety.ValidatePath(path + "."), ignoreCase: true);
		string missing = Path.Combine(fixture.Root, "not-created-server");
		Assert.Equal(missing, ServerDeletionSafety.ValidatePath(missing), ignoreCase: true);
	}

	[Fact]
	public void CustomBackupRootAndGameContainer_AreProtected()
	{
		using DeletionFixture fixture = new();
		Assert.Throws<InvalidOperationException>(() =>
			ServerDeletionSafety.ValidatePath(fixture.BackupRoot, fixture.BackupRoot));
		Assert.Throws<InvalidOperationException>(() =>
			ServerDeletionSafety.ValidatePath(Path.Combine(fixture.BackupRoot, "Game"), fixture.BackupRoot));
		string dedicated = Path.Combine(fixture.BackupRoot, "Game", "Server");
		Assert.Equal(dedicated, ServerDeletionSafety.ValidatePath(dedicated, fixture.BackupRoot), ignoreCase: true);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Deletion_RemovesOnlyTheSelectedFixtureAndRequestedBackups(bool deleteBackups)
	{
		using DeletionFixture fixture = new();
		GameServer selected = fixture.CreateServer("Selected");
		GameServer sibling = fixture.CreateServer("Sibling");
		string selectedBackup = fixture.CreateBackup(selected);
		string siblingBackup = fixture.CreateBackup(sibling);
		string selectedAddOns = fixture.CreateAddOnData(selected);
		string siblingAddOns = fixture.CreateAddOnData(sibling);

		ServerFolderDeletionResult result =
			await FolderHandler.ServerFolder.DeleteFilesAsync(selected, deleteBackups);

		Assert.True(result.InstallationDeleted);
		Assert.False(Directory.Exists(selected.InstallPath));
		Assert.True(File.Exists(Path.Combine(sibling.InstallPath, "world", "marker.txt")));
		Assert.Equal(deleteBackups, result.BackupsDeleted);
		Assert.Equal(!deleteBackups, Directory.Exists(selectedBackup));
		Assert.True(File.Exists(Path.Combine(siblingBackup, "marker.txt")));
		Assert.True(Directory.Exists(fixture.BackupRoot));
		Assert.True(result.AddOnDataDeleted);
		Assert.Equal(selectedAddOns, result.AddOnDataPath, ignoreCase: true);
		Assert.False(Directory.Exists(selectedAddOns));
		Assert.True(File.Exists(Path.Combine(siblingAddOns, "installed.json")));
		Assert.True(Directory.Exists(fixture.AddOnRoot));
		if (deleteBackups)
			Assert.Equal(selectedBackup, result.BackupPath, ignoreCase: true);
		else
			Assert.Null(result.BackupPath);
	}

	[Theory]
	[InlineData(true, "")]
	[InlineData(false, "")]
	[InlineData(true, " ")]
	[InlineData(false, " ")]
	[InlineData(true, "///")]
	[InlineData(false, "///")]
	public void InvalidBackupIdentity_IsRejectedBeforeAnyFixtureFilesAreDeleted(bool changeGame, string value)
	{
		using DeletionFixture fixture = new();
		GameServer server = fixture.CreateServer();
		string backup = fixture.CreateBackup(server);
		if (changeGame)
			server.Game = value;
		else
			server.ServerName = value;

		Assert.Throws<InvalidOperationException>(() =>
			FolderHandler.ServerFolder.DeleteFiles(server, deleteBackups: true));
		Assert.True(File.Exists(Path.Combine(server.InstallPath, "world", "marker.txt")));
		Assert.True(File.Exists(Path.Combine(backup, "marker.txt")));
	}

	[Fact]
	public void MissingFixtureFolders_AreReportedWithoutDeletingSiblings()
	{
		using DeletionFixture fixture = new();
		GameServer sibling = fixture.CreateServer("Sibling");
		GameServer missing = new()
		{
			Game = "Deletion Test", ServerName = "Missing",
			InstallPath = Path.Combine(fixture.Root, "Servers", "Missing")
		};
		ServerFolderDeletionResult result = FolderHandler.ServerFolder.DeleteFiles(missing, deleteBackups: true);
		Assert.False(result.InstallationDeleted);
		Assert.False(result.BackupsDeleted);
		Assert.False(result.AddOnDataDeleted);
		Assert.False(Directory.Exists(fixture.AddOnRoot));
		Assert.True(Directory.Exists(sibling.InstallPath));
	}

	[Theory]
	[InlineData("Rust")]
	[InlineData("Minecraft")]
	[InlineData("7 Days to Die")]
	[InlineData("Empyrion - Galactic Survival")]
	[InlineData("Formerly supported game")]
	public void Deletion_ClearsAddOnHistoryEvenWhenGameFilesAreAlreadyMissing(string game)
	{
		using DeletionFixture fixture = new();
		GameServer server = new()
		{
			Game = game, ServerName = "Missing installation",
			InstallPath = Path.Combine(fixture.Root, "Servers", "Missing")
		};
		string history = fixture.CreateAddOnData(server);
		ServerFolderDeletionResult result = FolderHandler.ServerFolder.DeleteFiles(server, deleteBackups: false);
		Assert.False(result.InstallationDeleted);
		Assert.True(result.AddOnDataDeleted);
		Assert.False(Directory.Exists(history));
		Assert.True(Directory.Exists(fixture.AddOnRoot));
	}

	[Fact]
	public void Deletion_PreservesAddOnsOfAnotherServerWithTheSameName()
	{
		using DeletionFixture fixture = new();
		GameServer selected = fixture.CreateServer("Selected");
		GameServer sibling = fixture.CreateServer("Sibling");
		sibling.ServerName = selected.ServerName;
		string selectedHistory = fixture.CreateAddOnData(selected);
		string siblingHistory = fixture.CreateAddOnData(sibling);
		Assert.NotEqual(selectedHistory, siblingHistory);

		FolderHandler.ServerFolder.DeleteFiles(selected, deleteBackups: false);

		Assert.False(Directory.Exists(selectedHistory));
		Assert.True(File.Exists(Path.Combine(siblingHistory, "installed.json")));
		Assert.True(Directory.Exists(sibling.InstallPath));
	}

	[Fact]
	public void InvalidAddOnDataFolder_IsRejectedBeforeGameOrBackupDeletion()
	{
		using DeletionFixture fixture = new();
		GameServer server = fixture.CreateServer();
		string backup = fixture.CreateBackup(server);
		string history = ModPackageManager.GetServerDataFolder(server);
		Directory.CreateDirectory(fixture.AddOnRoot);
		File.WriteAllText(history, "A file is not a server history directory.");

		Assert.Throws<IOException>(() => FolderHandler.ServerFolder.DeleteFiles(server, deleteBackups: true));

		Assert.True(File.Exists(Path.Combine(server.InstallPath, "world", "marker.txt")));
		Assert.True(File.Exists(Path.Combine(backup, "marker.txt")));
		Assert.True(File.Exists(history));
	}

	[Theory]
	[InlineData("root")]
	[InlineData("server")]
	[InlineData("nested")]
	public void LinkedAddOnData_IsRejectedBeforeAnyDeletion(string linkPosition)
	{
		using DeletionFixture fixture = new();
		GameServer server = fixture.CreateServer();
		string backup = fixture.CreateBackup(server);
		string history = ModPackageManager.GetServerDataFolder(server);
		string link = linkPosition switch
		{
			"root" => fixture.AddOnRoot,
			"server" => history,
			_ => Path.Combine(history, "Transactions", "linked-recovery")
		};
		string target = Path.Combine(fixture.Root, "UnrelatedData");
		Directory.CreateDirectory(target);
		File.WriteAllText(Path.Combine(target, "keep.txt"), "Must never be deleted.");
		fixture.CreateJunction(link, target);

		Assert.Throws<InvalidDataException>(() =>
			FolderHandler.ServerFolder.ValidateDeletionTargets(server, deleteBackups: true));
		Assert.Throws<InvalidDataException>(() =>
			FolderHandler.ServerFolder.DeleteFiles(server, deleteBackups: true));

		Assert.True(File.Exists(Path.Combine(server.InstallPath, "world", "marker.txt")));
		Assert.True(File.Exists(Path.Combine(backup, "marker.txt")));
		Assert.True(File.Exists(Path.Combine(target, "keep.txt")));
	}

	[Fact]
	public void InstallationDeletionFailure_PreservesAddOnRecoveryCopies()
	{
		using DeletionFixture fixture = new();
		GameServer server = fixture.CreateServer();
		string history = fixture.CreateAddOnData(server);
		using FileStream locked = new(Path.Combine(server.InstallPath, "world", "marker.txt"),
			FileMode.Open, FileAccess.Read, FileShare.Read);

		Assert.Throws<IOException>(() => FolderHandler.ServerFolder.DeleteFiles(server, deleteBackups: false));

		Assert.True(File.Exists(Path.Combine(history, "installed.json")));
		Assert.True(Directory.Exists(Path.Combine(history, "Transactions")));
		Assert.True(Directory.Exists(Path.Combine(history, "ScenarioChanges")));
	}

	[Fact]
	public void AddOnCleanupFailure_IsReportedAndCanBeRetriedWithoutTouchingOtherHistory()
	{
		using DeletionFixture fixture = new();
		GameServer selected = fixture.CreateServer();
		GameServer sibling = fixture.CreateServer("Sibling");
		string selectedHistory = fixture.CreateAddOnData(selected);
		string siblingHistory = fixture.CreateAddOnData(sibling);
		using (FileStream locked = new(Path.Combine(selectedHistory, "installed.json"),
			FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			Assert.Throws<IOException>(() => FolderHandler.ServerFolder.DeleteFiles(selected, deleteBackups: false));
			Assert.True(File.Exists(Path.Combine(siblingHistory, "installed.json")));
		}

		ServerFolderDeletionResult result = FolderHandler.ServerFolder.DeleteFiles(selected, deleteBackups: false);
		Assert.True(result.AddOnDataDeleted);
		Assert.False(Directory.Exists(selectedHistory));
		Assert.True(File.Exists(Path.Combine(siblingHistory, "installed.json")));
	}

	[Theory]
	[InlineData("")]
	[InlineData("fr")]
	[InlineData("de")]
	[InlineData("es")]
	public void DeletionMessages_ArePresentInEachLanguageWithoutFallback(string language)
	{
		ResourceManager manager = new("Synix_Control_Panel.Localization.Strings", typeof(LocalizationManager).Assembly);
		CultureInfo culture = CultureInfo.GetCultureInfo(language);
		using ResourceSet resources = Assert.IsAssignableFrom<ResourceSet>(
			manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false));
		foreach (string key in new[]
		{
			"FileSystem.Error.UnsafeDeletionPath", "FileSystem.Error.LinkedDeletionPath",
			"FileSystem.Error.DeletionPathUnavailable", "ServerActions.Delete.ConfirmBody",
			"ServerActions.Activity.AddOnDataDeleted"
		})
		{
			string template = Assert.IsType<string>(resources.GetString(key));
			Assert.Contains("{0}", template);
			Assert.Contains(@"C:\Example server", string.Format(culture, template, @"C:\Example server"));
		}
	}

	private sealed class DeletionFixture : IDisposable
	{
		private readonly bool _previousUseCustomBackup = AppSettings.Default.UseCustomBackupPath;
		private readonly string _previousBackupPath = AppSettings.Default.CustomBackupPath;
		private readonly string? _previousAddOnRoot = ModPackageManager.DataRootOverride;
		private readonly List<string> _junctions = [];
		private readonly string _fixtureBase = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SynixDeletionSafetyTests"));
		internal string Root { get; }
		internal string BackupRoot => Path.Combine(Root, "Backups");
		internal string AddOnRoot => Path.Combine(Root, "AddOns");

		internal DeletionFixture()
		{
			Root = Path.Combine(_fixtureBase, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(BackupRoot);
			// In-memory settings only, restored on disposal. Never use real backups in deletion tests.
			AppSettings.Default.UseCustomBackupPath = true;
			AppSettings.Default.CustomBackupPath = BackupRoot;
			ModPackageManager.DataRootOverride = AddOnRoot;
		}

		internal GameServer CreateServer(string name = "Selected")
		{
			string installPath = Path.Combine(Root, "Servers", name);
			Directory.CreateDirectory(Path.Combine(installPath, "world"));
			File.WriteAllText(Path.Combine(installPath, "world", "marker.txt"), "Disposable regression-test data.");
			return new GameServer { Game = "Deletion Test", ServerName = name, InstallPath = installPath };
		}

		internal string CreateBackup(GameServer server)
		{
			string path = Path.Combine(BackupRoot, Core.Instance.GetSafeName(server.Game), Core.Instance.GetSafeName(server.ServerName));
			Directory.CreateDirectory(path);
			File.WriteAllText(Path.Combine(path, "marker.txt"), "Disposable backup regression-test data.");
			return path;
		}

		internal string CreateAddOnData(GameServer server)
		{
			string root = ModPackageManager.GetServerDataFolder(server);
			string transaction = Path.Combine("Transactions", Guid.NewGuid().ToString("N"));
			foreach (string relative in new[]
			{
				"installed.json", Path.Combine(transaction, "Incoming", "package.cs"),
				Path.Combine(transaction, "Staging", "TestPlugin.cs"),
				Path.Combine(transaction, "PreviousFiles", "TestPlugin.cs"),
				Path.Combine(transaction, "RemovalRollback", "TestPlugin.cs"),
				Path.Combine("ScenarioChanges", Guid.NewGuid().ToString("N"), "dedicated.yaml")
			})
			{
				string path = Path.Combine(root, relative);
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				File.WriteAllText(path, "Disposable add-on recovery fixture; never executed.");
			}
			return root;
		}

		internal void CreateJunction(string link, string target)
		{
			AssertInsideRoot(link);
			AssertInsideRoot(target);
			Directory.CreateDirectory(Path.GetDirectoryName(link)!);
			ProcessStartInfo info = new(Path.Combine(Environment.SystemDirectory, "cmd.exe"))
			{
				UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
				RedirectStandardOutput = true, RedirectStandardError = true
			};
			foreach (string argument in new[] { "/d", "/c", "mklink", "/J", link, target })
				info.ArgumentList.Add(argument);
			using Process process = Process.Start(info)!;
			if (!process.WaitForExit(10_000))
			{
				process.Kill();
				throw new TimeoutException("Fixture junction creation timed out.");
			}
			Assert.True(process.ExitCode == 0, process.StandardError.ReadToEnd());
			_junctions.Add(link);
		}

		private void AssertInsideRoot(string path) => Assert.StartsWith(
			Path.GetFullPath(Root) + Path.DirectorySeparatorChar,
			Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);

		public void Dispose()
		{
			AppSettings.Default.UseCustomBackupPath = _previousUseCustomBackup;
			AppSettings.Default.CustomBackupPath = _previousBackupPath;
			ModPackageManager.DataRootOverride = _previousAddOnRoot;
			string resolvedRoot = Path.GetFullPath(Root);
			if (!string.Equals(Path.GetDirectoryName(resolvedRoot), _fixtureBase, StringComparison.OrdinalIgnoreCase) ||
				!Guid.TryParseExact(Path.GetFileName(resolvedRoot), "N", out _))
				throw new InvalidOperationException("Refusing to clean up an unexpected test directory.");
			foreach (string link in _junctions.AsEnumerable().Reverse())
			{
				AssertInsideRoot(link);
				if (Directory.Exists(link))
					Directory.Delete(link); // Remove only the fixture link, never its target.
			}
			if (Directory.Exists(resolvedRoot))
				Directory.Delete(resolvedRoot, recursive: true);
		}
	}
}
