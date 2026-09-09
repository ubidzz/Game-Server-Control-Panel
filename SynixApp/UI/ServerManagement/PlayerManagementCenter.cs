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
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixApp.UI.ServerManagement
{
	internal sealed class PlayerManagementCenter : Form
	{
		private readonly GameServer _server;
		private readonly DataGridView _grid;
		private readonly Label _summary;
		private readonly Label _status;
		private readonly ModernSettingsButton _refresh;
		private readonly ModernSettingsButton _kick;
		private readonly ModernSettingsButton _allowlist;
		private readonly ModernSettingsButton _operator;

		internal PlayerManagementCenter(GameServer server)
		{
			_server = server ?? throw new ArgumentNullException(nameof(server));
			Text = LocalizationManager.Get("Menu.PlayerManagementCenter");
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = true;
			MinimumSize = new Size(840, 600);
			ClientSize = new Size(900, 620);
			BackColor = SettingsPalette.Window;
			ForeColor = SettingsPalette.PrimaryText;
			Font = new Font("Segoe UI", 9.5F);

			Controls.Add(new Label
			{
				Text = LocalizationManager.Get("Menu.PlayerManagementCenter"),
				Font = new Font("Segoe UI", 19F, FontStyle.Bold),
				Location = new Point(28, 22),
				Size = new Size(620, 42),
				ForeColor = SettingsPalette.PrimaryText,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
			});
			_summary = new Label
			{
				Text = BuildSummary(_server.CurrentPlayers),
				Location = new Point(30, 66),
				Size = new Size(820, 28),
				ForeColor = SettingsPalette.SecondaryText,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
			};
			Controls.Add(_summary);

			_grid = new DataGridView
			{
				Name = "playerManagementGrid",
				Location = new Point(28, 112),
				Size = new Size(844, 372),
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				ReadOnly = true,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				AutoGenerateColumns = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				MultiSelect = false,
				RowHeadersVisible = false
			};
			_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "PlayerName",
				HeaderText = LocalizationManager.Get("Text.FD49E5BC2A30C83CDB47"),
				DataPropertyName = nameof(GamePlayerInfo.Name),
				AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
				FillWeight = 60
			});
			_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Score",
				HeaderText = LocalizationManager.Get("Text.A60FFBB10DC4CF25CA49"),
				DataPropertyName = nameof(GamePlayerInfo.Score),
				Width = 150
			});
			_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Connected",
				HeaderText = LocalizationManager.Get("Text.1F914C4386C0676EE418"),
				Width = 190
			});
			GridStyler.DarkTheme(_grid);
			GridStyler.ApplyDashboardTheme(_grid);
			Controls.Add(_grid);

			_status = new Label
			{
				Name = "playerManagementStatus",
				Text = LocalizationManager.Get("Text.0C10BD130731730DDC5C"),
				Location = new Point(28, 496),
				Size = new Size(844, 48),
				ForeColor = SettingsPalette.SecondaryText,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
			};
			Controls.Add(_status);

			bool minecraftActions = GameDatabase.IsMinecraft(_server.Game) &&
				MinecraftControlProfile.IsJava(_server);
			_kick = CreatePlayerActionButton(
				LocalizationManager.Get("PlayerCenter.Action.Kick"),
				28,
				minecraftActions,
				108);
			_allowlist = CreatePlayerActionButton(
				LocalizationManager.Get("PlayerCenter.Action.Allowlist"),
				148,
				minecraftActions,
				148);
			_operator = CreatePlayerActionButton(
				LocalizationManager.Get("PlayerCenter.Action.Operator"),
				308,
				minecraftActions,
				148);
			_kick.Click += async (_, _) => await RunMinecraftPlayerCommandAsync(
				"kick",
				"PlayerCenter.Confirm.Kick");
			_allowlist.Click += async (_, _) => await RunMinecraftPlayerCommandAsync(
				"whitelist add",
				"PlayerCenter.Confirm.Allowlist");
			_operator.Click += async (_, _) => await RunMinecraftPlayerCommandAsync(
				"op",
				"PlayerCenter.Confirm.Operator");
			Controls.AddRange([_kick, _allowlist, _operator]);
			_grid.SelectionChanged += (_, _) => UpdateMinecraftActionState();

			_refresh = new ModernSettingsButton
			{
				Name = "playerManagementRefresh",
				Text = LocalizationManager.Get("Text.BF53C2057CB18B677699"),
				Location = new Point(566, 560),
				Size = new Size(148, 44),
				UseAccentStyle = true,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			};
			_refresh.Click += async (_, _) => await RefreshPlayersAsync();
			ModernSettingsButton close = new()
			{
				Name = "playerManagementClose",
				Text = LocalizationManager.Get("ModManager.Button.Close"),
				Location = new Point(726, 560),
				Size = new Size(146, 44),
				DialogResult = DialogResult.OK,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			};
			Controls.AddRange([_refresh, close]);
			CancelButton = close;
			ThemeManager.Apply(this);
			UpdateMinecraftActionState();
		}

		protected override async void OnShown(EventArgs eventArgs)
		{
			base.OnShown(eventArgs);
			await RefreshPlayersAsync();
		}

		private async Task RefreshPlayersAsync()
		{
			_refresh.Enabled = false;
			_status.Text = LocalizationManager.Get("PlayerCenter.Loading");
			try
			{
				PlayerQueryResult result = await PlayerQueryService.QueryAsync(_server);
				_grid.Rows.Clear();
				foreach (GamePlayerInfo player in result.Players)
				{
					int row = _grid.Rows.Add(player.Name, player.Score, FormatDuration(player.ConnectedFor));
					_grid.Rows[row].Tag = player;
				}
				_summary.Text = BuildSummary(result.Players.Count);
				_status.Text = LocalizationManager.TranslateRuntimeText(
					result.Message) + (result.IsSupported
					? GameDatabase.IsMinecraft(_server.Game)
						? LocalizationManager.Get(
							"PlayerCenter.Guidance.Minecraft")
						: LocalizationManager.Get(
							"PlayerCenter.Guidance.UnsupportedActions")
					: string.Empty);
				_status.ForeColor = result.IsSuccessful
					? SettingsPalette.Success
					: result.IsSupported ? SettingsPalette.Warning : SettingsPalette.SecondaryText;
			}
			finally
			{
				_refresh.Enabled = true;
				UpdateMinecraftActionState();
			}
		}

		private ModernSettingsButton CreatePlayerActionButton(
			string text,
			int left,
			bool visible,
			int width)
		{
			return new ModernSettingsButton
			{
				Name = "playerManagement" + new string(
					text.Where(char.IsAsciiLetterOrDigit).ToArray()),
				Text = text,
				Location = new Point(left, 560),
				Size = new Size(width, 44),
				Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
				Visible = visible,
				Enabled = false
			};
		}

		private void UpdateMinecraftActionState()
		{
			bool enabled = _grid.SelectedRows.Count == 1 && _refresh.Enabled;
			_kick.Enabled = _kick.Visible && enabled;
			_allowlist.Enabled = _allowlist.Visible && enabled;
			_operator.Enabled = _operator.Visible && enabled;
		}

		private async Task RunMinecraftPlayerCommandAsync(
			string command,
			string confirmationResourceKey)
		{
			if (_grid.SelectedRows.Count != 1 ||
				_grid.SelectedRows[0].Tag is not GamePlayerInfo player ||
				!MinecraftRconClient.IsSafePlayerName(player.Name))
			{
				_status.Text = LocalizationManager.Get(
					"PlayerCenter.SelectValidPlayer");
				_status.ForeColor = SettingsPalette.Warning;
				return;
			}

			if (LocalizedMessageBox.Show(
				this,
				LocalizationManager.Get(
					confirmationResourceKey,
					player.Name),
				LocalizationManager.Get("PlayerCenter.Confirm.Title"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return;
			}

			(bool succeeded, string message) = await Servers.SendMinecraftCommandAsync(
				_server,
				$"{command} {player.Name}");
			_status.Text = message;
			_status.ForeColor = succeeded ? SettingsPalette.Success : SettingsPalette.Warning;
			if (succeeded && command == "kick")
				await RefreshPlayersAsync();
		}

		private static string FormatDuration(TimeSpan duration) =>
			LocalizationManager.Get(
				duration.TotalHours >= 1
					? "PlayerCenter.Duration.Hours"
					: "PlayerCenter.Duration.Minutes",
				duration.TotalHours >= 1
					? (int)duration.TotalHours
					: duration.Minutes,
				duration.TotalHours >= 1
					? duration.Minutes
					: duration.Seconds);

		private string BuildSummary(int playerCount) =>
			LocalizationManager.Get(
				playerCount == 1
					? "PlayerCenter.Summary.One"
					: "PlayerCenter.Summary.Many",
				_server.ServerName,
				_server.Game,
				playerCount);
	}
}
