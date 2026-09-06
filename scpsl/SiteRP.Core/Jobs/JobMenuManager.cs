using UserSettings.ServerSpecific;

namespace SiteRP.Core.Jobs;

/// <summary>
/// SiteRP job selector exposed in SCP:SL's Server-Specific Settings menu (M).
/// It merges its entries with settings registered by other plugins (including SLWardrobe).
/// </summary>
public static class JobMenuManager
{
    private const int DropdownId = 771001;
    private const int JoinButtonId = 771002;
    private const int InfoButtonId = 771003;

    private static JobDefinition[] _menuJobs = Array.Empty<JobDefinition>();
    private static ServerSpecificSettingBase[] _ownedSettings = Array.Empty<ServerSpecificSettingBase>();
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        JobWhitelistRepository.Load();
        RebuildJobList();

        string[] options = _menuJobs.Select(FormatOption).ToArray();
        if (options.Length == 0)
            options = new[] { "Aucun metier configure" };

        _ownedSettings = new ServerSpecificSettingBase[]
        {
            new SSGroupHeader("SITERP — CHOIX DU METIER", false, "Choisis ton metier RP. Les roles reserves necessitent une whitelist persistante."),
            new SSDropdownSetting(DropdownId, "Metier", options, 0, SSDropdownSetting.DropdownEntryType.Regular, "[PUBLIC] libre, [WL] whitelist, [STAFF] reserve a l'administration."),
            new SSButton(InfoButtonId, "Informations", "VOIR ACCES / PLACES"),
            new SSButton(JoinButtonId, "Rejoindre", "PRENDRE CE METIER", 0.75f, "Le serveur verifie la whitelist et le nombre de places avant d'attribuer le role."),
        };

        ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        // Avoid duplicates if the assembly was hot-reloaded.
        existing = existing.Where(x => x.SettingId != DropdownId && x.SettingId != JoinButtonId && x.SettingId != InfoButtonId).ToArray();
        ServerSpecificSettingsSync.DefinedSettings = existing.Concat(_ownedSettings).ToArray();
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();

        _registered = true;
        Logger.Info($"[SiteRP Jobs] Menu M active: {_menuJobs.Length} metiers exposes.");
    }

    public static void Unregister()
    {
        if (!_registered)
            return;

        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
        if (ServerSpecificSettingsSync.DefinedSettings is not null)
        {
            ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings
                .Where(x => !_ownedSettings.Contains(x) && x.SettingId != DropdownId && x.SettingId != JoinButtonId && x.SettingId != InfoButtonId)
                .ToArray();
            ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
            ServerSpecificSettingsSync.SendToAll();
        }

        _ownedSettings = Array.Empty<ServerSpecificSettingBase>();
        _registered = false;
    }

    public static void Refresh()
    {
        bool wasRegistered = _registered;
        if (wasRegistered)
            Unregister();
        Register();
    }

    private static void RebuildJobList()
    {
        _menuJobs = JobCatalog.All
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string FormatOption(JobDefinition job)
    {
        string access = job.AccessMode switch
        {
            JobAccessMode.Public => "PUBLIC",
            JobAccessMode.Whitelist => "WL",
            JobAccessMode.StaffOnly => "STAFF",
            _ => "?",
        };
        return $"[{access}] [{job.Category}] {job.Name}";
    }

    private static void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        Player? player = Player.Get(hub);
        if (player is null || !player.IsReady)
            return;

        if (setting.SettingId == DropdownId)
        {
            JobDefinition? selected = GetSelectedJob(hub);
            if (selected is not null)
                SendJobInfo(player, selected, 4);
            return;
        }

        if (setting.SettingId == InfoButtonId)
        {
            JobDefinition? selected = GetSelectedJob(hub);
            if (selected is null)
            {
                player.SendBroadcast("<color=red>SiteRP:</color> Aucun metier selectionne.", 4);
                return;
            }

            SendJobInfo(player, selected, 7);
            return;
        }

        if (setting.SettingId != JoinButtonId)
            return;

        JobDefinition? job = GetSelectedJob(hub);
        if (job is null)
        {
            player.SendBroadcast("<color=red>SiteRP:</color> Aucun metier selectionne.", 4);
            return;
        }

        bool ok = JobRuntime.TryJoin(player, job.UcrRoleId, out string response);
        string color = ok ? "green" : "red";
        player.SendBroadcast($"<b><color={color}>SITERP JOBS</color></b>\n{response}", ok ? (ushort)5 : (ushort)7);
    }

    private static JobDefinition? GetSelectedJob(ReferenceHub hub)
    {
        if (_menuJobs.Length == 0)
            return null;

        SSDropdownSetting selected = ServerSpecificSettingsSync.GetSettingOfUser<SSDropdownSetting>(hub, DropdownId);
        int index = selected.SyncSelectionIndexValidated;
        if (index < 0 || index >= _menuJobs.Length)
            return null;
        return _menuJobs[index];
    }

    private static void SendJobInfo(Player player, JobDefinition job, ushort duration)
    {
        int occupied = JobRuntime.CountPlayersOnRole(job.UcrRoleId);
        string slots = job.MaxPlayers <= 0 ? $"{occupied}/∞" : $"{occupied}/{job.MaxPlayers}";
        string access;

        switch (job.AccessMode)
        {
            case JobAccessMode.Public:
                access = "<color=green>PUBLIC</color>";
                break;
            case JobAccessMode.StaffOnly:
                access = JobRuntime.IsStaff(player)
                    ? "<color=red>STAFF (autorise)</color>"
                    : "<color=red>STAFF UNIQUEMENT</color>";
                break;
            default:
                bool allowed = JobRuntime.IsStaff(player) || JobWhitelistRepository.IsWhitelisted(JobRuntime.GetPersistentUserId(player), job.UcrRoleId);
                access = allowed
                    ? "<color=green>WHITELIST — AUTORISE</color>"
                    : "<color=orange>WHITELIST — ACCES RESERVE</color>";
                break;
        }

        player.SendBroadcast(
            $"<b>{job.Name}</b> <size=18>[ID {job.UcrRoleId}]</size>\n" +
            $"{job.Category} | Places: {slots} | {access}\n" +
            $"<size=18>{job.Description}</size>",
            duration);
    }
}
