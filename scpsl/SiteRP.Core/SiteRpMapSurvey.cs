using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using MapGeneration;
using UnityEngine;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

/// <summary>
/// Runtime survey utility used to redesign the vanilla facility without guessing coordinates.
/// It never changes room geometry. It only records the live generated map and admin markers.
/// </summary>
internal static class SiteRpMapSurvey
{
    private const int MaxTransformsPerRoom = 1800;

    private static readonly HashSet<RoomName> TargetRooms = new()
    {
        RoomName.EzEvacShelter,
        RoomName.EzCollapsedTunnel,
        RoomName.Hcz049,
        RoomName.Hcz127,
        RoomName.HczWarhead,
        RoomName.Lcz173,
    };

    private static readonly string[] SuspiciousKeywords =
    {
        "blood", "ragdoll", "corpse", "body", "dead", "gore", "debris", "broken",
        "destroy", "collapse", "damage", "wreck", "ruin", "gas", "smoke", "flesh",
        "bone", "arm", "leg", "head", "guard", "scientist"
    };

    public static string LastReportPath { get; private set; } = string.Empty;

    public static string TargetRoomNames => string.Join(", ", TargetRooms.OrderBy(x => x.ToString()).Select(x => x.ToString()));

    private static string OutputDirectory
    {
        get
        {
            string path = Path.Combine(PathManager.Configs.FullName, Server.Port.ToString(), "SiteRP");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string MarkersPath => Path.Combine(OutputDirectory, "SiteRP_MapMarkers.txt");

    public static string WriteAudit(int? seed = null)
    {
        try
        {
            string seedPart = seed.HasValue ? $"_seed-{seed.Value}" : string.Empty;
            string fileName = $"SiteRP_MapAudit_{DateTime.UtcNow:yyyyMMdd_HHmmss}{seedPart}.txt";
            string path = Path.Combine(OutputDirectory, fileName);

            StringBuilder sb = new();
            sb.AppendLine("============================================================");
            sb.AppendLine("SiteRP MAP AUDIT - READ ONLY");
            sb.AppendLine("============================================================");
            sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
            sb.AppendLine($"Server port: {Server.Port}");
            sb.AppendLine($"Seed: {(seed.HasValue ? seed.Value.ToString() : "unknown/manual")}");
            sb.AppendLine($"Room count: {Room.List.Count}");
            sb.AppendLine($"Target rooms: {TargetRoomNames}");
            sb.AppendLine();
            sb.AppendLine("This file is intentionally generated BEFORE SiteRP decorative-ragdoll cleanup.");
            sb.AppendLine("No geometry or vanilla object is modified by the audit.");
            sb.AppendLine();

            sb.AppendLine("==================== FACILITY ROOM SUMMARY ====================");
            foreach (Room room in Room.List.OrderBy(r => r.Zone).ThenBy(r => r.Name).ThenBy(r => r.Shape))
            {
                sb.AppendLine(
                    $"{RoomId(room)} | index={GetRoomIndex(room)} | worldPos={V(room.Position)} | worldRot={V(room.Rotation.eulerAngles)} | doors={room.Doors.Count()}");
            }

            sb.AppendLine();
            sb.AppendLine("==================== PRE-EXISTING RAGDOLLS ====================");
            Ragdoll[] ragdolls = Ragdoll.List.ToArray();
            sb.AppendLine($"Count before SiteRP cleanup: {ragdolls.Length}");
            foreach (Ragdoll ragdoll in ragdolls)
            {
                Room? room = Room.GetRoomAtPosition(ragdoll.Position);
                string roomInfo = room is null ? "room=unknown" : $"room={RoomId(room)} index={GetRoomIndex(room)} localPos={V(room.Transform.InverseTransformPoint(ragdoll.Position))}";
                sb.AppendLine($"role={ragdoll.Role} nickname={Safe(ragdoll.Nickname)} worldPos={V(ragdoll.Position)} {roomInfo}");
            }

            sb.AppendLine();
            sb.AppendLine("==================== TARGET ROOM HIERARCHIES ====================");
            foreach (Room room in Room.List.Where(r => TargetRooms.Contains(r.Name)).OrderBy(r => r.Name))
            {
                WriteRoomDetail(sb, room);
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            LastReportPath = path;
            LabLogger.Info($"[SiteRP.Map] Audit generated: {path}");
            return path;
        }
        catch (Exception e)
        {
            string error = $"Map audit failed: {e.GetType().Name}: {e.Message}";
            LabLogger.Error($"[SiteRP.Map] {error}\n{e}");
            return error;
        }
    }

    public static string DescribePosition(Player player)
    {
        Room? room = player.Room ?? Room.GetRoomAtPosition(player.Position);
        if (room is null)
            return $"Aucune salle detectee. worldPos={V(player.Position)} worldRot={V(player.Rotation.eulerAngles)}";

        Vector3 localPos = room.Transform.InverseTransformPoint(player.Position);
        Quaternion localRotQ = Quaternion.Inverse(room.Rotation) * player.Rotation;
        Vector3 localRot = localRotQ.eulerAngles;

        return $"room={RoomId(room)} | index={GetRoomIndex(room)} | localPos={V(localPos)} | localRot={V(localRot)} | worldPos={V(player.Position)}";
    }

    public static string AppendMarker(Player player, string label)
    {
        Room? room = player.Room ?? Room.GetRoomAtPosition(player.Position);
        if (room is null)
            return "Impossible de creer le marqueur: aucune salle detectee.";

        Vector3 localPos = room.Transform.InverseTransformPoint(player.Position);
        Vector3 localRot = (Quaternion.Inverse(room.Rotation) * player.Rotation).eulerAngles;
        string cleanLabel = string.IsNullOrWhiteSpace(label) ? "unnamed" : label.Replace('\r', ' ').Replace('\n', ' ').Trim();

        string line =
            $"{DateTime.UtcNow:O} | {Safe(cleanLabel)} | room={RoomId(room)} | index={GetRoomIndex(room)} | localPos={V(localPos)} | localRot={V(localRot)} | worldPos={V(player.Position)}";

        File.AppendAllText(MarkersPath, line + Environment.NewLine, Encoding.UTF8);
        LabLogger.Info($"[SiteRP.Map] Marker: {line}");
        return $"Marqueur enregistre: {cleanLabel}\n{DescribePosition(player)}\nFichier: {MarkersPath}";
    }

    private static void WriteRoomDetail(StringBuilder sb, Room room)
    {
        sb.AppendLine();
        sb.AppendLine("------------------------------------------------------------");
        sb.AppendLine($"ROOM {RoomId(room)}");
        sb.AppendLine($"index={GetRoomIndex(room)} worldPos={V(room.Position)} worldRot={V(room.Rotation.eulerAngles)} doors={room.Doors.Count()}");
        sb.AppendLine("------------------------------------------------------------");

        Transform[] transforms = room.GameObject.GetComponentsInChildren<Transform>(true);
        sb.AppendLine($"Transform count: {transforms.Length} (report cap {MaxTransformsPerRoom})");

        int count = 0;
        foreach (Transform transform in transforms)
        {
            if (count++ >= MaxTransformsPerRoom)
            {
                sb.AppendLine($"... TRUNCATED after {MaxTransformsPerRoom} transforms ...");
                break;
            }

            string name = transform.gameObject.name ?? "<unnamed>";
            bool suspicious = SuspiciousKeywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
            string flag = suspicious ? " [REVIEW]" : string.Empty;
            Vector3 localToRoom = room.Transform.InverseTransformPoint(transform.position);
            Quaternion localRotation = Quaternion.Inverse(room.Rotation) * transform.rotation;

            Component[] components = transform.gameObject.GetComponents<Component>();
            string componentNames = string.Join(",",
                components
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .Take(12));

            sb.AppendLine(
                $"{flag} path={RelativePath(room.Transform, transform)} | active={transform.gameObject.activeSelf} | localPos={V(localToRoom)} | localRot={V(localRotation.eulerAngles)} | scale={V(transform.lossyScale)} | components=[{componentNames}]");
        }
    }

    private static string RoomId(Room room) => $"{room.Zone}_{room.Shape}_{room.Name}";

    private static int GetRoomIndex(Room room)
    {
        List<Room> sameType = Room.List
            .Where(x => x.Zone == room.Zone && x.Shape == room.Shape && x.Name == room.Name)
            .ToList();
        return sameType.IndexOf(room);
    }

    private static string RelativePath(Transform root, Transform current)
    {
        if (current == root)
            return root.gameObject.name;

        List<string> parts = new();
        Transform? cursor = current;
        int guard = 0;
        while (cursor != null && cursor != root && guard++ < 64)
        {
            parts.Add(cursor.gameObject.name ?? "<unnamed>");
            cursor = cursor.parent;
        }
        parts.Reverse();
        return root.gameObject.name + "/" + string.Join("/", parts);
    }

    private static string V(Vector3 value) => $"({value.x:0.###},{value.y:0.###},{value.z:0.###})";

    private static string Safe(string value) => value.Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
}
