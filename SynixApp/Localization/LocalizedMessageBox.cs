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
/// Keeps confirmation and error dialogs on the selected interface language.
/// Variable server data and technical exception details remain unchanged.
/// </summary>
internal static class LocalizedMessageBox
{
	public static DialogResult Show(string text) =>
		MessageBox.Show(LocalizationManager.TranslateMessageText(text));

	public static DialogResult Show(string text, string caption) =>
		MessageBox.Show(
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption));

	public static DialogResult Show(
		string text,
		string caption,
		MessageBoxButtons buttons) =>
		MessageBox.Show(
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption),
			buttons);

	public static DialogResult Show(
		string text,
		string caption,
		MessageBoxButtons buttons,
		MessageBoxIcon icon) =>
		MessageBox.Show(
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption),
			buttons,
			icon);

	public static DialogResult Show(
		string text,
		string caption,
		MessageBoxButtons buttons,
		MessageBoxIcon icon,
		MessageBoxDefaultButton defaultButton) =>
		MessageBox.Show(
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption),
			buttons,
			icon,
			defaultButton);

	public static DialogResult Show(
		string text,
		string caption,
		MessageBoxButtons buttons,
		MessageBoxIcon icon,
		MessageBoxDefaultButton defaultButton,
		MessageBoxOptions options) =>
		MessageBox.Show(
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption),
			buttons,
			icon,
			defaultButton,
			options);

	public static DialogResult Show(
		IWin32Window? owner,
		string text) =>
		MessageBox.Show(
			owner,
			LocalizationManager.TranslateMessageText(text));

	public static DialogResult Show(
		IWin32Window? owner,
		string text,
		string caption) =>
		MessageBox.Show(
			owner,
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption));

	public static DialogResult Show(
		IWin32Window? owner,
		string text,
		string caption,
		MessageBoxButtons buttons) =>
		MessageBox.Show(
			owner,
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption),
			buttons);

	public static DialogResult Show(
		IWin32Window? owner,
		string text,
		string caption,
		MessageBoxButtons buttons,
		MessageBoxIcon icon) =>
		MessageBox.Show(
			owner,
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption),
			buttons,
			icon);

	public static DialogResult Show(
		IWin32Window? owner,
		string text,
		string caption,
		MessageBoxButtons buttons,
		MessageBoxIcon icon,
		MessageBoxDefaultButton defaultButton) =>
		MessageBox.Show(
			owner,
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption),
			buttons,
			icon,
			defaultButton);

	public static DialogResult Show(
		IWin32Window? owner,
		string text,
		string caption,
		MessageBoxButtons buttons,
		MessageBoxIcon icon,
		MessageBoxDefaultButton defaultButton,
		MessageBoxOptions options) =>
		MessageBox.Show(
			owner,
			LocalizationManager.TranslateMessageText(text),
			LocalizationManager.TranslateMessageText(caption),
			buttons,
			icon,
			defaultButton,
			options);
}
