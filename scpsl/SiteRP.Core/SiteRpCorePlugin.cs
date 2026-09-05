using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using PlayerRoles;
using UnityEngine;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

public sealed class SiteRpCorePlugin : Plugin
{
    public override string Name => "SiteRP.Core";
    public override string Description => "Persistent SCP:SL roleplay core: permanent round, containment controls, clean operational facility, RP jobs/UCR bridge, Site-76-inspired facility blueprint, safe map survey and staff mode.";
    public override string Author => "SiteRP";
    public override Version Version => new(0, 6, 0);
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    internal static SiteRpCorePlugin? Instance { get; private set; }
    internal SiteRpEvents Events { get; } = new();

    public static bool PermanentRoundEnabled { get; set; } = true;
    public static bool ContainmentLocked { get; set; } = true;
    public static bool BlockAutomaticWaves { get; set; } = true;
    public static bool BlockDecontamination { get; set; } = true;
    public static bool BlockWarhead { get; set; } = true;
    public static bool BlockEscapes { get; set; } = true;

    // SiteRP Clean: removes network ragdolls already present when the facility is generated.
    // The v0.3 audit proved that the vanilla decorative corpses are static scene geometry,
    // therefore the Operational layer handles them visually without touching vanilla objects.
    public static bool CleanDecorativeRagdolls { get; set; } = true;

    // Keeps the operational-site look by preventing new blood decals during gameplay.
    public static bool BlockBloodDecals { get; set; } = true;

    // Generates the legacy focused map audit when explicitly enabled.
    public static bool AutomaticMapAudit { get; set; } = false;

    // Non-destructive, fully reversible clean-facility overlay based on the v0.3 audit.
    public static bool OperationalMapEnabled { get; set; } = true;

    internal static Dictionary<string, StaffSnapshot> StaffSnapshots { get; } = new(StringComparer.OrdinalIgnoreCase);

    public override void Enable()
    {
        Instance = this;
        CustomHandlersManager.RegisterEventsHandler(Events);

        if (PermanentRoundEnabled)
            Round.IsLocked = true;

        LabLogger.Info("[SiteRP.Core] v0.6.0 active - RP jobs/UCR bridge + STAFF role restore enabled.");
    }

    public override void Disable()
    {
        CustomHandlersManager.UnregisterEventsHandler(Events);
        SiteRpOperationalMap.Remove();

        foreach (Player player in Player.ReadyList)
        {
            if (!StaffSnapshots.TryGetValue(player.UserId, out StaffSnapshot snapshot))
                continue;

            RestoreRoleFromSnapshot(player, snapshot);
            RestoreStaffSnapshotImmediate(player, snapshot);
        }

        StaffSnapshots.Clear();
        Round.IsLocked = false;
        Instance = null;
        LabLogger.Info("[SiteRP.Core] disabled.");
    }

    internal static int CleanupDecorativeRagdolls(string phase)
    {
        if (!CleanDecorativeRagdolls)
            return 0;

        Ragdoll[] ragdolls = Ragdoll.List.ToArray();
        foreach (Ragdoll ragdoll in ragdolls)
        {
            if (!ragdoll.IsDestroyed)
                ragdoll.Destroy();
        }

        LabLogger.Info($"[SiteRP.Clean] {phase}: removed {ragdolls.Length} pre-existing network ragdoll(s). Static vanilla decorations are handled by SiteRP Operational.");
        return ragdolls.Length;
    }

    public static bool IsStaffMode(Player player) =>
        StaffSnapshots.TryGetValue(player.UserId, out StaffSnapshot snapshot) && !snapshot.Restoring;

    public static bool EnterStaffMode(Player player, out string response)
    {
        if (StaffSnapshots.ContainsKey(player.UserId))
        {
            response = "Tu es deja en mode staff.";
            return false;
        }

        StaffSnapshot snapshot = new()
        {
            OriginalRole = player.Role,
            OriginalCustomRoleId = SiteRpUcrBridge.GetCurrentRoleId(player),
            OriginalPosition = player.Position,
            OriginalGroupName = player.GroupName,
            OriginalGroupColor = player.GroupColor,
            OriginalCustomInfo = player.CustomInfo,
            OriginalDisplayName = player.DisplayName,
            OriginalGodMode = player.IsGodModeEnabled,
            OriginalNoclip = player.IsNoclipEnabled,
        };

        StaffSnapshots[player.UserId] = snapshot;

        // Preferred path: the dedicated UCR STAFF role (1999) gives staff a distinct visual identity.
        // Fallback keeps staff mode usable even if UCR is temporarily unavailable/misconfigured.
        if (!SiteRpUcrBridge.TrySpawnRole(player, SiteRpUcrBridge.StaffRoleId))
            player.SetRole(RoleTypeId.Tutorial);

        response = snapshot.OriginalCustomRoleId.HasValue
            ? $"Mode staff active. Role RP UCR {snapshot.OriginalCustomRoleId.Value} sauvegarde."
            : "Mode staff active.";
        return true;
    }

    public static bool ExitStaffMode(Player player, out string response)
    {
        if (!StaffSnapshots.TryGetValue(player.UserId, out StaffSnapshot snapshot))
        {
            response = "Tu n'es pas en mode staff.";
            return false;
        }

        snapshot.Restoring = true;
        RestoreRoleFromSnapshot(player, snapshot);

        response = snapshot.OriginalCustomRoleId.HasValue
            ? $"Mode staff desactive. Retour au role RP UCR {snapshot.OriginalCustomRoleId.Value}."
            : "Mode staff desactive. Retour a ton role RP.";
        return true;
    }

    internal static void OnPlayerRoleChanged(Player player)
    {
        if (!StaffSnapshots.TryGetValue(player.UserId, out StaffSnapshot snapshot))
            return;

        if (snapshot.Restoring)
        {
            RestoreStaffSnapshotImmediate(player, snapshot);
            StaffSnapshots.Remove(player.UserId);
            return;
        }

        // UCR STAFF is based on Tutorial; this also handles the non-UCR fallback path.
        if (player.Role == RoleTypeId.Tutorial)
            ApplyStaffAppearance(player, snapshot.OriginalPosition);
    }

    internal static void ApplyStaffAppearance(Player player, Vector3 position)
    {
        player.GroupName = "STAFF";
        player.GroupColor = "red";
        player.CustomInfo = "STAFF | HORS-RP | MODERATION";
        player.IsGodModeEnabled = true;
        player.IsNoclipEnabled = true;
        player.Position = position;
        player.SendBroadcast("<b><color=red>MODE STAFF</color></b>\nTu es hors RP. Retape .staff pour revenir a ton metier.", 6);
    }

    private static void RestoreRoleFromSnapshot(Player player, StaffSnapshot snapshot)
    {
        if (snapshot.OriginalCustomRoleId.HasValue &&
            SiteRpUcrBridge.TrySpawnRole(player, snapshot.OriginalCustomRoleId.Value))
        {
            return;
        }

        SiteRpUcrBridge.ClearCustomRole(player);
        player.SetRole(snapshot.OriginalRole);
    }

    internal static void RestoreStaffSnapshotImmediate(Player player, StaffSnapshot snapshot)
    {
        player.GroupName = snapshot.OriginalGroupName;
        player.GroupColor = snapshot.OriginalGroupColor;
        player.CustomInfo = snapshot.OriginalCustomInfo;
        player.DisplayName = snapshot.OriginalDisplayName;
        player.IsGodModeEnabled = snapshot.OriginalGodMode;
        player.IsNoclipEnabled = snapshot.OriginalNoclip;
        player.Position = snapshot.OriginalPosition;
    }

    internal static void CleanupPlayer(Player player)
    {
        StaffSnapshots.Remove(player.UserId);
    }
}

internal sealed class StaffSnapshot
{
    public RoleTypeId OriginalRole { get; set; }
    public int? OriginalCustomRoleId { get; set; }
    public Vector3 OriginalPosition { get; set; }
    public string OriginalGroupName { get; set; } = string.Empty;
    public string OriginalGroupColor { get; set; } = string.Empty;
    public string OriginalCustomInfo { get; set; } = string.Empty;
    public string OriginalDisplayName { get; set; } = string.Empty;
    public bool OriginalGodMode { get; set; }
    public bool OriginalNoclip { get; set; }
    public bool Restoring { get; set; }
}
