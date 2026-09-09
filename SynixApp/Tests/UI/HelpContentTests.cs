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

using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.UI.Help;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class HelpContentTests
{
	[Fact]
	public void Catalog_HasFocusedGuidesInAllNineCategories()
	{
		Dictionary<string, HelpItem> articles = HelpGUI.CreateHelpArticles();
		(string Category, int Count)[] categories =
		[
			("Start", 6), ("Dash", 8), ("Config", 4), ("Net", 4), ("Maint", 6),
			("Watch", 3), ("Trouble", 4), ("Games", 8), ("Support", 4)
		];

		Assert.Equal(47, articles.Count);
		foreach ((string category, int count) in categories)
			Assert.Equal(count, articles.Values.Count(article => article.Category == category));

		Assert.All(articles, entry =>
		{
			Assert.False(string.IsNullOrWhiteSpace(entry.Key));
			Assert.True(entry.Value.Answer.Length > 100, $"Empty help article: {entry.Key}");
		});
		Assert.Same(articles["First-Time Setup Guide"], articles["first-time setup guide"]);
	}

	[Theory]
	[InlineData("License & Proprietary Terms", "398121174F3022BF0D94C7C41A9DE911500B09B76319D194080675EC2E8A1124")]
	[InlineData("Donate & Support Development", "D04779E1D2FA691FDC9151B27037A87CA16C0BE3C86CFE80D86C169F42618C68")]
	public void LicenseAndDonationText_RemainUnchanged(string title, string expectedHash)
	{
		string answer = HelpGUI.CreateHelpArticles()[title].Answer;
		Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(answer))));
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("fr-FR")]
	[InlineData("de-DE")]
	[InlineData("es-ES")]
	public void Catalog_RemainsEnglishForEveryInterfaceCulture(string cultureName)
	{
		Dictionary<string, HelpItem> baseline = HelpGUI.CreateHelpArticles();
		CultureInfo originalCulture = CultureInfo.CurrentCulture;
		CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
			Dictionary<string, HelpItem> localized = HelpGUI.CreateHelpArticles();

			Assert.Equal(baseline.Keys.ToArray(), localized.Keys.ToArray());
			foreach ((string title, HelpItem article) in baseline)
			{
				Assert.Equal(article.Category, localized[title].Category);
				Assert.Equal(article.Answer, localized[title].Answer);
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUiCulture;
		}
	}

	[Theory]
	[InlineData("Satisfactory: Connect Automatically", "sAtIsFaCtOrY", true)]
	[InlineData("Satisfactory: Connect Automatically", "server.GenerateAPIToken", true)]
	[InlineData("Satisfactory: Connection Problems and Token Safety", "server.InvalidateAPITokens", true)]
	[InlineData("Moving Synix to Another PC", ".synixbackup", true)]
	[InlineData("WinGet Installation and Updates", "ubidzz.Synix", true)]
	[InlineData("First-Time Setup Guide", "", true)]
	[InlineData("First-Time Setup Guide", "no-such-help-term-190723", false)]
	public void Search_MatchesTitlesAndFullArticleText(string title, string query, bool expected)
	{
		KeyValuePair<string, HelpItem> article = new(title, HelpGUI.CreateHelpArticles()[title]);
		Assert.Equal(expected, HelpGUI.MatchesFilter(article, query));
	}

	[Fact]
	public void SetupGuide_ExplainsConfigurationWithoutRepeatingTheQuickStart()
	{
		Dictionary<string, HelpItem> articles = HelpGUI.CreateHelpArticles();
		string guide = articles["Server Setup Pages"].Answer;
		Assert.Contains("Server details → Required settings → Review → Save server", guide);
		Assert.Contains("Review is optional", guide);
		Assert.Contains("not download or installation progress", guide);
		Assert.Contains("Switching modes does not erase saved values", guide);
		Assert.Contains("does not mean the game has finished installing", articles["First-Time Setup Guide"].Answer);
		Assert.DoesNotContain("Configuration Support banner", guide);
	}

	[Fact]
	public void SatisfactoryGuides_ExplainConnectionAndTokenSafety()
	{
		Dictionary<string, HelpItem> articles = HelpGUI.CreateHelpArticles();
		string connect = articles["Satisfactory: Connect Automatically"].Answer;
		string security = articles["Satisfactory: Connection Problems and Token Safety"].Answer;
		foreach (string detail in new[]
		{
			"Connect automatically once", "-NewConsole", "in-game Server Manager",
			"server.GenerateAPIToken", "complete fresh output", "verifies the local API",
			"encrypted for your Windows account", "administrator access, not just monitoring",
			"keeps any previously saved connection", "without an API token"
		})
			Assert.Contains(detail, connect);

		Assert.Contains("does not revoke the token at the server", security);
		Assert.Contains("server.InvalidateAPITokens", security);
		Assert.Contains("Do not disable certificate checks", security);
		Assert.Contains("same token value", security);
	}

	[Fact]
	public void BackupGuides_KeepCoverageAndRestoreWarnings()
	{
		Dictionary<string, HelpItem> articles = HelpGUI.CreateHelpArticles();
		string backup = articles["Creating and Keeping Backups"].Answer;
		string transfer = articles["Moving Synix to Another PC"].Answer;
		string restore = articles["Restoring a Server Backup"].Answer;
		Assert.Contains("installation folder, not only", backup);
		Assert.Contains("may be outside that folder", backup);
		Assert.Contains("does not move or delete existing archives", backup);
		Assert.Contains("Do not extract a ZIP over a running server", restore);
		Assert.Contains("SHA-256 receipts", restore);
		Assert.Contains("main C:\\Synix tree", transfer);
		Assert.Contains("external game saves need separate handling", transfer);
		Assert.Contains("Normal export is not encrypted", transfer);
		Assert.Contains("Back up the destination's current Synix data first", transfer);
	}

	[Fact]
	public void SecurityAndMaintenanceGuides_KeepImportantLimits()
	{
		Dictionary<string, HelpItem> articles = HelpGUI.CreateHelpArticles();
		Assert.Contains("no individual-app bypass", articles["Installation and Windows Security Problems"].Answer);
		Assert.Contains("Do not disable Windows security features", articles["Installation and Windows Security Problems"].Answer);
		Assert.Contains("Do not enable a router DMZ, disable the firewall", articles["Connecting Locally and Over the Internet"].Answer);
		Assert.Contains("even if players remain", articles["Scheduling Smart Maintenance"].Answer);
		Assert.Contains("not a firewall, traffic filter, or DDoS protection", articles["Resource Checks and Network Guard"].Answer);
		Assert.Contains("not an ordinary single-process", articles["Dune: Awakening Deployment"].Answer);
		Assert.Contains("do not dynamically load arbitrary handlers", articles["Game Definitions and Contributions"].Answer);
	}

	[Fact]
	public void WingetGuide_UsesExactIdentifierWithoutAReleasePin()
	{
		string guide = HelpGUI.CreateHelpArticles()["WinGet Installation and Updates"].Answer;
		foreach (string action in new[] { "install", "list", "upgrade", "uninstall" })
			Assert.Contains($"winget {action} --exact --id ubidzz.Synix", guide);
		Assert.Contains("Do not delete C:\\Synix", guide);
		Assert.Contains("do not code-sign Synix", guide);
	}

	[Fact]
	public void HelpWindow_RendersEveryAuthoredArticleWithoutAppendingContent()
	{
		RunOnStaThread(() =>
		{
			using HelpGUI window = new();
			TreeView tree = FindControl<TreeView>(window, "treeNavigation");
			RichTextBox answer = FindControl<RichTextBox>(window, "lblAnswer");
			Label title = FindControl<Label>(window, "lblTopicTitle");
			_ = tree.Handle;
			Dictionary<string, HelpItem> articles = HelpGUI.CreateHelpArticles();
			Assert.Equal(9, tree.Nodes.Count);
			Assert.Equal("Getting Started & Setup", tree.Nodes[0].Text);
			Assert.Equal(HelpGUI.WelcomeText.ReplaceLineEndings("\n"), answer.Text.ReplaceLineEndings("\n"));
			Assert.Contains("Offline help", FindControl<Label>(window, "lblFooterHint").Text);
			Assert.Equal(articles.Count, tree.Nodes.Cast<TreeNode>().Sum(category => category.Nodes.Count));

			HashSet<string> rendered = new(StringComparer.Ordinal);
			foreach (TreeNode category in tree.Nodes)
			{
				foreach (TreeNode topic in category.Nodes)
				{
					string key = Assert.IsType<string>(topic.Tag);
					Assert.True(rendered.Add(key), $"Duplicate navigation entry: {key}");
					tree.SelectedNode = topic;
					Assert.Equal(key, title.Text);
					Assert.Equal(articles[key].Answer.ReplaceLineEndings("\n"), answer.Text.ReplaceLineEndings("\n"));
				}
			}
			Assert.Equal(articles.Count, rendered.Count);
		});
	}

	[Fact]
	public void HelpWindow_SearchHighlightsBodyTextAndCanReturnToAllTopics()
	{
		RunOnStaThread(() =>
		{
			using HelpGUI window = new();
			TreeView tree = FindControl<TreeView>(window, "treeNavigation");
			TextBox search = FindControl<TextBox>(window, "txtSearch");
			RichTextBox answer = FindControl<RichTextBox>(window, "lblAnswer");
			_ = tree.Handle;

			search.Text = "Satisfactory: Connect Automatically";
			Assert.Equal("1 matching article", FindControl<Label>(window, "lblArticleCount").Text);
			Assert.Contains("server.GenerateAPIToken", answer.Text);

			search.Text = "server.InvalidateAPITokens";
			Assert.Equal("Satisfactory: Connection Problems and Token Safety",
				FindControl<Label>(window, "lblTopicTitle").Text);
			Assert.Equal("server.InvalidateAPITokens", answer.SelectedText);
			Assert.True(answer.SelectionStart > 1000, "Search should reveal the matching text in the full article.");

			search.Text = "no-such-help-term-190723";
			Assert.Empty(tree.Nodes.Cast<TreeNode>());
			Assert.Contains("Synix could not find a topic", answer.Text);

			search.Clear();
			Assert.Equal(HelpGUI.CreateHelpArticles().Count,
				tree.Nodes.Cast<TreeNode>().Sum(category => category.Nodes.Count));
		});
	}

	private static T FindControl<T>(Control parent, string name) where T : Control =>
		Assert.IsType<T>(Assert.Single(parent.Controls.Find(name, true)));

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
		Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Help window test did not finish.");
		if (failure is not null)
			ExceptionDispatchInfo.Capture(failure).Throw();
	}
}
