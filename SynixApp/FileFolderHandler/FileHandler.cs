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
using Synix_Control_Panel.SynixEngine;
using System.Text;
using System.Threading.Channels;

namespace Synix_Control_Panel.SynixApp.FileFolderHandler
{
	public static class FileHandler
	{
		private static readonly string FileName = "servers.json";

		private const int LogQueueCapacity = 4096;
		private const int MaxLogFilesPerCategory = 10;

		private static readonly object _logWriteLock = new();

		private static readonly Channel<(string LogFileName, string Content)>
			_logQueue = Channel.CreateBounded<(string LogFileName, string Content)>(
				new BoundedChannelOptions(LogQueueCapacity)
				{
					SingleReader = true,
					SingleWriter = false,
					AllowSynchronousContinuations = false,
					FullMode = BoundedChannelFullMode.DropOldest
				});

		private static readonly Task _logWorkerTask;
		private static int _loggingShutdownStarted;

		static FileHandler()
		{
			_logWorkerTask = Task.Run(ProcessLogQueueAsync);
		}

		public static bool SaveServers()
		{
			try
			{
				string jsonString = Core
					.SerializeServersForStorage(ServerRegistry.Servers);
				string savedPath = Path.Combine(Core.DataPath, FileName);

				WriteTextAtomically(savedPath, jsonString);

				ApplicationLogService.WriteLocalized(
					"FileHandler.Activity.Saved",
					Color.DarkSeaGreen,
					arguments: [savedPath]);
				return true;
			}
			catch (Exception ex)
			{
				ApplicationLogService.WriteLocalized(
					"FileHandler.Activity.SaveError",
					arguments: [ex.Message]);
				return false;
			}
		}

		public static void LoadServers()
		{
			string fullPath = Path.Combine(Core.DataPath, FileName);

			if (File.Exists(fullPath))
			{
				try
				{
					string jsonString = File.ReadAllText(fullPath);
					List<GameServer> loadedServers = Core
						.DeserializeServersAndMigrate(
							jsonString,
							out ServerDataMigrationSummary migrationSummary);

					if (loadedServers != null)
					{
						ServerRegistry.Servers.Clear();
						foreach (var server in loadedServers)
						{
							var masterData = GameDatabase.GetGame(server.Game);
							if (masterData != null)
							{
								string fullExePath = Path.Combine(server.InstallPath, masterData.ExeName);

								string iconPath = Synix_Control_Panel.SynixEngine.Core.GetLocalServerIcon(server.Game, fullExePath);

								if (File.Exists(iconPath))
								{
									if (!ServerIconCache.Icons.ContainsKey(server.Game))
									{
										using (var ms = new MemoryStream(File.ReadAllBytes(iconPath)))
										{
											using (var tempImage = System.Drawing.Image.FromStream(ms))
											{
												ServerIconCache.Icons[server.Game] = new Bitmap(tempImage);
											}
										}
									}
									server.DisplayIcon = ServerIconCache.Icons[server.Game];
								}
							}
							ServerRegistry.Servers.Add(server);
						}

						if (migrationSummary.Changed)
						{
							if (migrationSummary.MigratedServerCount > 0)
								CreateServerDataMigrationBackup(fullPath, migrationSummary.TargetVersion);

							if (SaveServers())
							{
								if (migrationSummary.MigratedServerCount > 0)
								{
									ApplicationLogService.WriteLocalized(
										"FileHandler.Activity.MigrationUpgraded",
										Color.DarkSeaGreen,
										arguments:
										[
											migrationSummary.MigratedServerCount,
											migrationSummary.TargetVersion
										]);
								}

								if (migrationSummary.MigratedPasswordServerCount > 0)
								{
									ApplicationLogService.WriteLocalized(
										"FileHandler.Activity.MigrationProtected",
										Color.DarkSeaGreen,
										arguments:
										[
											migrationSummary.MigratedPasswordServerCount
										]);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					ApplicationLogService.WriteLocalized(
						"FileHandler.Activity.LoadError",
						arguments: [ex.Message]);
				}
			}
		}

		internal static string CreateServerDataMigrationBackup(
			string fullPath,
			int targetVersion)
		{
			string backupPath = fullPath + $".before-data-v{targetVersion}.bak";
			if (!File.Exists(backupPath))
				File.Copy(fullPath, backupPath, overwrite: false);

			return backupPath;
		}

		public static void WriteTextAtomically(string fullPath, string content)
		{
			string? directory = Path.GetDirectoryName(fullPath);
			if (string.IsNullOrWhiteSpace(directory))
				throw new ArgumentException(LocalizationManager.Get(
					"FileSystem.Error.DestinationFolderRequired"), nameof(fullPath));

			Directory.CreateDirectory(directory);
			string temporaryPath = Path.Combine(
				directory,
				$".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

			try
			{
				using (FileStream stream = new(
					temporaryPath,
					FileMode.CreateNew,
					FileAccess.Write,
					FileShare.None,
					4096,
					FileOptions.WriteThrough))
				using (StreamWriter writer = new(
					stream,
					new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
					4096,
					leaveOpen: true))
				{
					writer.Write(content);
					writer.Flush();
					stream.Flush(flushToDisk: true);
				}

				if (File.Exists(fullPath))
					File.Replace(temporaryPath, fullPath, null, ignoreMetadataErrors: true);
				else
					File.Move(temporaryPath, fullPath);
			}
			finally
			{
				try
				{
					if (File.Exists(temporaryPath))
						File.Delete(temporaryPath);
				}
				catch (Exception suppressedException)
				{
					Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
				}
			}
		}

		public static bool Create(string folderPath, string fileName, string content)
		{
			try
			{
				FolderHandler.Create(folderPath);

				string fullPath = Path.Combine(folderPath, fileName);

				File.WriteAllText(fullPath, content);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool QueueLog(string logFileName, string content)
		{
			if (string.IsNullOrWhiteSpace(logFileName) ||
				string.IsNullOrWhiteSpace(content))
			{
				return false;
			}

			if (Volatile.Read(ref _loggingShutdownStarted) != 0)
				return false;

			return _logQueue.Writer.TryWrite(
				(logFileName, SecretRedactor.Redact(content)));
		}

		private static async Task ProcessLogQueueAsync()
		{
			await foreach (var entry in
				_logQueue.Reader.ReadAllAsync())
			{
				WriteLogCore(
					entry.LogFileName,
					entry.Content);
			}
		}

		public static async Task FlushLogsAsync()
		{
			if (Interlocked.Exchange(
					ref _loggingShutdownStarted,
					1) == 0)
			{
				_logQueue.Writer.TryComplete();
			}

			try
			{
				await _logWorkerTask.ConfigureAwait(false);
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
		}

		public static bool WriteLogImmediate(
	string logFileName,
	string content)
		{
			if (string.IsNullOrWhiteSpace(logFileName) ||
				string.IsNullOrWhiteSpace(content))
			{
				return false;
			}

			return WriteLogCore(logFileName, content);
		}

		private static bool WriteLogCore(
			string logFileName,
			string content)
		{
			content = SecretRedactor.Redact(content);
			lock (_logWriteLock)
			{
				try
				{
					FolderHandler.Create(Core.LogsPath);

					string safeLogFileName =
						SanitizeLogFileName(logFileName);

					DateTime currentTime = DateTime.Now;

					string dailyFileName =
						$"{safeLogFileName}_{currentTime:yyyy-MM-dd}.log";

					string fullPath = Path.Combine(
						Core.LogsPath,
						dailyFileName);

					bool isNewDailyFile = !File.Exists(fullPath);

					File.AppendAllText(
						fullPath,
						content.TrimEnd() + Environment.NewLine);

					if (isNewDailyFile)
					{
						CleanupOldLogFiles(safeLogFileName);
					}

					return true;
				}
				catch
				{
					return false;
				}
			}
		}

		private static void CleanupOldLogFiles(
			string safeLogFileName)
		{
			try
			{
				var oldLogFiles = new DirectoryInfo(Core.LogsPath)
					.GetFiles(
						$"{safeLogFileName}_*.log",
						SearchOption.TopDirectoryOnly)
					.OrderByDescending(
						file => file.LastWriteTimeUtc)
					.Skip(MaxLogFilesPerCategory)
					.ToList();

				foreach (FileInfo oldLogFile in oldLogFiles)
				{
					try
					{
						oldLogFile.Delete();
					}
					catch (Exception suppressedException)
					{
						Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
					}
				}
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
		}

		private static string SanitizeLogFileName(
			string logFileName)
		{
			char[] characters = logFileName.Trim().ToCharArray();
			char[] invalidCharacters = Path.GetInvalidFileNameChars();

			for (int index = 0; index < characters.Length; index++)
			{
				char character = characters[index];

				if (Array.IndexOf(invalidCharacters, character) >= 0 ||
					character == '/' ||
					character == '\\' ||
					character == '*' ||
					character == '?')
				{
					characters[index] = '_';
				}
			}

			string safeName = new string(characters)
				.Trim(' ', '.');

			if (string.IsNullOrWhiteSpace(safeName))
				safeName = "Synix";

			if (safeName.Length > 80)
				safeName = safeName[..80];

			return safeName;
		}

		public static bool Copy(string sourceFilePath, string targetFolderPath, string targetFileName, bool overwrite = true)
		{
			try
			{
				if (!File.Exists(sourceFilePath))
				{
					return false;
				}

				FolderHandler.Create(targetFolderPath);

				string fullTargetPath = Path.Combine(targetFolderPath, targetFileName);

				File.Copy(sourceFilePath, fullTargetPath, overwrite);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
