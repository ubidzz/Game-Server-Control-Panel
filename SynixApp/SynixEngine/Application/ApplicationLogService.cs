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
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using System.Runtime.CompilerServices;

namespace Synix_Control_Panel.SynixEngine
{
	public static class ApplicationLogService
	{
		private const string ApplicationLogName = "Synix_Log";

		public static void Write(string message, Color? color = null, bool bold = false)
		{
			if (ApplicationUiService.PublishLog(
				message,
				color ?? Color.White,
				bold))
			{
				return;
			}

			FileHandler.QueueLog(
				"Synix_Background_Service",
				$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
		}

		/// <summary>
		/// Publishes translated activity text while retaining invariant English in
		/// the technical log used for troubleshooting and support.
		/// </summary>
		public static void WriteLocalized(
			string resourceKey,
			Color? color = null,
			bool bold = false,
			params object?[] arguments)
		{
			string technicalMessage = LocalizationManager.GetEnglish(
				resourceKey,
				arguments);
			string localizedMessage = LocalizationManager.Get(
				resourceKey,
				arguments);
			if (ApplicationUiService.PublishLog(
				technicalMessage,
				localizedMessage,
				color ?? Color.White,
				bold))
			{
				return;
			}

			FileHandler.QueueLog(
				"Synix_Background_Service",
				$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {technicalMessage}");
		}

		/// <summary>
		/// Records an intentionally suppressed exception in the text log without
		/// publishing it to Activity &amp; Diagnostics on the dashboard.
		/// </summary>
		public static void WriteSuppressedException(
			Exception exception,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "")
		{
			ArgumentNullException.ThrowIfNull(exception);
			string sourceName = Path.GetFileName(sourceFilePath);
			string location = string.IsNullOrWhiteSpace(sourceName)
				? memberName
				: $"{sourceName}::{memberName}";
			string entry = SecretRedactor.Redact(
				$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] Suppressed exception in {location}:{Environment.NewLine}{exception}");

			try
			{
				if (!FileHandler.QueueLog(ApplicationLogName, entry))
					_ = FileHandler.WriteLogImmediate(ApplicationLogName, entry);
			}
			catch (Exception loggingException)
			{
				System.Diagnostics.Debug.WriteLine(
					$"[SUPPRESSED EXCEPTION LOG FAILURE] {loggingException}{Environment.NewLine}{entry}");
			}
		}
	}
}
