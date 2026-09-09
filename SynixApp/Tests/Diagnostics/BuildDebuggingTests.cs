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
using Synix_Control_Panel.SynixEngine;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Synix_Control_Panel.Tests;

public sealed class BuildDebuggingTests
{
	[Fact]
	public void SynixBuild_UsesSourceSymbolsOnlyForDebugConfiguration()
	{
		string assemblyPath = typeof(Core).Assembly.Location;
		using FileStream assembly = File.OpenRead(assemblyPath);
		using PEReader reader = new(assembly);
		bool containsSymbols = reader.ReadDebugDirectory()
			.Any(entry => entry.Type == DebugDirectoryEntryType.CodeView);
#if DEBUG
		Assert.True(containsSymbols, "Debug builds must support source breakpoints and stack traces.");
		Assert.True(File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")));
#else
		Assert.False(containsSymbols, "Release packaging should keep its existing symbol-free output.");
#endif
	}
}
