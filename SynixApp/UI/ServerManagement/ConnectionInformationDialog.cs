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
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixApp.UI.ServerManagement
{
	internal sealed class ConnectionInformationDialog : Form
	{
		private readonly GameServer _server;
		private readonly Label _lanValue;
		private readonly Label _publicValue;
		private string _lanAddress = string.Empty;
		private string _publicAddress = string.Empty;

		internal ConnectionInformationDialog(GameServer server)
		{
			_server = server ?? throw new ArgumentNullException(nameof(server));
			bool isBedrock = MinecraftControlProfile.IsBedrock(_server);
			Text = LocalizationManager.Get("Menu.ConnectionInformation");
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = false;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			ClientSize = new Size(760, 520);
			BackColor = SettingsPalette.Window;
			ForeColor = SettingsPalette.PrimaryText;
			Font = new Font("Segoe UI", 9.5F);

			Controls.Add(new Label
			{
				Text = LocalizationManager.Get(
					"Connection.Heading",
					_server.ServerName),
				Font = new Font("Segoe UI", 18F, FontStyle.Bold),
				Location = new Point(28, 22),
				Size = new Size(704, 40),
				ForeColor = SettingsPalette.PrimaryText
			});
			Controls.Add(new Label
			{
				Text = LocalizationManager.Get("Connection.Subtitle"),
				Location = new Point(30, 66),
				Size = new Size(700, 28),
				ForeColor = SettingsPalette.SecondaryText
			});

			_lanValue = AddConnectionCard(
				LocalizationManager.Get("Connection.Local.Title"),
				LocalizationManager.Get("Connection.Local.Description"),
				108,
				out ModernSettingsButton lanCopy);
			_publicValue = AddConnectionCard(
				LocalizationManager.Get("Connection.Public.Title"),
				isBedrock
					? LocalizationManager.Get(
						"Connection.Public.BedrockDescription")
					: LocalizationManager.Get(
						"Connection.Public.Description"),
				226,
				out ModernSettingsButton publicCopy);
			lanCopy.Click += (_, _) => CopyAddress(_lanAddress);
			publicCopy.Click += (_, _) => CopyAddress(_publicAddress);

			Controls.Add(new Label
			{
				Text = isBedrock
					? LocalizationManager.Get(
						"Connection.Ports.BedrockSummary",
						_server.Port,
						_server.QueryPort)
					: BuildStandardPortSummary(),
				Location = new Point(30, 356),
				Size = new Size(700, 54),
				ForeColor = SettingsPalette.SecondaryText
			});

			ModernSettingsButton close = new()
			{
				Text = LocalizationManager.Get("ModManager.Button.Close"),
				Location = new Point(574, 446),
				Size = new Size(158, 44),
				DialogResult = DialogResult.OK,
				UseAccentStyle = true
			};
			Controls.Add(close);
			AcceptButton = close;
			CancelButton = close;
			ThemeManager.Apply(this);
		}

		protected override async void OnShown(EventArgs eventArgs)
		{
			base.OnShown(eventArgs);
			bool privacyMode = Properties.Settings.Default.PrivacyMode;
			try
			{
				string lanIp = await Core.Instance.GetLocalIP();
				string publicIp = privacyMode ? string.Empty : await Core.Instance.GetPublicIP();
				_lanAddress = FormatAddress(lanIp, _server.Port);
				_publicAddress = string.IsNullOrWhiteSpace(publicIp)
					? string.Empty
					: FormatAddress(publicIp.Trim(), _server.Port);
				_lanValue.Text = privacyMode
					? LocalizationManager.Get("Connection.Address.Hidden")
					: _lanAddress;
				_publicValue.Text = privacyMode
					? LocalizationManager.Get("Connection.Address.Hidden")
					: string.IsNullOrWhiteSpace(_publicAddress)
						? LocalizationManager.Get(
							"Connection.Address.PublicUnavailable")
						: _publicAddress;
			}
			catch (Exception exception)
			{
				_lanValue.Text = LocalizationManager.Get(
					"Connection.Address.Unavailable");
				_publicValue.Text = LocalizationManager.Get(
					"Connection.Address.Unavailable");
				_ = exception;
			}
		}

		internal static string FormatAddress(string address, int port) =>
			$"{address}:{port}";

		private string BuildStandardPortSummary()
		{
			List<string> ports =
			[
				LocalizationManager.Get("Connection.Port.Game", _server.Port)
			];
			if (_server.QueryPort > 0 && _server.QueryPort != _server.Port)
			{
				ports.Add(LocalizationManager.Get(
					"Connection.Port.Query",
					_server.QueryPort));
			}
			if (_server.EnableRcon && _server.RconPort > 0)
			{
				ports.Add(LocalizationManager.Get(
					"Connection.Port.Rcon",
					_server.RconPort));
			}
			if (_server.AppPort is > 0)
			{
				ports.Add(LocalizationManager.Get(
					"Connection.Port.App",
					_server.AppPort.Value));
			}

			return LocalizationManager.Get(
				"Connection.Ports.StandardSummary",
				string.Join(", ", ports));
		}

		private Label AddConnectionCard(
			string title,
			string description,
			int top,
			out ModernSettingsButton copyButton)
		{
			ModernSettingsCard card = new()
			{
				Location = new Point(28, top),
				Size = new Size(704, 104),
				FillColor = SettingsPalette.Card,
				BorderColor = SettingsPalette.Divider
			};
			card.Controls.Add(new Label
			{
				Text = title,
				Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
				Location = new Point(20, 14),
				Size = new Size(460, 24),
				ForeColor = SettingsPalette.PrimaryText
			});
			Label value = new()
			{
				Text = LocalizationManager.Get("Text.47D2A515EF2F05B87D68"),
				Font = new Font("Consolas", 11F, FontStyle.Bold),
				Location = new Point(20, 43),
				Size = new Size(450, 25),
				ForeColor = SettingsPalette.Accent
			};
			card.Controls.Add(value);
			card.Controls.Add(new Label
			{
				Text = description,
				Location = new Point(20, 72),
				Size = new Size(510, 22),
				ForeColor = SettingsPalette.SecondaryText
			});
			copyButton = new ModernSettingsButton
			{
				Text = LocalizationManager.Get("Text.B0E3D6C0E2C3C45CBD0F"),
				Location = new Point(548, 31),
				Size = new Size(134, 42)
			};
			card.Controls.Add(copyButton);
			Controls.Add(card);
			return value;
		}

		private static void CopyAddress(string address)
		{
			if (string.IsNullOrWhiteSpace(address))
				return;
			try { Clipboard.SetText(address); }
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
		}
	}
}
