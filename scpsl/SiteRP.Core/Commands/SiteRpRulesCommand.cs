using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using SiteRP.Core.Jobs;

namespace SiteRP.Core.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(ClientCommandHandler))]
public sealed class SiteRpRulesCommand : ICommand
{
    public string Command => "siterprules";
    public string[] Aliases => new[] { "rules", "regles", "reglement" };
    public string Description => "Affiche le règlement DarkRP / SCP-RP obligatoire du serveur.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null)
        {
            response = $"Reglement SiteRP v{SiteRpRulesRepository.CurrentVersion}: {SiteRpRulesRepository.Pages.Count} pages. Les joueurs peuvent utiliser .regles en jeu.";
            return true;
        }

        SiteRpInteractiveUi.OpenRules(player, true);
        response = "Reglement SiteRP ouvert. PORTÉE radio = page suivante, J = retour/fermer.";
        return true;
    }
}
