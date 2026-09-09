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
using Microsoft.Win32;
using Synix_Control_Panel.SynixApp.FileFolderHandler;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixEngine
{
	public sealed record ProblemReportDraft(
		string ServerType,
		string FailedAction,
		string Summary,
		string WhatHappened,
		string ExpectedResult);

	public sealed record PreparedProblemReport(
		string Title,
		string Body,
		IReadOnlyList<string> Labels);

	public sealed record GitHubConnectionInfo(
		string UserName,
		DateTimeOffset? AccessTokenExpiresAtUtc);

	public sealed record GitHubDeviceAuthorization(
		string UserCode,
		Uri VerificationUri,
		DateTimeOffset ExpiresAtUtc,
		int PollIntervalSeconds)
	{
		internal string DeviceCode { get; init; } = string.Empty;
	}

	public sealed record GitHubIssueResult(
		int Number,
		Uri HtmlUri);

	public sealed class ProblemReportException : Exception
	{
		public ProblemReportException(string message, Exception? inner = null)
			: base(message, inner)
		{
		}
	}

	public partial class Core
	{
		public const string GitHubClientId = "Iv23liczYVyoVslA2oHR";
		public const string DiscordBugForumUrl = "https://discord.gg/pTeMSsYDM";
		public const string GitHubAuthorizationSettingsUrl =
			"https://github.com/settings/apps/authorizations";

		private const string GitHubOwner = "ubidzz";
		private const string GitHubRepository = "Synix-Control-Panel";
		private const string GitHubApiVersion = "2026-03-10";
		private const int GitHubConnectionFormatVersion = 1;
		private const int MaximumReportTextLength = 12000;
		private static readonly Uri GitHubDeviceCodeUri =
			new("https://github.com/login/device/code");
		private static readonly Uri GitHubAccessTokenUri =
			new("https://github.com/login/oauth/access_token");
		private static readonly Uri GitHubCurrentUserUri =
			new("https://api.github.com/user");
		private static readonly Uri GitHubIssueUri =
			new($"https://api.github.com/repos/{GitHubOwner}/{GitHubRepository}/issues");
		private static readonly HttpClient GitHubClient = CreateGitHubClient();
		private static readonly SemaphoreSlim GitHubConnectionLock = new(1, 1);
		private static readonly JsonSerializerOptions GitHubJsonOptions = new()
		{
			PropertyNameCaseInsensitive = true,
			WriteIndented = true
		};
		private static readonly Regex WindowsUserPathPattern = new(
			@"(?i)\b[A-Z]:\\Users\\[^\\\r\n]+",
			RegexOptions.Compiled,
			TimeSpan.FromSeconds(1));
		private static readonly Regex Ipv4Pattern = new(
			@"(?<![\d.])(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)(?![\d.])",
			RegexOptions.Compiled,
			TimeSpan.FromSeconds(1));
		private static readonly Regex Ipv6Pattern = new(
			@"(?i)(?<![0-9a-f:])(?:[0-9a-f]{1,4}:){2,7}[0-9a-f]{0,4}(?![0-9a-f:])",
			RegexOptions.Compiled,
			TimeSpan.FromSeconds(1));

		public static IReadOnlyList<string> ProblemReportActions { get; } =
		[
			"Server installation",
			"Server update or file validation",
			"Server startup",
			"Server shutdown",
			"Server restart or watchdog",
			"Incorrect server status",
			"CPU, memory, or player monitoring",
			"Local network connection",
			"Internet or public connection",
			"Ports, firewall, or RCON",
			"Server backups",
			"Transfer export",
			"Transfer import",
			"Transfer package verification",
			"Server settings or passwords",
			"Discord alerts",
			"Synix update",
			"MSI, WinGet, or standalone installation",
			"Window or display problem",
			"Synix crash or freeze",
			"Server template or launch behavior",
			"Other"
		];

		private static string GitHubConnectionPath =>
			Path.Combine(DataPath, "github-connection.json");

		public static string GetProblemReportSynixVersion()
		{
			return GetCurrentVersion().ToString(3);
		}

		public static string GetProblemReportWindowsVersion()
		{
			return GetWindowsVersionDescription();
		}

		public static PreparedProblemReport PrepareProblemReport(
			ProblemReportDraft draft)
		{
			ArgumentNullException.ThrowIfNull(draft);

			string serverType = SanitizeProblemReportText(draft.ServerType);
			string failedAction = SanitizeProblemReportText(draft.FailedAction);
			string summary = SanitizeProblemReportText(draft.Summary);
			string whatHappened = SanitizeProblemReportText(draft.WhatHappened);
			string expectedResult = SanitizeProblemReportText(draft.ExpectedResult);

			if (string.IsNullOrWhiteSpace(serverType))
				throw new ProblemReportException(LocalizationManager.Get(
					"ProblemReport.Error.ServerTypeRequired"));
			if (!ProblemReportActions.Contains(failedAction, StringComparer.Ordinal))
				throw new ProblemReportException(LocalizationManager.Get(
					"ProblemReport.Error.ActionRequired"));
			if (string.IsNullOrWhiteSpace(summary))
				throw new ProblemReportException(LocalizationManager.Get(
					"ProblemReport.Error.SummaryRequired"));
			if (string.IsNullOrWhiteSpace(whatHappened))
				throw new ProblemReportException(LocalizationManager.Get(
					"ProblemReport.Error.DescriptionRequired"));

			GameCompatibilityVerification verification = GetGameCompatibility(serverType);
			string synixVersion = GetCurrentVersion().ToString(3);
			string windowsVersion = GetWindowsVersionDescription();
			string title = TrimIssueTitle(
				$"[Compatibility] {serverType}: {failedAction} - {summary}");

			StringBuilder body = new();
			body.AppendLine("## Problem report");
			body.AppendLine();
			body.AppendLine($"- **Server type:** {EscapeMarkdownInline(serverType)}");
			body.AppendLine($"- **Failed action:** {EscapeMarkdownInline(failedAction)}");
			body.AppendLine($"- **Summary:** {EscapeMarkdownInline(summary)}");
			body.AppendLine();
			body.AppendLine("## What happened");
			body.AppendLine();
			body.AppendLine(whatHappened);
			body.AppendLine();
			body.AppendLine("## Expected result");
			body.AppendLine();
			body.AppendLine(string.IsNullOrWhiteSpace(expectedResult)
				? "Not provided."
				: expectedResult);
			body.AppendLine();
			body.AppendLine("## Automatic system information");
			body.AppendLine();
			body.AppendLine($"- **Synix version:** {EscapeMarkdownInline(synixVersion)}");
			body.AppendLine($"- **Windows version:** {EscapeMarkdownInline(windowsVersion)}");
			body.AppendLine($"- **Report time:** {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
			body.AppendLine();
			body.AppendLine("## Local verification history");
			body.AppendLine();
			body.AppendLine(FormatVerificationLine("Install", verification.Install));
			body.AppendLine(FormatVerificationLine("Start", verification.Start));
			body.AppendLine(FormatVerificationLine("Stop", verification.Stop));
			body.AppendLine(FormatVerificationLine("Monitoring", verification.Monitoring));
			body.AppendLine();
			body.AppendLine("---");
			body.AppendLine("Created by Synix. Passwords, webhooks, IP addresses, account names, and private server configuration were not collected automatically.");

			return new PreparedProblemReport(
				title,
				body.ToString().TrimEnd(),
				["compatibility-report", "needs-triage"]);
		}

		public static string SanitizeProblemReportText(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return string.Empty;

			string sanitized = SecretRedactor.Redact(value.Replace("\0", string.Empty, StringComparison.Ordinal));
			sanitized = WindowsUserPathPattern.Replace(sanitized, @"C:\Users\[user]");
			sanitized = Ipv4Pattern.Replace(sanitized, "[IP address removed]");
			sanitized = Ipv6Pattern.Replace(sanitized, "[IP address removed]");
			sanitized = sanitized.Trim();

			return sanitized.Length <= MaximumReportTextLength
				? sanitized
				: sanitized[..MaximumReportTextLength];
		}

		public static GitHubConnectionInfo? GetGitHubConnectionInfo()
		{
			GitHubConnectionState? state = LoadGitHubConnection(GitHubConnectionPath);
			return state == null
				? null
				: new GitHubConnectionInfo(
					state.UserName,
					state.AccessTokenExpiresAtUtc);
		}

		public static async Task<GitHubDeviceAuthorization> BeginGitHubConnectionAsync(
			CancellationToken cancellationToken = default)
		{
			using FormUrlEncodedContent content = new(
			[
				new KeyValuePair<string, string>("client_id", GitHubClientId)
			]);
			using HttpRequestMessage request = CreateGitHubRequest(
				HttpMethod.Post,
				GitHubDeviceCodeUri);
			request.Content = content;

			using HttpResponseMessage response = await GitHubClient.SendAsync(
				request,
				cancellationToken).ConfigureAwait(false);
			GitHubDeviceCodeResponse payload = await ReadGitHubPayloadAsync<GitHubDeviceCodeResponse>(
				response,
				cancellationToken).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode ||
				string.IsNullOrWhiteSpace(payload.DeviceCode) ||
				string.IsNullOrWhiteSpace(payload.UserCode) ||
				!TryValidateGitHubDeviceUri(payload.VerificationUri, out Uri? verificationUri))
			{
				throw CreateGitHubException(
					response.StatusCode,
					payload.ErrorDescription ?? payload.Error);
			}

			int expiresIn = Math.Clamp(payload.ExpiresIn, 60, 1800);
			int interval = Math.Clamp(payload.Interval, 5, 60);
			return new GitHubDeviceAuthorization(
				payload.UserCode,
				verificationUri!,
				DateTimeOffset.UtcNow.AddSeconds(expiresIn),
				interval)
			{
				DeviceCode = payload.DeviceCode
			};
		}

		public static async Task<GitHubConnectionInfo> CompleteGitHubConnectionAsync(
			GitHubDeviceAuthorization authorization,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(authorization);
			if (string.IsNullOrWhiteSpace(authorization.DeviceCode))
				throw new ProblemReportException(LocalizationManager.Get(
					"ProblemReport.GitHub.Error.RequestIncomplete"));

			int interval = Math.Clamp(authorization.PollIntervalSeconds, 5, 60);
			while (DateTimeOffset.UtcNow < authorization.ExpiresAtUtc)
			{
				await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken)
					.ConfigureAwait(false);

				GitHubTokenResponse token = await RequestGitHubTokenAsync(
					[
						new KeyValuePair<string, string>("client_id", GitHubClientId),
						new KeyValuePair<string, string>("device_code", authorization.DeviceCode),
						new KeyValuePair<string, string>(
							"grant_type",
							"urn:ietf:params:oauth:grant-type:device_code")
					],
					cancellationToken).ConfigureAwait(false);

				if (!string.IsNullOrWhiteSpace(token.AccessToken))
				{
					string userName = await GetGitHubUserNameAsync(
						token.AccessToken,
						cancellationToken).ConfigureAwait(false);
					GitHubConnectionState state = CreateConnectionState(token, userName);
					SaveGitHubConnection(state, GitHubConnectionPath);
					return new GitHubConnectionInfo(
						state.UserName,
						state.AccessTokenExpiresAtUtc);
				}

				switch (token.Error)
				{
					case "authorization_pending":
						continue;
					case "slow_down":
						interval = Math.Min(interval + 5, 60);
						continue;
					case "access_denied":
						throw new ProblemReportException(LocalizationManager.Get(
							"ProblemReport.GitHub.Error.Cancelled"));
					case "expired_token":
					case "token_expired":
						throw new ProblemReportException(LocalizationManager.Get(
							"ProblemReport.GitHub.Error.CodeExpired"));
					case "device_flow_disabled":
						throw new ProblemReportException(LocalizationManager.Get(
							"ProblemReport.GitHub.Error.DeviceFlowDisabled"));
					default:
						throw new ProblemReportException(
							string.IsNullOrWhiteSpace(token.ErrorDescription)
								? LocalizationManager.Get(
									"ProblemReport.GitHub.Error.ConnectionFailed")
								: LocalizationManager.Get(
									"ProblemReport.GitHub.Error.ConnectionDetail",
									token.ErrorDescription));
				}
			}

			throw new ProblemReportException(LocalizationManager.Get(
				"ProblemReport.GitHub.Error.CodeExpired"));
		}

		public static async Task<GitHubIssueResult> SubmitProblemReportToGitHubAsync(
			PreparedProblemReport report,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(report);
			await GitHubConnectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				GitHubConnectionState state = LoadGitHubConnection(GitHubConnectionPath) ??
					throw new ProblemReportException(LocalizationManager.Get(
						"ProblemReport.GitHub.Error.ConnectFirst"));
				state = await RefreshGitHubConnectionIfNeededAsync(
					state,
					cancellationToken).ConfigureAwait(false);

				string accessToken = RevealProtectedConnectionValue(
					state.ProtectedAccessToken,
					LocalizationManager.Get(
						"ProblemReport.GitHub.Value.AccessToken"));
				try
				{
					using HttpRequestMessage request = CreateGitHubRequest(
						HttpMethod.Post,
						GitHubIssueUri,
						accessToken);
					request.Content = new StringContent(
						JsonSerializer.Serialize(
							new GitHubIssueRequest(report.Title, report.Body, report.Labels),
							GitHubJsonOptions),
						Encoding.UTF8,
						"application/json");

					using HttpResponseMessage response = await GitHubClient.SendAsync(
						request,
						cancellationToken).ConfigureAwait(false);
					GitHubIssueResponse payload = await ReadGitHubPayloadAsync<GitHubIssueResponse>(
						response,
						cancellationToken).ConfigureAwait(false);

					if (response.IsSuccessStatusCode &&
						payload.Number > 0 &&
						Uri.TryCreate(payload.HtmlUrl, UriKind.Absolute, out Uri? issueUri) &&
						issueUri.Scheme == Uri.UriSchemeHttps &&
						issueUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
					{
						return new GitHubIssueResult(payload.Number, issueUri);
					}

					if (response.StatusCode == HttpStatusCode.Unauthorized)
					{
						DisconnectGitHub();
						throw new ProblemReportException(LocalizationManager.Get(
							"ProblemReport.GitHub.Error.AuthorizationDisconnected"));
					}

					if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
					{
						throw new ProblemReportException(
							LocalizationManager.Get(
								"ProblemReport.GitHub.Error.IssueTrackerAccess"));
					}

					throw CreateGitHubException(response.StatusCode, payload.Message);
				}
				finally
				{
					ClearStringReference(ref accessToken);
				}
			}
			finally
			{
				GitHubConnectionLock.Release();
			}
		}

		public static bool DisconnectGitHub()
		{
			try
			{
				if (File.Exists(GitHubConnectionPath))
					File.Delete(GitHubConnectionPath);
				return true;
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				ApplicationLogService.WriteSuppressedException(exception);
				return false;
			}
		}

		internal static void SaveGitHubConnection(
			GitHubConnectionState state,
			string connectionPath)
		{
			ArgumentNullException.ThrowIfNull(state);
			if (!IsProtected(state.ProtectedAccessToken) ||
				(!string.IsNullOrEmpty(state.ProtectedRefreshToken) &&
				 !IsProtected(state.ProtectedRefreshToken)) ||
				!IsProtected(state.ProtectedUserName))
			{
				throw new ProblemReportException(LocalizationManager.Get(
					"ProblemReport.GitHub.Error.ProtectionRequired"));
			}

			FileHandler.WriteTextAtomically(
				connectionPath,
				JsonSerializer.Serialize(state, GitHubJsonOptions));
		}

		internal static GitHubConnectionState? LoadGitHubConnection(string connectionPath)
		{
			try
			{
				if (!File.Exists(connectionPath))
					return null;

				GitHubConnectionStorage? storage = JsonSerializer.Deserialize<GitHubConnectionStorage>(
					File.ReadAllText(connectionPath),
					GitHubJsonOptions);
				if (storage == null ||
					storage.FormatVersion != GitHubConnectionFormatVersion ||
					!IsProtected(storage.ProtectedAccessToken) ||
					!IsProtected(storage.ProtectedUserName) ||
					(!string.IsNullOrEmpty(storage.ProtectedRefreshToken) &&
					 !IsProtected(storage.ProtectedRefreshToken)))
				{
					return null;
				}

				return new GitHubConnectionState
				{
					ProtectedAccessToken = storage.ProtectedAccessToken,
					ProtectedRefreshToken = storage.ProtectedRefreshToken,
					ProtectedUserName = storage.ProtectedUserName,
					AccessTokenExpiresAtUtc = storage.AccessTokenExpiresAtUtc,
					RefreshTokenExpiresAtUtc = storage.RefreshTokenExpiresAtUtc
				};
			}
			catch (Exception exception) when (exception is IOException or
				UnauthorizedAccessException or
				JsonException or
				SynixPasswordProtectionException)
			{
				ApplicationLogService.WriteSuppressedException(exception);
				return null;
			}
		}

		private static async Task<GitHubConnectionState> RefreshGitHubConnectionIfNeededAsync(
			GitHubConnectionState state,
			CancellationToken cancellationToken)
		{
			if (!state.AccessTokenExpiresAtUtc.HasValue ||
				state.AccessTokenExpiresAtUtc.Value > DateTimeOffset.UtcNow.AddMinutes(2))
			{
				return state;
			}

			if (string.IsNullOrWhiteSpace(state.ProtectedRefreshToken) ||
				(state.RefreshTokenExpiresAtUtc.HasValue &&
				 state.RefreshTokenExpiresAtUtc.Value <= DateTimeOffset.UtcNow.AddMinutes(2)))
			{
				DisconnectGitHub();
				throw new ProblemReportException(LocalizationManager.Get(
					"ProblemReport.GitHub.Error.ConnectionExpired"));
			}

			string refreshToken = RevealProtectedConnectionValue(
				state.ProtectedRefreshToken,
				LocalizationManager.Get(
					"ProblemReport.GitHub.Value.RefreshToken"));
			try
			{
				GitHubTokenResponse response = await RequestGitHubTokenAsync(
					[
						new KeyValuePair<string, string>("client_id", GitHubClientId),
						new KeyValuePair<string, string>("grant_type", "refresh_token"),
						new KeyValuePair<string, string>("refresh_token", refreshToken)
					],
					cancellationToken).ConfigureAwait(false);

				if (string.IsNullOrWhiteSpace(response.AccessToken))
				{
					DisconnectGitHub();
					throw new ProblemReportException(LocalizationManager.Get(
						"ProblemReport.GitHub.Error.ConnectionExpired"));
				}

				GitHubConnectionState refreshed = CreateConnectionState(
					response,
					state.UserName);
				SaveGitHubConnection(refreshed, GitHubConnectionPath);
				return refreshed;
			}
			finally
			{
				ClearStringReference(ref refreshToken);
			}
		}

		private static async Task<GitHubTokenResponse> RequestGitHubTokenAsync(
			IEnumerable<KeyValuePair<string, string>> values,
			CancellationToken cancellationToken)
		{
			using FormUrlEncodedContent content = new(values);
			using HttpRequestMessage request = CreateGitHubRequest(
				HttpMethod.Post,
				GitHubAccessTokenUri);
			request.Content = content;
			using HttpResponseMessage response = await GitHubClient.SendAsync(
				request,
				cancellationToken).ConfigureAwait(false);
			return await ReadGitHubPayloadAsync<GitHubTokenResponse>(
				response,
				cancellationToken).ConfigureAwait(false);
		}

		private static async Task<string> GetGitHubUserNameAsync(
			string accessToken,
			CancellationToken cancellationToken)
		{
			using HttpRequestMessage request = CreateGitHubRequest(
				HttpMethod.Get,
				GitHubCurrentUserUri,
				accessToken);
			using HttpResponseMessage response = await GitHubClient.SendAsync(
				request,
				cancellationToken).ConfigureAwait(false);
			GitHubUserResponse payload = await ReadGitHubPayloadAsync<GitHubUserResponse>(
				response,
				cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(payload.Login))
				throw CreateGitHubException(response.StatusCode, payload.Message);
			return payload.Login.Trim();
		}

		private static GitHubConnectionState CreateConnectionState(
			GitHubTokenResponse token,
			string userName)
		{
			DateTimeOffset now = DateTimeOffset.UtcNow;
			return new GitHubConnectionState
			{
				ProtectedAccessToken = Protect(token.AccessToken),
				ProtectedRefreshToken = Protect(token.RefreshToken),
				ProtectedUserName = Protect(userName),
				AccessTokenExpiresAtUtc = token.ExpiresIn > 0
					? now.AddSeconds(token.ExpiresIn)
					: null,
				RefreshTokenExpiresAtUtc = token.RefreshTokenExpiresIn > 0
					? now.AddSeconds(token.RefreshTokenExpiresIn)
					: null
			};
		}

		private static HttpClient CreateGitHubClient()
		{
			HttpClient client = new()
			{
				Timeout = TimeSpan.FromSeconds(30)
			};
			client.DefaultRequestHeaders.UserAgent.Add(
				new ProductInfoHeaderValue("Synix-Control-Panel", GetCurrentVersion().ToString(3)));
			return client;
		}

		private static HttpRequestMessage CreateGitHubRequest(
			HttpMethod method,
			Uri uri,
			string? accessToken = null)
		{
			HttpRequestMessage request = new(method, uri);
			request.Headers.Accept.Add(
				new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
			request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", GitHubApiVersion);
			if (!string.IsNullOrWhiteSpace(accessToken))
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			return request;
		}

		private static async Task<T> ReadGitHubPayloadAsync<T>(
			HttpResponseMessage response,
			CancellationToken cancellationToken)
			where T : new()
		{
			try
			{
				await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken)
					.ConfigureAwait(false);
				return await JsonSerializer.DeserializeAsync<T>(
					stream,
					GitHubJsonOptions,
					cancellationToken).ConfigureAwait(false) ?? new T();
			}
			catch (JsonException)
			{
				return new T();
			}
		}

		private static ProblemReportException CreateGitHubException(
			HttpStatusCode statusCode,
			string? message)
		{
			string safeMessage = SanitizeProblemReportText(message);
			return new ProblemReportException(
				string.IsNullOrWhiteSpace(safeMessage)
					? LocalizationManager.Get(
						"ProblemReport.GitHub.Error.RequestFailedCode",
						(int)statusCode)
					: LocalizationManager.Get(
						"ProblemReport.GitHub.Error.RequestFailedDetail",
						safeMessage));
		}

		private static bool TryValidateGitHubDeviceUri(
			string? rawUri,
			out Uri? validatedUri)
		{
			validatedUri = null;
			if (!Uri.TryCreate(rawUri, UriKind.Absolute, out Uri? candidate) ||
				candidate.Scheme != Uri.UriSchemeHttps ||
				!candidate.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
				!candidate.AbsolutePath.Equals("/login/device", StringComparison.Ordinal))
			{
				return false;
			}

			validatedUri = candidate;
			return true;
		}

		private static string GetWindowsVersionDescription()
		{
			try
			{
				using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
					@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
				string productName = key?.GetValue("ProductName") as string ?? "Windows";
				string displayVersion = key?.GetValue("DisplayVersion") as string ?? string.Empty;
				string buildText = key?.GetValue("CurrentBuildNumber") as string ?? string.Empty;
				int updateBuildRevision = key?.GetValue("UBR") is int revision ? revision : 0;
				if (int.TryParse(buildText, out int build) && build >= 22000)
					productName = Regex.Replace(productName, "Windows 10", "Windows 11", RegexOptions.IgnoreCase);
				string buildDescription = buildText.Length == 0
					? string.Empty
					: $" (build {buildText}{(updateBuildRevision > 0 ? $".{updateBuildRevision}" : string.Empty)})";
				return $"{productName}{(displayVersion.Length > 0 ? $" {displayVersion}" : string.Empty)}{buildDescription}".Trim();
			}
			catch (Exception exception) when (exception is System.Security.SecurityException or
				UnauthorizedAccessException or
				IOException)
			{
				return $"{RuntimeInformation.OSDescription} ({Environment.OSVersion.Version})";
			}
		}

		private static string FormatVerificationLine(
			string label,
			GameVerificationEvidence? evidence)
		{
			return evidence == null
				? $"- **{label} verified:** Not yet verified on this computer"
				: $"- **{label} verified:** Synix v{EscapeMarkdownInline(evidence.SynixVersion)} on {evidence.VerifiedAtUtc:yyyy-MM-dd}";
		}

		private static string EscapeMarkdownInline(string value)
		{
			return value
				.Replace("\\", "\\\\", StringComparison.Ordinal)
				.Replace("`", "\\`", StringComparison.Ordinal)
				.Replace("*", "\\*", StringComparison.Ordinal)
				.Replace("_", "\\_", StringComparison.Ordinal)
				.Replace("[", "\\[", StringComparison.Ordinal)
				.Replace("]", "\\]", StringComparison.Ordinal);
		}

		private static string TrimIssueTitle(string title)
		{
			const int maximumTitleLength = 220;
			return title.Length <= maximumTitleLength
				? title
				: title[..maximumTitleLength].TrimEnd();
		}

		private static string RevealProtectedConnectionValue(
			string protectedValue,
			string valueName)
		{
			if (!IsProtected(protectedValue))
				throw new ProblemReportException(LocalizationManager.Get(
					"ProblemReport.GitHub.Error.SavedValueUnprotected",
					valueName));
			try
			{
				return Reveal(protectedValue);
			}
			catch (SynixPasswordProtectionException exception)
			{
				throw new ProblemReportException(
					LocalizationManager.Get(
						"ProblemReport.GitHub.Error.UnlockSavedValue",
						valueName),
					exception);
			}
		}

		private static void ClearStringReference(ref string value)
		{
			value = string.Empty;
		}

		internal sealed class GitHubConnectionState
		{
			public int FormatVersion { get; set; } = GitHubConnectionFormatVersion;
			public string ProtectedAccessToken { get; set; } = string.Empty;
			public string ProtectedRefreshToken { get; set; } = string.Empty;
			public string ProtectedUserName { get; set; } = string.Empty;
			public DateTimeOffset? AccessTokenExpiresAtUtc { get; set; }
			public DateTimeOffset? RefreshTokenExpiresAtUtc { get; set; }
			[JsonIgnore]
			public string UserName => RevealProtectedConnectionValue(
				ProtectedUserName,
				LocalizationManager.Get(
					"ProblemReport.GitHub.Value.AccountName"));
		}

		private sealed class GitHubConnectionStorage
		{
			public int FormatVersion { get; set; }
			public string ProtectedAccessToken { get; set; } = string.Empty;
			public string ProtectedRefreshToken { get; set; } = string.Empty;
			public string ProtectedUserName { get; set; } = string.Empty;
			public DateTimeOffset? AccessTokenExpiresAtUtc { get; set; }
			public DateTimeOffset? RefreshTokenExpiresAtUtc { get; set; }
		}

		private sealed class GitHubDeviceCodeResponse
		{
			[JsonPropertyName("device_code")]
			public string DeviceCode { get; set; } = string.Empty;
			[JsonPropertyName("user_code")]
			public string UserCode { get; set; } = string.Empty;
			[JsonPropertyName("verification_uri")]
			public string VerificationUri { get; set; } = string.Empty;
			[JsonPropertyName("expires_in")]
			public int ExpiresIn { get; set; }
			[JsonPropertyName("interval")]
			public int Interval { get; set; }
			[JsonPropertyName("error")]
			public string Error { get; set; } = string.Empty;
			[JsonPropertyName("error_description")]
			public string ErrorDescription { get; set; } = string.Empty;
		}

		private sealed class GitHubTokenResponse
		{
			[JsonPropertyName("access_token")]
			public string AccessToken { get; set; } = string.Empty;
			[JsonPropertyName("expires_in")]
			public int ExpiresIn { get; set; }
			[JsonPropertyName("refresh_token")]
			public string RefreshToken { get; set; } = string.Empty;
			[JsonPropertyName("refresh_token_expires_in")]
			public int RefreshTokenExpiresIn { get; set; }
			[JsonPropertyName("error")]
			public string Error { get; set; } = string.Empty;
			[JsonPropertyName("error_description")]
			public string ErrorDescription { get; set; } = string.Empty;
		}

		private sealed record GitHubIssueRequest(
			[property: JsonPropertyName("title")] string Title,
			[property: JsonPropertyName("body")] string Body,
			[property: JsonPropertyName("labels")] IReadOnlyList<string> Labels);

		private sealed class GitHubIssueResponse
		{
			[JsonPropertyName("number")]
			public int Number { get; set; }
			[JsonPropertyName("html_url")]
			public string HtmlUrl { get; set; } = string.Empty;
			[JsonPropertyName("message")]
			public string Message { get; set; } = string.Empty;
		}

		private sealed class GitHubUserResponse
		{
			[JsonPropertyName("login")]
			public string Login { get; set; } = string.Empty;
			[JsonPropertyName("message")]
			public string Message { get; set; } = string.Empty;
		}
	}
}
