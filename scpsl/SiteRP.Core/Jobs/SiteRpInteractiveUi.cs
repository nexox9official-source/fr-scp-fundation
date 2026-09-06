using MEC;
using PlayerRoles;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Arrival/deployment state. Primary onboarding is now fully in the in-game HUD:
/// rules first, then jobs. Native Server-Specific Settings remain a fallback/admin UI.
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
            {
                JobHudManager.Open(player, "Choisis ton département puis ton métier.");
            }
            else
            {
                RulesHudManager.Open(player, "Lis les pages du règlement puis valide avec Entrée.");
            }
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

    public static void OpenNativeJobs(Player player)
    {
        if (player is null || !player.IsReady)
            return;
        if (!SiteRpRulesRepository.HasAccepted(player))
        {
            JobMenuManager.ShowRules(player);
            PromptM(player, "Fallback natif RÈGLEMENT prêt.");
            return;
        }

        JobMenuManager.ShowJobs(player);
        PromptM(player, "Fallback natif MÉTIERS prêt.");
    }

    public static void OpenRules(Player player, bool reviewOnly = true)
    {
        if (player is null || !player.IsReady)
            return;
        RulesHudManager.Open(player, reviewOnly ? "Consultation du règlement." : null);
    }

    public static void HandleMenuKey(Player player) => OpenJobs(player);

    // Kept for binary/source compatibility with older builds. Radio is never hijacked.
    public static bool HandleRadioNext(Player player) => false;
    public static bool HandleRadioConfirm(Player player) => false;

    public static void MarkDeployed(Player player)
    {
        if (player is null)
            return;
        DeployedPlayers.Add(JobRuntime.GetPersistentUserId(player));
        player.IsGodModeEnabled = false;
        player.SendHint(
            "<align=center><size=28><color=#73D673><b>DÉPLOIEMENT AUTORISÉ</b></color></size>\n" +
            "<size=18>Bienvenue sur le Site.</size>\n" +
            "<size=16>Le menu métiers reste accessible avec <b>J</b> ou <b>.jobs</b>.</size></align>",
            7f);
    }

    public static void CleanupPlayer(Player player)
    {
        if (player is null)
            return;
        DeployedPlayers.Remove(JobRuntime.GetPersistentUserId(player));
        RulesHudManager.Cleanup(player);
        JobHudManager.Cleanup(player);
        JobMenuManager.CleanupPlayer(player);
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

    private static void PromptM(Player player, string line)
    {
        player.SendHint(
            $"<align=center><size=24><color=#62A8FF><b>SITERP</b></color></size>\n" +
            $"<size=18>{line}</size>\n<size=16>Ouvre <b>M → Server Specific Settings</b>.</size></align>",
            6f);
    }
}
