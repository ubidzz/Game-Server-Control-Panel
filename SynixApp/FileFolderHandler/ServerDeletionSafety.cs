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
using Synix_Control_Panel.SynixEngine;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Synix_Control_Panel.SynixApp.FileFolderHandler;

/// <summary>Final, game-independent checks before recursive server-folder deletion.</summary>
internal static class ServerDeletionSafety
{
	internal static string ValidatePath(string path, string? backupBase = null)
	{
		string fullPath = NormalizePath(path);
		if (SamePath(fullPath, Path.GetPathRoot(fullPath)!))
			throw UnsafePath(path);

		// Reject aliases through directory links before checking protected locations.
		// This also prevents a custom path from hiding a protected parent directory.
		for (DirectoryInfo? directory = new(fullPath); directory != null; directory = directory.Parent)
		{
			if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
			{
				throw new InvalidOperationException(LocalizationManager.Get(
					"FileSystem.Error.LinkedDeletionPath", path));
			}
		}

		if (Directory.Exists(fullPath))
			fullPath = ExpandExistingLongPath(fullPath);

		foreach (string root in ProtectedRoots())
		{
			if (!string.IsNullOrWhiteSpace(root) && IsSameOrAncestor(fullPath, root))
				throw UnsafePath(path);
		}
		foreach (string tree in ProtectedTrees())
		{
			if (!string.IsNullOrWhiteSpace(tree) &&
				(IsSameOrAncestor(fullPath, tree) || IsSameOrAncestor(tree, fullPath)))
				throw UnsafePath(path);
		}

		string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string? profilesRoot = string.IsNullOrWhiteSpace(profile) ? null : Path.GetDirectoryName(profile);
		string? parent = Path.GetDirectoryName(fullPath);
		// Protect other user-profile roots and the per-game containers shared by servers.
		foreach (string? container in new[] { profilesRoot, Core.GamesPath, Core.DefaultBackupPath, backupBase })
		{
			if (!string.IsNullOrWhiteSpace(container) &&
				(IsSameOrAncestor(fullPath, container) || (parent != null && SamePath(parent, container))))
				throw UnsafePath(path);
		}

		return fullPath;
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
			path.StartsWith(@"\\?\", StringComparison.Ordinal) ||
			path.StartsWith(@"\\.\", StringComparison.Ordinal))
			throw UnsafePath(path);

		string root = Path.GetPathRoot(path)!;
		foreach (string segment in path[root.Length..].Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries))
		{
			// GetFullPath normalizes a single trailing period before protected-path
			// comparisons (some game names end in one). Reject ambiguous trailing
			// spaces/multiple periods, relative components, and alternate streams.
			if (segment is "." or ".." || segment.EndsWith("..", StringComparison.Ordinal) || segment.EndsWith(' ') ||
				segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				throw UnsafePath(path);
		}

		try
		{
			return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
		}
		catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
		{
			throw UnsafePath(path);
		}
	}

	private static IEnumerable<string> ProtectedRoots()
	{
		yield return Core.RootPath;
		yield return Core.GamesPath;
		yield return Core.DefaultBackupPath;
		yield return Path.GetTempPath();
		foreach (Environment.SpecialFolder folder in new[]
		{
			Environment.SpecialFolder.UserProfile, Environment.SpecialFolder.DesktopDirectory,
			Environment.SpecialFolder.MyDocuments, Environment.SpecialFolder.MyMusic,
			Environment.SpecialFolder.MyPictures, Environment.SpecialFolder.MyVideos,
			Environment.SpecialFolder.ApplicationData, Environment.SpecialFolder.LocalApplicationData,
			Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolder.CommonDocuments,
			Environment.SpecialFolder.CommonDesktopDirectory
		})
			yield return Environment.GetFolderPath(folder);

		string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrWhiteSpace(profile))
			yield return Path.Combine(profile, "Downloads");
		foreach (string variable in new[] { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" })
		{
			string? folder = Environment.GetEnvironmentVariable(variable);
			if (!string.IsNullOrWhiteSpace(folder) && Path.IsPathFullyQualified(folder))
				yield return folder;
		}
	}

	private static IEnumerable<string> ProtectedTrees()
	{
		yield return Core.DataPath;
		yield return Core.SteamCmdPath;
		yield return AppContext.BaseDirectory;
		foreach (Environment.SpecialFolder folder in new[]
		{
			Environment.SpecialFolder.Windows, Environment.SpecialFolder.System,
			Environment.SpecialFolder.SystemX86, Environment.SpecialFolder.ProgramFiles,
			Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.CommonProgramFiles,
			Environment.SpecialFolder.CommonProgramFilesX86
		})
			yield return Environment.GetFolderPath(folder);

		string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (!string.IsNullOrWhiteSpace(local))
		{
			yield return Path.Combine(local, "Synix");
			yield return Path.Combine(local, "Programs");
		}
		string common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
		if (!string.IsNullOrWhiteSpace(common))
			yield return Path.Combine(common, "Microsoft");
	}

	private static string ExpandExistingLongPath(string path)
	{
		StringBuilder buffer = new(32768);
		uint length = GetLongPathName(path, buffer, (uint)buffer.Capacity);
		if (length == 0 || length >= buffer.Capacity)
			throw new IOException(LocalizationManager.Get(
				"FileSystem.Error.DeletionPathUnavailable", path), new Win32Exception(Marshal.GetLastWin32Error()));
		return Path.TrimEndingDirectorySeparator(Path.GetFullPath(buffer.ToString()));
	}

	private static bool SamePath(string left, string right) => string.Equals(
		Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
		Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)), StringComparison.OrdinalIgnoreCase);

	private static bool IsSameOrAncestor(string candidate, string protectedPath)
	{
		string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
		string child = Path.TrimEndingDirectorySeparator(Path.GetFullPath(protectedPath));
		return SamePath(root, child) || child.StartsWith(
			root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar,
			StringComparison.OrdinalIgnoreCase);
	}

	private static InvalidOperationException UnsafePath(string? path) => new(LocalizationManager.Get(
		"FileSystem.Error.UnsafeDeletionPath", path ?? string.Empty));

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern uint GetLongPathName(string shortPath, StringBuilder longPath, uint bufferLength);
}
