namespace SiteRP.Core.Jobs;

/// <summary>
/// Compatibility shim kept for older SiteRP calls. SiteRP no longer uses the native
/// Server-Specific Settings pages as the jobs/rules menu. Only the small configurable
/// keybind section is registered by JobHudKeybindManager; the actual UI is the HSM HUD.
/// </summary>
public static class JobMenuManager
{
    public static void Register()
    {
        JobWhitelistRepository.Load();
        SiteRpRulesRepository.Load();
        JobCatalog.Reload();
        Logger.Info($"[SiteRP UI] HSM HUD mode active: {JobCatalog.All.Count} métiers. Native settings are used only for customizable HUD keybinds.");
    }

    public static void Unregister()
    {
    }

    public static void Refresh()
    {
        JobWhitelistRepository.Load();
        SiteRpRulesRepository.Load();
        JobCatalog.Reload();
    }

    public static void CleanupPlayer(Player player)
    {
    }

    public static void ShowRules(Player player)
    {
        if (player is null || !player.IsReady)
            return;
        RulesHudManager.Open(player);
    }

    public static void ShowJobs(Player player)
    {
        if (player is null || !player.IsReady)
            return;
        if (!SiteRpRulesRepository.HasAccepted(player))
        {
            RulesHudManager.Open(player, "Le règlement doit être accepté avant le choix du métier.");
            return;
        }
        JobHudManager.Open(player);
    }

    public static void ShowStaff(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        player.SendHint(
            "<align=center><size=27><color=#62A8FF><b>SITERP — WHITELISTS STAFF</b></color></size>\n" +
            "<size=17>La gestion n'utilise pas le HUD joueur.</size>\n" +
            "<size=15>Utilise les commandes Remote Admin: <b>jobs whitelist add/remove</b>.</size></align>",
            8f);
    }
}
