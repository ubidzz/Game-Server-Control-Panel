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

namespace Synix_Control_Panel.SynixApp.Localization;

/// <summary>
/// Keeps the value used by Synix separate from the translated text shown to
/// the user. This prevents a language change from altering saved settings,
/// launch arguments, or game configuration values.
/// </summary>
internal sealed class LocalizedOption
{
	private readonly string? _displayText;

	public LocalizedOption(string value, string resourceKey)
		: this(value, resourceKey, null)
	{
	}

	private LocalizedOption(
		string value,
		string? resourceKey,
		string? displayText)
	{
		Value = value;
		_displayText = displayText;
		ResourceKey = resourceKey ?? string.Empty;
	}

	public static LocalizedOption FromDisplayText(
		string value,
		string displayText) =>
		new(value, null, displayText);

	public string Value { get; }

	public string ResourceKey { get; }

	public override string ToString()
	{
		return _displayText ?? LocalizationManager.Get(ResourceKey);
	}
}
