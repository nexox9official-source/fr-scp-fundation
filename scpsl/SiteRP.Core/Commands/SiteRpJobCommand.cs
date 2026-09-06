using System;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using SiteRP.Core.Jobs;

namespace SiteRP.Core.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(ClientCommandHandler))]
public sealed class SiteRpJobCommand : ICommand
{
    public string Command => "siterpjob";
    public string[] Aliases => new[] { "srjob", "jobsrp", "job", "jobs", "metier", "metiers" };
    public string Description => "SiteRP Jobs: HUD metiers, deploiement et gestion des whitelists persistantes.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string[] args = arguments.ToArray();
        Player? actor = Player.Get(sender);

        if (args.Length == 0)
        {
            if (actor is null)
            {
                response = Help();
                return true;
            }

            SiteRpInteractiveUi.OpenJobs(actor);
            response = "HUD SiteRP ouvert. Navigation: jobs prev/next, jobs catprev/catnext, jobs select, jobs close.";
            return true;
        }

        string action = args[0].ToLowerInvariant();

        if (action == "menu" || action == "open" || action == "ouvrir" || action == "hud")
        {
            if (!RequirePlayer(actor, out response))
                return false;

            SiteRpInteractiveUi.OpenJobs(actor!);
            response = "HUD SiteRP ouvert.";
            return true;
        }

        if (action == "native" || action == "m" || action == "settings" || action == "reglages")
        {
            if (!RequirePlayer(actor, out response))
                return false;

            SiteRpInteractiveUi.OpenNativeJobs(actor!);
            response = "Fallback natif envoye. Ouvre M -> Server Specific Settings -> METIERS.";
            return true;
        }

        if (action == "prev" || action == "previous" || action == "precedent")
        {
            if (!RequirePlayer(actor, out response))
                return false;
            return JobHudManager.PreviousJob(actor!, out response);
        }

        if (action == "next" || action == "suivant")
        {
            if (!RequirePlayer(actor, out response))
                return false;
            return JobHudManager.NextJob(actor!, out response);
        }

        if (action == "catprev" || action == "categoryprev" || action == "departementprev")
        {
            if (!RequirePlayer(actor, out response))
                return false;
            return JobHudManager.PreviousCategory(actor!, out response);
        }

        if (action == "catnext" || action == "categorynext" || action == "departementnext")
        {
            if (!RequirePlayer(actor, out response))
                return false;
            return JobHudManager.NextCategory(actor!, out response);
        }

        if (action == "select" || action == "choose" || action == "confirm" || action == "confirmer")
        {
            if (!RequirePlayer(actor, out response))
                return false;
            return JobHudManager.Select(actor!, out response);
        }

        if (action == "close" || action == "fermer")
        {
            if (!RequirePlayer(actor, out response))
                return false;
            JobHudManager.Close(actor!);
            response = "HUD SiteRP ferme.";
            return true;
        }

        if (action == "refresh" || action == "actualiser")
        {
            if (!RequirePlayer(actor, out response))
                return false;
            JobHudManager.Refresh(actor!);
            response = "HUD SiteRP actualise.";
            return true;
        }

        if (action == "join")
        {
            if (!RequirePlayer(actor, out response))
                return false;

            if (!SiteRpRulesRepository.HasAccepted(actor!))
            {
                SiteRpInteractiveUi.OpenRules(actor!, false);
                response = "Accepte d'abord le reglement dans M -> Server Specific Settings.";
                return false;
            }

            if (args.Length < 2 || !int.TryParse(args[1], out int roleId))
            {
                response = "Usage: jobs join <roleId>";
                return false;
            }

            bool initial = !SiteRpInteractiveUi.IsDeployed(actor!);
            bool result = JobRuntime.TryJoin(actor!, roleId, out response, initial);
            if (result && initial)
                SiteRpInteractiveUi.MarkDeployed(actor!);
            else if (!result)
                JobHudManager.Open(actor!, response);
            return result;
        }

        if (action == "list" || action == "liste")
        {
            StringBuilder sb = new();
            foreach (JobDefinition job in JobCatalog.All.OrderBy(x => x.SortOrder))
            {
                sb.Append(job.UcrRoleId).Append(" = ").Append(job.Name)
                    .Append(" [").Append(job.AccessMode).Append("] ")
                    .Append(JobRuntime.CountPlayersOnRole(job.UcrRoleId)).Append('/')
                    .Append(job.MaxPlayers <= 0 ? "∞" : job.MaxPlayers.ToString()).AppendLine();
            }
            response = sb.ToString();
            return true;
        }

        if (!HasManagementAccess(actor))
        {
            response = "Acces refuse : Remote Admin / siterp.jobs.manage requis.";
            return false;
        }

        if (action == "reload")
        {
            JobWhitelistRepository.Load();
            JobMenuManager.Refresh();
            if (actor is not null)
                JobHudManager.Refresh(actor);
            response = "SiteRP recharge : roles, whitelists, regles, HUD et fallback M actualises.";
            return true;
        }

        if (action != "whitelist" && action != "wl")
        {
            response = Help();
            return false;
        }

        if (args.Length < 2)
        {
            response = WhitelistHelp();
            return false;
        }

        string sub = args[1].ToLowerInvariant();
        if (sub == "add" || sub == "grant")
        {
            if (args.Length < 4 || !int.TryParse(args[3], out int roleId))
            {
                response = "Usage: jobs whitelist add <playerId|steamId64> <roleId>";
                return false;
            }

            JobDefinition? role = JobCatalog.Find(roleId);
            if (role is null)
            {
                response = $"Role SiteRP inconnu: {roleId}.";
                return false;
            }

            string? steamId = ResolvePersistentId(args[2]);
            if (steamId is null)
            {
                response = "Joueur/SteamID64 introuvable.";
                return false;
            }

            string grantedBy = actor is null ? "SERVER" : $"{actor.Nickname} ({JobRuntime.GetPersistentUserId(actor)})";
            bool added = JobWhitelistRepository.Grant(steamId, roleId, grantedBy);
            response = added
                ? $"Whitelist ajoutee et sauvegardee: {steamId} -> {roleId} ({role.Name})."
                : "Ce joueur possede deja cette whitelist.";
            return added;
        }

        if (sub == "remove" || sub == "revoke" || sub == "del")
        {
            if (args.Length < 4 || !int.TryParse(args[3], out int roleId))
            {
                response = "Usage: jobs whitelist remove <playerId|steamId64> <roleId>";
                return false;
            }

            string? steamId = ResolvePersistentId(args[2]);
            if (steamId is null)
            {
                response = "Joueur/SteamID64 introuvable.";
                return false;
            }

            bool removed = JobWhitelistRepository.Revoke(steamId, roleId);
            response = removed
                ? $"Whitelist retiree et sauvegardee: {steamId} -> {roleId}."
                : "Aucune whitelist correspondante.";
            return removed;
        }

        if (sub == "player")
        {
            if (args.Length < 3)
            {
                response = "Usage: jobs whitelist player <playerId|steamId64>";
                return false;
            }

            string? steamId = ResolvePersistentId(args[2]);
            if (steamId is null)
            {
                response = "Joueur/SteamID64 introuvable.";
                return false;
            }

            IReadOnlyList<WhitelistEntry> entries = JobWhitelistRepository.GetForPlayer(steamId);
            response = entries.Count == 0
                ? $"{steamId}: aucune whitelist."
                : string.Join("\n", entries.Select(x => $"{x.RoleId} - {JobCatalog.Find(x.RoleId)?.Name ?? "role inconnu"} | par {x.GrantedBy}"));
            return true;
        }

        if (sub == "role")
        {
            if (args.Length < 3 || !int.TryParse(args[2], out int roleId))
            {
                response = "Usage: jobs whitelist role <roleId>";
                return false;
            }

            IReadOnlyList<WhitelistEntry> entries = JobWhitelistRepository.GetForRole(roleId);
            response = entries.Count == 0
                ? $"Role {roleId}: whitelist vide."
                : string.Join("\n", entries.Select(x => $"{x.SteamId64} | par {x.GrantedBy} | {x.GrantedAtUtc}"));
            return true;
        }

        response = WhitelistHelp();
        return false;
    }

    private static bool RequirePlayer(Player? player, out string response)
    {
        if (player is not null)
        {
            response = string.Empty;
            return true;
        }

        response = "Cette commande doit etre executee par un joueur.";
        return false;
    }

    private static bool HasManagementAccess(Player? player)
    {
        if (player is null)
            return true;

        try
        {
            return player.RemoteAdminAccess || player.HasPermission("siterp.jobs.manage");
        }
        catch
        {
            return player.RemoteAdminAccess;
        }
    }

    private static string? ResolvePersistentId(string input)
    {
        if (int.TryParse(input, out int playerId))
        {
            Player? player = Player.Get(playerId);
            return player is null ? null : JobRuntime.GetPersistentUserId(player);
        }

        Player? byUserId = Player.Get(input);
        if (byUserId is not null)
            return JobRuntime.GetPersistentUserId(byUserId);

        string cleaned = input.Trim();
        int providerIndex = cleaned.IndexOf('@');
        if (providerIndex > 0)
            cleaned = cleaned.Substring(0, providerIndex);

        return cleaned.Length >= 16 && cleaned.All(char.IsDigit) ? cleaned : null;
    }

    private static string Help() =>
        "SiteRP Jobs HUD:\n" +
        "jobs = ouvrir le HUD\n" +
        "jobs prev | next\n" +
        "jobs catprev | catnext\n" +
        "jobs select\n" +
        "jobs refresh\n" +
        "jobs close\n" +
        "jobs native = fallback souris M -> Server Specific Settings\n" +
        "jobs list\n" +
        "jobs join <roleId>\n" +
        "STAFF: jobs whitelist add/remove/player/role ...\n" +
        "STAFF: jobs reload";

    private static string WhitelistHelp() =>
        "Whitelist SiteRP: gestion ingame via M -> Server Specific Settings -> WHITELISTS STAFF, ou:\n" +
        "jobs whitelist add <playerId|steamId64> <roleId>\n" +
        "jobs whitelist remove <playerId|steamId64> <roleId>\n" +
        "jobs whitelist player <playerId|steamId64>\n" +
        "jobs whitelist role <roleId>\n" +
        "Les changements sont sauvegardes immediatement.";
}
