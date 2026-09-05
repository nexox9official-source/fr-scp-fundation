using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;

namespace SiteRP.Core;

internal sealed class SiteRpEvents : CustomEventsHandler
{
    public override void OnServerMapGenerated(MapGeneratedEventArgs ev)
    {
        if (SiteRpCorePlugin.AutomaticMapAudit)
            SiteRpMapSurvey.WriteAudit(ev.Seed);

        SiteRpCorePlugin.CleanupDecorativeRagdolls($"map generated (seed {ev.Seed})");

        if (SiteRpCorePlugin.OperationalMapEnabled)
            SiteRpOperationalMap.Apply();
    }

    public override void OnServerRoundStarted()
    {
        if (SiteRpCorePlugin.PermanentRoundEnabled)
            Round.IsLocked = true;

        SiteRpCorePlugin.CleanupDecorativeRagdolls("round started");

        // Safety retry in case an AdminToy prefab was not available during MapGenerated.
        if (SiteRpCorePlugin.OperationalMapEnabled && !SiteRpOperationalMap.IsApplied)
            SiteRpOperationalMap.Apply();
    }

    public override void OnServerRoundEnding(RoundEndingEventArgs ev)
    {
        if (!SiteRpCorePlugin.PermanentRoundEnabled)
            return;

        ev.IsAllowed = false;
        Round.IsLocked = true;
    }

    public override void OnServerWaveRespawning(WaveRespawningEventArgs ev)
    {
        if (SiteRpCorePlugin.BlockAutomaticWaves)
            ev.IsAllowed = false;
    }

    public override void OnServerLczDecontaminationStarting(LczDecontaminationStartingEventArgs ev)
    {
        if (SiteRpCorePlugin.BlockDecontamination)
            ev.IsAllowed = false;
    }

    public override void OnWarheadStarting(WarheadStartingEventArgs ev)
    {
        if (SiteRpCorePlugin.BlockWarhead)
            ev.IsAllowed = false;
    }

    public override void OnPlayerEscaping(PlayerEscapingEventArgs ev)
    {
        if (SiteRpCorePlugin.BlockEscapes)
            ev.IsAllowed = false;
    }

    public override void OnPlayerPlacingBlood(PlayerPlacingBloodEventArgs ev)
    {
        if (SiteRpCorePlugin.BlockBloodDecals)
            ev.IsAllowed = false;
    }

    public override void OnPlayerInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (!SiteRpCorePlugin.ContainmentLocked || !ev.Player.IsSCP)
            return;

        string? scpId = SiteRpScpStateManager.GetScpId(ev.Player.Role);
        if (scpId is not null && SiteRpScpStateManager.CanLeaveContainment(scpId))
            return;

        ev.IsAllowed = false;
        string state = scpId is null
            ? "CONTAINED"
            : SiteRpScpStateManager.Get(scpId).ToString().ToUpperInvariant();

        ev.Player.SendBroadcast(
            $"<b>CONFINEMENT ACTIF</b>\nEtat SCP: {state}. Porte verrouillee par SiteRP.",
            3);
    }

    public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev)
    {
        SiteRpCorePlugin.OnPlayerRoleChanged(ev.Player);
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        SiteRpCorePlugin.CleanupPlayer(ev.Player);
    }
}
