using LabApi.Loader.Features.Paths;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Persistent acceptance store for the mandatory SiteRP/DarkRP rules.
/// Acceptance is versioned: changing CurrentVersion automatically requires players to accept again.
/// </summary>
public static class SiteRpRulesRepository
{
    public const string CurrentVersion = "1.0";
    public const int MinimumReadSeconds = 12;

    private static readonly object Sync = new();
    private static readonly Dictionary<string, RulesAcceptance> Accepted = new(StringComparer.OrdinalIgnoreCase);

    public static string DirectoryPath => Path.Combine(PathManager.Configs.FullName, Server.Port.ToString(), "SiteRP");
    public static string FilePath => Path.Combine(DirectoryPath, "RulesAccepted.txt");

    public static IReadOnlyList<string> Pages { get; } = new[]
    {
        "<b>1. ROLEPLAY / DARKRP</b>\n\n" +
        "• Reste en personnage pendant les scènes RP. Le FailRP volontaire est interdit.\n" +
        "• FearRP : protège ta vie et réagis de façon crédible à une menace armée.\n" +
        "• MetaGaming interdit : aucune information Discord, stream, OOC ou spectateur ne peut être utilisée en RP.\n" +
        "• PowerGaming interdit : ne force pas une action impossible ou le résultat d'une scène sur un autre joueur.\n" +
        "• NLR : après une mort / nouvelle vie, n'utilise pas les informations de ton ancienne vie et ne reviens pas te venger immédiatement.",

        "<b>2. VIOLENCE / ARMES</b>\n\n" +
        "• RDM, FreeKill et MassRDM sont interdits. Toute violence doit avoir une raison RP valable.\n" +
        "• L'usage d'une arme doit respecter ton métier, la hiérarchie et les règles d'engagement.\n" +
        "• Fouilles, menottages, confiscations et arrestations aléatoires sont interdits.\n" +
        "• N'abuse pas du friendly-fire, des grenades, Tesla, portes, ascenseurs ou autres mécaniques pour tuer/gêner sans raison RP.\n" +
        "• Une scène hostile doit laisser une chance raisonnable de comprendre et de réagir, sauf danger immédiat crédible.",

        "<b>3. FONDATION / SCP</b>\n\n" +
        "• Aucun SCP ne doit être libéré sans raison RP autorisée, événement ou état de brèche validé.\n" +
        "• Respecte les niveaux d'accréditation, zones restreintes, ordres légitimes et procédures de confinement.\n" +
        "• Pas d'ogive, décontamination, évacuation générale, CASSIE ou alarme critique sans autorisation RP adaptée.\n" +
        "• Les tests scientifiques dangereux doivent être encadrés et justifiés ; la Sécurité accompagne lorsque nécessaire.\n" +
        "• La Sécurité protège le Site : elle ne harcèle pas gratuitement les Classe-D. Les Classe-D peuvent comploter/fuir, mais pas FreeKill ou rush suicidaire sans logique RP.",

        "<b>4. COMMUNICATION / COMMUNAUTÉ</b>\n\n" +
        "• Micspam, soundboard abusif, flood, harcèlement, menaces réelles et propos haineux sont interdits.\n" +
        "• Sépare clairement l'OOC de l'IC. Les insultes RP ne justifient jamais le harcèlement hors-RP.\n" +
        "• Usurper l'identité d'un membre du staff ou se faire passer pour un modérateur est interdit.\n" +
        "• Exploits, duplication, sortie de map, abus de bugs et contournement des systèmes du serveur sont interdits. Signale les bugs au staff.\n" +
        "• Cheats, clients/modifications donnant un avantage injuste ou automatisations interdites = sanction immédiate.",

        "<b>5. STAFF / ACCEPTATION</b>\n\n" +
        "• Pendant une intervention de modération, suis les instructions du staff et mets la scène RP en pause si demandé.\n" +
        "• Utilise les reports/tickets : ne règle pas une infraction par vengeance ou justice personnelle hors-RP.\n" +
        "• Le staff peut annuler, corriger ou recommencer une scène lorsque c'est nécessaire pour réparer un abus.\n" +
        "• Les sanctions dépendent de la gravité, des récidives et de l'intention.\n\n" +
        "<color=#73D673><b>En validant, tu confirmes avoir lu et accepté le règlement SiteRP.</b></color>"
    };

    public static void Load()
    {
        lock (Sync)
        {
            Accepted.Clear();
            Directory.CreateDirectory(DirectoryPath);

            if (!File.Exists(FilePath))
            {
                SaveUnsafe();
                return;
            }

            try
            {
                foreach (string raw in File.ReadAllLines(FilePath))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    string[] parts = line.Split('|');
                    if (parts.Length < 2)
                        continue;

                    string version = parts[0].Trim();
                    string steamId64 = parts[1].Trim();
                    if (steamId64.Length == 0)
                        continue;

                    Accepted[steamId64] = new RulesAcceptance
                    {
                        Version = version,
                        SteamId64 = steamId64,
                        AcceptedAtUtc = parts.Length >= 3 ? parts[2].Trim() : string.Empty,
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SiteRP Rules] Impossible de charger {FilePath}: {ex}");
                Accepted.Clear();
            }
        }
    }

    public static bool HasAccepted(Player player) => HasAccepted(JobRuntime.GetPersistentUserId(player));

    public static bool HasAccepted(string steamId64)
    {
        lock (Sync)
            return Accepted.TryGetValue(steamId64, out RulesAcceptance? entry) &&
                string.Equals(entry.Version, CurrentVersion, StringComparison.OrdinalIgnoreCase);
    }

    public static void Accept(Player player)
    {
        string steamId64 = JobRuntime.GetPersistentUserId(player);
        if (string.IsNullOrWhiteSpace(steamId64))
            return;

        lock (Sync)
        {
            Accepted[steamId64] = new RulesAcceptance
            {
                Version = CurrentVersion,
                SteamId64 = steamId64,
                AcceptedAtUtc = DateTime.UtcNow.ToString("O"),
            };
            SaveUnsafe();
        }
    }

    private static void SaveUnsafe()
    {
        Directory.CreateDirectory(DirectoryPath);
        string temp = FilePath + ".tmp";
        List<string> lines = new()
        {
            "# SiteRP mandatory rules acceptance",
            "# rulesVersion|steamId64|acceptedAtUtc",
        };

        foreach (RulesAcceptance entry in Accepted.Values.OrderBy(x => x.SteamId64, StringComparer.OrdinalIgnoreCase))
            lines.Add($"{entry.Version}|{entry.SteamId64}|{entry.AcceptedAtUtc}");

        File.WriteAllLines(temp, lines);
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        File.Move(temp, FilePath);
    }

    private sealed class RulesAcceptance
    {
        public string Version { get; set; } = string.Empty;
        public string SteamId64 { get; set; } = string.Empty;
        public string AcceptedAtUtc { get; set; } = string.Empty;
    }
}
