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
using System.Runtime.InteropServices;

namespace Synix_Control_Panel.SynixApp.ServerHandler.Satisfactory;

/// <summary>Verifies the loopback API belongs to this server before trusting its certificate.</summary>
internal static class SatisfactoryLocalApiIdentity
{
	internal static void Verify(GameServer server)
	{
		_ = SatisfactoryApiClient.CreateEndpoint(server.Port);
		int[] owners = ListenerOwners(server.Port);
		ServerProcessIdentity[] identities = Servers.GetServerProcessSnapshot(server);
		if (owners.Length == 0 || owners.Any(pid => !identities.Any(identity => identity.ProcessId == pid &&
			SatisfactoryConsoleTokenReader.MatchesLiveProcess(server.InstallPath, identity))))
			throw new SatisfactoryApiException(SatisfactoryApiError.LocalIdentity);
	}

	internal static int[] ListenerOwners(int port)
	{
		const uint insufficientBuffer = 122;
		int length = 0;
		uint result = GetExtendedTcpTable(IntPtr.Zero, ref length, false, 2, 3, 0); // IPv4, OWNER_PID_LISTENER
		for (int attempt = 0; attempt < 3 && result == insufficientBuffer; attempt++)
		{
			if (length < 4 || length > 1024 * 1024) break;
			int allocated = length;
			IntPtr table = Marshal.AllocHGlobal(allocated);
			try
			{
				result = GetExtendedTcpTable(table, ref length, false, 2, 3, 0);
				if (result == insufficientBuffer) continue;
				if (result != 0) break;
				int rows = Marshal.ReadInt32(table);
				if (rows < 0 || rows > (allocated - 4) / 24) break;
				List<int> owners = [];
				for (int row = 0; row < rows; row++)
				{
					int offset = 4 + row * 24;
					// MIB_TCPROW_OWNER_PID: state, local address, local port,
					// remote address, remote port, owning PID. Ports are network-order.
					uint address = unchecked((uint)Marshal.ReadInt32(table, offset + 4));
					int localPort = (Marshal.ReadByte(table, offset + 8) << 8) | Marshal.ReadByte(table, offset + 9);
					if (Marshal.ReadInt32(table, offset) == 2 && localPort == port && (address == 0 || address == 0x0100007F))
						owners.Add(Marshal.ReadInt32(table, offset + 20));
				}
				return owners.Distinct().ToArray();
			}
			finally { Marshal.FreeHGlobal(table); }
		}
		throw new SatisfactoryApiException(SatisfactoryApiError.LocalIdentity);
	}

	[DllImport("iphlpapi.dll")]
	private static extern uint GetExtendedTcpTable(IntPtr table, ref int length,
		[MarshalAs(UnmanagedType.Bool)] bool sort, int addressFamily, int tableClass, uint reserved);
}
