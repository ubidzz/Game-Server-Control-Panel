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
using System.Text.Json;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixApp.Database.GameConfigurations
{
	internal readonly record struct ConfigurationTemplate(
		string RelativePath,
		string Content,
		int Revision = 1);

	internal abstract class TemplateConfigurationDefinition : ConfigurationDefinition
	{
		protected abstract IReadOnlyList<ConfigurationTemplate> Templates { get; }
		public override bool SupportsFullReset => true;
		public override ConfigFormat Format =>
			GameDatabase.GetGame(GameName)?.Format ?? ConfigFormat.StandardINI;

		public override string RelativePath => Templates.Count > 0
			? Templates[0].RelativePath
			: string.Empty;

		public override ManagedConfigurationInput SupportedInputs =>
			GetSupportedInputs();

		public override IReadOnlyList<ConfigurationValidationItem> Validate(
			ConfigurationContext context)
		{
			List<ConfigurationValidationItem> items = [];
			foreach (ConfigurationTemplate template in Templates)
			{
				string fullPath = ResolveFullPath(context.Server, template.RelativePath);
				if (!File.Exists(fullPath))
				{
					items.Add(new ConfigurationValidationItem(
						ConfigurationValidationState.Failed,
						template.RelativePath,
						LocalizationManager.Get(
							"Configuration.Check.RequiredFile.Missing")));
					continue;
				}

				try
				{
					string expandedTemplate = ExpandTemplate(template.Content, context);
					bool structureMatches = ConfigHandler.HasRequiredStructure(
						fullPath,
						expandedTemplate,
						Format);
					items.Add(new ConfigurationValidationItem(
						structureMatches
							? ConfigurationValidationState.Passed
							: ConfigurationValidationState.Failed,
						LocalizationManager.Get(
							"Configuration.Check.FileStructure",
							template.RelativePath),
						structureMatches
							? LocalizationManager.Get(
								"Configuration.Check.TemplateStructure.Present")
							: LocalizationManager.Get(
								"Configuration.Check.TemplateStructure.Invalid")));

					List<ConfigLine> desiredValues =
						ConfigHandler.LoadConfigText(expandedTemplate, Format);
					Dictionary<string, ConfigLine> probeValues =
						ConfigHandler.LoadConfigText(
							ExpandTemplate(template.Content, context, true),
							Format)
						.ToDictionary(value => value.Id, StringComparer.Ordinal);
					List<ConfigLine> managedValues = desiredValues
						.Where(value =>
							probeValues.TryGetValue(value.Id, out ConfigLine? probe) &&
							!TemplateValuesMatch(value, probe))
						.ToList();
					List<ConfigLine> existingValues = ConfigHandler.LoadConfig(fullPath, Format);

					foreach (ConfigLine desired in managedValues)
					{
						string settingName = string.IsNullOrWhiteSpace(desired.Path)
							? desired.Key
							: desired.Path;
						string displayName = $"{template.RelativePath}: {settingName}";
						List<ConfigLine> matches = existingValues.Where(existing =>
							string.Equals(existing.Id, desired.Id, StringComparison.Ordinal) &&
							string.Equals(existing.Key, desired.Key, StringComparison.Ordinal) &&
							string.Equals(existing.Path, desired.Path, StringComparison.Ordinal) &&
							existing.Type == desired.Type)
							.ToList();

						if (matches.Count == 0)
						{
							items.Add(new ConfigurationValidationItem(
								ConfigurationValidationState.Failed,
								displayName,
								LocalizationManager.Get(
									"Configuration.Check.ManagedTag.MissingOrType")));
							continue;
						}

						if (matches.Count > 1)
						{
							items.Add(new ConfigurationValidationItem(
								ConfigurationValidationState.Failed,
								displayName,
								LocalizationManager.Get(
									"Configuration.Check.ManagedTag.Duplicate")));
							continue;
						}

						bool valueMatches = TemplateValuesMatch(matches[0], desired);
						items.Add(new ConfigurationValidationItem(
							valueMatches
								? ConfigurationValidationState.Passed
								: ConfigurationValidationState.Failed,
							displayName,
							valueMatches
								? LocalizationManager.Get(
									"Configuration.Check.Value.Matches")
								: LocalizationManager.Get(
									"Configuration.Check.Value.Differs")));
					}

					if (managedValues.Count == 0)
					{
						items.Add(new ConfigurationValidationItem(
							ConfigurationValidationState.Passed,
							LocalizationManager.Get(
								"Configuration.Check.FileManagedValues",
								template.RelativePath),
							LocalizationManager.Get(
								"Configuration.Check.ManagedValues.NotReplaced")));
					}
				}
				catch (Exception exception)
				{
					items.Add(new ConfigurationValidationItem(
						ConfigurationValidationState.Failed,
						template.RelativePath,
						LocalizationManager.Get(
							"Configuration.Check.FileInspect.Failed",
							exception.Message)));
				}
			}

			return items;
		}

		public override ConfigurationApplyResult Apply(ConfigurationContext context)
		{
			try
			{
				bool created = false;
				bool changed = false;
				List<string> missing = [];
				List<string> upgradeBackups = [];
				foreach (ConfigurationTemplate template in Templates)
				{
					string fullPath = ResolveFullPath(context.Server, template.RelativePath);
					string expandedTemplate = ExpandTemplate(template.Content, context);
					if (!File.Exists(fullPath))
					{
						WriteNewFile(fullPath, expandedTemplate);
						created = true;
						changed = true;
						continue;
					}

					if (context.Server.ManagedConfigurationVersion < SchemaVersion)
					{
						string? backup = CreateUpgradeBackup(
							fullPath,
							template.Revision);
						if (backup != null)
							upgradeBackups.Add(backup);
					}

					(bool fileChanged, IReadOnlyList<string> fileMissing) =
						UpdateManagedValues(
							fullPath,
							template.Content,
							expandedTemplate,
							context);
					changed |= fileChanged;
					missing.AddRange(fileMissing.Select(setting =>
						$"{template.RelativePath}: {setting}"));
				}

				string backupMessage = upgradeBackups.Count > 0
					? LocalizationManager.Get(
						"Configuration.Apply.UpgradeBackupsPreserved",
						upgradeBackups.Count)
					: string.Empty;
				if (missing.Count > 0)
				{
					return new ConfigurationApplyResult(
						true,
						false,
						changed,
						created,
						LocalizationManager.Get(
							"Configuration.Apply.ManagedSettingsMissing.Multiple",
							string.Join(", ", missing)) +
						backupMessage);
				}

				return new ConfigurationApplyResult(
					true,
					true,
					changed,
					created,
					(created
						? LocalizationManager.Get(
							"Configuration.Apply.RequiredFilesCreated",
							GameName)
						: changed
							? LocalizationManager.Get(
								"Configuration.Apply.ManagedSettingsUpdated",
								GameName)
							: LocalizationManager.Get(
								"Configuration.Apply.FilesCurrent",
								GameName)) +
					backupMessage);
			}
			catch (Exception ex)
			{
				return ConfigurationApplyResult.Failure(
					LocalizationManager.Get(
						"Configuration.Apply.CreateFailed",
						GameName,
						ex.Message));
			}
		}

		public override bool NeedsStructuralRepair(ConfigurationContext context)
		{
			foreach (ConfigurationTemplate template in Templates)
			{
				string path = ResolveFullPath(context.Server, template.RelativePath);
				string content = ExpandTemplate(template.Content, context);
				if (!ConfigHandler.HasRequiredStructure(path, content, Format))
				{
					return true;
				}
			}

			return false;
		}

		public override ConfigurationApplyResult ResetToTemplate(
			ConfigurationContext context)
		{
			try
			{
				// Expand and validate every template before backing up or replacing files.
				return ReplaceWithTemplates(
					context,
					Templates.Select(template => new ResetTemplate(
						template.RelativePath,
						ExpandTemplate(template.Content, context),
						Format))
						.ToArray());
			}
			catch (InvalidDataException exception)
			{
				return ConfigurationApplyResult.Failure(exception.Message);
			}
		}

		public override bool ConfigurationFileExists(GameServer server)
		{
			return Templates.All(template =>
				File.Exists(ResolveFullPath(server, template.RelativePath)));
		}

		internal override IReadOnlyList<string> ResolveConfigurationPaths(
			GameServer server)
		{
			return Templates
				.Select(template => ResolveFullPath(server, template.RelativePath))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		protected string ExpandTemplate(
			string template,
			ConfigurationContext context,
			bool useProbeValues = false)
		{
			GameServer server = context.Server;
			string ProbeText(string value, string name) => useProbeValues
				? $"{value}__synix_probe_{name}__"
				: value;
			string TextValue(string value, string name)
			{
				string expanded = ProbeText(value, name);
				return Format == ConfigFormat.JSON
					? JsonEncodedText.Encode(expanded).ToString()
					: expanded;
			}
			int ProbeNumber(int value) => useProbeValues
				? value == int.MaxValue ? value - 1 : value + 1
				: value;
			bool ProbeBoolean(bool value) => useProbeValues ? !value : value;
			GameInfo? game = GameDatabase.GetGame(GameName);
			string BooleanValue(bool value) =>
				GameFix.ResolveBooleanValue(game, ProbeBoolean(value));
			string GameModeValue() => GameFix.ResolveGameModeValue(
				game,
				RequireSingleLine(server.GameMode, "GameMode"));
			string WorldSeedValue()
			{
				string value = RequireSingleLine(server.WorldSeed, "WorldSeed");
				if (Format != ConfigFormat.YAML || !template.Contains("{WorldSeed}", StringComparison.Ordinal))
					return TextValue(value, "world_seed");
				if (!GameServerInputValidator.TryParseRequiredNumericWorldSeed(
					GameName, value, out int seed, out string seedError))
					throw new InvalidDataException(seedError);
				return ProbeNumber(seed).ToString(CultureInfo.InvariantCulture);
			}

			// Replace only placeholders in the trusted template, never text introduced
			// by a user's name/password that happens to contain another placeholder.
			Dictionary<string, string> replacements = new(StringComparer.Ordinal)
			{
				["{ServerName}"] = TextValue(RequireSingleLine(server.ServerName, "ServerName"), "server_name"),
				["{Password}"] = TextValue(RequireSingleLine(context.Passwords.ServerPassword, "Password"), "password"),
				["{HasPassword}"] = BooleanValue(!string.IsNullOrWhiteSpace(context.Passwords.ServerPassword)),
				["{AdminPassword}"] = TextValue(RequireSingleLine(context.Passwords.AdminPassword, "AdminPassword"), "admin_password"),
				["{MaxPlayers}"] = ProbeNumber(server.MaxPlayers).ToString(CultureInfo.InvariantCulture),
				["{Port}"] = ProbeNumber(server.Port).ToString(CultureInfo.InvariantCulture),
				["{QueryPort}"] = ProbeNumber(server.QueryPort).ToString(CultureInfo.InvariantCulture),
				["{RCONPort}"] = ProbeNumber(server.RconPort).ToString(CultureInfo.InvariantCulture),
				["{RCONPassword}"] = TextValue(RequireSingleLine(context.Passwords.RconPassword, "RCONPassword"), "rcon_password"),
				["{EnableRcon}"] = BooleanValue(server.EnableRcon),
				["{Identity}"] = TextValue(context.Identity, "identity"),
				["{WorldName}"] = TextValue(RequireSingleLine(server.WorldName, "WorldName"), "world_name"),
				["{WorldSeed}"] = WorldSeedValue(),
				["{WorldSize}"] = ProbeNumber(server.WorldSize).ToString(CultureInfo.InvariantCulture),
				["{AppPort}"] = ProbeNumber(server.AppPort ?? 0).ToString(CultureInfo.InvariantCulture),
				["{LocalIP}"] = TextValue(RequireSingleLine(context.LocalIp, "LocalIP"), "local_ip"),
				["{PublicIP}"] = TextValue(RequireSingleLine(context.PublicIp, "PublicIP"), "public_ip"),
				["{IsPvp}"] = BooleanValue(string.Equals(server.GameMode, "PVP", StringComparison.OrdinalIgnoreCase)),
				["{IsPve}"] = BooleanValue(string.Equals(server.GameMode, "PVE", StringComparison.OrdinalIgnoreCase)),
				["{Crossplay}"] = BooleanValue(server.CrossplayEnabled),
				["{GameMode}"] = TextValue(GameModeValue(), "game_mode")
			};
			if (Format == ConfigFormat.YAML)
				return ConfigHandler.ExpandYamlTemplate(template, replacements);
			return Regex.Replace(template, @"\{[A-Za-z]+\}",
				match => replacements.GetValueOrDefault(match.Value, match.Value),
				RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
		}

		private (bool Changed, IReadOnlyList<string> Missing) UpdateManagedValues(
			string path,
			string template,
			string expandedTemplate,
			ConfigurationContext context)
		{
			List<ConfigLine> desiredValues =
				ConfigHandler.LoadConfigText(expandedTemplate, Format);
			Dictionary<string, ConfigLine> probeValues =
				ConfigHandler.LoadConfigText(
					ExpandTemplate(template, context, true),
					Format)
				.ToDictionary(value => value.Id, StringComparer.Ordinal);
			List<ConfigLine> managedValues = desiredValues
				.Where(value =>
					probeValues.TryGetValue(value.Id, out ConfigLine? probe) &&
					!TemplateValuesMatch(value, probe))
				.ToList();

			if (managedValues.Count == 0)
			{
				return (false, []);
			}

			if (Format == ConfigFormat.YAML)
				return ConfigHandler.UpdateYamlManagedValues(path, managedValues);

			List<ConfigLine> existingValues = ConfigHandler.LoadConfig(path, Format);
			Dictionary<string, ConfigLine> existingById = existingValues
				.ToDictionary(value => value.Id, StringComparer.Ordinal);
			List<string> missing = [];
			bool changed = false;

			foreach (ConfigLine desired in managedValues)
			{
				if (!existingById.TryGetValue(desired.Id, out ConfigLine? existing) ||
					!string.Equals(existing.Key, desired.Key, StringComparison.Ordinal) ||
					!string.Equals(existing.Path, desired.Path, StringComparison.Ordinal) ||
					existing.Type != desired.Type)
				{
					missing.Add(string.IsNullOrWhiteSpace(desired.Path)
						? desired.Key
						: desired.Path);
					continue;
				}

				if (TemplateValuesMatch(existing, desired))
				{
					continue;
				}

				existing.Value = desired.Value;
				changed = true;
			}

			if (changed)
			{
				ConfigHandler.SaveConfig(path, existingValues, Format);
			}

			return (changed, missing);
		}

		protected static bool TemplateValuesMatch(ConfigLine first, ConfigLine second)
		{
			if (first.Type == ConfigValueType.Boolean &&
				bool.TryParse(first.Value, out bool firstBoolean) &&
				bool.TryParse(second.Value, out bool secondBoolean))
			{
				return firstBoolean == secondBoolean;
			}

			return string.Equals(first.Value, second.Value, StringComparison.Ordinal);
		}

		private ManagedConfigurationInput GetSupportedInputs()
		{
			ManagedConfigurationInput supported = ManagedConfigurationInput.None;
			foreach (ConfigurationTemplate template in Templates)
			{
				string content = template.Content;
				if (content.Contains("{ServerName}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.ServerName;
				if (content.Contains("{Password}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.ServerPassword;
				if (content.Contains("{AdminPassword}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.AdminPassword;
				if (content.Contains("{WorldSeed}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.WorldSeed;
				if (content.Contains("{GameMode}", StringComparison.Ordinal) ||
					content.Contains("{IsPvp}", StringComparison.Ordinal) ||
					content.Contains("{IsPve}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.GameMode;
				if (content.Contains("{MaxPlayers}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.MaxPlayers;
				if (content.Contains("{QueryPort}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.QueryPort;
				if (content.Contains("{WorldName}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.WorldName;
				if (content.Contains("{RCONPort}", StringComparison.Ordinal) ||
					content.Contains("{RCONPassword}", StringComparison.Ordinal) ||
					content.Contains("{EnableRcon}", StringComparison.Ordinal))
				{
					supported |= ManagedConfigurationInput.Rcon;
				}
				if (content.Contains("{WorldSize}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.WorldSize;
				if (content.Contains("{Port}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.Port;
				if (content.Contains("{AppPort}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.AppPort;
				if (content.Contains("{Crossplay}", StringComparison.Ordinal))
					supported |= ManagedConfigurationInput.Crossplay;
			}

			return supported;
		}

		private static string? CreateUpgradeBackup(
			string path,
			int templateRevision)
		{
			string backupPath = $"{path}.synix.before-template-v{templateRevision}.bak";
			if (File.Exists(backupPath))
				return null;

			File.Copy(path, backupPath, false);
			return backupPath;
		}
	}
}
