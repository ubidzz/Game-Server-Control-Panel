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
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.Database.GameDefinitions;
using Synix_Control_Panel.SynixEngine;
using System.Reflection;

namespace Synix_Control_Panel.SynixApp.ServerHandler
{
	public static class GameFix
	{
		private static readonly IReadOnlyDictionary<string, ConfigurationDefinition> ConfigurationIndex =
			CreateConfigurationIndex();

		public static bool ManualConfigWasCreated { get; set; }

		internal static bool ManagedConfigurationsEnabled =>
			ShouldUseManagedConfigurations(
				Core.IsOfficialRelease,
				Properties.Settings.Default.DisablePremadeConfigurationsForDevelopment);

		internal static bool ShouldUseManagedConfigurations(
			bool isOfficialRelease,
			bool disabledForDevelopment)
		{
			return isOfficialRelease || !disabledForDevelopment;
		}

		internal static bool TryGetConfiguration(
			string gameName,
			out ConfigurationDefinition? definition)
		{
			return ConfigurationIndex.TryGetValue(
				GameDatabase.GetCanonicalGameName(gameName),
				out definition);
		}

		internal static ConfigFileCreationMode GetConfigFileCreationMode(
			string gameName)
		{
			return GameDatabase.GetGame(gameName)?.ConfigFileCreation ??
				ConfigFileCreationMode.Unknown;
		}

		internal static string ResolveGameModeValue(
			GameInfo? game,
			string selectedMode)
		{
			if (game == null)
				return selectedMode;
			if (string.Equals(selectedMode, "PVP", StringComparison.OrdinalIgnoreCase))
				return game.PvpValue;
			if (string.Equals(selectedMode, "PVE", StringComparison.OrdinalIgnoreCase))
				return game.PveValue;
			return selectedMode;
		}

		internal static string ResolveBooleanValue(
			GameInfo? game,
			bool enabled)
		{
			if (game == null)
				return enabled ? "true" : "false";
			return enabled ? game.BooleanTrueValue : game.BooleanFalseValue;
		}

		internal static string ResolveCrossplayValue(
			GameInfo? game,
			bool enabled)
		{
			if (game == null)
				return enabled ? "true" : "false";
			return enabled ? game.CrossplayEnabledValue : game.CrossplayDisabledValue;
		}

		internal static bool CanResetManagedConfiguration(GameServer server)
		{
			return server != null &&
				GetConfigFileCreationMode(server.Game) is
					ConfigFileCreationMode.SynixTemplate or
					ConfigFileCreationMode.GameGenerated &&
				TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) &&
				definition?.SupportsFullReset == true;
		}

		internal static bool CanManuallyResetManagedConfiguration(
			GameServer server,
			bool serverIsBusy)
		{
			return !serverIsBusy && CanResetManagedConfiguration(server);
		}

		internal static ConfigurationBackupSnapshot? BackupManagedConfiguration(
			GameServer server,
			string reason)
		{
			return TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) &&
				definition != null
				? ConfigurationBackupManager.CreateSnapshot(server, definition, reason)
				: null;
		}

		internal static bool HasManagedConfigurationBackup(GameServer server)
		{
			return TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) &&
				definition != null &&
				ConfigurationBackupManager.HasSnapshot(server, definition);
		}

		internal static ConfigurationRestoreResult RestorePreviousManagedConfiguration(
			GameServer server)
		{
			return TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) &&
				definition != null
				? ConfigurationBackupManager.RestoreLatest(server, definition)
				: new ConfigurationRestoreResult(
					false,
					0,
					LocalizationManager.Get(
						"Configuration.Restore.ManagedDefinitionMissing"));
		}

		internal static bool NeedsManagedConfigurationRepair(GameServer server)
		{
			if (server.PreserveImportedConfiguration)
				return false;

			if (GetConfigFileCreationMode(server.Game) is
					ConfigFileCreationMode.Unknown or
					ConfigFileCreationMode.LaunchArgumentsOnly ||
				!TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) ||
				definition?.SupportsFullReset != true)
			{
				return false;
			}

			try
			{
				ConfigurationContext context = new(
					server,
					new SynixServerPasswords("template", "template", "template"),
					Core.Instance.GetSafeName(server.ServerName),
					"0.0.0.0",
					"0.0.0.0");
				return definition.NeedsStructuralRepair(context);
			}
			catch
			{
				return true;
			}
		}

		internal static async Task<ConfigurationValidationReport>
			ValidateManagedConfiguration(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			List<ConfigurationValidationItem> items = [];
			ConfigFileCreationMode creationMode =
				GetConfigFileCreationMode(server.Game);

			if (creationMode == ConfigFileCreationMode.Unknown)
			{
				items.Add(new ConfigurationValidationItem(
					ConfigurationValidationState.Warning,
					LocalizationManager.Get("Configuration.Check.Behavior"),
					LocalizationManager.Get(
						"Configuration.Check.Behavior.NotVerified")));
				return new ConfigurationValidationReport(
					server.Game,
					server.ManagedConfigurationVersion,
					0,
					false,
					items);
			}

			if (!TryGetConfiguration(
				server.Game,
				out ConfigurationDefinition? definition) ||
				definition == null)
			{
				ConfigurationValidationState state =
					creationMode == ConfigFileCreationMode.LaunchArgumentsOnly
						? ConfigurationValidationState.Passed
						: ConfigurationValidationState.Failed;
				items.Add(new ConfigurationValidationItem(
					state,
					creationMode == ConfigFileCreationMode.LaunchArgumentsOnly
						? LocalizationManager.Get(
							"Configuration.Check.LaunchArguments")
						: LocalizationManager.Get(
							"Configuration.Check.ManagedDefinition"),
					creationMode == ConfigFileCreationMode.LaunchArgumentsOnly
						? LocalizationManager.Get(
							"Configuration.Check.ValuesThroughArguments")
						: LocalizationManager.Get(
							"Configuration.Check.ManagedDefinition.Missing")));
				return new ConfigurationValidationReport(
					server.Game,
					server.ManagedConfigurationVersion,
					0,
					false,
					items);
			}

			if (!ManagedConfigurationsEnabled)
			{
				// This preference controls automatic writes, not the health of the
				// files. An explicit repair cannot (and must not) change it.
				items.Add(new ConfigurationValidationItem(
					ConfigurationValidationState.Information,
					LocalizationManager.Get(
						"Configuration.Check.DevelopmentSetting"),
					LocalizationManager.Get(
						"Configuration.Check.DevelopmentSetting.Disabled")));
			}

			if (server.ManagedConfigurationVersion == definition.SchemaVersion)
			{
				items.Add(new ConfigurationValidationItem(
					ConfigurationValidationState.Passed,
					LocalizationManager.Get("Configuration.Check.TemplateRevision"),
					LocalizationManager.Get(
						"Configuration.Check.TemplateRevision.Current",
						definition.SchemaVersion)));
			}
			else if (server.ManagedConfigurationVersion < definition.SchemaVersion)
			{
				items.Add(new ConfigurationValidationItem(
					ConfigurationValidationState.Warning,
					LocalizationManager.Get("Configuration.Check.TemplateRevision"),
					LocalizationManager.Get(
						"Configuration.Check.TemplateRevision.Outdated",
						server.ManagedConfigurationVersion,
						definition.SchemaVersion)));
			}
			else
			{
				items.Add(new ConfigurationValidationItem(
					ConfigurationValidationState.Warning,
					LocalizationManager.Get("Configuration.Check.TemplateRevision"),
					LocalizationManager.Get(
						"Configuration.Check.TemplateRevision.Newer",
						server.ManagedConfigurationVersion,
						definition.SchemaVersion)));
			}

			string localIp = string.Empty;
			string publicIp = string.Empty;
			if (definition.RequiresNetworkAddresses)
			{
				try
				{
					localIp = await Core.Instance.GetLocalIP();
					publicIp = await Core.Instance.GetPublicIP();
				}
				catch (Exception exception)
				{
					items.Add(new ConfigurationValidationItem(
						ConfigurationValidationState.Failed,
						LocalizationManager.Get(
							"Configuration.Check.NetworkValues"),
						LocalizationManager.Get(
							"Configuration.Check.NetworkValues.Failed",
							exception.Message)));
					return new ConfigurationValidationReport(
						server.Game,
						server.ManagedConfigurationVersion,
						definition.SchemaVersion,
						definition.SupportsFullReset,
						items);
				}
			}

			SynixServerPasswords passwords = default;
			if (definition.UsesConfigurationFile)
			{
				try
				{
					passwords = Core.RevealServerPasswords(server);
				}
				catch (SynixPasswordProtectionException)
				{
					items.Add(new ConfigurationValidationItem(
						ConfigurationValidationState.Failed,
						LocalizationManager.Get(
							"Configuration.Check.ProtectedPasswords"),
						LocalizationManager.Get(
							"Configuration.Check.ProtectedPasswords.UnlockFailed")));
					return new ConfigurationValidationReport(
						server.Game,
						server.ManagedConfigurationVersion,
						definition.SchemaVersion,
						definition.SupportsFullReset,
						items);
				}
			}

			ConfigurationContext context = new(
				server,
				passwords,
				Core.Instance.GetSafeName(server.ServerName),
				localIp,
				publicIp);
			items.AddRange(definition.Validate(context));
			items.Add(new ConfigurationValidationItem(
				ConfigurationValidationState.Passed,
				LocalizationManager.Get("Configuration.Check.FixConfig"),
				definition.SupportsFullReset
					? LocalizationManager.Get(
						"Configuration.Check.FixConfig.Available")
					: LocalizationManager.Get(
						"Configuration.Check.FixConfig.Unavailable")));

			return new ConfigurationValidationReport(
				server.Game,
				server.ManagedConfigurationVersion,
				definition.SchemaVersion,
				definition.SupportsFullReset,
				items);
		}

		internal static ManagedConfigurationInput GetManagedConfigurationInputs(
			string gameName)
		{
			if (GetConfigFileCreationMode(gameName) is
					ConfigFileCreationMode.Unknown or
					ConfigFileCreationMode.LaunchArgumentsOnly ||
				!TryGetConfiguration(gameName, out ConfigurationDefinition? definition) ||
				definition == null)
			{
				return ManagedConfigurationInput.None;
			}

			return definition.SupportedInputs;
		}

		internal static GameManagementCapability GetManagementCapabilities(
			GameInfo? game)
		{
			if (game == null)
				return GameManagementCapability.None;

			string arguments = game.RequiredArgs ?? string.Empty;
			string rconSyntax = game.RconSyntax ?? string.Empty;
			ManagedConfigurationInput configuration =
				GetManagedConfigurationInputs(game.Game);
			GameManagementCapability capabilities = GameManagementCapability.None;

			void Include(
				bool argumentUsesValue,
				ManagedConfigurationInput configurationInput,
				GameManagementCapability capability)
			{
				if (argumentUsesValue ||
					(configuration & configurationInput) != ManagedConfigurationInput.None)
				{
					capabilities |= capability;
				}
			}

			Include(
				arguments.Contains("{pass}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.ServerPassword,
				GameManagementCapability.ServerPassword);
			Include(
				arguments.Contains("{adminpass}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.AdminPassword,
				GameManagementCapability.AdminPassword);
			Include(
				arguments.Contains("{seed}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.WorldSeed,
				GameManagementCapability.WorldSeed);
			Include(
				arguments.Contains("{mode}", StringComparison.OrdinalIgnoreCase) ||
				game.GameModes.Count > 0,
				ManagedConfigurationInput.GameMode,
				GameManagementCapability.GameMode);
			Include(
				arguments.Contains("{MaxPlayers}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.MaxPlayers,
				GameManagementCapability.MaxPlayers);
			Include(
				arguments.Contains("{query}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.QueryPort,
				GameManagementCapability.QueryPort);
			Include(
				arguments.Contains("{map}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.WorldName,
				GameManagementCapability.WorldName);
			Include(
				arguments.Contains("{rcon}", StringComparison.OrdinalIgnoreCase) ||
				rconSyntax.Contains("{rcon_port}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.Rcon,
				GameManagementCapability.Rcon);
			Include(
				arguments.Contains("{world_size}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.WorldSize,
				GameManagementCapability.WorldSize);
			Include(
				arguments.Contains("{port}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.Port,
				GameManagementCapability.Port);
			Include(
				arguments.Contains("{app_port}", StringComparison.OrdinalIgnoreCase),
				ManagedConfigurationInput.AppPort,
				GameManagementCapability.AppPort);
			Include(
				arguments.Contains("{crossplay}", StringComparison.Ordinal),
				ManagedConfigurationInput.Crossplay,
				GameManagementCapability.Crossplay);
			Include(
				false,
				ManagedConfigurationInput.InviteCode,
				GameManagementCapability.InviteCode);

			if (arguments.Contains("{ram}", StringComparison.OrdinalIgnoreCase))
				capabilities |= GameManagementCapability.Ram;
			if (GameCapabilityResolver.UsesMinecraftConfiguration(game))
				capabilities |= GameManagementCapability.GameVersion;

			return capabilities;
		}

		internal static bool NeedsManagedConfiguration(GameServer server)
		{
			if (server.PreserveImportedConfiguration)
			{
				return false;
			}

			if (!ManagedConfigurationsEnabled)
			{
				return false;
			}

			ConfigFileCreationMode creationMode =
				GetConfigFileCreationMode(server.Game);
			if (creationMode is ConfigFileCreationMode.Unknown or
				ConfigFileCreationMode.LaunchArgumentsOnly)
			{
				return false;
			}

			if (!TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) ||
				definition == null)
			{
				return false;
			}

			if (creationMode == ConfigFileCreationMode.GameGenerated)
			{
				try
				{
					if (!definition.ConfigurationFileExists(server))
					{
						return false;
					}
				}
				catch
				{
					return false;
				}
			}

			if (server.ManagedConfigurationVersion < definition.SchemaVersion)
			{
				return true;
			}

			try
			{
				return !definition.ConfigurationFileExists(server);
			}
			catch
			{
				return true;
			}
		}

		internal static async Task<ConfigurationApplyResult> ApplyManagedConfiguration(
			GameServer server)
		{
			if (!ManagedConfigurationsEnabled)
			{
				return new ConfigurationApplyResult(
					true,
					true,
					false,
					false,
					LocalizationManager.Get(
						"Configuration.Apply.PremadeDisabled"));
			}

			ConfigFileCreationMode creationMode =
				GetConfigFileCreationMode(server.Game);
			if (creationMode == ConfigFileCreationMode.Unknown)
			{
				return new ConfigurationApplyResult(
					true,
					true,
					false,
					false,
					LocalizationManager.Get(
						"Configuration.Check.Behavior.NotVerified"));
			}

			if (creationMode == ConfigFileCreationMode.LaunchArgumentsOnly)
			{
				return ConfigurationApplyResult.ArgumentsOnly();
			}

			if (!TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) ||
				definition == null)
			{
				return new ConfigurationApplyResult(
					true,
					true,
					false,
					false,
					LocalizationManager.Get(
						"Configuration.Apply.ManagedDefinitionMissing"));
			}

			string localIp = string.Empty;
			string publicIp = string.Empty;
			bool needsNetworkAddresses = definition.RequiresNetworkAddresses;
			if (needsNetworkAddresses)
			{
				try
				{
					needsNetworkAddresses = !definition.ConfigurationFileExists(server);
				}
				catch (Exception suppressedException)
				{
					Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
				}
			}

			if (needsNetworkAddresses)
			{
				localIp = await Core.Instance.GetLocalIP();
				publicIp = await Core.Instance.GetPublicIP();
			}

			SynixServerPasswords passwords = default;
			if (definition.UsesConfigurationFile)
			{
				try
				{
					passwords = Core.RevealServerPasswords(server);
				}
				catch (SynixPasswordProtectionException)
				{
					return ConfigurationApplyResult.Failure(
						LocalizationManager.Get(
							"Configuration.Apply.PasswordUnlockFailed"));
				}
			}

			ConfigurationContext context = new(
				server,
				passwords,
				Core.Instance.GetSafeName(server.ServerName),
				localIp,
				publicIp);
			ConfigurationBackupSnapshot? snapshot =
				ConfigurationBackupManager.CreateSnapshot(
					server,
					definition,
					LocalizationManager.Get(
						"Configuration.Backup.BeforeApply"));
			ConfigurationApplyResult result = definition.Apply(context);
			if (!result.Changed)
				ConfigurationBackupManager.Discard(snapshot);
			if (result.Succeeded && result.Complete)
			{
				server.ManagedConfigurationVersion = definition.SchemaVersion;
			}

			return result;
		}

		internal static async Task<ConfigurationApplyResult?> ApplyFirstGeneratedConfiguration(
			GameServer server)
		{
			if (GetConfigFileCreationMode(server.Game) != ConfigFileCreationMode.GameGenerated ||
				!NeedsManagedConfiguration(server))
			{
				return null;
			}

			return await ApplyManagedConfiguration(server);
		}

		internal static async Task<ConfigurationApplyResult> ResetManagedConfiguration(
			GameServer server)
		{
			if (GetConfigFileCreationMode(server.Game) is
					ConfigFileCreationMode.Unknown or
					ConfigFileCreationMode.LaunchArgumentsOnly ||
				!TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) ||
				definition == null ||
				!definition.SupportsFullReset)
			{
				return ConfigurationApplyResult.Failure(
					LocalizationManager.Get(
						"Configuration.Apply.ResetTemplateMissing.Generic"));
			}

			string localIp = string.Empty;
			string publicIp = string.Empty;
			if (definition.RequiresNetworkAddresses)
			{
				try
				{
					localIp = await Core.Instance.GetLocalIP();
					publicIp = await Core.Instance.GetPublicIP();
				}
				catch (Exception exception)
				{
					return ConfigurationApplyResult.Failure(
						LocalizationManager.Get(
							"Configuration.Apply.NetworkFailed",
							exception.Message));
				}
			}

			SynixServerPasswords passwords = default;
			if (definition.UsesConfigurationFile)
			{
				try
				{
					passwords = Core.RevealServerPasswords(server);
				}
				catch (SynixPasswordProtectionException)
				{
					return ConfigurationApplyResult.Failure(
						LocalizationManager.Get(
							"Configuration.Apply.ResetPasswordUnlockFailed"));
				}
			}

			ConfigurationContext context = new(
				server,
				passwords,
				Core.Instance.GetSafeName(server.ServerName),
				localIp,
				publicIp);
			ConfigurationBackupSnapshot? snapshot =
				ConfigurationBackupManager.CreateSnapshot(
					server,
					definition,
					LocalizationManager.Get(
						"Configuration.Backup.BeforeReset"));
			ConfigurationApplyResult result = definition.ResetToTemplate(context);
			if (!result.Changed)
				ConfigurationBackupManager.Discard(snapshot);
			if (result.Succeeded && result.Complete)
			{
				server.ManagedConfigurationVersion = definition.SchemaVersion;
			}

			return result;
		}

		public static async Task<bool> PostInstall(GameServer server)
		{
			if (string.IsNullOrWhiteSpace(server.InstallPath) ||
				!Directory.Exists(server.InstallPath))
			{
				return false;
			}

			bool applied = false;
			if (GameCapabilityResolver.UsesMinecraftConfiguration(server))
			{
				ManualConfigWasCreated = true;
				applied = true;
			}

			try
			{
				TrustedPostInstallExecutionResult postInstall =
					TrustedPostInstallExecutor.Execute(server);
				foreach (string message in postInstall.Messages)
				{
					Color color = postInstall.Succeeded
						? message.Contains("not present", StringComparison.OrdinalIgnoreCase)
							? Color.Orange
							: Color.LightGreen
						: Color.Red;
					Core.Instance.LogLocalized(
						"Configuration.PostInstall.Activity",
						color,
						arguments: [LocalizationManager.TranslateRuntimeText(message)]);
				}
				if (!postInstall.Succeeded)
				{
					Core.Instance.LogLocalized(
						"Configuration.PostInstall.Error",
						Color.Red);
				}
				applied |= postInstall.Changed;

				ConfigFileCreationMode configurationMode =
					GetConfigFileCreationMode(server.Game);
				if ((configurationMode is
						ConfigFileCreationMode.SynixTemplate or
						ConfigFileCreationMode.GameGenerated) &&
					TryGetConfiguration(server.Game, out ConfigurationDefinition? definition) &&
					definition != null)
				{
					bool configurationAvailable =
						configurationMode == ConfigFileCreationMode.SynixTemplate;
					if (!configurationAvailable)
					{
						configurationAvailable = definition.ConfigurationFileExists(server);
					}

					if (configurationAvailable)
					{
						ConfigurationApplyResult result = await ApplyManagedConfiguration(server);
						if (!result.Succeeded)
						{
							Core.Instance.LogLocalized(
								"Configuration.Activity.Error",
								Color.Red,
								arguments: [result.Message]);
						}
						else
						{
							if (result.Created)
							{
								ManualConfigWasCreated = true;
							}

							if (result.Changed)
							{
								applied = true;
							}

							if (!result.Complete)
							{
								Core.Instance.LogLocalized(
									"Configuration.Activity.Warning",
									Color.Orange,
									arguments: [result.Message]);
							}
						}
					}
				}
			}
			catch (Exception exception)
			{
				ApplicationLogService.WriteSuppressedException(
					exception,
					"ApplyManagedConfigurationPostInstall");
				return false;
			}

			return applied;
		}

		private static IReadOnlyDictionary<string, ConfigurationDefinition> CreateConfigurationIndex()
		{
			Dictionary<string, ConfigurationDefinition> index =
				new(StringComparer.OrdinalIgnoreCase);
			foreach (ConfigurationDefinition definition in DiscoverCompiledConfigurations())
			{
				AddConfiguration(index, definition);
			}

			foreach (EmbeddedGamePackage package in TrustedGameDefinitionCatalog.Packages)
			{
				if (package.Configuration == null ||
					index.ContainsKey(package.Definition.Game))
					continue;

				AddConfiguration(
					index,
					new EmbeddedTemplateConfigurationDefinition(
						package.Definition.Game,
						package.Configuration));
			}

			return index;
		}

		private static IEnumerable<ConfigurationDefinition> DiscoverCompiledConfigurations()
		{
			Type definitionType = typeof(ConfigurationDefinition);
			foreach (Type type in typeof(GameFix).Assembly.GetTypes()
				.Where(type =>
					type != definitionType &&
					!type.IsAbstract &&
					definitionType.IsAssignableFrom(type))
				.OrderBy(type => type.FullName, StringComparer.Ordinal))
			{
				ConstructorInfo? constructor = type.GetConstructor(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
					binder: null,
					Type.EmptyTypes,
					modifiers: null);
				if (constructor?.Invoke(null) is ConfigurationDefinition definition)
					yield return definition;
			}
		}

		private static void AddConfiguration(
			Dictionary<string, ConfigurationDefinition> index,
			ConfigurationDefinition definition)
		{
			if (!index.TryAdd(definition.GameName, definition))
				throw new InvalidDataException(
					LocalizationManager.Get(
						"Configuration.Definition.Duplicate",
						definition.GameName));

			foreach (string alias in definition.Aliases)
			{
				if (!index.TryAdd(alias, definition))
					throw new InvalidDataException(
						LocalizationManager.Get(
							"Configuration.Definition.DuplicateAlias",
							alias));
			}
		}

	}
}
