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
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Design;
using Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;
using static Synix_Control_Panel.SynixEngine.Core;

namespace Synix_Control_Panel.SynixApp.UI.ServerManagement;

public partial class SatisfactoryControlDialog : Form
{
	private readonly GameServer? _server;
	private readonly CancellationTokenSource _lifetime = new();
	private readonly Dictionary<string, string> _originalOptions = new(StringComparer.Ordinal);
	private bool _busy;
	private bool _mutating;

	public SatisfactoryControlDialog()
	{
		InitializeComponent();
		if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
			ThemeManager.Apply(this);
		StyleTable(overviewGrid);
		StyleTable(optionsGrid);
		StyleTable(savesGrid);
		UpdateAvailability();
	}

	internal SatisfactoryControlDialog(GameServer server) : this()
	{
		if (!GameDatabase.IsSatisfactory(server.Game)) throw new ArgumentException(nameof(server));
		_server = server;
		UpdateAvailability();
	}

	private static string T(string key, params object?[] arguments) =>
		LocalizationManager.Get("Satisfactory." + key, arguments);

	protected override async void OnShown(EventArgs e)
	{
		base.OnShown(e);
		if (_server != null && SatisfactoryIntegration.IsConnected(_server) && SatisfactoryIntegration.IsLive(_server))
		{
			ShowPage(overviewPage);
			await RunAsync(RefreshOverviewAsync);
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (_mutating && e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
		base.OnFormClosing(e);
		if (!e.Cancel) _lifetime.Cancel();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !IsDisposed)
		{
			_lifetime.Cancel();
			_lifetime.Dispose();
		}
		base.Dispose(disposing);
	}

	private void UpdateAvailability()
	{
		bool live = _server != null && SatisfactoryIntegration.IsLive(_server);
		bool connected = _server != null && SatisfactoryIntegration.IsConnected(_server);
		connectAutomatically.Enabled = !_busy && live;
		forget.Enabled = !_busy && _server != null && !string.IsNullOrEmpty(_server.AuthenticationToken);
		foreach (var button in actionButtons) button.Enabled = !_busy && live && connected;
		if (!_busy)
			connectionStatus.Text = _server == null ? LocalizationManager.Get("Satisfactory.NotConnected") : !live ? LocalizationManager.Get("Satisfactory.StartFirst") :
				connected ? LocalizationManager.Get("Satisfactory.ConnectedHint", _server.ServerName) : LocalizationManager.Get("Satisfactory.NotConnected");
	}

	private async Task RunAsync(Func<Task> action)
	{
		if (_busy || _server == null || IsDisposed) return;
		_busy = true;
		UpdateAvailability();
		connectionStatus.Text = LocalizationManager.Get("Satisfactory.Working");
		string? resultMessage = null;
		try
		{
			await action();
			if (!IsDisposed) resultMessage = connectionStatus.Text == LocalizationManager.Get("Satisfactory.Working") ? LocalizationManager.Get("Satisfactory.Done") : connectionStatus.Text;
		}
		catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
		catch (Exception exception)
		{
			string key = SatisfactoryIntegration.SafeErrorKey(exception);
			SatisfactoryIntegration.LogFailure(key);
			resultMessage = LocalizationManager.Get(key);
		}
		finally
		{
			_busy = false;
			if (!IsDisposed)
			{
				UpdateAvailability();
				if (resultMessage != null) connectionStatus.Text = resultMessage;
			}
		}
	}

	private bool Confirm(string key, params object?[] args) => LocalizedMessageBox.Show(this,
		T(key, args), LocalizationManager.Get("Satisfactory.Title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
		MessageBoxDefaultButton.Button2) == DialogResult.Yes;

	private async Task ConnectAutomaticallyAsync()
	{
		if (_server == null) return;
		using ServerOperationLease lease = ServerOperationCoordinator.TryBegin(_server, ServerOperationKind.Configure);
		if (!lease.Acquired) { connectionStatus.Text = lease.FailureReason; return; }
		_mutating = true;
		try
		{
			connectionStatus.Text = LocalizationManager.Get("Satisfactory.ConnectingAutomatically");
			SatisfactoryServerState state = await SatisfactoryAutoConnect.ConnectAsync(_server, _lifetime.Token);
			if (IsDisposed) return;
			RenderState(state);
			ShowPage(overviewPage);
			connectionStatus.Text = LocalizationManager.Get("Satisfactory.Connected");
			Core.Instance.UpdateGridStatus();
		}
		finally { _mutating = false; }
	}

	private void Disconnect()
	{
		if (_server == null || !Confirm("DisconnectConfirm")) return;
		try
		{
			if (!SatisfactoryIntegration.SaveConnection(_server, "", ""))
				throw new SatisfactoryApiException(SatisfactoryApiError.SaveConnection);
			overviewGrid.Rows.Clear();
			optionsGrid.Rows.Clear();
			savesGrid.Rows.Clear();
			consoleOutput.Clear();
			UpdateAvailability();
			Core.Instance.UpdateGridStatus();
		}
		catch (Exception exception)
		{ connectionStatus.Text = LocalizationManager.Get(SatisfactoryIntegration.SafeErrorKey(exception)); }
	}

	private async Task RefreshOverviewAsync()
	{
		using SatisfactoryApiClient client = SatisfactoryIntegration.CreateClient(_server!);
		try
		{
			SatisfactoryServerState state = await client.QueryStateAsync(_lifetime.Token);
			_lifetime.Token.ThrowIfCancellationRequested();
			SatisfactoryIntegration.RecordState(_server!, state);
			RenderState(state);
			Core.Instance.UpdateGridStatus();
		}
		catch
		{
			_server!.SatisfactoryLastSuccessUtc = null;
			if (!IsDisposed) overviewGrid.Rows.Clear();
			throw;
		}
	}

	private void RenderState(SatisfactoryServerState state)
	{
		overviewGrid.Rows.Clear();
		overviewGrid.Rows.Add(LocalizationManager.Get("Satisfactory.Session"), state.ActiveSessionName);
		overviewGrid.Rows.Add(LocalizationManager.Get("Satisfactory.Players"), $"{state.NumConnectedPlayers} / {state.PlayerLimit}");
		overviewGrid.Rows.Add(LocalizationManager.Get("Satisfactory.State"), T(!state.IsGameRunning ? "WaitingSession" : state.IsGamePaused ? "Paused" : "Playing"));
		overviewGrid.Rows.Add(LocalizationManager.Get("Satisfactory.TickRate"), state.AverageTickRate.ToString("F1", CultureInfo.CurrentCulture));
		overviewGrid.Rows.Add(LocalizationManager.Get("Satisfactory.Duration"), TimeSpan.FromSeconds(state.TotalGameDuration).ToString());
		overviewGrid.Rows.Add(LocalizationManager.Get("Satisfactory.TechTier"), state.TechTier);
		connectionStatus.Text = LocalizationManager.Get("Satisfactory.Updated", DateTime.Now.ToString("T"));
	}

	private async Task LifecycleAsync(bool restart, bool stop)
	{
		if (_busy || _server == null || !Confirm(stop ? "StopConfirm" : restart ? "RestartConfirm" : "StartConfirm")) return;
		await RunAsync(async () =>
		{
			_mutating = true;
			try
			{
				bool success = stop ? await Core.Instance.StopServerAndReport(_server) :
					await Core.Instance.ExecuteStartSequence(_server, restart ? "RESTART" : "");
				connectionStatus.Text = LocalizationManager.Get(success ? "Satisfactory.Done" : "Satisfactory.LifecycleFailed");
			}
			finally { _mutating = false; }
		});
	}

	private async Task WithChangeAsync(Func<SatisfactoryApiClient, Task> action)
	{
		if (_server == null || !SatisfactoryIntegration.IsLive(_server))
			throw new SatisfactoryApiException(SatisfactoryApiError.Unavailable);
		using ServerOperationLease lease = ServerOperationCoordinator.TryBegin(_server, ServerOperationKind.Configure);
		if (!lease.Acquired) { connectionStatus.Text = lease.FailureReason; return; }
		_mutating = true;
		try
		{
			using SatisfactoryApiClient client = SatisfactoryIntegration.CreateClient(_server);
			await action(client);
		}
		finally { _mutating = false; }
	}

	private async Task RefreshOptionsAsync()
	{
		if (OptionsChanged().Count > 0 && !Confirm("DiscardOptions")) return;
		using SatisfactoryApiClient client = SatisfactoryIntegration.CreateClient(_server!);
		JsonElement data = await client.CallAsync("GetServerOptions", null, _lifetime.Token);
		_lifetime.Token.ThrowIfCancellationRequested();
		JsonElement options = data.GetProperty("serverOptions");
		data.TryGetProperty("pendingServerOptions", out JsonElement pending);
		optionsGrid.Rows.Clear();
		_originalOptions.Clear();
		foreach (JsonProperty option in options.EnumerateObject())
		{
			string value = option.Value.GetString() ?? "";
			_originalOptions[option.Name] = value;
			string next = pending.ValueKind == JsonValueKind.Object && pending.TryGetProperty(option.Name, out JsonElement p) ? p.GetString() ?? "" : "";
			optionsGrid.Rows.Add(option.Name, value, next);
		}
	}

	private Dictionary<string, string> OptionsChanged()
	{
		optionsGrid.EndEdit();
		Dictionary<string, string> changes = new(StringComparer.Ordinal);
		foreach (DataGridViewRow row in optionsGrid.Rows)
		{
			string key = row.Cells[0].Value?.ToString() ?? "";
			string value = row.Cells[1].Value?.ToString() ?? "";
			if (_originalOptions.TryGetValue(key, out string? original) && value != original) changes[key] = value;
		}
		return changes;
	}

	private async Task ApplyOptionsAsync()
	{
		Dictionary<string, string> changes = OptionsChanged();
		if (changes.Count == 0 || !Confirm("ApplyConfirm", changes.Count)) return;
		await WithChangeAsync(async client =>
		{
			await client.CallAsync("ApplyServerOptions", new { UpdatedServerOptions = changes }, _lifetime.Token);
			foreach (var item in changes) _originalOptions[item.Key] = item.Value;
			connectionStatus.Text = LocalizationManager.Get("Satisfactory.OptionsApplied");
		});
	}

	private async Task RefreshSavesAsync()
	{
		using SatisfactoryApiClient client = SatisfactoryIntegration.CreateClient(_server!);
		JsonElement data = await client.CallAsync("EnumerateSessions", null, _lifetime.Token);
		_lifetime.Token.ThrowIfCancellationRequested();
		savesGrid.Rows.Clear();
		foreach (JsonElement session in data.GetProperty("sessions").EnumerateArray())
			foreach (JsonElement save in session.GetProperty("saveHeaders").EnumerateArray())
				savesGrid.Rows.Add(session.GetProperty("sessionName").GetString() ?? "",
					save.GetProperty("saveName").GetString() ?? "", save.GetProperty("saveDateTime").GetString() ?? "");
	}

	private string SelectedSave() => savesGrid.CurrentRow?.Cells[1].Value?.ToString() is string name && name.Length > 0
		? name : throw new SatisfactoryApiException(SatisfactoryApiError.SelectSave);

	private async Task SaveNowAsync()
	{
		string name = saveNameInput.Text.Trim();
		if (string.IsNullOrEmpty(name)) name = "Synix_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
		if (!Confirm("SaveConfirm", name)) return;
		await WithChangeAsync(async client => await client.CallAsync("SaveGame", new { SaveName = name }, _lifetime.Token));
		await RefreshSavesAsync();
	}

	private async Task LoadSaveAsync()
	{
		string name = SelectedSave();
		if (!Confirm("LoadConfirm", name)) return;
		await WithChangeAsync(async client =>
		{
			// Preserve progress before changing the active session.
			SatisfactoryServerState state = await client.QueryStateAsync(_lifetime.Token);
			if (state.IsGameRunning)
				await client.CallAsync("SaveGame", new { SaveName = "Synix_BeforeLoad_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) }, _lifetime.Token);
			await client.CallAsync("LoadGame", new { SaveName = name, EnableAdvancedGameSettings = false }, _lifetime.Token);
			SatisfactoryIntegration.ClearState(_server!);
			overviewGrid.Rows.Clear();
			connectionStatus.Text = LocalizationManager.Get("Satisfactory.Loading");
		});
	}

	private async Task UploadAsync()
	{
		using OpenFileDialog picker = new() { Filter = LocalizationManager.Get("Satisfactory.SaveFilter"), CheckFileExists = true };
		if (picker.ShowDialog(this) != DialogResult.OK || !Confirm("UploadConfirm", Path.GetFileName(picker.FileName))) return;
		await WithChangeAsync(async client => await client.UploadSaveAsync(picker.FileName, _lifetime.Token));
		await RefreshSavesAsync();
	}

	private async Task DownloadAsync()
	{
		string name = SelectedSave();
		using SaveFileDialog picker = new() { Filter = LocalizationManager.Get("Satisfactory.SaveFilter"), FileName = "Satisfactory.sav", DefaultExt = "sav", OverwritePrompt = true };
		if (picker.ShowDialog(this) != DialogResult.OK) return;
		string temporary = picker.FileName + ".synix-" + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			using SatisfactoryApiClient client = SatisfactoryIntegration.CreateClient(_server!);
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
			timeout.CancelAfter(TimeSpan.FromMinutes(2));
			await using (FileStream file = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				await client.DownloadSaveAsync(name, file, timeout.Token);
			File.Move(temporary, picker.FileName, overwrite: true);
		}
		finally
		{
			if (File.Exists(temporary)) File.Delete(temporary);
		}
	}

	private async Task SendCommandAsync()
	{
		string command = commandInput.Text.Trim();
		if (!SatisfactoryApiClient.IsSafeConsoleCommand(command))
			throw new SatisfactoryApiException(SatisfactoryApiError.Command);
		if (SatisfactoryApiClient.IsStopCommand(command))
		{
			if (!Confirm("StopConfirm")) return;
			_mutating = true;
			try { await Core.Instance.StopServerAndReport(_server!); }
			finally { _mutating = false; }
			return;
		}
		if (!Confirm("CommandConfirm")) return;
		await WithChangeAsync(async client =>
		{
			JsonElement result = await client.CallAsync("RunCommand", new { Command = command }, _lifetime.Token);
			// Do not publish commands/results to activity, disk logs or reports.
			string safeOutput = client.SanitizeOutput(result.GetProperty("commandResult").GetString() ?? "");
			consoleOutput.Text = safeOutput;
		});
	}
}
