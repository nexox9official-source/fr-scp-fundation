using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
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
        // SCPs are controlled by the containment/079 policy below, not by human onboarding.
        if (!ev.Player.IsSCP && !SiteRpInteractiveUi.IsDeployed(ev.Player))
        {
            ev.IsAllowed = false;
            ev.Player.SendBroadcast(
                "<b><color=#62A8FF>SITERP — ENREGISTREMENT REQUIS</color></b>\nOuvre le HUD, accepte le règlement puis choisis ton métier.",
                4);
            return;
        }

        if (!SiteRpCorePlugin.ContainmentLocked || !ev.Player.IsSCP)
            return;

        // C.A.S.S.I.E. cooperative is allowed to operate ordinary doors from cameras.
        // Special lock/unlock/lockdown permissions are handled by SiteRpScp079Policy.
        if (ev.Player.Role == RoleTypeId.Scp079)
        {
            if (SiteRpScpStateManager.Can079OpenCloseDoors)
                return;

            ev.IsAllowed = false;
            ev.Player.SendBroadcast("<b><color=#00B7EB>SITERP IA</color></b>\nControle des portes indisponible: C.A.S.S.I.E. hors ligne/reconfinee.", 3);
            return;
        }

        string? scpId = SiteRpScpStateManager.GetScpId(ev.Player.Role);
        if (scpId is not null && SiteRpScpStateManager.CanLeaveContainment(scpId))
            return;

        ev.IsAllowed = false;
        string state = scpId is null ? "CONTAINED" : SiteRpScpStateManager.Get(scpId).ToString().ToUpperInvariant();
        ev.Player.SendBroadcast($"<b>CONFINEMENT ACTIF</b>\nEtat SCP: {state}. Porte verrouillee par SiteRP.", 3);
    }

    // Radio behavior remains completely vanilla.

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
                "L'interface d'admission s'affiche automatiquement.\n" +
                "Règlement → choix du métier → déploiement sur la map.\n" +
                "Le HUD se pilote avec les raccourcis SiteRP configurables; les commandes restent un secours.\n" +
                "<b>M reste réservé à ton interface native/admin.</b> La radio fonctionne normalement.",
                12);
        });
    }

    public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev)
    {
        SiteRpCorePlugin.OnPlayerRoleChanged(ev.Player);
        SiteRpCustomTeamDisplay.ScheduleRefresh(ev.Player);

        if (ev.Player.Role == RoleTypeId.Scp079)
            Timing.CallDelayed(1f, () => SiteRpScp079Policy.ShowProtocol(ev.Player));
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        SiteRpInteractiveUi.CleanupPlayer(ev.Player);
        JobRuntime.CleanupPlayer(ev.Player);
        SiteRpCorePlugin.CleanupPlayer(ev.Player);
    }
}
