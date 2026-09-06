using System;
using System.Linq;
using System.Reflection;
using LabApi.Features.Wrappers;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

/// <summary>
/// Optional bridge to UncomplicatedCustomRoles without adding a hard assembly reference.
/// This keeps SiteRP.Core bootable even if UCR is temporarily missing while preserving
/// exact custom-role IDs when entering/leaving staff mode and when selecting RP jobs.
/// </summary>
internal static class SiteRpUcrBridge
{
    public const int StaffRoleId = 1999;

    public static int? GetCurrentRoleId(Player player)
    {
        try
        {
            Type? summonedType = FindType("UncomplicatedCustomRoles.API.Features.SummonedCustomRole");
            if (summonedType is null)
                return null;

            MethodInfo? tryGet = summonedType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "TryGet" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(Player));

            if (tryGet is null)
                return null;

            object?[] args = { player, null };
            if (tryGet.Invoke(null, args) is not bool ok || !ok || args[1] is null)
                return null;

            object summoned = args[1]!;
            object? role = summonedType.GetProperty("Role", BindingFlags.Public | BindingFlags.Instance)?.GetValue(summoned);
            if (role is null)
                return null;

            PropertyInfo? idProperty = role.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            return idProperty?.GetValue(role) is int id ? id : null;
        }
        catch (Exception e)
        {
            LabLogger.Warn($"[SiteRP.UCR] Impossible de lire le role UCR actuel: {e.Message}");
            return null;
        }
    }

    public static bool TryGetActiveRoleId(Player player, out int roleId)
    {
        int? current = GetCurrentRoleId(player);
        roleId = current ?? 0;
        return current.HasValue;
    }

    public static bool TrySpawnRole(Player player, int roleId) => TrySpawnRole(player, roleId, out _);

    public static bool TrySpawnRole(Player player, int roleId, out string error)
    {
        error = string.Empty;
        try
        {
            Type? managerType = FindType("UncomplicatedCustomRoles.Manager.SpawnManager");
            if (managerType is null)
            {
                error = "UncomplicatedCustomRoles n'est pas charge.";
                return false;
            }

            MethodInfo? clear = managerType.GetMethod("ClearCustomTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo? summon = managerType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "SummonCustomSubclass" && m.GetParameters().Length == 3);

            if (summon is null)
            {
                error = "API SpawnManager.SummonCustomSubclass introuvable.";
                return false;
            }

            // Remove the previous cosmetic before changing role so a player never keeps an
            // Alpha/MTF helmet when switching back to a civilian/scientist role.
            SiteRpSkinBridge.RemoveSuit(player);
            clear?.Invoke(null, new object[] { player });
            summon.Invoke(null, new object[] { player, roleId, true });

            // Direct LabAPI bridge. This intentionally bypasses UCR's current Wardrobe
            // integration bug on mixed LabAPI + EXILED Loader servers.
            SiteRpSkinBridge.ApplyForRole(player, roleId);
            return true;
        }
        catch (Exception e)
        {
            error = e.GetBaseException().Message;
            LabLogger.Warn($"[SiteRP.UCR] Impossible d'attribuer le role UCR {roleId}: {error}");
            return false;
        }
    }

    public static void ClearCustomRole(Player player)
    {
        try
        {
            SiteRpSkinBridge.RemoveSuit(player);
            Type? managerType = FindType("UncomplicatedCustomRoles.Manager.SpawnManager");
            MethodInfo? clear = managerType?.GetMethod("ClearCustomTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            clear?.Invoke(null, new object[] { player });
        }
        catch (Exception e)
        {
            LabLogger.Warn($"[SiteRP.UCR] Impossible de retirer le role UCR: {e.GetBaseException().Message}");
        }
    }

    private static Type? FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? type = assembly.GetType(fullName, false);
            if (type is not null)
                return type;
        }
        return null;
    }
}
