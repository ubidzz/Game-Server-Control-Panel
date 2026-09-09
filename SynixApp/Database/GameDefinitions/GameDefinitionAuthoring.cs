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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Synix_Control_Panel.SynixApp.Database.GameDefinitions
{
	internal sealed record GameDefinitionTemplateDraft(
		string RelativePath,
		string SourcePath);

	internal sealed class GameDefinitionDraft
	{
		public string Id { get; init; } = string.Empty;
		public int CatalogOrder { get; init; }
		public int DefinitionRevision { get; init; } = 1;
		public string Game { get; init; } = string.Empty;
		public string AppId { get; init; } = string.Empty;
		public bool RequiresSteamLogin { get; init; }
		public string SteamAppConfig { get; init; } = string.Empty;
		public string Executable { get; init; } = string.Empty;
		public string Arguments { get; init; } = string.Empty;
		public string RconSyntax { get; init; } = string.Empty;
		public int Port { get; init; } = 7777;
		public int QueryPort { get; init; } = 27015;
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
		public ConfigFormat Format { get; init; } = ConfigFormat.StandardINI;
		public string RelativeConfigPath { get; init; } = string.Empty;
		public string TemplateSourcePath { get; init; } = string.Empty;
		public IReadOnlyList<GameDefinitionTemplateDraft> AdditionalTemplates { get; init; } = [];
		public int ConfigurationRevision { get; init; } = 1;
		public string ExternalDataFolderName { get; init; } = string.Empty;
		public IReadOnlyList<string> RequiredLaunchFiles { get; init; } = [];
		public IReadOnlyList<string> OptionalLaunchFiles { get; init; } = [];
		public string LaunchFileSetupInstructions { get; init; } = string.Empty;
		public bool NeedsConfigWarning { get; init; }
		public string WarningMessage { get; init; } = string.Empty;
		public string IconUrl { get; init; } = string.Empty;
		public bool CopySteamRuntimeFiles { get; init; }
		public string SteamRuntimeTargetDirectory { get; init; } = string.Empty;
		public bool IsQueryable { get; init; } = true;
		public IReadOnlyList<string> LogPaths { get; init; } = [];
		public GameRuntimeRequirements RuntimeRequirements { get; init; } = new();
		public GameLaunchBehavior LaunchBehavior { get; init; } = new();
		public IReadOnlyList<string> SupportedServerFrameworks { get; init; } = [];
	}

	internal sealed record GameDefinitionSaveResult(
		string DefinitionPath,
		IReadOnlyList<string> TemplatePaths,
		string Json)
	{
		internal string? TemplatePath => TemplatePaths.FirstOrDefault();
	}

	internal static class GameDefinitionAuthoring
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true
		};

		internal static int GetNextCatalogOrder() =>
			GameDatabase.GetGameList().Count == 0
				? 0
				: GameDatabase.GetGameList().Max(game => game.CatalogOrder) + 1;

		internal static string CreateDefinitionJson(GameDefinitionDraft draft)
		{
			ArgumentNullException.ThrowIfNull(draft);
			JsonObject root = new()
			{
				["schemaVersion"] = 1,
				["definitionRevision"] = draft.DefinitionRevision,
				["id"] = draft.Id.Trim(),
				["catalogOrder"] = draft.CatalogOrder,
				["game"] = draft.Game.Trim(),
				["aliases"] = new JsonArray(),
				["appId"] = draft.AppId.Trim(),
				["requiresSteamLogin"] = draft.RequiresSteamLogin,
				["steamAppConfig"] = draft.SteamAppConfig.Trim(),
				["executable"] = draft.Executable.Trim(),
				["downloadUrl"] = string.Empty,
				["arguments"] = draft.Arguments.Trim(),
				["rconSyntax"] = draft.RconSyntax.Trim(),
				["port"] = draft.Port,
				["queryPort"] = draft.QueryPort,
				["appPort"] = null,
				["worldSize"] = 0,
				["worldSeed"] = "12345",
				["maps"] = CreateStringArray(draft.Maps),
				["gameModes"] = CreateStringArray(draft.GameModes),
				["pvpValue"] = draft.PvpValue.Trim(),
				["pveValue"] = draft.PveValue.Trim(),
				["booleanTrueValue"] = draft.BooleanTrueValue.Trim(),
				["booleanFalseValue"] = draft.BooleanFalseValue.Trim(),
				["crossplayEnabledValue"] = draft.CrossplayEnabledValue.Trim(),
				["crossplayDisabledValue"] = draft.CrossplayDisabledValue.Trim(),
				["configFileCreation"] = draft.ConfigFileCreation.ToString(),
				["relativeConfigPath"] = draft.RelativeConfigPath.Trim(),
				["format"] = draft.Format.ToString(),
				["externalDataFolderName"] = draft.ExternalDataFolderName.Trim(),
				["requiredLaunchFiles"] = CreateStringArray(draft.RequiredLaunchFiles),
				["optionalLaunchFiles"] = CreateStringArray(draft.OptionalLaunchFiles),
				["launchFileSetupInstructions"] = draft.LaunchFileSetupInstructions.Trim(),
				["needsConfigWarning"] =
					draft.NeedsConfigWarning ||
					draft.RequiredLaunchFiles.Count > 0 ||
					UsesManagedTemplate(draft),
				["warningMessage"] =
					string.IsNullOrWhiteSpace(draft.WarningMessage)
						? LocalizationManager.GetEnglish(
							"GameDefinition.Default.ConfigurationWarning")
						: draft.WarningMessage.Trim(),
				["iconUrl"] = draft.IconUrl.Trim(),
				["isQueryable"] = draft.IsQueryable,
				["probeProtocol"] = ServerProbeProtocol.Auto.ToString(),
				["supportsManualConnectionTesting"] = draft.IsQueryable,
				["probePath"] = string.Empty,
				["eosDeploymentId"] = string.Empty,
				["runtimeRequirements"] = new JsonObject
				{
					["minimumSystemMemoryGb"] = draft.RuntimeRequirements.MinimumSystemMemoryGb,
					["requiresAvx2"] = draft.RuntimeRequirements.RequiresAvx2,
					["requiresHardwareVirtualization"] = draft.RuntimeRequirements.RequiresHardwareVirtualization,
					["requiresHyperV"] = draft.RuntimeRequirements.RequiresHyperV,
					["requiresWindowsProfessionalOrHigher"] = draft.RuntimeRequirements.RequiresWindowsProfessionalOrHigher,
					["minimumDotNetFramework"] =
						draft.RuntimeRequirements.MinimumDotNetFramework.ToString(),
					["visualCppRedistributables"] = CreateStringArray(
						draft.RuntimeRequirements.VisualCppRedistributables.Select(
							requirement => requirement.ToString()))
				},
				["launchBehavior"] = new JsonObject
				{
					["steamAppId"] = draft.LaunchBehavior.SteamAppId,
					["runElevated"] = draft.LaunchBehavior.RunElevated,
					["requiresVisibleWindow"] =
						draft.LaunchBehavior.RequiresVisibleWindow,
					["lifecycleTracking"] = draft.LaunchBehavior.LifecycleTracking.ToString(),
					["allowLaunchFileExport"] = draft.LaunchBehavior.AllowLaunchFileExport,
					["readyMessage"] = draft.LaunchBehavior.ReadyMessage.Trim(),
					["readyLogText"] = draft.LaunchBehavior.ReadyLogText.Trim()
				},
				["supportedServerFrameworks"] =
					CreateStringArray(draft.SupportedServerFrameworks),
				["logPaths"] = CreateStringArray(draft.LogPaths)
			};

			if (draft.CopySteamRuntimeFiles)
			{
				root["postInstallActions"] = new JsonArray
				{
					new JsonObject
					{
						["type"] = TrustedPostInstallActionType.CopySteamRuntimeFiles.ToString(),
						["targetDirectory"] =
							draft.SteamRuntimeTargetDirectory.Trim()
					}
				};
			}

			IReadOnlyList<GameDefinitionTemplateDraft> templates =
				GetManagedTemplates(draft);
			if (templates.Count > 0)
			{
				IReadOnlyList<string> templateContents = templates
					.Where(template => File.Exists(template.SourcePath))
					.Select(template => File.ReadAllText(template.SourcePath))
					.ToArray();
				ManagedConfigurationInput managedInputs =
					TrustedGameDefinitionCatalog.GetTemplateManagedInputs(templateContents);
				root["configuration"] = new JsonObject
				{
					["schemaVersion"] = 1,
					["revision"] = draft.ConfigurationRevision,
					["managedInputs"] = CreateStringArray(
						Enum.GetValues<ManagedConfigurationInput>()
							.Where(input => input != ManagedConfigurationInput.None &&
								(managedInputs & input) != ManagedConfigurationInput.None)
							.Select(input => input.ToString())),
					["templates"] = new JsonArray(templates
						.Select(template => (JsonNode)new JsonObject
						{
							["revision"] = draft.ConfigurationRevision,
							["relativePath"] = template.RelativePath.Trim(),
							["templateFile"] = Path.GetFileName(template.SourcePath)
						})
						.ToArray())
				};
			}

			return root.ToJsonString(JsonOptions) + Environment.NewLine;
		}

		internal static EmbeddedGamePackage ValidateDraft(GameDefinitionDraft draft)
		{
			if (draft.RequiredLaunchFiles.Count > 0 &&
				string.IsNullOrWhiteSpace(draft.LaunchFileSetupInstructions))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.RequiredFilesInstructions"));
			}
			if (draft.NeedsConfigWarning && string.IsNullOrWhiteSpace(draft.WarningMessage))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.FirstStartWarning"));
			}

			IReadOnlyList<GameDefinitionTemplateDraft> templates =
				GetManagedTemplates(draft);
			if (UsesManagedTemplate(draft) && templates.Count == 0)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.TemplateRequired"));
			}
			if (!UsesManagedTemplate(draft) && templates.Count > 0)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.TemplateModeInvalid"));
			}

			Dictionary<string, string> templateContents =
				new(StringComparer.OrdinalIgnoreCase);
			if (UsesManagedTemplate(draft))
			{
				HashSet<string> destinations = new(StringComparer.OrdinalIgnoreCase);
				foreach (GameDefinitionTemplateDraft template in templates)
				{
					if (string.IsNullOrWhiteSpace(template.RelativePath))
						throw new InvalidDataException(
							LocalizationManager.Get(
								"GameDefinition.Authoring.TemplateLocationRequired"));
					if (string.IsNullOrWhiteSpace(template.SourcePath) ||
						!File.Exists(template.SourcePath))
					{
						throw new FileNotFoundException(
							LocalizationManager.Get(
								"GameDefinition.Authoring.TemplateMissing",
								template.SourcePath));
					}
					if (!destinations.Add(template.RelativePath.Trim()))
						throw new InvalidDataException(
							LocalizationManager.Get(
								"GameDefinition.Authoring.DuplicateDestination",
								template.RelativePath));

					string templateFile = Path.GetFileName(template.SourcePath);
					if (!templateContents.TryAdd(
						templateFile,
						File.ReadAllText(template.SourcePath)))
					{
						throw new InvalidDataException(
							LocalizationManager.Get(
								"GameDefinition.Authoring.DuplicateTemplateName",
								templateFile));
					}
				}
			}

			string json = CreateDefinitionJson(draft);

			return TrustedGameDefinitionCatalog.ParsePackage(
				json,
				$"{draft.Id}.game.json",
				(_, templateFile) => templateContents.GetValueOrDefault(templateFile));
		}

		internal static GameDefinitionSaveResult SaveDraft(
			GameDefinitionDraft draft,
			string projectDirectory)
		{
			EmbeddedGamePackage package = ValidateDraft(draft);
			string definitionsRoot = Path.GetFullPath(Path.Combine(
				projectDirectory,
				"Database",
				"GameDefinitions"));
			string definitionDirectory = ResolveInside(
				definitionsRoot,
				draft.Id.Trim());
			Directory.CreateDirectory(definitionDirectory);
			string definitionPath = ResolveInside(
				definitionDirectory,
				draft.Id.Trim() + ".game.json");
			ValidateCatalogPlacement(package, definitionPath);
			string json = CreateDefinitionJson(draft);
			BackupIfPresent(definitionPath);
			WriteAtomically(definitionPath, json);

			List<string> templatePaths = [];
			if (UsesManagedTemplate(draft))
			{
				string templatesDirectory = ResolveInside(
					definitionDirectory,
					"Templates");
				Directory.CreateDirectory(templatesDirectory);
				foreach (GameDefinitionTemplateDraft template in GetManagedTemplates(draft))
				{
					string templatePath = ResolveInside(
						templatesDirectory,
						Path.GetFileName(template.SourcePath));
					if (!string.Equals(
						Path.GetFullPath(template.SourcePath),
						templatePath,
						StringComparison.OrdinalIgnoreCase))
					{
						BackupIfPresent(templatePath);
						File.Copy(template.SourcePath, templatePath, true);
					}
					templatePaths.Add(templatePath);
				}
			}

			return new GameDefinitionSaveResult(
				definitionPath,
				templatePaths,
				json);
		}

		private static bool UsesManagedTemplate(GameDefinitionDraft draft) =>
			draft.ConfigFileCreation is
				ConfigFileCreationMode.SynixTemplate or
				ConfigFileCreationMode.GameGenerated;

		private static IReadOnlyList<GameDefinitionTemplateDraft> GetManagedTemplates(
			GameDefinitionDraft draft)
		{
			List<GameDefinitionTemplateDraft> templates = [];
			if (!string.IsNullOrWhiteSpace(draft.RelativeConfigPath) ||
				!string.IsNullOrWhiteSpace(draft.TemplateSourcePath))
			{
				templates.Add(new GameDefinitionTemplateDraft(
					draft.RelativeConfigPath,
					draft.TemplateSourcePath));
			}
			templates.AddRange(draft.AdditionalTemplates);
			return templates;
		}

		private static JsonArray CreateStringArray(IEnumerable<string> values) =>
			new(values
				.Select(value => value.Trim())
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Select(value => JsonValue.Create(value))
				.ToArray());

		private static void ValidateCatalogPlacement(
			EmbeddedGamePackage package,
			string definitionPath)
		{
			IReadOnlyList<EmbeddedGamePackage> others = TrustedGameDefinitionCatalog.Packages
				.Where(candidate => !string.Equals(
					candidate.Definition.DefinitionId,
					package.Definition.DefinitionId,
					StringComparison.OrdinalIgnoreCase))
				.ToArray();
			if (others.Any(candidate =>
				candidate.Definition.CatalogOrder == package.Definition.CatalogOrder))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.CatalogOrderUsed",
						package.Definition.CatalogOrder));
			}

			HashSet<string> otherNames = new(StringComparer.OrdinalIgnoreCase);
			foreach (EmbeddedGamePackage other in others)
			{
				otherNames.Add(other.Definition.Game);
				otherNames.UnionWith(other.Definition.Aliases);
			}
			IEnumerable<string> newNames =
				[package.Definition.Game, .. package.Definition.Aliases];
			string? duplicateName = newNames.FirstOrDefault(otherNames.Contains);
			if (duplicateName != null)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.NameUsed",
						duplicateName));
			}

			EmbeddedGamePackage? existing = TrustedGameDefinitionCatalog.Packages
				.FirstOrDefault(candidate => string.Equals(
					candidate.Definition.DefinitionId,
					package.Definition.DefinitionId,
					StringComparison.OrdinalIgnoreCase));
			int existingDefinitionRevision = existing?.Definition.DefinitionRevision ?? 0;
			int existingConfigurationRevision = existing?.Configuration?.Revision ?? 0;
			ReadSourceRevisions(
				definitionPath,
				ref existingDefinitionRevision,
				ref existingConfigurationRevision);
			if (existingDefinitionRevision > 0 &&
				package.Definition.DefinitionRevision <= existingDefinitionRevision)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.IncreaseDefinitionRevision",
						existingDefinitionRevision));
			}
			if (existingConfigurationRevision > 0 &&
				package.Configuration != null &&
				package.Configuration.Revision <= existingConfigurationRevision)
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.IncreaseConfigurationRevision",
						existingConfigurationRevision));
			}
		}

		private static void ReadSourceRevisions(
			string definitionPath,
			ref int definitionRevision,
			ref int configurationRevision)
		{
			if (!File.Exists(definitionPath))
				return;
			using JsonDocument document = JsonDocument.Parse(File.ReadAllText(definitionPath));
			JsonElement root = document.RootElement;
			if (root.TryGetProperty("definitionRevision", out JsonElement definitionElement) &&
				definitionElement.TryGetInt32(out int sourceDefinitionRevision))
			{
				definitionRevision = Math.Max(definitionRevision, sourceDefinitionRevision);
			}
			if (root.TryGetProperty("configuration", out JsonElement configurationElement) &&
				configurationElement.ValueKind == JsonValueKind.Object &&
				configurationElement.TryGetProperty("revision", out JsonElement revisionElement) &&
				revisionElement.TryGetInt32(out int sourceConfigurationRevision))
			{
				configurationRevision = Math.Max(
					configurationRevision,
					sourceConfigurationRevision);
			}
		}

		private static string ResolveInside(string root, string relativePath)
		{
			string fullRoot = Path.GetFullPath(root)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
			if (!fullPath.StartsWith(
				fullRoot + Path.DirectorySeparatorChar,
				StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException(
					LocalizationManager.Get(
						"GameDefinition.Authoring.PathOutsideProject"));
			}
			return fullPath;
		}

		private static void BackupIfPresent(string path)
		{
			if (!File.Exists(path))
				return;
			string backup = $"{path}.{DateTime.Now:yyyyMMdd-HHmmss}.bak";
			File.Copy(path, backup, false);
		}

		private static void WriteAtomically(string path, string content)
		{
			string temporary = path + $".{Guid.NewGuid():N}.tmp";
			try
			{
				File.WriteAllText(temporary, content, new UTF8Encoding(false, true));
				File.Move(temporary, path, true);
			}
			finally
			{
				if (File.Exists(temporary))
					File.Delete(temporary);
			}
		}
	}
}
