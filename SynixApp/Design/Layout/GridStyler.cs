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
using System.Drawing.Drawing2D;
using static Synix_Control_Panel.SynixEngine.Core;

namespace Synix_Control_Panel.SynixApp.Design
{
	public static class GridStyler
	{
		private static Color RowBackground => SettingsPalette.Input;
		private static Color AlternateRowBackground => SettingsPalette.AlternateInput;
		private static Color SelectionBackground => SettingsPalette.Selection;
		private static Color Divider => SettingsPalette.Divider;
		private static Font? _statusFont;

		public static void DarkTheme(DataGridView grid)
		{
			grid.AutoGenerateColumns = false;
			grid.AllowUserToResizeColumns = false;
			grid.AllowUserToResizeRows = false;
			grid.EnableHeadersVisualStyles = false;
			grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
			grid.ColumnHeadersHeight = 40;
			grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
			{
				Alignment = DataGridViewContentAlignment.MiddleLeft,
				BackColor = SettingsPalette.Sidebar,
				ForeColor = SettingsPalette.SecondaryText,
				Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
				Padding = new Padding(8, 0, 4, 0),
				SelectionBackColor = SettingsPalette.Sidebar,
				SelectionForeColor = SettingsPalette.SecondaryText
			};

			grid.DefaultCellStyle = new DataGridViewCellStyle
			{
				Alignment = DataGridViewContentAlignment.MiddleLeft,
				BackColor = RowBackground,
				ForeColor = SettingsPalette.PrimaryText,
				Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
				Padding = new Padding(8, 0, 4, 0),
				SelectionBackColor = SelectionBackground,
				SelectionForeColor = SettingsPalette.PrimaryText
			};

			grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
			{
				BackColor = AlternateRowBackground,
				ForeColor = SettingsPalette.PrimaryText,
				SelectionBackColor = SelectionBackground,
				SelectionForeColor = SettingsPalette.PrimaryText
			};

			// DarkTheme is shared by every grid. Dashboard-only behavior must be
			// enabled explicitly so unrelated grids keep their own interaction model.
			grid.CellMouseEnter -= ServerGrid_CellMouseEnter;
			grid.CellMouseLeave -= ServerGrid_CellMouseLeave;
			if (grid.Cursor == Cursors.Hand)
				grid.Cursor = Cursors.Default;
		}

		public static void EnableServerDetailsInteraction(DataGridView grid)
		{
			grid.CellMouseEnter -= ServerGrid_CellMouseEnter;
			grid.CellMouseEnter += ServerGrid_CellMouseEnter;
			grid.CellMouseLeave -= ServerGrid_CellMouseLeave;
			grid.CellMouseLeave += ServerGrid_CellMouseLeave;
		}

		public static void ApplyDashboardTheme(DataGridView grid)
		{
			grid.RowHeadersVisible = false;
			grid.BackgroundColor = RowBackground;
			grid.BorderStyle = BorderStyle.None;
			grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
			grid.GridColor = Divider;
			grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			grid.RowTemplate.Height = 44;
		}

		public static void ApplyRoundedCorners(DataGridView grid, int radius)
		{
			grid.Tag = radius;
			UpdateGridRegion(grid);
			grid.Resize -= Grid_Resize;
			grid.Resize += Grid_Resize;
			grid.Paint -= Grid_Paint;
			grid.Paint += Grid_Paint;
			grid.Scroll -= Grid_Scroll;
			grid.Scroll += Grid_Scroll;
		}

		public static void SetStatusColor(DataGridView grid, DataGridViewCellFormattingEventArgs eventArgs)
		{
			if (eventArgs.ColumnIndex < 0 || eventArgs.ColumnIndex >= grid.Columns.Count)
				return;

			DataGridViewColumn column = grid.Columns[eventArgs.ColumnIndex];
			if (column.DataPropertyName != "Status" || eventArgs.Value == null)
				return;

			string status = eventArgs.Value.ToString()?.Trim() ?? string.Empty;
			if (_statusFont == null || _statusFont.FontFamily.Name != grid.Font.FontFamily.Name)
			{
				_statusFont?.Dispose();
				_statusFont = new Font("Segoe UI", 9F, FontStyle.Bold);
			}

			eventArgs.CellStyle.Font = _statusFont;
			Color statusColor = GetStatusColor(status);
			eventArgs.CellStyle.ForeColor = statusColor;
			eventArgs.CellStyle.SelectionForeColor = statusColor;
		}

		public static void StyleSettingsButton(Button button, Color? hoverColor = null)
		{
			StyleTitleButton(button, hoverColor ?? SettingsPalette.CardHover);
			button.Text = "⚙";
			button.Font = new Font("Segoe UI Symbol", 12F, FontStyle.Bold);
		}

		public static void StyleMinimizeButton(Button button)
		{
			StyleTitleButton(button, SettingsPalette.CardHover);
			button.Text = "—";
		}

		public static void StyleCloseButton(Button button)
		{
			StyleTitleButton(button, Color.FromArgb(232, 17, 35));
			button.FlatAppearance.MouseDownBackColor = Color.FromArgb(178, 11, 22);
			button.Text = "✕";
		}

		public static void StyleIconButton(Button button, Image? icon, Color hoverColor)
		{
			StyleTitleButton(button, SettingsPalette.CardHover);
			button.Image = null;
			button.Text = string.Empty;
			button.Padding = Padding.Empty;
			button.Paint += (_, paintArgs) =>
			{
				Point pointer = button.PointToClient(Cursor.Position);
				bool hovered = button.ClientRectangle.Contains(pointer);
				bool pressed = hovered && (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left;
				Color surface = pressed
					? SettingsPalette.Selection
					: hovered
						? SettingsPalette.CardHover
						: SettingsPalette.TitleBar;
				paintArgs.Graphics.Clear(surface);

				if (icon == null)
					return;

				paintArgs.Graphics.InterpolationMode =
					System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				int iconSize = Math.Min(20, Math.Min(button.Width, button.Height) - 12);
				Rectangle iconBounds = new(
					(button.Width - iconSize) / 2,
					(button.Height - iconSize) / 2,
					iconSize,
					iconSize);
				if (ThemeManager.IsDarkMode)
				{
					paintArgs.Graphics.DrawImage(icon, iconBounds);
				}
				else
				{
					Color iconColor = SettingsPalette.SecondaryText;
					float red = iconColor.R / 255F;
					float green = iconColor.G / 255F;
					float blue = iconColor.B / 255F;
					using System.Drawing.Imaging.ImageAttributes imageAttributes = new();
					imageAttributes.SetColorMatrix(
						new System.Drawing.Imaging.ColorMatrix(
							new[]
							{
								new[] { 0F, 0F, 0F, 0F, 0F },
								new[] { 0F, 0F, 0F, 0F, 0F },
								new[] { 0F, 0F, 0F, 0F, 0F },
								new[] { 0F, 0F, 0F, 1F, 0F },
								new[] { red, green, blue, 0F, 1F }
							}));
					paintArgs.Graphics.DrawImage(
						icon,
						iconBounds,
						0,
						0,
						icon.Width,
						icon.Height,
						GraphicsUnit.Pixel,
						imageAttributes);
				}
				if (hovered)
				{
					using Pen accentPen = new(
						Color.FromArgb(210, hoverColor.R, hoverColor.G, hoverColor.B),
						2F);
					paintArgs.Graphics.DrawLine(
						accentPen,
						8,
						button.Height - 2,
						button.Width - 8,
						button.Height - 2);
				}
			};
			button.MouseEnter += (_, _) => button.Invalidate();
			button.MouseLeave += (_, _) => button.Invalidate();
			button.MouseDown += (_, _) => button.Invalidate();
			button.MouseUp += (_, _) => button.Invalidate();
		}

		public static void DashboardLabels(Label cpuLabel, Label ramLabel)
		{
			cpuLabel.ForeColor = SettingsPalette.Accent;
			ramLabel.ForeColor = SettingsPalette.Ram;
		}

		private static void StyleTitleButton(Button button, Color hoverColor)
		{
			button.BackColor = SettingsPalette.TitleBar;
			button.FlatStyle = FlatStyle.Flat;
			button.FlatAppearance.BorderSize = 0;
			button.FlatAppearance.MouseOverBackColor = hoverColor;
			button.FlatAppearance.MouseDownBackColor = SettingsPalette.Selection;
			button.ForeColor = SettingsPalette.PrimaryText;
			button.TabStop = false;
			button.UseVisualStyleBackColor = false;
		}

		private static Color GetStatusColor(string status)
		{
			if (status.Equals(StatusManager.GetStatus(ServerState.Running), StringComparison.OrdinalIgnoreCase))
				return SettingsPalette.Success;
			if (status.Equals(StatusManager.GetStatus(ServerState.Stopped), StringComparison.OrdinalIgnoreCase))
				return SettingsPalette.Danger;
			if (status.Equals(StatusManager.GetStatus(ServerState.Crashed), StringComparison.OrdinalIgnoreCase))
				return SettingsPalette.Danger;
			if (status.StartsWith("Starting", StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith("Stopping", StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith("Installing", StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith("Updating", StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith("Backing Up", StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith("Restoring", StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith("Validating", StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith("Exporting", StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith("Deleting", StringComparison.OrdinalIgnoreCase))
			{
				return SettingsPalette.Warning;
			}

			return SettingsPalette.SecondaryText;
		}

		private static void ServerGrid_CellMouseEnter(object? sender, DataGridViewCellEventArgs eventArgs)
		{
			if (sender is not DataGridView grid || eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0)
				return;

			grid.Cursor = Cursors.Hand;
			grid.Rows[eventArgs.RowIndex].Cells[eventArgs.ColumnIndex].ToolTipText =
				LocalizationManager.Get("Dashboard.Grid.DetailsTooltip");
		}

		private static void ServerGrid_CellMouseLeave(object? sender, DataGridViewCellEventArgs eventArgs)
		{
			if (sender is DataGridView grid)
				grid.Cursor = Cursors.Default;
		}

		private static void Grid_Resize(object? sender, EventArgs eventArgs)
		{
			if (sender is DataGridView grid)
				UpdateGridRegion(grid);
		}

		private static void Grid_Scroll(object? sender, ScrollEventArgs eventArgs)
		{
			// DataGridView scrolls existing pixels, including our fixed rounded outline.
			// Repaint the whole surface (also the empty area below the rows) so copied
			// border pixels cannot accumulate as vertical/horizontal trails. Invalidate
			// lets Windows combine repaint requests without forcing a synchronous refresh.
			if (sender is DataGridView grid)
				grid.Invalidate();
		}

		private static void UpdateGridRegion(DataGridView grid)
		{
			if (grid.Width <= 1 || grid.Height <= 1)
				return;

			int radius = grid.Tag is int configuredRadius ? configuredRadius : 8;
			using GraphicsPath path = RoundedGeometry.Create(
				new Rectangle(0, 0, grid.Width, grid.Height),
				radius);
			Region? oldRegion = grid.Region;
			grid.Region = new Region(path);
			oldRegion?.Dispose();
		}

		private static void Grid_Paint(object? sender, PaintEventArgs eventArgs)
		{
			if (sender is not DataGridView grid || grid.Width <= 1 || grid.Height <= 1)
				return;

			int radius = grid.Tag is int configuredRadius ? configuredRadius : 8;
			eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using GraphicsPath path = RoundedGeometry.Create(
				new Rectangle(0, 0, grid.Width - 1, grid.Height - 1),
				radius);
			using Pen borderPen = new(SettingsPalette.Border, 1F);
			eventArgs.Graphics.DrawPath(borderPen, path);
		}
	}
}
