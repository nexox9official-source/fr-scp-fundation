using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

/// <summary>
/// RP policy layer for SCP-079 / C.A.S.S.I.E.
/// The vanilla 079 interface remains intact, but dangerous facility actions are gated by
/// SiteRP's persistent SCP state so 079 can exist as a cooperative Foundation AI.
/// </summary>
internal static class SiteRpScp079Policy
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        Scp079Events.BlackingOutRoom += OnBlackingOutRoom;
        Scp079Events.BlackingOutZone += OnBlackingOutZone;
        Scp079Events.ChangingCamera += OnChangingCamera;
        Scp079Events.LockingDoor += OnLockingDoor;
        Scp079Events.UnlockingDoor += OnUnlockingDoor;
        Scp079Events.LockingDownRoom += OnLockingDownRoom;
        Scp079Events.UsingTesla += OnUsingTesla;
        Scp079Events.Pinging += OnPinging;
        Scp079Events.Recontaining += OnRecontaining;
        Scp079Events.Recontained += OnRecontained;

        _registered = true;
        LabLogger.Info("[SiteRP.079] RP policy registered.");
    }

    public static void Unregister()
    {
        if (!_registered)
            return;

        Scp079Events.BlackingOutRoom -= OnBlackingOutRoom;
        Scp079Events.BlackingOutZone -= OnBlackingOutZone;
        Scp079Events.ChangingCamera -= OnChangingCamera;
        Scp079Events.LockingDoor -= OnLockingDoor;
        Scp079Events.UnlockingDoor -= OnUnlockingDoor;
        Scp079Events.LockingDownRoom -= OnLockingDownRoom;
        Scp079Events.UsingTesla -= OnUsingTesla;
        Scp079Events.Pinging -= OnPinging;
        Scp079Events.Recontaining -= OnRecontaining;
        Scp079Events.Recontained -= OnRecontained;

        _registered = false;
    }

    private static void OnBlackingOutRoom(Scp079BlackingOutRoomEventsArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseHostileSystems)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "BLACKOUT refuse: C.A.S.S.I.E. n'est pas compromise.");
    }

    private static void OnBlackingOutZone(Scp079BlackingOutZoneEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseHostileSystems)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "BLACKOUT DE ZONE refuse: autorisation BREACHED/HOSTILE requise.");
    }

    private static void OnChangingCamera(Scp079ChangingCameraEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseCameras)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "Reseau cameras hors ligne: IA desactivee/reconfinee.");
    }

    private static void OnLockingDoor(Scp079LockingDoorEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseHostileSystems)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "Verrouillage hostile refuse tant que l'IA Fondation est saine.");
    }

    private static void OnUnlockingDoor(Scp079UnlockingDoorEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseFoundationSupport)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "Controle des portes indisponible: IA hors ligne.");
    }

    private static void OnLockingDownRoom(Scp079LockingDownRoomEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseHostileSystems)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "Lockdown hostile refuse: passage en BREACHED/HOSTILE requis.");
    }

    private static void OnUsingTesla(Scp079UsingTeslaEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseHostileSystems)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "Tesla refusee: protocole lethal interdit a C.A.S.S.I.E. saine.");
    }

    private static void OnPinging(Scp079PingingEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseFoundationSupport)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "Ping indisponible: IA hors ligne.");
    }

    private static void OnRecontaining(Scp079RecontainingEventArgs ev)
    {
        SiteRpScpStateManager.TrySetScpState("079", "recontaining", out _);
    }

    private static void OnRecontained(Scp079RecontainedEventArgs ev)
    {
        SiteRpScpStateManager.TrySetScpState("079", "recontained", out _);
        string actor = ev.Activator?.Nickname ?? "systeme";
        LabLogger.Info($"[SiteRP.079] Recontained by {actor}.");
    }

    private static void Denied(Player? player, string message)
    {
        player?.SendBroadcast($"<b><color=#00B7EB>SITERP IA</color></b>\n{message}", 3);
    }
}
