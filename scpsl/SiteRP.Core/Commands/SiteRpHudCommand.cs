using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using SiteRP.Core.Jobs;

namespace SiteRP.Core.Commands;

/// <summary>
/// Single bind-friendly client command for every SiteRP HUD action.
/// Players can map any key with SCP:SL's native bind/cmdbind command.
/// </summary>
[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SiteRpHudCommand : ICommand
{
    public string Command => "siterphud";
    public string[] Aliases => new[] { "hud", "rphud" };
    public string Description => "Interface SiteRP bindable: hud [jobs|rules|prev|next|catprev|catnext|select|close|help].";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null)
        {
            response = Help();
            return true;
        }

        string[] args = arguments.ToArray();
        string action = args.Length == 0 ? "open" : args[0].Trim().ToLowerInvariant();

        switch (action)
        {
            case "open":
            case "toggle":
            case "menu":
            case "ouvrir":
                OpenPrimary(player);
                response = "Interface SiteRP ouverte.";
                return true;

            case "jobs":
            case "job":
            case "metiers":
            case "metier":
                SiteRpInteractiveUi.OpenJobs(player);
                response = SiteRpRulesRepository.HasAccepted(player)
                    ? "HUD métiers ouvert."
                    : "Règlement requis avant le choix du métier.";
                return true;

            case "rules":
            case "rule":
            case "regles":
            case "reglement":
                SiteRpInteractiveUi.OpenRules(player, true);
                response = "HUD règlement ouvert.";
                return true;

            case "prev":
            case "previous":
            case "precedent":
                return Previous(player, out response);

            case "next":
            case "suivant":
                return Next(player, out response);

            case "catprev":
            case "categoryprev":
            case "departementprev":
                if (RulesHudManager.IsOpen(player))
                    return RulesHudManager.Previous(player, out response);
                if (JobHudManager.IsOpen(player))
                    return JobHudManager.PreviousCategory(player, out response);
                response = "Aucun HUD SiteRP ouvert.";
                return false;

            case "catnext":
            case "categorynext":
            case "departementnext":
                if (RulesHudManager.IsOpen(player))
                    return RulesHudManager.Next(player, out response);
                if (JobHudManager.IsOpen(player))
                    return JobHudManager.NextCategory(player, out response);
                response = "Aucun HUD SiteRP ouvert.";
                return false;

            case "select":
            case "confirm":
            case "valider":
            case "choisir":
                if (RulesHudManager.IsOpen(player))
                    return RulesHudManager.Select(player, out response);
                if (JobHudManager.IsOpen(player))
                    return JobHudManager.Select(player, out response);
                response = "Aucun HUD SiteRP ouvert.";
                return false;

            case "close":
            case "fermer":
                SiteRpInteractiveUi.Close(player);
                response = "Interface SiteRP fermée.";
                return true;

            case "help":
            case "touches":
            case "keys":
            case "binds":
                response = Help();
                player.SendHint(BindHelpHint(), 18f);
                return true;

            default:
                response = Help();
                return false;
        }
    }

    private static void OpenPrimary(Player player)
    {
        if (SiteRpRulesRepository.HasAccepted(player))
            JobHudManager.Open(player);
        else
            RulesHudManager.Open(player);
    }

    private static bool Previous(Player player, out string response)
    {
        if (RulesHudManager.IsOpen(player))
            return RulesHudManager.Previous(player, out response);
        if (JobHudManager.IsOpen(player))
            return JobHudManager.PreviousJob(player, out response);
        response = "Aucun HUD SiteRP ouvert.";
        return false;
    }

    private static bool Next(Player player, out string response)
    {
        if (RulesHudManager.IsOpen(player))
            return RulesHudManager.Next(player, out response);
        if (JobHudManager.IsOpen(player))
            return JobHudManager.NextJob(player, out response);
        response = "Aucun HUD SiteRP ouvert.";
        return false;
    }

    private static string Help() =>
        "SiteRP HUD\n" +
        ".hud = ouvre le règlement ou les métiers\n" +
        ".hud rules = règlement\n" +
        ".hud jobs = métiers\n" +
        ".hud prev / next = navigation\n" +
        ".hud catprev / catnext = département (ou page du règlement)\n" +
        ".hud select = continuer / valider / choisir\n" +
        ".hud close = fermer\n\n" +
        "Tu peux choisir tes propres touches dans la console client (~) avec bind/cmdbind, par exemple:\n" +
        "bind j .hud\n" +
        "bind leftarrow .hud prev\n" +
        "bind rightarrow .hud next\n" +
        "bind uparrow .hud catprev\n" +
        "bind downarrow .hud catnext\n" +
        "bind enter .hud select\n" +
        "bind backspace .hud close";

    private static string BindHelpHint() =>
        "<align=center><size=27><color=#62A8FF><b>SITERP — TOUCHES HUD</b></color></size>\n" +
        "<size=17>Les touches sont <b>libres</b> et choisies par chaque joueur.</size>\n" +
        "<size=15>Ouvre la console <b>~</b>, puis utilise <b>bind &lt;touche&gt; .hud ...</b>.</size>\n" +
        "<size=15>Exemple: <b>bind j .hud</b> pour ouvrir le HUD.</size>\n" +
        "<size=14>Écris <b>.hud help</b> dans la console pour voir toutes les commandes.</size></align>";
}
