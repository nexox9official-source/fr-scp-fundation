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
/// Detailed read-only survey for the rooms selected by SiteRpFacilityPlan.
/// It exists specifically to prevent blind placement when building the Site-76-inspired RP facility.
/// </summary>
internal static class SiteRpFacilitySurvey
{
    private const int MaxTransformsPerRoom = 2400;

    private static readonly string[] ReviewKeywords =
    {
        "door", "connector", "spawn", "collider", "locker", "pickup", "workstation", "camera",
        "light", "panel", "screen", "console", "desk", "chair", "table", "shelf", "cabinet",
        "blood", "ragdoll", "corpse", "body", "dead", "gore", "debris", "broken", "damage"
    };

    public static string LastReportPath { get; private set; } = string.Empty;

    private static string OutputDirectory
    {
        get
        {
            string path = Path.Combine(PathManager.Configs.FullName, Server.Port.ToString(), "SiteRP");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string WriteAudit(int? seed = null)
    {
        try
        {
            HashSet<RoomName> targets = new(SiteRpFacilityPlan.AuditRooms);
            string seedPart = seed.HasValue ? $"_seed-{seed.Value}" : string.Empty;
            string path = Path.Combine(OutputDirectory, $"SiteRP_FacilityAudit_{DateTime.UtcNow:yyyyMMdd_HHmmss}{seedPart}.txt");

            StringBuilder sb = new();
            sb.AppendLine("============================================================");
            sb.AppendLine("SiteRP OPERATIONAL FACILITY AUDIT - READ ONLY");
            sb.AppendLine("============================================================");
            sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
            sb.AppendLine($"Server port: {Server.Port}");
            sb.AppendLine($"Seed: {(seed.HasValue ? seed.Value.ToString() : "unknown/manual")}");
            sb.AppendLine($"Room count: {Room.List.Count}");
            sb.AppendLine();
            sb.AppendLine(SiteRpFacilityPlan.Describe());
            sb.AppendLine();
            sb.AppendLine("No vanilla object is changed by this audit.");
            sb.AppendLine("Coordinates are room-local so future modules survive facility rotation/seed changes.");

            foreach (Room room in Room.List.Where(x => targets.Contains(x.Name)).OrderBy(x => x.Zone).ThenBy(x => x.Name))
            {
                sb.AppendLine();
                sb.AppendLine("------------------------------------------------------------");
                sb.AppendLine($"ROOM {room.Zone}_{room.Shape}_{room.Name}");
                sb.AppendLine($"worldPos={V(room.Position)} worldRot={V(room.Rotation.eulerAngles)} doors={room.Doors.Count()}");
                sb.AppendLine("------------------------------------------------------------");

                Transform[] transforms = room.GameObject.GetComponentsInChildren<Transform>(true);
                sb.AppendLine($"Transform count: {transforms.Length} (cap {MaxTransformsPerRoom})");

                int count = 0;
                foreach (Transform transform in transforms)
                {
                    if (count++ >= MaxTransformsPerRoom)
                    {
                        sb.AppendLine($"... TRUNCATED after {MaxTransformsPerRoom} transforms ...");
                        break;
                    }

                    string name = transform.gameObject.name ?? "<unnamed>";
                    bool review = ReviewKeywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                    string flag = review ? " [REVIEW]" : string.Empty;
                    Vector3 localPos = room.Transform.InverseTransformPoint(transform.position);
                    Quaternion localRotation = Quaternion.Inverse(room.Rotation) * transform.rotation;
                    Component[] components = transform.gameObject.GetComponents<Component>();
                    string componentNames = string.Join(",", components.Where(c => c != null).Select(c => c.GetType().Name).Distinct().Take(14));

                    sb.AppendLine($"{flag} path={RelativePath(room.Transform, transform)} | active={transform.gameObject.activeSelf} | localPos={V(localPos)} | localRot={V(localRotation.eulerAngles)} | scale={V(transform.lossyScale)} | components=[{componentNames}]");
                }
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            LastReportPath = path;
            LabLogger.Info($"[SiteRP.Facility] Audit generated: {path}");
            return path;
        }
        catch (Exception e)
        {
            string error = $"Facility audit failed: {e.GetType().Name}: {e.Message}";
            LabLogger.Error($"[SiteRP.Facility] {error}\n{e}");
            return error;
        }
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
}
