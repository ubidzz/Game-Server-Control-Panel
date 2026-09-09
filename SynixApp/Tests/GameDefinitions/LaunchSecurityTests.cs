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
using System.Diagnostics;
using Synix_Control_Panel.SynixApp.Database;
using Synix_Control_Panel.SynixApp.ServerHandler;
using Synix_Control_Panel.SynixEngine;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class LaunchSecurityTests
{
	[Theory]
	[InlineData("name", "NameWith\"Quote")]
	[InlineData("name", "NameWith\r\nLineBreak")]
	[InlineData("password", "Private%EXAMPLE_VARIABLE%")]
	[InlineData("password", "Private!EXAMPLE_VARIABLE!")]
	[InlineData("password", "Private\"Quote")]
	[InlineData("map", "MapWith\"Quote")]
	public void BatchTemplateRejectsUnsafeNormalFieldsWithoutExposingTheirValues(string field, string value)
	{
		var definition = GameDatabase.GetGame("Smalland: Survive the Wilds")!;
		GameServer server = Server(definition.Game);
		if (field == "name") server.ServerName = value;
		if (field == "map") server.WorldName = value;
		SynixServerPasswords passwords = new(field == "password" ? value : "normal-password", "", "");
		Assert.False(GameLaunchCommandBuilder.TryBuildArguments(server, definition, definition.AppID, passwords,
			out string arguments, out string error));
		Assert.Empty(arguments);
		Assert.DoesNotContain(value, error);
		Assert.Contains("batch", error, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void LiteralTemplateLikeTextIsNeverExpandedAsAnotherField()
	{
		var definition = GameDatabase.GetGame("Smalland: Survive the Wilds")!;
		GameServer server = Server(definition.Game);
		server.ServerName = "Literal {InstallPath}";
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(server, definition, definition.AppID,
			new("Literal {ServerName}", "", ""), out string arguments, out string error), error);
		Assert.Contains("-ServerName=\"Literal {InstallPath}\"", arguments);
		Assert.Contains("-Password=\"Literal {ServerName}\"", arguments);
	}

	[Theory]
	[InlineData("-value unquoted(value)")]
	[InlineData("-value %EXAMPLE_VARIABLE%")]
	[InlineData("-value \"%EXAMPLE VARIABLE%\"")]
	[InlineData("-value \"!EXAMPLE VARIABLE!\"")]
	[InlineData("-first \"50% complete\" -second \"50% complete\"")]
	[InlineData("-value %~dp0")]
	[InlineData("-value %%A")]
	[InlineData("-value \"unclosed")]
	[InlineData("-value safe & another-command")]
	public void FinalBatchBoundaryAlsoChecksCallersThatDoNotUseTheTemplateBuilder(string arguments)
	{
		Assert.Throws<ArgumentException>(() => GameLaunchCommandBuilder.BuildCommandProcessorArguments(
			@"C:\Synix\Games\Test\start.cmd", arguments));
	}

	[Fact]
	public void BatchValuesAllowSinglePercentageAndExclamationCharacters()
	{
		Assert.Contains("-progress \"50% complete!\"", GameLaunchCommandBuilder.BuildCommandProcessorArguments(
			@"C:\Synix\Games\Test\start.cmd", "-progress \"50% complete!\""));
	}

	[Fact]
	public void NativeLaunchesAndUserSuppliedExtraArgumentsAreNotRewritten()
	{
		var definition = GameDatabase.GetGame("Rust")!;
		GameServer server = Server(definition.Game);
		server.ExtraArgs = "-custom-field \"two  words\" /Config=server.json";
		SynixServerPasswords passwords = new("native%EXAMPLE%with!literal!", "", "");
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(server, definition, definition.AppID, passwords,
			out string arguments, out string error), error);
		Assert.Contains(server.ExtraArgs, arguments);
		var start = GameLaunchCommandBuilder.CreateProcessStartInfo(
			@"C:\Synix\Games\Test\server.exe", arguments, server.InstallPath, false, true, false);
		Assert.Equal(arguments, start.Arguments);
		Assert.Equal(@"C:\Synix\Games\Test\server.exe", start.FileName);
	}

	[Fact]
	public void RoutineLogPreviewOmitsUnknownCustomSecretsButKeepsLaunchInputUnchanged()
	{
		var definition = GameDatabase.GetGame("Rust")!;
		GameServer server = Server(definition.Game);
		const string custom = "-undocumented-account \"PrivateUnknownValue\"";
		server.ExtraArgs = custom;
		SynixServerPasswords passwords = new("PrivateManagedValue", "PrivateAdminValue", "PrivateRconValue");
		Assert.True(GameLaunchCommandBuilder.TryBuildArguments(server, definition, definition.AppID, passwords,
			out string launch, out string error), error);
		Assert.Contains(custom, launch);
		Assert.True(GameLaunchCommandBuilder.TryBuildLogArguments(server, definition, definition.AppID, passwords, "",
			out string preview, out error), error);
		Assert.DoesNotContain("PrivateUnknownValue", preview);
		Assert.DoesNotContain("PrivateManagedValue", preview);
		Assert.DoesNotContain("PrivateRconValue", preview);
		Assert.Contains("contents omitted", preview);
		Assert.Equal(custom, server.ExtraArgs);
	}

	[Fact]
	public void EveryBuiltInTemplateStillBuildsExportArgumentsAndPrivateLogPreviews()
	{
		foreach (var definition in GameDatabase.GetGameList())
		{
			GameServer server = Server(definition.Game);
			server.ExtraArgs = "-custom-option \"PrivateUnknownValue\"";
			SynixServerPasswords passwords = new("NormalPassword", "AdminPassword", "RconPassword", "ApplicationToken");
			Assert.True(GameLaunchCommandBuilder.TryBuildArguments(server, definition, definition.AppID, passwords,
				out string export, out string error, forBatchFile: true), definition.Game + ": " + error);
			Assert.Contains(server.ExtraArgs, export);
			Assert.NotNull(Core.EscapeWindowsBatchCommandLine(export));
			Assert.True(GameLaunchCommandBuilder.TryBuildLogArguments(server, definition, definition.AppID, passwords, "",
				out string preview, out error), definition.Game + ": " + error);
			Assert.DoesNotContain("PrivateUnknownValue", preview);
		}
	}

	[Fact]
	public void BatchLauncherPreservesQuotedPunctuationAndRepeatedSpaces()
	{
		WithFolder(root =>
		{
			var definition = GameDatabase.GetGame("Smalland: Survive the Wilds")!;
			GameServer server = Server(definition.Game);
			server.ServerName = "Research & Build (Team)";
			Assert.True(GameLaunchCommandBuilder.TryBuildArguments(server, definition, definition.AppID,
				new("two  words^!", "", ""), out string arguments, out string error), error);
			string script = Path.Combine(root, "capture.cmd");
			File.WriteAllText(script, "@echo off\r\n> \"%~dp0captured.txt\" echo %*\r\nexit /b 0\r\n");
			RunCapture(script, arguments, root);
			string captured = File.ReadAllText(Path.Combine(root, "captured.txt")).Trim();
			Assert.Equal(arguments, captured);
			Assert.Contains("-Password=\"two  words^!\"", captured);
		});
	}

	[Theory]
	[InlineData(@"C:\Games & Files\50% Complete (Test)^!")]
	[InlineData("password%VALUE% with spaces & punctuation!")]
	public void ExportedQuotedValuesRoundTripWithoutAddingLiteralCarets(string value)
	{
		WithFolder(root =>
		{
			string script = Path.Combine(root, "capture.cmd");
			File.WriteAllText(script, "@echo off\r\nsetlocal DisableDelayedExpansion\r\n> \"%~dp0captured.txt\" echo \"" +
				Core.EscapeWindowsBatchQuotedValue(value) + "\"\r\nexit /b 0\r\n");
			RunCapture(script, "", root);
			Assert.Equal("\"" + value + "\"", File.ReadAllText(Path.Combine(root, "captured.txt")).Trim());
		});
	}

	private static void RunCapture(string script, string arguments, string root)
	{
		using Process process = Process.Start(GameLaunchCommandBuilder.CreateProcessStartInfo(script, arguments, root, false, true, false))!;
		if (!process.WaitForExit(10_000)) { process.Kill(); throw new TimeoutException("The harmless argument-capture fixture timed out."); }
		Assert.Equal(0, process.ExitCode);
	}

	private static void WithFolder(Action<string> action)
	{
		string root = Path.Combine(Path.GetTempPath(), "SynixLaunchSecurityTests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		try { action(root); }
		finally
		{
			string resolved = Path.GetFullPath(root);
			Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())), Path.GetDirectoryName(resolved));
			Assert.StartsWith("SynixLaunchSecurityTests-", Path.GetFileName(resolved));
			if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
		}
	}

	private static GameServer Server(string game) => new()
	{
		Game = game, ServerName = "Compatibility Test", InstallPath = @"C:\Synix\Games\Compatibility Test",
		WorldName = "TestWorld", WorldSeed = "12345", WorldSize = 4000, Port = 28015,
		QueryPort = 28016, AppPort = 28017, MaxPlayers = 24, MaxRam = 4, EnableRcon = true,
		RconPort = 28018, GameMode = "PVE", ExtraArgs = ""
	};
}
