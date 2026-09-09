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
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.ServerHandler;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Synix_Control_Panel.SynixEngine
{
	public partial class Core
	{
		private static readonly Regex SafeRegex = new Regex(@"^[a-zA-Z0-9\s\-+:\""\\/._=?,!@#$%&*'()]*$", RegexOptions.Compiled);
		private const int MaximumExtraArgumentsLength = 16_384;

		public bool CanServerStart(GameServer server, out string errorMessage)
		{
			var dbEntry = GameDatabase.GetGame(server.Game);
			if (dbEntry == null)
			{
				errorMessage = LocalizationManager.Get(
					"Validator.GameDefinitionMissing");
				return false;
			}

			string fullPath = GameLaunchCommandBuilder.ResolveExecutablePath(server, dbEntry);
			if (!File.Exists(fullPath))
			{
				errorMessage = LocalizationManager.Get(
					"Validator.GameFilesMissing");
				return false;
			}

			foreach ((int port, string name) in GetRequiredServerPorts(server, dbEntry))
			{
				if (!IsPortInUseLocally(port))
					continue;

				errorMessage = LocalizationManager.Get(
					"Validator.PortInUse",
					name,
					port);
				return false;
			}

			errorMessage = "";
			return true;
		}

		internal static IReadOnlyList<(int Port, string Name)> GetRequiredServerPorts(
			GameServer server,
			GameInfo game)
		{
			ArgumentNullException.ThrowIfNull(server);
			ArgumentNullException.ThrowIfNull(game);

			List<(int Port, string Name)> ports = [];
			HashSet<int> added = [];
			GameManagementCapability capabilities =
				GameFix.GetManagementCapabilities(game);
			bool Supports(GameManagementCapability capability) =>
				(capabilities & capability) != GameManagementCapability.None;

			void Add(int port, string name)
			{
				if (port is >= 1 and <= 65535 && added.Add(port))
					ports.Add((port, name));
			}

			if (Supports(GameManagementCapability.Port))
				Add(server.Port, LocalizationManager.Get("PortRole.Game"));

			if (Supports(GameManagementCapability.QueryPort))
				Add(server.QueryPort, LocalizationManager.Get("PortRole.Query"));

			if (server.EnableRcon && Supports(GameManagementCapability.Rcon))
			{
				Add(server.RconPort, LocalizationManager.Get("PortRole.Rcon"));
			}

			if (Supports(GameManagementCapability.AppPort) &&
				server.AppPort.HasValue)
			{
				Add(server.AppPort.Value, LocalizationManager.Get("PortRole.App"));
			}

			return ports;
		}

		public bool ValidateNameAndReport(string name, string game, GameServer? excluding = null)
		{
			bool exists = ServerRegistry.Servers.Any(s =>
				s.Game.Equals(game, StringComparison.OrdinalIgnoreCase) &&
				s.ServerName.Equals(name, StringComparison.OrdinalIgnoreCase) &&
				s != excluding);

			if (exists)
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"Validator.DuplicateName.Body",
						game,
						name),
					LocalizationManager.Get("Validator.DuplicateName.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return false;
			}
			return true;
		}

		public bool ValidatePortsAndReport(
			GameServer? excluding,
			int game,
			int query,
			int rcon,
			bool checkRcon,
			int app,
			bool checkAppPort,
			string gameName,
			bool checkGamePort = true,
			bool checkQueryPort = true)
		{
			var portChecks = new List<(int Value, string Name)>();

			if (checkGamePort)
				portChecks.Add((game, LocalizationManager.Get("PortRole.Game.Title")));

			if (checkQueryPort)
				portChecks.Add((query, LocalizationManager.Get("PortRole.Query.Title")));

			if (checkRcon)
				portChecks.Add((rcon, LocalizationManager.Get("PortRole.Rcon.Title")));

			if (checkAppPort)
				portChecks.Add((app, LocalizationManager.Get("PortRole.RustPlus.Title")));

			var duplicateSelection = portChecks
				.GroupBy(check => check.Value)
				.FirstOrDefault(group => group.Count() > 1);
			if (duplicateSelection != null)
			{
				string roles = string.Join(
					LocalizationManager.Get(
						"Validator.PortConflict.RolesSeparator"),
					duplicateSelection.Select(check =>
						LocalizationManager.TranslateRuntimeText(check.Name)));
				LocalizedMessageBox.Show(
					LocalizationManager.Get(
						"Validator.PortConflict.Duplicate.Body",
						roles,
						duplicateSelection.Key),
					LocalizationManager.Get(
						"Validator.NetworkConflict.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Stop);
				return false;
			}

			foreach (var check in portChecks)
			{
				var owner = GetConfiguredPortCollisionOwner(check.Value, excluding);
				if (owner != null)
				{
					LocalizedMessageBox.Show(
						LocalizationManager.Get(
							"Validator.PortConflict.Configured.Body",
							LocalizationManager.TranslateRuntimeText(check.Name),
							check.Value,
							owner),
						LocalizationManager.Get(
							"Validator.NetworkConflict.Title"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Stop);
					return false;
				}

				if (IsPortInUseLocally(check.Value))
				{
					LocalizedMessageBox.Show(
						LocalizationManager.Get(
							"Validator.PortConflict.Socket.Body",
							LocalizationManager.TranslateRuntimeText(check.Name),
							check.Value),
						LocalizationManager.Get(
							"Validator.SystemConflict.Title"),
						MessageBoxButtons.OK,
						MessageBoxIcon.Stop);
					return false;
				}
			}

			if (checkAppPort && gameName.Contains("Rust", StringComparison.OrdinalIgnoreCase) && app < 10000)
			{
				LocalizedMessageBox.Show(
					LocalizationManager.Get("Validator.RustAppPort.Body"),
					LocalizationManager.Get("Validator.LogicError.Title"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return false;
			}

			return true;
		}

		public bool ValidateFolderAndReport(string path, bool isEditMode)
		{
			if (!isEditMode && Directory.Exists(path))
			{
				if (Directory.EnumerateFileSystemEntries(path).Any())
				{
					var result = LocalizedMessageBox.Show(
						LocalizationManager.Get("Validator.FolderNotEmpty.Body"),
						LocalizationManager.Get("Validator.FolderNotEmpty.Title"),
						MessageBoxButtons.YesNo);
					return result == DialogResult.Yes;
				}
			}
			return true;
		}

		public bool ShouldBlockForConfig(GameServer server)
		{
			if (server.IsFirstBoot)
			{
				DialogResult result = DialogResult.Cancel;
				ApplicationLogService.WriteLocalized(
					"Validator.Activity.OpeningConfigurationWarning",
					Color.Yellow,
					arguments: [server.ServerName]);
				ApplicationUiService.Invoke(() =>
				{
					using (var warningForm = new WarningDatabase(server))
					{
						result = warningForm.ShowDialog(
							ApplicationUiService.DialogOwner);
					}
				});

				return result != DialogResult.OK;
			}

			return false;
		}

		public bool ValidateIntegrityAndReport(GameServer server, bool showDialog = true)
		{
			if (!CanServerStart(server, out string errorMessage))
			{
				ApplicationLogService.WriteLocalized(
					"Validator.Activity.Error",
					Color.Red,
					true,
					errorMessage);

				if (showDialog)
				{
					ApplicationUiService.Invoke(() =>
						PlainEnglishErrorDialog.ShowError(
							ApplicationUiService.DialogOwner,
							LocalizationManager.Get(
								"Validator.ErrorAction.StartServer"),
							errorMessage));
				}

				server.Status = "Needs Repair";

				return false;
			}

			return true;
		}

		public bool PassSpamLock(GameServer server, out string lockMessage, string serverTrigger)
		{
			lockMessage = string.Empty;
			string status = server.Status ?? "";

			bool isTransitioning = status.StartsWith(StatusManager.GetStatus(ServerState.Starting), StringComparison.OrdinalIgnoreCase) ||
								   status.StartsWith(StatusManager.GetStatus(ServerState.Stopping), StringComparison.OrdinalIgnoreCase) ||
								   status.StartsWith(StatusManager.GetStatus(ServerState.Installing), StringComparison.OrdinalIgnoreCase) ||
								   status.StartsWith(StatusManager.GetStatus(ServerState.Updating), StringComparison.OrdinalIgnoreCase) ||
								   status.StartsWith(StatusManager.GetStatus(ServerState.BackingUp), StringComparison.OrdinalIgnoreCase) ||
								   status.StartsWith(StatusManager.GetStatus(ServerState.Restoring), StringComparison.OrdinalIgnoreCase) ||
								   status.StartsWith(StatusManager.GetStatus(ServerState.Export), StringComparison.OrdinalIgnoreCase) ||
								   status.StartsWith(StatusManager.GetStatus(ServerState.Validating), StringComparison.OrdinalIgnoreCase) ||
								   status.StartsWith(StatusManager.GetStatus(ServerState.Deleting), StringComparison.OrdinalIgnoreCase);

			bool isRunning = status == StatusManager.GetStatus(ServerState.Running);
			bool isStopped = status == StatusManager.GetStatus(ServerState.Stopped);
			bool isCrashed = status == StatusManager.GetStatus(ServerState.Crashed);

			bool isLocked = false;

			switch (serverTrigger)
			{
				case "Backup":
				case "Delete":
				case "EditConfig":
				case "Validate":
				case "Config":
				case "Update":
				case "Start":
				case "Export":
					isLocked = isTransitioning || isRunning;
					break;
				case "Restore":
					isLocked = !isStopped || server.PID.HasValue;
					break;
				case "Restart":
					isLocked = isTransitioning || isStopped;
					break;
				case "Stop":
					isLocked = isTransitioning || isStopped || isCrashed;
					break;
			}

			if (isLocked)
			{
				lockMessage = LocalizationManager.Get(
					"Validator.OperationLocked",
					LocalizationManager.Get($"ServerOperation.Name.{serverTrigger}"),
					server.ServerName,
					status);
				return false;
			}

			return true;
		}

		public string? GetPortCollisionOwner(
			int port,
			GameServer? excluding = null,
			bool activeOnly = false)
		{
			GameServer? owner = ServerRegistry.Servers.FirstOrDefault(server =>
			{
				if (server == excluding)
					return false;
				if (activeOnly && !IsActivePortReservation(server))
					return false;

				GameInfo? gameData = GameDatabase.GetGame(server.Game);
				return gameData != null &&
					GetRequiredServerPorts(server, gameData).Any(required =>
						required.Port == port);
			});

			return owner?.ServerName;
		}

		public string? GetConfiguredPortCollisionOwner(
			int port,
			GameServer? excluding = null)
		{
			GameServer? owner = ServerRegistry.Servers.FirstOrDefault(server =>
			{
				if (server == excluding)
					return false;

				return HasConfiguredPort(server, port);
			});

			return owner?.ServerName;
		}

		internal static bool HasConfiguredPort(GameServer server, int port)
		{
			ArgumentNullException.ThrowIfNull(server);
			return server.Port == port ||
				server.QueryPort == port ||
				(server.EnableRcon && server.RconPort == port) ||
				server.AppPort == port ||
				(MinecraftControlProfile.IsJava(server) &&
					server.MinecraftManagementPort == port);
		}

		internal static bool IsActivePortReservation(GameServer server)
		{
			ArgumentNullException.ThrowIfNull(server);
			string status = server.Status ?? string.Empty;
			return status.Equals(
					StatusManager.GetStatus(ServerState.Running),
					StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith(
					StatusManager.GetStatus(ServerState.Starting),
					StringComparison.OrdinalIgnoreCase) ||
				status.StartsWith(
					StatusManager.GetStatus(ServerState.Stopping),
					StringComparison.OrdinalIgnoreCase);
		}

		public bool PassResourceGuard(out string message)
		{
			message = string.Empty;

			if (TotalCpuUsage >= 80.0)
			{
				message = LocalizationManager.Get(
					"Validator.ResourceGuard.Cpu",
					TotalCpuUsage);
				return false;
			}

			double currentRamPercent = (TotalRamUsageGb / TotalRamGb) * 100;

			if (currentRamPercent >= 85.0)
			{
				message = LocalizationManager.Get(
					"Validator.ResourceGuard.Ram",
					currentRamPercent);
				return false;
			}

			return true;
		}

		public static bool IsStringSafe(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return true;

			if (input.Contains("..")) return false;

			return SafeRegex.IsMatch(input);
		}

		public static bool TryValidateExtraArguments(
			string? arguments,
			out string errorMessage)
		{
			errorMessage = string.Empty;
			if (string.IsNullOrWhiteSpace(arguments))
				return true;

			if (arguments.Length > MaximumExtraArgumentsLength)
			{
				errorMessage = LocalizationManager.Get(
					"Validation.ExtraArguments.TooLong",
					MaximumExtraArgumentsLength);
				return false;
			}

			if (arguments.IndexOfAny(['\0', '\r', '\n']) >= 0)
			{
				errorMessage = LocalizationManager.Get(
					"Validation.ExtraArguments.LineBreaks");
				return false;
			}

			if (ContainsBatchVariableExpansion(arguments, '%'))
			{
				errorMessage = LocalizationManager.Get(
					"Validation.ExtraArguments.PercentExpansion");
				return false;
			}

			if (ContainsBatchVariableExpansion(arguments, '!'))
			{
				errorMessage = LocalizationManager.Get(
					"Validation.ExtraArguments.DelayedExpansion");
				return false;
			}

			bool insideQuotes = false;
			for (int index = 0; index < arguments.Length; index++)
			{
				char character = arguments[index];
				if (!insideQuotes && character == '^')
				{
					errorMessage = LocalizationManager.Get(
						"Validation.ExtraArguments.EscapeOperator");
					return false;
				}

				if (character == '"')
				{
					insideQuotes = !insideQuotes;
					continue;
				}

				if (!insideQuotes && character is '&' or '|' or '<' or '>')
				{
					string commandOperator = index + 1 < arguments.Length &&
						arguments[index + 1] == character &&
						character is '&' or '|'
							? new string(character, 2)
							: character.ToString();
					errorMessage = LocalizationManager.Get(
						"Validation.ExtraArguments.CommandOperator",
						commandOperator);
					return false;
				}
			}

			if (insideQuotes)
			{
				errorMessage = LocalizationManager.Get(
					"Validation.ExtraArguments.UnclosedQuote");
				return false;
			}

			return true;
		}

		public static string EscapeWindowsBatchCommandLine(string commandLine)
		{
			ArgumentNullException.ThrowIfNull(commandLine);
			if (commandLine.IndexOfAny(['\0', '\r', '\n']) >= 0)
				throw new ArgumentException(
					LocalizationManager.Get("Validation.BatchCommand.LineBreaks"),
					nameof(commandLine));

			StringBuilder escaped = new(commandLine.Length + 16);
			bool insideQuotes = false;
			foreach (char character in commandLine)
			{
				if (character == '"')
				{
					insideQuotes = !insideQuotes;
					escaped.Append(character);
					continue;
				}

				if (character == '%')
				{
					escaped.Append("%%");
					continue;
				}

				if (!insideQuotes && character == '^')
				{
					escaped.Append("^^");
					continue;
				}

				if (!insideQuotes && character is '&' or '|' or '<' or '>' or '(' or ')')
					escaped.Append('^');

				escaped.Append(character);
			}

			if (insideQuotes)
				throw new ArgumentException(LocalizationManager.Get("Validation.ExtraArguments.UnclosedQuote"), nameof(commandLine));
			return escaped.ToString();
		}

		internal static string EscapeWindowsBatchQuotedValue(string value)
		{
			ArgumentNullException.ThrowIfNull(value);
			if (value.IndexOfAny(['\0', '\r', '\n', '"']) >= 0)
				throw new ArgumentException(LocalizationManager.Get("LaunchCommand.Error.UnsafeBatchArguments"), nameof(value));
			return value.Replace("%", "%%", StringComparison.Ordinal);
		}

		private static bool ContainsBatchVariableExpansion(string value, char delimiter)
		{
			for (int opening = 0; opening < value.Length; opening++)
			{
				if (value[opening] != delimiter)
					continue;

				if (delimiter == '%' && opening + 1 < value.Length)
				{
					char parameter = value[opening + 1];
					if (char.IsDigit(parameter) || parameter == '*' ||
						(parameter == '~' && opening + 2 < value.Length &&
						 (char.IsDigit(value[opening + 2]) || value[opening + 2] == '*')))
					{
						return true;
					}
				}

				int closing = value.IndexOf(delimiter, opening + 1);
				if (closing <= opening + 1)
					continue;

				bool looksLikeVariable = true;
				for (int index = opening + 1; index < closing; index++)
				{
					if (char.IsWhiteSpace(value[index]) || value[index] is '"' or '\'')
					{
						looksLikeVariable = false;
						break;
					}
				}

				if (looksLikeVariable)
					return true;
			}

			return false;
		}

		public static bool IsGameServerConfigSafe(object obj)
		{
			if (obj == null) return false;

			if (obj is string directString)
			{
				return IsStringSafe(directString);
			}

			PropertyInfo[] properties = obj.GetType().GetProperties();

			foreach (var prop in properties)
			{
				if (prop.PropertyType == typeof(string))
				{
					string value = (string?)prop.GetValue(obj) ?? string.Empty;
					bool isSafe = prop.Name.Equals(
						nameof(GameServer.ExtraArgs),
						StringComparison.Ordinal)
						? TryValidateExtraArguments(value, out _)
						: IsStringSafe(value);

					if (!isSafe)
					{
						Core.Instance.LogLocalized(
							"Validator.Activity.UnsafeProperty",
							arguments: [prop.Name]);
						return false;
					}
				}
			}
			return true;
		}

		public static int GetSystemJavaVersion()
		{
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo
				{
					FileName = "java",
					Arguments = "-version",
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				using Process proc = Process.Start(psi) ??
					throw new InvalidOperationException(
						LocalizationManager.Get("Validator.Java.StartFailed"));
				string output = proc.StandardError.ReadToEnd();
				proc.WaitForExit();

				if (output.Contains("version \"1.8")) return 8;
				if (output.Contains("version \"1.7")) return 7;

				int startIndex = output.IndexOf("version \"") + 9;
				if (startIndex > 8)
				{
					int endIndex = output.IndexOf('.', startIndex);
					if (endIndex == -1) endIndex = output.IndexOf('"', startIndex);

					if (endIndex > startIndex)
					{
						string versionStr = output.Substring(startIndex, endIndex - startIndex);
						if (int.TryParse(versionStr, out int version)) return version;
					}
				}
			}
			catch (Exception suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
			return 0;
		}
	}
}
