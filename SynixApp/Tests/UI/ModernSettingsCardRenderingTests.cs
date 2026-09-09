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
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.Design.Controls;
using Synix_Control_Panel.SynixApp.UI.Help;
using Synix_Control_Panel.SynixApp.UI.Settings;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class ModernSettingsCardRenderingTests
{
	[Theory]
	[InlineData(334, 112, 12, false)]
	[InlineData(335, 113, 12, false)]
	[InlineData(334, 112, 12, true)]
	[InlineData(418, 140, 15, false)]
	[InlineData(501, 168, 18, true)]
	[InlineData(668, 224, 24, false)]
	[InlineData(80, 48, 40, false)]
	public void RoundedBorder_DoesNotClipAntialiasedCorners(
		int width, int height, int radius, bool lightTheme)
	{
		RunOnStaThread(() =>
		{
			using Panel parent = new()
			{
				BackColor = lightTheme ? Color.FromArgb(230, 234, 240) : Color.FromArgb(8, 13, 24),
				Size = new Size(width, height)
			};
			using ModernSettingsCard card = new()
			{
				Size = parent.Size,
				CornerRadius = radius,
				BackColor = lightTheme ? Color.FromArgb(248, 250, 252) : Color.FromArgb(17, 27, 45),
				FillColor = lightTheme ? Color.FromArgb(248, 250, 252) : Color.FromArgb(17, 27, 45),
				BorderColor = lightTheme ? Color.FromArgb(148, 163, 184) : Color.FromArgb(38, 52, 77)
			};
			parent.Controls.Add(card);
			using Bitmap unclipped = RenderWithoutWindowRegion(card);
			using Bitmap rendered = RenderCard(card, parent.BackColor);
			int cornerSize = Math.Min(radius + 2, Math.Min(width, height) / 2);
			for (int y = 0; y < cornerSize; y++)
			{
				for (int x = 0; x < cornerSize; x++)
				{
					foreach (Point corner in new[]
					{
						new Point(x, y), new Point(width - 1 - x, y),
						new Point(x, height - 1 - y), new Point(width - 1 - x, height - 1 - y)
					})
					{
						Assert.Equal(unclipped.GetPixel(corner.X, corner.Y).ToArgb(),
							rendered.GetPixel(corner.X, corner.Y).ToArgb());
					}
				}
			}

			Assert.Equal(parent.BackColor.ToArgb(), rendered.GetPixel(0, 0).ToArgb());
			Assert.Equal(card.FillColor.ToArgb(), rendered.GetPixel(width / 2, height / 2).ToArgb());
			Assert.Equal(card.BorderColor.ToArgb(), rendered.GetPixel(width / 2, 0).ToArgb());
			Assert.Equal(card.BorderColor.ToArgb(), rendered.GetPixel(width - 1, height / 2).ToArgb());
		});
	}

	[Fact]
	public void RoundedRegion_StillClipsDockedChildren_AfterResizeAndRadiusChanges()
	{
		RunOnStaThread(() =>
		{
			using Panel parent = new() { BackColor = Color.Black, Size = new Size(100, 80) };
			using ModernSettingsCard card = new() { Size = parent.Size, CornerRadius = 12 };
			using Panel child = new() { Dock = DockStyle.Fill, BackColor = Color.Red };
			parent.Controls.Add(card);
			card.Controls.Add(child);

			foreach (int radius in new[] { 12, 20, 0, 8 })
			{
				card.Size = new Size(card.Width + 1, card.Height + 1);
				card.CornerRadius = radius;
				Assert.NotNull(card.Region);
				Assert.True(card.Region.IsVisible(card.Width / 2, card.Height / 2));
				using Bitmap rendered = RenderCard(card, parent.BackColor);
				AssertOutlineVisible(card, rendered);
				Assert.Equal(Color.Red.ToArgb(), rendered.GetPixel(card.Width / 2, card.Height / 2).ToArgb());
				if (radius > 0)
				{
					Assert.False(card.Region.IsVisible(0, 0));
					Assert.Equal(parent.BackColor.ToArgb(), rendered.GetPixel(0, 0).ToArgb());
				}
			}
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void NestedOpaqueLayouts_DoNotPaintOverCardOutline(bool lightTheme)
	{
		RunOnStaThread(() =>
		{
			using Panel parent = new()
			{
				BackColor = lightTheme ? Color.White : Color.Black,
				Size = new Size(818, 122)
			};
			using ModernSettingsCard card = new()
			{
				Size = parent.Size,
				CornerRadius = 13,
				FillColor = lightTheme ? Color.Gainsboro : Color.FromArgb(17, 27, 45),
				BorderColor = lightTheme ? Color.DimGray : Color.FromArgb(38, 52, 77)
			};
			using TableLayoutPanel layout = new() { Dock = DockStyle.Fill, BackColor = card.FillColor };
			using Panel nested = new() { Dock = DockStyle.Fill, BackColor = card.FillColor, Margin = Padding.Empty };
			parent.Controls.Add(card);
			card.Controls.Add(layout);
			layout.Controls.Add(nested);
			using Bitmap rendered = RenderCard(card, parent.BackColor);
			AssertOutlineVisible(card, rendered);
			Assert.Equal(card.DisplayRectangle, layout.Bounds);
		});
	}

	[Fact]
	public void ContentInset_RespectsExistingPadding()
	{
		RunOnStaThread(() =>
		{
			using ModernSettingsCard card = new()
			{
				Size = new Size(334, 112),
				Padding = new Padding(22, 18, 20, 18)
			};
			using Panel child = new() { Dock = DockStyle.Fill };
			card.Controls.Add(child);
			Assert.Equal(new Rectangle(22, 18, 292, 76), child.Bounds);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SettingsAndHelpCards_KeepTheirVisibleBorders(bool helpWindow)
	{
		RunOnStaThread(() =>
		{
			using Control page = helpWindow ? new HelpGUI() : new GeneralSettingsPage();
			if (helpWindow)
			{
				TreeView navigation = Assert.IsType<TreeView>(Assert.Single(page.Controls.Find("treeNavigation", true)));
				_ = navigation.Handle;
				navigation.SelectedNode = navigation.Nodes[0].Nodes[0];
				Assert.Equal((string)navigation.SelectedNode.Tag!,
					Assert.Single(page.Controls.Find("lblTopicTitle", true)).Text);
			}
			string[] names = helpWindow
				? ["articleCard"]
				: ["settingsCard", "settingsCardDarkMode", "settingsCardSteamDownloads", "settingsCardLanguage"];
			foreach (string name in names)
			{
				ModernSettingsCard card = Assert.IsType<ModernSettingsCard>(Assert.Single(page.Controls.Find(name, true)));
				// Host the real content without showing a window. A hidden Form otherwise
				// prevents DrawToBitmap from creating and painting its child layouts.
				using Panel viewport = new() { Size = card.Size, BackColor = card.Parent!.BackColor };
				viewport.Controls.Add(card);
				Assert.True(card.Controls[0].Visible);
				using Bitmap rendered = RenderCard(card, viewport.BackColor);
				AssertOutlineVisible(card, rendered);
			}
		});
	}

	private static void AssertOutlineVisible(ModernSettingsCard card, Bitmap rendered)
	{
		using Bitmap expected = RenderWithoutWindowRegion(card);
		int width = card.Width;
		int height = card.Height;
		Assert.Equal(card.BorderColor.ToArgb(), rendered.GetPixel(width / 2, 0).ToArgb());
		Assert.Equal(card.BorderColor.ToArgb(), rendered.GetPixel(width / 2, height - 1).ToArgb());
		Assert.Equal(card.BorderColor.ToArgb(), rendered.GetPixel(0, height / 2).ToArgb());
		Assert.Equal(card.BorderColor.ToArgb(), rendered.GetPixel(width - 1, height / 2).ToArgb());
		int cornerSize = Math.Min(card.CornerRadius + 2, Math.Min(width, height) / 2);
		for (int y = 0; y < cornerSize; y++)
			for (int x = 0; x < cornerSize; x++)
				foreach (Point corner in new[]
				{
					new Point(x, y), new Point(width - 1 - x, y),
					new Point(x, height - 1 - y), new Point(width - 1 - x, height - 1 - y)
				})
				{
					int edgeColor = expected.GetPixel(corner.X, corner.Y).ToArgb();
					if (edgeColor != card.FillColor.ToArgb())
						Assert.Equal(edgeColor, rendered.GetPixel(corner.X, corner.Y).ToArgb());
				}
	}

	private static Bitmap RenderCard(ModernSettingsCard card, Color parentColor)
	{
		using Bitmap controlBitmap = new(card.Width, card.Height);
		card.DrawToBitmap(controlBitmap, card.ClientRectangle);
		Bitmap result = new(card.Width, card.Height);
		using Graphics graphics = Graphics.FromImage(result);
		graphics.Clear(parentColor);
		// Also apply the managed region when compositing the control onto its parent.
		if (card.Region is not null)
			graphics.SetClip(card.Region, CombineMode.Replace);
		graphics.DrawImageUnscaled(controlBitmap, Point.Empty);
		return result;
	}

	private static Bitmap RenderWithoutWindowRegion(ModernSettingsCard card)
	{
		Bitmap result = new(card.Width, card.Height);
		using Graphics graphics = Graphics.FromImage(result);
		using PaintEventArgs paint = new(graphics, card.ClientRectangle);
		// Invoke painting directly to obtain the full smoothed edge, before HWND clipping.
		foreach (string method in new[] { "OnPaintBackground", "OnPaint" })
			typeof(ModernSettingsCard).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(card, new object[] { paint });
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
		Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Card rendering did not finish.");
		if (failure is not null)
			ExceptionDispatchInfo.Capture(failure).Throw();
	}
}
