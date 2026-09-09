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
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.Design.Controls;
using Synix_Control_Panel.SynixApp.UI.ServerManagement;
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class RoundedGridScrollingTests
{
	[Theory]
	[InlineData(ScrollOrientation.HorizontalScroll)]
	[InlineData(ScrollOrientation.VerticalScroll)]
	public void ScrollRepaintsTheWholeRoundedSurfaceWithoutDuplicateHandlers(ScrollOrientation orientation)
	{
		RunOnStaThread(() =>
		{
			using ScrollableTestGrid grid = new() { Size = new Size(700, 300) };
			GridStyler.ApplyRoundedCorners(grid, 10);
			GridStyler.ApplyRoundedCorners(grid, 10);
			_ = grid.Handle;
			Region? originalRegion = grid.Region;
			List<Rectangle> invalidated = [];
			grid.Invalidated += (_, args) => invalidated.Add(args.InvalidRect);

			foreach (ScrollEventType type in new[] { ScrollEventType.SmallIncrement, ScrollEventType.SmallDecrement, ScrollEventType.ThumbTrack })
			{
				invalidated.Clear();
				grid.RaiseScroll(new ScrollEventArgs(type, 0, 12, orientation));
				Assert.Equal(grid.ClientRectangle, Assert.Single(invalidated));
				Assert.Same(originalRegion, grid.Region); // Scrolling should not allocate new window regions.
			}
		});
	}

	[Theory]
	[InlineData(880, 334)]
	[InlineData(1104, 334)]
	[InlineData(1051, 418)]
	public void BackupTableRepaintsAfterRepeatedHorizontalScrollingAndKeepsItsSelection(int width, int height)
	{
		RunOnStaThread(() =>
		{
			DateTime created = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
			ServerBackupArchive backup = new(@"C:\SynixTestFixtures\backups\backup_test.zip",
				created, 4096, 16384, ServerBackupIntegrity.Recorded, created);
			using ServerBackupRestoreDialog dialog = new(new GameServer { ServerName = "Scroll test" }, [backup]);
			DataGridView grid = Assert.IsType<DataGridView>(Assert.Single(dialog.Controls.Find("backupGrid", true)));
			// Host only the real table in an unseen viewport. No backup files are read or restored.
			using Panel viewport = new() { Size = new Size(width, height), BackColor = SettingsPalette.Window };
			viewport.Controls.Add(grid);
			grid.Dock = DockStyle.Fill;
			using Bitmap initial = Render(grid);
			Assert.Contains(grid.Controls.OfType<HScrollBar>(), bar => bar.Visible);
			List<Rectangle> invalidated = [];
			grid.Invalidated += (_, args) => invalidated.Add(args.InvalidRect);

			foreach (int offset in new[] { 8, 25, 60, 30, 4, 0, 60, 0 })
			{
				invalidated.Clear();
				grid.HorizontalScrollingOffset = offset;
				Assert.Equal(offset, grid.HorizontalScrollingOffset);
				// Assert before DrawToBitmap: a forced bitmap render would hide this regression.
				Assert.Contains(grid.ClientRectangle, invalidated);
				Assert.Same(backup, dialog.SelectedBackup);
				Assert.Single(grid.Rows.Cast<DataGridViewRow>());
				using Bitmap rendered = Render(grid);
				for (int x = 16; x < Math.Min(160, width - 16); x++)
					Assert.Equal(grid.BackgroundColor.ToArgb(), rendered.GetPixel(x, height / 2).ToArgb());
			}
			Assert.False(dialog.Visible);
		});
	}

	private static Bitmap Render(Control control)
	{
		Bitmap result = new(control.Width, control.Height);
		control.DrawToBitmap(result, control.ClientRectangle);
		return result;
	}

	private static void RunOnStaThread(Action action)
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try { action(); }
			catch (Exception exception) { failure = exception; }
		}) { IsBackground = true };
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Rounded-grid scroll regression did not finish.");
		if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
	}

	private sealed class ScrollableTestGrid : DataGridView
	{
		internal void RaiseScroll(ScrollEventArgs args) => OnScroll(args);
	}
}
