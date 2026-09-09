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

using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class UserInterfaceResourceTests
{
	[Theory]
	[InlineData("Dashboard")]
	[InlineData("Settings")]
	[InlineData("Server setup")]
	[InlineData("Resource monitor")]
	[InlineData("Help")]
	[InlineData("Configuration editor")]
	[InlineData("Game definition builder")]
	[InlineData("Satisfactory")]
	public void MainWindows_InitializeRenderResizeAndDisposeWithoutRuntimeServices(string area)
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			LicenseContext originalContext = LicenseManager.CurrentContext;
			try
			{
				// Exercise the actual controls/resources without dashboard startup,
				// server loading, downloads, settings writes, or monitoring services.
				LicenseManager.CurrentContext = new DesigntimeLicenseContext();
				using Form window = area switch
				{
					"Dashboard" => new MainGUI(),
					"Settings" => new AppSettings(),
					"Server setup" => new ServerSettingsGUI(),
					"Resource monitor" => new ResourceMonitorGUI(),
					"Help" => new HelpGUI(),
					"Configuration editor" => new ServerConfig(),
					"Game definition builder" => new GameDefinitionBuilder(),
					"Satisfactory" => new SatisfactoryControlDialog(),
					_ => throw new ArgumentOutOfRangeException(nameof(area))
				};
				Assert.NotEmpty(window.Controls.Cast<Control>());
				_ = window.Handle;
				for (int pass = 0; pass < 2; pass++)
				{
					window.PerformLayout();
					using Bitmap rendered = new(window.Width, window.Height);
					window.DrawToBitmap(rendered, new Rectangle(Point.Empty, rendered.Size));
					window.ClientSize += new Size(24, 24);
				}
				Assert.False(window.Visible);
			}
			catch (Exception exception) { failure = exception; }
			finally { LicenseManager.CurrentContext = originalContext; }
		}) { IsBackground = true };
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		Assert.True(thread.Join(TimeSpan.FromSeconds(30)), $"{area} initialization/rendering did not finish.");
		if (failure is not null)
			ExceptionDispatchInfo.Capture(failure).Throw();
	}

	[Fact]
	public void EmbeddedFormResources_MatchTheirCurrentTypeNamespaces()
	{
		Assembly assembly = typeof(MainGUI).Assembly;
		string expectedDashboardResource =
			$"{typeof(MainGUI).FullName}.resources";
		string[] resourceNames = assembly.GetManifestResourceNames();

		Assert.Contains(expectedDashboardResource, resourceNames);

		string[] formResourceNames = resourceNames
			.Where(name => name.EndsWith(
				".resources",
				StringComparison.Ordinal))
			.Where(name => !string.Equals(
				name,
				"Synix_Control_Panel.Localization.Strings.resources",
				StringComparison.Ordinal))
			.Where(name => !string.Equals(
				name,
				"Synix_Control_Panel.Properties.Resources.resources",
				StringComparison.Ordinal))
			.ToArray();

		foreach (string resourceName in formResourceNames)
		{
			string typeName = resourceName[..^".resources".Length];
			Assert.NotNull(assembly.GetType(typeName));
		}
	}
}
