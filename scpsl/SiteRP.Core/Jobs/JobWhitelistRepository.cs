using System.Text.Json;
using LabApi.Loader.Features.Paths;

namespace SiteRP.Core.Jobs;

public static class JobWhitelistRepository
{
    private static readonly object Sync = new();
    private static WhitelistStore _store = new();

    public static string DirectoryPath => Path.Combine(PathManager.Configs.FullName, Server.Port.ToString(), "SiteRP");
    public static string FilePath => Path.Combine(DirectoryPath, "JobWhitelists.json");

    public static void Load()
    {
        lock (Sync)
        {
            Directory.CreateDirectory(DirectoryPath);
            if (!File.Exists(FilePath))
            {
                _store = new WhitelistStore();
                SaveUnsafe();
                return;
            }

            try
            {
                WhitelistStore? parsed = JsonSerializer.Deserialize<WhitelistStore>(File.ReadAllText(FilePath));
                _store = parsed ?? new WhitelistStore();
                _store.Entries ??= new List<WhitelistEntry>();
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
                GrantedBy = grantedBy,
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
        string json = JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(temp, json);
        File.Move(temp, FilePath, true);
    }

    private static WhitelistEntry Clone(WhitelistEntry entry) => new()
    {
        SteamId64 = entry.SteamId64,
        RoleId = entry.RoleId,
        GrantedBy = entry.GrantedBy,
        GrantedAtUtc = entry.GrantedAtUtc,
    };
}
