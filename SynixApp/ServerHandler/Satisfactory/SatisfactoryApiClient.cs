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
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;

internal enum SatisfactoryApiError
{
	Certificate, Port, Token, Unavailable, Connection, Authentication, Request,
	Response, SaveSize, ProtectedToken, SaveConnection, SelectSave, Command,
	AmbiguousToken, ConsoleUnavailable, ConsoleTokenMissing, ConsoleBusy,
	ConsoleInputBusy, ConsoleFilters, LocalIdentity
}

internal sealed class SatisfactoryApiException(SatisfactoryApiError error) : Exception
{
	internal string ResourceKey { get; } = $"Satisfactory.Error.{error}";
	public override string Message => LocalizationManager.Get(ResourceKey);
}

internal sealed record SatisfactoryServerState(
	string ActiveSessionName, int NumConnectedPlayers, int PlayerLimit,
	bool IsGameRunning, bool IsGamePaused, double AverageTickRate,
	long TotalGameDuration, int TechTier);

/// <summary>
/// Local-only HTTPS client. No redirects, proxies, password login or insecure
/// local-access switch. The verified local server's certificate is pinned before
/// any bearer token is sent. Protocol names and values are never translated.
/// </summary>
internal sealed class SatisfactoryApiClient : IDisposable
{
	internal const string GenerateTokenCommand = "server.GenerateAPIToken";
	private const int MaxJsonBytes = 2 * 1024 * 1024;
	private const long MaxSaveBytes = 512L * 1024 * 1024;
	private readonly HttpClient _http;
	private readonly string _token;
	internal Uri Endpoint { get; }

	internal SatisfactoryApiClient(int port, string token, string fingerprint,
		HttpMessageHandler? testHandler = null)
	{
		Endpoint = CreateEndpoint(port);
		_token = NormalizeToken(token);
		if (!IsFingerprint(fingerprint))
			throw new SatisfactoryApiException(SatisfactoryApiError.Certificate);
		_http = new HttpClient(testHandler ?? new HttpClientHandler
		{
			AllowAutoRedirect = false,
			UseProxy = false,
			ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
				MatchesCertificate(certificate, fingerprint)
		}) { Timeout = Timeout.InfiniteTimeSpan };
	}

	internal static Uri CreateEndpoint(int port)
	{
		if (port is < 1 or > 65535)
			throw new SatisfactoryApiException(SatisfactoryApiError.Port);
		return new Uri($"https://127.0.0.1:{port}/api/v1");
	}

	internal static bool IsFingerprint(string? value) =>
		value is { Length: 64 } && value.All(Uri.IsHexDigit);

	internal static bool MatchesCertificate(X509Certificate2? certificate, string fingerprint) =>
		certificate != null && IsFingerprint(fingerprint) &&
		string.Equals(certificate.GetCertHashString(HashAlgorithmName.SHA256),
			fingerprint, StringComparison.OrdinalIgnoreCase);

	internal static string NormalizeToken(string? value)
	{
		string token = value?.Trim() ?? string.Empty;
		if (token.Length is < 3 or > 4096 || token.Any(c =>
			!char.IsAsciiLetterOrDigit(c) && c is not '+' and not '/' and not '=' and not '.' and not '-' and not '_'))
			throw new SatisfactoryApiException(SatisfactoryApiError.Token);
		string[] parts = token.Split('.');
		if (parts.Length != 2 || parts[1].Length is < 32 or > 512 || !parts[1].All(Uri.IsHexDigit))
			throw new SatisfactoryApiException(SatisfactoryApiError.Token);
		// Application tokens, unlike player/admin login tokens, survive sessions.
		try
		{
			string payload = parts[0].Replace('-', '+').Replace('_', '/');
			payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
			using JsonDocument document = JsonDocument.Parse(Convert.FromBase64String(payload));
			if (document.RootElement.GetProperty("pl").GetString() != "APIToken")
				throw new FormatException();
		}
		catch (Exception exception) when (exception is FormatException or JsonException or KeyNotFoundException or InvalidOperationException)
		{
			throw new SatisfactoryApiException(SatisfactoryApiError.Token);
		}
		return token;
	}

	internal static async Task<string> InspectCertificateAsync(int port, CancellationToken cancellationToken)
	{
		string fingerprint = string.Empty;
		// This separate unauthenticated health request only discovers identity.
		// It never receives or sends an application token.
		using HttpClient http = new(new HttpClientHandler
		{
			AllowAutoRedirect = false,
			UseProxy = false,
			ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
			{
				fingerprint = certificate?.GetCertHashString(HashAlgorithmName.SHA256) ?? string.Empty;
				return IsFingerprint(fingerprint);
			}
		}) { Timeout = TimeSpan.FromSeconds(8) };
		using HttpRequestMessage request = new(HttpMethod.Post, CreateEndpoint(port));
		request.Content = JsonContent("HealthCheck", new { ClientCustomData = "" });
		try
		{
			using HttpResponseMessage response = await http.SendAsync(request,
				HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			if (!response.IsSuccessStatusCode || !IsFingerprint(fingerprint))
				throw new SatisfactoryApiException(SatisfactoryApiError.Unavailable);
			return fingerprint;
		}
		catch (HttpRequestException) { throw new SatisfactoryApiException(SatisfactoryApiError.Unavailable); }
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{ throw new SatisfactoryApiException(SatisfactoryApiError.Unavailable); }
	}

	internal async Task<SatisfactoryServerState> QueryStateAsync(CancellationToken cancellationToken = default)
	{
		JsonElement data = await CallAsync("QueryServerState", null, cancellationToken);
		try
		{
			JsonElement state = data.GetProperty("serverGameState");
			SatisfactoryServerState result = new(
				state.GetProperty("activeSessionName").GetString() ?? "",
				state.GetProperty("numConnectedPlayers").GetInt32(),
				state.GetProperty("playerLimit").GetInt32(),
				state.GetProperty("isGameRunning").GetBoolean(),
				state.GetProperty("isGamePaused").GetBoolean(),
				state.GetProperty("averageTickRate").GetDouble(),
				state.GetProperty("totalGameDuration").GetInt64(),
				state.GetProperty("techTier").GetInt32());
			if (result.NumConnectedPlayers < 0 || result.PlayerLimit < 1 ||
				result.TotalGameDuration < 0 || !double.IsFinite(result.AverageTickRate))
				throw new JsonException();
			return result;
		}
		catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
		{ throw new SatisfactoryApiException(SatisfactoryApiError.Response); }
	}

	internal async Task<JsonElement> CallAsync(string function, object? data = null,
		CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(20));
		using HttpRequestMessage request = CreateRequest(JsonContent(function, data));
		try
		{
			using HttpResponseMessage response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
			return await ReadResponseAsync(response, timeout.Token);
		}
		catch (HttpRequestException) { throw new SatisfactoryApiException(SatisfactoryApiError.Connection); }
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{ throw new SatisfactoryApiException(SatisfactoryApiError.Unavailable); }
	}

	private HttpRequestMessage CreateRequest(HttpContent content)
	{
		HttpRequestMessage request = new(HttpMethod.Post, Endpoint) { Content = content };
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
		return request;
	}

	private static StringContent JsonContent(string function, object? data) =>
		new(JsonSerializer.Serialize(new { function, data = data ?? new { } }, new JsonSerializerOptions
		{ PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), Encoding.UTF8, "application/json");

	private static async Task<JsonElement> ReadResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
			throw new SatisfactoryApiException(SatisfactoryApiError.Authentication);
		if (response.StatusCode == HttpStatusCode.NoContent)
			return JsonSerializer.SerializeToElement(new { });
		if (!response.IsSuccessStatusCode)
			throw new SatisfactoryApiException(SatisfactoryApiError.Request);
		if (response.Content.Headers.ContentLength > MaxJsonBytes)
			throw new SatisfactoryApiException(SatisfactoryApiError.Response);
		using MemoryStream content = new();
		await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
		byte[] buffer = new byte[8192];
		int count;
		while ((count = await source.ReadAsync(buffer, cancellationToken)) != 0)
		{
			if (content.Length + count > MaxJsonBytes)
				throw new SatisfactoryApiException(SatisfactoryApiError.Response);
			content.Write(buffer, 0, count);
		}
		try
		{
			using JsonDocument document = JsonDocument.Parse(content.ToArray());
			JsonElement root = document.RootElement;
			if (root.TryGetProperty("errorCode", out _) ||
				(root.TryGetProperty("error", out JsonElement error) && error.ValueKind != JsonValueKind.Null))
				throw new SatisfactoryApiException(SatisfactoryApiError.Request);
			return root.TryGetProperty("data", out JsonElement data)
				? data.Clone() : JsonSerializer.SerializeToElement(new { });
		}
		catch (JsonException) { throw new SatisfactoryApiException(SatisfactoryApiError.Response); }
	}

	internal async Task UploadSaveAsync(string path, CancellationToken cancellationToken)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromMinutes(2));
		using FileStream file = File.OpenRead(path);
		if (file.Length > MaxSaveBytes)
			throw new SatisfactoryApiException(SatisfactoryApiError.SaveSize);
		using MultipartFormDataContent content = new();
		content.Add(JsonContent("UploadSaveGame", new { SaveName = Path.GetFileNameWithoutExtension(path), LoadSaveGame = false }), "data");
		content.Add(new StreamContent(file), "saveGameFile", Path.GetFileName(path));
		using HttpRequestMessage request = CreateRequest(content);
		using HttpResponseMessage response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
		await ReadResponseAsync(response, timeout.Token);
	}

	internal async Task DownloadSaveAsync(string name, Stream destination, CancellationToken cancellationToken)
	{
		using HttpRequestMessage request = CreateRequest(JsonContent("DownloadSaveGame", new { SaveName = name }));
		using HttpResponseMessage response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
		{
			await ReadResponseAsync(response, cancellationToken);
			throw new SatisfactoryApiException(SatisfactoryApiError.Request);
		}
		if (response.Content.Headers.ContentLength > MaxSaveBytes)
			throw new SatisfactoryApiException(SatisfactoryApiError.SaveSize);
		await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
		byte[] buffer = new byte[81920];
		long total = 0;
		int count;
		while ((count = await source.ReadAsync(buffer, cancellationToken)) != 0)
		{
			total += count;
			if (total > MaxSaveBytes) throw new SatisfactoryApiException(SatisfactoryApiError.SaveSize);
			await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
		}
		if (total == 0) throw new SatisfactoryApiException(SatisfactoryApiError.Response);
	}

	internal static bool IsStopCommand(string command) =>
		new[] { "quit", "exit", "stop", "shutdown", "server.shutdown", "server.stop", "server.quit", "server.exit" }
			.Contains(command.Trim().Split(' ', '\t')[0].Trim('"'), StringComparer.OrdinalIgnoreCase);

	internal static bool IsSafeConsoleCommand(string command) =>
		!string.IsNullOrWhiteSpace(command) && command.Length <= 1024 &&
		!command.Any(c => char.IsControl(c) || c is ';' or '|' or '&') &&
		!Regex.IsMatch(command, @"(?i)(token|password|allowinsecure|\bexec\b|\brestart\b)");

	internal string SanitizeOutput(string output) => Regex.Replace(
		output.Replace(_token, "[redacted]", StringComparison.Ordinal),
		@"[A-Za-z0-9+/=_-]{12,}\.[A-Fa-f0-9]{32,}", "[redacted]", RegexOptions.NonBacktracking);

	public void Dispose() => _http.Dispose();
}
