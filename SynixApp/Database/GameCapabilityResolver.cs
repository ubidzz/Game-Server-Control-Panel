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
using Synix_Control_Panel.SynixApp.ServerHandler;

namespace Synix_Control_Panel.SynixApp.Database
{
	public static class GameCapabilityResolver
	{
		private static readonly GameControlCapabilities DefaultCapabilities = new();

		public static GameControlCapabilities Resolve(GameDefinition? game)
		{
			return game?.ControlCapabilities ?? DefaultCapabilities;
		}

		public static GameControlCapabilities Resolve(GameServer? server)
		{
			return server == null
				? DefaultCapabilities
				: Resolve(GameDatabase.GetGame(server.Game));
		}

		public static bool UsesMinecraftLifecycle(GameDefinition? game) =>
			Resolve(game).Lifecycle == GameLifecycleControllerKind.Minecraft;

		public static bool UsesMinecraftLifecycle(GameServer? server) =>
			Resolve(server).Lifecycle == GameLifecycleControllerKind.Minecraft;

		public static bool UsesMinecraftConsole(GameServer? server) =>
			Resolve(server).Console == GameConsoleControllerKind.Minecraft;

		public static bool UsesMinecraftConfiguration(GameServer? server) =>
			Resolve(server).Configuration == GameConfigurationControllerKind.Minecraft;

		public static bool UsesMinecraftConfiguration(GameDefinition? game) =>
			Resolve(game).Configuration == GameConfigurationControllerKind.Minecraft;

		public static bool UsesMinecraftPlayers(GameServer? server) =>
			Resolve(server).Players == GamePlayerControllerKind.Minecraft;
	}
}
