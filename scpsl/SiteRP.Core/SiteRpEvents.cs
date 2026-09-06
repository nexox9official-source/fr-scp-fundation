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

        // Start the persistent Site even when the server is empty. If the base game rejects
        // a zero-player force-start on a particular build, OnPlayerJoined retries immediately.
        Timing.CallDelayed(2f, SiteRpCorePlugin.EnsurePermanentRoundStarted);

        // LabAPI's AdminToy wrappers read NetworkClient.prefabs. During MapGenerated the
        // Facility scene can still be loading, so those prefabs may not exist yet.
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
                LabLogger.Warn($"[SiteRP.Map] Operational non applique apres {attempt + 1} tentative(s). Le reste de SiteRP continue normalement. Dernier resultat: {result}");
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
        // Arrival gate: a player must read/accept the rules and select a SiteRP job before
        // being allowed to use the Facility as an RP character.
        if (!SiteRpInteractiveUi.IsDeployed(ev.Player))
        {
            ev.IsAllowed = false;
            ev.Player.SendBroadcast("<b><color=#62A8FF>SITERP</color></b>\nAccepte le règlement et choisis ton métier avant le déploiement.", 3);
            return;
        }

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

    public override void OnPlayerChangingRadioRange(PlayerChangingRadioRangeEventArgs ev)
    {
        if (!SiteRpInteractiveUi.IsOpen(ev.Player))
            return;

        ev.IsAllowed = false;
        SiteRpInteractiveUi.HandleRadioNext(ev.Player);
    }

    public override void OnPlayerTogglingRadio(PlayerTogglingRadioEventArgs ev)
    {
        if (!SiteRpInteractiveUi.IsOpen(ev.Player))
            return;

        ev.IsAllowed = false;
        SiteRpInteractiveUi.HandleRadioConfirm(ev.Player);
    }

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
                "Règlement obligatoire puis choix du métier avant déploiement.\n" +
                "Dans l'interface: PORTÉE RADIO = suivant, ON/OFF = valider, J = retour.",
                10);
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
