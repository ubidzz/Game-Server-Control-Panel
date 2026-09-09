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
using Synix_Control_Panel.SynixApp.Database.GameDefinitions;
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.ServerHandler;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixApp.UI.GameDefinitions
{
	public partial class GameDefinitionBuilder : Form
	{
		private sealed record BuilderOption<T>(string Label, T Value)
		{
			public override string ToString() => Label;
		}

		public GameDefinitionBuilder()
		{
			InitializeComponent();
			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
				return;

			ThemeManager.Apply(this);
			ApplyTemplateGridTheme();
			cmbConfigMode.DataSource = new BuilderOption<ConfigFileCreationMode>[]
			{
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.ConfigMode.Unknown"),
					ConfigFileCreationMode.Unknown),
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.ConfigMode.GameGenerated"),
					ConfigFileCreationMode.GameGenerated),
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.ConfigMode.Template"),
					ConfigFileCreationMode.SynixTemplate),
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.ConfigMode.ArgumentsOnly"),
					ConfigFileCreationMode.LaunchArgumentsOnly)
			};
			cmbFormat.DataSource = new BuilderOption<ConfigFormat>[]
			{
				new("INI / CFG / properties", ConfigFormat.StandardINI),
				new("XML", ConfigFormat.XML),
				new("JSON", ConfigFormat.JSON),
				new("YAML / YML", ConfigFormat.YAML),
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.Format.SpaceSeparated"),
					ConfigFormat.Space),
				new("SCS SII", ConfigFormat.SII)
			};
			cmbLifecycleTracking.DataSource = new BuilderOption<GameLifecycleTrackingMode>[]
			{
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.Lifecycle.Process"),
					GameLifecycleTrackingMode.Process),
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.Lifecycle.External"),
					GameLifecycleTrackingMode.ExternalDeployment)
			};
			cmbDotNetFramework.DataSource = new BuilderOption<DotNetFrameworkRequirement>[]
			{
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.DotNet.None"),
					DotNetFrameworkRequirement.None),
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.DotNet.Framework48"),
					DotNetFrameworkRequirement.NetFramework48),
				new(LocalizationManager.Get(
					"GameDefinitions.Builder.DotNet.Framework481"),
					DotNetFrameworkRequirement.NetFramework481)
			};
			cmbArgumentTag.DataSource = GameDefinitionArgumentTags.LaunchArguments
				.Select(tag => new BuilderOption<GameDefinitionArgumentTag>(
					$"{tag.Token} — {tag.Name}",
					tag))
				.ToArray();
			numCatalogOrder.Value = Math.Min(
				numCatalogOrder.Maximum,
				GameDefinitionAuthoring.GetNextCatalogOrder());
			SelectOption(cmbConfigMode, ConfigFileCreationMode.Unknown);
			SelectOption(cmbFormat, ConfigFormat.StandardINI);
			SelectOption(cmbLifecycleTracking, GameLifecycleTrackingMode.Process);
			SelectOption(cmbDotNetFramework, DotNetFrameworkRequirement.None);
			rtbGuide.Text = BuildGuideText();
			RefreshConfigurationControls();
			RefreshPostInstallControls();
			ShowGuide();
		}

		private void cmbConfigMode_SelectedIndexChanged(
			object? sender,
			EventArgs eventArgs)
		{
			RefreshConfigurationControls();
		}

		private void txtGame_Leave(object? sender, EventArgs eventArgs)
		{
			if (!string.IsNullOrWhiteSpace(txtId.Text))
				return;
			txtId.Text = CreateId(txtGame.Text);
		}

		private void btnBrowseTemplate_Click(
			object? sender,
			EventArgs eventArgs)
		{
			using OpenFileDialog dialog = new()
			{
				Title = LocalizationManager.Get(
					"GameDefinitions.Builder.TemplatePicker.Title"),
				Filter = LocalizationManager.Get(
					"GameDefinitions.Builder.TemplatePicker.Filter"),
				CheckFileExists = true
			};
			if (dialog.ShowDialog(this) == DialogResult.OK)
				txtTemplate.Text = dialog.FileName;
		}

		private void btnAddTemplates_Click(
			object? sender,
			EventArgs eventArgs)
		{
			using OpenFileDialog dialog = new()
			{
				Title = LocalizationManager.Get(
					"GameDefinitions.Builder.AddTemplatesPicker.Title"),
				Filter = LocalizationManager.Get(
					"GameDefinitions.Builder.AddTemplatesPicker.Filter"),
				CheckFileExists = true,
				Multiselect = true
			};
			if (dialog.ShowDialog(this) != DialogResult.OK)
				return;

			foreach (string file in dialog.FileNames)
			{
				if (string.IsNullOrWhiteSpace(txtTemplate.Text))
				{
					txtTemplate.Text = file;
					if (string.IsNullOrWhiteSpace(txtConfigPath.Text))
						txtConfigPath.Text = Path.GetFileName(file);
					continue;
				}

				dgvAdditionalTemplates.Rows.Add(
					Path.GetFileName(file),
					file);
			}
		}

		private void btnRemoveTemplate_Click(
			object? sender,
			EventArgs eventArgs)
		{
			foreach (DataGridViewRow row in dgvAdditionalTemplates.SelectedRows
				.Cast<DataGridViewRow>()
				.OrderByDescending(row => row.Index))
			{
				if (!row.IsNewRow)
					dgvAdditionalTemplates.Rows.RemoveAt(row.Index);
			}
		}

		private void btnInsertArgumentTag_Click(
			object? sender,
			EventArgs eventArgs)
		{
			if (cmbArgumentTag.SelectedItem is not
				BuilderOption<GameDefinitionArgumentTag> option)
			{
				return;
			}

			int selectionStart = txtArguments.SelectionStart;
			string spacerBefore = selectionStart > 0 &&
				!char.IsWhiteSpace(txtArguments.Text[selectionStart - 1])
					? " "
					: string.Empty;
			string insertion = spacerBefore + option.Value.Token;
			txtArguments.Text = txtArguments.Text.Insert(selectionStart, insertion);
			txtArguments.SelectionStart = selectionStart + insertion.Length;
			txtArguments.Focus();
		}

		private void btnShowGuide_Click(object? sender, EventArgs eventArgs) =>
			ShowGuide();

		private void btnShowPreview_Click(object? sender, EventArgs eventArgs) =>
			ShowPreview();

		private void chkSteamRuntime_CheckedChanged(
			object? sender,
			EventArgs eventArgs) =>
			RefreshPostInstallControls();

		private void btnValidate_Click(object? sender, EventArgs eventArgs)
		{
			try
			{
				GameDefinitionDraft draft = BuildDraft();
				EmbeddedGamePackage package =
					GameDefinitionAuthoring.ValidateDraft(draft);
				rtbPreview.Text = GameDefinitionAuthoring.CreateDefinitionJson(draft);
				lblStatus.ForeColor = SettingsPalette.Success;
				LocalizationManager.BindText(
					lblStatus,
					"GameDefinitions.Builder.ValidSummary",
					package.Definition.DefinitionRevision,
					package.Configuration?.Templates.Count ?? 0,
					package.PostInstallActions.Count);
				ShowPreview();
			}
			catch (Exception exception)
			{
				lblStatus.ForeColor = SettingsPalette.Danger;
				lblStatus.Text = LocalizationManager.TranslateRuntimeText(
					exception.Message);
			}
		}

		private void btnSave_Click(object? sender, EventArgs eventArgs)
		{
			if (Core.IsOfficialRelease)
			{
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("MessageText.EA2CD9B6AE3E2608F49E"),
					LocalizationManager.Get("MessageText.B21513D99A8B9F37D043"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}

			try
			{
				string? projectDirectory =
					Core.FindProjectDirectory(AppContext.BaseDirectory) ??
					Core.FindProjectDirectory(Environment.CurrentDirectory);
				if (projectDirectory == null)
					throw new DirectoryNotFoundException(
						LocalizationManager.Get(
							"GameDefinitions.Builder.ProjectNotFound"));

				GameDefinitionSaveResult result =
					GameDefinitionAuthoring.SaveDraft(
						BuildDraft(),
						projectDirectory);
				rtbPreview.Text = result.Json;
				lblStatus.ForeColor = SettingsPalette.Success;
				LocalizationManager.BindText(
					lblStatus,
					"GameDefinitions.Builder.SavedPath",
					result.DefinitionPath);

				DialogResult open = LocalizedMessageBox.Show(
					this,
					LocalizationManager.Get("GameDefinitions.Builder.Saved.Body"),
					LocalizationManager.Get("MessageText.8A04520CAFD5494EA500"),
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Information);
				if (open == DialogResult.Yes)
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = Path.GetDirectoryName(result.DefinitionPath)!,
						UseShellExecute = true
					});
				}
			}
			catch (Exception exception)
			{
				lblStatus.ForeColor = SettingsPalette.Danger;
				lblStatus.Text = LocalizationManager.TranslateRuntimeText(
					exception.Message);
				LocalizedMessageBox.Show(
					this,
					LocalizationManager.TranslateRuntimeText(exception.Message),
					LocalizationManager.Get("MessageText.B97D1F258E17E5840885"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		private GameDefinitionDraft BuildDraft()
		{
			return new GameDefinitionDraft
			{
				Id = txtId.Text,
				CatalogOrder = numCatalogOrder.Value,
				DefinitionRevision = numDefinitionRevision.Value,
				Game = txtGame.Text,
				AppId = txtAppId.Text,
				RequiresSteamLogin = chkSteamLogin.Checked,
				SteamAppConfig = txtSteamAppConfig.Text,
				Executable = txtExecutable.Text,
				Arguments = txtArguments.Text,
				RconSyntax = txtRconSyntax.Text,
				Port = numPort.Value,
				QueryPort = numQueryPort.Value,
				Maps = ParseList(txtMaps.Text),
				GameModes = ParseList(txtGameModes.Text),
				ConfigFileCreation = GetSelectedValue(
					cmbConfigMode,
					ConfigFileCreationMode.Unknown),
				Format = GetSelectedValue(cmbFormat, ConfigFormat.StandardINI),
				RelativeConfigPath = txtConfigPath.Text,
				TemplateSourcePath = txtTemplate.Text,
				AdditionalTemplates = ReadAdditionalTemplates(),
				ConfigurationRevision = numConfigRevision.Value,
				ExternalDataFolderName = txtExternalDataFolder.Text,
				RequiredLaunchFiles = ParseList(txtRequiredLaunchFiles.Text),
				OptionalLaunchFiles = ParseList(txtOptionalLaunchFiles.Text),
				LaunchFileSetupInstructions = txtSetupInstructions.Text,
				NeedsConfigWarning = chkFirstStartWarning.Checked,
				WarningMessage = txtWarningMessage.Text,
				IconUrl = txtIconUrl.Text,
				CopySteamRuntimeFiles = chkSteamRuntime.Checked,
				SteamRuntimeTargetDirectory = txtSteamRuntimeTarget.Text,
				IsQueryable = chkQueryable.Checked,
				LogPaths = ParseList(txtLogPaths.Text),
				RuntimeRequirements = new GameRuntimeRequirements
				{
					MinimumSystemMemoryGb = numMinimumRam.Value,
					RequiresAvx2 = chkRequiresAvx2.Checked,
					RequiresHardwareVirtualization = chkRequiresVirtualization.Checked,
					RequiresHyperV = chkRequiresHyperV.Checked,
					RequiresWindowsProfessionalOrHigher = chkRequiresWindowsPro.Checked,
					MinimumDotNetFramework = GetSelectedValue(
						cmbDotNetFramework,
						DotNetFrameworkRequirement.None),
					VisualCppRedistributables = GetVisualCppRequirements()
				},
				LaunchBehavior = new GameLaunchBehavior
				{
					RunElevated = chkRunElevated.Checked,
					RequiresVisibleWindow = chkRequiresVisibleWindow.Checked,
					LifecycleTracking = GetSelectedValue(
						cmbLifecycleTracking,
						GameLifecycleTrackingMode.Process),
					AllowLaunchFileExport = chkAllowLaunchExport.Checked,
					ReadyMessage = txtReadyMessage.Text
				}
			};
		}

		private void RefreshConfigurationControls()
		{
			ConfigFileCreationMode mode = GetSelectedValue(
				cmbConfigMode,
				ConfigFileCreationMode.Unknown);
			bool enabled = mode is
				ConfigFileCreationMode.SynixTemplate or
				ConfigFileCreationMode.GameGenerated;
			txtConfigPath.Enabled = enabled;
			cmbFormat.Enabled = enabled;
			txtTemplate.Enabled = enabled;
			btnBrowseTemplate.Enabled = enabled;
			numConfigRevision.Enabled = enabled;
			dgvAdditionalTemplates.Enabled = enabled;
			btnAddTemplates.Enabled = enabled;
			btnRemoveTemplate.Enabled = enabled;
			lblConfigModeHelp.Text = mode switch
			{
				ConfigFileCreationMode.SynixTemplate =>
					LocalizationManager.Get(
						"GameDefinitions.Builder.ConfigModeHelp.Template"),
				ConfigFileCreationMode.GameGenerated =>
					LocalizationManager.Get(
						"GameDefinitions.Builder.ConfigModeHelp.GameGenerated"),
				ConfigFileCreationMode.LaunchArgumentsOnly =>
					LocalizationManager.Get(
						"GameDefinitions.Builder.ConfigModeHelp.ArgumentsOnly"),
				_ =>
					LocalizationManager.Get(
						"GameDefinitions.Builder.ConfigModeHelp.Unknown")
			};
		}

		private void RefreshPostInstallControls()
		{
			txtSteamRuntimeTarget.Enabled = chkSteamRuntime.Checked;
		}

		private IReadOnlyList<VisualCppRedistributableRequirement>
			GetVisualCppRequirements()
		{
			List<VisualCppRedistributableRequirement> requirements = [];
			if (chkRequiresVisualCpp2013.Checked)
			{
				requirements.Add(
					VisualCppRedistributableRequirement.VisualCpp2013X64);
			}
			if (chkRequiresVisualCpp2015To2022.Checked)
			{
				requirements.Add(
					VisualCppRedistributableRequirement.VisualCpp2015To2022X64);
			}
			return requirements;
		}

		private void chkRequiresHyperV_CheckedChanged(
			object? sender,
			EventArgs eventArgs)
		{
			if (chkRequiresHyperV.Checked)
				chkRequiresWindowsPro.Checked = true;
		}

		private void cmbLifecycleTracking_SelectedIndexChanged(
			object? sender,
			EventArgs eventArgs)
		{
			if (GetSelectedValue(
				cmbLifecycleTracking,
				GameLifecycleTrackingMode.Process) ==
				GameLifecycleTrackingMode.ExternalDeployment)
			{
				chkQueryable.Checked = false;
			}
		}

		private IReadOnlyList<GameDefinitionTemplateDraft> ReadAdditionalTemplates()
		{
			dgvAdditionalTemplates.EndEdit();
			return dgvAdditionalTemplates.Rows
				.Cast<DataGridViewRow>()
				.Where(row => !row.IsNewRow)
				.Select(row => new GameDefinitionTemplateDraft(
					Convert.ToString(row.Cells[colTemplateDestination.Index].Value)?.Trim() ?? string.Empty,
					Convert.ToString(row.Cells[colTemplateSource.Index].Value)?.Trim() ?? string.Empty))
				.Where(template =>
					!string.IsNullOrWhiteSpace(template.RelativePath) ||
					!string.IsNullOrWhiteSpace(template.SourcePath))
				.ToArray();
		}

		private void ApplyTemplateGridTheme()
		{
			dgvAdditionalTemplates.BackgroundColor = SettingsPalette.Input;
			dgvAdditionalTemplates.GridColor = SettingsPalette.Divider;
			dgvAdditionalTemplates.ColumnHeadersDefaultCellStyle.BackColor = SettingsPalette.Sidebar;
			dgvAdditionalTemplates.ColumnHeadersDefaultCellStyle.ForeColor = SettingsPalette.SecondaryText;
			dgvAdditionalTemplates.DefaultCellStyle.BackColor = SettingsPalette.Input;
			dgvAdditionalTemplates.DefaultCellStyle.ForeColor = SettingsPalette.PrimaryText;
			dgvAdditionalTemplates.DefaultCellStyle.SelectionBackColor = SettingsPalette.Selection;
			dgvAdditionalTemplates.DefaultCellStyle.SelectionForeColor = SettingsPalette.PrimaryText;
		}

		private void ShowGuide()
		{
			rtbGuide.Visible = true;
			rtbPreview.Visible = false;
			LocalizationManager.BindText(
				lblRightPane,
				"Text.3CEBC2F3146476331D53");
			btnShowGuide.UseAccentStyle = true;
			btnShowPreview.UseAccentStyle = false;
			btnShowGuide.Invalidate();
			btnShowPreview.Invalidate();
		}

		private void ShowPreview()
		{
			rtbGuide.Visible = false;
			rtbPreview.Visible = true;
			LocalizationManager.BindText(
				lblRightPane,
				"Text.F08E959E621C8112615C");
			btnShowGuide.UseAccentStyle = false;
			btnShowPreview.UseAccentStyle = true;
			btnShowGuide.Invalidate();
			btnShowPreview.Invalidate();
		}

		private static T GetSelectedValue<T>(ComboBox comboBox, T fallback) =>
			comboBox.SelectedItem is BuilderOption<T> option
				? option.Value
				: fallback;

		private static void SelectOption<T>(ComboBox comboBox, T value)
			where T : struct, Enum
		{
			for (int index = 0; index < comboBox.Items.Count; index++)
			{
				if (comboBox.Items[index] is BuilderOption<T> option &&
					EqualityComparer<T>.Default.Equals(option.Value, value))
				{
					comboBox.SelectedIndex = index;
					return;
				}
			}
		}

		private static string BuildGuideText()
		{
			string launchTags = string.Join(
				Environment.NewLine,
				GameDefinitionArgumentTags.LaunchArguments.Select(tag =>
					$"{tag.Token}  " +
					LocalizationManager.TranslateRuntimeText(tag.Description)));
			string rconTags = string.Join(
				Environment.NewLine,
				GameDefinitionArgumentTags.RconSyntax.Select(tag =>
					$"{tag.Token}  " +
					LocalizationManager.TranslateRuntimeText(tag.Description)));

			return LocalizationManager.Get("GameDefinitions.Builder.Guide")
				.Replace(
					"[LAUNCH_ARGUMENT_TAGS]",
					launchTags,
					StringComparison.Ordinal)
				.Replace(
					"[RCON_ARGUMENT_TAGS]",
					rconTags,
					StringComparison.Ordinal);
		}

		private static string CreateId(string name)
		{
			string id = Regex.Replace(
				name.Trim().ToLowerInvariant(),
				"[^a-z0-9]+",
				"-").Trim('-');
			return id;
		}

		private static IReadOnlyList<string> ParseList(string value) =>
			value
				.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
				.Select(item => item.Trim())
				.Where(item => item.Length > 0)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
	}
}
