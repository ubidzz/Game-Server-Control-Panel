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
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Synix_Control_Panel.SynixApp.Design.Controls;

namespace Synix_Control_Panel.SynixApp.UI.ServerSetup;

/// <summary>Setup checkpoints only; installation progress belongs to the dashboard.</summary>
public sealed class ServerSetupProgressStrip : UserControl
{
	internal enum StepState { Waiting, Attention, Complete, Next, Ready }
	private readonly StepButton[] _steps;
	public event EventHandler<int>? StepSelected;

	public ServerSetupProgressStrip()
	{
		Size = new Size(914, 92);
		BackColor = SettingsPalette.Window;
		ModernSettingsCard card = new() { Dock = DockStyle.Fill };
		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Fill,
			BackColor = SettingsPalette.Card,
			ColumnCount = 4,
			RowCount = 1,
			Margin = Padding.Empty
		};
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_steps = new StepButton[4];
		for (int index = 0; index < _steps.Length; index++)
		{
			int step = index;
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
			StepButton button = new(index)
			{
				Name = $"btnSetupStep{index + 1}",
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				TabIndex = index
			};
			button.Click += (_, _) => StepSelected?.Invoke(this, step);
			_steps[index] = button;
			layout.Controls.Add(button, index, 0);
		}
		card.Controls.Add(layout);
		Controls.Add(card);
		UpdateState(false, false, false);
	}

	internal void UpdateState(bool detailsReady, bool requirementsReady, bool reviewed, bool editMode = false)
	{
		// Review is optional, so this strip must never add another Save gate.
		requirementsReady &= detailsReady;
		reviewed &= requirementsReady;
		StepState[] states =
		[
			detailsReady ? StepState.Complete : StepState.Attention,
			!detailsReady ? StepState.Waiting : requirementsReady ? StepState.Complete : StepState.Attention,
			!requirementsReady ? StepState.Waiting : reviewed ? StepState.Complete : StepState.Next,
			!requirementsReady ? StepState.Waiting : reviewed ? StepState.Next : StepState.Ready
		];
		string[] titles = ["Details", "Required", "Review", "Save"];
		for (int index = 0; index < _steps.Length; index++)
		{
			StepButton button = _steps[index];
			button.Enabled = index == 0 || (index == 1 ? detailsReady : requirementsReady);
			button.State = states[index];
			LocalizationManager.BindText(button, index == 3 && editMode
				? "ServerSetup.Button.SaveChanges" : $"ServerSetup.Progress.{titles[index]}");
			button.StatusText = LocalizationManager.Get($"ServerSetup.Progress.Status.{states[index]}");
			LocalizationManager.BindAccessibleName(button, "ServerSetup.Progress.Step", index + 1, button.Text, button.StatusText);
			LocalizationManager.BindAccessibleDescription(button, $"ServerSetup.Progress.{titles[index]}.Hint");
			button.Invalidate();
		}
	}

	internal void UpdateAttentionPulse(float pulse)
	{
		foreach (StepButton button in _steps)
		{
			if (button.State != StepState.Attention)
				continue;
			button.Pulse = pulse;
			button.Invalidate();
		}
	}

	internal sealed class StepButton : Button
	{
		private readonly int _index;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal StepState State { get; set; }
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal string StatusText { get; set; } = string.Empty;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal float Pulse { get; set; }

		internal StepButton(int index)
		{
			_index = index;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
				ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			FlatStyle = FlatStyle.Flat;
			FlatAppearance.BorderSize = 0;
			Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			UseMnemonic = false;
			Cursor = Cursors.Hand;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			float scale = DeviceDpi / 96F;
			int Px(float value) => (int)Math.Round(value * scale);
			e.Graphics.Clear(SettingsPalette.Card);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Color color = State switch
			{
				StepState.Complete => SettingsPalette.Success,
				StepState.Attention => Blend(SettingsPalette.SecondaryText, SettingsPalette.Warning, 0.4F + Pulse * 0.6F),
				StepState.Next or StepState.Ready => SettingsPalette.Accent,
				_ => SettingsPalette.MutedText
			};
			int radius = Px(13);
			int centerX = Width / 2;
			int centerY = Px(22);
			using Pen track = new(SettingsPalette.Divider, scale);
			if (_index > 0)
				e.Graphics.DrawLine(track, 0, centerY, centerX - radius, centerY);
			if (_index < 3)
				e.Graphics.DrawLine(track, centerX + radius, centerY, Width, centerY);
			Rectangle circle = new(centerX - radius, centerY - radius, radius * 2, radius * 2);
			using SolidBrush fill = new(SettingsPalette.Input);
			using Pen outline = new(color, State == StepState.Waiting ? scale : 1.5F * scale);
			e.Graphics.FillEllipse(fill, circle);
			e.Graphics.DrawEllipse(outline, circle);
			TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
				TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
			TextRenderer.DrawText(e.Graphics, State == StepState.Complete ? "✓" : (_index + 1).ToString(), Font, circle, color, flags);
			TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(Px(4), Px(39), Width - Px(8), Px(19)),
				Enabled ? SettingsPalette.PrimaryText : SettingsPalette.MutedText, flags);
			TextRenderer.DrawText(e.Graphics, StatusText, Font, new Rectangle(Px(4), Px(59), Width - Px(8), Px(17)), color, flags);
			if (Focused && ShowFocusCues)
				ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -Px(3), -Px(3)), color, SettingsPalette.Card);
		}

		private static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
			(int)(from.R + (to.R - from.R) * amount),
			(int)(from.G + (to.G - from.G) * amount),
			(int)(from.B + (to.B - from.B) * amount));
	}
}
