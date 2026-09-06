using System;
using System.Linq;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Adds the plugin permissions SiteRP administrators need without overwriting the host's
/// existing LabAPI permissions.yml. The normal LabAPI provider still remains active.
/// </summary>
public sealed class SiteRpPermissionProvider : IPermissionsProvider
{
    private static readonly string[] StaffPermissions =
    {
        "siterp.jobs.manage",
        "siterp.jobs.override",
        "slwardrobe.use",
        "slwardrobe.admin",
    };

    public string[] GetPermissions(Player player) => IsPrivileged(player) ? StaffPermissions.ToArray() : Array.Empty<string>();

    public bool HasPermissions(Player player, params string[] permissions) =>
        permissions.All(permission => HasPermission(player, permission));

    public bool HasAnyPermission(Player player, params string[] permissions) =>
        permissions.Any(permission => HasPermission(player, permission));

    public bool HasPermission(Player player, string specificPermission)
    {
        if (!IsPrivileged(player))
            return false;

        if (specificPermission == ".*")
            return false;

        return StaffPermissions.Any(x => string.Equals(x, specificPermission, StringComparison.OrdinalIgnoreCase))
            || string.Equals(specificPermission, "slwardrobe.*", StringComparison.OrdinalIgnoreCase)
            || string.Equals(specificPermission, "siterp.jobs.*", StringComparison.OrdinalIgnoreCase);
    }

    public void AddPermissions(Player player, params string[] permissions)
    {
        // Read-only provider. Persistent custom grants are managed by the normal LabAPI provider.
    }

    public void RemovePermissions(Player player, params string[] permissions)
    {
        // Read-only provider.
    }

    public void ReloadPermissions()
    {
    }

    private static bool IsPrivileged(Player player)
    {
        if (player is null)
            return false;

        if (player.RemoteAdminAccess)
            return true;

        string group = player.PermissionsGroupName ?? string.Empty;
        return group.Equals("owner", StringComparison.OrdinalIgnoreCase)
            || group.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || group.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
    }
}
