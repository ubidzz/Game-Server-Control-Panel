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

namespace Synix_Control_Panel.SynixEngine.ModManagement;

/// <summary>Shared boundaries for add-on destinations, history and rollback files.</summary>
internal static class ModPathSafety
{
	internal static bool IsSafeRelativePath(string? value)
	{
		if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value))
			return false;
		string[] parts = value.Replace('/', '\\').Split('\\');
		return parts.All(part =>
			part.Length > 0 && part is not "." and not ".." &&
			!part.EndsWith('.') && !part.EndsWith(' ') &&
			part.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
			!IsDeviceName(part));
	}

	internal static string Resolve(string rootPath, string relativePath)
	{
		if (!IsSafeRelativePath(relativePath))
			throw UnsafePath();
		string root = ValidateAbsolutePath(rootPath);
		string destination = Path.GetFullPath(Path.Combine(root, relativePath));
		if (!destination.StartsWith(root.TrimEnd('\\', '/') + Path.DirectorySeparatorChar,
			StringComparison.OrdinalIgnoreCase))
			throw UnsafePath();
		EnsureNoLinks(destination);
		return destination;
	}

	internal static void EnsureNoLinks(string path)
	{
		// GetAttributes also checks file links and dangling links; Exists alone does not.
		// Include the root and its ancestors, not just children of the selected folder.
		for (string? current = ValidateAbsolutePath(path); !string.IsNullOrEmpty(current);
			current = Path.GetDirectoryName(current))
		{
			try
			{
				if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
					throw new InvalidDataException(LocalizationManager.Get("ModManager.Error.LinkedPath"));
			}
			catch (FileNotFoundException) { }
			catch (DirectoryNotFoundException) { }
		}
	}

	internal static void EnsureTreeHasNoLinks(string path)
	{
		EnsureNoLinks(path);
		if (!Directory.Exists(path))
			return;
		Stack<string> pending = new();
		pending.Push(path);
		while (pending.TryPop(out string? directory))
		{
			foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
			{
				EnsureNoLinks(entry);
				if (Directory.Exists(entry))
					pending.Push(entry);
			}
		}
	}

	private static string ValidateAbsolutePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) ||
			path.StartsWith(@"\\?\", StringComparison.Ordinal) ||
			path.StartsWith(@"\\.\", StringComparison.Ordinal))
			throw UnsafePath();
		return Path.GetFullPath(path);
	}

	private static bool IsDeviceName(string part)
	{
		string name = part.Split('.')[0].ToUpperInvariant();
		return name is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" ||
			(name.Length == 4 && (name.StartsWith("COM", StringComparison.Ordinal) ||
				name.StartsWith("LPT", StringComparison.Ordinal)) && "123456789¹²³".Contains(name[3]));
	}

	private static InvalidDataException UnsafePath() =>
		new(LocalizationManager.Get("ModManager.Error.UnsafePath"));
}
