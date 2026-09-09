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

using System.Text.RegularExpressions;
using Synix_Control_Panel.SynixApp.UI.Help;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class HelpContentQualityTests
{
	public static IEnumerable<object[]> HelpTopics()
	{
		yield return ["Welcome"];
		foreach (string title in HelpGUI.CreateHelpArticles().Keys)
			yield return [title];
	}

	[Theory]
	[MemberData(nameof(HelpTopics))]
	public void Help_IsEvergreenPlainTextWithoutWikiLinksOrImportedWrappers(string title)
	{
		string text = title == "Welcome"
			? HelpGUI.WelcomeText
			: title + "\n" + HelpGUI.CreateHelpArticles()[title].Answer;

		Assert.False(Regex.IsMatch(text, @"\bwiki\b", RegexOptions.IgnoreCase),
			$"{title} should contain its instructions, not refer readers to a wiki.");
		Assert.False(Regex.IsMatch(text, @"\bv?\d+\.\d+\.\d+(?:\.\d+)?\b", RegexOptions.IgnoreCase),
			$"{title} must not be pinned to an application release.");
		Assert.DoesNotContain("/releases/tag/", text);
		Assert.DoesNotContain("DETAILS AND EXAMPLES", text);
		Assert.DoesNotContain("COMPLETE BUILT-IN GUIDE", text);
		Assert.DoesNotContain("← All guides", text);
		Assert.False(text.Contains("what's new", StringComparison.OrdinalIgnoreCase));
		Assert.False(Regex.IsMatch(text, @"\[[^\]\r\n]+\]\(https?://"),
			"Markdown links must not leak into the plain-text Help control.");
	}

	[Fact]
	public void OperationalGuides_DoNotRepeatSubstantiveParagraphs()
	{
		Dictionary<string, string> owners = new(StringComparer.Ordinal);
		foreach ((string title, HelpItem article) in HelpGUI.CreateHelpArticles())
		{
			// Legal text is deliberately preserved verbatim, not editorially deduplicated.
			if (title is "License & Proprietary Terms" or "Donate & Support Development")
				continue;

			foreach (string paragraph in Regex.Split(article.Answer, @"\n\s*\n"))
			{
				string normalized = Regex.Replace(paragraph, @"\s+", " ").Trim().ToLowerInvariant();
				if (normalized.Length < 100)
					continue; // Short section labels and context-specific warnings may recur.

				Assert.False(owners.TryGetValue(normalized, out string? previous),
					$"Repeated paragraph in '{title}' and '{previous}': {paragraph}");
				owners.Add(normalized, title);
			}
		}
	}

	[Fact]
	public void QuickStart_HasOneProcedureWithoutMarketingOrReleaseNotes()
	{
		string text = HelpGUI.CreateHelpArticles()["First-Time Setup Guide"].Answer;
		string[] numbers = Regex.Matches(text, @"(?m)^(\d+)\. ")
			.Select(match => match.Groups[1].Value).ToArray();

		Assert.Equal(new[] { "1", "2", "3", "4", "5", "6", "7", "8" }, numbers);
		Assert.True(text.Length < 2200, "Keep the first-use path readable rather than appending more guides.");
		Assert.DoesNotContain("WELCOME TO SYNIX CONTROL PANEL", text);
		Assert.DoesNotContain("Your first server, in order", text);
		Assert.DoesNotContain("INSTALL AND FIRST START", text);
	}

	[Fact]
	public void Catalog_DoesNotShipAnotherCopyOfTheWiki()
	{
		Assert.DoesNotContain(typeof(HelpGUI).Assembly.GetManifestResourceNames(),
			name => name.StartsWith("Synix.Help.Wiki.", StringComparison.Ordinal));
		Assert.DoesNotContain(HelpGUI.CreateHelpArticles(),
			entry => entry.Key.StartsWith("Wiki", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void SupportAndDownloadLinks_AreActionableAndNotVersionPinned()
	{
		string[] expectedUrls =
		[
			"https://github.com/ubidzz/Synix-Control-Panel/releases/latest",
			"https://github.com/ubidzz/Synix-Control-Panel/issues",
			"https://github.com/ubidzz/Synix-Control-Panel",
			"https://discord.gg/WduKEU3j8s",
			"https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist",
			"https://account.duneawakening.com/"
		];
		string text = string.Join("\n", HelpGUI.CreateHelpArticles().Values.Select(article => article.Answer));
		string[] urls = Regex.Matches(text, @"https?://[^\s]+").Select(match => match.Value).ToArray();

		Assert.Equal(expectedUrls.Order(StringComparer.Ordinal), urls.Order(StringComparer.Ordinal));
		Assert.All(urls, url => Assert.Equal(Uri.UriSchemeHttps, new Uri(url).Scheme));
	}
}
