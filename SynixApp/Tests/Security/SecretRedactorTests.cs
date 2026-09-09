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
using System.Text;
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class SecretRedactorTests
{
	public static IEnumerable<object[]> Formats()
	{
		foreach (string text in new[]
		{
			"+rcon.password \"SecretAlpha SecretBeta\"",
			"--rcon-password='SecretAlpha SecretBeta'",
			"""{"Password": "SecretAlpha SecretBeta", "Port": 7777}""",
			"""{"rcon.password": "SecretAlpha \"SecretBeta\"", "Port": 7777}""",
			"server_password=\"SecretAlpha SecretBeta\"",
			"AdminPassword='SecretAlpha SecretBeta'",
			"authenticationToken=SecretAlphaSecretBeta",
			"access_token: SecretAlphaSecretBeta",
			"refresh-token=SecretAlphaSecretBeta",
			"--api-key SecretAlphaSecretBeta",
			"-userToken=\"SecretAlpha SecretBeta\"",
			"Authorization: Bearer SecretAlphaSecretBeta",
			"Proxy-Authorization: Basic SecretAlphaSecretBeta",
			"""{"Authorization":"Bearer SecretAlphaSecretBeta"}""",
			"password=\"SecretAlpha SecretBeta\nNext harmless line",
			"rcon.password='SecretAlpha SecretBeta\nNext harmless line"
		})
			yield return [text];
	}

	[Theory]
	[MemberData(nameof(Formats))]
	public void MasksCompleteSecretsInLogsAndReportsWithoutAccumulatingMarkers(string text)
	{
		string redacted = SecretRedactor.Redact(text);
		Assert.DoesNotContain("SecretAlpha", redacted);
		Assert.DoesNotContain("SecretBeta", redacted);
		Assert.Contains(SecretRedactor.Removed, redacted);
		Assert.Equal(redacted, SecretRedactor.Redact(redacted));
		string report = Core.SanitizeProblemReportText(text);
		Assert.DoesNotContain("SecretAlpha", report);
		Assert.DoesNotContain("SecretBeta", report);
	}

	[Fact]
	public void BareSatisfactoryTokensRemainMaskedWithoutReadingOrChangingTheToken()
	{
		string token = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"pl\":\"APIToken\"}")) + "." + new string('A', 128);
		Assert.DoesNotContain(token, SecretRedactor.Redact(token));
		Assert.DoesNotContain(token, Core.SanitizeProblemReportText(token));
	}

	[Fact]
	public void NormalDiagnosticsAndNonSecretValuesRemainReadable()
	{
		const string message = "Server startup complete. Port=7777; Players=2; token creation succeeded.";
		Assert.Equal(message, SecretRedactor.Redact(message));
		Assert.Equal("password=[secret removed];port=7777", SecretRedactor.Redact("password=PrivateValue;port=7777"));
	}

	[Fact]
	public void OversizedInputIsOmittedInsteadOfLeakingAnUncheckedTail()
	{
		Assert.Equal(SecretRedactor.Omitted, SecretRedactor.Redact(new string('x', 256 * 1024) + " password=PrivateTail"));
	}

	[Fact]
	public void BothTechnicalAndLocalizedDashboardEventsAreMaskedBeforeDispatch()
	{
		ApplicationLogEventArgs? captured = null;
		EventHandler<ApplicationLogEventArgs> handler = (_, entry) => captured = entry;
		ApplicationUiService.LogRequested += handler;
		try
		{
			Assert.True(ApplicationUiService.PublishLog("password=PrivateTechnical", """{"token":"PrivateLocalized"}""", Color.White));
			Assert.NotNull(captured);
			Assert.DoesNotContain("PrivateTechnical", captured.TechnicalMessage);
			Assert.DoesNotContain("PrivateLocalized", captured.LocalizedMessage);
		}
		finally { ApplicationUiService.LogRequested -= handler; }
	}

	[Fact]
	public void MultilineReportKeepsUsefulLinesAroundMaskedQuotedValues()
	{
		const string input = "Before\npassword=\"SecretAlpha SecretBeta\"\nAfter: server is stopped";
		string output = Core.SanitizeProblemReportText(input);
		Assert.Contains("Before", output);
		Assert.Contains("After: server is stopped", output);
		Assert.DoesNotContain("SecretAlpha", output);
		Assert.DoesNotContain("SecretBeta", output);
	}
}

