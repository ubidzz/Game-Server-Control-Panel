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
using Synix_Control_Panel.SynixApp.ServerHandler;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synix_Control_Panel.SynixEngine.ModManagement
{
	internal enum ModSystemSupportLevel
	{
		Managed,
		DetectedOnly
	}

	internal enum ModContentKind
	{
		Plugin,
		Mod
	}

	internal enum ModTargetMode
	{
		FileImport,
		ArgumentIds,
		ConfigurationIds,
		DetectionOnly
	}

	internal enum ModIdStoreStyle
	{
		Csv,
		RepeatedKey
	}

	internal sealed class ModIdStore
	{
		public string RelativePath { get; init; } = string.Empty;
		public string Section { get; init; } = string.Empty;
		public string Key { get; init; } = string.Empty;
		public ModIdStoreStyle Style { get; init; }
	}

	internal sealed class ModSystemCatalogDocument
	{
		public int SchemaVersion { get; init; }
		public List<ModSystemProfile> Profiles { get; init; } = [];
	}

	internal sealed class ModCatalogLink
	{
		public string Name { get; init; } = string.Empty;
		public string Url { get; init; } = string.Empty;
	}

	internal sealed class ModSystemProfile
	{
		public string Id { get; init; } = string.Empty;
		public string DisplayName { get; init; } = string.Empty;
		public string Description { get; init; } = string.Empty;
		public ModSystemSupportLevel SupportLevel { get; init; }
		public List<string> GameNames { get; init; } = [];
		public string FrameworkName { get; init; } = string.Empty;
		public List<string> FrameworkMarkers { get; init; } = [];
		public string CatalogUrl { get; init; } = string.Empty;
		public List<ModCatalogLink> Catalogs { get; init; } = [];
		public bool RestartRequired { get; init; } = true;
		public List<ModInstallTarget> Targets { get; init; } = [];

		[JsonIgnore]
		public bool CanManage => SupportLevel == ModSystemSupportLevel.Managed &&
			Targets.Any(target => target.CanManage);
	}

	internal sealed class ModInstallTarget
	{
		public string Id { get; init; } = string.Empty;
		public string DisplayName { get; init; } = string.Empty;
		public ModContentKind Kind { get; init; }
		public ModTargetMode Mode { get; init; }
		public string ProviderName { get; init; } = string.Empty;
		public string RelativePath { get; init; } = string.Empty;
		public List<string> AllowedExtensions { get; init; } = [];
		public List<string> MarkerPaths { get; init; } = [];
		public List<string> FrameworkNames { get; init; } = [];
		public bool AllowArchives { get; init; }
		public bool ArchiveOnly { get; init; }
		public bool PreserveArchiveContents { get; init; }
		public string RequiredArchiveFileName { get; init; } = string.Empty;
		public bool WrapRootArchiveFiles { get; init; }
		public bool ScanDirectories { get; init; }
		public bool Recursive { get; init; }
		public string ArgumentName { get; init; } = string.Empty;
		public int MaximumIds { get; init; } = 100;
		public List<string> RequiredArguments { get; init; } = [];
		public List<ModIdStore> IdStores { get; init; } = [];

		[JsonIgnore]
		public bool CanImport => Mode == ModTargetMode.FileImport;

		[JsonIgnore]
		public bool CanManageIds => Mode is ModTargetMode.ArgumentIds or
			ModTargetMode.ConfigurationIds;

		[JsonIgnore]
		public bool CanManage => CanImport || CanManageIds;
	}

	internal sealed record ModSystemDetection(
		ModSystemProfile Profile,
		bool FrameworkDetected,
		ModInstallTarget RecommendedTarget,
		IReadOnlyList<ModInstallTarget> ActiveTargets)
	{
		internal string SupportText => Profile.SupportLevel switch
		{
			_ when RecommendedTarget.CanManageIds =>
				LocalizationManager.Get("ModCatalog.Support.ProviderIds"),
			ModSystemSupportLevel.Managed when FrameworkDetected =>
				LocalizationManager.Get("ModCatalog.Support.FileImport"),
			ModSystemSupportLevel.Managed =>
				LocalizationManager.Get("ModCatalog.Support.FrameworkRequired"),
			_ => LocalizationManager.Get("ModCatalog.Support.DetectionOnly")
		};
	}

	internal static class ModSystemCatalog
	{
		private const int CurrentSchemaVersion = 1;
		private const int MaximumCatalogBytes = 256 * 1024;
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = false,
			ReadCommentHandling = JsonCommentHandling.Disallow,
			UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
			Converters =
			{
				new JsonStringEnumConverter(
					namingPolicy: null,
					allowIntegerValues: false)
			}
		};
		private static readonly Lazy<IReadOnlyList<ModSystemProfile>> EmbeddedProfileCache =
			new(LoadEmbeddedProfiles, LazyThreadSafetyMode.ExecutionAndPublication);
		private static readonly string[] GenericFolderCandidates =
		[
			"BepInEx/plugins",
			"oxide/plugins",
			"plugins",
			"mods"
		];
		private static readonly HashSet<string> ForbiddenImportExtensions = new(
			[
				".bat", ".cmd", ".com", ".exe", ".hta", ".js", ".lnk", ".msi",
				".msp", ".ps1", ".reg", ".scr", ".vbs", ".wsf"
			],
			StringComparer.OrdinalIgnoreCase);

		internal static string? ExternalProfileRootOverride { get; set; }
		internal static string ExternalProfileRoot => ExternalProfileRootOverride ??
			Path.Combine(Core.DataPath, "ModSystems");

		internal static IReadOnlyList<ModSystemProfile> Profiles => LoadAvailableProfiles();

		internal static IReadOnlyList<ModSystemProfile> GetProfiles(string gameName) =>
			Profiles.Where(profile => profile.GameNames.Any(name =>
				name.Equals(gameName, StringComparison.OrdinalIgnoreCase))).ToArray();

		internal static bool CanManageAddOns(GameServer? server) =>
			server != null && !MinecraftControlProfile.IsBedrock(server) &&
			// Folder discovery and catalog links alone are not mod-management support.
			// Keep supported games available even before their loader is installed.
			GetProfiles(server.Game).Any(profile => profile.CanManage);

		internal static IReadOnlyList<ModSystemProfile> GetProfiles(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			IReadOnlyList<ModSystemProfile> exact = GetProfiles(server.Game);
			if (exact.Count > 0)
				return exact;
			ModSystemProfile? discovered = BuildFolderDiscoveryProfile(server);
			return discovered == null ? [] : [discovered];
		}

		internal static ModSystemDetection? Detect(
			GameServer server,
			ModSystemProfile? profile = null)
		{
			ArgumentNullException.ThrowIfNull(server);
			profile ??= GetProfiles(server.Game).FirstOrDefault();
			if (profile == null || profile.Targets.Count == 0)
				return null;

			string framework = GameCapabilityResolver.UsesMinecraftLifecycle(server)
				? server.MinecraftLoader
				: server.ServerFramework;
			ModInstallTarget[] activeTargets = profile.Targets
				.Where(target => IsTargetActive(server.InstallPath, framework, target))
				.ToArray();
			ModInstallTarget recommended = activeTargets.FirstOrDefault(target => target.CanManage) ??
				profile.Targets.FirstOrDefault(target => target.CanManage) ??
				profile.Targets[0];
			bool frameworkDetected = profile.SupportLevel == ModSystemSupportLevel.DetectedOnly ||
				recommended.CanManageIds ||
				FrameworkMatches(profile, framework) ||
				profile.FrameworkMarkers.Any(marker => PathExists(server.InstallPath, marker)) ||
				activeTargets.Length > 0;

			return new ModSystemDetection(
				profile,
				frameworkDetected,
				recommended,
				activeTargets);
		}

		internal static ModSystemCatalogDocument Parse(string json, string sourceName)
		{
			if (string.IsNullOrWhiteSpace(json))
				throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.Empty", sourceName));
			if (json.Length > MaximumCatalogBytes)
				throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.TooLarge", sourceName));

			ModSystemCatalogDocument document;
			try
			{
				document = JsonSerializer.Deserialize<ModSystemCatalogDocument>(json, JsonOptions) ??
					throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.MissingCatalog", sourceName));
			}
			catch (JsonException exception)
			{
				throw new InvalidDataException(
					LocalizationManager.Get("ModCatalog.Error.InvalidCatalog", sourceName, exception.Message),
					exception);
			}

			Validate(document, sourceName);
			return document;
		}

		private static IReadOnlyList<ModSystemProfile> LoadEmbeddedProfiles()
		{
			Assembly assembly = typeof(ModSystemCatalog).Assembly;
			List<ModSystemProfile> profiles = [];
			foreach (string resourceName in assembly.GetManifestResourceNames()
				.Where(name => name.EndsWith(".modsystem.json", StringComparison.OrdinalIgnoreCase))
				.OrderBy(name => name, StringComparer.Ordinal))
			{
				using Stream stream = assembly.GetManifestResourceStream(resourceName) ??
					throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.ResourceOpen", resourceName));
				if (stream.Length > MaximumCatalogBytes)
					throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.TooLarge", resourceName));
				using StreamReader reader = new(stream);
				profiles.AddRange(Parse(reader.ReadToEnd(), resourceName).Profiles);
			}

			HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
			foreach (ModSystemProfile profile in profiles)
			{
				if (!ids.Add(profile.Id))
					throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.DuplicateProfile", profile.Id));
			}
			return profiles;
		}

		private static IReadOnlyList<ModSystemProfile> LoadAvailableProfiles()
		{
			Dictionary<string, ModSystemProfile> profiles = EmbeddedProfileCache.Value
				.ToDictionary(profile => profile.Id, StringComparer.OrdinalIgnoreCase);
			if (!Directory.Exists(ExternalProfileRoot))
				return profiles.Values.ToArray();

			foreach (string file in Directory.EnumerateFiles(
				ExternalProfileRoot,
				"*.modsystem.json",
				SearchOption.TopDirectoryOnly))
			{
				try
				{
					foreach (ModSystemProfile profile in Parse(File.ReadAllText(file), file).Profiles)
						profiles[profile.Id] = profile;
				}
				catch (Exception exception)
				{
					ApplicationLogService.WriteLocalized(
						"ModCatalog.Activity.ProfileIgnored",
						Color.Orange,
						false,
						Path.GetFileName(file),
						exception.Message);
				}
			}
			return profiles.Values.ToArray();
		}

		private static ModSystemProfile? BuildFolderDiscoveryProfile(GameServer server)
		{
			if (!Directory.Exists(server.InstallPath))
				return null;
			List<ModInstallTarget> targets = [];
			foreach (string relativePath in GenericFolderCandidates)
			{
				string fullPath = ResolveInsideInstallPath(server.InstallPath, relativePath);
				if (!Directory.Exists(fullPath))
					continue;
				string normalized = relativePath.Replace('\\', '/');
				string folderName = Path.GetFileName(relativePath);
				bool pluginFolder = folderName.Equals("plugins", StringComparison.OrdinalIgnoreCase);
				List<string> extensions = normalized.StartsWith("oxide/", StringComparison.OrdinalIgnoreCase)
					? [".cs", ".dll"]
					: normalized.StartsWith("BepInEx/", StringComparison.OrdinalIgnoreCase)
						? [".dll"]
						: pluginFolder
							? [".jar", ".dll", ".cs"]
							: [".jar", ".dll", ".pak", ".mod"];
					targets.Add(new ModInstallTarget
				{
					Id = normalized.Replace('/', '-').ToLowerInvariant(),
					DisplayName = relativePath.Replace('/', Path.DirectorySeparatorChar),
					Kind = pluginFolder ? ModContentKind.Plugin : ModContentKind.Mod,
					Mode = ModTargetMode.DetectionOnly,
					ProviderName = LocalizationManager.Get(
						"ModManager.DetectedProfile.Provider"),
					RelativePath = relativePath,
					AllowedExtensions = extensions,
					MarkerPaths = [relativePath],
					AllowArchives = false,
					ScanDirectories = true,
					Recursive = false
				});
			}
			if (targets.Count == 0)
				return null;

			return new ModSystemProfile
			{
				Id = "dynamic-folder-discovery",
				DisplayName = LocalizationManager.Get(
					"ModManager.DetectedProfile.DisplayName"),
				Description = LocalizationManager.Get(
					"ModManager.DetectedProfile.Description"),
				SupportLevel = ModSystemSupportLevel.DetectedOnly,
				GameNames = [server.Game],
				FrameworkName = LocalizationManager.Get(
					"ModManager.DetectedProfile.Framework"),
				Targets = targets
			};
		}

		private static void Validate(ModSystemCatalogDocument document, string sourceName)
		{
			if (document.SchemaVersion != CurrentSchemaVersion)
				throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.SchemaVersion", sourceName));
			if (document.Profiles.Count == 0)
				throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.NoProfiles", sourceName));

			foreach (ModSystemProfile profile in document.Profiles)
			{
				if (string.IsNullOrWhiteSpace(profile.Id) ||
					string.IsNullOrWhiteSpace(profile.DisplayName) ||
					profile.GameNames.Count == 0 ||
					profile.Targets.Count == 0)
				{
					throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.IncompleteProfile", sourceName));
				}
				if (!string.IsNullOrWhiteSpace(profile.CatalogUrl) &&
					!IsSafeCatalogUrl(profile.CatalogUrl))
				{
					throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.UnsafeAddress", sourceName));
				}
				HashSet<string> catalogNames = new(StringComparer.OrdinalIgnoreCase);
				HashSet<string> catalogUrls = new(StringComparer.OrdinalIgnoreCase);
				foreach (ModCatalogLink catalog in profile.Catalogs)
				{
					if (string.IsNullOrWhiteSpace(catalog.Name) || catalog.Name.Length > 80 ||
						catalog.Name.Any(char.IsControl) || !IsSafeCatalogUrl(catalog.Url) ||
						!catalogNames.Add(catalog.Name.Trim()) || !catalogUrls.Add(catalog.Url))
					{
						throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.InvalidCatalogChoice", sourceName));
					}
				}

				HashSet<string> targetIds = new(StringComparer.OrdinalIgnoreCase);
				foreach (ModInstallTarget target in profile.Targets)
				{
					if (string.IsNullOrWhiteSpace(target.Id) ||
						string.IsNullOrWhiteSpace(target.DisplayName) ||
						(!target.CanManageIds &&
							(string.IsNullOrWhiteSpace(target.RelativePath) ||
								!IsSafeRelativePath(target.RelativePath))) ||
						!targetIds.Add(target.Id))
					{
						throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.InvalidTarget", sourceName));
					}
					if (target.Mode == ModTargetMode.FileImport && target.AllowedExtensions.Count == 0)
						throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.NoFileTypes", sourceName));
					if (target.Mode == ModTargetMode.FileImport && target.AllowedExtensions.Any(extension =>
						ForbiddenImportExtensions.Contains(extension)))
					{
						throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.DangerousType", sourceName));
					}
					if ((target.ArchiveOnly || target.PreserveArchiveContents ||
						!string.IsNullOrWhiteSpace(target.RequiredArchiveFileName) ||
						target.WrapRootArchiveFiles) && !target.AllowArchives)
					{
						throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.ArchiveRules", sourceName));
					}
					if (!string.IsNullOrWhiteSpace(target.RequiredArchiveFileName) &&
						(target.RequiredArchiveFileName.Length > 128 ||
						target.RequiredArchiveFileName != Path.GetFileName(target.RequiredArchiveFileName) ||
						target.RequiredArchiveFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
					{
						throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.InvalidArchiveName", sourceName));
					}
					if (target.WrapRootArchiveFiles && string.IsNullOrWhiteSpace(target.RequiredArchiveFileName))
						throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.ArchiveMarkerRequired", sourceName));
					if (target.Mode == ModTargetMode.ArgumentIds &&
						(string.IsNullOrWhiteSpace(target.ArgumentName) ||
							!target.ArgumentName.StartsWith('-') ||
							target.ArgumentName.Any(character =>
								!char.IsAsciiLetterOrDigit(character) && character != '-')))
					{
						throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.InvalidProviderArgument", sourceName));
					}
					if (target.Mode == ModTargetMode.ConfigurationIds)
					{
						if (target.IdStores.Count == 0)
							throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.NoConfigStores", sourceName));
						foreach (ModIdStore store in target.IdStores)
						{
							if (!IsSafeRelativePath(store.RelativePath) ||
								!IsSafeIniName(store.Section) ||
								!IsSafeIniName(store.Key))
							{
								throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.InvalidConfigStore", sourceName));
							}
						}
					}
					foreach (string requiredArgument in target.RequiredArguments)
					{
						if (string.IsNullOrWhiteSpace(requiredArgument) ||
							!requiredArgument.StartsWith('-') ||
							requiredArgument.Any(character =>
								!char.IsAsciiLetterOrDigit(character) && character != '-'))
						{
							throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.InvalidLaunchArgument", sourceName));
						}
					}
					foreach (string extension in target.AllowedExtensions)
					{
						if (extension.Length is < 2 or > 12 ||
							!extension.StartsWith('.') ||
							extension.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '.'))
						{
							throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.InvalidExtension", sourceName));
						}
					}
				}
			}
		}

		internal static bool IsSafeRelativePath(string value)
		{
			return ModPathSafety.IsSafeRelativePath(value);
		}

		private static bool IsSafeCatalogUrl(string value) =>
			Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
			uri.Scheme == Uri.UriSchemeHttps &&
			uri.IsDefaultPort &&
			string.IsNullOrEmpty(uri.UserInfo);

		private static bool IsSafeIniName(string value) =>
			!string.IsNullOrWhiteSpace(value) &&
			value.Length <= 128 &&
			value.IndexOfAny(['[', ']', '=', '\r', '\n', '\0']) < 0;

		private static bool FrameworkMatches(ModSystemProfile profile, string framework) =>
			!string.IsNullOrWhiteSpace(profile.FrameworkName) &&
			!string.IsNullOrWhiteSpace(framework) &&
			profile.FrameworkName.Equals(framework, StringComparison.OrdinalIgnoreCase);

		private static bool IsTargetActive(
			string installPath,
			string framework,
			ModInstallTarget target) =>
			target.CanManageIds ||
			target.FrameworkNames.Any(name => name.Equals(framework, StringComparison.OrdinalIgnoreCase)) ||
			target.MarkerPaths.Any(marker => PathExists(installPath, marker)) ||
			(!string.IsNullOrWhiteSpace(target.RelativePath) &&
				Directory.Exists(ResolveInsideInstallPath(installPath, target.RelativePath)));

		private static bool PathExists(string installPath, string relativePath)
		{
			string path = ResolveInsideInstallPath(installPath, relativePath);
			return File.Exists(path) || Directory.Exists(path);
		}

		internal static string ResolveInsideInstallPath(string installPath, string relativePath)
		{
			if (string.IsNullOrWhiteSpace(installPath))
				throw new InvalidOperationException(LocalizationManager.Get("ModCatalog.Error.InstallFolderMissing"));
			if (!IsSafeRelativePath(relativePath))
				throw new InvalidDataException(LocalizationManager.Get("ModCatalog.Error.UnsafeFolder"));
			return ModPathSafety.Resolve(installPath, relativePath);
		}
	}
}
