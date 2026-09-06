using UserSettings.ServerSpecific;
using UnityEngine;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Registers only the SiteRP open/back keybind. The actual jobs/rules UI is rendered in-game
/// by SiteRpInteractiveUi and navigated with the Radio controls.
/// SCP:SL intentionally requires players to confirm server-proposed keybinds for privacy,
/// therefore J is a suggested key rather than a silently forced client binding.
/// </summary>
public static class JobMenuManager
{
    private const int MenuKeybindId = 771010;

    private static ServerSpecificSettingBase[] _ownedSettings = Array.Empty<ServerSpecificSettingBase>();
    private static ServerSpecificSettingBase[] _foreignSettings = Array.Empty<ServerSpecificSettingBase>();
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        JobWhitelistRepository.Load();
        SiteRpRulesRepository.Load();
        JobCatalog.Reload();

        _ownedSettings = new ServerSpecificSettingBase[]
        {
            new SSGroupHeader(
                "SITERP — RACCOURCI INTERFACE",
                false,
                "Le menu RP s'affiche directement en jeu. Cette section sert uniquement à confirmer la touche d'ouverture/retour."),
            new SSKeybindSetting(
                MenuKeybindId,
                "Menu SiteRP / Retour",
                KeyCode.J,
                true,
                "Touche conseillée: J. Ouvre/ferme le menu métiers et sert de retour dans l'interface."),
        };

        ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        _foreignSettings = existing.Where(x => x.SettingId != MenuKeybindId).ToArray();
        ServerSpecificSettingsSync.DefinedSettings = _foreignSettings.Concat(_ownedSettings).ToArray();
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();

        _registered = true;
        Logger.Info($"[SiteRP UI] Interface Hint/Radio active: {JobCatalog.All.Count} métiers. Touche suggérée: J (ouvrir/retour).");
    }

    public static void Unregister()
    {
        if (!_registered)
            return;

        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
        ServerSpecificSettingsSync.DefinedSettings = _foreignSettings;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();

        _ownedSettings = Array.Empty<ServerSpecificSettingBase>();
        _foreignSettings = Array.Empty<ServerSpecificSettingBase>();
        _registered = false;
    }

    public static void Refresh()
    {
        if (_registered)
            Unregister();
        Register();
    }

    private static void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        if (setting.SettingId != MenuKeybindId || setting is not SSKeybindSetting keybind || !keybind.SyncIsPressed)
            return;

        Player? player = Player.Get(hub);
        if (player is null || !player.IsReady)
            return;

        SiteRpInteractiveUi.HandleMenuKey(player);
    }
}
