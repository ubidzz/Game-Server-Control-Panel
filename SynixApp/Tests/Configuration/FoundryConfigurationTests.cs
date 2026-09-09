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
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class FoundryConfigurationTests
{
	[Fact]
	public void CapturedAppCfgIsEmbeddedAndUpdatesOnlyManagedSettings()
	{
		string root = Path.Combine(Path.GetTempPath(), "SynixFoundryTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		try
		{
			Assert.True(GameFix.TryGetConfiguration("Foundry", out ConfigurationDefinition? definition));
			Assert.IsType<EmbeddedTemplateConfigurationDefinition>(definition);
			Assert.Equal(3, definition!.SchemaVersion);
			GameServer server = new()
			{
				Game = "Foundry", InstallPath = root, ServerName = "Synix Foundry",
				Port = 3724, QueryPort = 27015, MaxPlayers = 10, WorldSeed = "12345"
			};
			ConfigurationContext context = new(server,
				new SynixServerPasswords("test-{Port}-password", "unused-admin", "unused-rcon"),
				"foundry-test", string.Empty, string.Empty);
			ConfigurationApplyResult result = definition.Apply(context);
			Assert.True(result.Complete, result.Message);
			string path = Path.Combine(root, "app.cfg");
			Dictionary<string, ConfigLine> initial = ConfigHandler.LoadConfig(path, definition.Format)
				.ToDictionary(value => value.Key);
			Assert.Equal(11, initial.Count);
			Assert.Equal("Synix Foundry", initial["server_name"].Value);
			Assert.Equal("test-{Port}-password", initial["server_password"].Value);
			Assert.Equal("foundry-test", initial["server_world_name"].Value);
			Assert.Equal("3724", initial["server_port"].Value);
			Assert.Equal("27015", initial["server_query_port"].Value);
			Assert.Equal("12345", initial["mapseed"].Value);
			Assert.Equal("300", initial["autosave_interval"].Value);
			Assert.Equal(string.Empty, initial["server_persistent_data_override_folder"].Value);
			string original = File.ReadAllText(path)
				.Replace("autosave_interval=300", "autosave_interval=600")
				.Replace("pause_server_when_empty=true", "pause_server_when_empty=false")
				.Replace("server_is_public=true", "server_is_public=false") + "\ncustom_setting=keep\n";
			File.WriteAllText(path, original);
			server.ManagedConfigurationVersion = 2;
			server.MaxPlayers = 20;
			result = definition.Apply(context);
			Assert.True(result.Complete, result.Message);
			Assert.Equal(original.Replace("server_max_players=10", "server_max_players=20"), File.ReadAllText(path));
			Assert.Equal(original, File.ReadAllText(path + ".synix.before-template-v3.bak"));
			Assert.DoesNotContain(definition.Validate(context), item => item.State == ConfigurationValidationState.Failed);
			Assert.False(definition.Apply(context).Changed);
			EmbeddedGamePackage package = TrustedGameDefinitionCatalog.Packages
				.Single(package => package.Definition.Game == "Foundry");
			Assert.Equal("app.cfg", Assert.Single(package.Configuration!.Templates).TemplateFile);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}
}
