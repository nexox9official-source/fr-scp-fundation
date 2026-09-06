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
    public string[] Aliases => new[] { "srjob", "jobsrp" };
    public string Description => "SiteRP Jobs: rejoindre un metier et gerer les whitelists persistantes.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string[] args = arguments.ToArray();
        if (args.Length == 0)
        {
            response = Help();
            return true;
        }

        Player? actor = Player.Get(sender);
        string action = args[0].ToLowerInvariant();

        if (action == "join")
        {
            if (actor is null)
            {
                response = "La commande join doit etre executee par un joueur.";
                return false;
            }

            if (args.Length < 2 || !int.TryParse(args[1], out int roleId))
            {
                response = "Usage: siterpjob join <roleId>";
                return false;
            }

            return JobRuntime.TryJoin(actor, roleId, out response);
        }

        if (action == "list")
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
            response = "SiteRP Jobs recharge : whitelist + menu M actualises.";
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
                response = "Usage: siterpjob whitelist add <playerId|steamId64> <roleId>";
                return false;
            }

            if (JobCatalog.Find(roleId) is null)
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
                ? $"Whitelist ajoutee et sauvegardee: {steamId} -> {roleId} ({JobCatalog.Find(roleId)!.Name})."
                : "Ce joueur possede deja cette whitelist.";
            return added;
        }

        if (sub == "remove" || sub == "revoke" || sub == "del")
        {
            if (args.Length < 4 || !int.TryParse(args[3], out int roleId))
            {
                response = "Usage: siterpjob whitelist remove <playerId|steamId64> <roleId>";
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
                response = "Usage: siterpjob whitelist player <playerId|steamId64>";
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
                response = "Usage: siterpjob whitelist role <roleId>";
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

    private static bool HasManagementAccess(Player? player)
    {
        if (player is null)
            return true; // server console

        return player.RemoteAdminAccess || player.HasPermission("siterp.jobs.manage");
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
        "SiteRP Jobs:\n" +
        "siterpjob list\n" +
        "siterpjob join <roleId>\n" +
        "siterpjob whitelist add <playerId|steamId64> <roleId>\n" +
        "siterpjob whitelist remove <playerId|steamId64> <roleId>\n" +
        "siterpjob whitelist player <playerId|steamId64>\n" +
        "siterpjob whitelist role <roleId>\n" +
        "siterpjob reload";

    private static string WhitelistHelp() =>
        "Whitelist SiteRP: add, remove, player, role. Les changements sont sauvegardes immediatement.";
}
