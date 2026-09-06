using MEC;
using PlayerRoles;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Arrival/deployment state. The actual interactive UI is the native SCP:SL
/// Server-Specific Settings screen managed by JobMenuManager.
/// </summary>
public static class SiteRpInteractiveUi
{
    private static readonly HashSet<string> DeployedPlayers = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsOpen(Player player) => false;

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
                JobMenuManager.ShowJobs(player);
            else
                JobMenuManager.ShowRules(player);

            player.SendHint(
                "<align=center><size=30><color=#62A8FF><b>SITERP — ENREGISTREMENT</b></color></size>\n" +
                "<size=20>Ouvre le menu <b>M</b> puis <b>Server Specific Settings</b>.</size>\n" +
                "<size=17>Règlement obligatoire → choix du métier → déploiement.</size>\n" +
                "<size=15><color=#B9C7D8>La radio fonctionne normalement et n'est plus utilisée par l'interface.</color></size></align>",
                15f);
        });
    }

    public static void OpenJobs(Player player)
    {
        if (player is null || !player.IsReady)
            return;
        if (!SiteRpRulesRepository.HasAccepted(player))
        {
            OpenRules(player, false);
            return;
        }
        JobMenuManager.ShowJobs(player);
        PromptM(player, "La page MÉTIERS est prête.");
    }

    public static void OpenRules(Player player, bool reviewOnly = true)
    {
        if (player is null || !player.IsReady)
            return;
        JobMenuManager.ShowRules(player);
        PromptM(player, "La page RÈGLEMENT est prête.");
    }

    public static void HandleMenuKey(Player player) => OpenJobs(player);

    // Kept only for binary/source compatibility with older commands. Radio events no longer call these.
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
            "<size=18>Bienvenue sur le Site. Les métiers restent accessibles depuis <b>M</b>.</size></align>",
            6f);
    }

    public static void CleanupPlayer(Player player)
    {
        if (player is null)
            return;
        DeployedPlayers.Remove(JobRuntime.GetPersistentUserId(player));
        JobMenuManager.CleanupPlayer(player);
    }

    public static void Close(Player player)
    {
        // The native M interface is opened/closed by the SCP:SL client itself.
    }

    private static void PromptM(Player player, string line)
    {
        player.SendHint(
            $"<align=center><size=24><color=#62A8FF><b>SITERP</b></color></size>\n" +
            $"<size=18>{line}</size>\n<size=16>Ouvre <b>M → Server Specific Settings</b>.</size></align>",
            6f);
    }
}
