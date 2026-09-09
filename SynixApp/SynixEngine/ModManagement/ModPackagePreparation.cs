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

internal readonly record struct ModPackageLimits(int Entries, long FileBytes, long TotalBytes)
{
	// Scenarios contain full galaxies and shared assets; other package limits stay unchanged.
	internal static ModPackageLimits For(ModInstallTarget target) =>
		ModPackageHandlers.For(target).Limits;
}

internal static class ModPackageFiles
{
	internal static string SuggestPackageName(string name)
	{
		string cleaned = string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '_')).Trim();
		if (cleaned.Length > 60) cleaned = cleaned[..60].TrimEnd();
		if (cleaned.Length == 0) cleaned = "Imported package";
		return ModPathSafety.IsSafeRelativePath(cleaned) ? cleaned : "Package-" + cleaned;
	}

	internal static void CopyExactBounded(Stream input, Stream output, long expectedBytes)
	{
		byte[] buffer = new byte[81920];
		long written = 0;
		int count;
		while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
		{
			written = checked(written + count);
			if (written > expectedBytes) throw Error("Size");
			output.Write(buffer, 0, count);
		}
		if (written != expectedBytes) throw Error("Size");
	}

	internal static InvalidDataException Error(string key) => new(LocalizationManager.Get("ModPackages.Error." + key));
}

// A folder is snapshotted into a private ZIP first, then uses the normal scan/hash/import gate.
internal sealed class PreparedModPackage : IDisposable
{
	private readonly string _root;
	internal string Path { get; }
	private PreparedModPackage(string root, string path) { _root = root; Path = path; }

	internal static PreparedModPackage FromFolder(string source, ModInstallTarget target)
	{
		if (!target.AllowFolderImport) throw ModPackageFiles.Error("FolderUnsupported");
		ModPathSafety.EnsureNoLinks(source);
		if (!Directory.Exists(source)) throw ModPackageFiles.Error("Layout");
		IModPackageHandler handler = ModPackageHandlers.For(target);
		string marker = handler.FolderMarker(target);
		if (string.IsNullOrWhiteSpace(marker)) throw ModPackageFiles.Error("Layout");
		if (!Directory.EnumerateFiles(source, marker).Any() &&
			(!handler.AllowsWrappedSourceFolder || !Directory.EnumerateDirectories(source)
				.Any(path => (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0 && Directory.EnumerateFiles(path, marker).Any())))
			throw ModPackageFiles.Error("Layout");
		string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SynixAddOn-" + Guid.NewGuid().ToString("N"));
		ModPathSafety.EnsureNoLinks(root);
		Directory.CreateDirectory(root);
		PreparedModPackage result = new(root, System.IO.Path.Combine(root,
			ModPackageFiles.SuggestPackageName(System.IO.Path.GetFileName(source.TrimEnd('\\', '/'))) + ".zip"));
		try
		{
			ModPackageLimits limits = ModPackageLimits.For(target);
			long bytes = 0;
			int count = 0;
			using (ZipArchive zip = ZipFile.Open(result.Path, ZipArchiveMode.Create))
			{
				Stack<string> pending = new();
				pending.Push(source);
				while (pending.TryPop(out string? directory))
				{
					foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
					{
						if (++count > limits.Entries) throw ModPackageFiles.Error("Size");
						ModPathSafety.EnsureNoLinks(entry);
						string relative = System.IO.Path.GetRelativePath(source, entry).Replace('\\', '/');
						if (!ModPathSafety.IsSafeRelativePath(relative)) throw ModPackageFiles.Error("Layout");
						if (Directory.Exists(entry)) { pending.Push(entry); continue; }
						using FileStream input = new(entry, FileMode.Open, FileAccess.Read, FileShare.Read);
						if (input.Length > limits.FileBytes || (bytes = checked(bytes + input.Length)) > limits.TotalBytes)
							throw ModPackageFiles.Error("Size");
						using Stream output = zip.CreateEntry(relative, CompressionLevel.Fastest).Open();
						ModPackageFiles.CopyExactBounded(input, output, input.Length);
					}
				}
			}
			return result;
		}
		catch { result.Dispose(); throw; }
	}

	public void Dispose()
	{
		ModPathSafety.EnsureTreeHasNoLinks(_root);
		if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
	}
}
