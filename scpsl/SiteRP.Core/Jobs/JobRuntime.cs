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

    public static bool CanJoin(Player player, JobDefinition job, out string reason)
    {
        reason = string.Empty;
        if (player == null)
        {
            reason = "Joueur introuvable.";
            return false;
        }

        if (SiteRpUcrBridge.TryGetActiveRoleId(player, out int currentRole) && currentRole == job.UcrRoleId)
        {
            reason = "Tu occupes deja ce metier.";
            return false;
        }

        int cooldown = GetRemainingCooldown(player);
        if (cooldown > 0)
        {
            reason = $"Changement de metier en cooldown : encore {cooldown}s.";
            return false;
        }

        if (job.AccessMode == JobAccessMode.StaffOnly && !IsStaff(player))
        {
            reason = "Acces STAFF uniquement.";
            return false;
        }

        if (job.AccessMode == JobAccessMode.Whitelist &&
            !IsStaff(player) &&
            !JobWhitelistRepository.IsWhitelisted(GetPersistentUserId(player), job.UcrRoleId))
        {
            reason = "Acces reserve : tu n'es pas whitelist pour ce metier.";
            return false;
        }

        if (job.MaxPlayers > 0)
        {
            int occupied = CountPlayersOnRole(job.UcrRoleId);
            if (occupied >= job.MaxPlayers)
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

        if (!IsStaff(player))
            LastJobChanges[GetPersistentUserId(player)] = DateTime.UtcNow;

        string skin = string.IsNullOrWhiteSpace(job.WardrobeName) ? "apparence standard" : job.WardrobeName;
        response = $"Metier attribue: {job.Name}. Tenue: {skin}. Cooldown: {JobChangeCooldownSeconds}s.";
        return true;
    }

    public static void CleanupPlayer(Player player)
    {
        // Deliberately keep the timestamp for the current server session so reconnecting
        // cannot be used to bypass the RP job cooldown.
    }
}
