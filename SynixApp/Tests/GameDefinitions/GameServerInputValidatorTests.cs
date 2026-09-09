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
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class GameServerInputValidatorTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("545322654334")]
	[InlineData("2147483648")]
	[InlineData("-2147483649")]
	[InlineData("-2147483648")]
	[InlineData("-1")]
	[InlineData("0")]
	[InlineData("1.5")]
	[InlineData("1e3")]
	[InlineData("world-name")]
	[InlineData("123\n")]
	public void EmpyrionRejectsInvalidSeedsBeforeConfigurationIsWritten(string? seed)
	{
		GameInfo definition = GameDatabase.GetGame("Empyrion - Galactic Survival")!;
		Assert.True(GameServerInputValidator.RequiresNumericWorldSeed(definition));
		Assert.False(GameServerInputValidator.TryValidateWorldSeed(definition, seed, out string error));
		Assert.Contains("World Seed", error);
		Assert.Contains("2147483647", error);
		Assert.Contains("Server Setup > World Generation", error);
	}

	[Theory]
	[InlineData("1011345")]
	[InlineData("1")]
	[InlineData("2147483647")]
	[InlineData(" +12345 ")]
	public void NumericSeedValidationAcceptsSupportedWholeNumbers(string seed)
	{
		GameInfo definition = GameDatabase.GetGame("Empyrion - Galactic Survival")!;
		Assert.True(GameServerInputValidator.TryValidateWorldSeed(definition, seed, out string error), error);
		Assert.Empty(error);
	}

	[Theory]
	[InlineData("Minecraft", "")]
	[InlineData("Minecraft", "a text seed")]
	[InlineData("7 Days to Die", "Navezgane")]
	[InlineData("Foundry", "545322654334")]
	[InlineData("Satisfactory", "")]
	public void OtherGamesKeepTheirExistingSeedRules(string game, string seed)
	{
		GameInfo definition = GameDatabase.GetGame(game)!;
		Assert.False(GameServerInputValidator.RequiresNumericWorldSeed(definition));
		Assert.True(GameServerInputValidator.TryValidateWorldSeed(definition, seed, out string error), error);
	}

	[Fact]
	public void ValheimRejectsPasswordsShorterThanFiveCharacters()
	{
		GameInfo definition = GameDatabase.GetGame("Valheim")!;

		bool valid = GameServerInputValidator.TryValidate(
			definition,
			"Dedicated Server",
			new SynixServerPasswords("123", string.Empty, string.Empty),
			out string error);

		Assert.False(valid);
		Assert.Contains("at least 5 characters", error);
	}

	[Fact]
	public void ValheimRejectsPasswordContainedInServerName()
	{
		GameInfo definition = GameDatabase.GetGame("Valheim")!;

		bool valid = GameServerInputValidator.TryValidate(
			definition,
			"My VikingSecret Realm",
			new SynixServerPasswords("secret", string.Empty, string.Empty),
			out string error);

		Assert.False(valid);
		Assert.Contains("appear in the server name", error);
	}

	[Fact]
	public void ValheimAcceptsAValidIndependentPassword()
	{
		GameInfo definition = GameDatabase.GetGame("Valheim")!;

		bool valid = GameServerInputValidator.TryValidate(
			definition,
			"Dedicated Server",
			new SynixServerPasswords("VikingSecret", string.Empty, string.Empty),
			out string error);

		Assert.True(valid, error);
		Assert.Empty(error);
	}

	[Fact]
	public void GamesWithoutDeclaredRulesKeepExistingPasswordBehavior()
	{
		GameInfo definition = GameDatabase.GetGame("Rust")!;

		bool valid = GameServerInputValidator.TryValidate(
			definition,
			"123",
			new SynixServerPasswords("123", string.Empty, string.Empty),
			out string error);

		Assert.True(valid, error);
		Assert.Empty(error);
	}

	[Fact]
	public void EcoRequiresAUserTokenForOnlineAuthentication()
	{
		GameInfo definition = GameDatabase.GetGame("Eco")!;

		bool valid = GameServerInputValidator.TryValidate(
			definition,
			"Eco Server",
			new SynixServerPasswords(string.Empty, string.Empty, string.Empty),
			out string error);

		Assert.False(valid);
		Assert.Contains("Eco User Token", error);
	}

	[Fact]
	public void EcoAcceptsASafeUserToken()
	{
		GameInfo definition = GameDatabase.GetGame("Eco")!;

		bool valid = GameServerInputValidator.TryValidate(
			definition,
			"Eco Server",
			new SynixServerPasswords(
				string.Empty,
				string.Empty,
				string.Empty,
				"eco-user-token_123.test"),
			out string error);

		Assert.True(valid, error);
		Assert.Empty(error);
	}

	[Fact]
	public void EcoRejectsTokensThatCouldAlterTheLaunchCommand()
	{
		GameInfo definition = GameDatabase.GetGame("Eco")!;

		bool valid = GameServerInputValidator.TryValidate(
			definition,
			"Eco Server",
			new SynixServerPasswords(
				string.Empty,
				string.Empty,
				string.Empty,
				"token\" -offline"),
			out string error);

		Assert.False(valid);
		Assert.Contains("cannot be passed safely", error);
	}
}
