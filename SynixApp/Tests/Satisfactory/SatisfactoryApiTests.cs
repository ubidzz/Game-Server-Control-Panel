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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.Design.Controls;
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;
using Synix_Control_Panel.SynixApp.UI.ServerManagement;
using Synix_Control_Panel.SynixEngine;
using Xunit;
using static Synix_Control_Panel.SynixEngine.Core;

namespace Synix_Control_Panel.Tests;

public sealed class SatisfactoryApiTests
{
	private static string Token(string privilege = "APIToken") =>
		Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { pl = privilege }))) + "." + new string('a', 64);
	private static string Pin => new('B', 64);
	private const string StateResponse = """
		{"data":{"serverGameState":{"activeSessionName":"Save","numConnectedPlayers":3,"playerLimit":10,"isGameRunning":true,"isGamePaused":false,"averageTickRate":29.5,"totalGameDuration":42,"techTier":4}}}
		""";

	[Fact]
	public async Task QueryUsesLocalGamePortBearerAndDocumentedEnvelope()
	{
		using SatisfactoryApiClient client = new(7788, Token(), Pin, new Handler(async request =>
		{
			Assert.Equal("https://127.0.0.1:7788/api/v1", request.RequestUri!.AbsoluteUri);
			Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
			Assert.Equal(Token(), request.Headers.Authorization.Parameter);
			Assert.Equal(HttpMethod.Post, request.Method);
			using JsonDocument body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
			Assert.Equal("QueryServerState", body.RootElement.GetProperty("function").GetString());
			Assert.DoesNotContain(Token(), body.RootElement.GetRawText());
			return Json(StateResponse);
		}));
		SatisfactoryServerState state = await client.QueryStateAsync();
		Assert.Equal("Save", state.ActiveSessionName);
		Assert.Equal(3, state.NumConnectedPlayers);
		Assert.Equal(10, state.PlayerLimit);
	}

	[Fact]
	public async Task SettingsKeepEnglishDictionaryKeysAndSendOnlyExplicitChanges()
	{
		using SatisfactoryApiClient client = new(7777, Token(), Pin, new Handler(async request =>
		{
			using JsonDocument body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
			JsonElement changes = body.RootElement.GetProperty("data").GetProperty("updatedServerOptions");
			Assert.Single(changes.EnumerateObject());
			Assert.Equal("True", changes.GetProperty("FG.DSAutoPause").GetString());
			return new HttpResponseMessage(HttpStatusCode.NoContent);
		}));
		await client.CallAsync("ApplyServerOptions", new { UpdatedServerOptions = new Dictionary<string, string> { ["FG.DSAutoPause"] = "True" } });
	}

	[Theory]
	[InlineData(401, "Satisfactory.Error.Authentication")]
	[InlineData(403, "Satisfactory.Error.Authentication")]
	[InlineData(307, "Satisfactory.Error.Request")]
	[InlineData(500, "Satisfactory.Error.Request")]
	public async Task FailedResponsesDoNotExposeServerErrorTextOrToken(int code, string key)
	{
		using SatisfactoryApiClient client = new(7777, Token(), Pin, new Handler(_ => Task.FromResult(
			new HttpResponseMessage((HttpStatusCode)code) { Content = new StringContent(Token() + " private server response") })));
		SatisfactoryApiException exception = await Assert.ThrowsAsync<SatisfactoryApiException>(() => client.QueryStateAsync());
		Assert.Equal(key, exception.ResourceKey);
		Assert.DoesNotContain(Token(), exception.ToString());
		Assert.Null(exception.InnerException);
	}

	[Theory]
	[InlineData("not-json")]
	[InlineData("{\"data\":{}}")]
	[InlineData("{\"data\":{\"serverGameState\":{\"numConnectedPlayers\":0}}}")]
	public async Task MalformedOrIncompleteStateIsUnavailableNotZeroPlayers(string response)
	{
		using SatisfactoryApiClient client = new(7777, Token(), Pin, new Handler(_ => Task.FromResult(Json(response))));
		await Assert.ThrowsAsync<SatisfactoryApiException>(() => client.QueryStateAsync());
	}

	[Fact]
	public async Task JsonBodiesHaveBoundedMemoryAndCancellationIsPreserved()
	{
		using SatisfactoryApiClient large = new(7777, Token(), Pin, new Handler(_ => Task.FromResult(Json(new string('x', 2 * 1024 * 1024 + 1)))));
		await Assert.ThrowsAsync<SatisfactoryApiException>(() => large.QueryStateAsync());
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		using SatisfactoryApiClient canceled = new(7777, Token(), Pin, new Handler(_ => Task.FromResult(Json(StateResponse))));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled.QueryStateAsync(cancellation.Token));
	}

	[Fact]
	public void CertificateMustMatchTheExactAcceptedFingerprint()
	{
		using RSA rsa = RSA.Create(2048);
		CertificateRequest request = new("CN=Satisfactory", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
		string fingerprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);
		Assert.True(SatisfactoryApiClient.MatchesCertificate(certificate, fingerprint));
		Assert.True(SatisfactoryApiClient.MatchesCertificate(certificate, fingerprint.ToLowerInvariant()));
		Assert.False(SatisfactoryApiClient.MatchesCertificate(certificate, Pin));
		Assert.False(SatisfactoryApiClient.MatchesCertificate(null, fingerprint));
		Assert.False(SatisfactoryApiClient.MatchesCertificate(certificate, ""));
	}

	[Fact]
	public async Task RealTlsHandshakeRejectsChangedCertificateBeforeSendingAnyToken()
	{
		using RSA rsa = RSA.Create(2048);
		CertificateRequest certificateRequest = new("CN=Satisfactory", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		using X509Certificate2 certificate = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
		using TcpListener listener = new(IPAddress.Loopback, 0);
		listener.Start();
		using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
		Task<string> received = Task.Run(async () =>
		{
			using TcpClient peer = await listener.AcceptTcpClientAsync(timeout.Token);
			using SslStream tls = new(peer.GetStream());
			try
			{
				await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate }, timeout.Token);
				byte[] buffer = new byte[8192];
				int count = await tls.ReadAsync(buffer, timeout.Token);
				return Encoding.UTF8.GetString(buffer, 0, count);
			}
			catch (Exception exception) when (exception is IOException or System.Security.Authentication.AuthenticationException)
			{ return string.Empty; }
		}, timeout.Token);
		using SatisfactoryApiClient client = new(((IPEndPoint)listener.LocalEndpoint).Port, Token(), Pin);
		SatisfactoryApiException failure = await Assert.ThrowsAsync<SatisfactoryApiException>(() => client.QueryStateAsync(timeout.Token));
		Assert.Equal("Satisfactory.Error.Connection", failure.ResourceKey);
		Assert.Equal("", await received);
	}

	[Fact]
	public void OnlyApplicationTokensAreAcceptedAndWhitespaceIsTrimmed()
	{
		Assert.Equal(Token(), SatisfactoryApiClient.NormalizeToken("  " + Token() + "\r\n"));
		Assert.Throws<SatisfactoryApiException>(() => SatisfactoryApiClient.NormalizeToken(Token("Administrator")));
		Assert.Throws<SatisfactoryApiException>(() => SatisfactoryApiClient.NormalizeToken("Bearer " + Token()));
		Assert.Throws<SatisfactoryApiException>(() => SatisfactoryApiClient.NormalizeToken("admin-password"));
		Assert.Throws<SatisfactoryApiException>(() => new SatisfactoryApiClient(0, Token(), Pin));
	}

	[Fact]
	public void ConnectionIsEncryptedAndFailedPersistenceRestoresEverySecret()
	{
		GameServer server = new() { Game = "Satisfactory", Password = "old-password", AuthenticationToken = "legacy-token" };
		Assert.False(SatisfactoryIntegration.SaveConnection(server, Token(), Pin, () => false));
		Assert.Equal("legacy-token", server.AuthenticationToken);
		Assert.Equal("old-password", server.Password);
		Assert.Equal(0, server.PasswordStorageVersion);
		Assert.Equal("", server.SatisfactoryCertificateFingerprint);
		Assert.True(SatisfactoryIntegration.SaveConnection(server, Token(), Pin, () => true));
		Assert.True(IsProtected(server.AuthenticationToken));
		Assert.Equal(Token(), RevealServerPasswords(server).AuthenticationToken);
		Assert.DoesNotContain(Token(), SerializeServersForStorage([server]));
		Assert.True(SatisfactoryIntegration.SaveConnection(server, "", "", () => true));
		Assert.False(SatisfactoryIntegration.IsConnected(server));
		Assert.Equal("old-password", RevealServerPasswords(server).ServerPassword);
	}

	[Fact]
	public void CountsRequireFreshApiDataWithoutChangingConfiguredLimitOrServerStatus()
	{
		GameServer server = new() { Game = "Satisfactory", MaxPlayers = 8, Status = StatusManager.GetStatus(ServerState.Running) };
		Assert.Equal("N/A", server.PlayerCount);
		SatisfactoryIntegration.SaveConnection(server, Token(), Pin, () => true);
		Assert.Equal("N/A", server.PlayerCount);
		SatisfactoryIntegration.RecordState(server, new("Factory", 3, 10, true, false, 30, 42, 4));
		Assert.Equal("3 / 10", server.PlayerCount);
		Assert.Equal(8, server.MaxPlayers);
		server.SatisfactoryLastSuccessUtc = DateTime.UtcNow.AddMinutes(-1);
		Assert.Equal("N/A", server.PlayerCount);
		Assert.Equal(StatusManager.GetStatus(ServerState.Running), server.Status);
		Assert.Equal(3, server.CurrentPlayers);
	}

	[Fact]
	public async Task MissingTokenDoesNotProbeOrBlockSaveAndUnplannedShutdownIsRejected()
	{
		GameServer server = new() { Game = "Satisfactory", Status = StatusManager.GetStatus(ServerState.Running) };
		await SatisfactoryIntegration.PollAsync(server);
		Assert.Null(server.SatisfactoryLastAttemptUtc);
		Assert.True(GameServerInputValidator.TryValidate(GameDatabase.GetGame("Satisfactory")!, "Factory", new("", "", ""), out _));
		SatisfactoryIntegration.SaveConnection(server, Token(), Pin, () => true);
		Assert.False(await SatisfactoryIntegration.TryShutdownAsync(server));
		Assert.Equal(StatusManager.GetStatus(ServerState.Running), server.Status);
	}

	[Fact]
	public void UnknownPlayerCountDefersMaintenanceUpToExistingMaximumDelay()
	{
		GameServer server = new() { Game = "Satisfactory", IsScheduledRestartEnabled = true,
			Status = StatusManager.GetStatus(ServerState.Running), RestartTime = "04:00", MaintenanceMaximumDelayMinutes = 30 };
		SatisfactoryIntegration.SaveConnection(server, Token(), Pin, () => true);
		DateTime now = new(2026, 9, 5, 4, 10, 0);
		Assert.Equal(SmartMaintenanceDecision.DeferForPlayers, SmartMaintenancePlanner.Evaluate(server, now).Decision);
		Assert.Equal(SmartMaintenanceDecision.RunNow, SmartMaintenancePlanner.Evaluate(server, now.AddMinutes(30)).Decision);
	}

	[Theory]
	[InlineData("quit", true, true)]
	[InlineData(" EXIT ", true, true)]
	[InlineData("quit now", true, true)]
	[InlineData("server.SaveGame Test", true, false)]
	[InlineData("FG.AutosaveInterval 300", true, false)]
	[InlineData("server.GenerateAPIToken", false, false)]
	[InlineData("FG.DedicatedServer.AllowInsecureLocalAccess 1", false, false)]
	[InlineData("exec commands.txt", false, false)]
	[InlineData("foo; quit", false, false)]
	[InlineData("foo\r\nquit", false, false)]
	public void CommandsKeepLifecycleAndSecretOperationsOutOfRawConsole(string command, bool safe, bool stop)
	{
		Assert.Equal(safe, SatisfactoryApiClient.IsSafeConsoleCommand(command));
		Assert.Equal(stop, SatisfactoryApiClient.IsStopCommand(command));
	}

	[Fact]
	public async Task SaveDownloadUsesChosenStreamAndRejectsJsonErrorBodies()
	{
		using MemoryStream file = new();
		using SatisfactoryApiClient client = new(7777, Token(), Pin, new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
		{ Content = new ByteArrayContent([1, 2, 3, 4]) })));
		await client.DownloadSaveAsync("Factory", file, CancellationToken.None);
		Assert.Equal(new byte[] { 1, 2, 3, 4 }, file.ToArray());
		using SatisfactoryApiClient error = new(7777, Token(), Pin, new Handler(_ => Task.FromResult(Json("{\"errorCode\":\"file_not_found\"}"))));
		await Assert.ThrowsAsync<SatisfactoryApiException>(() => error.DownloadSaveAsync("Factory", file, CancellationToken.None));
		Assert.Equal(4, file.Length);
	}

	[Theory]
	[InlineData("en-US", true)]
	[InlineData("en-US", false)]
	[InlineData("fr-FR", true)]
	[InlineData("de-DE", true)]
	[InlineData("es-ES", true)]
	public void ConnectionHasOneSetupButtonAndNoManualTokenSteps(string language, bool dark)
	{
		RunOnSta(() =>
		{
			bool previousTheme = ThemeManager.IsDarkMode;
			ThemeManager.Initialize(dark);
			try
			{
			LocalizationManager.Initialize(language);
			using ServerSettingsSecurityPage security = new();
			security.ConfigureForGame(GameDatabase.GetGame("Satisfactory"));
			security.LoadSecrets(new("", "", "", Token()), "");
			security.SetPrivacyMode(false);
			Assert.Equal(Token(), security.AuthenticationToken);
			Assert.False(security.RequiredAuthenticationTokenMissing);
			Assert.True(Find<TextBox>(security, "txtAuthenticationToken").UseSystemPasswordChar);
			using SatisfactoryControlDialog dialog = new(new GameServer { Game = "Satisfactory" });
			dialog.StartPosition = FormStartPosition.Manual;
			dialog.Location = new Point(-32000, -32000);
			dialog.Size = dialog.MinimumSize;
			dialog.Show();
			Application.DoEvents();
			Button connect = Find<Button>(dialog, "satisfactoryConnectAutomatically");
			Assert.False(connect.Enabled);
			Assert.True(connect.Right <= connect.Parent!.ClientSize.Width);
			Assert.True(connect.Bottom <= connect.Parent.ClientSize.Height);
			Assert.True(TextRenderer.MeasureText(connect.Text, connect.Font).Width + 16 <= connect.Width);
			Assert.Equal(LocalizationManager.Get("Satisfactory.ConnectAutomatically"), connect.Text);
			foreach (string name in new[] { "Token", "ReadConsole", "PasteToken", "TestSave", "CopyCommand" })
				Assert.Empty(dialog.Controls.Find("satisfactory" + name, true));
			Assert.Empty(dialog.Controls.Find("generateTokenCommand", true));
			Assert.Single(connect.Parent.Controls.OfType<Button>());
			Assert.Equal(5, Find<Panel>(dialog, "satisfactoryPages").Controls.Count);
			Button tab = Find<Button>(dialog, "satisfactorySetup");
			Assert.True(tab.Bottom <= tab.Parent!.ClientSize.Height);
			Assert.True(Assert.IsType<ModernSettingsButton>(tab).UseAccentStyle);
			DataGridView overview = Find<DataGridView>(dialog, "overviewGrid");
			Assert.False(overview.EnableHeadersVisualStyles);
			Assert.Equal(SettingsPalette.Sidebar, overview.ColumnHeadersDefaultCellStyle.BackColor);
			Assert.Equal(SettingsPalette.Selection, overview.DefaultCellStyle.SelectionBackColor);
			Assert.IsType<ModernSettingsCard>(overview.Parent);
			Label steps = Find<Label>(dialog, "tokenSteps");
			Assert.True(TextRenderer.MeasureText(steps.Text, steps.Font, new Size(steps.Width, int.MaxValue), TextFormatFlags.WordBreak).Height <= steps.Height);
			if (Environment.GetEnvironmentVariable("SYNIX_RENDER_SATISFACTORY") == "1")
			{
				using Bitmap bitmap = new(dialog.Width, dialog.Height);
				dialog.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
				bitmap.Save(Path.Combine(AppContext.BaseDirectory, $"satisfactory-setup-{language}-{dark}.png"));
				DataGridView grid = Find<DataGridView>(dialog, "overviewGrid");
				grid.Rows.Add(LocalizationManager.Get("Satisfactory.Session"), "Factory — Example");
				grid.Rows.Add(LocalizationManager.Get("Satisfactory.Players"), "3 / 8");
				grid.Rows.Add(LocalizationManager.Get("Satisfactory.State"), LocalizationManager.Get("Satisfactory.Playing"));
				Find<Button>(dialog, "satisfactoryOverview").PerformClick();
				dialog.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
				bitmap.Save(Path.Combine(AppContext.BaseDirectory, $"satisfactory-overview-{language}-{dark}.png"));
			}
			}
			finally { ThemeManager.Initialize(previousTheme); }
		});
	}

	private static T Find<T>(Control control, string name) where T : Control =>
		Assert.IsAssignableFrom<T>(Assert.Single(control.Controls.Find(name, true)));
	private static void RunOnSta(Action action)
	{
		Exception? error = null;
		Thread thread = new(() => { try { action(); } catch (Exception exception) { error = exception; }
			finally { LocalizationManager.Initialize("en-US"); } });
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
	}
	private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK)
	{ Content = new StringContent(value, Encoding.UTF8, "application/json") };
	private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{ cancellationToken.ThrowIfCancellationRequested(); return send(request); }
	}
}
