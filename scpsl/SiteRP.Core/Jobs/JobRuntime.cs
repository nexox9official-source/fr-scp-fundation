using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace SiteRP.Core.Jobs;

public static class JobRuntime
{
    public static bool IsStaff(Player player)
    {
        if (player == null)
            return false;

        try
        {
            if (player.HasPermission("siterp.jobs.manage"))
                return true;
        }
        catch
        {
            // Fall back to RA group names if permissions are not yet configured.
        }

        string group = player.PermissionsGroupName ?? string.Empty;
        return group.Equals("owner", StringComparison.OrdinalIgnoreCase)
            || group.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || group.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
    }

    public static int CountPlayersOnRole(int roleId)
    {
        int count = 0;
        foreach (Player player in Player.List)
        {
            if (SiteRpUcrBridge.TryGetActiveRoleId(player, out int current) && current == roleId)
                count++;
        }
        return count;
    }

    public static bool CanJoin(Player player, JobDefinition job, out string reason)
    {
        reason = string.Empty;
        if (player == null)
        {
            reason = "Joueur introuvable.";
            return false;
        }

        if (job.AccessMode == JobAccessMode.StaffOnly && !IsStaff(player))
        {
            reason = "Acces STAFF uniquement.";
            return false;
        }

        if (job.AccessMode == JobAccessMode.Whitelist && !IsStaff(player) && !JobWhitelistRepository.IsWhitelisted(player.UserId, job.UcrRoleId))
        {
            reason = "Role reserve : whitelist requise.";
            return false;
        }

        if (job.MaxPlayers > 0)
        {
            int occupied = CountPlayersOnRole(job.UcrRoleId);
            bool alreadyOnRole = SiteRpUcrBridge.TryGetActiveRoleId(player, out int current) && current == job.UcrRoleId;
            if (!alreadyOnRole && occupied >= job.MaxPlayers)
            {
                reason = $"Role complet ({occupied}/{job.MaxPlayers}).";
                return false;
            }
        }

        return true;
    }

    public static bool TryJoin(Player player, int roleId, out string response)
    {
        JobDefinition? job = JobCatalog.Find(roleId);
        if (job == null)
        {
            response = $"Role SiteRP inconnu: {roleId}.";
            return false;
        }

        if (!CanJoin(player, job, out response))
            return false;

        if (!SiteRpUcrBridge.TrySpawnRole(player, roleId, out string error))
        {
            response = string.IsNullOrWhiteSpace(error) ? "UCR n'a pas pu attribuer le metier." : error;
            return false;
        }

        response = $"Metier attribue: {job.Name}.";
        return true;
    }
}
