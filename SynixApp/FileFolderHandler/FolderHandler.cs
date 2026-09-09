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
using Synix_Control_Panel.SynixEngine.ModManagement;

namespace Synix_Control_Panel.SynixApp.FileFolderHandler
{
	public sealed record ServerFolderDeletionResult(
		string InstallationPath,
		bool InstallationDeleted,
		string? BackupPath,
		bool BackupsDeleted,
		string AddOnDataPath,
		bool AddOnDataDeleted);

	public static class FolderHandler
	{
		public static void Create(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		public static class ServerFolder
		{
			public static Task<ServerFolderDeletionResult> DeleteFilesAsync(
				GameServer server,
				bool deleteBackups)
			{
				ArgumentNullException.ThrowIfNull(server);
				return Task.Run(() => DeleteFiles(server, deleteBackups));
			}

			internal static ServerFolderDeletionResult DeleteFiles(
				GameServer server,
				bool deleteBackups)
			{
				// Validate every target before deleting anything, including add-on
				// recovery files stored separately from the game's installation.
				(string installationPath, string? backupRoot, string addOnDataPath) =
					ValidateDeletionTargets(server, deleteBackups);
				bool installationDeleted = false;
				if (Directory.Exists(installationPath))
				{
					Directory.Delete(installationPath, true);
					installationDeleted = true;
				}

				bool backupsDeleted = false;
				if (backupRoot != null && Directory.Exists(backupRoot))
				{
					Directory.Delete(backupRoot, true);
					backupsDeleted = true;
				}

				// Retain add-on recovery copies until the requested game-file deletions
				// succeed. Recheck links immediately before removing this exact folder.
				ModPathSafety.EnsureTreeHasNoLinks(addOnDataPath);
				bool addOnDataDeleted = false;
				if (Directory.Exists(addOnDataPath))
				{
					Directory.Delete(addOnDataPath, recursive: true);
					addOnDataDeleted = true;
				}

				return new ServerFolderDeletionResult(
					installationPath,
					installationDeleted,
					backupRoot,
					backupsDeleted,
					addOnDataPath,
					addOnDataDeleted);
			}

			internal static (string InstallationPath, string? BackupPath, string AddOnDataPath) ValidateDeletionTargets(
				GameServer server, bool deleteBackups)
			{
				ArgumentNullException.ThrowIfNull(server);
				string installationPath = ServerDeletionSafety.ValidatePath(server.InstallPath);
				string? backupRoot = null;
				if (deleteBackups)
				{
					string cleanGame = SynixEngine.Core.Instance.GetSafeName(server.Game);
					string cleanServer = SynixEngine.Core.Instance.GetSafeName(server.ServerName);
					string baseBackupFolder = SynixEngine.Core.DefaultBackupPath;

					if (Properties.Settings.Default.UseCustomBackupPath &&
						!string.IsNullOrWhiteSpace(Properties.Settings.Default.CustomBackupPath) &&
						Directory.Exists(Properties.Settings.Default.CustomBackupPath))
					{
						baseBackupFolder = Properties.Settings.Default.CustomBackupPath;
					}

					if (string.IsNullOrWhiteSpace(server.Game) || string.IsNullOrWhiteSpace(server.ServerName) ||
						string.IsNullOrWhiteSpace(cleanGame) || string.IsNullOrWhiteSpace(cleanServer))
						throw new InvalidOperationException(LocalizationManager.Get(
							"FileSystem.Error.UnsafeDeletionPath", baseBackupFolder));
					backupRoot = ServerDeletionSafety.ValidatePath(
						Path.Combine(baseBackupFolder, cleanGame, cleanServer), baseBackupFolder);
				}
				// Use the same identity as imports, not a name wildcard or a sweep of
				// AddOns. This also protects other servers that happen to share a name.
				string addOnDataPath = ModPackageManager.GetServerDataFolder(server);
				ModPathSafety.EnsureTreeHasNoLinks(addOnDataPath);
				if (File.Exists(addOnDataPath))
					throw new IOException(LocalizationManager.Get(
						"FileSystem.Error.DeletionPathUnavailable", addOnDataPath));
				return (installationPath, backupRoot, addOnDataPath);
			}

			public static bool Rename(GameServer oldServer, GameServer newServer)
			{

				if (!oldServer.IsDefaultPath)
				{
					return false;
				}

				if (oldServer.InstallPath != newServer.InstallPath)
				{
					try
					{
						if (Directory.Exists(oldServer.InstallPath))
						{
							Directory.Move(oldServer.InstallPath, newServer.InstallPath);
							return true;
						}
					}
					catch (Exception ex)
					{
						throw new IOException(
							LocalizationManager.Get(
								"FileSystem.Error.FolderMoveFailed",
								ex.Message),
							ex);
					}
				}
				return false;
			}
		}
	}
}
