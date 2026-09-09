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
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class GameSupportCatalogFilterTests
{
	private static readonly string[] FilterControlNames =
	[
		"catalogNameFilter",
		"catalogSortFilter",
		"catalogCompatibilityFilter",
		"catalogConfigurationFilter",
		"catalogPlayerFilter",
		"catalogCrossplayFilter",
		"catalogProgramFilter",
		"catalogVerificationFilter"
	];

	private static readonly GameSupportRow[] Rows =
	[
		new("Alpha Server", "Fully verified", "Full configuration support", "Named players", "Available", "alpha.exe", "2026-08-01"),
		new("Beta Server", "Needs community testing", "Basic installation support", "Not available", "Not listed", "beta.exe", "Not verified"),
		new("7 Days Sample", "Fully verified", "Full configuration support", "Not available", "Not listed", "seven.exe", "2026-08-02"),
		new("#Hidden Sample", "Needs configuration template", "Basic installation support", "Not available", "Not listed", "hidden.exe", "Not verified")
	];

	[Fact]
	public void AlphabetNumberAndOtherNameGroupsAreRecognized()
	{
		Assert.True(GameSupportCatalogFilterEngine.MatchesNameGroup("Alpha Server", "A"));
		Assert.True(GameSupportCatalogFilterEngine.MatchesNameGroup(
			"7 Days Sample",
			GameSupportCatalogFilterEngine.Numbers));
		Assert.True(GameSupportCatalogFilterEngine.MatchesNameGroup(
			"#Hidden Sample",
			GameSupportCatalogFilterEngine.Other));
		Assert.False(GameSupportCatalogFilterEngine.MatchesNameGroup("Beta Server", "A"));
	}

	[Fact]
	public void FiltersCombineAcrossCatalogSettings()
	{
		GameSupportCatalogFilter filter = new(
			"alpha.exe",
			"A",
			"Fully verified",
			"Full configuration support",
			"Named players",
			"Available",
			"alpha.exe",
			"2026-08-01",
			false);

		GameSupportRow result = Assert.Single(
			GameSupportCatalogFilterEngine.Apply(Rows, filter));

		Assert.Equal("Alpha Server", result.Game);
	}

	[Fact]
	public void DefaultAndReverseSortUseGameName()
	{
		GameSupportCatalogFilter ascending = new(
			"",
			null,
			null,
			null,
			null,
			null,
			null,
			null,
			false);
		GameSupportCatalogFilter descending = ascending with { Descending = true };

		Assert.Equal(
			["#Hidden Sample", "7 Days Sample", "Alpha Server", "Beta Server"],
			GameSupportCatalogFilterEngine.Apply(Rows, ascending).Select(row => row.Game));
		Assert.Equal(
			["Beta Server", "Alpha Server", "7 Days Sample", "#Hidden Sample"],
			GameSupportCatalogFilterEngine.Apply(Rows, descending).Select(row => row.Game));
	}

	[Fact]
	public void CatalogWindowConstructsWithEveryGridFilter()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using GameSupportCatalog catalog = new();
				Assert.All(FilterControlNames, name =>
					Assert.Single(catalog.Controls.Find(name, true)));
				Assert.Single(catalog.Controls.Find("clearCatalogFilters", true));
			}
			catch (Exception exception)
			{
				failure = exception;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();

		Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
		Assert.Null(failure);
	}
}
