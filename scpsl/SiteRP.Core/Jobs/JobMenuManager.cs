namespace SiteRP.Core.Jobs;

/// <summary>
/// Compatibility shim kept for older SiteRP calls. SiteRP no longer injects anything
/// into SCP:SL Server-Specific Settings: M is left completely untouched for the
/// server/admin interface. Player interaction is handled by the hint HUD + bindable commands.
/// </summary>
public static class JobMenuManager
{
    public static void Register()
    {
        JobWhitelistRepository.Load();
        SiteRpRulesRepository.Load();
        JobCatalog.Reload();
        Logger.Info($"[SiteRP UI] HUD-only mode active: {JobCatalog.All.Count} métiers. M/Server-Specific Settings untouched.");
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
            "<size=17>La gestion n'utilise plus M.</size>\n" +
            "<size=15>Utilise les commandes Remote Admin: <b>jobs whitelist add/remove</b>.</size></align>",
            8f);
    }
}
