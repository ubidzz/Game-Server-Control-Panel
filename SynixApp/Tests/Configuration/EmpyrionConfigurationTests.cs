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
using Synix_Control_Panel.SynixApp.Database.GameDefinitions;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class EmpyrionConfigurationTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "SynixEmpyrionTests", Guid.NewGuid().ToString("N"));
	private readonly ConfigurationDefinition _definition;
	private readonly GameServer _server;
	private string ConfigPath => Path.Combine(_root, "dedicated.yaml");

	public EmpyrionConfigurationTests()
	{
		Directory.CreateDirectory(_root);
		Assert.True(GameFix.TryGetConfiguration("Empyrion - Galactic Survival", out ConfigurationDefinition? definition));
		_definition = Assert.IsType<EmbeddedTemplateConfigurationDefinition>(definition);
		_server = new GameServer
		{
			Game = "Empyrion - Galactic Survival", InstallPath = _root, ServerName = "Synix Empyrion",
			Port = 30100, QueryPort = 30101, MaxPlayers = 12,
			WorldName = "Default Multiplayer", WorldSeed = "1011345"
		};
	}

	[Fact]
	public void CompleteCapturedTemplateCreatesUsableSettingsWithSafeDefaults()
	{
		GameInfo game = GameDatabase.GetGame(_server.Game)!;
		Assert.Equal(ConfigFormat.YAML, game.Format);
		Assert.Equal(ConfigFileCreationMode.SynixTemplate, game.ConfigFileCreation);
		Assert.Equal("dedicated.yaml", game.RelativeConfigPath);
		Assert.Equal("530870", game.AppID);
		Assert.Equal("EmpyrionLauncher.exe", game.ExeName);
		Assert.Equal(1, _definition.SchemaVersion);
		ConfigurationContext context = Context();
		ConfigurationApplyResult result = _definition.Apply(context);
		Assert.True(result.Complete, result.Message);
		Assert.True(result.Created);
		Assert.Equal(ConfigPath, Assert.Single(_definition.ResolveConfigurationPaths(_server)));
		Dictionary<string, ConfigLine> values = Read();
		Assert.Equal(14, values.Count);
		Assert.Equal("Synix Empyrion", values["ServerConfig.Srv_Name"].Value);
		Assert.Equal("30100", values["ServerConfig.Srv_Port"].Value);
		Assert.Equal("12", values["ServerConfig.Srv_MaxPlayers"].Value);
		Assert.Equal("test-password", values["ServerConfig.Srv_Password"].Value);
		Assert.Equal(ConfigValueType.Secret, values["ServerConfig.Srv_Password"].Type);
		Assert.Equal("true", values["ServerConfig.Srv_Public"].Value);
		Assert.Equal("true", values["ServerConfig.EACActive"].Value);
		Assert.Equal("90", values["ServerConfig.TimeoutBootingPfServer"].Value);
		Assert.Equal("DediGame", values["GameConfig.GameName"].Value);
		Assert.Equal("Survival", values["GameConfig.Mode"].Value);
		Assert.Equal(_server.WorldName, values["GameConfig.CustomScenario"].Value);
		Assert.Equal(_server.WorldSeed, values["GameConfig.Seed"].Value);
		Assert.Equal(ConfigValueType.Number, values["GameConfig.Seed"].Type);
		Assert.DoesNotContain(values.Keys, key => key.StartsWith("ServerConfig.Tel_", StringComparison.Ordinal));
		Assert.Contains("### Dedicated server settings", File.ReadAllText(ConfigPath));
		Assert.DoesNotContain("{Identity}", TrustedGameDefinitionCatalog.Packages
			.Single(package => package.Definition.Game == _server.Game).Configuration!.Templates[0].Content);
		AssertValid(context);
		Assert.False(_definition.Apply(context).Changed);
	}

	[Fact]
	public void DirectLauncherUsesTheShippedHeadlessServerCommand()
	{
		GameInfo game = GameDatabase.GetGame(_server.Game)!;
		Assert.Equal("-startDedi", game.RequiredArgs);
		Assert.True(GameLaunchCommandBuilder.TryGetLauncherKind(game.ExeName, out GameLauncherKind kind));
		Assert.Equal(GameLauncherKind.NativeExecutable, kind);
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(
			_server, game, game.AppID, new SynixServerPasswords(string.Empty, string.Empty, string.Empty),
			out string arguments, out string error), error);
		Assert.Equal("-startDedi", arguments);

		string executablePath = GameLaunchCommandBuilder.ResolveExecutablePath(_server, game);
		var startInfo = GameLaunchCommandBuilder.CreateProcessStartInfo(
			executablePath, arguments, _root, runElevated: false,
			createNoWindow: true, redirectStandardInput: false);
		Assert.Equal(Path.Combine(_root, "EmpyrionLauncher.exe"), startInfo.FileName);
		Assert.Equal(_root, startInfo.WorkingDirectory);
		Assert.Equal("-startDedi", startInfo.Arguments);
		Assert.False(startInfo.UseShellExecute);
	}

	[Fact]
	public void ExistingConfigAddsOptionalManagedKeysWithoutResettingSaveGameplayOrComments()
	{
		Assert.True(_definition.Apply(Context()).Complete);
		string original = File.ReadAllText(ConfigPath)
			.Replace("Srv_Password: \"test-password\"", "# Srv_Password: \"\"")
			.Replace("Srv_MaxPlayers: 12", "# Srv_MaxPlayers: 8")
			.Replace("GameName: DediGame", "GameName: ExistingSave")
			.Replace("Mode: Survival", "Mode: Creative")
			.Replace("EACActive: true", "EACActive: false")
			.Replace("MaxAllowedSizeClass: 10", "MaxAllowedSizeClass: 20") +
			"\nCustomSettings:\n  Message: 'Keep # my settings'\n  Enabled: true\n";
		File.WriteAllText(ConfigPath, original);
		_server.ServerName = "Renamed \"Server\" # one";
		_server.MaxPlayers = 24;
		_server.Port = 30200;
		_server.WorldSeed = "54321";
		ConfigurationContext context = Context("new-{Port}-test-password");

		ConfigurationApplyResult result = _definition.Apply(context);

		Assert.True(result.Complete, result.Message);
		Assert.True(result.Changed);
		Assert.False(result.Created);
		Assert.Equal(original, File.ReadAllText(ConfigPath + ".synix.before-template-v1.bak"));
		Dictionary<string, ConfigLine> values = Read();
		Assert.Equal(_server.ServerName, values["ServerConfig.Srv_Name"].Value);
		Assert.Equal("24", values["ServerConfig.Srv_MaxPlayers"].Value);
		Assert.Equal("30200", values["ServerConfig.Srv_Port"].Value);
		Assert.Equal("new-{Port}-test-password", values["ServerConfig.Srv_Password"].Value);
		Assert.Equal("54321", values["GameConfig.Seed"].Value);
		Assert.Equal("ExistingSave", values["GameConfig.GameName"].Value);
		Assert.Equal("Creative", values["GameConfig.Mode"].Value);
		Assert.Equal("false", values["ServerConfig.EACActive"].Value);
		Assert.Equal("20", values["ServerConfig.MaxAllowedSizeClass"].Value);
		Assert.Equal("Keep # my settings", values["CustomSettings.Message"].Value);
		Assert.Contains("# Srv_MaxPlayers: 8", File.ReadAllText(ConfigPath));
		Assert.Contains("# Tel_Enabled: true", File.ReadAllText(ConfigPath));
		AssertValid(context);
		Assert.False(_definition.Apply(context).Changed);
		Assert.Equal(original, File.ReadAllText(ConfigPath + ".synix.before-template-v1.bak"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("12345")]
	[InlineData("literal-{Port}-{WorldSeed}-{GameMode}")]
	[InlineData(" \u263A 🚀 \" quote ' slash \\ # colon: space ")]
	public void PasswordRoundTripsThroughCreationUpdateAndValidation(string password)
	{
		ConfigurationContext context = Context(password);
		Assert.True(_definition.Apply(context).Complete);
		Assert.Equal(password, Read()["ServerConfig.Srv_Password"].Value);
		_server.ServerName += " {Password} 🚀";
		Assert.True(_definition.Apply(context).Complete);
		Assert.Equal(password, Read()["ServerConfig.Srv_Password"].Value);
		Assert.Equal(_server.ServerName, Read()["ServerConfig.Srv_Name"].Value);
		AssertValid(context);
	}

	[Fact]
	public void UnsupportedOrBrokenExistingFileIsNeverReplacedByAnOrdinaryUpdate()
	{
		const string original = "ServerConfig:\n  Srv_Name: &name Custom\nGameConfig:\n  GameName: ExistingSave\n";
		File.WriteAllText(ConfigPath, original);
		ConfigurationApplyResult result = _definition.Apply(Context());
		Assert.False(result.Succeeded);
		Assert.Equal(original, File.ReadAllText(ConfigPath));
		Assert.True(GameFix.NeedsManagedConfigurationRepair(_server));
	}

	[Fact]
	public void MissingParentSectionOrWrongTypeIsNotGuessed()
	{
		const string original = "ServerConfig:\n  Srv_Port: \"not a number\"\n  Custom: Keep\n";
		File.WriteAllText(ConfigPath, original);
		ConfigurationApplyResult result = _definition.Apply(Context());
		Assert.True(result.Succeeded, result.Message);
		Assert.False(result.Complete);
		Assert.Equal("not a number", Read()["ServerConfig.Srv_Port"].Value);
		Assert.Equal("Keep", Read()["ServerConfig.Custom"].Value);
		Assert.DoesNotContain(Read().Keys, key => key.StartsWith("GameConfig.", StringComparison.Ordinal));
		Assert.Equal(original, File.ReadAllText(ConfigPath + ".synix.before-template-v1.bak"));
	}

	[Fact]
	public void ExplicitResetKeepsThePreviousFileRecoverable()
	{
		const string broken = "broken: [unfinished";
		File.WriteAllText(ConfigPath, broken);
		ConfigurationApplyResult result = _definition.ResetToTemplate(Context());
		Assert.True(result.Complete, result.Message);
		Assert.Contains(Directory.GetFiles(_root, "*.bak"), path => File.ReadAllText(path) == broken);
		AssertValid(Context());
	}

	[Theory]
	[InlineData("")]
	[InlineData("not-a-seed")]
	[InlineData("1\nInjected: true")]
	[InlineData("545322654334")]
	public void InvalidSeedFailsBeforeCreatingAFile(string seed)
	{
		_server.WorldSeed = seed;
		Assert.False(_definition.Apply(Context()).Succeeded);
		Assert.False(File.Exists(ConfigPath));
	}

	[Fact]
	public void InvalidSavedSeedDoesNotChangeFilesAndResetWorksAfterUserCorrectsIt()
	{
		Assert.True(_definition.Apply(Context()).Complete);
		string original = File.ReadAllText(ConfigPath);
		string[] originalFiles = Directory.GetFiles(_root);
		_server.WorldSeed = "545322654334";

		ConfigurationApplyResult failed = _definition.ResetToTemplate(Context());
		Assert.False(failed.Succeeded);
		Assert.False(failed.Changed);
		Assert.Contains("World Generation", failed.Message);
		Assert.Contains("2147483647", failed.Message);
		Assert.Equal(original, File.ReadAllText(ConfigPath));
		Assert.Equal(originalFiles, Directory.GetFiles(_root));
		Assert.Equal("545322654334", _server.WorldSeed);

		_server.WorldSeed = "1011345";
		ConfigurationApplyResult repaired = _definition.ResetToTemplate(Context());
		Assert.True(repaired.Complete, repaired.Message);
		Assert.Equal("1011345", Read()["GameConfig.Seed"].Value);
		AssertValid(Context());
	}

	[Theory]
	[InlineData("1")]
	[InlineData("2147483647")]
	public void NumericSeedBoundariesSurviveCreationValidationAndReset(string seed)
	{
		_server.WorldSeed = seed;
		Assert.True(_definition.Apply(Context()).Complete);
		Assert.Equal(seed, Read()["GameConfig.Seed"].Value);
		AssertValid(Context());
		Assert.True(_definition.ResetToTemplate(Context()).Complete);
		Assert.Equal(seed, Read()["GameConfig.Seed"].Value);
		AssertValid(Context());
	}

	[Fact]
	public void CaptureRecognizesYamlAndRedactsPasswordAndTelnetPasswordWithoutTouchingSource()
	{
		File.WriteAllText(ConfigPath, "ServerConfig:\n  Srv_Password: \"test-server-secret\"\n  Tel_Pwd: test-telnet-secret\n");
		string original = File.ReadAllText(ConfigPath);
		string destination = Path.Combine(_root, "capture");
		GeneratedConfigurationCaptureResult result = GeneratedConfigurationCollector.CollectServer(
			_server, destination, includeAllGeneratedFiles: true);
		Assert.Equal(1, result.CopiedFiles);
		Assert.Empty(result.Errors);
		string captured = File.ReadAllText(Assert.Single(Directory.GetFiles(destination, "*.yaml", SearchOption.AllDirectories)));
		Assert.DoesNotContain("test-server-secret", captured);
		Assert.DoesNotContain("test-telnet-secret", captured);
		Assert.Equal(original, File.ReadAllText(ConfigPath));
	}

	private Dictionary<string, ConfigLine> Read() =>
		ConfigHandler.LoadConfig(ConfigPath, ConfigFormat.YAML).ToDictionary(value => value.Path);

	private ConfigurationContext Context(string password = "test-password") => new(
		_server, new SynixServerPasswords(password, "unused-admin", "unused-rcon"),
		"synix-server-identity", string.Empty, string.Empty);

	private void AssertValid(ConfigurationContext context)
	{
		Assert.False(_definition.NeedsStructuralRepair(context));
		Assert.DoesNotContain(_definition.Validate(context), item => item.State == ConfigurationValidationState.Failed);
	}

	public void Dispose() => Directory.Delete(_root, recursive: true);
}
