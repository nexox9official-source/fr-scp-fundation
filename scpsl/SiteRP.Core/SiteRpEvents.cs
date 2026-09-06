using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

internal sealed class SiteRpEvents : CustomEventsHandler
{
    private static int _operationalGeneration;

    public override void OnServerMapGenerated(MapGeneratedEventArgs ev)
    {
        if (SiteRpCorePlugin.AutomaticMapAudit)
            SiteRpMapSurvey.WriteAudit(ev.Seed);

        SiteRpCorePlugin.CleanupDecorativeRagdolls($"map generated (seed {ev.Seed})");

        // LabAPI's AdminToy wrappers read NetworkClient.prefabs. During MapGenerated the
        // Facility scene can still be loading, so those prefabs may not exist yet.
        // Delay the overlay until the scene/prefab registry has settled instead of throwing.
        ScheduleOperationalApply(1.5f, 0);
    }

    public override void OnServerRoundStarted()
    {
        if (SiteRpCorePlugin.PermanentRoundEnabled)
            Round.IsLocked = true;

        SiteRpCorePlugin.CleanupDecorativeRagdolls("round started");

        // Second safety trigger. If MapGenerated happened before the admin-toy prefabs
        // were available, this retries after RoundStarted without touching the vanilla map.
        ScheduleOperationalApply(0.75f, 0);
    }

    private static void ScheduleOperationalApply(float delay, int attempt)
    {
        int generation = ++_operationalGeneration;
        Timing.CallDelayed(delay, () =>
        {
            if (generation != _operationalGeneration)
                return;

            if (!SiteRpCorePlugin.OperationalMapEnabled || SiteRpOperationalMap.IsApplied)
                return;

            string result = SiteRpOperationalMap.Apply();
            if (SiteRpOperationalMap.IsApplied)
                return;

            if (attempt >= 4)
            {
                LabLogger.Warn($"[SiteRP.Map] Operational non applique apres {attempt + 1} tentative(s). Le reste de SiteRP continue normalement. Dernier resultat: {result}");
                return;
            }

            // NetworkClient.prefabs is sometimes populated slightly later on Linux headless.
            // Retry progressively instead of failing the whole operational layer permanently.
            ScheduleOperationalApply(1.5f + attempt, attempt + 1);
        });
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
