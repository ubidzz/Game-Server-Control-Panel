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
using Synix_Control_Panel.SynixApp.Database.GameConfigurations;
using Synix_Control_Panel.SynixApp.Database.GameDefinitions;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using System.Text.Json.Nodes;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class DysterraConfigurationTests : IDisposable
{
	private readonly string _testRoot = Path.Combine(
		Path.GetTempPath(), "SynixDysterraConfigurationTests", Guid.NewGuid().ToString("N"));
	private readonly ConfigurationDefinition _definition;
	private readonly GameServer _server;

	public DysterraConfigurationTests()
	{
		Assert.True(GameFix.TryGetConfiguration("Dysterra", out ConfigurationDefinition? definition));
		_definition = Assert.IsType<EmbeddedTemplateConfigurationDefinition>(definition);
		Directory.CreateDirectory(_testRoot);
		_server = new GameServer
		{
			Game = "Dysterra",
			InstallPath = _testRoot,
			ServerName = "Synix Dysterra Test",
			WorldName = "MyServer",
			MaxPlayers = 24,
			Port = 27015,
			QueryPort = 27015
		};
	}

	[Fact]
	public void CapturedTemplateCreatesCompleteWorldSettingsWithoutInstalledSample()
	{
		Assert.Equal(3, _definition.SchemaVersion);
		Assert.Equal(
			ManagedConfigurationInput.ServerName |
			ManagedConfigurationInput.ServerPassword |
			ManagedConfigurationInput.MaxPlayers,
			_definition.SupportedInputs);
		Assert.True(_definition.SupportsFullReset);
		ConfigurationContext context = CreateContext();

		ConfigurationApplyResult result = _definition.Apply(context);

		Assert.True(result.Succeeded, result.Message);
		Assert.True(result.Complete, result.Message);
		Assert.True(result.Created);
		string path = _definition.ResolveFullPath(_server);
		Assert.Equal(Path.Combine(_testRoot, @"Dysterra\WorldSettings\MyServer.json"), path);
		Assert.Equal(path, Assert.Single(_definition.ResolveConfigurationPaths(_server)));
		Assert.Equal(path, Assert.Single(Directory.GetFiles(_testRoot, "*", SearchOption.AllDirectories)));
		JsonObject settings = ReadSettings();
		Assert.Equal(_server.ServerName, settings["WorldName"]!.GetValue<string>());
		Assert.Equal("test-server-password", settings["Password"]!.GetValue<string>());
		Assert.Equal(24, settings["MaxPlayers"]!.GetValue<int>());
		Assert.Equal("Default", settings["GameMode"]!.GetValue<string>());
		Assert.Empty(settings["AdminUsers"]!.AsArray());
		Assert.Empty(settings["ForbiddenLanguages"]!.AsArray());
		Assert.Equal("/Game/Blueprints/SpotEvent/SpotEvents_Official", settings["SpotEvents"]!.GetValue<string>());
		Assert.Equal(23, settings["ValueOverrides"]!.AsObject().Count);
		Assert.Equal(24, settings["ValueOverrides"]!["GameTimeMultiplier"]!.GetValue<int>());
		Assert.Equal(0.8m, settings["ValueOverrides"]!["DemolishRefundMultiply"]!.GetValue<decimal>());
		Assert.Equal(14515200, settings["ValueOverrides"]!["World_Lifetime"]!.GetValue<int>());
		Assert.Equal(3, settings["Items"]!.AsArray().Count);
		Assert.Equal("FirstStart", settings["Items"]![0]!["id"]!.GetValue<string>());
		Assert.Equal(4, settings["Items"]![0]!["InitItemList"]!["ItemList"]!.AsArray().Count);
		Assert.Equal("Respawn", settings["Items"]![1]!["id"]!.GetValue<string>());
		Assert.Equal(2, settings["Items"]![1]!["InitItemList"]!["ItemList"]!.AsArray().Count);
		Assert.Equal(500, settings["Items"]![2]!["MaxStack"]!.GetValue<int>());
		AssertValid(context);

		ConfigurationApplyResult unchanged = _definition.Apply(context);
		Assert.True(unchanged.Complete, unchanged.Message);
		Assert.False(unchanged.Changed);
		Assert.False(unchanged.Created);
	}

	[Fact]
	public void EmbeddedTemplateContainsPlaceholdersInsteadOfPersonalSettings()
	{
		EmbeddedGamePackage package = TrustedGameDefinitionCatalog.Packages
			.Single(package => package.Definition.Game == "Dysterra");
		Assert.NotNull(package.Configuration);
		EmbeddedConfigurationTemplate template = Assert.Single(package.Configuration.Templates);
		Assert.Equal("MyServer.json", template.TemplateFile);
		Assert.Equal(_definition.SchemaVersion, package.Configuration.Revision);
		Assert.Equal(_definition.SchemaVersion, template.Revision);
		Assert.Contains("\"WorldName\": \"{ServerName}\"", template.Content);
		Assert.Contains("\"Password\": \"{Password}\"", template.Content);
		Assert.Contains("\"MaxPlayers\": {MaxPlayers}", template.Content);
		Assert.DoesNotContain("test-server-password", template.Content);
	}

	[Theory]
	[InlineData("")]
	[InlineData("test-\"quoted\"-password\\path-é")]
	public void ManagedTextRemainsValidJsonOnCreateAndUpdate(string password)
	{
		_server.ServerName = "Test \"Dysterra\" \\ Café";
		ConfigurationContext context = CreateContext(password);
		Assert.True(_definition.Apply(context).Complete);
		Assert.Equal(_server.ServerName, ReadSettings()["WorldName"]!.GetValue<string>());
		Assert.Equal(password, ReadSettings()["Password"]!.GetValue<string>());

		_server.ServerName = "Updated \"Dysterra\" \\ Café";
		context = CreateContext(password + "-updated");
		Assert.True(_definition.Apply(context).Complete);
		Assert.Equal(_server.ServerName, ReadSettings()["WorldName"]!.GetValue<string>());
		Assert.Equal(password + "-updated", ReadSettings()["Password"]!.GetValue<string>());
		AssertValid(context);
	}

	[Fact]
	public void UpgradePreservesCustomGameplayAdminsAndItemsAndBacksUpExistingFile()
	{
		Assert.True(_definition.Apply(CreateContext()).Complete);
		JsonObject settings = ReadSettings();
		settings["WorldInfo"] = "Keep this custom description";
		settings["GameMode"] = "CustomMode";
		settings["ValueOverrides"]!["ResourceDropMultiplier"] = 3;
		settings["ValueOverrides"]!["FutureGameSetting"] = 7;
		settings["Items"]![0]!["InitHealth"] = 90;
		settings["Items"]![0]!["InitItemList"]!["ItemList"]!.AsArray().Add(
			JsonNode.Parse("""{"Item":{"RowName":"custom-test-item"},"Count":2}"""));
		settings["AdminUsers"]!.AsArray().Add("test-admin-id");
		settings["ForbiddenLanguages"]!.AsArray().Add("test-language");
		settings["FutureSettings"] = JsonNode.Parse("""{"Enabled":true,"MaxPlayers":7}""");
		string original = settings.ToJsonString();
		string path = _definition.ResolveFullPath(_server);
		File.WriteAllText(path, original);
		_server.ManagedConfigurationVersion = 2;
		_server.ServerName = "Updated Dysterra Server";
		_server.MaxPlayers = 48;
		ConfigurationContext context = CreateContext("updated-test-password");

		ConfigurationApplyResult result = _definition.Apply(context);

		Assert.True(result.Succeeded, result.Message);
		Assert.True(result.Complete, result.Message);
		Assert.True(result.Changed);
		Assert.False(result.Created);
		Assert.Equal(original, File.ReadAllText(path + ".synix.before-template-v3.bak"));
		settings["WorldName"] = _server.ServerName;
		settings["Password"] = "updated-test-password";
		settings["MaxPlayers"] = 48;
		Assert.True(JsonNode.DeepEquals(settings, ReadSettings()));
		AssertValid(context);

		_server.MaxPlayers = 32;
		Assert.True(_definition.Apply(context).Complete);
		Assert.Equal(original, File.ReadAllText(path + ".synix.before-template-v3.bak"));
	}

	[Fact]
	public void FixConfigRestoresCompleteTemplateAndKeepsPreviousFileRecoverable()
	{
		ConfigurationContext context = CreateContext();
		Assert.True(_definition.Apply(context).Complete);
		string path = _definition.ResolveFullPath(_server);
		const string broken = "{ broken configuration";
		File.WriteAllText(path, broken);
		Assert.True(GameFix.NeedsManagedConfigurationRepair(_server));
		_server.MaxPlayers = 16;

		ConfigurationApplyResult result = _definition.ResetToTemplate(context);

		Assert.True(result.Succeeded, result.Message);
		Assert.True(result.Complete, result.Message);
		Assert.Equal(16, ReadSettings()["MaxPlayers"]!.GetValue<int>());
		Assert.Equal(3, ReadSettings()["Items"]!.AsArray().Count);
		Assert.Contains(
			Directory.GetFiles(_testRoot, "*.bak", SearchOption.AllDirectories),
			backup => File.ReadAllText(backup) == broken);
		AssertValid(context);
	}

	[Fact]
	public void OrdinaryUpdateDoesNotResetPartialExistingWorldSettings()
	{
		string path = _definition.ResolveFullPath(_server);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		const string original = """{"WorldName":"Existing","MaxPlayers":12,"CustomField":true}""";
		File.WriteAllText(path, original);
		ConfigurationContext context = CreateContext();

		ConfigurationApplyResult result = _definition.Apply(context);

		Assert.True(result.Succeeded, result.Message);
		Assert.False(result.Complete);
		JsonObject settings = ReadSettings();
		Assert.True(settings["CustomField"]!.GetValue<bool>());
		Assert.False(settings.ContainsKey("Password"));
		Assert.False(settings.ContainsKey("Items"));
		Assert.Equal(original, File.ReadAllText(path + ".synix.before-template-v3.bak"));
		Assert.True(_definition.NeedsStructuralRepair(context));
	}

	private ConfigurationContext CreateContext(string password = "test-server-password") => new(
		_server,
		new SynixServerPasswords(password, "test-admin-password", "test-rcon-password"),
		"synix-dysterra-test",
		string.Empty,
		string.Empty);

	private JsonObject ReadSettings() =>
		JsonNode.Parse(File.ReadAllText(_definition.ResolveFullPath(_server)))!.AsObject();

	private void AssertValid(ConfigurationContext context)
	{
		Assert.False(_definition.NeedsStructuralRepair(context));
		Assert.DoesNotContain(_definition.Validate(context), item => item.State == ConfigurationValidationState.Failed);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testRoot))
			Directory.Delete(_testRoot, recursive: true);
	}
}
