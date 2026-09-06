using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using SiteRP.Core.Jobs;

namespace SiteRP.Core.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(ClientCommandHandler))]
public sealed class SiteRpSkinCommand : ICommand
{
    public string Command => "siterpskin";
    public string[] Aliases => new[] { "rpskin", "skinrp" };
    public string Description => "Teste les tenues SLWardrobe SiteRP sur toi-meme (staff).";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null)
        {
            response = "Commande joueur uniquement.";
            return false;
        }

        if (!JobRuntime.IsStaff(player))
        {
            response = "Acces refuse : staff/Remote Admin requis.";
            return false;
        }

        string[] args = arguments.ToArray();
        if (args.Length == 0)
        {
            response = Help();
            return true;
        }

        string action = args[0].ToLowerInvariant();
        if (action == "list")
        {
            string[] suits = JobCatalog.All
                .Select(x => x.WardrobeName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            response = suits.Length == 0 ? "Aucune tenue SiteRP referencee." : string.Join("\n", suits);
            return true;
        }

        if (action == "remove" || action == "off")
        {
            SiteRpSkinBridge.RemoveSuit(player);
            response = "Tenue SiteRP retiree.";
            return true;
        }

        if (action == "role")
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int roleId))
            {
                response = "Usage: rpskin role <roleId>";
                return false;
            }

            JobDefinition? job = JobCatalog.Find(roleId);
            if (job is null || string.IsNullOrWhiteSpace(job.WardrobeName))
            {
                response = "Ce role n'a pas encore de tenue custom associee.";
                return false;
            }

            return SiteRpSkinBridge.ApplySuit(player, job.WardrobeName, out response)
                ? Success(job.WardrobeName, out response)
                : false;
        }

        if (action == "preview" || action == "apply" || action == "wear")
        {
            if (args.Length < 2)
            {
                response = "Usage: rpskin preview <nomTenue>";
                return false;
            }

            string suitName = args[1];
            if (!SiteRpSkinBridge.ApplySuit(player, suitName, out string error))
            {
                response = error;
                return false;
            }

            return Success(suitName, out response);
        }

        response = Help();
        return false;
    }

    private static bool Success(string suitName, out string response)
    {
        response = $"Tenue demandee: {suitName}. Active 'Own Suit Visibility' dans Echap > Parametres > Server-Specific > SLWardrobe pour la voir sur toi.";
        return true;
    }

    private static string Help() =>
        "SiteRP Skins:\n" +
        "rpskin list\n" +
        "rpskin preview <nomTenue>\n" +
        "rpskin role <roleId>\n" +
        "rpskin remove";
}
