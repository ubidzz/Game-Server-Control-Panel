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
using System.Runtime.InteropServices;

namespace Synix_Control_Panel.SynixApp.Design
{
	public static class SynixMenuStyler
	{
		private const int DwmWindowCornerPreference = 33;
		private const int DwmCornerPreferenceRound = 2;
		private static readonly Font MenuFont = new("Segoe UI", 11F, FontStyle.Regular);

		[DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
		private static extern void DwmSetWindowAttribute(
			IntPtr window,
			int attribute,
			ref int value,
			uint valueSize);

		public static void Apply(ToolStripDropDown menu)
		{
			ArgumentNullException.ThrowIfNull(menu);
			menu.Renderer = new SynixMenuRenderer();
			menu.BackColor = SettingsPalette.Card;
			menu.ForeColor = SettingsPalette.PrimaryText;
			menu.Font = MenuFont;
			if (menu is ContextMenuStrip contextMenu)
				contextMenu.ShowImageMargin = false;

			ApplyRoundedWindow(menu);
			foreach (ToolStripItem item in menu.Items)
			{
				item.BackColor = SettingsPalette.Card;
				item.ForeColor = SettingsPalette.PrimaryText;
				item.Padding = new Padding(0, 4, 0, 4);
				if (item is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
					Apply(dropDownItem.DropDown);
			}
		}

		private static void ApplyRoundedWindow(ToolStripDropDown menu)
		{
			void ApplyDwm()
			{
				if (Environment.OSVersion.Version.Build < 22000)
					return;
				try
				{
					int preference = DwmCornerPreferenceRound;
					DwmSetWindowAttribute(
						menu.Handle,
						DwmWindowCornerPreference,
						ref preference,
						sizeof(int));
				}
				catch (ExternalException)
				{
					// Windows can reject this visual hint; the renderer still supplies the theme.
				}
			}

			if (menu.IsHandleCreated)
				ApplyDwm();
			else
				menu.HandleCreated += (_, _) => ApplyDwm();
		}
	}
}
