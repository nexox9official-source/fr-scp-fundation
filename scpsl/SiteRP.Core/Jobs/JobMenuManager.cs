using UserSettings.ServerSpecific;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Native SCP:SL Server-Specific Settings job selector.
/// This is deliberately separate from the vanilla Remote Admin M panel: UCR roles are
/// server plugin roles, not vanilla RoleTypeId entries, and therefore are not injected
/// into Northwood's built-in RA role list.
/// </summary>
public static class JobMenuManager
{
    private const int CategoryDropdownId = 771000;
    private const int JobDropdownId = 771001;
    private const int JoinButtonId = 771002;
    private const int InfoButtonId = 771003;

    private static JobDefinition[] _menuJobs = Array.Empty<JobDefinition>();
    private static string[] _categories = Array.Empty<string>();
    private static ServerSpecificSettingBase[] _ownedSettings = Array.Empty<ServerSpecificSettingBase>();
    private static ServerSpecificSettingBase[] _foreignSettings = Array.Empty<ServerSpecificSettingBase>();
    private static readonly Dictionary<ReferenceHub, int> PlayerCategoryIndexes = new();
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        JobWhitelistRepository.Load();
        JobCatalog.Reload();
        RebuildJobList();

        _categories = _menuJobs.Select(x => x.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (_categories.Length == 0)
            _categories = new[] { "AUCUN METIER" };

        _ownedSettings = CreateMenuSettings(0);

        ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        _foreignSettings = existing
            .Where(x => x.SettingId != CategoryDropdownId && x.SettingId != JobDropdownId && x.SettingId != JoinButtonId && x.SettingId != InfoButtonId)
            .ToArray();

        ServerSpecificSettingsSync.DefinedSettings = _foreignSettings.Concat(_ownedSettings).ToArray();
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;

        // Do NOT resend settings from ServerOnStatusReceived. Sending SSS settings can itself
        // cause another client status/value sync. With other SSS plugins (SLWardrobe in
        // particular), that created a feedback loop and thousands of cosmetic toggle events.
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();

        _registered = true;
        Logger.Info($"[SiteRP Jobs] Selecteur Server-Specific actif: {_menuJobs.Length} metiers, {_categories.Length} categories, cooldown {JobRuntime.JobChangeCooldownSeconds}s.");
    }

    public static void Unregister()
    {
        if (!_registered)
            return;

        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
        PlayerCategoryIndexes.Clear();

        ServerSpecificSettingsSync.DefinedSettings = _foreignSettings;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();

        _ownedSettings = Array.Empty<ServerSpecificSettingBase>();
        _foreignSettings = Array.Empty<ServerSpecificSettingBase>();
        _registered = false;
    }

    public static void Refresh()
    {
        if (_registered)
            Unregister();
        Register();
    }

    private static void RebuildJobList()
    {
        _menuJobs = JobCatalog.All
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.UcrRoleId)
            .ToArray();
    }

    private static ServerSpecificSettingBase[] CreateMenuSettings(int categoryIndex)
    {
        categoryIndex = Math.Max(0, Math.Min(categoryIndex, _categories.Length - 1));
        JobDefinition[] jobs = GetJobsForCategory(categoryIndex);
        string[] jobOptions = jobs.Select(FormatJobOnly).ToArray();
        if (jobOptions.Length == 0)
            jobOptions = new[] { "Aucun metier dans cette categorie" };

        return new ServerSpecificSettingBase[]
        {
            new SSGroupHeader("SITERP — CHOIX DU METIER", false, $"Interface RP native. Choisis uniquement un metier auquel tu as acces. Changement: {JobRuntime.JobChangeCooldownSeconds}s de cooldown."),
            new SSDropdownSetting(CategoryDropdownId, "Departement / Unite", _categories, categoryIndex, SSDropdownSetting.DropdownEntryType.Hybrid, "Change de categorie pour filtrer la liste des metiers."),
            new SSDropdownSetting(JobDropdownId, "Metier / Grade", jobOptions, 0, SSDropdownSetting.DropdownEntryType.Regular, "[PUBLIC] libre, [WL] whitelist persistante, [STAFF] administration uniquement."),
            new SSButton(InfoButtonId, "Informations", "VOIR ACCES / PLACES"),
            new SSButton(JoinButtonId, "Changer de metier", "VALIDER LE METIER", 0.75f, $"Whitelist, places et cooldown {JobRuntime.JobChangeCooldownSeconds}s sont controles cote serveur."),
        };
    }

    private static string FormatJobOnly(JobDefinition job) => $"[{AccessLabel(job)}] {job.Name}";

    private static string AccessLabel(JobDefinition job) => job.AccessMode switch
    {
        JobAccessMode.Public => "PUBLIC",
        JobAccessMode.Whitelist => "WL",
        JobAccessMode.StaffOnly => "STAFF",
        _ => "?",
    };

    private static JobDefinition[] GetJobsForCategory(int categoryIndex)
    {
        if (_categories.Length == 0)
            return Array.Empty<JobDefinition>();

        categoryIndex = Math.Max(0, Math.Min(categoryIndex, _categories.Length - 1));
        string category = _categories[categoryIndex];
        return _menuJobs.Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private static int GetCategoryIndex(ReferenceHub hub)
    {
        if (PlayerCategoryIndexes.TryGetValue(hub, out int index))
            return Math.Max(0, Math.Min(index, _categories.Length - 1));
        return 0;
    }

    private static JobDefinition? GetSelectedJob(ReferenceHub hub)
    {
        JobDefinition[] jobs = GetJobsForCategory(GetCategoryIndex(hub));
        if (jobs.Length == 0)
            return null;

        SSDropdownSetting selected = ServerSpecificSettingsSync.GetSettingOfUser<SSDropdownSetting>(hub, JobDropdownId);
        int index = selected.SyncSelectionIndexRaw;
        if (index < 0 || index >= jobs.Length)
            return jobs[0];
        return jobs[index];
    }

    private static void SendCustomizedMenu(ReferenceHub hub, int categoryIndex)
    {
        if (!_registered || hub is null)
            return;

        PlayerCategoryIndexes[hub] = categoryIndex;
        ServerSpecificSettingBase[] personalized = _foreignSettings.Concat(CreateMenuSettings(categoryIndex)).ToArray();
        ServerSpecificSettingsSync.SendToPlayer(hub, personalized);
    }

    private static void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        Player? player = Player.Get(hub);
        if (player is null || !player.IsReady)
            return;

        if (setting.SettingId == CategoryDropdownId && setting is SSDropdownSetting categorySetting)
        {
            int categoryIndex = categorySetting.SyncSelectionIndexRaw;
            categoryIndex = Math.Max(0, Math.Min(categoryIndex, _categories.Length - 1));

            if (GetCategoryIndex(hub) != categoryIndex)
                SendCustomizedMenu(hub, categoryIndex);
            else
                PlayerCategoryIndexes[hub] = categoryIndex;
            return;
        }

        if (setting.SettingId == JobDropdownId)
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
        player.SendBroadcast($"<b><color={color}>SITERP METIERS</color></b>\n{response}", ok ? (ushort)5 : (ushort)7);
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

        int cooldown = JobRuntime.GetRemainingCooldown(player);
        string cooldownText = cooldown > 0 ? $"<color=orange>{cooldown}s</color>" : "<color=green>pret</color>";
        string skin = string.IsNullOrWhiteSpace(job.WardrobeName) ? "standard" : job.WardrobeName;

        player.SendBroadcast(
            $"<b>{job.Name}</b> <size=18>[ID {job.UcrRoleId}]</size>\n" +
            $"{job.Category} | Places: {slots} | {access}\n" +
            $"Cooldown: {cooldownText} | Tenue: {skin}\n" +
            $"<size=18>{job.Description}</size>",
            duration);
    }
}
