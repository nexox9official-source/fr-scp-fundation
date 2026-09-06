using System;
using System.Collections.Generic;
using System.Linq;
using PlayerRoles;
using LabApi.Features.Console;

namespace SiteRP.Core;

internal enum SiteRpSiteState
{
    Normal,
    Incident,
    Breach,
    MajorBreach,
    Evacuation,
}

internal enum SiteRpScpState
{
    Contained,
    Testing,
    Cooperative,
    Hostile,
    Breached,
    Recontaining,
    Recontained,
    Disabled,
}

internal static class SiteRpScpStateManager
{
    private static readonly string[] KnownScps =
    {
        "049", "079", "096", "106", "173", "939", "3114",
        "008-X", "999", "1048", "650",
    };

    private static readonly Dictionary<string, SiteRpScpState> States =
        new(StringComparer.OrdinalIgnoreCase);

    public static SiteRpSiteState SiteState { get; private set; } = SiteRpSiteState.Normal;

    static SiteRpScpStateManager()
    {
        Reset();
    }

    public static void Reset()
    {
        States.Clear();
        foreach (string scp in KnownScps)
            States[scp] = SiteRpScpState.Contained;

        // SiteRP: 079 starts as C.A.S.S.I.E., a Foundation-controlled cooperative AI.
        States["079"] = SiteRpScpState.Cooperative;
        SiteState = SiteRpSiteState.Normal;
    }

    public static bool TrySetSiteState(string raw, out string response)
    {
        string normalized = Normalize(raw);
        SiteRpSiteState? state = normalized switch
        {
            "normal" => SiteRpSiteState.Normal,
            "incident" => SiteRpSiteState.Incident,
            "breach" => SiteRpSiteState.Breach,
            "majorbreach" or "major" => SiteRpSiteState.MajorBreach,
            "evacuation" or "evac" => SiteRpSiteState.Evacuation,
            _ => null,
        };

        if (!state.HasValue)
        {
            response = "Etat de site invalide. Valeurs: NORMAL, INCIDENT, BREACH, MAJOR_BREACH, EVACUATION.";
            return false;
        }

        SiteState = state.Value;
        Logger.Info($"[SiteRP.SCP] Site state => {SiteState}");
        response = $"Etat du Site: {SiteState.ToString().ToUpperInvariant()}.";
        return true;
    }

    public static bool TrySetScpState(string scp, string rawState, out string response)
    {
        string id = NormalizeScpId(scp);
        if (!States.ContainsKey(id))
        {
            response = $"SCP inconnu: {scp}. Connus: {string.Join(", ", KnownScps)}";
            return false;
        }

        string normalized = Normalize(rawState);
        SiteRpScpState? state = normalized switch
        {
            "contained" or "contain" => SiteRpScpState.Contained,
            "testing" or "test" => SiteRpScpState.Testing,
            "cooperative" or "coop" or "cassie" or "healthy" => SiteRpScpState.Cooperative,
            "hostile" => SiteRpScpState.Hostile,
            "breached" or "breach" or "released" or "release" or "compromised" => SiteRpScpState.Breached,
            "recontaining" => SiteRpScpState.Recontaining,
            "recontained" => SiteRpScpState.Recontained,
            "disabled" or "disable" or "offline" => SiteRpScpState.Disabled,
            _ => null,
        };

        if (!state.HasValue)
        {
            response = "Etat SCP invalide. Valeurs: CONTAINED, TESTING, COOPERATIVE, HOSTILE, BREACHED, RECONTAINING, RECONTAINED, DISABLED.";
            return false;
        }

        States[id] = state.Value;
        Logger.Info($"[SiteRP.SCP] SCP-{id} state => {state.Value}");

        response = id == "079"
            ? $"SCP-079 / C.A.S.S.I.E.: {state.Value.ToString().ToUpperInvariant()}."
            : $"SCP-{id}: {state.Value.ToString().ToUpperInvariant()}.";
        return true;
    }

    public static SiteRpScpState Get(string scp)
    {
        string id = NormalizeScpId(scp);
        return States.TryGetValue(id, out SiteRpScpState state) ? state : SiteRpScpState.Contained;
    }

    public static bool CanLeaveContainment(string scp)
    {
        SiteRpScpState state = Get(scp);
        return state is SiteRpScpState.Breached or SiteRpScpState.Hostile;
    }

    public static bool Is079Online => Get("079") is not (SiteRpScpState.Disabled or SiteRpScpState.Recontained or SiteRpScpState.Recontaining);

    public static bool Can079UseCameras => Is079Online;

    public static bool Can079UseFoundationSupport =>
        Get("079") is SiteRpScpState.Cooperative or SiteRpScpState.Testing or SiteRpScpState.Hostile or SiteRpScpState.Breached;

    /// <summary>Normal camera-side door open/close. This is non-lethal Foundation support.</summary>
    public static bool Can079OpenCloseDoors => Can079UseFoundationSupport;

    /// <summary>Emergency unlock is available from INCIDENT upward, or always when 079 is hostile/compromised.</summary>
    public static bool Can079UnlockDoors =>
        Can079UseHostileSystems ||
        (Get("079") is SiteRpScpState.Cooperative or SiteRpScpState.Testing &&
         SiteState is SiteRpSiteState.Incident or SiteRpSiteState.Breach or SiteRpSiteState.MajorBreach or SiteRpSiteState.Evacuation);

    /// <summary>Door locking is a security protocol from BREACH upward while C.A.S.S.I.E. is cooperative.</summary>
    public static bool Can079LockDoors =>
        Can079UseHostileSystems ||
        (Get("079") is SiteRpScpState.Cooperative or SiteRpScpState.Testing &&
         SiteState is SiteRpSiteState.Breach or SiteRpSiteState.MajorBreach or SiteRpSiteState.Evacuation);

    /// <summary>Room lockdown is reserved for major emergencies unless 079 is hostile.</summary>
    public static bool Can079LockdownRooms =>
        Can079UseHostileSystems ||
        (Get("079") is SiteRpScpState.Cooperative or SiteRpScpState.Testing &&
         SiteState is SiteRpSiteState.MajorBreach or SiteRpSiteState.Evacuation);

    /// <summary>Blackouts and Tesla are lethal/hostile systems and never part of normal Foundation support.</summary>
    public static bool Can079UseHostileSystems =>
        Get("079") is SiteRpScpState.Hostile or SiteRpScpState.Breached;

    public static string Describe079Permissions()
    {
        string ai = Get("079").ToString().ToUpperInvariant();
        string site = SiteState.ToString().ToUpperInvariant();
        return $"IA={ai} | SITE={site} | cameras={(Can079UseCameras ? "OUI" : "NON")} | portes={(Can079OpenCloseDoors ? "OUVRIR/FERMER" : "NON")} | unlock={(Can079UnlockDoors ? "OUI" : "NON")} | lock={(Can079LockDoors ? "OUI" : "NON")} | lockdown={(Can079LockdownRooms ? "OUI" : "NON")} | lethal={(Can079UseHostileSystems ? "OUI" : "NON")}";
    }

    public static string Status()
    {
        IEnumerable<string> rows = States
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"SCP-{x.Key}: {x.Value.ToString().ToUpperInvariant()}");

        return $"Site: {SiteState.ToString().ToUpperInvariant()}\n" + string.Join("\n", rows);
    }

    public static string? GetScpId(RoleTypeId role) => role switch
    {
        RoleTypeId.Scp049 => "049",
        RoleTypeId.Scp0492 => "049",
        RoleTypeId.Scp079 => "079",
        RoleTypeId.Scp096 => "096",
        RoleTypeId.Scp106 => "106",
        RoleTypeId.Scp173 => "173",
        RoleTypeId.Scp939 => "939",
        RoleTypeId.Scp3114 => "3114",
        _ => null,
    };

    private static string NormalizeScpId(string value)
    {
        string normalized = value.Trim().ToUpperInvariant()
            .Replace("SCP-", string.Empty)
            .Replace("SCP", string.Empty)
            .Replace("_", "-")
            .Replace(" ", string.Empty);

        return normalized switch
        {
            "008X" => "008-X",
            _ => normalized,
        };
    }

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant().Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
}
