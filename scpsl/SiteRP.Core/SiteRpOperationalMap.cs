using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration;
using UnityEngine;
using AdminToy = LabApi.Features.Wrappers.AdminToy;
using PrimitiveObjectToy = LabApi.Features.Wrappers.PrimitiveObjectToy;
using PrimitiveFlags = AdminToys.PrimitiveFlags;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

/// <summary>
/// Non-destructive operational-site overlay built from the real v0.3 map audit.
/// It NEVER destroys, disables or moves vanilla geometry. Every object created here is
/// a networked AdminToy tracked by this class and can be removed instantly.
/// </summary>
internal static class SiteRpOperationalMap
{
    private static readonly List<AdminToy> Spawned = new();

    private static readonly Color Wall = new(0.42f, 0.45f, 0.47f, 1f);
    private static readonly Color WallLight = new(0.60f, 0.63f, 0.64f, 1f);
    private static readonly Color Dark = new(0.08f, 0.10f, 0.12f, 1f);
    private static readonly Color FoundationBlue = new(0.10f, 0.25f, 0.38f, 1f);
    private static readonly Color Floor = new(0.24f, 0.27f, 0.29f, 1f);

    public static bool IsApplied => Spawned.Any(x => x is not null && !x.IsDestroyed);

    public static int SpawnedCount => Spawned.Count(x => x is not null && !x.IsDestroyed);

    public static string Apply()
    {
        Remove();

        try
        {
            int rooms = 0;

            foreach (Room room in Room.List.Where(x => x.Name == RoomName.EzCollapsedTunnel))
            {
                BuildCollapsedTunnelSeal(room);
                rooms++;
            }

            foreach (Room room in Room.List.Where(x => x.Name == RoomName.EzEvacShelter))
            {
                BuildEvacShelterFront(room);
                rooms++;
            }

            foreach (Room room in Room.List.Where(x => x.Name == RoomName.Hcz127))
            {
                Build127Cleanup(room);
                rooms++;
            }

            foreach (Room room in Room.List.Where(x => x.Name == RoomName.Hcz049))
            {
                Build049Cleanup(room);
                rooms++;
            }

            foreach (Room room in Room.List.Where(x => x.Name == RoomName.HczWarhead))
            {
                BuildWarheadCleanup(room);
                rooms++;
            }

            string result = $"SiteRP Operational applique: {rooms} salle(s), {SpawnedCount} element(s) reversibles.";
            LabLogger.Info($"[SiteRP.Map] {result}");
            return result;
        }
        catch (Exception e)
        {
            string error = $"SiteRP Operational erreur: {e.GetType().Name}: {e.Message}";
            LabLogger.Error($"[SiteRP.Map] {error}\n{e}");
            Remove();
            return error;
        }
    }

    public static string Remove()
    {
        int removed = 0;
        foreach (AdminToy toy in Spawned.ToArray())
        {
            try
            {
                if (toy is not null && !toy.IsDestroyed)
                {
                    toy.Destroy();
                    removed++;
                }
            }
            catch (Exception e)
            {
                LabLogger.Warn($"[SiteRP.Map] Impossible de retirer un element Operational: {e.Message}");
            }
        }

        Spawned.Clear();
        if (removed > 0)
            LabLogger.Info($"[SiteRP.Map] SiteRP Operational retire: {removed} element(s). La map vanilla n'a pas ete modifiee.");

        return $"SiteRP Operational retire: {removed} element(s).";
    }

    public static string Reload()
    {
        Remove();
        return Apply();
    }

    private static void BuildCollapsedTunnelSeal(Room room)
    {
        AddBox(room, new Vector3(0f, 1.70f, 3.42f), Vector3.zero, new Vector3(3.48f, 3.42f, 0.18f), Wall, true);
        AddBox(room, new Vector3(0f, 1.70f, 3.31f), Vector3.zero, new Vector3(2.15f, 2.55f, 0.07f), Dark, false);
        AddBox(room, new Vector3(0f, 2.84f, 3.25f), Vector3.zero, new Vector3(3.15f, 0.20f, 0.06f), FoundationBlue, false);
        AddBox(room, new Vector3(-1.43f, 1.70f, 3.24f), Vector3.zero, new Vector3(0.12f, 2.55f, 0.06f), FoundationBlue, false);
        AddBox(room, new Vector3(1.43f, 1.70f, 3.24f), Vector3.zero, new Vector3(0.12f, 2.55f, 0.06f), FoundationBlue, false);
    }

    private static void BuildEvacShelterFront(Room room)
    {
        AddBox(room, new Vector3(0f, 2.18f, 3.10f), Vector3.zero, new Vector3(6.20f, 4.36f, 0.18f), WallLight, true);
        AddBox(room, new Vector3(0f, 0.55f, 3.00f), Vector3.zero, new Vector3(6.02f, 0.62f, 0.08f), Dark, false);
        AddBox(room, new Vector3(0f, 3.35f, 2.99f), Vector3.zero, new Vector3(5.95f, 0.22f, 0.08f), FoundationBlue, false);
        AddBench(room, new Vector3(-2.52f, 0.36f, 5.15f), 0f);
        AddBench(room, new Vector3(2.52f, 0.36f, 5.15f), 0f);
        AddCabinet(room, new Vector3(-2.35f, 1.05f, 3.55f));
        AddCabinet(room, new Vector3(2.35f, 1.05f, 3.55f));
    }

    private static void Build127Cleanup(Room room)
    {
        AddBox(room, new Vector3(-2.55f, 0.035f, 0.05f), Vector3.zero, new Vector3(4.25f, 0.07f, 5.05f), Floor, true);
        AddBox(room, new Vector3(-3.55f, 0.72f, 0.35f), Vector3.zero, new Vector3(1.25f, 1.45f, 2.70f), Wall, true);
        AddBox(room, new Vector3(-3.55f, 1.46f, 0.35f), Vector3.zero, new Vector3(1.27f, 0.08f, 2.72f), Dark, false);
        AddBox(room, new Vector3(-2.91f, 0.78f, 0.35f), Vector3.zero, new Vector3(0.05f, 1.15f, 2.45f), FoundationBlue, false);
    }

    private static void Build049Cleanup(Room room)
    {
        AddBox(room, new Vector3(-1.95f, 89.10f, -1.65f), Vector3.zero, new Vector3(1.45f, 0.10f, 1.55f), Floor, true);
        AddBox(room, new Vector3(-1.95f, 89.92f, -1.72f), Vector3.zero, new Vector3(1.25f, 1.55f, 0.62f), Wall, true);
        AddBox(room, new Vector3(-1.95f, 90.35f, -1.39f), Vector3.zero, new Vector3(0.95f, 0.16f, 0.05f), FoundationBlue, false);
        AddBox(room, new Vector3(2.12f, 89.55f, 3.14f), Vector3.zero, new Vector3(1.55f, 1.02f, 2.35f), Wall, true);
        AddBox(room, new Vector3(2.12f, 90.10f, 3.14f), Vector3.zero, new Vector3(1.58f, 0.09f, 2.38f), Dark, false);
        AddBox(room, new Vector3(18.70f, 94.05f, 12.84f), Vector3.zero, new Vector3(2.10f, 1.25f, 1.25f), Wall, true);
        AddBox(room, new Vector3(18.70f, 94.72f, 12.84f), Vector3.zero, new Vector3(2.12f, 0.09f, 1.27f), FoundationBlue, false);
    }

    private static void BuildWarheadCleanup(Room room)
    {
        AddBox(room, new Vector3(32.55f, -70.37f, -9.00f), Vector3.zero, new Vector3(3.35f, 0.06f, 2.15f), Floor, true);
        AddBox(room, new Vector3(25.36f, -69.23f, -7.87f), Vector3.zero, new Vector3(1.85f, 1.55f, 0.08f), Wall, false);
        AddBox(room, new Vector3(25.36f, -68.75f, -7.82f), Vector3.zero, new Vector3(1.55f, 0.13f, 0.05f), FoundationBlue, false);
    }

    private static void AddBench(Room room, Vector3 localPosition, float yaw)
    {
        AddBox(room, localPosition, new Vector3(0f, yaw, 0f), new Vector3(0.55f, 0.28f, 1.75f), Dark, true);
        AddBox(room, localPosition + new Vector3(0f, 0.46f, -0.34f), new Vector3(0f, yaw, 0f), new Vector3(0.55f, 0.72f, 0.12f), Wall, true);
    }

    private static void AddCabinet(Room room, Vector3 localPosition)
    {
        AddBox(room, localPosition, Vector3.zero, new Vector3(0.72f, 2.05f, 0.68f), Wall, true);
        AddBox(room, localPosition + new Vector3(0f, 0.35f, 0.35f), Vector3.zero, new Vector3(0.54f, 0.08f, 0.04f), FoundationBlue, false);
        AddBox(room, localPosition + new Vector3(0f, -0.35f, 0.35f), Vector3.zero, new Vector3(0.54f, 0.08f, 0.04f), Dark, false);
    }

    private static PrimitiveObjectToy AddBox(Room room, Vector3 localPosition, Vector3 localRotation, Vector3 scale, Color color, bool collidable)
    {
        Vector3 worldPosition = room.Transform.TransformPoint(localPosition);
        Quaternion worldRotation = room.Rotation * Quaternion.Euler(localRotation);

        PrimitiveObjectToy toy = PrimitiveObjectToy.Create(worldPosition, worldRotation, scale, null, false);
        toy.Type = PrimitiveType.Cube;
        toy.Color = color;
        toy.Flags = collidable ? PrimitiveFlags.Visible | PrimitiveFlags.Collidable : PrimitiveFlags.Visible;
        toy.IsStatic = true;
        toy.Spawn();
        Spawned.Add(toy);
        return toy;
    }
}
