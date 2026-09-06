using LabApi.Features.Wrappers;

namespace SiteRP.Core;

/// <summary>
/// Central RP alarm controller used by physical facility buttons.
/// Alarm state also drives SCP-079 cooperative emergency permissions.
/// </summary>
internal static class SiteRpAlarmSystem
{
    private static readonly Dictionary<int, DateTime> LastUse = new();

    public static void Press(Player player, SiteRpSiteState target)
    {
        if (player is null || !player.IsReady)
            return;

        if (!CanOperate(player))
        {
            player.SendBroadcast("<b><color=#FF6961>CONTROLE ALARME REFUSE</color></b>\nAcces reserve a la Direction, Securite, commandement FIM ou staff.", 4);
            return;
        }

        if (LastUse.TryGetValue(player.PlayerId, out DateTime last) && (DateTime.UtcNow - last).TotalSeconds < 2.5)
        {
            player.SendBroadcast("<b>CONTROLE ALARME</b>\nAttends quelques secondes avant une nouvelle commande.", 3);
            return;
        }
        LastUse[player.PlayerId] = DateTime.UtcNow;

        if (!SiteRpScpStateManager.TrySetSiteState(ToCommand(target), out string response))
        {
            player.SendBroadcast($"<b>CONTROLE ALARME</b>\n{response}", 4);
            return;
        }

        string color = Color(target);
        string label = Label(target);
        string actor = player.Nickname;

        foreach (Player targetPlayer in Player.ReadyList)
        {
            targetPlayer.SendBroadcast(
                $"<b><color={color}>SITERP — {label}</color></b>\n" +
                $"Etat du Site modifie par {actor}.\n" +
                $"C.A.S.S.I.E.: {SiteRpScpStateManager.Describe079Permissions()}",
                8);
        }

        Announce(target);
        Logger.Info($"[SiteRP.Alarm] {actor} ({player.UserId}) => {target}. {SiteRpScpStateManager.Describe079Permissions()}");
    }

    public static bool CanOperate(Player player)
    {
        if (SiteRpCorePlugin.IsStaffMode(player) || player.IsNorthwoodStaff)
            return true;

        if (!SiteRpUcrBridge.TryGetActiveRoleId(player, out int id))
            return false;

        // Administration/Direction + all Site security + FIM coordinator + FIM commandants.
        if (id >= 1001 && id < 1100)
            return true;
        if (id >= 1401 && id < 1500)
            return true;
        if (id == 1590)
            return true;
        if (id >= 1600 && id < 1800 && id % 10 == 1)
            return true;

        return false;
    }

    private static void Announce(SiteRpSiteState state)
    {
        try
        {
            switch (state)
            {
                case SiteRpSiteState.Normal:
                    Announcer.Message("ATTENTION ALL CLEAR", "ALERTE LEVEE — STATUT DU SITE : NORMAL", true, 4f, 0f);
                    break;
                case SiteRpSiteState.Incident:
                    Announcer.Message("ATTENTION FACILITY INCIDENT", "ALERTE INCIDENT — PERSONNEL EN VIGILANCE", true, 5f, 0f);
                    break;
                case SiteRpSiteState.Breach:
                    Announcer.Message("ATTENTION CONTAINMENT BREACH DETECTED", "ALERTE CONFINEMENT — BREACH", true, 7f, 0f);
                    break;
                case SiteRpSiteState.MajorBreach:
                    Announcer.Message("WARNING CONTAINMENT BREACH", "ALERTE MAJEURE — RUPTURE DE CONFINEMENT", true, 8f, 0f);
                    break;
                case SiteRpSiteState.Evacuation:
                    Announcer.Message("ATTENTION ALL PERSONNEL EVACUATE", "EVACUATION DU SITE ORDONNEE", true, 10f, 0f);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"[SiteRP.Alarm] Annonce C.A.S.S.I.E. impossible: {ex.GetBaseException().Message}");
        }
    }

    private static string ToCommand(SiteRpSiteState state) => state switch
    {
        SiteRpSiteState.Normal => "normal",
        SiteRpSiteState.Incident => "incident",
        SiteRpSiteState.Breach => "breach",
        SiteRpSiteState.MajorBreach => "major_breach",
        SiteRpSiteState.Evacuation => "evacuation",
        _ => "normal",
    };

    private static string Label(SiteRpSiteState state) => state switch
    {
        SiteRpSiteState.Normal => "SITE NORMAL",
        SiteRpSiteState.Incident => "ALERTE INCIDENT",
        SiteRpSiteState.Breach => "ALERTE BREACH",
        SiteRpSiteState.MajorBreach => "ALERTE MAJEURE",
        SiteRpSiteState.Evacuation => "EVACUATION",
        _ => state.ToString().ToUpperInvariant(),
    };

    private static string Color(SiteRpSiteState state) => state switch
    {
        SiteRpSiteState.Normal => "#73D673",
        SiteRpSiteState.Incident => "#FFB84D",
        SiteRpSiteState.Breach => "#FF6961",
        SiteRpSiteState.MajorBreach => "#FF3030",
        SiteRpSiteState.Evacuation => "#62A8FF",
        _ => "#FFFFFF",
    };
}
