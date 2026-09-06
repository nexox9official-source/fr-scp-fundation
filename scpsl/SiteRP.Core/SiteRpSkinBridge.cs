using System;
using System.Linq;
using System.Reflection;
using LabApi.Features.Wrappers;
using SiteRP.Core.Jobs;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

/// <summary>
/// Applies SLWardrobe suits directly with the LabAPI Player wrapper.
/// UCR 9.6.0 can currently mix an EXILED Player instance into its Wardrobe integration
/// when EXILED Loader is also present. SiteRP bypasses that integration and calls the
/// LabAPI SLWardrobe build directly, while remaining optional/reflection-based.
/// </summary>
internal static class SiteRpSkinBridge
{
    public static void ApplyForRole(Player player, int roleId)
    {
        string suitName = JobCatalog.Find(roleId)?.WardrobeName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(suitName))
        {
            RemoveSuit(player);
            return;
        }

        if (!ApplySuit(player, suitName, out string error))
            LabLogger.Warn($"[SiteRP.Skins] Impossible d'appliquer {suitName} au role {roleId}: {error}");
    }

    public static bool ApplySuit(Player player, string suitName, out string error)
    {
        error = string.Empty;
        try
        {
            Type? wardrobeType = FindType("SLWardrobe.SLWardrobe");
            if (wardrobeType is null)
            {
                error = "SLWardrobe LabAPI n'est pas charge.";
                return false;
            }

            object? instance = wardrobeType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (instance is null)
            {
                error = "SLWardrobe.Instance indisponible.";
                return false;
            }

            MethodInfo? apply = wardrobeType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "ApplySuit" &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(Player) &&
                    m.GetParameters()[1].ParameterType == typeof(string));

            if (apply is null)
            {
                error = "SLWardrobe.ApplySuit(LabApi Player, string) introuvable.";
                return false;
            }

            apply.Invoke(instance, new object[] { player, suitName });
            LabLogger.Debug($"[SiteRP.Skins] Morph demande: {suitName} -> {player.Nickname}.");
            return true;
        }
        catch (Exception e)
        {
            error = e.GetBaseException().Message;
            return false;
        }
    }

    public static void RemoveSuit(Player player)
    {
        try
        {
            Type? binderType = FindType("SLWardrobe.SuitBinder");
            MethodInfo? remove = binderType?
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "RemoveSuit" &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(Player));

            MethodInfo? visibility = binderType?
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "SetPlayerInvisibility" &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(Player) &&
                    m.GetParameters()[1].ParameterType == typeof(bool));

            remove?.Invoke(null, new object[] { player });

            // Full SiteRP morphs use make_wearer_invisible=true. SLWardrobe.RemoveSuit only
            // destroys suit objects and does not itself remove the Fade effect, so always
            // restore the native player visibility when SiteRP removes/switches a morph.
            visibility?.Invoke(null, new object[] { player, false });
        }
        catch (Exception e)
        {
            LabLogger.Debug($"[SiteRP.Skins] Retrait morph ignore: {e.GetBaseException().Message}");
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
