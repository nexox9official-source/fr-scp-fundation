using UnityEngine;
using UserSettings.ServerSpecific;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Native SCP:SL keybinds for the SiteRP onboarding/jobs HUD. The game requires
/// each player to confirm/assign suggested keys once.
/// </summary>
public static class JobHudKeybindManager
{
    private const int OpenId = 770950;
    private const int PreviousJobId = 770951;
    private const int NextJobId = 770952;
    private const int PreviousCategoryId = 770953;
    private const int NextCategoryId = 770954;
    private const int SelectId = 770955;
    private const int CloseId = 770956;

    private static ServerSpecificSettingBase[] _existing = Array.Empty<ServerSpecificSettingBase>();
    private static ServerSpecificSettingBase[] _owned = Array.Empty<ServerSpecificSettingBase>();
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        _existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        _owned = new ServerSpecificSettingBase[]
        {
            new SSGroupHeader("SITERP — RACCOURCIS INTERFACE RP", false,
                "A configurer une seule fois. J ouvre le règlement si nécessaire, puis le choix des métiers."),
            new SSKeybindSetting(OpenId, "SITERP: ouvrir / rafraichir l'interface", KeyCode.J, true),
            new SSKeybindSetting(PreviousJobId, "SITERP: précédent", KeyCode.LeftArrow, true),
            new SSKeybindSetting(NextJobId, "SITERP: suivant", KeyCode.RightArrow, true),
            new SSKeybindSetting(PreviousCategoryId, "SITERP: département/page précédent", KeyCode.UpArrow, true),
            new SSKeybindSetting(NextCategoryId, "SITERP: département/page suivant", KeyCode.DownArrow, true),
            new SSKeybindSetting(SelectId, "SITERP: continuer / choisir", KeyCode.Return, true),
            new SSKeybindSetting(CloseId, "SITERP: fermer l'interface", KeyCode.Backspace, true),
        };

        ServerSpecificSettingsSync.DefinedSettings = _existing.Concat(_owned).ToArray();
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();
        _registered = true;

        Logger.Info("[SiteRP HUD] Raccourcis natifs enregistrés: J, flèches, Entrée, Retour arrière.");
    }

    public static void Unregister()
    {
        if (!_registered)
            return;

        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
        HashSet<int> ownedIds = new() { OpenId, PreviousJobId, NextJobId, PreviousCategoryId, NextCategoryId, SelectId, CloseId };
        ServerSpecificSettingsSync.DefinedSettings = (ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>())
            .Where(x => !ownedIds.Contains(x.SettingId))
            .ToArray();
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();
        _registered = false;
    }

    private static void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        if (setting is not SSKeybindSetting keybind || !keybind.SyncIsPressed)
            return;

        Player? player = Player.Get(hub);
        if (player is null || !player.IsReady)
            return;

        bool rulesOpen = RulesHudManager.IsOpen(player);
        bool jobsOpen = JobHudManager.IsOpen(player);

        switch (setting.SettingId)
        {
            case OpenId:
                if (SiteRpRulesRepository.HasAccepted(player))
                    JobHudManager.Open(player);
                else
                    RulesHudManager.Open(player);
                break;
            case PreviousJobId:
                if (rulesOpen)
                    RulesHudManager.Previous(player, out _);
                else if (jobsOpen)
                    JobHudManager.PreviousJob(player, out _);
                break;
            case NextJobId:
                if (rulesOpen)
                    RulesHudManager.Next(player, out _);
                else if (jobsOpen)
                    JobHudManager.NextJob(player, out _);
                break;
            case PreviousCategoryId:
                if (rulesOpen)
                    RulesHudManager.Previous(player, out _);
                else if (jobsOpen)
                    JobHudManager.PreviousCategory(player, out _);
                break;
            case NextCategoryId:
                if (rulesOpen)
                    RulesHudManager.Next(player, out _);
                else if (jobsOpen)
                    JobHudManager.NextCategory(player, out _);
                break;
            case SelectId:
                if (rulesOpen)
                    RulesHudManager.Select(player, out _);
                else if (jobsOpen)
                    JobHudManager.Select(player, out _);
                break;
            case CloseId:
                if (rulesOpen)
                    RulesHudManager.Close(player);
                else if (jobsOpen)
                    JobHudManager.Close(player);
                break;
        }
    }
}
