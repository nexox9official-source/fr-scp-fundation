using UnityEngine;
using UserSettings.ServerSpecific;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Native SCP:SL key controls for the SiteRP HUD.
/// Players assign/confirm them once in Server-Specific Settings; normal HUD usage then
/// requires no console commands. M is never used to open the SiteRP HUD.
/// </summary>
public static class JobHudKeybindManager
{
    private const int ToggleId = 770950;
    private const int PreviousJobId = 770951;
    private const int NextJobId = 770952;
    private const int PreviousCategoryId = 770953;
    private const int NextCategoryId = 770954;
    private const int SelectId = 770955;

    private static readonly HashSet<int> OwnedIds = new()
    {
        ToggleId,
        PreviousJobId,
        NextJobId,
        PreviousCategoryId,
        NextCategoryId,
        SelectId,
    };

    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        ServerSpecificSettingBase[] current = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        current = current.Where(x => !OwnedIds.Contains(x.SettingId)).ToArray();

        ServerSpecificSettingBase[] owned =
        {
            new SSGroupHeader(
                "SITERP — CONTROLES HUD RP",
                false,
                "A regler une seule fois. Ensuite aucune commande n'est necessaire pour naviguer dans le reglement ou les metiers."),
            new SSKeybindSetting(
                ToggleId,
                "SITERP : ouvrir / fermer le HUD",
                KeyCode.J,
                true),
            new SSKeybindSetting(
                PreviousJobId,
                "SITERP : metier / page precedente",
                KeyCode.UpArrow,
                true),
            new SSKeybindSetting(
                NextJobId,
                "SITERP : metier / page suivante",
                KeyCode.DownArrow,
                true),
            new SSKeybindSetting(
                PreviousCategoryId,
                "SITERP : departement / page precedent",
                KeyCode.LeftArrow,
                true),
            new SSKeybindSetting(
                NextCategoryId,
                "SITERP : departement / page suivant",
                KeyCode.RightArrow,
                true),
            new SSKeybindSetting(
                SelectId,
                "SITERP : valider / choisir",
                KeyCode.Return,
                true),
        };

        ServerSpecificSettingsSync.DefinedSettings = current.Concat(owned).ToArray();
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();
        _registered = true;

        Logger.Info("[SiteRP HUD] Native HUD controls active. Suggested keys: J toggle, arrows navigate, Enter validates. Commands are fallback only.");
    }

    public static void Unregister()
    {
        if (!_registered)
            return;

        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
        ServerSpecificSettingsSync.DefinedSettings = (ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>())
            .Where(x => !OwnedIds.Contains(x.SettingId))
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
            case ToggleId:
                if (rulesOpen || jobsOpen)
                {
                    SiteRpInteractiveUi.Close(player);
                    return;
                }

                if (SiteRpRulesRepository.HasAccepted(player))
                    JobHudManager.Open(player);
                else
                    RulesHudManager.Open(player);
                return;

            case PreviousJobId:
                if (rulesOpen)
                    RulesHudManager.Previous(player, out _);
                else if (jobsOpen)
                    JobHudManager.PreviousJob(player, out _);
                return;

            case NextJobId:
                if (rulesOpen)
                    RulesHudManager.Next(player, out _);
                else if (jobsOpen)
                    JobHudManager.NextJob(player, out _);
                return;

            case PreviousCategoryId:
                if (rulesOpen)
                    RulesHudManager.Previous(player, out _);
                else if (jobsOpen)
                    JobHudManager.PreviousCategory(player, out _);
                return;

            case NextCategoryId:
                if (rulesOpen)
                    RulesHudManager.Next(player, out _);
                else if (jobsOpen)
                    JobHudManager.NextCategory(player, out _);
                return;

            case SelectId:
                if (rulesOpen)
                    RulesHudManager.Select(player, out _);
                else if (jobsOpen)
                    JobHudManager.Select(player, out _);
                return;
        }
    }
}
