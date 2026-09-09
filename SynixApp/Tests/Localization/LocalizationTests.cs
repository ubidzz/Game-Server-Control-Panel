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

using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Synix_Control_Panel.SynixApp.Localization;
using Synix_Control_Panel.SynixApp.MonitoringHandler;
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class LocalizationTests
{
	[Fact]
	public void SupportedLanguages_IncludeEnglishFrenchGermanAndSpanish()
	{
		Assert.Contains(
			"Synix_Control_Panel.Localization.SupportedLanguages.txt",
			typeof(LocalizationManager).Assembly.GetManifestResourceNames());
		Assert.Contains(
			LocalizationManager.SupportedLanguages,
			language => language.Code == "en-US");
		Assert.Contains(
			LocalizationManager.SupportedLanguages,
			language => language.Code == "fr-FR");
		Assert.Contains(
			LocalizationManager.SupportedLanguages,
			language => language.Code == "de-DE");
		Assert.Contains(
			LocalizationManager.SupportedLanguages,
			language => language.Code == "es-ES");
	}

	[Fact]
	public void LanguageSelection_UsesFrenchAndFallsBackToEnglishSafely()
	{
		try
		{
			LocalizationManager.Initialize("fr-FR");
			Assert.Equal("Enregistrer", LocalizationManager.Get("Text.1509F561F2416598629B"));
			Assert.Equal("Français", LocalizationManager.Get("Language.French"));
			Assert.Equal(
				"PANNEAU DE CONTRÔLE SYNIX  •  v1.2.3",
				LocalizationManager.Get("Settings.VersionLabel", "1.2.3"));

			LocalizationManager.Initialize("unsupported-language");
			Assert.Equal("en-US", LocalizationManager.CurrentLanguageCode);
			Assert.Equal("French", LocalizationManager.Get("Language.French"));
		}
		finally
		{
			LocalizationManager.Initialize(
				LocalizationManager.DefaultLanguageCode);
		}
	}

	[Fact]
	public void LocalizedControl_UpdatesWhenCodeAssignsKnownEnglishText()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				LocalizationManager.Initialize("fr-FR");
				using Label label = new() { Text = "Save Changes" };
				LocalizationManager.Apply(label);
				Assert.Equal("Enregistrer les modifications", label.Text);

				label.Text = "Checking for updates...";
				Assert.Equal("Recherche de mises à jour…", label.Text);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void ControlsAddedAfterAWindowIsLocalizedUseTheSelectedLanguage()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				LocalizationManager.Initialize("de-DE");
				using Panel root = new();
				LocalizationManager.Apply(root);

				using Label addedLater = new() { Text = "Save Changes" };
				root.Controls.Add(addedLater);

				Assert.Equal("Änderungen speichern", addedLater.Text);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void RuntimeLabelValue_ReplacesTranslatedPlaceholderBinding()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				LocalizationManager.Initialize("fr-FR");
				using Label label = new() { Text = "Public IP: Fetching..." };
				LocalizationManager.Apply(label);
				Assert.NotEqual("Public IP: Fetching...", label.Text);

				label.Text = "IP publique : 203.0.113.25";
				LocalizationManager.Apply(label);
				Assert.Equal("IP publique : 203.0.113.25", label.Text);

				LocalizationManager.SetLanguage("en-US");
				LocalizationManager.Apply(label);
				Assert.Equal("IP publique : 203.0.113.25", label.Text);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void GeneralSettings_LanguageAndDownloadChoicesKeepStableValues()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				LocalizationManager.Initialize("fr-FR");
				using GeneralSettingsPage page = new();
				LocalizationManager.Apply(page);

				Assert.Equal(
					"Langue",
					page.Controls.Find("lblTitleLanguage", true).Single().Text);
				ComboBox language = Assert.IsAssignableFrom<ComboBox>(
					page.Controls.Find("cmbLanguage", true).Single());
				Assert.Equal(
					["Anglais", "Allemand", "Espagnol", "Français"],
					language.Items.Cast<object>().Select(item => item.ToString()));

				page.UiLanguageCode = "fr-FR";
				page.LimitSteamCmdDownloadSpeed = true;
				Assert.Equal("fr-FR", page.UiLanguageCode);
				Assert.True(page.LimitSteamCmdDownloadSpeed);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void TranslatedOptions_KeepTheirEnglishInternalValues()
	{
		CultureInfo originalCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
			LocalizationManager.Initialize("fr-FR");
			LocalizedOption option = new(
				"Server Installation",
				"ProblemAction.ServerInstallation");

			Assert.Equal("Installation du serveur", option.ToString());
			Assert.Equal("Server Installation", option.Value);
			Assert.Equal("en-US", CultureInfo.CurrentCulture.Name);
			Assert.Equal("fr-FR", CultureInfo.CurrentUICulture.Name);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			LocalizationManager.Initialize(
				LocalizationManager.DefaultLanguageCode);
		}
	}

	[Fact]
	public void NewlyOpenedWindowsAndInputHintsUseTheSelectedLanguage()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				LocalizationManager.Initialize("fr-FR");
				using AddServerChoiceDialog addServer = new();
				string[] addServerText = GetControlText(addServer).ToArray();
				Assert.Equal("Ajouter un serveur", addServer.Text);
				Assert.Contains(
					"Comment souhaitez-vous ajouter un serveur ?",
					addServerText);
				Assert.Contains("Créer", addServerText);
				Assert.Contains("Voir le catalogue", addServerText);

				using AdvancedSettingsPage advanced = new();
				LocalizationManager.Apply(advanced);
				string[] advancedText = GetControlText(advanced).ToArray();
				Assert.Contains(
					"Nettoyage des règles de pare-feu orphelines",
					advancedText);
				Assert.Contains("Service d’arrière-plan Synix", advancedText);
				advanced.BackgroundServiceEnabled = true;
				Assert.Contains(
					"Activé pour la connexion Windows — Fermer Synix le quitte toujours complètement.",
					GetControlText(advanced));

				using ProblemReportSettingsPage report = new();
				LocalizationManager.Apply(report);
				TextBox summary = Assert.IsAssignableFrom<TextBox>(
					report.Controls.Find("txtSummary", true).Single());
				Label warning = Assert.IsAssignableFrom<Label>(
					report.Controls.Find(
						"lblEnglishReportWarning",
						true).Single());
				Assert.StartsWith("Exemple :", summary.PlaceholderText);
				Assert.True(warning.Visible);
				Assert.Contains("en anglais", warning.Text);

				summary.Text = "Keep this server report in English";
				LocalizationManager.SetLanguage("en-US");
				LocalizationManager.Apply(report);
				Assert.StartsWith("Example:", summary.PlaceholderText);
				Assert.False(warning.Visible);
				Assert.Equal(
					"Keep this server report in English",
					summary.Text);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void ResourceMonitorTranslatesHeadersAndRuntimeSummaries()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				LocalizationManager.Initialize("fr-FR");
				using ResourceMonitorGUI monitor = new();
				DataGridView grid = Assert.IsAssignableFrom<DataGridView>(
					monitor.Controls.Find("resourceGrid", true).Single());
				Assert.Equal(
					"NOM DU SERVEUR",
					LocalizationManager.TranslateKnownText("SERVER NAME"));
				Assert.Equal("ÉTAT", grid.Columns[0].HeaderText);
				Assert.Equal("NOM DU SERVEUR", grid.Columns[1].HeaderText);

				typeof(ResourceMonitorGUI).GetMethod(
					"UpdateSummaryCards",
					System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.NonPublic)!
					.Invoke(monitor,
					[
						new ResourceMonitor.ServerUsage(),
						0
					]);
				Label activeCaption = Assert.IsAssignableFrom<Label>(
					monitor.Controls.Find(
						"lblActiveServersCaption",
						true).Single());
				Label updated = Assert.IsAssignableFrom<Label>(
					monitor.Controls.Find("lblLastUpdated", true).Single());
				Assert.Equal(
					"Aucun processus de serveur en cours détecté",
					activeCaption.Text);
				Assert.StartsWith("Mis à jour à", updated.Text);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void ConnectionPlayerAndModWindowsTranslateRuntimeInterfaceText()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				LocalizationManager.Initialize("fr-FR");
				GameServer server = new()
				{
					Game = "Minecraft",
					ServerName = "Serveur de test",
					Port = 25565,
					QueryPort = 25566,
					InstallPath = Path.Combine(
						Path.GetTempPath(),
						"synix-localization-missing-folder"),
					Status = Core.StatusManager.GetStatus(Core.ServerState.Stopped),
					MinecraftEdition = "Java",
					MinecraftLoader = "Fabric"
				};

				using ConnectionInformationDialog connection = new(server);
				string[] connectionText = GetControlText(connection).ToArray();
				Assert.Contains("Se connecter à Serveur de test", connectionText);
				Assert.Contains("Même ordinateur ou réseau domestique", connectionText);
				Assert.Contains(
					connectionText,
					text => text.StartsWith("Ports configurés : jeu 25565, requête 25566."));

				using PlayerManagementCenter players = new(server);
				string[] playerText = GetControlText(players).ToArray();
				Assert.Contains(
					"Serveur de test • Minecraft • 0 joueurs nommés",
					playerText);
				Assert.Contains("Expulser", playerText);
				Assert.Contains("Ajouter à la liste blanche", playerText);
				DataGridView playerGrid = GetControls<DataGridView>(players).Single();
				Assert.Equal("JOUEUR", playerGrid.Columns[0].HeaderText);

				using ModPluginManager mods = new(server);
				string[] modText = GetControlText(mods).ToArray();
				Assert.Contains("SERVEUR", modText);
				Assert.Contains("SYSTÈME DE MODULES", modText);
				Assert.Contains("Liste de contrôle de sécurité automatique", modText);
				Assert.Contains("Installer un fichier", modText);
				ComboBox[] selectors = GetControls<ComboBox>(mods).ToArray();
				Assert.Equal(2, selectors.Length);
				Assert.Equal("Modules Minecraft", selectors[0].GetItemText(
					selectors[0].Items[0]));
				Assert.Equal("Mods du chargeur", selectors[1].GetItemText(
					selectors[1].Items[0]));
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	private static IEnumerable<string> GetControlText(Control root)
	{
		if (!string.IsNullOrWhiteSpace(root.Text))
			yield return root.Text;

		foreach (Control child in root.Controls)
		{
			foreach (string text in GetControlText(child))
				yield return text;
		}
	}

	private static IEnumerable<TControl> GetControls<TControl>(Control root)
		where TControl : Control
	{
		if (root is TControl typed)
			yield return typed;

		foreach (Control child in root.Controls)
		{
			foreach (TControl nested in GetControls<TControl>(child))
				yield return nested;
		}
	}

	[Fact]
	public void EveryLanguageCatalog_CoversEveryTranslatableStaticEnglishText()
	{
		ResourceManager manager = new(
			"Synix_Control_Panel.Localization.Strings",
			typeof(LocalizationManager).Assembly);
		ResourceSet english = Assert.IsAssignableFrom<ResourceSet>(manager.GetResourceSet(
			CultureInfo.InvariantCulture,
			createIfNotExists: true,
			tryParents: false));
		HashSet<string> intentionallyInvariant = new(StringComparer.Ordinal)
		{
			@"C:\Synix\Games\Example Server",
			"D",
			"Discord",
			"Example Game",
			"GH",
			"Mbps",
			"N/A",
			"PID",
			"PVE",
			"RCON",
			"serverconfig.xml",
			"Synix",
			"XML"
		};

		foreach (string languageCode in new[] { "fr", "de", "es" })
		{
			ResourceSet translated = Assert.IsAssignableFrom<ResourceSet>(
				manager.GetResourceSet(
					CultureInfo.GetCultureInfo(languageCode),
					createIfNotExists: true,
					tryParents: false));
			HashSet<string> translatedKeys = translated
				.Cast<DictionaryEntry>()
				.Select(entry => Assert.IsType<string>(entry.Key))
				.ToHashSet(StringComparer.Ordinal);

			List<string> missing = english
				.Cast<DictionaryEntry>()
				.Where(entry => entry.Key is string key &&
					key.StartsWith("Text.", StringComparison.Ordinal))
				.Select(entry => new
				{
					Key = Assert.IsType<string>(entry.Key),
					Value = Assert.IsType<string>(entry.Value)
				})
				.Where(entry =>
					entry.Value.Any(char.IsLetter) &&
					!intentionallyInvariant.Contains(entry.Value) &&
					!translatedKeys.Contains(entry.Key))
				.Select(entry => entry.Value)
				.OrderBy(value => value, StringComparer.Ordinal)
				.ToList();

			Assert.True(
				missing.Count == 0,
				$"{languageCode} translations are missing: " +
				string.Join(" | ", missing));
		}
	}

	[Fact]
	public void EverySemanticEnglishKey_HasATranslationInEachSupportedLanguage()
	{
		ResourceManager manager = new(
			"Synix_Control_Panel.Localization.Strings",
			typeof(LocalizationManager).Assembly);
		ResourceSet english = Assert.IsAssignableFrom<ResourceSet>(manager.GetResourceSet(
			CultureInfo.InvariantCulture,
			createIfNotExists: true,
			tryParents: false));
		string[] semanticKeys = english
			.Cast<DictionaryEntry>()
			.Select(entry => Assert.IsType<string>(entry.Key))
			.Where(key =>
				!key.StartsWith("Text.", StringComparison.Ordinal) &&
				!key.StartsWith("DynamicText.", StringComparison.Ordinal) &&
				!key.StartsWith("MessageText.", StringComparison.Ordinal))
			.OrderBy(key => key, StringComparer.Ordinal)
			.ToArray();

		foreach (string languageCode in new[] { "fr", "de", "es" })
		{
			ResourceSet translated = Assert.IsAssignableFrom<ResourceSet>(
				manager.GetResourceSet(
					CultureInfo.GetCultureInfo(languageCode),
					createIfNotExists: true,
					tryParents: false));
			HashSet<string> translatedKeys = translated
				.Cast<DictionaryEntry>()
				.Select(entry => Assert.IsType<string>(entry.Key))
				.ToHashSet(StringComparer.Ordinal);
			string[] missing = semanticKeys
				.Where(key => !translatedKeys.Contains(key))
				.ToArray();

			Assert.True(
				missing.Length == 0,
				$"{languageCode} semantic resources are missing: " +
				string.Join(", ", missing));
		}
	}

	[Fact]
	public void EveryRuntimeInterfaceFragment_HasATranslationInEachSupportedLanguage()
	{
		ResourceManager manager = new(
			"Synix_Control_Panel.Localization.Strings",
			typeof(LocalizationManager).Assembly);
		ResourceSet english = Assert.IsAssignableFrom<ResourceSet>(manager.GetResourceSet(
			CultureInfo.InvariantCulture,
			createIfNotExists: true,
			tryParents: false));
		string[] runtimeKeys = english
			.Cast<DictionaryEntry>()
			.Select(entry => Assert.IsType<string>(entry.Key))
			.Where(key =>
				key.StartsWith("DynamicText.", StringComparison.Ordinal) ||
				key.StartsWith("MessageText.", StringComparison.Ordinal))
			.ToArray();

		foreach (string languageCode in new[] { "fr", "de", "es" })
		{
			ResourceSet translated = Assert.IsAssignableFrom<ResourceSet>(
				manager.GetResourceSet(
					CultureInfo.GetCultureInfo(languageCode),
					createIfNotExists: true,
					tryParents: false));
			HashSet<string> translatedKeys = translated
				.Cast<DictionaryEntry>()
				.Select(entry => Assert.IsType<string>(entry.Key))
				.ToHashSet(StringComparer.Ordinal);
			string[] missing = runtimeKeys
				.Where(key => !translatedKeys.Contains(key))
				.ToArray();

			Assert.True(
				missing.Length == 0,
				$"{languageCode} runtime interface resources are missing: " +
				string.Join(", ", missing));
		}
	}

	[Fact]
	public void EveryLanguageCatalog_CoversTheCommonOperationalDialogs()
	{
		ResourceManager manager = new(
			"Synix_Control_Panel.Localization.Strings",
			typeof(LocalizationManager).Assembly);
		ResourceSet english = Assert.IsAssignableFrom<ResourceSet>(manager.GetResourceSet(
			CultureInfo.InvariantCulture,
			createIfNotExists: true,
			tryParents: false));
		string[] requiredEnglishText =
		[
			"Config Save Error",
			"Confirm Update",
			"Discard Changes?",
			"No Server Selected",
			"Server Must Be Stopped",
			"Server Settings Need Attention",
			"Synix Is Busy",
			"Update Ready to Install",
			"You must stop the server before updating it.",
			"You must stop the server before validating server files."
		];
		Dictionary<string, string> required = english
			.Cast<DictionaryEntry>()
			.Where(entry =>
				entry.Key is string key &&
				key.StartsWith("MessageText.", StringComparison.Ordinal) &&
				entry.Value is string value &&
				requiredEnglishText.Contains(value, StringComparer.Ordinal))
			.ToDictionary(
				entry => Assert.IsType<string>(entry.Key),
				entry => Assert.IsType<string>(entry.Value),
				StringComparer.Ordinal);
		Assert.Equal(requiredEnglishText.Length, required.Count);

		foreach (string languageCode in new[] { "fr", "de", "es" })
		{
			ResourceSet translated = Assert.IsAssignableFrom<ResourceSet>(
				manager.GetResourceSet(
					CultureInfo.GetCultureInfo(languageCode),
					createIfNotExists: true,
					tryParents: false));
			Dictionary<string, string> translatedValues = translated
				.Cast<DictionaryEntry>()
				.ToDictionary(
					entry => Assert.IsType<string>(entry.Key),
					entry => Assert.IsType<string>(entry.Value),
					StringComparer.Ordinal);

			foreach ((string key, string englishText) in required)
			{
				Assert.True(translatedValues.TryGetValue(key, out string? value));
				Assert.NotEqual(englishText, value);
			}
		}
	}

	[Fact]
	public void RuntimeAndDialogTranslation_PreserveInsertedServerData()
	{
		try
		{
			LocalizationManager.Initialize("fr-FR");
			string runtime = LocalizationManager.TranslateRuntimeText(
				"Server process started. Waiting for its configured listener (127.0.0.1:7777)");
			Assert.StartsWith(
				"Processus serveur démarré.",
				runtime,
				StringComparison.Ordinal);
			Assert.EndsWith("127.0.0.1:7777)", runtime, StringComparison.Ordinal);

			Assert.Equal(
				"Vous devez arrêter le serveur avant de le mettre à jour.",
				LocalizationManager.TranslateMessageText(
					"You must stop the server before updating it."));
		}
		finally
		{
			LocalizationManager.Initialize(LocalizationManager.DefaultLanguageCode);
		}
	}

	[Fact]
	public void GermanAndSpanishTranslateCoreInterfaceAndServerSetupStatus()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				using Label label = new() { Text = "CONFIGURATION STATUS" };

				LocalizationManager.Initialize("es-ES");
				LocalizationManager.Apply(label);
				Assert.Equal("ESTADO DE LA CONFIGURACIÓN", label.Text);
				Assert.Equal(
					"Consulta el mensaje de validación de abajo",
					LocalizationManager.Get(
						"ServerSetup.Status.SeeValidationMessage"));
				Assert.Equal(
					"Configuración: 70 %",
					LocalizationManager.Get("ServerSetup.Completion", 70));

				label.Text = "CONFIGURATION STATUS";
				LocalizationManager.Initialize("de-DE");
				LocalizationManager.Apply(label);
				Assert.Equal("KONFIGURATIONSSTATUS", label.Text);
				Assert.Equal(
					"Beachte die Validierungsmeldung unten",
					LocalizationManager.Get(
						"ServerSetup.Status.SeeValidationMessage"));
				Assert.Equal(
					"Einrichtung: 70 %",
					LocalizationManager.Get("ServerSetup.Completion", 70));
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void NamedBindings_UpdateEveryInterfaceTextTargetWithoutChangingCommandData()
	{
		Exception? failure = null;
		Thread thread = new(() =>
		{
			try
			{
				const string commandExample =
					"say Server maintenance in 5 minutes";
				using Form root = new();
				using Label label = new();
				using Button button = new();
				using TextBox input = new();
				root.Controls.AddRange([label, button, input]);

				LocalizationManager.Initialize("fr-FR");
				LocalizationManager.BindText(label, "Catalog.Available");
				LocalizationManager.BindAccessibleName(
					button,
					"MinecraftConsole.Quick.AccessibleName",
					commandExample);
				LocalizationManager.BindAccessibleDescription(
					button,
					"GameDefinitions.ArgumentCheck.Launcher.Supported");
				LocalizationManager.BindPlaceholderText(
					input,
					"MinecraftConsole.CommandPlaceholder",
					commandExample);

				Assert.Equal(LocalizationManager.Get("Catalog.Available"), label.Text);
				Assert.Contains(commandExample, button.AccessibleName);
				Assert.Contains(commandExample, input.PlaceholderText);

				LocalizationManager.SetLanguage("de-DE");
				LocalizationManager.Apply(root);

				Assert.Equal(LocalizationManager.Get("Catalog.Available"), label.Text);
				Assert.Equal(
					LocalizationManager.Get(
						"GameDefinitions.ArgumentCheck.Launcher.Supported"),
					button.AccessibleDescription);
				Assert.Equal(
					LocalizationManager.Get(
						"MinecraftConsole.Quick.AccessibleName",
						commandExample),
					button.AccessibleName);
				Assert.Equal(
					LocalizationManager.Get(
						"MinecraftConsole.CommandPlaceholder",
						commandExample),
					input.PlaceholderText);
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				LocalizationManager.Initialize(
					LocalizationManager.DefaultLanguageCode);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		Assert.Null(failure);
	}

	[Fact]
	public void EveryTranslatedResource_PreservesItsFormatPlaceholders()
	{
		ResourceManager manager = new(
			"Synix_Control_Panel.Localization.Strings",
			typeof(LocalizationManager).Assembly);
		ResourceSet english = Assert.IsAssignableFrom<ResourceSet>(
			manager.GetResourceSet(
				CultureInfo.InvariantCulture,
				createIfNotExists: true,
				tryParents: false));
		Dictionary<string, string> englishValues = english
			.Cast<DictionaryEntry>()
			.ToDictionary(
				entry => Assert.IsType<string>(entry.Key),
				entry => Assert.IsType<string>(entry.Value),
				StringComparer.Ordinal);
		Regex placeholder = new(@"\{\d+(?:,[^}:]+)?(?::[^}]+)?\}");

		foreach (string languageCode in new[] { "fr", "de", "es" })
		{
			ResourceSet translated = Assert.IsAssignableFrom<ResourceSet>(
				manager.GetResourceSet(
					CultureInfo.GetCultureInfo(languageCode),
					createIfNotExists: true,
					tryParents: false));
			foreach (DictionaryEntry entry in translated)
			{
				string key = Assert.IsType<string>(entry.Key);
				if (!englishValues.TryGetValue(key, out string? englishText))
					continue;

				string translatedText = Assert.IsType<string>(entry.Value);
				string[] expected = placeholder.Matches(englishText)
					.Select(match => match.Value)
					.OrderBy(value => value, StringComparer.Ordinal)
					.ToArray();
				string[] actual = placeholder.Matches(translatedText)
					.Select(match => match.Value)
					.OrderBy(value => value, StringComparer.Ordinal)
					.ToArray();

				Assert.True(
					expected.SequenceEqual(actual, StringComparer.Ordinal),
					$"{languageCode} changed placeholders for {key}: " +
					$"expected [{string.Join(", ", expected)}], " +
					$"found [{string.Join(", ", actual)}]");
			}
		}
	}

	[Fact]
	public void InterfaceSource_DoesNotAssignVisibleEnglishLiteralsDirectly()
	{
		string projectDirectory = Assert.IsType<string>(
			Core.FindProjectDirectory(AppContext.BaseDirectory));
		Regex directVisibleText = new(
			"(?<!\\$)\\b(?:Text|HeaderText|PlaceholderText|AccessibleName|" +
			"AccessibleDescription|Title|Caption|Heading|Description|Filter)" +
			"\\s*=\\s*\"(?=[^\"\\r\\n]*[A-Za-z])[^\"\\r\\n]*\"");
		Regex directDialogText = new(
			"(?:MessageBox|LocalizedMessageBox)\\.Show\\s*\\(\\s*" +
			"\"[^\"\\r\\n]*[A-Za-z]|" +
			"PlainEnglishErrorDialog\\.ShowError\\s*\\([^,]+,\\s*" +
			"\"[^\"\\r\\n]*[A-Za-z]",
			RegexOptions.Singleline);
		Regex directExceptionText = new(
			"throw\\s+new\\s+[A-Za-z0-9_.<>]+\\s*\\(\\s*" +
			"\"[^\"\\r\\n]*[A-Za-z][^\"\\r\\n]*\"");
		Regex directResultText = new(
			"return\\s+new\\s*\\([^;\\r\\n]*,\\s*" +
			"\"[^\"\\r\\n]*[A-Za-z][^\"\\r\\n]*\"");
		List<string> violations = [];

		foreach (string path in Directory.EnumerateFiles(
			projectDirectory,
			"*.cs",
			SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(projectDirectory, path);
			if (relativePath.StartsWith("Tests" + Path.DirectorySeparatorChar) ||
				relativePath.StartsWith("obj" + Path.DirectorySeparatorChar) ||
				relativePath.Contains(
					Path.DirectorySeparatorChar + "Help" +
					Path.DirectorySeparatorChar,
					StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			string source = File.ReadAllText(path);
			foreach (Match match in directVisibleText.Matches(source)
				.Concat(directDialogText.Matches(source))
				.Concat(directExceptionText.Matches(source))
				.Concat(directResultText.Matches(source)))
			{
				int line = source.AsSpan(0, match.Index).Count('\n') + 1;
				violations.Add($"{relativePath}:{line}: {match.Value}");
			}
		}

		Assert.True(
			violations.Count == 0,
			"Visible text must use LocalizationManager resource bindings. " +
			string.Join(Environment.NewLine, violations));
	}
}
