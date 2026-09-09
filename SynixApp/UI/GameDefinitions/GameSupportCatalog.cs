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
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixApp.UI.GameDefinitions
{
	internal sealed class GameSupportCatalog : Form
	{
		private readonly TextBox _search;
		private readonly DataGridView _grid;
		private readonly Label _count;
		private readonly ModernSettingsButton _viewDetails;
		private readonly ModernSettingsButton _clearFilters;
		private readonly ModernSettingsComboBox _nameFilter;
		private readonly ModernSettingsComboBox _sortFilter;
		private readonly ModernSettingsComboBox _compatibilityFilter;
		private readonly ModernSettingsComboBox _configurationFilter;
		private readonly ModernSettingsComboBox _playerFilter;
		private readonly ModernSettingsComboBox _crossplayFilter;
		private readonly ModernSettingsComboBox _programFilter;
		private readonly ModernSettingsComboBox _verificationFilter;
		private readonly IReadOnlyList<GameSupportRow> _allRows;
		private bool _changingFilters;

		internal GameSupportCatalog()
		{
			Text = LocalizationManager.Get("Text.B5847872D81BE16B1771");
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimumSize = new Size(980, 600);
			ClientSize = new Size(1180, 720);
			BackColor = SettingsPalette.Window;
			ForeColor = SettingsPalette.PrimaryText;
			Font = new Font("Segoe UI", 9.5F);
			_allRows = GameDatabase.GetGameList().Select(CreateRow).ToArray();

			Controls.Add(new Label
			{
				Text = LocalizationManager.Get("Text.B5847872D81BE16B1771"),
				Font = new Font("Segoe UI", 19F, FontStyle.Bold),
				Location = new Point(28, 22),
				Size = new Size(620, 42),
				ForeColor = SettingsPalette.PrimaryText
			});
			Controls.Add(new Label
			{
				Text = LocalizationManager.Get("Text.8C5A71A792CF70840F4B"),
				Location = new Point(30, 66),
				Size = new Size(840, 28),
				ForeColor = SettingsPalette.SecondaryText
			});
			_search = new TextBox
			{
				Location = new Point(28, 108),
				Size = new Size(500, 38),
				BackColor = SettingsPalette.Input,
				ForeColor = SettingsPalette.PrimaryText,
				BorderStyle = BorderStyle.FixedSingle,
				Font = new Font("Segoe UI", 10F),
				PlaceholderText = LocalizationManager.Get("Text.5D719633D44DEE05A861"),
				Anchor = AnchorStyles.Top | AnchorStyles.Left
			};
			Controls.Add(_search);
			_count = new Label
			{
				Location = new Point(548, 113),
				Size = new Size(420, 28),
				ForeColor = SettingsPalette.SecondaryText,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
			};
			Controls.Add(_count);
			_clearFilters = new ModernSettingsButton
			{
				Name = "clearCatalogFilters",
				Text = LocalizationManager.Get("Text.5776503C1A52F4FA028F"),
				Location = new Point(1002, 104),
				Size = new Size(150, 40),
				Anchor = AnchorStyles.Top | AnchorStyles.Right
			};
			Controls.Add(_clearFilters);

			_nameFilter = CreateFilterBox();
			_sortFilter = CreateFilterBox();
			_compatibilityFilter = CreateFilterBox();
			_configurationFilter = CreateFilterBox();
			_playerFilter = CreateFilterBox();
			_crossplayFilter = CreateFilterBox();
			_programFilter = CreateFilterBox();
			_verificationFilter = CreateFilterBox();
			_nameFilter.Name = "catalogNameFilter";
			_sortFilter.Name = "catalogSortFilter";
			_compatibilityFilter.Name = "catalogCompatibilityFilter";
			_configurationFilter.Name = "catalogConfigurationFilter";
			_playerFilter.Name = "catalogPlayerFilter";
			_crossplayFilter.Name = "catalogCrossplayFilter";
			_programFilter.Name = "catalogProgramFilter";
			_verificationFilter.Name = "catalogVerificationFilter";
			PopulateFilterChoices();

			TableLayoutPanel filterPanel = new()
			{
				Location = new Point(24, 154),
				Size = new Size(1132, 82),
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				ColumnCount = 4,
				RowCount = 2,
				BackColor = SettingsPalette.Window,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
			filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
			filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
			filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
			filterPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
			filterPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
			filterPanel.Controls.Add(_nameFilter, 0, 0);
			filterPanel.Controls.Add(_sortFilter, 1, 0);
			filterPanel.Controls.Add(_compatibilityFilter, 2, 0);
			filterPanel.Controls.Add(_configurationFilter, 3, 0);
			filterPanel.Controls.Add(_playerFilter, 0, 1);
			filterPanel.Controls.Add(_crossplayFilter, 1, 1);
			filterPanel.Controls.Add(_programFilter, 2, 1);
			filterPanel.Controls.Add(_verificationFilter, 3, 1);
			Controls.Add(filterPanel);

			_grid = new DataGridView
			{
				Location = new Point(28, 250),
				Size = new Size(1124, 390),
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				ReadOnly = true,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				AutoGenerateColumns = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				MultiSelect = false,
				RowHeadersVisible = false,
				ScrollBars = ScrollBars.Both,
				AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
				RowTemplate = { Height = 42 }
			};
			AddColumn("Game", LocalizationManager.Get("Text.1FD5F5A768D30B9B6651"), 250, true);
			AddColumn("Compatibility", LocalizationManager.Get("Catalog.Column.Compatibility"), 190);
			AddColumn("Configuration", LocalizationManager.Get("Text.D5CDE76290BF3E730FE4"), 210);
			AddColumn("PlayerData", LocalizationManager.Get("Catalog.Column.PlayerDetails"), 155);
			AddColumn("Crossplay", LocalizationManager.Get("Text.444D9B21E5BAAF8243D5"), 110);
			AddColumn("Executable", LocalizationManager.Get("Catalog.Column.ServerProgram"), 240);
			AddColumn("LastVerified", LocalizationManager.Get("Text.74E5C919B46AE61321E3"), 130);
			GridStyler.DarkTheme(_grid);
			GridStyler.ApplyDashboardTheme(_grid);
			_grid.AllowUserToResizeColumns = true;
			_grid.CellDoubleClick += GridCellDoubleClick;
			_grid.CellMouseEnter += GridCellMouseEnter;
			_grid.SelectionChanged += (_, _) => UpdateDetailsButton();
			_grid.KeyDown += GridKeyDown;
			Controls.Add(_grid);

			_viewDetails = new ModernSettingsButton
			{
				Text = LocalizationManager.Get("Text.90789C12D0731B4405F2"),
				Location = new Point(836, 656),
				Size = new Size(156, 44),
				UseAccentStyle = true,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			};
			_viewDetails.Click += (_, _) => OpenSelectedGameDetails();
			Controls.Add(_viewDetails);

			ModernSettingsButton close = new()
			{
				Text = LocalizationManager.Get("ModManager.Button.Close"),
				Location = new Point(1002, 656),
				Size = new Size(150, 44),
				DialogResult = DialogResult.OK,
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right
			};
			Controls.Add(close);
			AcceptButton = _viewDetails;
			CancelButton = close;
			_search.TextChanged += FilterChanged;
			_nameFilter.SelectedIndexChanged += FilterChanged;
			_sortFilter.SelectedIndexChanged += FilterChanged;
			_compatibilityFilter.SelectedIndexChanged += FilterChanged;
			_configurationFilter.SelectedIndexChanged += FilterChanged;
			_playerFilter.SelectedIndexChanged += FilterChanged;
			_crossplayFilter.SelectedIndexChanged += FilterChanged;
			_programFilter.SelectedIndexChanged += FilterChanged;
			_verificationFilter.SelectedIndexChanged += FilterChanged;
			_clearFilters.Click += (_, _) => ClearFilters();
			ApplyFilter();
			ThemeManager.Apply(this);
		}

		private static ModernSettingsComboBox CreateFilterBox() => new()
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(4, 3, 4, 3),
			MaxDropDownItems = 12
		};

		private void PopulateFilterChoices()
		{
			List<CatalogFilterChoice> nameChoices =
			[
				new(LocalizationManager.Get("Catalog.Filter.NameAll"), null)
			];
			nameChoices.AddRange(Enumerable.Range('A', 26).Select(character =>
				new CatalogFilterChoice(
					LocalizationManager.Get("Catalog.Filter.NameStarts", (char)character),
					((char)character).ToString())));
			nameChoices.Add(new(
				LocalizationManager.Get("Catalog.Filter.NameNumbers"),
				GameSupportCatalogFilterEngine.Numbers));
			nameChoices.Add(new(
				LocalizationManager.Get("Catalog.Filter.NameOther"),
				GameSupportCatalogFilterEngine.Other));
			SetChoices(_nameFilter, nameChoices);
			SetChoices(_sortFilter,
			[
				new(LocalizationManager.Get("Catalog.Filter.SortAscending"), "ascending"),
				new(LocalizationManager.Get("Catalog.Filter.SortDescending"), "descending")
			]);
			SetChoices(
				_compatibilityFilter,
				CreateValueChoices(LocalizationManager.Get("Catalog.Filter.Compatibility"), _allRows.Select(row => row.Compatibility)));
			SetChoices(
				_configurationFilter,
				CreateValueChoices(LocalizationManager.Get("Text.D5CDE76290BF3E730FE4"), _allRows.Select(row => row.Configuration)));
			SetChoices(
				_playerFilter,
				CreateValueChoices(LocalizationManager.Get("Catalog.Column.PlayerDetails"), _allRows.Select(row => row.PlayerData)));
			SetChoices(
				_crossplayFilter,
				CreateValueChoices(LocalizationManager.Get("Text.444D9B21E5BAAF8243D5"), _allRows.Select(row => row.Crossplay)));
			SetChoices(
				_programFilter,
				CreateValueChoices(LocalizationManager.Get("Catalog.Column.ServerProgram"), _allRows.Select(row => row.Executable)));
			SetChoices(
				_verificationFilter,
				CreateValueChoices(LocalizationManager.Get("Text.74E5C919B46AE61321E3"), _allRows.Select(row => row.LastVerified)));
		}

		private static IReadOnlyList<CatalogFilterChoice> CreateValueChoices(
			string heading,
			IEnumerable<string> values)
		{
			List<CatalogFilterChoice> choices =
			[
				new(LocalizationManager.Get("Catalog.Filter.All", heading), null)
			];
			choices.AddRange(values
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
				.Select(value => new CatalogFilterChoice(
					LocalizationManager.Get("Catalog.Filter.Value", heading, value),
					value)));
			return choices;
		}

		private static void SetChoices(
			ModernSettingsComboBox comboBox,
			IReadOnlyList<CatalogFilterChoice> choices)
		{
			comboBox.DisplayMember = nameof(CatalogFilterChoice.DisplayName);
			comboBox.Items.Clear();
			comboBox.Items.AddRange(choices.Cast<object>().ToArray());
			comboBox.SelectedIndex = 0;
		}

		private void AddColumn(
			string property,
			string heading,
			int width,
			bool frozen = false)
		{
			_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = property,
				HeaderText = heading,
				Name = property,
				Width = width,
				MinimumWidth = Math.Min(width, 120),
				AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
				Frozen = frozen
			});
		}

		private void ApplyFilter()
		{
			if (_changingFilters)
				return;

			GameSupportCatalogFilter filter = new(
				_search.Text.Trim(),
				SelectedValue(_nameFilter),
				SelectedValue(_compatibilityFilter),
				SelectedValue(_configurationFilter),
				SelectedValue(_playerFilter),
				SelectedValue(_crossplayFilter),
				SelectedValue(_programFilter),
				SelectedValue(_verificationFilter),
				SelectedValue(_sortFilter) == "descending");
			GameSupportRow[] visible = GameSupportCatalogFilterEngine.Apply(
				_allRows,
				filter);
			_grid.DataSource = visible;
			LocalizationManager.BindText(
				_count,
				"Catalog.Count",
				visible.Length,
				_allRows.Count);
			_clearFilters.Enabled = filter.HasActiveFilters;
			UpdateDetailsButton();
		}

		private static string? SelectedValue(ComboBox comboBox) =>
			(comboBox.SelectedItem as CatalogFilterChoice)?.Value;

		private void FilterChanged(object? sender, EventArgs eventArgs) => ApplyFilter();

		private void ClearFilters()
		{
			_changingFilters = true;
			try
			{
				_search.Clear();
				_nameFilter.SelectedIndex = 0;
				_sortFilter.SelectedIndex = 0;
				_compatibilityFilter.SelectedIndex = 0;
				_configurationFilter.SelectedIndex = 0;
				_playerFilter.SelectedIndex = 0;
				_crossplayFilter.SelectedIndex = 0;
				_programFilter.SelectedIndex = 0;
				_verificationFilter.SelectedIndex = 0;
			}
			finally
			{
				_changingFilters = false;
			}
			ApplyFilter();
		}

		private void GridCellDoubleClick(object? sender, DataGridViewCellEventArgs eventArgs)
		{
			if (eventArgs.RowIndex >= 0)
				OpenSelectedGameDetails();
		}

		private void GridCellMouseEnter(object? sender, DataGridViewCellEventArgs eventArgs)
		{
			if (eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0)
				return;

			DataGridViewCell cell = _grid.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex];
			string value = Convert.ToString(cell.FormattedValue)?.Trim() ?? string.Empty;
			cell.ToolTipText = string.IsNullOrWhiteSpace(value)
				? LocalizationManager.Get("Catalog.Tooltip.OpenDetails")
				: LocalizationManager.Get("Catalog.Tooltip.Value", value);
		}

		private void GridKeyDown(object? sender, KeyEventArgs eventArgs)
		{
			if (eventArgs.KeyCode != Keys.Enter)
				return;

			eventArgs.Handled = true;
			eventArgs.SuppressKeyPress = true;
			OpenSelectedGameDetails();
		}

		private void OpenSelectedGameDetails()
		{
			if (_grid.CurrentRow?.DataBoundItem is not GameSupportRow row)
				return;

			GameInfo? game = GameDatabase.GetGame(row.Game);
			if (game == null)
				return;

			using GameSupportDetailsDialog dialog = new(game);
			dialog.ShowDialog(this);
		}

		private void UpdateDetailsButton()
		{
			if (_viewDetails != null)
				_viewDetails.Enabled = _grid.CurrentRow?.DataBoundItem is GameSupportRow;
		}

		private static GameSupportRow CreateRow(GameInfo game)
		{
			GameCompatibilitySummary compatibility = Core.GetGameCompatibilitySummary(game.Game);
			ConfigurationSupportPresentation configuration = UserGuidance.GetConfigurationSupport(game);
			GameManagementCapability capabilities = GameFix.GetManagementCapabilities(game);
			bool crossplay = capabilities.HasFlag(GameManagementCapability.Crossplay);
			string playerData = game.CrossplayDisablesPlayerTracking
				? LocalizationManager.Get("Catalog.PlayerData.SteamOnly")
				: GameDatabase.GetProbeProtocol(game) == ServerProbeProtocol.A2S
				? LocalizationManager.Get("Catalog.PlayerData.Named")
				: GameDatabase.IsMinecraft(game.Game)
					? LocalizationManager.Get("Catalog.PlayerData.Count")
					: LocalizationManager.Get("Catalog.NotAvailable");
			string lastVerified = compatibility.Verification.LastTested?.VerifiedAtUtc
				.ToLocalTime().ToString("d", System.Globalization.CultureInfo.CurrentUICulture) ??
				LocalizationManager.Get("Catalog.NotVerified");
			return new(
				game.Game,
				LocalizationManager.TranslateKnownText(compatibility.DisplayName),
				LocalizationManager.TranslateKnownText(configuration.Status),
				playerData,
				LocalizationManager.Get(
					crossplay ? "Catalog.Available" : "Catalog.NotListed"),
				GameDatabase.IsMinecraft(game.Game)
					? "Start.bat / bedrock_server.exe"
					: game.ExeName,
				lastVerified);
		}

	}

	internal sealed record CatalogFilterChoice(string DisplayName, string? Value);

	internal sealed record GameSupportCatalogFilter(
		string Search,
		string? NameGroup,
		string? Compatibility,
		string? Configuration,
		string? PlayerData,
		string? Crossplay,
		string? Executable,
		string? LastVerified,
		bool Descending)
	{
		internal bool HasActiveFilters =>
			!string.IsNullOrWhiteSpace(Search) ||
			NameGroup != null ||
			Compatibility != null ||
			Configuration != null ||
			PlayerData != null ||
			Crossplay != null ||
			Executable != null ||
			LastVerified != null ||
			Descending;
	}

	internal static class GameSupportCatalogFilterEngine
	{
		internal const string Numbers = "0-9";
		internal const string Other = "other";

		internal static GameSupportRow[] Apply(
			IReadOnlyList<GameSupportRow> rows,
			GameSupportCatalogFilter filter)
		{
			IEnumerable<GameSupportRow> visible = rows;
			if (!string.IsNullOrWhiteSpace(filter.Search))
			{
				visible = visible.Where(row => row.SearchText.Contains(
					filter.Search.Trim(),
					StringComparison.OrdinalIgnoreCase));
			}

			if (filter.NameGroup != null)
				visible = visible.Where(row => MatchesNameGroup(row.Game, filter.NameGroup));
			visible = MatchValue(visible, filter.Compatibility, row => row.Compatibility);
			visible = MatchValue(visible, filter.Configuration, row => row.Configuration);
			visible = MatchValue(visible, filter.PlayerData, row => row.PlayerData);
			visible = MatchValue(visible, filter.Crossplay, row => row.Crossplay);
			visible = MatchValue(visible, filter.Executable, row => row.Executable);
			visible = MatchValue(visible, filter.LastVerified, row => row.LastVerified);

			return (filter.Descending
				? visible.OrderByDescending(row => row.Game, StringComparer.CurrentCultureIgnoreCase)
				: visible.OrderBy(row => row.Game, StringComparer.CurrentCultureIgnoreCase))
				.ToArray();
		}

		private static IEnumerable<GameSupportRow> MatchValue(
			IEnumerable<GameSupportRow> rows,
			string? selectedValue,
			Func<GameSupportRow, string> valueSelector)
		{
			return selectedValue == null
				? rows
				: rows.Where(row => valueSelector(row).Equals(
					selectedValue,
					StringComparison.OrdinalIgnoreCase));
		}

		internal static bool MatchesNameGroup(string game, string nameGroup)
		{
			string name = game.TrimStart();
			if (name.Length == 0)
				return nameGroup.Equals(Other, StringComparison.OrdinalIgnoreCase);

			char first = char.ToUpperInvariant(name[0]);
			if (nameGroup.Equals(Numbers, StringComparison.OrdinalIgnoreCase))
				return char.IsDigit(first);
			if (nameGroup.Equals(Other, StringComparison.OrdinalIgnoreCase))
				return !char.IsDigit(first) && (first < 'A' || first > 'Z');

			return nameGroup.Length == 1 && first == char.ToUpperInvariant(nameGroup[0]);
		}
	}

	internal sealed record GameSupportRow(
		string Game,
		string Compatibility,
		string Configuration,
		string PlayerData,
		string Crossplay,
		string Executable,
		string LastVerified)
	{
		internal string SearchText => string.Join(
			' ',
			Game,
			Compatibility,
			Configuration,
			PlayerData,
			Crossplay,
			Executable,
			LastVerified);
	}
}
