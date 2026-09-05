using System;
using System.Collections.Generic;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using PlayerRoles;
using UnityEngine;

namespace SiteRP.Core;

public sealed class SiteRpCorePlugin : Plugin
{
    public override string Name => "SiteRP.Core";
    public override string Description => "Persistent SCP:SL roleplay core: permanent round, containment controls and staff mode.";
    public override string Author => "SiteRP";
    public override Version Version => new(0, 1, 0);
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    internal static SiteRpCorePlugin? Instance { get; private set; }
    internal SiteRpEvents Events { get; } = new();

    public static bool PermanentRoundEnabled { get; set; } = true;
    public static bool ContainmentLocked { get; set; } = true;
    public static bool BlockAutomaticWaves { get; set; } = true;
    public static bool BlockDecontamination { get; set; } = true;
    public static bool BlockWarhead { get; set; } = true;
    public static bool BlockEscapes { get; set; } = true;

    internal static Dictionary<string, StaffSnapshot> StaffSnapshots { get; } = new(StringComparer.OrdinalIgnoreCase);

    public override void Enable()
    {
        Instance = this;
        CustomHandlersManager.RegisterEventsHandler(Events);

        if (PermanentRoundEnabled)
            Round.IsLocked = true;

        Logger.Info("[SiteRP.Core] v0.1.0 active - RP permanent enabled.");
    }

    public override void Disable()
    {
        CustomHandlersManager.UnregisterEventsHandler(Events);

        foreach (Player player in Player.ReadyList)
        {
            if (StaffSnapshots.TryGetValue(player.UserId, out StaffSnapshot snapshot))
                RestoreStaffSnapshotImmediate(player, snapshot);
        }

        StaffSnapshots.Clear();
        Round.IsLocked = false;
        Instance = null;
        Logger.Info("[SiteRP.Core] disabled.");
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
            OriginalPosition = player.Position,
            OriginalGroupName = player.GroupName,
            OriginalGroupColor = player.GroupColor,
            OriginalCustomInfo = player.CustomInfo,
            OriginalDisplayName = player.DisplayName,
            OriginalGodMode = player.IsGodModeEnabled,
            OriginalNoclip = player.IsNoclipEnabled,
        };

        StaffSnapshots[player.UserId] = snapshot;
        player.SetRole(RoleTypeId.Tutorial);

        response = "Mode staff active.";
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
        player.SetRole(snapshot.OriginalRole);

        response = "Mode staff desactive. Retour a ton role RP.";
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

        if (player.Role == RoleTypeId.Tutorial)
            ApplyStaffAppearance(player, snapshot.OriginalPosition);
    }

    internal static void ApplyStaffAppearance(Player player, Vector3 position)
    {
        player.GroupName = "STAFF";
        player.GroupColor = "red";
        player.CustomInfo = "STAFF | MODERATION";
        player.IsGodModeEnabled = true;
        player.IsNoclipEnabled = true;
        player.Position = position;
        player.SendBroadcast("<b>MODE STAFF</b>\nTu es hors RP. Retape .staff pour revenir.", 6);
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
    public RoleTypeId OriginalRole { get; init; }
    public Vector3 OriginalPosition { get; init; }
    public string OriginalGroupName { get; init; } = string.Empty;
    public string OriginalGroupColor { get; init; } = string.Empty;
    public string OriginalCustomInfo { get; init; } = string.Empty;
    public string OriginalDisplayName { get; init; } = string.Empty;
    public bool OriginalGodMode { get; init; }
    public bool OriginalNoclip { get; init; }
    public bool Restoring { get; set; }
}
