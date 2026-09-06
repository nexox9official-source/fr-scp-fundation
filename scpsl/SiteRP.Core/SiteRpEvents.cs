using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;
using SiteRP.Core.Jobs;
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
        Timing.CallDelayed(2f, SiteRpCorePlugin.EnsurePermanentRoundStarted);
        ScheduleOperationalApply(1.5f, 0);
    }

    public override void OnServerRoundStarted()
    {
        if (SiteRpCorePlugin.PermanentRoundEnabled)
        {
            Round.KeepRoundOnOne = true;
            Round.IsLocked = true;
        }

        SiteRpCorePlugin.CleanupDecorativeRagdolls("round started");
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
                LabLogger.Warn($"[SiteRP.Map] Operational non applique apres {attempt + 1} tentative(s). Dernier resultat: {result}");
                return;
            }
            ScheduleOperationalApply(1.5f + attempt, attempt + 1);
        });
    }

    public override void OnServerRoundEnding(RoundEndingEventArgs ev)
    {
        if (!SiteRpCorePlugin.PermanentRoundEnabled)
            return;
        ev.IsAllowed = false;
        Round.KeepRoundOnOne = true;
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
        if (!SiteRpInteractiveUi.IsDeployed(ev.Player))
        {
            ev.IsAllowed = false;
            ev.Player.SendBroadcast(
                "<b><color=#62A8FF>SITERP</color></b>\nOuvre M → Server Specific Settings, accepte le règlement puis choisis ton métier.",
                4);
            return;
        }

        if (!SiteRpCorePlugin.ContainmentLocked || !ev.Player.IsSCP)
            return;

        string? scpId = SiteRpScpStateManager.GetScpId(ev.Player.Role);
        if (scpId is not null && SiteRpScpStateManager.CanLeaveContainment(scpId))
            return;

        ev.IsAllowed = false;
        string state = scpId is null ? "CONTAINED" : SiteRpScpStateManager.Get(scpId).ToString().ToUpperInvariant();
        ev.Player.SendBroadcast($"<b>CONFINEMENT ACTIF</b>\nEtat SCP: {state}. Porte verrouillee par SiteRP.", 3);
    }

    // IMPORTANT: no radio event is intercepted here anymore.
    // Range changes and power toggles remain 100% vanilla SCP:SL behavior.

    public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        Player player = ev.Player;
        Timing.CallDelayed(0.8f, SiteRpCorePlugin.EnsurePermanentRoundStarted);
        Timing.CallDelayed(1.6f, () =>
        {
            if (player is null || !player.IsReady)
                return;

            SiteRpInteractiveUi.BeginArrival(player);
            player.SendBroadcast(
                "<b><color=#62A8FF>SITERP — BIENVENUE</color></b>\n" +
                "Ouvre <b>M → Server Specific Settings</b>.\n" +
                "Règlement obligatoire puis choix du métier avant déploiement.\n" +
                "La radio fonctionne normalement.",
                12);
        });
    }

    public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev)
    {
        SiteRpCorePlugin.OnPlayerRoleChanged(ev.Player);
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        SiteRpInteractiveUi.CleanupPlayer(ev.Player);
        JobRuntime.CleanupPlayer(ev.Player);
        SiteRpCorePlugin.CleanupPlayer(ev.Player);
    }
}
