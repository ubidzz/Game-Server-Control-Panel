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
using System.Text;
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.Database.GameDefinitions;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class YamlConfigurationTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "SynixYamlTests", Guid.NewGuid().ToString("N"));
	private string ConfigPath => Path.Combine(_root, "dedicated.yaml");

	public YamlConfigurationTests() => Directory.CreateDirectory(_root);

	[Theory]
	[InlineData("dedicated.yaml")]
	[InlineData("settings.yml")]
	[InlineData("SERVER.YAML")]
	public void YamlExtensionsAreRecognized(string fileName)
	{
		Assert.True(ConfigHandler.TryGetFormatFromPath(fileName, out ConfigFormat format));
		Assert.Equal(ConfigFormat.YAML, format);
		Assert.Equal("YAML", ConfigHandler.GetFormatDisplayName(format));
		Assert.Equal(0, (int)ConfigFormat.StandardINI);
		Assert.Equal(5, (int)ConfigFormat.SII);
	}

	[Fact]
	public void ReadsNestedMappingsListsQuotesAndTypesWithoutExposingCommentedSettings()
	{
		const string yaml = """
			---
			ServerConfig:
			    Srv_Name: My Server
			    Srv_Port: 30000
			    Srv_Public: true # published
			    Srv_Password: 'Joe''s # password'
			    Tel_Pwd: "test-secret"
			    # Tel_Enabled: true
			GameConfig:
			    Name: 'true'
			    Seed: "00123"
			    Empty:
			    EmptyComment: # empty too
			    Default: ~
			    Url: https://example.test/#fragment
			    Message: "A \"quote\" \x41 \u263A \U0001F680"
			Players:
			  - Name: One
			    Enabled: false
			  - Name: Two
			    Enabled: true
			Labels:
			- alpha
			- beta
			...
			""";
		Dictionary<string, ConfigLine> values = Load(yaml).ToDictionary(value => value.Path);
		Assert.Equal("My Server", values["ServerConfig.Srv_Name"].Value);
		Assert.Equal(ConfigValueType.Number, values["ServerConfig.Srv_Port"].Type);
		Assert.Equal(ConfigValueType.Boolean, values["ServerConfig.Srv_Public"].Type);
		Assert.Equal("Joe's # password", values["ServerConfig.Srv_Password"].Value);
		Assert.Equal(ConfigValueType.Secret, values["ServerConfig.Srv_Password"].Type);
		Assert.Equal(ConfigValueType.Secret, values["ServerConfig.Tel_Pwd"].Type);
		Assert.DoesNotContain("ServerConfig.Tel_Enabled", values.Keys);
		Assert.Equal(ConfigValueType.Text, values["GameConfig.Name"].Type);
		Assert.Equal(ConfigValueType.Text, values["GameConfig.Seed"].Type);
		Assert.Equal(ConfigValueType.Null, values["GameConfig.Empty"].Type);
		Assert.Equal("null", values["GameConfig.EmptyComment"].Value);
		Assert.Equal("null", values["GameConfig.Default"].Value);
		Assert.Equal("https://example.test/#fragment", values["GameConfig.Url"].Value);
		Assert.Equal("A \"quote\" A ☺ 🚀", values["GameConfig.Message"].Value);
		Assert.Equal("Two", values["Players[1].Name"].Value);
		Assert.Equal("beta", values["Labels[1]"].Value);
	}

	[Theory]
	[InlineData("\r\n", 0)]
	[InlineData("\n", 1)]
	[InlineData("\r", 2)]
	public void EditingPreservesCommentsSpacingEncodingAndEveryUnchangedByte(string newline, int encodingId)
	{
		string original = string.Join(newline,
			"# keep café", "ServerConfig:", "    Srv_Name: 'My Server'   # keep this",
			"    Srv_Port: 30000", "    Srv_Public: true", "    # disabled: value", "");
		Encoding encoding = encodingId switch
		{
			0 => new UTF8Encoding(false), 1 => new UTF8Encoding(true), _ => Encoding.Unicode
		};
		File.WriteAllText(ConfigPath, original, encoding);
		List<ConfigLine> values = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		values.Single(value => value.Key == "Srv_Name").Value = "Player's # server";
		values.Single(value => value.Key == "Srv_Port").Value = "30100";
		string expected = original.Replace("'My Server'", "'Player''s # server'").Replace("30000", "30100");

		Assert.Equal(expected, ConfigHandler.CreatePreview(ConfigPath, values, ConfigFormat.YAML));
		Assert.Equal(original, File.ReadAllText(ConfigPath, encoding));
		ConfigHandler.SaveConfig(ConfigPath, values, ConfigFormat.YAML);
		Assert.Equal(encoding.GetPreamble().Concat(encoding.GetBytes(expected)), File.ReadAllBytes(ConfigPath));
		byte[] saved = File.ReadAllBytes(ConfigPath);
		ConfigHandler.SaveConfig(ConfigPath, ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML), ConfigFormat.YAML);
		Assert.Equal(saved, File.ReadAllBytes(ConfigPath));
	}

	[Theory]
	[InlineData("")]
	[InlineData("false")]
	[InlineData("123")]
	[InlineData("null")]
	[InlineData("yes")]
	[InlineData(" # comment")]
	[InlineData("name: value")]
	[InlineData("{Port}")]
	[InlineData("&anchor")]
	[InlineData("*alias")]
	[InlineData("!tag")]
	[InlineData("[one, two]")]
	[InlineData(" \"quotes\" \\ path 🚀 ")]
	public void TextChangesRemainLiteralAndCannotBecomeYamlStructure(string text)
	{
		File.WriteAllText(ConfigPath, "ServerConfig:\n  Srv_Name: Original # trailing\n  Keep: 7\n");
		List<ConfigLine> values = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		values.Single(value => value.Key == "Srv_Name").Value = text;
		ConfigHandler.SaveConfig(ConfigPath, values, ConfigFormat.YAML);
		List<ConfigLine> saved = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		Assert.Equal(2, saved.Count);
		Assert.Equal(text, saved[0].Value);
		Assert.Equal(ConfigValueType.Text, saved[0].Type);
		Assert.Equal("7", saved[1].Value);
		Assert.EndsWith(" # trailing\n  Keep: 7\n", File.ReadAllText(ConfigPath));
	}

	[Fact]
	public void RepeatedLeafNamesAndQuotedKeysHaveDifferentIdentities()
	{
		File.WriteAllText(ConfigPath, "A:\n  Name: One\nB:\n  Name: Two\n\"A.Name\": Three\n");
		List<ConfigLine> values = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		Assert.Equal(3, values.Select(value => value.Id).Distinct().Count());
		values.Single(value => value.Path == "B.Name").Value = "Changed";
		ConfigHandler.SaveConfig(ConfigPath, values, ConfigFormat.YAML);
		Assert.Equal("A:\n  Name: One\nB:\n  Name: \"Changed\"\n\"A.Name\": Three\n", File.ReadAllText(ConfigPath));
	}

	[Theory]
	[InlineData("A:\n\tPort: 30000")]
	[InlineData("A:\n  Port: 1\n Port: 2")]
	[InlineData("Port: 1\nPort: 2")]
	[InlineData("Port: 1\n\"Port\": 2")]
	[InlineData("A:\n  Name: x\nA:\n  Port: 2")]
	[InlineData("A: &anchor value")]
	[InlineData("A: *anchor")]
	[InlineData("A: !custom value")]
	[InlineData("A: !!str value")]
	[InlineData("<<: *defaults")]
	[InlineData("A: [one, two]")]
	[InlineData("A: {name: test}")]
	[InlineData("A: |\n  multiline")]
	[InlineData("A: >\n  multiline")]
	[InlineData("A: plain\n  continuation")]
	[InlineData("A: 'unterminated")]
	[InlineData("A: \"x\"garbage")]
	[InlineData("A: \"x\"#comment")]
	[InlineData("A: \"invalid\\q\"")]
	[InlineData("A: \"\\uD800\"")]
	[InlineData("A: \"\\U00110000\"")]
	[InlineData("---\nA: 1\n---\nB: 2")]
	[InlineData("A: 1\n...\nB: 2")]
	[InlineData("%YAML 1.2\n---\nA: 1")]
	[InlineData("A: 1\0")]
	public void InvalidOrUnsupportedYamlIsRefusedWithoutChangingTheFile(string yaml)
	{
		File.WriteAllText(ConfigPath, yaml);
		byte[] original = File.ReadAllBytes(ConfigPath);
		Assert.Throws<InvalidDataException>(() => ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML));
		Assert.Throws<InvalidDataException>(() => ConfigHandler.SaveConfig(ConfigPath, [], ConfigFormat.YAML));
		Assert.Equal(original, File.ReadAllBytes(ConfigPath));
	}

	[Fact]
	public void ExcessiveDepthIsRefused()
	{
		string yaml = string.Concat(Enumerable.Range(0, 70).Select(index =>
			new string(' ', index * 2) + "Key:\n"));
		Assert.Throws<InvalidDataException>(() => Load(yaml));
	}

	[Fact]
	public void ExternalChangesToAnEditedSettingAreNotOverwritten()
	{
		File.WriteAllText(ConfigPath, "Port: 30000\nName: before\n");
		List<ConfigLine> values = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		values[0].Value = "31000";
		File.WriteAllText(ConfigPath, "Port: 32000\nName: elsewhere\n");
		Assert.Throws<InvalidDataException>(() => ConfigHandler.SaveConfig(ConfigPath, values, ConfigFormat.YAML));
		Assert.Equal("Port: 32000\nName: elsewhere\n", File.ReadAllText(ConfigPath));
	}

	[Fact]
	public void ExternalChangesToOtherSettingsSurvive()
	{
		File.WriteAllText(ConfigPath, "Port: 30000\nName: before\n");
		List<ConfigLine> values = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		values[0].Value = "31000";
		File.WriteAllText(ConfigPath, "Port: 30000\nName: elsewhere\n");
		ConfigHandler.SaveConfig(ConfigPath, values, ConfigFormat.YAML);
		Assert.Equal("Port: 31000\nName: elsewhere\n", File.ReadAllText(ConfigPath));
	}

	[Theory]
	[InlineData("Number: 1", "Number", "not a number")]
	[InlineData("Enabled: true", "Enabled", "maybe")]
	[InlineData("Text: original", "Text", "new\nInjected: value")]
	[InlineData("Text: original", "Text", "new\u2028Injected: value")]
	public void InvalidFieldValuesCannotBeSaved(string yaml, string key, string replacement)
	{
		File.WriteAllText(ConfigPath, yaml);
		List<ConfigLine> values = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		values.Single(value => value.Key == key).Value = replacement;
		Assert.Throws<InvalidDataException>(() => ConfigHandler.SaveConfig(ConfigPath, values, ConfigFormat.YAML));
		Assert.Equal(yaml, File.ReadAllText(ConfigPath));
	}

	[Theory]
	[InlineData("Empty:")]
	[InlineData("Empty: # keep")]
	public void EmptyScalarsCanBeEditedWithSafeSeparators(string yaml)
	{
		File.WriteAllText(ConfigPath, yaml);
		List<ConfigLine> values = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		values[0].Value = "new value";
		ConfigHandler.SaveConfig(ConfigPath, values, ConfigFormat.YAML);
		Assert.Equal("new value", Assert.Single(ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML)).Value);
		if (yaml.Contains('#')) Assert.Contains("# keep", File.ReadAllText(ConfigPath));
	}

	[Fact]
	public void TemplatePlaceholdersWorkQuotedUnquotedAndInsideDescriptionsButNotInsideUserValues()
	{
		const string template = """
			# keep {Password} in documentation
			Server:
			  Name: {ServerName}
			  Password: "{Password}"
			  Description: 'Server: {ServerName}'
			  Port: {Port}
			""";
		Dictionary<string, string> replacements = new()
		{
			["{ServerName}"] = "Player's \"world\" 🚀",
			["{Password}"] = "literal-{Port}-#-password",
			["{Port}"] = "30000"
		};
		string expanded = ConfigHandler.ExpandYamlTemplate(template, replacements);
		Dictionary<string, ConfigLine> values = Load(expanded).ToDictionary(value => value.Key);
		Assert.Equal(replacements["{ServerName}"], values["Name"].Value);
		Assert.Equal(replacements["{Password}"], values["Password"].Value);
		Assert.Equal("Server: " + replacements["{ServerName}"], values["Description"].Value);
		Assert.Equal(ConfigValueType.Number, values["Port"].Type);
		Assert.Contains("# keep {Password} in documentation", expanded);
		Assert.DoesNotContain("synix_yaml_", expanded);
	}

	[Fact]
	public void DefinitionBuilderAcceptsYamlTemplatesAndRetainsTheirFormat()
	{
		File.WriteAllText(ConfigPath, "ServerConfig:\n  Srv_Name: {ServerName}\n  Srv_Port: {Port}\n");
		GameDefinitionDraft draft = new()
		{
			Id = "yaml-builder-test", CatalogOrder = GameDefinitionAuthoring.GetNextCatalogOrder(),
			Game = "YAML Builder Test", AppId = "3", Executable = "server.exe",
			Port = 30000, QueryPort = 30001, ConfigFileCreation = ConfigFileCreationMode.SynixTemplate,
			Format = ConfigFormat.YAML, RelativeConfigPath = "dedicated.yaml", TemplateSourcePath = ConfigPath
		};
		EmbeddedGamePackage package = GameDefinitionAuthoring.ValidateDraft(draft);
		Assert.Equal(ConfigFormat.YAML, package.Definition.Format);
		Assert.Contains(ManagedConfigurationInput.ServerName, package.Configuration!.ManagedInputs);
		Assert.Contains(ManagedConfigurationInput.Port, package.Configuration.ManagedInputs);
		Assert.Equal("dedicated.yaml", Assert.Single(package.Configuration.Templates).TemplateFile);
	}

	[Fact]
	public void ListEditsRetainSiblingItemsAndNestedMappings()
	{
		const string original = "Items:\n  - Name: One\n    Options:\n      Active: true\n  - Name: Two # stay here\n    Options:\n      Active: false\n";
		File.WriteAllText(ConfigPath, original);
		List<ConfigLine> values = ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML);
		values.Single(value => value.Path == "Items[1].Name").Value = "Replaced";
		values.Single(value => value.Path == "Items[0].Options.Active").Value = "false";
		ConfigHandler.SaveConfig(ConfigPath, values, ConfigFormat.YAML);
		Assert.Equal(original.Replace("Active: true", "Active: false").Replace("Name: Two", "Name: \"Replaced\""),
			File.ReadAllText(ConfigPath));
	}

	[Theory]
	[InlineData("0xFF", ConfigValueType.Number)]
	[InlineData("0o70", ConfigValueType.Number)]
	[InlineData("-1.5e+2", ConfigValueType.Number)]
	[InlineData(".inf", ConfigValueType.Number)]
	[InlineData("TrUe", ConfigValueType.Text)]
	[InlineData("\"123\"", ConfigValueType.Text)]
	[InlineData("'False'", ConfigValueType.Text)]
	public void ScalarTypesFollowYamlNotTheAppearanceOfQuotedText(string token, ConfigValueType expected)
	{
		Assert.Equal(expected, Assert.Single(Load("Value: " + token)).Type);
	}

	private static List<ConfigLine> Load(string text) => ConfigHandler.LoadConfigText(text, ConfigFormat.YAML);

	public void Dispose() => Directory.Delete(_root, recursive: true);
}
