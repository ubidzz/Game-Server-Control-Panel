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
namespace Synix_Control_Panel.SynixEngine
{
	public sealed class ApplicationLogEventArgs : EventArgs
	{
		public ApplicationLogEventArgs(
			string technicalMessage,
			string? localizedMessage,
			Color color,
			bool bold)
		{
			TechnicalMessage = SecretRedactor.Redact(technicalMessage);
			LocalizedMessage = localizedMessage == null ? null : SecretRedactor.Redact(localizedMessage);
			Color = color;
			Bold = bold;
		}

		public string TechnicalMessage { get; }
		public string? LocalizedMessage { get; }
		public string Message => TechnicalMessage;
		public Color Color { get; }
		public bool Bold { get; }
	}

	/// <summary>
	/// Provides the engine with neutral UI notifications and dispatching without
	/// coupling background services to the dashboard implementation.
	/// </summary>
	public static class ApplicationUiService
	{
		private static readonly object _syncRoot = new();
		private static WeakReference<Form>? _mainWindow;
		private static Func<bool, Task>? _privacyModeUpdater;

		public static event EventHandler<ApplicationLogEventArgs>? LogRequested;
		public static event EventHandler? GridRefreshRequested;

		public static bool IsAvailable => TryGetMainWindow(out _);

		public static IWin32Window? DialogOwner =>
			TryGetMainWindow(out Form window) ? window : null;

		public static void RegisterMainWindow(
			Form window,
			Func<bool, Task>? privacyModeUpdater = null)
		{
			ArgumentNullException.ThrowIfNull(window);
			lock (_syncRoot)
			{
				_mainWindow = new WeakReference<Form>(window);
				_privacyModeUpdater = privacyModeUpdater;
			}
		}

		public static void UnregisterMainWindow(Form window)
		{
			ArgumentNullException.ThrowIfNull(window);
			lock (_syncRoot)
			{
				if (_mainWindow?.TryGetTarget(out Form? current) == true &&
					ReferenceEquals(current, window))
				{
					_mainWindow = null;
					_privacyModeUpdater = null;
				}
			}
		}

		public static bool IsMainWindow(Form window)
		{
			ArgumentNullException.ThrowIfNull(window);
			return TryGetMainWindow(out Form current) &&
				ReferenceEquals(current, window);
		}

		public static bool PublishLog(string message, Color color, bool bold = false)
		{
			return PublishLog(message, null, color, bold);
		}

		public static bool PublishLog(
			string technicalMessage,
			string? localizedMessage,
			Color color,
			bool bold = false)
		{
			EventHandler<ApplicationLogEventArgs>? handlers = LogRequested;
			if (handlers == null)
				return false;

			ApplicationLogEventArgs eventArgs = new(
				technicalMessage,
				localizedMessage,
				color,
				bold);
			bool delivered = false;
			foreach (Delegate callback in handlers.GetInvocationList())
			{
				try
				{
					EventHandler<ApplicationLogEventArgs> handler =
						(EventHandler<ApplicationLogEventArgs>)callback;
					handler(null, eventArgs);
					delivered = true;
				}
				catch (Exception exception)
				{
					System.Diagnostics.Debug.WriteLine(
						$"[UI LOG DISPATCH ERROR] {exception}");
				}
			}

			return delivered;
		}

		public static void RequestGridRefresh()
		{
			EventHandler? handlers = GridRefreshRequested;
			if (handlers == null)
				return;

			foreach (Delegate callback in handlers.GetInvocationList())
			{
				try
				{
					EventHandler handler = (EventHandler)callback;
					handler(null, EventArgs.Empty);
				}
				catch (Exception exception)
				{
					System.Diagnostics.Debug.WriteLine(
						$"[UI STATUS DISPATCH ERROR] {exception}");
				}
			}
		}

		public static bool TryPost(Action action)
		{
			ArgumentNullException.ThrowIfNull(action);
			if (!TryGetMainWindow(out Form window) || !window.IsHandleCreated)
				return false;
			if (!window.InvokeRequired)
			{
				action();
				return true;
			}

			try
			{
				window.BeginInvoke(action);
				return true;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
		}

		public static void Invoke(Action action)
		{
			ArgumentNullException.ThrowIfNull(action);
			_ = Invoke(
				() =>
				{
					action();
					return true;
				});
		}

		public static T Invoke<T>(Func<T> action)
		{
			ArgumentNullException.ThrowIfNull(action);
			if (!TryGetMainWindow(out Form window) ||
				!window.IsHandleCreated ||
				!window.InvokeRequired)
			{
				return action();
			}

			T? result = default;
			Exception? actionException = null;
			try
			{
				window.Invoke(new MethodInvoker(() =>
				{
					try
					{
						result = action();
					}
					catch (Exception exception)
					{
						actionException = exception;
					}
				}));
			}
			catch (InvalidOperationException) when (
				window.IsDisposed || !window.IsHandleCreated)
			{
				return action();
			}

			if (actionException != null)
				System.Runtime.ExceptionServices.ExceptionDispatchInfo
					.Capture(actionException)
					.Throw();

			return result!;
		}

		public static void SetMainWindowTitle(string title)
		{
			_ = TryPost(() =>
			{
				if (TryGetMainWindow(out Form window))
					window.Text = title;
			});
		}

		public static Task UpdatePrivacyModeAsync(bool isEnabled)
		{
			Func<bool, Task>? updater;
			lock (_syncRoot)
			{
				updater = _privacyModeUpdater;
			}

			if (updater == null || !TryGetMainWindow(out Form window))
				return Task.CompletedTask;
			if (!window.IsHandleCreated || !window.InvokeRequired)
				return updater(isEnabled);

			TaskCompletionSource<bool> completion = new(
				TaskCreationOptions.RunContinuationsAsynchronously);
			try
			{
				window.BeginInvoke(new Action(async () =>
				{
					try
					{
						await updater(isEnabled);
						completion.TrySetResult(true);
					}
					catch (Exception exception)
					{
						completion.TrySetException(exception);
					}
				}));
			}
			catch (InvalidOperationException exception)
			{
				completion.TrySetException(exception);
			}

			return completion.Task;
		}

		private static bool TryGetMainWindow(out Form window)
		{
			lock (_syncRoot)
			{
				if (_mainWindow?.TryGetTarget(out Form? candidate) == true &&
					!candidate.IsDisposed)
				{
					window = candidate;
					return true;
				}
			}

			window = null!;
			return false;
		}
	}
}
