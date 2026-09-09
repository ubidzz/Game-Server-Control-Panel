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
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixApp.UI.Onboarding
{
	internal sealed class ExistingServerImportWizard : Form
	{
		private readonly TextBox _folderBox;
		private readonly ModernSettingsComboBox _gameBox;
		private readonly TextBox _nameBox;
		private readonly NumericUpDown _gamePort;
		private readonly NumericUpDown _queryPort;
		private readonly Label _detectionStatus;
		private readonly ModernSettingsButton _importButton;
		private IReadOnlyList<ExistingServerDetection> _detections = [];

		internal GameServer? ImportedServer { get; private set; }

		internal ExistingServerImportWizard()
		{
			Text = LocalizationManager.Get("Text.25FBAC53BB846E706EFB");
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = false;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			ClientSize = new Size(800, 590);
			BackColor = SettingsPalette.Window;
			ForeColor = SettingsPalette.PrimaryText;
			Font = new Font("Segoe UI", 9.5F);

			Controls.Add(Heading(
				LocalizationManager.Get("Onboarding.Import.Heading"),
				28, 24, 744, 42, 19F));
			Controls.Add(Body(
				LocalizationManager.Get("Onboarding.Import.Subtitle"),
				30, 68, 730, 50));

			ModernSettingsCard folderCard = Card(28, 126, 744, 132);
			folderCard.Controls.Add(Heading(
				LocalizationManager.Get("Onboarding.Import.StepFolder"),
				18, 14, 540, 26, 11F));
			_folderBox = Input(18, 52, 566, 38);
			_folderBox.TextChanged += (_, _) => DetectGames();
			folderCard.Controls.Add(_folderBox);
			ModernSettingsButton browse = new()
			{
				Text = LocalizationManager.Get("Text.3227AA9666253F7A7365"),
				Location = new Point(596, 50),
				Size = new Size(128, 42)
			};
			browse.Click += Browse_Click;
			folderCard.Controls.Add(browse);
			_detectionStatus = Body(
				LocalizationManager.Get("Onboarding.Import.NoFolderSelected"),
				18, 98, 706, 24);
			folderCard.Controls.Add(_detectionStatus);
			Controls.Add(folderCard);

			ModernSettingsCard detailsCard = Card(28, 274, 744, 226);
			detailsCard.Controls.Add(Heading(
				LocalizationManager.Get("Onboarding.Import.StepDetails"),
				18, 14, 680, 26, 11F));
			detailsCard.Controls.Add(FieldLabel(
				LocalizationManager.Get("Onboarding.Import.DetectedGame"),
				18, 52, 330));
			detailsCard.Controls.Add(FieldLabel(
				LocalizationManager.Get("Onboarding.Import.ServerName"),
				382, 52, 330));
			_gameBox = new ModernSettingsComboBox
			{
				Location = new Point(18, 78),
				Size = new Size(330, 40),
				Enabled = false
			};
			_gameBox.SelectedIndexChanged += GameBox_SelectedIndexChanged;
			detailsCard.Controls.Add(_gameBox);
			_nameBox = Input(382, 78, 342, 40);
			detailsCard.Controls.Add(_nameBox);

			detailsCard.Controls.Add(FieldLabel(
				LocalizationManager.Get("Text.3E8DBB6C3828E368EDBE"),
				18, 136, 160));
			detailsCard.Controls.Add(FieldLabel(
				LocalizationManager.Get("Text.516E776E998FF69A8D69"),
				200, 136, 160));
			_gamePort = PortInput(18, 162);
			_queryPort = PortInput(200, 162);
			detailsCard.Controls.Add(_gamePort);
			detailsCard.Controls.Add(_queryPort);
			detailsCard.Controls.Add(Body(
				LocalizationManager.Get("Onboarding.Import.PortsHelp"),
				382, 144, 342, 60));
			Controls.Add(detailsCard);

			ModernSettingsButton cancel = new()
			{
				Text = LocalizationManager.Get("Text.19766ED6CCB2F4A32778"),
				Location = new Point(464, 522),
				Size = new Size(142, 44),
				DialogResult = DialogResult.Cancel
			};
			_importButton = new ModernSettingsButton
			{
				Text = LocalizationManager.Get("Text.E903F7224FB457AE0BFD"),
				Location = new Point(618, 522),
				Size = new Size(154, 44),
				UseAccentStyle = true,
				Enabled = false
			};
			_importButton.Click += Import_Click;
			Controls.AddRange([cancel, _importButton]);
			CancelButton = cancel;
			ThemeManager.Apply(this);
		}

		private void Browse_Click(object? sender, EventArgs eventArgs)
		{
			using FolderBrowserDialog browser = new()
			{
				Description = LocalizationManager.Get("Onboarding.Import.FolderPicker"),
				UseDescriptionForTitle = true,
				ShowNewFolderButton = false
			};
			if (browser.ShowDialog(this) == DialogResult.OK)
				_folderBox.Text = browser.SelectedPath;
		}

		private void DetectGames()
		{
			_detections = ExistingServerImport.Detect(_folderBox.Text);
			_gameBox.Items.Clear();
			foreach (ExistingServerDetection detection in _detections)
				_gameBox.Items.Add(detection.DisplayName);

			_gameBox.Enabled = _detections.Count > 0;
			_importButton.Enabled = _detections.Count > 0;
			if (_detections.Count == 0)
			{
				LocalizationManager.BindText(
					_detectionStatus,
					Directory.Exists(_folderBox.Text.Trim())
						? "DynamicText.4477A4C46D66B4C3CDD3"
						: "DynamicText.CB565F94960FAEEC9A40");
				_detectionStatus.ForeColor = SettingsPalette.Warning;
				return;
			}

			_gameBox.SelectedIndex = 0;
			LocalizationManager.BindText(
				_detectionStatus,
				_detections.Count == 1
					? "Onboarding.Import.FoundOne"
					: "Onboarding.Import.FoundMany",
				_detections.Count == 1
					? _detections[0].DisplayName
					: _detections.Count);
			_detectionStatus.ForeColor = SettingsPalette.Success;
		}

		private void GameBox_SelectedIndexChanged(object? sender, EventArgs eventArgs)
		{
			ExistingServerDetection? detection = GetSelectedDetection();
			if (detection == null)
				return;

			_nameBox.Text = MakeUniqueName(detection.DisplayName);
			int defaultGamePort = detection.MinecraftEdition == MinecraftControlProfile.BedrockEdition
				? MinecraftControlProfile.BedrockDefaultPort
				: detection.Game.Port;
			int defaultQueryPort = detection.MinecraftEdition == MinecraftControlProfile.BedrockEdition
				? MinecraftControlProfile.BedrockDefaultIpv6Port
				: detection.Game.QueryPort;
			_gamePort.Value = ExistingServerImport.FindAvailablePort(
				defaultGamePort,
				ServerRegistry.Servers);
			_queryPort.Value = ExistingServerImport.FindAvailablePort(
				defaultQueryPort,
				ServerRegistry.Servers.Concat([new GameServer { Port = (int)_gamePort.Value }]));
		}

		private async void Import_Click(object? sender, EventArgs eventArgs)
		{
			ExistingServerDetection? detection = GetSelectedDetection();
			if (detection == null)
				return;

			try
			{
				_importButton.Enabled = false;
				ImportedServer = ExistingServerImport.Create(
					_folderBox.Text,
					detection.Game,
					_nameBox.Text,
					(int)_gamePort.Value,
					(int)_queryPort.Value,
					ServerRegistry.Servers,
					detection.MinecraftEdition);
				ServerRegistry.Servers.Add(ImportedServer);
				await Core.RefreshServerIconAsync(ImportedServer);
				if (!Synix_Control_Panel.SynixApp.FileFolderHandler.FileHandler.SaveServers())
					throw new IOException(LocalizationManager.Get(
						"Onboarding.Import.SaveFailed"));

				await Core.Instance.RebindProcesses();
				ApplicationLogService.WriteLocalized(
					"Onboarding.Import.Activity.Registered",
					SettingsPalette.Success,
					true,
					ImportedServer.ServerName);
				DialogResult = DialogResult.OK;
				Close();
			}
			catch (Exception exception)
			{
				if (ImportedServer != null)
					ServerRegistry.Servers.Remove(ImportedServer);
				ImportedServer = null;
				PlainEnglishErrorDialog.ShowError(
					this,
					LocalizationManager.Get("Onboarding.Import.ErrorAction"),
					exception.Message);
				_importButton.Enabled = true;
			}
		}

		private ExistingServerDetection? GetSelectedDetection() =>
			_gameBox.SelectedIndex >= 0 && _gameBox.SelectedIndex < _detections.Count
				? _detections[_gameBox.SelectedIndex]
				: null;

		private static string MakeUniqueName(string game)
		{
			string baseName = LocalizationManager.Get(
				"Onboarding.Import.DefaultServerName",
				game);
			string candidate = baseName;
			for (int suffix = 2; ServerRegistry.Servers.Any(server => server.ServerName.Equals(
				candidate,
				StringComparison.OrdinalIgnoreCase)); suffix++)
			{
				candidate = $"{baseName} {suffix}";
			}
			return candidate;
		}

		private static ModernSettingsCard Card(int left, int top, int width, int height) => new()
		{
			Location = new Point(left, top),
			Size = new Size(width, height),
			FillColor = SettingsPalette.Card,
			BorderColor = SettingsPalette.Divider
		};

		private static Label Heading(string text, int left, int top, int width, int height, float size) => new()
		{
			Text = text,
			Font = new Font("Segoe UI", size, FontStyle.Bold),
			ForeColor = SettingsPalette.PrimaryText,
			Location = new Point(left, top),
			Size = new Size(width, height)
		};

		private static Label Body(string text, int left, int top, int width, int height) => new()
		{
			Text = text,
			ForeColor = SettingsPalette.SecondaryText,
			Location = new Point(left, top),
			Size = new Size(width, height)
		};

		private static Label FieldLabel(string text, int left, int top, int width) => new()
		{
			Text = text,
			Font = new Font("Segoe UI", 9F, FontStyle.Bold),
			ForeColor = SettingsPalette.SecondaryText,
			Location = new Point(left, top),
			Size = new Size(width, 22)
		};

		private static TextBox Input(int left, int top, int width, int height) => new()
		{
			Location = new Point(left, top),
			Size = new Size(width, height),
			BackColor = SettingsPalette.Input,
			ForeColor = SettingsPalette.PrimaryText,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 10F)
		};

		private static NumericUpDown PortInput(int left, int top) => new()
		{
			Location = new Point(left, top),
			Size = new Size(160, 40),
			Minimum = 1,
			Maximum = 65535,
			BackColor = SettingsPalette.Input,
			ForeColor = SettingsPalette.PrimaryText,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 10F)
		};
	}
}
