using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

/// <summary>
/// RP policy layer for SCP-079 / C.A.S.S.I.E.
/// Cameras, pings and normal door operation are Foundation-support gameplay.
/// Emergency locks scale with the Site alert; blackouts/Tesla remain hostile-only.
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
        LabLogger.Info("[SiteRP.079] RP policy registered: cooperative door/camera support + alert-level emergency permissions.");
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

    public static void ShowProtocol(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        player.SendBroadcast(
            "<b><color=#00B7EB>SITERP — C.A.S.S.I.E. / SCP-079</color></b>\n" +
            SiteRpScpStateManager.Describe079Permissions() + "\n" +
            "NORMAL: cameras, ping, ouverture/fermeture. INCIDENT: unlock urgence. BREACH: verrouillage. MAJOR/EVAC: lockdown. Tesla/blackout: uniquement HOSTILE/BREACHED.",
            12);
    }

    private static void OnBlackingOutRoom(Scp079BlackingOutRoomEventsArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseHostileSystems)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "BLACKOUT refuse: protocole lethal/hostile. Passage HOSTILE ou BREACHED requis.");
    }

    private static void OnBlackingOutZone(Scp079BlackingOutZoneEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseHostileSystems)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "BLACKOUT DE ZONE refuse: C.A.S.S.I.E. cooperative ne coupe pas une zone entiere.");
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
        if (SiteRpScpStateManager.Can079LockDoors)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "VERROUILLAGE refuse: alerte BREACH minimum pour C.A.S.S.I.E. cooperative.");
    }

    private static void OnUnlockingDoor(Scp079UnlockingDoorEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UnlockDoors)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "DEVERROUILLAGE D'URGENCE refuse: alerte INCIDENT minimum.");
    }

    private static void OnLockingDownRoom(Scp079LockingDownRoomEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079LockdownRooms)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "LOCKDOWN refuse: MAJOR BREACH/EVACUATION minimum, sauf IA hostile.");
    }

    private static void OnUsingTesla(Scp079UsingTeslaEventArgs ev)
    {
        if (SiteRpScpStateManager.Can079UseHostileSystems)
            return;

        ev.IsAllowed = false;
        Denied(ev.Player, "TESLA refusee: protocole lethal interdit a C.A.S.S.I.E. cooperative.");
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
        player?.SendBroadcast($"<b><color=#00B7EB>SITERP IA</color></b>\n{message}\n<color=#8EA0B5>{SiteRpScpStateManager.Describe079Permissions()}</color>", 4);
    }
}
