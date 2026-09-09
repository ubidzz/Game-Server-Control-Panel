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
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine.ModManagement;

namespace Synix_Control_Panel.SynixApp.UI.ServerManagement;

internal abstract class ModPackageDialog : Form
{
	protected ModPackageDialog(string title)
	{
		Text = LocalizationManager.Get(title);
		StartPosition = FormStartPosition.CenterParent;
		ShowInTaskbar = false;
		MinimizeBox = MaximizeBox = false;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		ClientSize = new Size(720, 470);
		BackColor = SettingsPalette.Window;
		ForeColor = SettingsPalette.PrimaryText;
		Font = new Font("Segoe UI", 10F);
		AddText(Text, 20, 48, heading: true);
	}

	protected Label AddText(string text, int top, int height, bool heading = false)
	{
		Label label = new() { Text = text, Location = new Point(28, top), Size = new Size(664, height),
			UseMnemonic = false, ForeColor = heading ? SettingsPalette.PrimaryText : SettingsPalette.SecondaryText };
		if (heading) label.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
		Controls.Add(label);
		return label;
	}

	protected TextBox AddInput(int top, string name, bool readOnly = false)
	{
		TextBox input = new() { Name = name, Location = new Point(28, top), Width = 664, ReadOnly = readOnly,
			BackColor = SettingsPalette.Input, ForeColor = SettingsPalette.PrimaryText, BorderStyle = BorderStyle.FixedSingle };
		Controls.Add(input);
		return input;
	}

	protected ModernSettingsButton AddButton(string key, int left, int top, Action click)
	{
		ModernSettingsButton button = new() { Text = LocalizationManager.Get(key), Location = new Point(left, top), Size = new Size(190, 42) };
		button.Click += (_, _) => click();
		Controls.Add(button);
		return button;
	}

	protected void AddCancel()
	{
		ModernSettingsButton cancel = AddButton("ModManager.Button.Close", 294, 400, () => { DialogResult = DialogResult.Cancel; Close(); });
		CancelButton = cancel;
	}
}

internal sealed class ModPackagePicker : ModPackageDialog
{
	private readonly bool _scenario;
	private readonly TextBox _source;
	private readonly TextBox _folder;
	private readonly Label _status;
	internal string SourcePath => _source.Text;
	internal bool IsFolder { get; private set; }
	internal string? ScenarioFolder => _scenario ? _folder.Text : null;

	internal ModPackagePicker(ModInstallTarget target) : base(ModPackageHandlers.For(target).PickerTitleKey)
	{
		bool scenario = EmpyrionAddOns.IsScenario(target);
		_scenario = scenario;
		AddText(LocalizationManager.Get(ModPackageHandlers.For(target).PickerHelpKey), 74, 88);
		_source = AddInput(168, "modPackageSource", readOnly: true);
		AddButton("EmpyrionMods.Picker.Zip", 28, 209, ChooseZip);
		AddButton("EmpyrionMods.Picker.Folder", 230, 209, ChooseFolder);
		AddText(LocalizationManager.Get(scenario ? "EmpyrionMods.Picker.FolderName" : "ModPackages.Picker.Destination"), 266, 28);
		_folder = AddInput(296, "modPackageFolderName", readOnly: !scenario);
		_folder.MaxLength = 80;
		if (!scenario) _folder.Text = target.RelativePath.Replace('/', '\\');
		_status = AddText(string.Empty, 337, 55);
		AddCancel();
		AddButton("EmpyrionMods.Picker.Review", 502, 400, Accept).UseAccentStyle = true;
		ThemeManager.Apply(this);
	}

	private void ChooseZip()
	{
		using OpenFileDialog picker = new() { Filter = LocalizationManager.Get("ModManager.FileFilter.ArchiveOnly"), CheckFileExists = true };
		if (picker.ShowDialog(this) == DialogResult.OK) SetSource(picker.FileName, false);
	}

	private void ChooseFolder()
	{
		using FolderBrowserDialog picker = new() { Description = LocalizationManager.Get("ModPackages.Picker.SelectFolder"), UseDescriptionForTitle = true, ShowNewFolderButton = false };
		if (picker.ShowDialog(this) == DialogResult.OK) SetSource(picker.SelectedPath, true);
	}

	private void SetSource(string path, bool folder)
	{
		_source.Text = path;
		IsFolder = folder;
		if (_scenario) _folder.Text = EmpyrionAddOns.SuggestFolderName(folder ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path));
		_status.Text = string.Empty;
	}

	private void Accept()
	{
		try
		{
			if (IsFolder ? !Directory.Exists(SourcePath) : !File.Exists(SourcePath)) throw ModPackageFiles.Error("Layout");
			if (_scenario) EmpyrionAddOns.ValidateFolderName(_folder.Text, scenario: true);
			DialogResult = DialogResult.OK;
			Close();
		}
		catch (Exception exception) { _status.Text = exception.Message; _status.ForeColor = SettingsPalette.Warning; }
	}
}

internal sealed class EmpyrionScenarioPicker : ModPackageDialog
{
	private readonly ModernSettingsComboBox _scenario;
	private readonly TextBox _save;
	private readonly Label _status;
	internal EmpyrionScenarioState OriginalSelection { get; }
	internal string Scenario => _scenario.SelectedItem as string ?? string.Empty;
	internal string SaveName => _save.Text;

	internal EmpyrionScenarioPicker(GameServer server) : base("EmpyrionMods.Button.ChooseScenario")
	{
		OriginalSelection = EmpyrionAddOns.ReadSelection(server);
		AddText(LocalizationManager.Get("EmpyrionMods.Selection.Help", OriginalSelection.Scenario, OriginalSelection.SaveName), 74, 90);
		AddText(LocalizationManager.Get("EmpyrionMods.Selection.Scenario"), 169, 26);
		_scenario = new ModernSettingsComboBox { Name = "empyrionScenarioSelector", Location = new Point(28, 201), Size = new Size(664, 36), DropDownStyle = ComboBoxStyle.DropDownList };
		foreach (string scenario in EmpyrionAddOns.GetScenarios(server)) _scenario.Items.Add(scenario);
		Controls.Add(_scenario);
		AddText(LocalizationManager.Get("EmpyrionMods.Selection.SaveName"), 253, 28);
		_save = AddInput(289, "empyrionSaveName");
		_save.MaxLength = 80;
		_status = AddText(LocalizationManager.Get("EmpyrionMods.Selection.NewSaveNotice"), 333, 58);
		_scenario.SelectedIndexChanged += (_, _) =>
		{
			_save.Text = Scenario == OriginalSelection.Scenario ? OriginalSelection.SaveName :
				EmpyrionAddOns.SuggestFolderName(Scenario) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
		};
		_scenario.SelectedItem = OriginalSelection.Scenario;
		if (_scenario.SelectedIndex < 0 && _scenario.Items.Count > 0) _scenario.SelectedIndex = 0;
		AddCancel();
		AddButton("EmpyrionMods.Selection.Use", 502, 400, () =>
		{
			try
			{
				EmpyrionAddOns.ValidateFolderName(Scenario, scenario: true);
				EmpyrionAddOns.ValidateFolderName(SaveName);
				DialogResult = DialogResult.OK;
				Close();
			}
			catch (Exception exception) { _status.Text = exception.Message; _status.ForeColor = SettingsPalette.Warning; }
		}).UseAccentStyle = true;
		ThemeManager.Apply(this);
	}
}
