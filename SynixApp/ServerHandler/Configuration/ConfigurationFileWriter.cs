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
namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	internal static class ConfigurationFileWriter
	{
		public static void WriteAtomically(string path, byte[] content)
		{
			string fullPath = Path.GetFullPath(path);
			string directory = Path.GetDirectoryName(fullPath)
				?? throw new InvalidOperationException(LocalizationManager.Get(
					"Configuration.Editor.Error.DirectoryUnavailable"));
			string temporaryPath = Path.Combine(
				directory,
				$".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.synix.tmp");
			string backupPath = fullPath + ".synix.bak";

			try
			{
				File.WriteAllBytes(temporaryPath, content);
				try
				{
					File.Replace(temporaryPath, fullPath, backupPath, true);
				}
				catch (PlatformNotSupportedException)
				{
					ReplaceWithFallback(temporaryPath, fullPath, backupPath);
				}
				catch (IOException)
				{
					ReplaceWithFallback(temporaryPath, fullPath, backupPath);
				}
			}
			finally
			{
				if (File.Exists(temporaryPath))
					File.Delete(temporaryPath);
			}
		}

		private static void ReplaceWithFallback(
			string temporaryPath,
			string destinationPath,
			string backupPath)
		{
			if (!File.Exists(temporaryPath))
				return;

			File.Copy(destinationPath, backupPath, true);
			File.Move(temporaryPath, destinationPath, true);
		}
	}
}
