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

namespace Synix_Control_Panel.SynixApp.UI.Diagnostics
{
	internal sealed class PlainEnglishErrorDialog : Form
	{
		private readonly PlainEnglishError _error;
		private readonly TextBox _technicalBox;
		private readonly Button _detailsButton;

		internal PlainEnglishErrorDialog(string operation, string? technicalDetails)
		{
			_error = UserGuidance.TranslateError(operation, technicalDetails);
			Text = _error.Heading;
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = false;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			ClientSize = new Size(690, 390);
			BackColor = SettingsPalette.Window;
			ForeColor = SettingsPalette.PrimaryText;
			Font = new Font("Segoe UI", 9.5F);

			Label title = new()
			{
				Text = _error.Heading,
				Font = new Font("Segoe UI", 18F, FontStyle.Bold),
				ForeColor = SettingsPalette.PrimaryText,
				Location = new Point(28, 24),
				Size = new Size(630, 42)
			};
			Label explanationHeading = CreateHeading(
				LocalizationManager.Get("Text.483BD49023AE2BFB08DA"),
				82);
			Label explanation = CreateBody(_error.Explanation, 108, 50);
			Label nextHeading = CreateHeading(
				LocalizationManager.Get("Diagnostics.Error.WhatToDoNext"),
				170);
			Label next = CreateBody(_error.NextStep, 196, 66);

			_technicalBox = new TextBox
			{
				Multiline = true,
				ReadOnly = true,
				ScrollBars = ScrollBars.Vertical,
				Text = _error.TechnicalDetails,
				BackColor = SettingsPalette.Input,
				ForeColor = SettingsPalette.SecondaryText,
				BorderStyle = BorderStyle.FixedSingle,
				Location = new Point(28, 276),
				Size = new Size(632, 64),
				Visible = false
			};

			_detailsButton = new ModernSettingsButton
			{
				Text = LocalizationManager.Get("Text.9C3A965A650194E6FECC"),
				Location = new Point(28, 300),
				Size = new Size(190, 42)
			};
			_detailsButton.Click += (_, _) => ToggleDetails();

			ModernSettingsButton copyButton = new()
			{
				Text = LocalizationManager.Get("Text.C6BB8A5950C5AD175A37"),
				Location = new Point(376, 300),
				Size = new Size(130, 42)
			};
			copyButton.Click += (_, _) =>
			{
				try { Clipboard.SetText(_error.TechnicalDetails); }
				catch (Exception suppressedException)
				{
					Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
				}
			};

			ModernSettingsButton closeButton = new()
			{
				Text = LocalizationManager.Get("ModManager.Button.Close"),
				Location = new Point(518, 300),
				Size = new Size(142, 42),
				DialogResult = DialogResult.OK,
				UseAccentStyle = true
			};

			Controls.AddRange([
				title, explanationHeading, explanation, nextHeading, next,
				_technicalBox, _detailsButton, copyButton, closeButton]);
			AcceptButton = closeButton;
			CancelButton = closeButton;
			ThemeManager.Apply(this);
		}

		internal static void ShowError(
			IWin32Window? owner,
			string operation,
			string? technicalDetails)
		{
			if (owner is Control control && control.InvokeRequired)
			{
				if (!control.IsDisposed && control.IsHandleCreated)
				{
					control.BeginInvoke((Action)(() =>
						ShowError(control, operation, technicalDetails)));
				}
				return;
			}
			using PlainEnglishErrorDialog dialog = new(operation, technicalDetails);
			dialog.ShowDialog(owner);
		}

		private Label CreateHeading(string text, int top) => new()
		{
			Text = text,
			Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
			ForeColor = SettingsPalette.Accent,
			Location = new Point(28, top),
			Size = new Size(632, 24)
		};

		private Label CreateBody(string text, int top, int height) => new()
		{
			Text = text,
			ForeColor = SettingsPalette.PrimaryText,
			Location = new Point(28, top),
			Size = new Size(632, height)
		};

		private void ToggleDetails()
		{
			bool show = !_technicalBox.Visible;
			_technicalBox.Visible = show;
			LocalizationManager.BindText(
				_detailsButton,
				show
					? "DynamicText.9380C57621B46B84DD91"
					: "Text.9C3A965A650194E6FECC");
			_detailsButton.Location = new Point(28, show ? 348 : 300);
			foreach (Control control in Controls.OfType<Button>().Where(control => !ReferenceEquals(control, _detailsButton)))
				control.Top = show ? 348 : 300;
			ClientSize = new Size(690, show ? 438 : 390);
		}
	}
}
