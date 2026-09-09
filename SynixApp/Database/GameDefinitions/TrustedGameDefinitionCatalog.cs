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
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixApp.Database.GameDefinitions
{
	internal sealed class EmbeddedConfigurationTemplate
	{
		public int Revision { get; init; } = 1;
		public string RelativePath { get; init; } = string.Empty;
		public string TemplateFile { get; init; } = string.Empty;
		public string Content { get; set; } = string.Empty;
	}

	internal sealed class EmbeddedConfigurationDefinition
	{
		public int SchemaVersion { get; init; } = 1;
		public int Revision { get; init; } = 1;
		public bool RequiresNetworkAddresses { get; init; }
		public IReadOnlyList<ManagedConfigurationInput> ManagedInputs { get; init; } = [];
		public IReadOnlyList<EmbeddedConfigurationTemplate> Templates { get; init; } = [];
	}

	internal enum TrustedPostInstallActionType
	{
		Unknown,
		CopySteamRuntimeFiles,
		EnsureDirectory
	}

	internal sealed class EmbeddedPostInstallAction
	{
		public TrustedPostInstallActionType Type { get; init; }
		public string TargetDirectory { get; init; } = string.Empty;
	}

	internal sealed class EmbeddedGameDefinition
	{
		public int SchemaVersion { get; init; } = 1;
		public int DefinitionRevision { get; init; } = 1;
		public string Id { get; init; } = string.Empty;
		public int CatalogOrder { get; init; } = int.MaxValue;
		public string Game { get; init; } = string.Empty;
		public IReadOnlyList<string> Aliases { get; init; } = [];
		public string AppId { get; init; } = string.Empty;
		public bool RequiresSteamLogin { get; init; }
		public string SteamAppConfig { get; init; } = string.Empty;
		public string Executable { get; init; } = string.Empty;
		public string DownloadUrl { get; init; } = string.Empty;
		public string Arguments { get; init; } = string.Empty;
		public string RconSyntax { get; init; } = string.Empty;
		public int Port { get; init; }
		public int QueryPort { get; init; }
		public int? AppPort { get; init; }
		public int MaximumPlayers { get; init; } = GameDefinition.DefaultMaximumPlayers;
		public bool RequiresAdminPassword { get; init; }
		public bool RequiresAuthenticationToken { get; init; }
		public string AuthenticationTokenLabel { get; init; } =
			LocalizationManager.GetEnglish("GameInput.AuthenticationToken.Label");
		public string AuthenticationTokenHelpUrl { get; init; } = string.Empty;
		public int MinimumServerPasswordLength { get; init; }
		public bool ServerPasswordMustNotAppearInName { get; init; }
		public int WorldSize { get; init; }
		public string WorldSeed { get; init; } = "12345";
		public IReadOnlyList<string> Maps { get; init; } = [];
		public IReadOnlyList<string> GameModes { get; init; } = [];
		public string PvpValue { get; init; } = "PVP";
		public string PveValue { get; init; } = "PVE";
		public string BooleanTrueValue { get; init; } = "true";
		public string BooleanFalseValue { get; init; } = "false";
		public string CrossplayEnabledValue { get; init; } = "true";
		public string CrossplayDisabledValue { get; init; } = "false";
		public ConfigFileCreationMode ConfigFileCreation { get; init; } =
			ConfigFileCreationMode.Unknown;
		public string RelativeConfigPath { get; init; } = string.Empty;
		public ConfigFormat Format { get; init; } = ConfigFormat.StandardINI;
		public string ExternalDataFolderName { get; init; } = string.Empty;
		public IReadOnlyList<string> RequiredLaunchFiles { get; init; } = [];
		public IReadOnlyList<string> OptionalLaunchFiles { get; init; } = [];
		public string LaunchFileSetupInstructions { get; init; } = string.Empty;
		public bool NeedsConfigWarning { get; init; }
		public string WarningMessage { get; init; } =
			LocalizationManager.GetEnglish(
				"GameDefinition.Default.ConfigurationWarning");
		public string IconUrl { get; init; } = string.Empty;
		public bool IsQueryable { get; init; } = true;
		public bool CrossplayDisablesPlayerTracking { get; init; }
		public ServerProbeProtocol ProbeProtocol { get; init; } = ServerProbeProtocol.Auto;
		public bool SupportsManualConnectionTesting { get; init; } = true;
		public string ProbePath { get; init; } = string.Empty;
		public string EosDeploymentId { get; init; } = string.Empty;
		public GameRuntimeRequirements RuntimeRequirements { get; init; } = new();
		public GameLaunchBehavior LaunchBehavior { get; init; } = new();
		public GameControlCapabilities ControlCapabilities { get; init; } = new();
		public IReadOnlyList<string> SupportedServerFrameworks { get; init; } = [];
		public IReadOnlyList<string> LogPaths { get; init; } = [];
		public IReadOnlyList<EmbeddedPostInstallAction> PostInstallActions { get; init; } = [];
		public EmbeddedConfigurationDefinition? Configuration { get; init; }
	}

	internal sealed record EmbeddedGamePackage(
		GameInfo Definition,
		EmbeddedConfigurationDefinition? Configuration,
		IReadOnlyList<EmbeddedPostInstallAction> PostInstallActions,
		string ResourceName);

	internal static partial class TrustedGameDefinitionCatalog
	{
		private const int MaximumDefinitionBytes = 1024 * 1024;
		private const int MaximumTemplateCharacters = 512 * 1024;
		private static readonly Regex PlaceholderPattern =
			PlaceholderRegex();
		private static readonly HashSet<string> SupportedPlaceholders =
			new(StringComparer.Ordinal)
			{
				"ServerName",
				"Password",
				"HasPassword",
				"AdminPassword",
				"MaxPlayers",
				"Port",
				"QueryPort",
				"RCONPort",
				"RCONPassword",
				"EnableRcon",
				"Identity",
				"WorldName",
				"WorldSeed",
				"WorldSize",
				"AppPort",
				"LocalIP",
				"PublicIP",
				"IsPvp",
				"IsPve",
				"Crossplay",
				"GameMode"
			};
		private static readonly JsonSerializerOptions SerializerOptions =
			new()
			{
				PropertyNameCaseInsensitive = false,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				ReadCommentHandling = JsonCommentHandling.Disallow,
				UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
				Converters =
				{
					new JsonStringEnumConverter(
						namingPolicy: null,
						allowIntegerValues: false)
				}
			};
		private static readonly Lazy<IReadOnlyList<EmbeddedGamePackage>> PackageCache =
			new(LoadEmbeddedPackages, LazyThreadSafetyMode.ExecutionAndPublication);

		internal static IReadOnlyList<EmbeddedGamePackage> Packages => PackageCache.Value;

		internal static bool TryGetPackage(
			string gameName,
			out EmbeddedGamePackage? package)
		{
			string canonicalName = GameDatabase.GetCanonicalGameName(gameName);
			package = Packages.FirstOrDefault(candidate =>
				string.Equals(
					candidate.Definition.Game,
					canonicalName,
					StringComparison.OrdinalIgnoreCase));
			return package != null;
		}

		internal static IReadOnlyList<GameInfo> LoadDefinitions()
		{
			GameInfo[] definitions = Packages
				.Select(package => package.Definition)
				.ToArray();
			ValidateCatalog(definitions);
			return definitions;
		}

		internal static EmbeddedGamePackage ParsePackage(
			string json,
			string resourceName,
			Func<string, string, string?>? templateLoader = null)
		{
			if (string.IsNullOrWhiteSpace(json))
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.ResourceEmpty",
					resourceName));
			if (json.Length > MaximumDefinitionBytes)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.ResourceTooLarge",
					resourceName));

			EmbeddedGameDefinition manifest;
			try
			{
				manifest = JsonSerializer.Deserialize<EmbeddedGameDefinition>(
					json,
					SerializerOptions) ?? throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.DefinitionMissing",
							resourceName));
			}
			catch (JsonException exception)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.InvalidDefinition",
						resourceName,
						exception.Message),
					exception);
			}

			LoadTemplateFiles(manifest, resourceName, templateLoader);
			ValidateManifest(manifest, resourceName);
			string relativeConfigPath = !string.IsNullOrWhiteSpace(manifest.RelativeConfigPath)
				? manifest.RelativeConfigPath
				: manifest.Configuration?.Templates.FirstOrDefault()?.RelativePath ?? string.Empty;

			GameInfo definition = new()
			{
				DefinitionId = manifest.Id,
				CatalogOrder = manifest.CatalogOrder,
				Game = manifest.Game.Trim(),
				Aliases = manifest.Aliases.Select(alias => alias.Trim()).ToArray(),
				DefinitionSchemaVersion = manifest.SchemaVersion,
				DefinitionRevision = manifest.DefinitionRevision,
				IsEmbeddedDefinition = true,
				AppID = manifest.AppId.Trim(),
				RequiresSteamLogin = manifest.RequiresSteamLogin,
				SteamAppConfig = manifest.SteamAppConfig,
				ExeName = manifest.Executable.Trim(),
				DownloadUrl = manifest.DownloadUrl.Trim(),
				RequiredArgs = manifest.Arguments,
				RconSyntax = manifest.RconSyntax,
				Port = manifest.Port,
				QueryPort = manifest.QueryPort,
				AppPort = manifest.AppPort,
				MaximumPlayers = manifest.MaximumPlayers,
				RequiresAdminPassword = manifest.RequiresAdminPassword,
				RequiresAuthenticationToken = manifest.RequiresAuthenticationToken,
				AuthenticationTokenLabel = manifest.AuthenticationTokenLabel.Trim(),
				AuthenticationTokenHelpUrl = manifest.AuthenticationTokenHelpUrl.Trim(),
				MinimumServerPasswordLength = manifest.MinimumServerPasswordLength,
				ServerPasswordMustNotAppearInName = manifest.ServerPasswordMustNotAppearInName,
				WorldSize = manifest.WorldSize,
				WorldSeed = manifest.WorldSeed,
				Maps = manifest.Maps.ToList(),
				GameModes = manifest.GameModes.ToList(),
				PvpValue = manifest.PvpValue,
				PveValue = manifest.PveValue,
				BooleanTrueValue = manifest.BooleanTrueValue,
				BooleanFalseValue = manifest.BooleanFalseValue,
				CrossplayEnabledValue = manifest.CrossplayEnabledValue,
				CrossplayDisabledValue = manifest.CrossplayDisabledValue,
				ConfigFileCreation = manifest.ConfigFileCreation,
				RelativeConfigPath = relativeConfigPath,
				Format = manifest.Format,
				ExternalDataFolderName = manifest.ExternalDataFolderName,
				RequiredLaunchFiles = manifest.RequiredLaunchFiles.ToArray(),
				OptionalLaunchFiles = manifest.OptionalLaunchFiles.ToArray(),
				LaunchFileSetupInstructions = manifest.LaunchFileSetupInstructions,
				NeedsConfigWarning = manifest.NeedsConfigWarning,
				WarningMessage = manifest.WarningMessage,
				IconUrl = manifest.IconUrl,
				IsQueryable = manifest.IsQueryable,
				CrossplayDisablesPlayerTracking = manifest.CrossplayDisablesPlayerTracking,
				ProbeProtocol = manifest.ProbeProtocol,
				SupportsManualConnectionTesting = manifest.SupportsManualConnectionTesting,
				ProbePath = manifest.ProbePath,
				EosDeploymentId = manifest.EosDeploymentId,
				RuntimeRequirements = manifest.RuntimeRequirements,
				LaunchBehavior = manifest.LaunchBehavior,
				ControlCapabilities = manifest.ControlCapabilities,
				SupportedServerFrameworks = manifest.SupportedServerFrameworks
					.Select(framework => framework.Trim())
					.ToArray(),
				LogPaths = manifest.LogPaths
					.Select(path => path.Trim())
					.ToArray()
			};

			return new EmbeddedGamePackage(
				definition,
				manifest.Configuration,
				manifest.PostInstallActions.ToArray(),
				resourceName);
		}

		private static IReadOnlyList<EmbeddedGamePackage> LoadEmbeddedPackages()
		{
			Assembly assembly = typeof(TrustedGameDefinitionCatalog).Assembly;
			List<EmbeddedGamePackage> packages = [];
			foreach (string resourceName in assembly.GetManifestResourceNames()
				.Where(name => name.EndsWith(".game.json", StringComparison.OrdinalIgnoreCase))
				.OrderBy(name => name, StringComparer.Ordinal))
			{
				using Stream stream = assembly.GetManifestResourceStream(resourceName) ??
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.ResourceOpenFailed",
						resourceName));
				if (stream.Length > MaximumDefinitionBytes)
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.ResourceTooLarge",
						resourceName));
				using StreamReader reader = new(stream);
				packages.Add(ParsePackage(
					reader.ReadToEnd(),
					resourceName,
					(manifestId, templateFile) => ReadEmbeddedTemplate(
						assembly,
						resourceName,
						manifestId,
						templateFile)));
			}

			HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
			foreach (EmbeddedGamePackage package in packages)
			{
				if (!names.Add(package.Definition.Game))
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.DuplicateEmbedded",
						package.Definition.Game));
			}

			return packages
				.OrderBy(package => package.Definition.CatalogOrder)
				.ThenBy(package => package.Definition.Game, StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		private static void ValidateManifest(
			EmbeddedGameDefinition manifest,
			string resourceName)
		{
			if (manifest.SchemaVersion != 1)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.SchemaUnsupported",
					resourceName,
					manifest.SchemaVersion));
			if (manifest.DefinitionRevision < 1)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.InvalidField",
					resourceName,
					"definitionRevision"));
			ValidateText(manifest.Id, "id", resourceName, 80, required: true);
			if (manifest.CatalogOrder < 0)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.InvalidField",
					resourceName,
					"catalogOrder"));
			if (!manifest.Id.All(character =>
				char.IsAsciiLetterOrDigit(character) || character == '-'))
			{
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.InvalidDefinitionId",
					resourceName));
			}
			ValidateText(manifest.Game, "game", resourceName, 120, required: true);
			ValidateText(manifest.AppId, "appId", resourceName, 32, required: true);
			if (!manifest.AppId.All(char.IsDigit))
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.NonNumericAppId",
					resourceName));
			ValidateSteamAppConfig(manifest.SteamAppConfig, manifest.AppId, resourceName);
			ValidateRelativePath(manifest.Executable, "executable", resourceName, required: true);
			ValidateSingleLine(manifest.Arguments, "arguments", resourceName, 16_384);
			ValidateSingleLine(manifest.RconSyntax, "rconSyntax", resourceName, 4_096);
			GameDefinitionArgumentTags.ValidateLaunchArguments(
				manifest.Arguments,
				resourceName);
			GameDefinitionArgumentTags.ValidateRconSyntax(
				manifest.RconSyntax,
				resourceName);
			bool usesRconTag = manifest.Arguments.Contains(
				"{rcon}",
				StringComparison.Ordinal);
			if (!string.IsNullOrWhiteSpace(manifest.RconSyntax) && !usesRconTag)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.RconSyntaxUnused",
						resourceName));
			}
			if (usesRconTag && string.IsNullOrWhiteSpace(manifest.RconSyntax))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.RconSyntaxMissing",
						resourceName));
			}
			ValidatePort(manifest.Port, "port", resourceName);
			ValidatePort(manifest.QueryPort, "queryPort", resourceName);
			if (manifest.AppPort.HasValue)
				ValidatePort(manifest.AppPort.Value, "appPort", resourceName);
			if (manifest.MaximumPlayers is < 1 or > 100_000)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.InvalidFieldValue",
						resourceName,
						"maximumPlayers"));
			}
			if (manifest.MinimumServerPasswordLength is < 0 or > 1024)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.InvalidFieldValue",
						resourceName,
						"minimumServerPasswordLength"));
			}
			if ((manifest.MinimumServerPasswordLength > 0 ||
				 manifest.ServerPasswordMustNotAppearInName) &&
				!manifest.Arguments.Contains("{pass}", StringComparison.Ordinal) &&
				!(manifest.Configuration?.Templates.Any(template =>
					template.Content.Contains("{Password}", StringComparison.Ordinal)) ?? false))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.PasswordPlaceholderMissing",
						resourceName));
			}
			if (manifest.RequiresAdminPassword &&
				!manifest.Arguments.Contains("{adminpass}", StringComparison.Ordinal) &&
				!(manifest.Configuration?.Templates.Any(template =>
					template.Content.Contains("{AdminPassword}", StringComparison.Ordinal)) ?? false))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.AdminPasswordPlaceholderMissing",
						resourceName));
			}
			bool usesAuthenticationToken = manifest.Arguments.Contains(
				"{auth_token}",
				StringComparison.Ordinal);
			if (manifest.RequiresAuthenticationToken && !usesAuthenticationToken)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.AuthenticationTokenPlaceholderMissing",
						resourceName));
			}
			if (!manifest.RequiresAuthenticationToken && usesAuthenticationToken)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.AuthenticationTokenNotDeclared",
						resourceName));
			}
			ValidateText(
				manifest.AuthenticationTokenLabel,
				"authenticationTokenLabel",
				resourceName,
				80,
				required: manifest.RequiresAuthenticationToken);
			ValidateHttpsUrl(
				manifest.AuthenticationTokenHelpUrl,
				"authenticationTokenHelpUrl",
				resourceName);
			ValidateHttpsUrl(manifest.DownloadUrl, "downloadUrl", resourceName);
			ValidateHttpsUrl(manifest.IconUrl, "iconUrl", resourceName);
			ValidateRelativePath(manifest.RelativeConfigPath, "relativeConfigPath", resourceName);
			ValidateRelativePath(manifest.ExternalDataFolderName, "externalDataFolderName", resourceName);
			foreach (string path in manifest.RequiredLaunchFiles)
				ValidateRelativePath(path, "requiredLaunchFiles", resourceName, required: true);
			foreach (string path in manifest.OptionalLaunchFiles)
				ValidateRelativePath(path, "optionalLaunchFiles", resourceName, required: true);
			ValidateUniqueText(manifest.Aliases, "aliases", resourceName);
			ValidateUniqueText(manifest.Maps, "maps", resourceName);
			ValidateUniqueText(manifest.GameModes, "gameModes", resourceName);
			ValidateDefinitionValue(manifest.PvpValue, "pvpValue", resourceName);
			ValidateDefinitionValue(manifest.PveValue, "pveValue", resourceName);
			ValidateDefinitionValue(manifest.BooleanTrueValue, "booleanTrueValue", resourceName);
			ValidateDefinitionValue(manifest.BooleanFalseValue, "booleanFalseValue", resourceName);
			ValidateDefinitionValue(manifest.CrossplayEnabledValue, "crossplayEnabledValue", resourceName);
			if (!string.IsNullOrEmpty(manifest.CrossplayDisabledValue))
				ValidateDefinitionValue(manifest.CrossplayDisabledValue, "crossplayDisabledValue", resourceName);
			if (manifest.CrossplayDisablesPlayerTracking &&
				!manifest.Arguments.Contains("{crossplay}", StringComparison.Ordinal))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.CrossplayPlaceholderMissing",
						resourceName));
			}
			ValidateRuntimeRequirements(manifest.RuntimeRequirements, resourceName);
			ValidateLaunchBehavior(manifest, resourceName);
			ValidateControlCapabilities(manifest.ControlCapabilities, resourceName);
			ValidateSupportedServerFrameworks(manifest, resourceName);
			ValidateUniqueText(manifest.LogPaths, "logPaths", resourceName);
			foreach (string path in manifest.LogPaths)
				ValidateRelativePattern(path, "logPaths", resourceName);
			if (manifest.PostInstallActions == null)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.NullCollection",
					resourceName,
					"postInstallActions"));
			if (manifest.PostInstallActions.Count > 16)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.TooManyPostInstallActions",
					resourceName));
			HashSet<string> postInstallActions = new(StringComparer.OrdinalIgnoreCase);
			foreach (EmbeddedPostInstallAction action in manifest.PostInstallActions)
			{
				if (action.Type is not TrustedPostInstallActionType.CopySteamRuntimeFiles and
					not TrustedPostInstallActionType.EnsureDirectory)
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.UnsupportedPostInstallAction",
							resourceName));
				}
				ValidateRelativePath(
					action.TargetDirectory,
					"postInstallActions.targetDirectory",
					resourceName,
					required: action.Type == TrustedPostInstallActionType.EnsureDirectory);
				string identity = $"{action.Type}\u001f{action.TargetDirectory}";
				if (!postInstallActions.Add(identity))
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.DuplicatePostInstallAction",
						resourceName));
			}

			if (manifest.Configuration != null)
			{
				if (manifest.ConfigFileCreation is not
						(ConfigFileCreationMode.SynixTemplate or
						 ConfigFileCreationMode.GameGenerated))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.InvalidTemplateMode",
							resourceName));
				}
				if (manifest.Configuration.SchemaVersion < 1)
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.InvalidConfigurationSchema",
						resourceName));
				if (manifest.Configuration.Revision < 1)
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.InvalidConfigurationRevision",
						resourceName));
				ValidateManagedInputs(manifest.Configuration, resourceName);
				HashSet<string> templatePaths = new(StringComparer.OrdinalIgnoreCase);
				foreach (EmbeddedConfigurationTemplate template in manifest.Configuration.Templates)
				{
					if (template.Revision < 1 ||
						template.Revision > manifest.Configuration.Revision)
					{
						throw new InvalidDataException(
							LocalizationManager.Get(
								"GameDefinition.Error.InvalidTemplateRevision",
								resourceName));
					}
					ValidateRelativePath(template.RelativePath, "configuration.templates.relativePath", resourceName, required: true);
					ValidateRelativePath(template.TemplateFile, "configuration.templates.templateFile", resourceName);
					if (!templatePaths.Add(template.RelativePath))
						throw new InvalidDataException(LocalizationManager.Get(
							"GameDefinition.Error.DuplicateTemplatePaths",
							resourceName));
					if (string.IsNullOrWhiteSpace(template.Content) || template.Content.Length > MaximumTemplateCharacters)
						throw new InvalidDataException(LocalizationManager.Get(
							"GameDefinition.Error.InvalidTemplateContent",
							resourceName));
					foreach (Match match in PlaceholderPattern.Matches(template.Content))
					{
						string placeholder = match.Groups[1].Value;
						if (!SupportedPlaceholders.Contains(placeholder))
							throw new InvalidDataException(LocalizationManager.Get(
								"GameDefinition.Error.UnsupportedPlaceholder",
								resourceName,
								placeholder));
					}
				}
			}
		}

		private static void ValidateRuntimeRequirements(
			GameRuntimeRequirements requirements,
			string resourceName)
		{
			if (requirements == null)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.NullField",
					resourceName,
					"runtimeRequirements"));
			if (requirements.MinimumSystemMemoryGb is < 0 or > 1024)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.InvalidField",
					resourceName,
					"minimumSystemMemoryGb"));
			if (!Enum.IsDefined(requirements.MinimumDotNetFramework))
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.InvalidField",
					resourceName,
					"minimumDotNetFramework"));
			if (requirements.VisualCppRedistributables == null)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.NullField",
						resourceName,
						"runtimeRequirements.visualCppRedistributables"));
			}
			if (requirements.VisualCppRedistributables.Any(requirement =>
				!Enum.IsDefined(requirement)) ||
				requirements.VisualCppRedistributables.Count !=
				requirements.VisualCppRedistributables.Distinct().Count())
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.InvalidVisualCppRequirement",
						resourceName));
			}
			if (requirements.RequiresHyperV &&
				!requirements.RequiresWindowsProfessionalOrHigher)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.HyperVWindowsRequirement",
						resourceName));
			}
		}

		private static void ValidateLaunchBehavior(
			EmbeddedGameDefinition manifest,
			string resourceName)
		{
			GameLaunchBehavior behavior = manifest.LaunchBehavior ??
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.NullField",
					resourceName,
					"launchBehavior"));
			ValidateText(
				behavior.SteamAppId,
				"launchBehavior.steamAppId",
				resourceName,
				10,
				required: false);
			if (!string.IsNullOrEmpty(behavior.SteamAppId) &&
				(!behavior.SteamAppId.All(char.IsAsciiDigit) ||
				 !uint.TryParse(behavior.SteamAppId, out uint steamAppId) ||
				 steamAppId == 0))
			{
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.InvalidFieldValue",
					resourceName,
					"launchBehavior.steamAppId"));
			}
			if (!GameLaunchCommandBuilder.TryGetLauncherKind(
				manifest.Executable,
				out _))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.UnsupportedLaunchFile",
						resourceName));
			}
			ValidateText(
				behavior.ReadyMessage,
				"launchBehavior.readyMessage",
				resourceName,
				512,
				required: false);
			ValidateText(
				behavior.ReadyLogText,
				"launchBehavior.readyLogText",
				resourceName,
				512,
				required: false);
			if (behavior.RunElevated &&
				!manifest.Executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
				!manifest.Executable.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) &&
				!manifest.Executable.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.UnsupportedElevatedLaunch",
						resourceName));
			}
			if (behavior.LifecycleTracking == GameLifecycleTrackingMode.ExternalDeployment &&
				manifest.IsQueryable)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.ExternalLifecycleQueryable",
						resourceName));
			}
		}

		private static void ValidateSupportedServerFrameworks(
			EmbeddedGameDefinition manifest,
			string resourceName)
		{
			ValidateUniqueText(
				manifest.SupportedServerFrameworks,
				"supportedServerFrameworks",
				resourceName);
			foreach (string framework in manifest.SupportedServerFrameworks)
			{
				if (!framework.Equals("Oxide", StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.UnsupportedFramework",
							resourceName));
				}
				if (!manifest.Id.Equals("rust", StringComparison.OrdinalIgnoreCase) ||
					manifest.AppId != "258550")
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.OxideOnlyForRust",
							resourceName));
				}
			}
		}

		private static void ValidateControlCapabilities(
			GameControlCapabilities? capabilities,
			string resourceName)
		{
			if (capabilities == null)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.NullField",
					resourceName,
					"controlCapabilities"));

			if (!Enum.IsDefined(capabilities.Lifecycle) ||
				!Enum.IsDefined(capabilities.Console) ||
				!Enum.IsDefined(capabilities.Configuration) ||
				!Enum.IsDefined(capabilities.Players))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.UnsupportedControlCapability",
						resourceName));
			}

			bool usesMinecraftController =
				capabilities.Console == GameConsoleControllerKind.Minecraft ||
				capabilities.Configuration == GameConfigurationControllerKind.Minecraft ||
				capabilities.Players == GamePlayerControllerKind.Minecraft;
			if (usesMinecraftController &&
				capabilities.Lifecycle != GameLifecycleControllerKind.Minecraft)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.MinecraftLifecycleRequired",
						resourceName));
			}
		}

		private static void ValidateManagedInputs(
			EmbeddedConfigurationDefinition configuration,
			string resourceName)
		{
			if (configuration.ManagedInputs == null)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.NullCollection",
					resourceName,
					"configuration.managedInputs"));

			ManagedConfigurationInput declared = ManagedConfigurationInput.None;
			foreach (ManagedConfigurationInput input in configuration.ManagedInputs)
			{
				if (input == ManagedConfigurationInput.None ||
					!Enum.IsDefined(input) ||
					(declared & input) != ManagedConfigurationInput.None)
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.InvalidManagedInput",
							resourceName));
				}
				declared |= input;
			}

			if (configuration.ManagedInputs.Count == 0)
				return;

			ManagedConfigurationInput discovered = GetTemplateManagedInputs(
				configuration.Templates.Select(template => template.Content));
			if (declared != discovered)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.ManagedInputsMismatch",
						resourceName,
						declared,
						discovered));
			}
		}

		internal static ManagedConfigurationInput GetTemplateManagedInputs(
			IEnumerable<string> templates)
		{
			ManagedConfigurationInput inputs = ManagedConfigurationInput.None;
			foreach (string content in templates)
			{
				if (content.Contains("{ServerName}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.ServerName;
				if (content.Contains("{Password}", StringComparison.Ordinal) ||
					content.Contains("{HasPassword}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.ServerPassword;
				if (content.Contains("{AdminPassword}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.AdminPassword;
				if (content.Contains("{WorldSeed}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.WorldSeed;
				if (content.Contains("{GameMode}", StringComparison.Ordinal) ||
					content.Contains("{IsPvp}", StringComparison.Ordinal) ||
					content.Contains("{IsPve}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.GameMode;
				if (content.Contains("{MaxPlayers}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.MaxPlayers;
				if (content.Contains("{QueryPort}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.QueryPort;
				if (content.Contains("{WorldName}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.WorldName;
				if (content.Contains("{RCONPort}", StringComparison.Ordinal) ||
					content.Contains("{RCONPassword}", StringComparison.Ordinal) ||
					content.Contains("{EnableRcon}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.Rcon;
				if (content.Contains("{WorldSize}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.WorldSize;
				if (content.Contains("{Port}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.Port;
				if (content.Contains("{AppPort}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.AppPort;
				if (content.Contains("{Crossplay}", StringComparison.Ordinal))
					inputs |= ManagedConfigurationInput.Crossplay;
			}
			return inputs;
		}

		private static void LoadTemplateFiles(
			EmbeddedGameDefinition manifest,
			string resourceName,
			Func<string, string, string?>? templateLoader)
		{
			if (manifest.Configuration == null)
				return;

			foreach (EmbeddedConfigurationTemplate template in manifest.Configuration.Templates)
			{
				bool hasFile = !string.IsNullOrWhiteSpace(template.TemplateFile);
				bool hasContent = !string.IsNullOrWhiteSpace(template.Content);
				if (hasFile == hasContent)
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.TemplateSourceAmbiguous",
							resourceName));
				}

				if (!hasFile)
					continue;

				ValidateRelativePath(
					template.TemplateFile,
					"configuration.templates.templateFile",
					resourceName,
					required: true);
				string? content = templateLoader?.Invoke(
					manifest.Id,
					template.TemplateFile);
				if (content == null)
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.EmbeddedTemplateMissing",
							resourceName,
							template.TemplateFile));
				}

				template.Content = content;
			}
		}

		private static string? ReadEmbeddedTemplate(
			Assembly assembly,
			string manifestResourceName,
			string manifestId,
			string templateFile)
		{
			string manifestFileName = $"{manifestId}.game.json";
			if (!manifestResourceName.EndsWith(
				manifestFileName,
				StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			string prefix = manifestResourceName[..^manifestFileName.Length];
			string normalizedTemplate = templateFile
				.Replace('\\', '.')
				.Replace('/', '.');
			string resourceName = $"{prefix}Templates.{normalizedTemplate}";
			using Stream? stream = assembly.GetManifestResourceStream(resourceName);
			if (stream == null || stream.Length > MaximumTemplateCharacters)
				return null;
			using StreamReader reader = new(stream);
			return reader.ReadToEnd();
		}

		private static void ValidateCatalog(IReadOnlyList<GameInfo> definitions)
		{
			HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
			foreach (GameInfo definition in definitions)
			{
				if (!names.Add(definition.Game))
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.DuplicateDefinition",
						definition.Game));
				foreach (string alias in definition.Aliases)
				{
					if (!names.Add(alias))
						throw new InvalidDataException(LocalizationManager.Get(
							"GameDefinition.Error.DuplicateName",
							alias));
				}
			}
		}

		private static void ValidateUniqueText(
			IReadOnlyList<string>? values,
			string field,
			string resourceName)
		{
			if (values == null)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.NullCollection",
					resourceName,
					field));

			HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
			foreach (string value in values)
			{
				ValidateText(value, field, resourceName, 512, required: true);
				if (!unique.Add(value.Trim()))
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.DuplicateFieldValue",
						resourceName,
						field));
			}
		}

		private static void ValidateRelativePath(
			string path,
			string field,
			string resourceName,
			bool required = false)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				if (required)
					throw new InvalidDataException(LocalizationManager.Get(
						"GameDefinition.Error.RequiredField",
						resourceName,
						field));
				return;
			}
			if (!string.Equals(path, path.Trim(), StringComparison.Ordinal) ||
				path.Any(char.IsControl))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.InvalidPath",
						resourceName,
						field));
			}

			if (path.Length > 512 ||
				Path.IsPathRooted(path) ||
				path.Contains(':') ||
				path.IndexOfAny(['*', '?', '"', '<', '>', '|', '\0']) >= 0 ||
				path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
					.Any(segment => segment is "." or ".."))
			{
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.UnsafePath",
					resourceName,
					field));
			}
		}

		private static void ValidateRelativePattern(
			string path,
			string field,
			string resourceName)
		{
			if (string.IsNullOrWhiteSpace(path) ||
				!string.Equals(path, path.Trim(), StringComparison.Ordinal) ||
				path.Any(char.IsControl) ||
				path.Length > 512 ||
				Path.IsPathRooted(path) ||
				path.Contains(':') ||
				path.IndexOfAny(['"', '<', '>', '|', '\0']) >= 0 ||
				path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
					.Any(segment => segment is "." or ".."))
			{
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.UnsafePattern",
					resourceName,
					field));
			}

			foreach (Match match in PlaceholderPattern.Matches(path))
			{
				string placeholder = match.Groups[1].Value;
				if (placeholder is not "Identity" and not "ServerName" and
					not "WorldName" and not "Port" and not "QueryPort")
				{
					throw new InvalidDataException(
						LocalizationManager.Get(
							"GameDefinition.Error.UnsupportedLogPlaceholder",
							resourceName,
							placeholder));
				}
			}
		}

		private static void ValidateHttpsUrl(
			string value,
			string field,
			string resourceName)
		{
			if (string.IsNullOrWhiteSpace(value))
				return;
			if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
				uri.Scheme != Uri.UriSchemeHttps ||
				string.IsNullOrWhiteSpace(uri.Host) ||
				!string.IsNullOrEmpty(uri.UserInfo))
			{
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.UnsafeUrl",
					resourceName,
					field));
			}
		}

		private static void ValidatePort(int port, string field, string resourceName)
		{
			if (port is < 1 or > 65535)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.InvalidField",
					resourceName,
					field));
		}

		private static void ValidateSteamAppConfig(
			string value,
			string appId,
			string resourceName)
		{
			if (string.IsNullOrWhiteSpace(value))
				return;
			if (!string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
				!Regex.IsMatch(
					value,
					@"^[0-9]{1,10} mod [A-Za-z0-9_-]{1,64}$",
					RegexOptions.CultureInvariant) ||
				!value.StartsWith(appId + " mod ", StringComparison.Ordinal))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.UnsafeSteamAppConfig",
						resourceName));
			}
		}

		private static void ValidateSingleLine(
			string value,
			string field,
			string resourceName,
			int maximumLength)
		{
			if (value.Length > maximumLength || value.IndexOfAny(['\r', '\n', '\0']) >= 0)
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.InvalidFieldValue",
					resourceName,
					field));
		}

		private static void ValidateDefinitionValue(
			string value,
			string field,
			string resourceName)
		{
			ValidateText(value, field, resourceName, 64, required: true);
			if (!string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
				value.Any(character =>
					char.IsControl(character) ||
					char.IsWhiteSpace(character) ||
					character is '"' or '\'' or '{' or '}' or '&' or '|' or ';'))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Error.UnsafeFieldValue",
						resourceName,
						field));
			}
		}

		private static void ValidateText(
			string value,
			string field,
			string resourceName,
			int maximumLength,
			bool required)
		{
			if (required && string.IsNullOrWhiteSpace(value))
				throw new InvalidDataException(LocalizationManager.Get(
					"GameDefinition.Error.RequiredField",
					resourceName,
					field));
			ValidateSingleLine(value, field, resourceName, maximumLength);
		}

		[GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)\}", RegexOptions.CultureInvariant)]
		private static partial Regex PlaceholderRegex();
	}
}
