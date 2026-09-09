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
using Synix_Control_Panel.SynixApp.Design.Controls;
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixApp.UI.ServerSetup
{
	public partial class ServerSettingsWorldPage : UserControl
	{
		private bool _isLoading;
		private bool _digitsOnlySeed;
		private GameInfo? _gameDefinition;
		private bool _seedSupported;

		public event EventHandler? SettingsChanged;

		public ServerSettingsWorldPage()
		{
			InitializeComponent();
			btnGenerateSeed.Click += GenerateSeedClicked;
			txtWorldSeed.KeyPress += WorldSeedKeyPress;
			txtWorldSeed.TextChanged += SettingsControlChanged;
			numWorldSize.ValueChanged += SettingsControlChanged;
		}

		public string WorldSeed => ReadManagedValue(txtWorldSeed).Trim();
		public int WorldSize => (int)numWorldSize.Value;
		public bool TryValidate(out string error) =>
			GameServerInputValidator.TryValidateWorldSeed(_gameDefinition, WorldSeed, out error);

		public void FocusWorldSeed()
		{
			txtWorldSeed.Focus();
			txtWorldSeed.SelectAll();
		}

		public void EnsureRandomSeedForNewServer()
		{
			if (_seedSupported && string.IsNullOrWhiteSpace(WorldSeed))
				txtWorldSeed.Text = GameServerInputValidator.GenerateWorldSeed();
		}

		public void LoadServer(GameServer server, GameInfo? gameData)
		{
			ArgumentNullException.ThrowIfNull(server);
			_isLoading = true;
			try
			{
				ConfigureForGame(gameData, isMinecraftBedrock: false);
				txtWorldSeed.Text = server.WorldSeed ?? string.Empty;
				int worldSize = IsSevenDaysToDie(gameData)
					? SevenDaysToDieConfiguration.NormalizeWorldSize(server.WorldSize)
					: server.WorldSize;
				numWorldSize.Value = Math.Clamp(
					worldSize,
					numWorldSize.Minimum,
					numWorldSize.Maximum);
			}
			finally
			{
				_isLoading = false;
			}
		}

		public void ConfigureForGame(GameInfo? gameData, bool isMinecraftBedrock)
		{
			_isLoading = true;
			try
			{
				_gameDefinition = gameData;
				ConfigureWorldSizeInput(gameData);
				_digitsOnlySeed = gameData?.Game.Equals(
					"Rust",
					StringComparison.OrdinalIgnoreCase) == true;

				GameManagementCapability capabilities = gameData == null
					? GameManagementCapability.None
					: GameFix.GetManagementCapabilities(gameData);
				if (isMinecraftBedrock)
				{
					capabilities |= GameManagementCapability.WorldSeed;
				}

				bool seedSupported =
					(capabilities & GameManagementCapability.WorldSeed) != 0;
				_seedSupported = seedSupported;
				bool sizeSupported =
					(capabilities & GameManagementCapability.WorldSize) != 0;
				bool numericSeedRequired = GameServerInputValidator.RequiresNumericWorldSeed(gameData);
				bool canGenerateSeed = seedSupported;
				btnGenerateSeed.Visible = canGenerateSeed;
				txtWorldSeed.Width = canGenerateSeed
					? btnGenerateSeed.Left - txtWorldSeed.Left - LogicalToDeviceUnits(12)
					: lblWorldSize.Left - txtWorldSeed.Left - LogicalToDeviceUnits(24);
				_digitsOnlySeed |= numericSeedRequired &&
					GameServerInputValidator.GetNumericWorldSeedMinimum(gameData?.Game) >= 0;
				LocalizationManager.BindText(lblWorldSeed, numericSeedRequired
					? "ServerSetup.World.Seed.Required" : "Text.33DED6ECB268AFB09FD2");
				lblWorldSeedHint.Visible = seedSupported;
				if (numericSeedRequired)
					LocalizationManager.BindText(lblWorldSeedHint, "ServerSetup.World.Seed.RangeHint",
						GameServerInputValidator.GetNumericWorldSeedMinimum(gameData?.Game).ToString(System.Globalization.CultureInfo.InvariantCulture),
						GameServerInputValidator.NumericWorldSeedMaximum.ToString(System.Globalization.CultureInfo.InvariantCulture));
				else
					LocalizationManager.BindText(lblWorldSeedHint, "ServerSetup.World.Seed.Hint");
				ConfigureManagedTextBox(
					txtWorldSeed,
					seedSupported,
					LocalizationManager.Get(gameData == null
						? "ServerSetup.Placeholder.SelectGame"
						: "ServerSetup.Placeholder.NotRequired"));
				numWorldSize.Tag = sizeSupported;
			}
			finally
			{
				_isLoading = false;
			}
		}

		public void ApplyDefaultWorldSize(GameInfo gameData)
		{
			ArgumentNullException.ThrowIfNull(gameData);
			ConfigureWorldSizeInput(gameData);
			int worldSize = IsSevenDaysToDie(gameData)
				? SevenDaysToDieConfiguration.NormalizeWorldSize(gameData.WorldSize)
				: gameData.WorldSize;
			if (worldSize > 0)
			{
				numWorldSize.Value = Math.Clamp(
					worldSize,
					numWorldSize.Minimum,
					numWorldSize.Maximum);
			}
		}

		public void ApplyAvailability(bool hasGame)
		{
			txtWorldSeed.Enabled = hasGame && IsRequired(txtWorldSeed);
			btnGenerateSeed.Enabled = hasGame && _seedSupported;
			numWorldSize.Enabled = hasGame && numWorldSize.Tag is true;
		}

		private void GenerateSeedClicked(object? sender, EventArgs eventArgs)
		{
			if (!_seedSupported)
				return;

			// Only fills the editable setting. Saving and any world changes stay explicit.
			txtWorldSeed.Text = GameServerInputValidator.GenerateWorldSeed();
			FocusWorldSeed();
		}

		public static bool IsSevenDaysToDie(GameInfo? gameData) =>
			gameData?.Game.Equals(
				"7 Days to Die",
				StringComparison.OrdinalIgnoreCase) == true;

		private void ConfigureWorldSizeInput(GameInfo? gameData)
		{
			if (IsSevenDaysToDie(gameData))
			{
				numWorldSize.Maximum = 10240;
				numWorldSize.Minimum = 6144;
				numWorldSize.Increment = 2048;
				return;
			}

			numWorldSize.Minimum = 50;
			numWorldSize.Maximum = 5000;
			numWorldSize.Increment = 1;
		}

		private void WorldSeedKeyPress(object? sender, KeyPressEventArgs eventArgs)
		{
			if (_digitsOnlySeed &&
				!char.IsControl(eventArgs.KeyChar) &&
				!char.IsDigit(eventArgs.KeyChar))
			{
				eventArgs.Handled = true;
			}
		}

		private void SettingsControlChanged(object? sender, EventArgs eventArgs)
		{
			if (!_isLoading)
				SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		private static string ReadManagedValue(TextBox textBox)
		{
			return textBox.Tag is ManagedTextBoxState { Required: false } state &&
				(textBox.ForeColor == Color.Gray || textBox.Text == state.Placeholder)
					? string.Empty
					: textBox.Text;
		}

		private static bool IsRequired(TextBox textBox) =>
			textBox.Tag is ManagedTextBoxState { Required: true };

		private static void ConfigureManagedTextBox(
			TextBox textBox,
			bool required,
			string placeholder)
		{
			textBox.GotFocus -= ManagedTextBoxGotFocus;
			textBox.LostFocus -= ManagedTextBoxLostFocus;
			textBox.Tag = new ManagedTextBoxState(required, placeholder);
			if (required)
			{
				if (textBox.ForeColor == Color.Gray)
				{
					textBox.Text = string.Empty;
				}
				textBox.ForeColor = SettingsPalette.PrimaryText;
				return;
			}

			textBox.ForeColor = Color.Gray;
			textBox.Text = placeholder;
			textBox.GotFocus += ManagedTextBoxGotFocus;
			textBox.LostFocus += ManagedTextBoxLostFocus;
		}

		private static void ManagedTextBoxGotFocus(object? sender, EventArgs eventArgs)
		{
			if (sender is TextBox textBox &&
				textBox.Tag is ManagedTextBoxState state &&
				textBox.Text == state.Placeholder)
			{
				textBox.Text = string.Empty;
				textBox.ForeColor = SettingsPalette.PrimaryText;
			}
		}

		private static void ManagedTextBoxLostFocus(object? sender, EventArgs eventArgs)
		{
			if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
			{
				textBox.ForeColor = Color.Gray;
				textBox.Text = textBox.Tag is ManagedTextBoxState state
					? state.Placeholder
					: string.Empty;
			}
		}

		private sealed record ManagedTextBoxState(
			bool Required,
			string Placeholder);
	}
}
