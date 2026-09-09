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
namespace Synix_Control_Panel.SynixApp.UI.ServerSetup;

public partial class ServerSettingsReviewPage : UserControl
{
	public ServerSettingsReviewPage() => InitializeComponent();

	internal void SetSummary(IEnumerable<(string ResourceKey, string Value)> settings)
	{
		gridSummary.Rows.Clear();
		foreach ((string resourceKey, string value) in settings)
			gridSummary.Rows.Add(LocalizationManager.Get(resourceKey), value);
		gridSummary.ClearSelection();
		// Values are data-grid cells, not localizable labels: names, paths, map names
		// and game identifiers must remain exactly as the user entered them.
	}
}
