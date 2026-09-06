using MEC;
using PlayerRoles;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Arrival/deployment state. Player onboarding is fully handled by the in-game HUD:
/// rules first, then jobs. M is intentionally never used by SiteRP.
/// </summary>
public static class SiteRpInteractiveUi
{
    private static readonly HashSet<string> DeployedPlayers = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsOpen(Player player) => JobHudManager.IsOpen(player) || RulesHudManager.IsOpen(player);

    public static bool IsDeployed(Player player)
    {
        if (player is null)
            return false;
        string id = JobRuntime.GetPersistentUserId(player);
        return DeployedPlayers.Contains(id) || SiteRpUcrBridge.TryGetActiveRoleId(player, out _);
    }

    public static void BeginArrival(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        string id = JobRuntime.GetPersistentUserId(player);
        DeployedPlayers.Remove(id);
        JobMenuManager.CleanupPlayer(player);
        JobHudManager.Cleanup(player);
        RulesHudManager.Cleanup(player);
        SiteRpUcrBridge.ClearCustomRole(player);
        player.SetRole(RoleTypeId.Tutorial);
        player.IsGodModeEnabled = true;
        player.IsNoclipEnabled = false;
        player.CustomInfo = "ENREGISTREMENT SITERP | NON DEPLOYE";
        player.ClearInventory();

        Timing.CallDelayed(0.45f, () =>
        {
            if (player is null || !player.IsReady || IsDeployed(player))
                return;

            player.ClearInventory();
            if (SiteRpRulesRepository.HasAccepted(player))
                JobHudManager.Open(player, "Choisis ton département puis ton métier.");
            else
                RulesHudManager.Open(player, "Lis les pages du règlement puis valide avec .hud select.");
        });
    }

    public static void OpenJobs(Player player)
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

    // Kept only for source/binary compatibility with older commands.
    // It now opens the normal HUD and never touches M.
    public static void OpenNativeJobs(Player player) => OpenJobs(player);

    public static void OpenRules(Player player, bool reviewOnly = true)
    {
        if (player is null || !player.IsReady)
            return;
        RulesHudManager.Open(player, reviewOnly ? "Consultation du règlement." : null);
    }

    public static void HandleMenuKey(Player player) => OpenJobs(player);

    // Kept for compatibility. Radio is never hijacked.
    public static bool HandleRadioNext(Player player) => false;
    public static bool HandleRadioConfirm(Player player) => false;

    public static void MarkDeployed(Player player)
    {
        if (player is null)
            return;

        DeployedPlayers.Add(JobRuntime.GetPersistentUserId(player));
        player.IsGodModeEnabled = false;
        player.IsNoclipEnabled = false;
        player.CustomInfo = string.Empty;
        SiteRpHudRenderer.Hide(player);
        player.SendBroadcast(
            "<b><color=#73D673>DÉPLOIEMENT AUTORISÉ</color></b>\n" +
            "Ton rôle est actif et tu as été déployé sur la map.\n" +
            "HUD métiers : .hud / .jobs ou la touche que tu as choisie.",
            7);
    }

    public static void CleanupPlayer(Player player)
    {
        if (player is null)
            return;
        DeployedPlayers.Remove(JobRuntime.GetPersistentUserId(player));
        RulesHudManager.Cleanup(player);
        JobHudManager.Cleanup(player);
        JobMenuManager.CleanupPlayer(player);
        SiteRpHudRenderer.Cleanup(player);
    }

    public static void Close(Player player)
    {
        if (player is null)
            return;
        if (RulesHudManager.IsOpen(player))
            RulesHudManager.Close(player);
        else
            JobHudManager.Close(player);
    }
}
