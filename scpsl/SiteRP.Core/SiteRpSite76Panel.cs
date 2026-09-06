using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using AdminToy = LabApi.Features.Wrappers.AdminToy;
using PrimitiveObjectToy = LabApi.Features.Wrappers.PrimitiveObjectToy;
using InteractableToy = LabApi.Features.Wrappers.InteractableToy;
using TextToy = LabApi.Features.Wrappers.TextToy;
using PrimitiveFlags = AdminToys.PrimitiveFlags;
using LabLogger = LabApi.Features.Console.Logger;

namespace SiteRP.Core;

/// <summary>
/// Site-76 specific physical SiteRP controls.
/// Coordinates are anchored beside Site-76's original Map System / command display,
/// whose schematic root is loaded at (35.85696, 1031.062, -43.02825).
/// This replaces the old vanilla-room Operational overlay when Site-76 is active.
/// </summary>
internal sealed class SiteRpSite76Events : CustomEventsHandler
{
    private static int _generation;

    public override void OnServerWaitingForPlayers()
    {
        Schedule(2.0f);
    }

    public override void OnServerMapGenerated(MapGeneratedEventArgs ev)
    {
        SiteRpSite76Panel.Remove();
        Schedule(3.0f);
    }

    public override void OnServerRoundStarted()
    {
        Schedule(1.0f);
    }

    private static void Schedule(float delay)
    {
        int generation = ++_generation;
        Timing.CallDelayed(delay, () =>
        {
            if (generation != _generation || SiteRpSite76Panel.IsApplied)
                return;

            string result = SiteRpSite76Panel.Apply();
            if (!SiteRpSite76Panel.IsApplied)
                LabLogger.Warn($"[SiteRP.Site76] Panneau Site-76 non applique: {result}");
        });
    }
}

internal static class SiteRpSite76Panel
{
    private static readonly List<AdminToy> Spawned = new();

    // Original Site-76 Map System world anchor:
    // schematic root + local Map System = (25.99296, 1040.580904, -35.20225).
    private static readonly Vector3 MapSystemAnchor = new(25.99296f, 1040.5809f, -35.20225f);
    private static readonly Quaternion PanelRotation = Quaternion.Euler(0f, 90f, 0f);

    private static readonly Color Dark = new(0.055f, 0.065f, 0.075f, 1f);
    private static readonly Color Border = new(0.10f, 0.25f, 0.38f, 1f);

    public static bool IsApplied => Spawned.Any(x => x is not null && !x.IsDestroyed);
    public static int SpawnedCount => Spawned.Count(x => x is not null && !x.IsDestroyed);

    public static string Apply()
    {
        Remove();

        try
        {
            // Mounted adjacent to the original Site-76 Map System to create one coherent command station.
            Vector3 center = MapSystemAnchor + new Vector3(0.0f, 0.15f, 2.35f);

            AddBox(center, PanelRotation, new Vector3(0.14f, 1.65f, 4.15f), Dark, false);
            AddBox(center + new Vector3(-0.075f, 0.72f, 0f), PanelRotation, new Vector3(0.06f, 0.10f, 3.90f), Border, false);

            AddAlarmButton(center, -1.45f, new Color(0.20f, 0.75f, 0.25f, 1f), SiteRpSiteState.Normal);
            AddAlarmButton(center, -0.72f, new Color(1.00f, 0.62f, 0.10f, 1f), SiteRpSiteState.Incident);
            AddAlarmButton(center, 0.00f, new Color(1.00f, 0.20f, 0.18f, 1f), SiteRpSiteState.Breach);
            AddAlarmButton(center, 0.72f, new Color(0.62f, 0.02f, 0.02f, 1f), SiteRpSiteState.MajorBreach);
            AddAlarmButton(center, 1.45f, new Color(0.20f, 0.55f, 1.00f, 1f), SiteRpSiteState.Evacuation);

            AddText(
                center + new Vector3(-0.09f, 0.43f, 0f),
                "<b>CONTROLE ALARMES — SITE-76</b>\n" +
                "<color=#73D673>NORMAL</color>   <color=#FFB84D>INCIDENT</color>   " +
                "<color=#FF6961>BREACH</color>   <color=#FF3030>MAJOR</color>   <color=#62A8FF>EVAC</color>");

            string result = $"panneau de commandement Site-76 actif ({SpawnedCount} elements).";
            LabLogger.Info($"[SiteRP.Site76] {result}");
            return result;
        }
        catch (Exception e)
        {
            string error = $"{e.GetType().Name}: {e.Message}";
            LabLogger.Error($"[SiteRP.Site76] Erreur panneau: {error}\n{e}");
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
                LabLogger.Warn($"[SiteRP.Site76] Impossible de retirer un element: {e.Message}");
            }
        }

        Spawned.Clear();
        return $"Site-76 panel retire: {removed} element(s).";
    }

    private static void AddAlarmButton(Vector3 center, float localZ, Color color, SiteRpSiteState state)
    {
        Vector3 offset = PanelRotation * new Vector3(-0.10f, -0.15f, localZ);
        Vector3 buttonPosition = center + offset;

        AddBox(buttonPosition, PanelRotation, new Vector3(0.22f, 0.43f, 0.43f), color, false);

        Vector3 interactionPosition = buttonPosition + (PanelRotation * new Vector3(-0.12f, 0f, 0f));
        InteractableToy toy = InteractableToy.Create(
            interactionPosition,
            PanelRotation,
            new Vector3(0.40f, 0.55f, 0.55f),
            null,
            false);
        toy.InteractionDuration = 0f;
        toy.OnInteracted += player => SiteRpAlarmSystem.Press(player, state);
        toy.Spawn();
        Spawned.Add(toy);
    }

    private static void AddText(Vector3 position, string text)
    {
        Quaternion rotation = PanelRotation * Quaternion.Euler(0f, 90f, 0f);
        TextToy toy = TextToy.Create(position, rotation, new Vector3(0.14f, 0.14f, 0.14f), null, false);
        toy.TextFormat = text;
        toy.DisplaySize = new Vector2(950f, 280f);
        toy.Spawn();
        Spawned.Add(toy);
    }

    private static PrimitiveObjectToy AddBox(Vector3 position, Quaternion rotation, Vector3 scale, Color color, bool collidable)
    {
        PrimitiveObjectToy toy = PrimitiveObjectToy.Create(position, rotation, scale, null, false);
        toy.Type = PrimitiveType.Cube;
        toy.Color = color;
        toy.Flags = collidable ? PrimitiveFlags.Visible | PrimitiveFlags.Collidable : PrimitiveFlags.Visible;
        toy.IsStatic = true;
        toy.Spawn();
        Spawned.Add(toy);
        return toy;
    }
}
