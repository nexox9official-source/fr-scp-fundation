using System.Collections.Generic;
using System.Linq;
using MapGeneration;

namespace SiteRP.Core;

/// <summary>
/// SiteRP Operational Facility blueprint inspired by the public Site-76 plugin design,
/// but mapped only onto rooms that exist in the audited SCP:SL 14.2.7 facility.
/// This class does not spawn or modify geometry; it exposes the safe module plan used by
/// the detailed facility audit and future ProjectMER/AdminToy modules.
/// </summary>
internal static class SiteRpFacilityPlan
{
    private static readonly (string Module, RoomName Room, string Purpose)[] Assignments =
    {
        ("ADMIN-DIRECTION", RoomName.EzOfficeStoried, "Direction du Site, bureau du Directeur, salle de reunion et secretariat"),
        ("ADMIN-GENERAL", RoomName.EzOfficeLarge, "Administration, RH, archives et bureaux du personnel administratif"),
        ("ADMIN-SECURE", RoomName.EzOfficeSmall, "Bureau securise, dossiers sensibles et responsable de service"),
        ("COMMAND", RoomName.EzIntercom, "Centre de commandement / communications et annonces du Site"),
        ("SECURITY-MTF", RoomName.HczArmory, "QG securite lourde, armurerie et staging MTF"),
        ("IT-MAINTENANCE", RoomName.HczServers, "Serveurs, IT, maintenance reseau et infrastructure"),
        ("RESEARCH-HEAVY", RoomName.HczTestroom, "Laboratoire d'essais lourds et recherche de confinement"),
        ("RESEARCH-IT", RoomName.LczComputerRoom, "Recherche informatique, analyse et postes de travail"),
        ("RESEARCH-LAB", RoomName.LczGlassroom, "Laboratoire propre / observation / experimentation"),
        ("BIOLOGY", RoomName.LczGreenhouse, "Biologie, botanique et recherche environnementale"),
        ("LCZ-SECURITY", RoomName.LczArmory, "Poste de securite LCZ et stockage controle"),
        ("STAFF-FACILITIES", RoomName.LczToilets, "Bloc sanitaire et point de vie du personnel"),
        ("EVAC-MEDICAL", RoomName.EzEvacShelter, "Accueil evacuation / triage medical d'urgence propre"),
        ("TECH-SEAL-A", RoomName.EzCollapsedTunnel, "Couloirs techniques condamnes, sans faux passage vers le vide"),
    };

    public static IEnumerable<RoomName> AuditRooms => Assignments.Select(x => x.Room).Distinct();

    public static string Describe()
    {
        List<string> lines = new()
        {
            "SiteRP Operational Facility - plan modules",
            "Inspire de Site-76, adapte a SCP:SL 14.2.7. Aucun amenagement nouveau n'est place sans audit local de la salle.",
            string.Empty,
        };

        foreach ((string module, RoomName room, string purpose) in Assignments)
            lines.Add($"{module}: {room} -> {purpose}");

        lines.Add(string.Empty);
        lines.Add("Etape actuelle: audit detaille de ces salles, puis construction module par module et reversible.");
        return string.Join("\n", lines);
    }
}
