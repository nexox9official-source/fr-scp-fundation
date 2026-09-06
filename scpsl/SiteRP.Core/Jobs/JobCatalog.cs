using System.Collections.ObjectModel;

namespace SiteRP.Core.Jobs;

public static class JobCatalog
{
    private static readonly ReadOnlyCollection<JobDefinition> _jobs = new List<JobDefinition>
    {
        new() { UcrRoleId = 1001, Name = "Directeur du Site", Category = "Direction", Description = "Autorite executive du Site.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Direction", SortOrder = 10 },
        new() { UcrRoleId = 1002, Name = "Directeur Adjoint", Category = "Direction", Description = "Adjoint direct du Directeur du Site.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Direction", SortOrder = 20 },
        new() { UcrRoleId = 1003, Name = "Chef de l'Administration", Category = "Administration", Description = "Responsable de l'administration du Site.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Administration", SortOrder = 30 },
        new() { UcrRoleId = 1004, Name = "Agent Administratif", Category = "Administration", Description = "Personnel administratif du Site.", AccessMode = JobAccessMode.Public, MaxPlayers = 6, WardrobeName = "SiteRP_Administration", SortOrder = 40 },
        new() { UcrRoleId = 1005, Name = "Responsable Communications", Category = "Administration", Description = "Responsable des communications internes.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Administration", SortOrder = 50 },
        new() { UcrRoleId = 1006, Name = "Agent des Affaires Internes", Category = "Administration", Description = "Enquetes internes et controle disciplinaire.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 3, WardrobeName = "SiteRP_InternalAffairs", SortOrder = 60 },
        new() { UcrRoleId = 1007, Name = "Archiviste", Category = "Administration", Description = "Gestion des archives et dossiers.", AccessMode = JobAccessMode.Public, MaxPlayers = 3, WardrobeName = "SiteRP_RAISA", SortOrder = 70 },
        new() { UcrRoleId = 1010, Name = "Liaison du Conseil O5", Category = "Haut Commandement", Description = "Liaison autorisee du Conseil O5.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_O5", SortOrder = 80 },
        new() { UcrRoleId = 1011, Name = "Membre du Conseil O5", Category = "Haut Commandement", Description = "Role exceptionnel du Conseil O5.", AccessMode = JobAccessMode.StaffOnly, MaxPlayers = 1, WardrobeName = "SiteRP_O5", SortOrder = 90 },
        new() { UcrRoleId = 1012, Name = "Liaison du Comite d'Ethique", Category = "Haut Commandement", Description = "Liaison du Comite d'Ethique.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Ethics", SortOrder = 100 },
        new() { UcrRoleId = 1013, Name = "Membre du Comite d'Ethique", Category = "Haut Commandement", Description = "Role exceptionnel du Comite d'Ethique.", AccessMode = JobAccessMode.StaffOnly, MaxPlayers = 1, WardrobeName = "SiteRP_Ethics", SortOrder = 110 },

        new() { UcrRoleId = 1101, Name = "Directeur de la Recherche", Category = "Recherche", Description = "Direction scientifique du Site.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_ResearchCommand", SortOrder = 200 },
        new() { UcrRoleId = 1102, Name = "Responsable du Confinement", Category = "Recherche", Description = "Responsable scientifique des procedures de confinement.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_ResearchCommand", SortOrder = 210 },
        new() { UcrRoleId = 1103, Name = "Chercheur Senior", Category = "Recherche", Description = "Chercheur experimente.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Research", SortOrder = 220 },
        new() { UcrRoleId = 1104, Name = "Chercheur", Category = "Recherche", Description = "Personnel scientifique.", AccessMode = JobAccessMode.Public, MaxPlayers = 10, WardrobeName = "SiteRP_Research", SortOrder = 230 },
        new() { UcrRoleId = 1105, Name = "Chercheur Junior", Category = "Recherche", Description = "Personnel scientifique junior.", AccessMode = JobAccessMode.Public, MaxPlayers = 10, WardrobeName = "SiteRP_Research", SortOrder = 240 },
        new() { UcrRoleId = 1106, Name = "Technicien de Laboratoire", Category = "Recherche", Description = "Support technique de laboratoire.", AccessMode = JobAccessMode.Public, MaxPlayers = 6, WardrobeName = "SiteRP_Research", SortOrder = 250 },
        new() { UcrRoleId = 1107, Name = "Specialiste Cognitohazards", Category = "Recherche", Description = "Specialiste des risques cognitifs.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_ResearchSpecialist", SortOrder = 260 },
        new() { UcrRoleId = 1108, Name = "Specialiste des Anomalies", Category = "Recherche", Description = "Specialiste des anomalies complexes.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 3, WardrobeName = "SiteRP_ResearchSpecialist", SortOrder = 270 },

        new() { UcrRoleId = 1201, Name = "Directeur Medical", Category = "Medical", Description = "Direction du service medical.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_MedicalCommand", SortOrder = 300 },
        new() { UcrRoleId = 1202, Name = "Medecin", Category = "Medical", Description = "Medecin du Site.", AccessMode = JobAccessMode.Public, MaxPlayers = 5, WardrobeName = "SiteRP_Medical", SortOrder = 310 },
        new() { UcrRoleId = 1203, Name = "Chirurgien", Category = "Medical", Description = "Chirurgien du Site.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_Medical", SortOrder = 320 },
        new() { UcrRoleId = 1204, Name = "Infirmier", Category = "Medical", Description = "Personnel infirmier.", AccessMode = JobAccessMode.Public, MaxPlayers = 6, WardrobeName = "SiteRP_Medical", SortOrder = 330 },
        new() { UcrRoleId = 1205, Name = "Paramedic", Category = "Medical", Description = "Intervention medicale d'urgence.", AccessMode = JobAccessMode.Public, MaxPlayers = 4, WardrobeName = "SiteRP_Paramedic", SortOrder = 340 },
        new() { UcrRoleId = 1206, Name = "Psychologue", Category = "Medical", Description = "Psychologue du Site.", AccessMode = JobAccessMode.Public, MaxPlayers = 2, WardrobeName = "SiteRP_Medical", SortOrder = 350 },
        new() { UcrRoleId = 1207, Name = "Medecin Biohazard", Category = "Medical", Description = "Support medical CBRN.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_CBRN", SortOrder = 360 },

        new() { UcrRoleId = 1301, Name = "Directeur de l'Ingenierie", Category = "Ingenierie", Description = "Direction technique du Site.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_EngineeringCommand", SortOrder = 400 },
        new() { UcrRoleId = 1302, Name = "Ingenieur Confinement", Category = "Ingenierie", Description = "Ingenierie des systemes de confinement.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Engineering", SortOrder = 410 },
        new() { UcrRoleId = 1303, Name = "Ingenieur", Category = "Ingenierie", Description = "Ingenieur du Site.", AccessMode = JobAccessMode.Public, MaxPlayers = 6, WardrobeName = "SiteRP_Engineering", SortOrder = 420 },
        new() { UcrRoleId = 1304, Name = "Technicien Maintenance", Category = "Maintenance", Description = "Maintenance generale du Site.", AccessMode = JobAccessMode.Public, MaxPlayers = 8, WardrobeName = "SiteRP_Maintenance", SortOrder = 430 },
        new() { UcrRoleId = 1305, Name = "Technicien Systemes / IT", Category = "Ingenierie", Description = "IT, serveurs et systemes techniques.", AccessMode = JobAccessMode.Public, MaxPlayers = 4, WardrobeName = "SiteRP_IT", SortOrder = 440 },
        new() { UcrRoleId = 1306, Name = "Electricien", Category = "Maintenance", Description = "Electricite et alimentation du Site.", AccessMode = JobAccessMode.Public, MaxPlayers = 4, WardrobeName = "SiteRP_Maintenance", SortOrder = 450 },
        new() { UcrRoleId = 1307, Name = "Logisticien", Category = "Logistique", Description = "Transport et approvisionnement.", AccessMode = JobAccessMode.Public, MaxPlayers = 6, WardrobeName = "SiteRP_Logistics", SortOrder = 460 },
        new() { UcrRoleId = 1308, Name = "Agent d'Entretien", Category = "Maintenance", Description = "Entretien courant du Site.", AccessMode = JobAccessMode.Public, MaxPlayers = 8, WardrobeName = "SiteRP_Maintenance", SortOrder = 470 },

        new() { UcrRoleId = 1401, Name = "Chef de la Securite", Category = "Securite", Description = "Commandement de la securite du Site.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_SecurityCommand", SortOrder = 500 },
        new() { UcrRoleId = 1402, Name = "Capitaine de la Securite", Category = "Securite", Description = "Commandement intermediaire de la securite.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_SecurityCommand", SortOrder = 510 },
        new() { UcrRoleId = 1403, Name = "Lieutenant de la Securite", Category = "Securite", Description = "Officier de securite.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 3, WardrobeName = "SiteRP_SecurityCommand", SortOrder = 520 },
        new() { UcrRoleId = 1404, Name = "Sergent de la Securite", Category = "Securite", Description = "Sous-officier de securite.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 5, WardrobeName = "SiteRP_Security", SortOrder = 530 },
        new() { UcrRoleId = 1405, Name = "Garde Senior", Category = "Securite", Description = "Garde experimente.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 8, WardrobeName = "SiteRP_Security", SortOrder = 540 },
        new() { UcrRoleId = 1406, Name = "Garde", Category = "Securite", Description = "Securite interne du Site.", AccessMode = JobAccessMode.Public, MaxPlayers = 16, WardrobeName = "SiteRP_Security", SortOrder = 550 },
        new() { UcrRoleId = 1407, Name = "Cadet de la Securite", Category = "Securite", Description = "Formation securite.", AccessMode = JobAccessMode.Public, MaxPlayers = 10, WardrobeName = "SiteRP_Security", SortOrder = 560 },
        new() { UcrRoleId = 1408, Name = "Armurier", Category = "Securite", Description = "Gestion de l'armurerie.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_Security", SortOrder = 570 },
        new() { UcrRoleId = 1409, Name = "Agent d'Intervention Interne", Category = "Securite", Description = "Intervention tactique interne.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 6, WardrobeName = "SiteRP_InternalResponse", SortOrder = 580 },

        new() { UcrRoleId = 1501, Name = "Classe-D", Category = "Classe-D", Description = "Personnel de Classe-D.", AccessMode = JobAccessMode.Public, MaxPlayers = 40, WardrobeName = "", SortOrder = 600 },
        new() { UcrRoleId = 1502, Name = "Classe-D Auxiliaire", Category = "Classe-D", Description = "Classe-D autorisee pour travaux encadres.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 8, WardrobeName = "", SortOrder = 610 },

        // Local RP roles added in v0.7.
        new() { UcrRoleId = 1410, Name = "Operateur CCTV", Category = "Securite", Description = "Surveillance video et suivi des incidents.", AccessMode = JobAccessMode.Public, MaxPlayers = 3, WardrobeName = "SiteRP_Security", SortOrder = 590 },
        new() { UcrRoleId = 1411, Name = "Dispatcher Securite", Category = "Securite", Description = "Coordination radio et dispatch securite.", AccessMode = JobAccessMode.Public, MaxPlayers = 2, WardrobeName = "SiteRP_Security", SortOrder = 591 },
        new() { UcrRoleId = 1412, Name = "Responsable Detention", Category = "Securite", Description = "Responsable de la zone de detention Classe-D.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Detention", SortOrder = 592 },
        new() { UcrRoleId = 1413, Name = "Agent Penitentiaire", Category = "Securite", Description = "Surveillance et escortes de Classe-D.", AccessMode = JobAccessMode.Public, MaxPlayers = 6, WardrobeName = "SiteRP_Detention", SortOrder = 593 },
        new() { UcrRoleId = 1310, Name = "Quartier-maitre", Category = "Logistique", Description = "Responsable des stocks et de l'equipement.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Logistics", SortOrder = 471 },
        new() { UcrRoleId = 1311, Name = "Responsable Approvisionnement", Category = "Logistique", Description = "Approvisionnement et distribution interne.", AccessMode = JobAccessMode.Public, MaxPlayers = 2, WardrobeName = "SiteRP_Logistics", SortOrder = 472 },
        new() { UcrRoleId = 1312, Name = "Pompier / Secours Incendie", Category = "Urgences", Description = "Intervention incendie et secours technique.", AccessMode = JobAccessMode.Public, MaxPlayers = 4, WardrobeName = "SiteRP_FireRescue", SortOrder = 473 },
        new() { UcrRoleId = 1313, Name = "Technicien CBRN Local", Category = "Urgences", Description = "Reponse locale aux risques chimiques et biologiques.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 3, WardrobeName = "SiteRP_CBRN", SortOrder = 474 },
        new() { UcrRoleId = 1020, Name = "Directeur RAISA", Category = "RAISA", Description = "Direction des archives, informations et securite documentaire.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_RAISA", SortOrder = 120 },
        new() { UcrRoleId = 1021, Name = "Agent RAISA", Category = "RAISA", Description = "Gestion des dossiers et securite de l'information.", AccessMode = JobAccessMode.Public, MaxPlayers = 4, WardrobeName = "SiteRP_RAISA", SortOrder = 121 },
        new() { UcrRoleId = 1022, Name = "Directeur du Renseignement", Category = "Renseignement", Description = "Direction du renseignement et contre-espionnage.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Intelligence", SortOrder = 122 },
        new() { UcrRoleId = 1023, Name = "Agent de Renseignement", Category = "Renseignement", Description = "Renseignement interne et contre-espionnage.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Intelligence", SortOrder = 123 },
        new() { UcrRoleId = 1024, Name = "Inspecteur du Site", Category = "Administration", Description = "Audit des procedures et inspections internes.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_InternalAffairs", SortOrder = 124 },
        new() { UcrRoleId = 1025, Name = "Agent de Liaison Fondation", Category = "Administration", Description = "Liaison avec les forces et services externes.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_Administration", SortOrder = 125 },
        new() { UcrRoleId = 1590, Name = "Coordinateur des FIM", Category = "Forces d'Intervention", Description = "Coordination des deploiements FIM sur le Site.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_MTFCommand", SortOrder = 690 },

        // Core task forces. Rare/specialist units remain whitelist-only.
        new() { UcrRoleId = 1601, Name = "EPSILON-11 | Commandant", Category = "FIM Epsilon-11", Description = "Commandement Nine-Tailed Fox.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Epsilon11", SortOrder = 700 },
        new() { UcrRoleId = 1602, Name = "EPSILON-11 | Lieutenant", Category = "FIM Epsilon-11", Description = "Officier Nine-Tailed Fox.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_Epsilon11", SortOrder = 701 },
        new() { UcrRoleId = 1603, Name = "EPSILON-11 | Sergent", Category = "FIM Epsilon-11", Description = "Sous-officier Nine-Tailed Fox.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 3, WardrobeName = "SiteRP_Epsilon11", SortOrder = 702 },
        new() { UcrRoleId = 1604, Name = "EPSILON-11 | Operateur", Category = "FIM Epsilon-11", Description = "Operateur Nine-Tailed Fox.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 8, WardrobeName = "SiteRP_Epsilon11", SortOrder = 703 },
        new() { UcrRoleId = 1605, Name = "EPSILON-11 | Specialiste Confinement", Category = "FIM Epsilon-11", Description = "Specialiste Nine-Tailed Fox.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Epsilon11", SortOrder = 704 },

        new() { UcrRoleId = 1611, Name = "ALPHA-1 | Commandant", Category = "FIM Alpha-1", Description = "Commandement Red Right Hand.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Alpha1", SortOrder = 710 },
        new() { UcrRoleId = 1612, Name = "ALPHA-1 | Lieutenant", Category = "FIM Alpha-1", Description = "Officier Red Right Hand.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_Alpha1", SortOrder = 711 },
        new() { UcrRoleId = 1613, Name = "ALPHA-1 | Operateur", Category = "FIM Alpha-1", Description = "Operateur Red Right Hand.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 8, WardrobeName = "SiteRP_Alpha1", SortOrder = 712 },
        new() { UcrRoleId = 1614, Name = "ALPHA-1 | Specialiste", Category = "FIM Alpha-1", Description = "Specialiste Red Right Hand.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Alpha1", SortOrder = 713 },

        new() { UcrRoleId = 1621, Name = "OMEGA-1 | Commandant", Category = "FIM Omega-1", Description = "Commandement Law's Left Hand.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Omega1", SortOrder = 720 },
        new() { UcrRoleId = 1622, Name = "OMEGA-1 | Enqueteur Senior", Category = "FIM Omega-1", Description = "Enqueteur senior du Comite d'Ethique.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_Omega1", SortOrder = 721 },
        new() { UcrRoleId = 1623, Name = "OMEGA-1 | Operateur", Category = "FIM Omega-1", Description = "Operateur Law's Left Hand.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 8, WardrobeName = "SiteRP_Omega1", SortOrder = 722 },
        new() { UcrRoleId = 1624, Name = "OMEGA-1 | Specialiste Ethique", Category = "FIM Omega-1", Description = "Specialiste du Comite d'Ethique.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 3, WardrobeName = "SiteRP_Omega1", SortOrder = 723 },

        new() { UcrRoleId = 1631, Name = "BETA-7 | Commandant", Category = "FIM Beta-7", Description = "Commandement Maz Hatters.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Beta7", SortOrder = 730 },
        new() { UcrRoleId = 1632, Name = "BETA-7 | Specialiste CBRN", Category = "FIM Beta-7", Description = "Specialiste CBRN Maz Hatters.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Beta7", SortOrder = 731 },
        new() { UcrRoleId = 1633, Name = "BETA-7 | Medic de Combat", Category = "FIM Beta-7", Description = "Medic de combat Maz Hatters.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Beta7", SortOrder = 732 },
        new() { UcrRoleId = 1634, Name = "BETA-7 | Operateur", Category = "FIM Beta-7", Description = "Operateur Maz Hatters.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 8, WardrobeName = "SiteRP_Beta7", SortOrder = 733 },
        new() { UcrRoleId = 1635, Name = "BETA-7 | Technicien Decontamination", Category = "FIM Beta-7", Description = "Technicien decontamination Maz Hatters.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Beta7", SortOrder = 734 },

        new() { UcrRoleId = 1641, Name = "NU-7 | Commandant", Category = "FIM Nu-7", Description = "Commandement Hammer Down.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 1, WardrobeName = "SiteRP_Nu7", SortOrder = 740 },
        new() { UcrRoleId = 1642, Name = "NU-7 | Lieutenant", Category = "FIM Nu-7", Description = "Officier Hammer Down.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 2, WardrobeName = "SiteRP_Nu7", SortOrder = 741 },
        new() { UcrRoleId = 1643, Name = "NU-7 | Specialiste Lourd", Category = "FIM Nu-7", Description = "Specialiste lourd Hammer Down.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Nu7", SortOrder = 742 },
        new() { UcrRoleId = 1644, Name = "NU-7 | Ingenieur de Combat", Category = "FIM Nu-7", Description = "Ingenieur de combat Hammer Down.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 4, WardrobeName = "SiteRP_Nu7", SortOrder = 743 },
        new() { UcrRoleId = 1645, Name = "NU-7 | Operateur", Category = "FIM Nu-7", Description = "Operateur Hammer Down.", AccessMode = JobAccessMode.Whitelist, MaxPlayers = 8, WardrobeName = "SiteRP_Nu7", SortOrder = 744 },

        new() { UcrRoleId = 1999, Name = "STAFF", Category = "STAFF", Description = "Mode moderation hors-RP.", AccessMode = JobAccessMode.StaffOnly, MaxPlayers = 32, WardrobeName = "SiteRP_Staff", SortOrder = 9999 },
    }.AsReadOnly();

    public static IReadOnlyList<JobDefinition> All => _jobs;

    public static JobDefinition? Find(int roleId) => _jobs.FirstOrDefault(x => x.UcrRoleId == roleId);
}
