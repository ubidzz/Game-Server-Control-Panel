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

namespace Synix_Control_Panel.SynixApp.UI.Onboarding
{
	internal enum AddServerChoice
	{
		None,
		CreateNew,
		ImportExisting,
		BrowseCatalog
	}

	internal sealed class AddServerChoiceDialog : Form
	{
		internal AddServerChoice SelectedChoice { get; private set; }

		internal AddServerChoiceDialog()
		{
			Text = LocalizationManager.Get("AddServer.Title");
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = false;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			ClientSize = new Size(720, 430);
			BackColor = SettingsPalette.Window;
			ForeColor = SettingsPalette.PrimaryText;
			Font = new Font("Segoe UI", 9.5F);

			Controls.Add(new Label
			{
				Text = LocalizationManager.Get("AddServer.Heading"),
				Font = new Font("Segoe UI", 19F, FontStyle.Bold),
				Location = new Point(28, 24),
				Size = new Size(660, 42),
				ForeColor = SettingsPalette.PrimaryText
			});
			Controls.Add(new Label
			{
				Text = LocalizationManager.Get("AddServer.Subtitle"),
				Location = new Point(30, 70),
				Size = new Size(650, 28),
				ForeColor = SettingsPalette.SecondaryText
			});

			AddChoiceCard(
				LocalizationManager.Get("AddServer.Create.Title"),
				LocalizationManager.Get("AddServer.Create.Description"),
				112,
				LocalizationManager.Get("AddServer.Create.Button"),
				AddServerChoice.CreateNew,
				true);
			AddChoiceCard(
				LocalizationManager.Get("AddServer.Import.Title"),
				LocalizationManager.Get("AddServer.Import.Description"),
				208,
				LocalizationManager.Get("AddServer.Import.Button"),
				AddServerChoice.ImportExisting,
				false);
			AddChoiceCard(
				LocalizationManager.Get("AddServer.Catalog.Title"),
				LocalizationManager.Get("AddServer.Catalog.Description"),
				304,
				LocalizationManager.Get("AddServer.Catalog.Button"),
				AddServerChoice.BrowseCatalog,
				false);

			ThemeManager.Apply(this);
		}

		private void AddChoiceCard(
			string title,
			string description,
			int top,
			string buttonText,
			AddServerChoice choice,
			bool accent)
		{
			ModernSettingsCard card = new()
			{
				Location = new Point(28, top),
				Size = new Size(664, 80),
				FillColor = SettingsPalette.Card,
				BorderColor = SettingsPalette.Divider
			};
			card.Controls.Add(new Label
			{
				Text = title,
				Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
				Location = new Point(18, 12),
				Size = new Size(430, 24),
				ForeColor = SettingsPalette.PrimaryText
			});
			card.Controls.Add(new Label
			{
				Text = description,
				Location = new Point(18, 39),
				Size = new Size(440, 34),
				ForeColor = SettingsPalette.SecondaryText
			});
			ModernSettingsButton button = new()
			{
				Text = buttonText,
				Location = new Point(486, 19),
				Size = new Size(158, 42),
				UseAccentStyle = accent
			};
			button.Click += (_, _) =>
			{
				SelectedChoice = choice;
				DialogResult = DialogResult.OK;
				Close();
			};
			card.Controls.Add(button);
			Controls.Add(card);
		}
	}
}
