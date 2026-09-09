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

namespace Synix_Control_Panel.SynixEngine
{
	internal enum SmartMaintenanceDecision
	{
		NotDue,
		DeferForPlayers,
		RunNow
	}

	internal sealed record SmartMaintenancePlan(
		SmartMaintenanceDecision Decision,
		DateTime ScheduledFor,
		TimeSpan Delay,
		string Reason);

	internal static class SmartMaintenancePlanner
	{
		internal static SmartMaintenancePlan Evaluate(GameServer server, DateTime now)
		{
			ArgumentNullException.ThrowIfNull(server);
			DateTime fallback = now.Date;
			if (!server.IsScheduledRestartEnabled)
				return NotDue(fallback, "Maintenance.Reason.Disabled");

			int dayIndex = (int)now.DayOfWeek;
			if (server.RestartDays == null ||
				server.RestartDays.Length <= dayIndex ||
				!server.RestartDays[dayIndex])
			{
				return NotDue(fallback, "Maintenance.Reason.DayNotSelected");
			}

			if (server.LastMaintenanceDate == now.ToString("yyyy-MM-dd"))
				return NotDue(fallback, "Maintenance.Reason.AlreadyComplete");

			if (!TimeSpan.TryParseExact(
				server.RestartTime,
				@"hh\:mm",
				System.Globalization.CultureInfo.InvariantCulture,
				out TimeSpan scheduledTime))
			{
				return NotDue(fallback, "Maintenance.Reason.InvalidTime");
			}

			DateTime scheduledFor = now.Date.Add(scheduledTime);
			if (now < scheduledFor)
				return NotDue(scheduledFor, "Maintenance.Reason.NotDue");

			TimeSpan delay = now - scheduledFor;
			if (!server.SmartMaintenanceEnabled && delay >= TimeSpan.FromMinutes(1))
				return NotDue(scheduledFor, "Maintenance.Reason.MinutePassed");

			int maximumDelay = Math.Clamp(server.MaintenanceMaximumDelayMinutes, 0, 720);
			bool playerCountUnknown = SynixApp.ServerHandler.Satisfactory.SatisfactoryIntegration.IsConnected(server) &&
				!SynixApp.ServerHandler.Satisfactory.SatisfactoryIntegration.HasFreshPlayerCount(server);
			if (server.SmartMaintenanceEnabled &&
				server.MaintenanceWaitForPlayers &&
				(server.CurrentPlayers > 0 || playerCountUnknown) &&
				delay < TimeSpan.FromMinutes(maximumDelay))
			{
				return new(
					SmartMaintenanceDecision.DeferForPlayers,
					scheduledFor,
					delay,
					playerCountUnknown ? LocalizationManager.Get("Satisfactory.UnknownPlayers") :
						LocalizationManager.Get("Maintenance.Reason.WaitingForPlayers", server.CurrentPlayers));
			}

			return new(
				SmartMaintenanceDecision.RunNow,
				scheduledFor,
				delay,
				server.CurrentPlayers > 0 && maximumDelay > 0
					? LocalizationManager.Get("Maintenance.Reason.MaximumDelayReached")
					: LocalizationManager.Get("Maintenance.Reason.Ready"));
		}

		private static SmartMaintenancePlan NotDue(DateTime scheduledFor, string resourceKey) =>
			new(
				SmartMaintenanceDecision.NotDue,
				scheduledFor,
				TimeSpan.Zero,
				LocalizationManager.Get(resourceKey));
	}
}
