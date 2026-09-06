using LabApi.Loader.Features.Paths;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Dependency-free persistent whitelist store. One record per line:
/// roleId|steamId64|grantedBy|timestampUtc
/// </summary>
public static class JobWhitelistRepository
{
    private static readonly object Sync = new();
    private static WhitelistStore _store = new();

    public static string DirectoryPath => Path.Combine(PathManager.Configs.FullName, Server.Port.ToString(), "SiteRP");
    public static string FilePath => Path.Combine(DirectoryPath, "JobWhitelists.txt");

    public static void Load()
    {
        lock (Sync)
        {
            Directory.CreateDirectory(DirectoryPath);
            _store = new WhitelistStore();

            if (!File.Exists(FilePath))
            {
                SaveUnsafe();
                return;
            }

            try
            {
                foreach (string rawLine in File.ReadAllLines(FilePath))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    string[] parts = line.Split('|');
                    if (parts.Length < 2 || !int.TryParse(parts[0], out int roleId))
                        continue;

                    _store.Entries.Add(new WhitelistEntry
                    {
                        RoleId = roleId,
                        SteamId64 = parts[1].Trim(),
                        GrantedBy = parts.Length >= 3 ? Unescape(parts[2]) : string.Empty,
                        GrantedAtUtc = parts.Length >= 4 ? parts[3].Trim() : string.Empty,
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SiteRP Jobs] Impossible de charger {FilePath}: {ex}");
                _store = new WhitelistStore();
            }
        }
    }

    public static bool IsWhitelisted(string steamId64, int roleId)
    {
        lock (Sync)
            return _store.Entries.Any(x => x.RoleId == roleId && string.Equals(x.SteamId64, steamId64, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<WhitelistEntry> GetForPlayer(string steamId64)
    {
        lock (Sync)
            return _store.Entries.Where(x => string.Equals(x.SteamId64, steamId64, StringComparison.OrdinalIgnoreCase)).Select(Clone).ToArray();
    }

    public static IReadOnlyList<WhitelistEntry> GetForRole(int roleId)
    {
        lock (Sync)
            return _store.Entries.Where(x => x.RoleId == roleId).Select(Clone).ToArray();
    }

    public static bool Grant(string steamId64, int roleId, string grantedBy)
    {
        if (string.IsNullOrWhiteSpace(steamId64))
            return false;

        lock (Sync)
        {
            if (_store.Entries.Any(x => x.RoleId == roleId && string.Equals(x.SteamId64, steamId64, StringComparison.OrdinalIgnoreCase)))
                return false;

            _store.Entries.Add(new WhitelistEntry
            {
                SteamId64 = steamId64.Trim(),
                RoleId = roleId,
                GrantedBy = grantedBy ?? string.Empty,
                GrantedAtUtc = DateTime.UtcNow.ToString("O"),
            });
            SaveUnsafe();
            return true;
        }
    }

    public static bool Revoke(string steamId64, int roleId)
    {
        lock (Sync)
        {
            int removed = _store.Entries.RemoveAll(x => x.RoleId == roleId && string.Equals(x.SteamId64, steamId64, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
                SaveUnsafe();
            return removed > 0;
        }
    }

    private static void SaveUnsafe()
    {
        Directory.CreateDirectory(DirectoryPath);
        string temp = FilePath + ".tmp";
        List<string> lines = new()
        {
            "# SiteRP persistent job whitelists",
            "# roleId|steamId64|grantedBy|timestampUtc",
        };

        foreach (WhitelistEntry entry in _store.Entries.OrderBy(x => x.RoleId).ThenBy(x => x.SteamId64, StringComparer.OrdinalIgnoreCase))
            lines.Add($"{entry.RoleId}|{entry.SteamId64}|{Escape(entry.GrantedBy)}|{entry.GrantedAtUtc}");

        File.WriteAllLines(temp, lines);
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        File.Move(temp, FilePath);
    }

    private static string Escape(string value) => (value ?? string.Empty).Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
    private static string Unescape(string value) => value ?? string.Empty;

    private static WhitelistEntry Clone(WhitelistEntry entry) => new()
    {
        SteamId64 = entry.SteamId64,
        RoleId = entry.RoleId,
        GrantedBy = entry.GrantedBy,
        GrantedAtUtc = entry.GrantedAtUtc,
    };
}
