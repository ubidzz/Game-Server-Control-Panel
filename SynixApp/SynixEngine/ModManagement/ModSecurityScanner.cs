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
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixEngine.ModManagement
{
	internal enum ModSecurityOutcome
	{
		Passed,
		ReviewRequired,
		Blocked
	}

	internal enum ModSecurityFindingSeverity
	{
		Information,
		Warning,
		Blocked
	}

	internal sealed record ModSecurityFinding(
		ModSecurityFindingSeverity Severity,
		string Message);

	internal sealed record ModSecurityReview(
		ModSecurityOutcome Outcome,
		string PackageSha256,
		string AntivirusStatus,
		IReadOnlyList<ModSecurityFinding> Findings)
	{
		internal string BuildUserMessage()
		{
			string heading = LocalizationManager.Get(Outcome switch
			{
				ModSecurityOutcome.Blocked => "ModSecurity.Heading.Blocked",
				ModSecurityOutcome.ReviewRequired =>
					"ModSecurity.Heading.ReviewRequired",
				_ => "ModSecurity.Heading.Completed"
			});
			StringBuilder message = new();
			message.AppendLine(heading);
			message.AppendLine();
			message.AppendLine(LocalizationManager.Get(
				"ModSecurity.Antivirus",
				AntivirusStatus));
			message.AppendLine(LocalizationManager.Get(
				"ModSecurity.Sha256",
				PackageSha256));
			foreach (ModSecurityFinding finding in Findings.Take(8))
				message.AppendLine($"• {finding.Message}");
			if (Findings.Count > 8)
				message.AppendLine(LocalizationManager.Get(
					"ModSecurity.MoreFindings",
					Findings.Count - 8));
			message.AppendLine();
			message.Append(LocalizationManager.Get("ModSecurity.SafetyNotice"));
			return message.ToString();
		}
	}

	internal static class ModSecurityScanner
	{
		private const int MaximumArchiveEntries = 2048;
		private const long MaximumSingleFileBytes = 256L * 1024 * 1024;
		private const long MaximumArchiveBytes = 512L * 1024 * 1024;
		private const int MaximumSourceInspectionBytes = 2 * 1024 * 1024;
		private static readonly HashSet<string> DangerousArchiveExtensions = new(
			[
				".bat", ".cmd", ".com", ".exe", ".hta", ".js", ".lnk", ".msi",
				".msp", ".ps1", ".reg", ".scr", ".vbs", ".wsf"
			],
			StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> ReservedWindowsNames = new(
			[
				"CON", "PRN", "AUX", "NUL",
				"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
				"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
			],
			StringComparer.OrdinalIgnoreCase);
		private static readonly (string Token, string Description)[] RiskySourceCapabilities =
		[
			("System.Diagnostics.Process", "ModSecurity.Capability.StartPrograms"),
			("Process.Start", "ModSecurity.Capability.StartPrograms"),
			("cmd.exe", "ModSecurity.Capability.CommandPrompt"),
			("powershell", "ModSecurity.Capability.PowerShell"),
			("DllImport", "ModSecurity.Capability.NativeFunctions"),
			("NativeLibrary", "ModSecurity.Capability.NativeLibraries"),
			("Microsoft.Win32.Registry", "ModSecurity.Capability.Registry"),
			("RegistryKey", "ModSecurity.Capability.Registry"),
			("File.Delete", "ModSecurity.Capability.DeleteFiles"),
			("Directory.Delete", "ModSecurity.Capability.DeleteFolders"),
			("HttpClient", "ModSecurity.Capability.Internet"),
			("WebClient", "ModSecurity.Capability.Internet"),
			("System.Net.Sockets", "ModSecurity.Capability.Network"),
			("Assembly.Load", "ModSecurity.Capability.LoadCode")
		];

		internal static bool IsCurrentProcessElevated()
		{
			try
			{
				using WindowsIdentity identity = WindowsIdentity.GetCurrent();
				return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
			}
			catch
			{
				return false;
			}
		}

		internal static async Task<ModSecurityReview> ReviewPackageAsync(
			string packagePath,
			ModInstallTarget target,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(target);
			List<ModSecurityFinding> findings = [];
			string hash;
			try
			{
				if (!File.Exists(packagePath))
					throw new FileNotFoundException(LocalizationManager.Get(
						"ModSecurity.Error.PackageMissing"));
				hash = await ComputeSha256Async(packagePath, cancellationToken);
				InspectPackageStructure(packagePath, target, findings);
			}
			catch (Exception exception) when (exception is IOException or
				UnauthorizedAccessException or InvalidDataException or CryptographicException or
				DecoderFallbackException or OverflowException)
			{
				findings.Add(new ModSecurityFinding(
					ModSecurityFindingSeverity.Blocked,
					exception.Message));
				return new ModSecurityReview(
					ModSecurityOutcome.Blocked,
					LocalizationManager.Get("ModSecurity.Status.Unavailable"),
					LocalizationManager.Get(
						"ModSecurity.Antivirus.NotRun"),
					findings);
			}

			DefenderScanResult antivirus = await ScanWithMicrosoftDefenderAsync(
				packagePath,
				cancellationToken);
			if (antivirus.Blocked)
			{
				findings.Add(new ModSecurityFinding(
					ModSecurityFindingSeverity.Blocked,
					LocalizationManager.Get(
						"ModSecurity.Finding.DefenderThreat")));
			}
			else if (!antivirus.Completed)
			{
				findings.Add(new ModSecurityFinding(
					ModSecurityFindingSeverity.Warning,
					LocalizationManager.Get(
						"ModSecurity.Finding.DefenderUnavailable")));
			}

			ModSecurityOutcome outcome = findings.Any(finding =>
				finding.Severity == ModSecurityFindingSeverity.Blocked)
				? ModSecurityOutcome.Blocked
				: findings.Any(finding => finding.Severity == ModSecurityFindingSeverity.Warning)
					? ModSecurityOutcome.ReviewRequired
					: ModSecurityOutcome.Passed;
			return new ModSecurityReview(outcome, hash, antivirus.Status, findings);
		}

		internal static IReadOnlyList<ModSecurityFinding> InspectPackageStructure(
			string packagePath,
			ModInstallTarget target)
		{
			List<ModSecurityFinding> findings = [];
			InspectPackageStructure(packagePath, target, findings);
			return findings;
		}

		private static void InspectPackageStructure(
			string packagePath,
			ModInstallTarget target,
			List<ModSecurityFinding> findings)
		{
			FileInfo package = new(packagePath);
			if (package.Length is <= 0 or > MaximumArchiveBytes)
				throw new InvalidDataException(LocalizationManager.Get(
					"ModSecurity.Error.UnsafeSize"));

			string extension = Path.GetExtension(packagePath);
			if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
			{
				if (!target.AllowArchives)
					throw new InvalidDataException(LocalizationManager.Get(
						"ModSecurity.Error.ZipNotAccepted"));
				InspectZip(packagePath, target, findings);
				return;
			}
			if (target.ArchiveOnly)
				throw new InvalidDataException(LocalizationManager.Get(
					"ModSecurity.Error.ZipOnly"));
			if (!target.AllowedExtensions.Any(allowed =>
				allowed.Equals(extension, StringComparison.OrdinalIgnoreCase)))
			{
				throw new InvalidDataException(LocalizationManager.Get(
					"ModSecurity.Error.FileTypeNotAllowed"));
			}
			if (package.Length > MaximumSingleFileBytes)
				throw new InvalidDataException(LocalizationManager.Get(
					"ModSecurity.Error.FileTooLarge"));

			ValidateFileSignature(packagePath, extension);
			AddCodeCapabilityNotice(extension, findings);
			if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
			{
				if (package.Length <= MaximumSourceInspectionBytes)
					InspectSourceText(File.ReadAllText(packagePath), findings);
				else
				{
					findings.Add(new ModSecurityFinding(
						ModSecurityFindingSeverity.Warning,
						LocalizationManager.Get(
							"ModSecurity.Finding.SourceTooLarge")));
				}
			}
		}

		private static void InspectZip(
			string packagePath,
			ModInstallTarget target,
			List<ModSecurityFinding> findings)
		{
			using ZipArchive archive = ZipFile.OpenRead(packagePath);
			if (archive.Entries.Count > MaximumArchiveEntries)
				throw new InvalidDataException(LocalizationManager.Get(
					"ModSecurity.Error.TooManyFiles"));

			long totalBytes = 0;
			int supportedFiles = 0;
			bool requiredFileFound = string.IsNullOrWhiteSpace(target.RequiredArchiveFileName);
			HashSet<string> destinations = new(StringComparer.OrdinalIgnoreCase);
			foreach (ZipArchiveEntry entry in archive.Entries)
			{
				if (string.IsNullOrWhiteSpace(entry.Name))
					continue;
				totalBytes = checked(totalBytes + entry.Length);
				if (entry.Length > MaximumSingleFileBytes || totalBytes > MaximumArchiveBytes)
					throw new InvalidDataException(LocalizationManager.Get(
						"ModSecurity.Error.ExpandedTooLarge"));
				if (IsSymbolicLink(entry))
					throw new InvalidDataException(LocalizationManager.Get(
						"ModSecurity.Error.SymbolicLink"));

				string relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
				ValidateArchivePath(relative);
				if (!destinations.Add(relative))
					throw new InvalidDataException(LocalizationManager.Get(
						"ModSecurity.Error.DuplicatePaths"));

				string extension = Path.GetExtension(entry.Name);
				if (entry.Name.Equals(target.RequiredArchiveFileName, StringComparison.OrdinalIgnoreCase))
					requiredFileFound = true;
				if (DangerousArchiveExtensions.Contains(extension))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"ModSecurity.Error.DangerousFile",
							entry.Name));
				}
				if (!target.PreserveArchiveContents && !target.AllowedExtensions.Any(allowed =>
					allowed.Equals(extension, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
				}

				supportedFiles++;
				AddCodeCapabilityNotice(extension, findings);
				using Stream stream = entry.Open();
				ValidateStreamSignature(stream, extension, entry.Name);
				if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) &&
					entry.Length <= MaximumSourceInspectionBytes)
				{
					using StreamReader reader = new(
						stream,
						new UTF8Encoding(false, true),
						detectEncodingFromByteOrderMarks: true,
						leaveOpen: true);
					InspectSourceText(reader.ReadToEnd(), findings);
				}
			}
			if (!requiredFileFound)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"ModSecurity.Error.RequiredFileMissing",
						target.RequiredArchiveFileName));
			}
			if (supportedFiles == 0)
				throw new InvalidDataException(LocalizationManager.Get(
					"ModSecurity.Error.NoSupportedFile"));
		}

		private static void ValidateArchivePath(string relativePath)
		{
			if (!ModSystemCatalog.IsSafeRelativePath(relativePath) || relativePath.Contains(':'))
				throw new InvalidDataException(LocalizationManager.Get(
					"ModSecurity.Error.PathEscapesFolder"));
			foreach (string part in relativePath.Split(
				[Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
				StringSplitOptions.RemoveEmptyEntries))
			{
				string fileName = Path.GetFileNameWithoutExtension(part).TrimEnd('.', ' ');
				if (part.EndsWith(' ') || part.EndsWith('.') ||
					part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
					ReservedWindowsNames.Contains(fileName))
				{
					throw new InvalidDataException(LocalizationManager.Get(
						"ModSecurity.Error.InvalidWindowsName"));
				}
			}
		}

		private static void ValidateFileSignature(string path, string extension)
		{
			using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			ValidateStreamSignature(stream, extension, Path.GetFileName(path));
		}

		private static void ValidateStreamSignature(Stream stream, string extension, string name)
		{
			if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
			{
				Span<byte> signature = stackalloc byte[2];
				if (stream.Read(signature) != signature.Length || signature[0] != (byte)'M' || signature[1] != (byte)'Z')
					throw new InvalidDataException(LocalizationManager.Get(
						"ModSecurity.Error.InvalidWindowsLibrary",
						name));
			}
			else if (extension.Equals(".jar", StringComparison.OrdinalIgnoreCase))
			{
				Span<byte> signature = stackalloc byte[4];
				if (stream.Read(signature) != signature.Length ||
					signature[0] != (byte)'P' || signature[1] != (byte)'K')
				{
					throw new InvalidDataException(LocalizationManager.Get(
						"ModSecurity.Error.InvalidJavaArchive",
						name));
				}
			}
		}

		private static void AddCodeCapabilityNotice(
			string extension,
			List<ModSecurityFinding> findings)
		{
			if (findings.Any(finding =>
				finding.Message == LocalizationManager.Get(
					"ModSecurity.Finding.ExecutableCode")))
			{
				return;
			}
			if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
				extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
				extension.Equals(".jar", StringComparison.OrdinalIgnoreCase))
			{
				findings.Add(new ModSecurityFinding(
					ModSecurityFindingSeverity.Information,
					LocalizationManager.Get(
						"ModSecurity.Finding.ExecutableCode")));
			}
		}

		private static void InspectSourceText(
			string source,
			List<ModSecurityFinding> findings)
		{
			if (source.IndexOf('\0') >= 0)
				throw new InvalidDataException(LocalizationManager.Get(
					"ModSecurity.Error.BinarySource"));
			foreach ((string token, string descriptionKey) in RiskySourceCapabilities)
			{
				string description = LocalizationManager.Get(descriptionKey);
				if (!source.Contains(token, StringComparison.OrdinalIgnoreCase) ||
					findings.Any(finding => finding.Message.Contains(description, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
				}
				findings.Add(new ModSecurityFinding(
					ModSecurityFindingSeverity.Warning,
					LocalizationManager.Get(
						"ModSecurity.Finding.Capability",
						description)));
			}
		}

		private static bool IsSymbolicLink(ZipArchiveEntry entry) =>
			((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000;

		private static async Task<string> ComputeSha256Async(
			string path,
			CancellationToken cancellationToken)
		{
			using FileStream stream = new(
				path,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				81920,
				FileOptions.Asynchronous | FileOptions.SequentialScan);
			byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
			return Convert.ToHexString(hash);
		}

		private static async Task<DefenderScanResult> ScanWithMicrosoftDefenderAsync(
			string packagePath,
			CancellationToken cancellationToken)
		{
			string? executable = FindMicrosoftDefenderCli();
			if (executable == null)
				return new DefenderScanResult(
					false,
					false,
					LocalizationManager.Get(
						"ModSecurity.Defender.ScannerUnavailable"));

			using Process process = new()
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = executable,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}
			};
			process.StartInfo.ArgumentList.Add("-Scan");
			process.StartInfo.ArgumentList.Add("-ScanType");
			process.StartInfo.ArgumentList.Add("3");
			process.StartInfo.ArgumentList.Add("-File");
			process.StartInfo.ArgumentList.Add(Path.GetFullPath(packagePath));
			process.StartInfo.ArgumentList.Add("-DisableRemediation");
			try
			{
				if (!process.Start())
					return new DefenderScanResult(
						false,
						false,
						LocalizationManager.Get(
							"ModSecurity.Defender.StartFailed"));
				Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
				Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
				using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
					cancellationToken);
				timeout.CancelAfter(TimeSpan.FromMinutes(2));
				await process.WaitForExitAsync(timeout.Token);
				await Task.WhenAll(standardOutput, standardError);
				string scanOutput = standardOutput.Result + Environment.NewLine + standardError.Result;
				if (OutputReportsThreat(scanOutput))
				{
					return new DefenderScanResult(
						true,
						true,
						LocalizationManager.Get(
							"ModSecurity.Defender.Threat"));
				}
				return process.ExitCode switch
				{
					0 => new DefenderScanResult(
						true,
						false,
						LocalizationManager.Get(
							"ModSecurity.Defender.Clean")),
					2 => new DefenderScanResult(
						false,
						false,
						LocalizationManager.Get(
							"ModSecurity.Defender.Inconclusive")),
					_ => new DefenderScanResult(
						false,
						false,
						LocalizationManager.Get(
							"ModSecurity.Defender.ScanFailed",
							process.ExitCode))
				};
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				TryTerminate(process);
				return new DefenderScanResult(
					false,
					false,
					LocalizationManager.Get(
						"ModSecurity.Defender.TimedOut"));
			}
			catch (Exception exception) when (exception is InvalidOperationException or
				System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
			{
				TryTerminate(process);
				return new DefenderScanResult(
					false,
					false,
					LocalizationManager.Get(
						"ModSecurity.Defender.PermissionRequired"));
			}
		}

		internal static bool OutputReportsThreat(string output)
		{
			if (string.IsNullOrWhiteSpace(output))
				return false;

			return Regex.IsMatch(
				output,
				@"\bfound\s+[1-9][0-9]*\s+threats?\b",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
				TimeSpan.FromMilliseconds(250)) ||
				output.Contains("threat detected", StringComparison.OrdinalIgnoreCase) ||
				output.Contains("threat name:", StringComparison.OrdinalIgnoreCase) ||
				output.Contains("threatname:", StringComparison.OrdinalIgnoreCase);
		}

		private static string? FindMicrosoftDefenderCli()
		{
			string commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string platformRoot = Path.Combine(commonData, "Microsoft", "Windows Defender", "Platform");
			try
			{
				if (Directory.Exists(platformRoot))
				{
					foreach (string directory in Directory.EnumerateDirectories(platformRoot)
						.OrderByDescending(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
					{
						string candidate = Path.Combine(directory, "MpCmdRun.exe");
						if (File.Exists(candidate))
							return candidate;
					}
				}
			}
			catch (UnauthorizedAccessException suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}

			string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string fallback = Path.Combine(programFiles, "Windows Defender", "MpCmdRun.exe");
			return File.Exists(fallback) ? fallback : null;
		}

		private static void TryTerminate(Process process)
		{
			try
			{
				if (!process.HasExited)
					process.Kill(entireProcessTree: true);
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
		}

		private sealed record DefenderScanResult(
			bool Completed,
			bool Blocked,
			string Status);
	}
}
