using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;

namespace SiteRP.Core;

internal sealed class SiteRpEvents : CustomEventsHandler
{
    public override void OnServerRoundStarted()
    {
        if (SiteRpCorePlugin.PermanentRoundEnabled)
            Round.IsLocked = true;
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

    public override void OnPlayerInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (!SiteRpCorePlugin.ContainmentLocked || !ev.Player.IsSCP)
            return;

        ev.IsAllowed = false;
        ev.Player.SendBroadcast("<b>CONFINEMENT ACTIF</b>\nCette porte ne peut pas etre ouverte par un SCP.", 3);
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
