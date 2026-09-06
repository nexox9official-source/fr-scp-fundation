using System.Collections.ObjectModel;
using LabApi.Loader.Features.Paths;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Builds the SiteRP jobs selector directly from every UCR YAML on this server.
/// Access policy stays controlled by SiteRP, while names/slots automatically follow UCR.
/// </summary>
public static class JobCatalog
{
    private static ReadOnlyCollection<JobDefinition> _jobs = new List<JobDefinition>().AsReadOnly();

    private static readonly HashSet<int> PublicRoleIds = new()
    {
        1004, 1007, 1021,
        1104, 1105, 1106,
        1202, 1204, 1205, 1206,
        1303, 1304, 1305, 1306, 1307, 1308, 1311, 1312,
        1406, 1407, 1410, 1411, 1413,
        1501,
    };

    private static readonly HashSet<int> StaffOnlyRoleIds = new() { 1011, 1013, 1999 };

    public static IReadOnlyList<JobDefinition> All => _jobs;

    public static void Reload()
    {
        string roleDirectory = Path.Combine(PathManager.Configs.FullName, "UncomplicatedCustomRoles", Server.Port.ToString());
        List<JobDefinition> roles = new();

        if (!Directory.Exists(roleDirectory))
        {
            Logger.Error($"[SiteRP Jobs] Dossier UCR introuvable: {roleDirectory}");
            _jobs = roles.AsReadOnly();
            return;
        }

        foreach (string file in Directory.GetFiles(roleDirectory, "*.yml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                if (TryReadRole(file, out JobDefinition? job) && job is not null)
                    roles.Add(job);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SiteRP Jobs] Role ignore ({Path.GetFileName(file)}): {ex.Message}");
            }
        }

        _jobs = roles
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.UcrRoleId)
            .ToList()
            .AsReadOnly();

        Logger.Info($"[SiteRP Jobs] {_jobs.Count} roles UCR charges dans le selecteur RP.");
    }

    public static JobDefinition? Find(int roleId) => _jobs.FirstOrDefault(x => x.UcrRoleId == roleId);

    private static bool TryReadRole(string file, out JobDefinition? job)
    {
        job = null;
        string[] lines = File.ReadAllLines(file);
        int id = 0;
        int maxPlayers = 0;
        string name = string.Empty;
        string customInfo = string.Empty;

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
                int.TryParse(Value(line), out id);
            else if (line.StartsWith("name:", StringComparison.OrdinalIgnoreCase) && name.Length == 0)
                name = Unquote(Value(line));
            else if (line.StartsWith("custom_info:", StringComparison.OrdinalIgnoreCase))
                customInfo = Unquote(Value(line));
            else if (line.StartsWith("max_players:", StringComparison.OrdinalIgnoreCase))
                int.TryParse(Value(line), out maxPlayers);
        }

        if (id <= 0 || string.IsNullOrWhiteSpace(name))
            return false;

        string category = !string.IsNullOrWhiteSpace(customInfo)
            ? customInfo.Split('|')[0].Trim()
            : InferCategory(id);

        JobAccessMode access = StaffOnlyRoleIds.Contains(id)
            ? JobAccessMode.StaffOnly
            : PublicRoleIds.Contains(id) ? JobAccessMode.Public : JobAccessMode.Whitelist;

        job = new JobDefinition
        {
            UcrRoleId = id,
            Name = name,
            Category = category,
            Description = $"{category} — {name}.",
            AccessMode = access,
            MaxPlayers = maxPlayers,
            WardrobeName = GetWardrobe(id),
            SortOrder = id,
        };
        return true;
    }

    private static string Value(string line)
    {
        int split = line.IndexOf(':');
        return split < 0 ? string.Empty : line.Substring(split + 1).Trim();
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && ((value[0] == '\'' && value[value.Length - 1] == '\'') || (value[0] == '"' && value[value.Length - 1] == '"')))
            return value.Substring(1, value.Length - 2).Replace("\\\"", "\"").Replace("\\n", " ");
        return value;
    }

    private static string InferCategory(int id)
    {
        if (id >= 1600 && id < 1800) return "FORCES D'INTERVENTION";
        if (id >= 1800 && id < 1900) return "INSURRECTION DU CHAOS";
        if (id >= 1500 && id < 1600) return "CLASSE-D / COORDINATION";
        if (id >= 1400 && id < 1500) return "SECURITE";
        if (id >= 1300 && id < 1400) return "INGENIERIE / LOGISTIQUE";
        if (id >= 1200 && id < 1300) return "MEDICAL";
        if (id >= 1100 && id < 1200) return "RECHERCHE";
        if (id >= 1000 && id < 1100) return "ADMINISTRATION";
        return "SITERP";
    }

    private static string GetWardrobe(int id)
    {
        if (id >= 1401 && id <= 1403) return "SiteRP_SecurityCommand";
        if ((id >= 1404 && id <= 1408) || id == 1410 || id == 1411) return "SiteRP_Security";
        if (id == 1409) return "SiteRP_InternalResponse";
        if (id == 1412 || id == 1413) return "SiteRP_Detention";
        if (id == 1205) return "SiteRP_Paramedic";
        if (id == 1207 || id == 1313) return "SiteRP_CBRN";
        if (id == 1312) return "SiteRP_FireRescue";
        if (id == 1590) return "SiteRP_MTFCommand";
        if (id >= 1601 && id <= 1605) return "SiteRP_Epsilon11";
        if (id >= 1611 && id <= 1614) return "SiteRP_Alpha1";
        if (id >= 1621 && id <= 1624) return "SiteRP_Omega1";
        if (id >= 1631 && id <= 1635) return "SiteRP_Beta7";
        if (id >= 1641 && id <= 1645) return "SiteRP_Nu7";

        // Units without their final bespoke wardrobe still receive the clean generic MTF
        // family instead of falling back to an unrelated vanilla appearance.
        if (id >= 1651 && id <= 1774)
            return id % 10 == 1 ? "SiteRP_MTFCommand" : "SiteRP_MTF";

        if (id == 1999) return "SiteRP_Staff";
        return string.Empty;
    }
}
