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
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;

namespace Synix_Control_Panel.SynixApp.Localization;

/// <summary>
/// Provides one shared source for all Synix interface text. The current UI
/// culture is intentionally kept separate from the process formatting culture
/// so game-server files and command-line values remain culture invariant.
/// </summary>
internal static class LocalizationManager
{
	public const string DefaultLanguageCode = "en-US";

	private static readonly ResourceManager ResourceManager = new(
		"Synix_Control_Panel.Localization.Strings",
		typeof(LocalizationManager).Assembly);

	private static readonly IReadOnlyList<SupportedLanguage> Languages =
		DiscoverSupportedLanguages();

	private static readonly IReadOnlyDictionary<string, string>
		StaticTextKeys = BuildStaticTextKeyMap();

	private static readonly IReadOnlyList<TranslationFragment>
		DynamicTextFragments = BuildTranslationFragments("DynamicText.");

	private static readonly IReadOnlyList<TranslationFragment>
		MessageTextFragments = BuildTranslationFragments("MessageText.");

	private static readonly ConditionalWeakTable<object, TextBinding>
		TextBindings = new();

	private static readonly ConditionalWeakTable<Control, AccessibleTextTarget>
		AccessibleTextTargets = new();

	private static readonly ConditionalWeakTable<Control, AccessibleDescriptionTarget>
		AccessibleDescriptionTargets = new();

	private static readonly ConditionalWeakTable<TextBox, PlaceholderTextTarget>
		PlaceholderTextTargets = new();

	private static readonly ConditionalWeakTable<Form, AppliedLanguageVersion>
		AppliedForms = new();

	private static readonly ConditionalWeakTable<Control, TextChangeTracking>
		TrackedTextControls = new();

	private static readonly ConditionalWeakTable<Control, ControlTreeTracking>
		TrackedControlTrees = new();

	private static readonly ConditionalWeakTable<Control, RuntimeTextBinding>
		RuntimeTextBindings = new();

	private static CultureInfo _currentUICulture =
		CultureInfo.GetCultureInfo(DefaultLanguageCode);

	private static int _languageVersion = 1;

	[ThreadStatic]
	private static int _localizedTextWriteDepth;

	public static event EventHandler? LanguageChanged;

	public static string CurrentLanguageCode => _currentUICulture.Name;

	public static IReadOnlyList<SupportedLanguage> SupportedLanguages =>
		Languages;

	public static void Initialize(string? languageCode)
	{
		_currentUICulture = ResolveCulture(languageCode);
		CultureInfo.CurrentUICulture = _currentUICulture;
		CultureInfo.DefaultThreadCurrentUICulture = _currentUICulture;
	}

	public static bool SetLanguage(string? languageCode)
	{
		CultureInfo culture = ResolveCulture(languageCode);
		if (string.Equals(
			culture.Name,
			_currentUICulture.Name,
			StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		_currentUICulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
		_languageVersion++;
		LanguageChanged?.Invoke(null, EventArgs.Empty);
		ApplyToOpenForms();
		return true;
	}

	public static string Get(string resourceKey)
	{
		return ResourceManager.GetString(resourceKey, _currentUICulture)
			?? ResourceManager.GetString(
				resourceKey,
				CultureInfo.InvariantCulture)
			?? $"[{resourceKey}]";
	}

	public static string Get(string resourceKey, params object?[] arguments)
	{
		return string.Format(
			_currentUICulture,
			Get(resourceKey),
			arguments);
	}

	/// <summary>
	/// Returns the invariant English resource text for technical logs and other
	/// support data that must not change with the selected interface language.
	/// </summary>
	public static string GetEnglish(string resourceKey, params object?[] arguments)
	{
		string value = ResourceManager.GetString(
			resourceKey,
			CultureInfo.InvariantCulture) ?? $"[{resourceKey}]";
		return arguments.Length == 0
			? value
			: string.Format(
				CultureInfo.GetCultureInfo(DefaultLanguageCode),
				value,
				arguments);
	}

	/// <summary>
	/// Binds a control directly to a named resource. Unlike assigning the result
	/// of <see cref="Get(string)"/> once, a bound value follows later language
	/// changes without keeping the English source text in window code.
	/// </summary>
	public static void BindText(
		Control control,
		string resourceKey,
		params object?[] arguments)
	{
		ArgumentNullException.ThrowIfNull(control);
		TrackTextChanges(control);
		RuntimeTextBindings.Remove(control);
		BindTextValue(
			control,
			resourceKey,
			arguments,
			value => control.Text = value);
	}

	public static void BindAccessibleName(
		Control control,
		string resourceKey,
		params object?[] arguments)
	{
		ArgumentNullException.ThrowIfNull(control);
		AccessibleTextTarget target = AccessibleTextTargets.GetValue(
			control,
			static value => new AccessibleTextTarget(value));
		BindTextValue(
			target,
			resourceKey,
			arguments,
			value => control.AccessibleName = value);
	}

	public static void BindAccessibleDescription(
		Control control,
		string resourceKey,
		params object?[] arguments)
	{
		ArgumentNullException.ThrowIfNull(control);
		AccessibleDescriptionTarget target =
			AccessibleDescriptionTargets.GetValue(
				control,
				static value => new AccessibleDescriptionTarget(value));
		BindTextValue(
			target,
			resourceKey,
			arguments,
			value => control.AccessibleDescription = value);
	}

	public static void BindPlaceholderText(
		TextBox textBox,
		string resourceKey,
		params object?[] arguments)
	{
		ArgumentNullException.ThrowIfNull(textBox);
		PlaceholderTextTarget target = PlaceholderTextTargets.GetValue(
			textBox,
			static value => new PlaceholderTextTarget(value));
		BindTextValue(
			target,
			resourceKey,
			arguments,
			value => textBox.PlaceholderText = value);
	}

	/// <summary>
	/// Translates a known English interface phrase while leaving server data,
	/// file values, and unknown diagnostic details unchanged.
	/// </summary>
	public static string TranslateKnownText(string text)
	{
		return !string.IsNullOrWhiteSpace(text) &&
			StaticTextKeys.TryGetValue(text, out string? resourceKey)
				? Get(resourceKey)
				: text;
	}

	/// <summary>
	/// Translates changing label and button text while preserving inserted server
	/// names, versions, counts, paths, and exception details.
	/// </summary>
	public static string TranslateRuntimeText(string text)
	{
		string translated = TranslateKnownText(text);
		if (!string.Equals(translated, text, StringComparison.Ordinal))
		{
			return translated;
		}

		return TranslateFragments(text, DynamicTextFragments);
	}

	/// <summary>
	/// Translates confirmation and error-dialog wording without translating the
	/// variable data embedded in the message.
	/// </summary>
	public static string TranslateMessageText(string text)
	{
		string translated = TranslateKnownText(text);
		if (!string.Equals(translated, text, StringComparison.Ordinal))
		{
			return translated;
		}

		translated = TranslateFragments(text, MessageTextFragments);
		return TranslateFragments(translated, DynamicTextFragments);
	}

	/// <summary>
	/// Applies translated text once to each open form for the active language.
	/// Calling this from Application.Idle is inexpensive because already-applied
	/// forms are skipped until the language changes.
	/// </summary>
	public static void ApplyToOpenForms()
	{
		foreach (Form form in Application.OpenForms)
		{
			if (form.IsDisposed)
			{
				continue;
			}

			AppliedLanguageVersion version = AppliedForms.GetOrCreateValue(form);
			if (version.Value == _languageVersion)
			{
				continue;
			}

			Apply(form);
			version.Value = _languageVersion;
		}
	}

	public static void Apply(Control root)
	{
		// The Help Center intentionally remains English so support instructions,
		// screenshots, and terminology match the English support workflow.
		if (root is global::Synix_Control_Panel.SynixApp.UI.Help.HelpGUI)
		{
			return;
		}

		ApplyControl(root);
	}

	private static void ApplyControl(Control control)
	{
		TrackControlTreeChanges(control);

		if (ShouldLocalizeText(control))
		{
			TrackTextChanges(control);
			ApplyControlText(control);
		}

		if (!string.IsNullOrWhiteSpace(control.AccessibleName))
		{
			ApplyBoundText(
				AccessibleTextTargets.GetValue(
					control,
					static value => new AccessibleTextTarget(value)),
				() => control.AccessibleName ?? string.Empty,
				value => control.AccessibleName = value);
		}

		if (!string.IsNullOrWhiteSpace(control.AccessibleDescription))
		{
			ApplyBoundText(
				AccessibleDescriptionTargets.GetValue(
					control,
					static value => new AccessibleDescriptionTarget(value)),
				() => control.AccessibleDescription ?? string.Empty,
				value => control.AccessibleDescription = value);
		}

		if (control is TextBox textBox &&
			!string.IsNullOrWhiteSpace(textBox.PlaceholderText))
		{
			ApplyBoundText(
				PlaceholderTextTargets.GetValue(
					textBox,
					static value => new PlaceholderTextTarget(value)),
				() => textBox.PlaceholderText,
				value => textBox.PlaceholderText = value);
		}

		if (control is MenuStrip menuStrip)
		{
			ApplyToolStripItems(menuStrip.Items);
		}
		else if (control is ContextMenuStrip contextMenuStrip)
		{
			ApplyToolStripItems(contextMenuStrip.Items);
		}
		else if (control is ToolStrip toolStrip)
		{
			ApplyToolStripItems(toolStrip.Items);
		}

		if (control.ContextMenuStrip is ContextMenuStrip attachedMenu)
		{
			ApplyToolStripItems(attachedMenu.Items);
		}

		if (control is DataGridView dataGridView)
		{
			foreach (DataGridViewColumn column in dataGridView.Columns)
			{
				ApplyBoundText(
					column,
					() => column.HeaderText,
					value => column.HeaderText = value);
			}
		}

		if (control is ListView listView)
		{
			foreach (ColumnHeader column in listView.Columns)
			{
				ApplyBoundText(
					column,
					() => column.Text,
					value => column.Text = value);
			}
		}

		foreach (Control child in control.Controls)
		{
			ApplyControl(child);
		}
	}

	private static void TrackControlTreeChanges(Control control)
	{
		if (TrackedControlTrees.TryGetValue(control, out _))
		{
			return;
		}

		TrackedControlTrees.Add(control, new ControlTreeTracking());
		control.ControlAdded += LocalizableControlAdded;
	}

	private static void LocalizableControlAdded(
		object? sender,
		ControlEventArgs eventArgs)
	{
		if (eventArgs.Control is { IsDisposed: false } addedControl)
		{
			ApplyControl(addedControl);
		}
	}

	private static bool ShouldLocalizeText(Control control)
	{
		return control is Form
			or Label
			or ButtonBase
			or GroupBox
			or TabPage;
	}

	private static void TrackTextChanges(Control control)
	{
		if (TrackedTextControls.TryGetValue(control, out _))
		{
			return;
		}

		TrackedTextControls.Add(control, new TextChangeTracking());
		control.TextChanged += LocalizableControlTextChanged;
	}

	private static void ApplyControlText(Control control)
	{
		if (RuntimeTextBindings.TryGetValue(
			control,
			out RuntimeTextBinding? runtimeBinding))
		{
			string translatedRuntime = TranslateRuntimeText(
				runtimeBinding.EnglishText);
			if (!string.Equals(
				control.Text,
				translatedRuntime,
				StringComparison.Ordinal))
			{
				WriteLocalizedText(
					value => control.Text = value,
					translatedRuntime);
			}
			return;
		}

		string englishText = control.Text;
		if (!string.IsNullOrWhiteSpace(englishText) &&
			!StaticTextKeys.ContainsKey(englishText))
		{
			string translatedRuntime = TranslateRuntimeText(englishText);
			if (!string.Equals(
				englishText,
				translatedRuntime,
				StringComparison.Ordinal))
			{
				RuntimeTextBindings.Add(
					control,
					new RuntimeTextBinding(englishText));
				WriteLocalizedText(
					value => control.Text = value,
					translatedRuntime);
				return;
			}
		}

		ApplyBoundText(
			control,
			() => control.Text,
			value => control.Text = value);
	}

	private static void LocalizableControlTextChanged(
		object? sender,
		EventArgs eventArgs)
	{
		if (sender is not Control control || _localizedTextWriteDepth > 0)
		{
			return;
		}

		string englishText = control.Text;
		RuntimeTextBindings.Remove(control);
		if (string.IsNullOrWhiteSpace(englishText) ||
			!StaticTextKeys.TryGetValue(englishText, out string? resourceKey))
		{
			// Labels such as IP addresses and live status summaries begin with a
			// translatable placeholder, then become runtime data. Keeping the old
			// placeholder binding would overwrite that data on the next language
			// refresh.
			TextBindings.Remove(control);
			string translatedRuntime = TranslateRuntimeText(englishText);
			if (!string.Equals(
				englishText,
				translatedRuntime,
				StringComparison.Ordinal))
			{
				RuntimeTextBindings.Add(
					control,
					new RuntimeTextBinding(englishText));
				WriteLocalizedText(
					value => control.Text = value,
					translatedRuntime);
			}
			return;
		}

		TextBindings.Remove(control);
		TextBindings.Add(control, new TextBinding(resourceKey));
		string translated = Get(resourceKey);
		if (!string.Equals(control.Text, translated, StringComparison.Ordinal))
		{
			WriteLocalizedText(value => control.Text = value, translated);
		}
	}

	private static void ApplyToolStripItems(ToolStripItemCollection items)
	{
		foreach (ToolStripItem item in items)
		{
			ApplyBoundText(
				item,
				() => item.Text ?? string.Empty,
				value => item.Text = value);

			if (item is ToolStripDropDownItem dropDownItem)
			{
				ApplyToolStripItems(dropDownItem.DropDownItems);
			}
		}
	}

	private static void ApplyBoundText(
		object target,
		Func<string> readText,
		Action<string> writeText)
	{
		if (!TextBindings.TryGetValue(target, out TextBinding? binding))
		{
			string englishText = readText();
			if (string.IsNullOrWhiteSpace(englishText) ||
				!StaticTextKeys.TryGetValue(englishText, out string? key))
			{
				return;
			}

			binding = new TextBinding(key);
			TextBindings.Add(target, binding);
		}

		string translated = binding.Arguments.Length == 0
			? Get(binding.ResourceKey)
			: Get(binding.ResourceKey, binding.Arguments);
		if (!string.Equals(readText(), translated, StringComparison.Ordinal))
		{
			WriteLocalizedText(writeText, translated);
		}
	}

	private static void BindTextValue(
		object target,
		string resourceKey,
		object?[] arguments,
		Action<string> writeText)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
		TextBindings.Remove(target);
		TextBinding binding = new(resourceKey, arguments);
		TextBindings.Add(target, binding);
		string translated = binding.Arguments.Length == 0
			? Get(binding.ResourceKey)
			: Get(binding.ResourceKey, binding.Arguments);
		WriteLocalizedText(writeText, translated);
	}

	private static void WriteLocalizedText(
		Action<string> writeText,
		string value)
	{
		_localizedTextWriteDepth++;
		try
		{
			writeText(value);
		}
		finally
		{
			_localizedTextWriteDepth--;
		}
	}

	private static IReadOnlyDictionary<string, string> BuildStaticTextKeyMap()
	{
		Dictionary<string, string> keys =
			new(StringComparer.Ordinal);
		IEnumerable<CultureInfo> cultures =
			new[] { CultureInfo.InvariantCulture }
				.Concat(Languages.Select(language =>
					CultureInfo.GetCultureInfo(language.Code)));

		foreach (CultureInfo culture in cultures)
		{
			ResourceSet? resourceSet = ResourceManager.GetResourceSet(
				culture,
				createIfNotExists: true,
				tryParents: true);
			if (resourceSet is null)
			{
				continue;
			}

			foreach (DictionaryEntry entry in resourceSet)
			{
				if (entry.Key is not string key ||
					entry.Value is not string value ||
					(!key.StartsWith("Text.", StringComparison.Ordinal) &&
						value.Contains('{')))
				{
					continue;
				}

				keys.TryAdd(value, key);
			}
		}

		return keys;
	}

	private static IReadOnlyList<TranslationFragment> BuildTranslationFragments(
		string keyPrefix)
	{
		List<TranslationFragment> fragments = [];
		ResourceSet? resourceSet = ResourceManager.GetResourceSet(
			CultureInfo.InvariantCulture,
			createIfNotExists: true,
			tryParents: true);
		if (resourceSet is null)
		{
			return fragments;
		}

		foreach (DictionaryEntry entry in resourceSet)
		{
			if (entry.Key is string key &&
				entry.Value is string value &&
				key.StartsWith(keyPrefix, StringComparison.Ordinal) &&
				!string.IsNullOrWhiteSpace(value))
			{
				fragments.Add(new TranslationFragment(key, value));
			}
		}

		return fragments
			.OrderByDescending(fragment => fragment.EnglishText.Length)
			.ThenBy(fragment => fragment.EnglishText, StringComparer.Ordinal)
			.ToArray();
	}

	private static string TranslateFragments(
		string text,
		IReadOnlyList<TranslationFragment> fragments)
	{
		if (string.IsNullOrWhiteSpace(text) ||
			string.Equals(
				_currentUICulture.Name,
				DefaultLanguageCode,
				StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}

		string translated = text;
		foreach (TranslationFragment fragment in fragments)
		{
			if (!translated.Contains(
				fragment.EnglishText,
				StringComparison.Ordinal))
			{
				continue;
			}

			translated = translated.Replace(
				fragment.EnglishText,
				Get(fragment.ResourceKey),
				StringComparison.Ordinal);
		}

		return translated;
	}

	private static CultureInfo ResolveCulture(string? languageCode)
	{
		SupportedLanguage? language = Languages.FirstOrDefault(candidate =>
			string.Equals(
				candidate.Code,
				languageCode,
				StringComparison.OrdinalIgnoreCase));
		if (language == null && !string.IsNullOrWhiteSpace(languageCode))
		{
			try
			{
				CultureInfo requestedCulture = CultureInfo.GetCultureInfo(languageCode);
				language = Languages.FirstOrDefault(candidate =>
					string.Equals(
						CultureInfo.GetCultureInfo(candidate.Code)
							.TwoLetterISOLanguageName,
						requestedCulture.TwoLetterISOLanguageName,
						StringComparison.OrdinalIgnoreCase));
			}
			catch (CultureNotFoundException suppressedException)
			{
				Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
			}
		}

		language ??= Languages[0];

		return CultureInfo.GetCultureInfo(language.Code);
	}

	private static IReadOnlyList<SupportedLanguage> DiscoverSupportedLanguages()
	{
		List<SupportedLanguage> languages =
		[
			new(DefaultLanguageCode, "Language.English", "English")
		];
		string satelliteAssemblyName =
			$"{typeof(LocalizationManager).Assembly.GetName().Name}.resources.dll";
		Assembly assembly = typeof(LocalizationManager).Assembly;

		try
		{
			using Stream? manifestStream = assembly.GetManifestResourceStream(
				"Synix_Control_Panel.Localization.SupportedLanguages.txt");
			if (manifestStream != null)
			{
				using StreamReader reader = new(
					manifestStream,
					Encoding.UTF8,
					detectEncodingFromByteOrderMarks: true,
					leaveOpen: false);
				while (reader.ReadLine() is string line)
				{
					string cultureName = line.Trim();
					if (cultureName.StartsWith("Strings.", StringComparison.Ordinal))
						cultureName = cultureName["Strings.".Length..];
					TryAddSupportedLanguage(languages, cultureName);
				}
			}
		}
		catch (IOException suppressedException)
		{
			Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
		}

		try
		{
			foreach (string directory in Directory.EnumerateDirectories(
				AppContext.BaseDirectory))
			{
				if (!File.Exists(Path.Combine(directory, satelliteAssemblyName)))
					continue;

				TryAddSupportedLanguage(
					languages,
					Path.GetFileName(directory));
			}
		}
		catch (IOException suppressedException)
		{
			Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
		}
		catch (UnauthorizedAccessException suppressedException)
		{
			Synix_Control_Panel.SynixEngine.ApplicationLogService.WriteSuppressedException(suppressedException);
		}

		return languages
			.OrderBy(language => language.Code == DefaultLanguageCode ? 0 : 1)
			.ThenBy(language => language.DisplayName, StringComparer.CurrentCulture)
			.ToArray();
	}

	private static void TryAddSupportedLanguage(
		ICollection<SupportedLanguage> languages,
		string? cultureName)
	{
		if (string.IsNullOrWhiteSpace(cultureName))
			return;

		CultureInfo resourceCulture;
		try
		{
			resourceCulture = CultureInfo.GetCultureInfo(cultureName);
		}
		catch (CultureNotFoundException)
		{
			return;
		}

		CultureInfo selectionCulture = resourceCulture.IsNeutralCulture
			? CultureInfo.CreateSpecificCulture(resourceCulture.Name)
			: resourceCulture;
		if (languages.Any(language => string.Equals(
			language.Code,
			selectionCulture.Name,
			StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}

		string languageCode = selectionCulture.TwoLetterISOLanguageName;
		string? resourceKey = languageCode.ToLowerInvariant() switch
		{
			"de" => "Language.German",
			"es" => "Language.Spanish",
			"fr" => "Language.French",
			_ => null
		};
		string nativeName = languageCode.ToLowerInvariant() switch
		{
			"de" => "Deutsch",
			"es" => "Español",
			"fr" => "Français",
			_ => selectionCulture.TextInfo.ToTitleCase(
				selectionCulture.NativeName)
		};
		languages.Add(new SupportedLanguage(
			selectionCulture.Name,
			resourceKey,
			nativeName));
	}

	internal sealed record SupportedLanguage(
		string Code,
		string? ResourceKey,
		string DisplayName);

	private sealed class TextBinding
	{
		public TextBinding(
			string resourceKey,
			IEnumerable<object?>? arguments = null)
		{
			ResourceKey = resourceKey;
			Arguments = arguments?.ToArray() ?? [];
		}

		public string ResourceKey { get; }
		public object?[] Arguments { get; }
	}

	private sealed class AppliedLanguageVersion
	{
		public int Value { get; set; }
	}

	private sealed class TextChangeTracking
	{
	}

	private sealed class ControlTreeTracking
	{
	}

	private sealed class RuntimeTextBinding
	{
		public RuntimeTextBinding(string englishText)
		{
			EnglishText = englishText;
		}

		public string EnglishText { get; }
	}

	private sealed record TranslationFragment(
		string ResourceKey,
		string EnglishText);

	/// <summary>
	/// Gives AccessibleName its own weak-table identity instead of sharing the
	/// control's visible-text binding.
	/// </summary>
	private sealed class AccessibleTextTarget
	{
		public AccessibleTextTarget(Control control)
		{
			Control = control;
		}

		public Control Control { get; }
	}

	private sealed class AccessibleDescriptionTarget
	{
		public AccessibleDescriptionTarget(Control control)
		{
			Control = control;
		}

		public Control Control { get; }
	}

	/// <summary>
	/// Keeps a text-box hint separate from its editable value. Placeholder text
	/// is interface text; Text remains user or server data and is never changed.
	/// </summary>
	private sealed class PlaceholderTextTarget
	{
		public PlaceholderTextTarget(TextBox textBox)
		{
			TextBox = textBox;
		}

		public TextBox TextBox { get; }
	}
}
