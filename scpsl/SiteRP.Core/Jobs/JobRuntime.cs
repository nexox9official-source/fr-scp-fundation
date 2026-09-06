using LabApi.Features.Permissions;

namespace SiteRP.Core.Jobs;

public static class JobRuntime
{
    public const int JobChangeCooldownSeconds = 60;
    private static readonly Dictionary<string, DateTime> LastJobChanges = new(StringComparer.OrdinalIgnoreCase);

    public static string GetPersistentUserId(Player player)
    {
        string raw = player.UserId ?? string.Empty;
        int providerIndex = raw.IndexOf('@');
        return providerIndex > 0 ? raw.Substring(0, providerIndex) : raw;
    }

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
            // Fall back to RA access/group names if providers are still initializing.
        }

        if (player.RemoteAdminAccess)
            return true;

        string group = player.PermissionsGroupName ?? string.Empty;
        return group.Equals("owner", StringComparison.OrdinalIgnoreCase)
            || group.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || group.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
    }

    public static int GetRemainingCooldown(Player player)
    {
        if (player == null || IsStaff(player))
            return 0;

        string id = GetPersistentUserId(player);
        if (!LastJobChanges.TryGetValue(id, out DateTime last))
            return 0;

        double remaining = JobChangeCooldownSeconds - (DateTime.UtcNow - last).TotalSeconds;
        return remaining <= 0 ? 0 : (int)Math.Ceiling(remaining);
    }

    public static int CountPlayersOnRole(int roleId)
    {
        int count = 0;
        foreach (Player player in Player.ReadyList)
        {
            if (SiteRpUcrBridge.TryGetActiveRoleId(player, out int current) && current == roleId)
                count++;
        }
        return count;
    }

    public static bool CanJoinIgnoringSameRole(Player player, JobDefinition job, out string reason)
    {
        if (SiteRpUcrBridge.TryGetActiveRoleId(player, out int currentRole) && currentRole == job.UcrRoleId)
        {
            reason = "Métier actuel";
            return false;
        }

        return CanJoin(player, job, out reason, ignoreCooldown: false);
    }

    public static bool CanJoin(Player player, JobDefinition job, out string reason, bool ignoreCooldown = false)
    {
        reason = string.Empty;
        if (player == null)
        {
            reason = "Joueur introuvable.";
            return false;
        }

        if (SiteRpUcrBridge.TryGetActiveRoleId(player, out int currentRole) && currentRole == job.UcrRoleId)
        {
            reason = "Tu occupes déjà ce métier.";
            return false;
        }

        int cooldown = ignoreCooldown ? 0 : GetRemainingCooldown(player);
        if (cooldown > 0)
        {
            reason = $"Cooldown métier : encore {cooldown}s.";
            return false;
        }

        if (job.AccessMode == JobAccessMode.StaffOnly && !IsStaff(player))
        {
            reason = "Accès STAFF uniquement.";
            return false;
        }

        if (job.AccessMode == JobAccessMode.Whitelist &&
            !IsStaff(player) &&
            !JobWhitelistRepository.IsWhitelisted(GetPersistentUserId(player), job.UcrRoleId))
        {
            reason = "Accès réservé : whitelist requise.";
            return false;
        }

        if (job.MaxPlayers > 0)
        {
            int occupied = CountPlayersOnRole(job.UcrRoleId);
            if (occupied >= job.MaxPlayers)
            {
                reason = $"Rôle complet ({occupied}/{job.MaxPlayers}).";
                return false;
            }
        }

        return true;
    }

    public static bool TryJoin(Player player, int roleId, out string response, bool initialDeployment = false)
    {
        JobDefinition? job = JobCatalog.Find(roleId);
        if (job == null)
        {
            response = $"Rôle SiteRP inconnu: {roleId}.";
            return false;
        }

        if (!CanJoin(player, job, out response, ignoreCooldown: initialDeployment))
            return false;

        if (!SiteRpUcrBridge.TrySpawnRole(player, roleId, out string error))
        {
            response = string.IsNullOrWhiteSpace(error) ? "UCR n'a pas pu attribuer le métier." : error;
            return false;
        }

        if (!IsStaff(player))
            LastJobChanges[GetPersistentUserId(player)] = DateTime.UtcNow;

        string skin = string.IsNullOrWhiteSpace(job.WardrobeName) ? "apparence standard" : job.WardrobeName;
        response = $"Métier attribué: {job.Name}. Tenue: {skin}.";
        return true;
    }

    public static void CleanupPlayer(Player player)
    {
        // Deliberately keep the timestamp for the current server session so reconnecting
        // cannot be used to bypass the RP job cooldown.
    }
}
