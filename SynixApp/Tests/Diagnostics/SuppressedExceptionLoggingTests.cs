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

public sealed class SuppressedExceptionLoggingTests
{
	[Fact]
	public void SuppressedExceptionsDoNotPublishToTheDashboard()
	{
		int dashboardMessageCount = 0;
		EventHandler<ApplicationLogEventArgs> handler = (_, _) =>
			dashboardMessageCount++;
		ApplicationUiService.LogRequested += handler;

		try
		{
			ApplicationLogService.WriteSuppressedException(
				new InvalidOperationException("Suppressed logging regression check."));

			Assert.Equal(0, dashboardMessageCount);
		}
		finally
		{
			ApplicationUiService.LogRequested -= handler;
		}
	}
}
