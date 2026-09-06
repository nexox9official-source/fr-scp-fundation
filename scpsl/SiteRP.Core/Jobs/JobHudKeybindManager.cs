using UnityEngine;
using UserSettings.ServerSpecific;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Optional native SCP:SL keybinds for the jobs HUD. The game intentionally requires
/// each player to confirm/assign the suggested keys once for privacy reasons.
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
            new SSGroupHeader("SITERP — RACCOURCIS HUD METIERS", false,
                "A configurer une seule fois. SCP:SL demande au joueur de confirmer les touches pour proteger sa vie privee."),
            new SSKeybindSetting(OpenId, "SITERP: ouvrir / rafraichir le HUD metiers", KeyCode.J, true,
                "Suggestion: J. Ouvre le HUD sans passer par le menu M."),
            new SSKeybindSetting(PreviousJobId, "SITERP: metier precedent", KeyCode.LeftArrow, true),
            new SSKeybindSetting(NextJobId, "SITERP: metier suivant", KeyCode.RightArrow, true),
            new SSKeybindSetting(PreviousCategoryId, "SITERP: departement precedent", KeyCode.UpArrow, true),
            new SSKeybindSetting(NextCategoryId, "SITERP: departement suivant", KeyCode.DownArrow, true),
            new SSKeybindSetting(SelectId, "SITERP: choisir / rejoindre le metier", KeyCode.Return, true),
            new SSKeybindSetting(CloseId, "SITERP: fermer le HUD", KeyCode.Backspace, true),
        };

        ServerSpecificSettingsSync.DefinedSettings = _existing.Concat(_owned).ToArray();
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();
        _registered = true;

        Logger.Info("[SiteRP HUD] Raccourcis natifs enregistres: J, fleches, Entree, Retour arriere (touches suggerees, confirmation joueur requise)." );
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

        switch (setting.SettingId)
        {
            case OpenId:
                JobHudManager.Open(player);
                break;
            case PreviousJobId:
                if (JobHudManager.IsOpen(player))
                    JobHudManager.PreviousJob(player, out _);
                break;
            case NextJobId:
                if (JobHudManager.IsOpen(player))
                    JobHudManager.NextJob(player, out _);
                break;
            case PreviousCategoryId:
                if (JobHudManager.IsOpen(player))
                    JobHudManager.PreviousCategory(player, out _);
                break;
            case NextCategoryId:
                if (JobHudManager.IsOpen(player))
                    JobHudManager.NextCategory(player, out _);
                break;
            case SelectId:
                if (JobHudManager.IsOpen(player))
                    JobHudManager.Select(player, out _);
                break;
            case CloseId:
                if (JobHudManager.IsOpen(player))
                    JobHudManager.Close(player);
                break;
        }
    }
}
