using UserSettings.ServerSpecific;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Native SiteRP interface implemented with SCP:SL Server-Specific Settings.
/// The radio is deliberately never used for navigation.
/// </summary>
public static class JobMenuManager
{
    private const int RulesNavId = 771100;
    private const int JobsNavId = 771101;
    private const int StaffNavId = 771102;
    private const int RulesTextId = 771110;
    private const int AcceptRulesId = 771111;
    private const int CategoryId = 771120;
    private const int JobInfoId = 771121;
    private const int JoinJobId = 771122;
    private const int RefreshJobsId = 771123;
    private const int StaffTargetId = 771130;
    private const int StaffRoleId = 771131;
    private const int StaffInfoId = 771132;
    private const int StaffGrantId = 771133;
    private const int StaffRevokeId = 771134;
    private const int StaffRefreshId = 771135;
    private const int FirstCategoryJobId = 772000;

    private static readonly Dictionary<string, PlayerUiState> States = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, List<JobDefinition>> JobsByDropdown = new();
    private static readonly List<string> Categories = new();
    private static readonly List<string> StaffTargetIds = new();
    private static readonly List<JobDefinition> StaffRoles = new();

    private static ServerSpecificSettingBase[] _ownedSettings = Array.Empty<ServerSpecificSettingBase>();
    private static ServerSpecificSettingBase[] _foreignSettings = Array.Empty<ServerSpecificSettingBase>();
    private static SSDropdownSetting? _categorySetting;
    private static SSDropdownSetting? _staffTargetSetting;
    private static SSDropdownSetting? _staffRoleSetting;
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        JobWhitelistRepository.Load();
        SiteRpRulesRepository.Load();
        JobCatalog.Reload();

        ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        _foreignSettings = existing.Where(x => x.SettingId < 771100 || x.SettingId > 772999).ToArray();
        BuildDefinitions();

        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();
        _registered = true;

        Logger.Info($"[SiteRP UI] Interface native M active: {JobCatalog.All.Count} metiers. La radio n'est plus utilisee par SiteRP.");
    }

    public static void Unregister()
    {
        if (!_registered)
            return;

        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;
        ServerSpecificSettingsSync.DefinedSettings = _foreignSettings;
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();
        States.Clear();
        _registered = false;
    }

    public static void Refresh()
    {
        JobWhitelistRepository.Load();
        SiteRpRulesRepository.Load();
        JobCatalog.Reload();
        BuildDefinitions();
        ServerSpecificSettingsSync.Version = Math.Max(1, ServerSpecificSettingsSync.Version + 1);
        ServerSpecificSettingsSync.SendToAll();
    }

    public static void CleanupPlayer(Player player)
    {
        if (player is null)
            return;
        States.Remove(JobRuntime.GetPersistentUserId(player));
    }

    public static void ShowRules(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        PlayerUiState state = GetState(player);
        if (state.RulesOpenedAtUtc == default)
            state.RulesOpenedAtUtc = DateTime.UtcNow;

        string rules = string.Join("\n\n<size=16>────────────────────────────</size>\n\n", SiteRpRulesRepository.Pages);
        string status = SiteRpRulesRepository.HasAccepted(player)
            ? "<color=#73D673><b>REGLEMENT DEJA ACCEPTE</b></color>"
            : "<color=#FFB84D><b>LECTURE ET ACCEPTATION OBLIGATOIRES AVANT LE DEPLOIEMENT</b></color>";

        List<ServerSpecificSettingBase> page = Navigation(player);
        page.Add(new SSGroupHeader("SITERP — REGLEMENT", false, "Reglement obligatoire du serveur."));
        page.Add(new SSTextArea(RulesTextId, $"{status}\n\n{rules}"));
        page.Add(new SSButton(
            AcceptRulesId,
            "Acceptation",
            SiteRpRulesRepository.HasAccepted(player) ? "DEJA ACCEPTE" : "J'ACCEPTE LE REGLEMENT",
            0.5f,
            "L'acceptation est sauvegardee par SteamID64 et version de reglement."));
        Send(player, page);
    }

    public static void ShowJobs(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        if (!SiteRpRulesRepository.HasAccepted(player))
        {
            ShowRules(player);
            Prompt(player, "Accepte d'abord le reglement dans le menu M.");
            return;
        }

        PlayerUiState state = GetState(player);
        if (Categories.Count == 0 || _categorySetting is null)
        {
            Prompt(player, "Aucun metier SiteRP n'est charge.");
            return;
        }

        state.CategoryIndex = ClampIndex(state.CategoryIndex, Categories.Count);
        int dropdownId = FirstCategoryJobId + state.CategoryIndex;
        List<JobDefinition> jobs = GetJobsForDropdown(dropdownId);

        if (jobs.Count > 0 && !jobs.Any(x => x.UcrRoleId == state.SelectedRoleId))
            state.SelectedRoleId = jobs[0].UcrRoleId;

        JobDefinition? selected = jobs.FirstOrDefault(x => x.UcrRoleId == state.SelectedRoleId) ?? jobs.FirstOrDefault();
        string info = selected is null ? "Aucun role dans cette categorie." : DescribeJob(player, selected);

        List<ServerSpecificSettingBase> page = Navigation(player);
        page.Add(new SSGroupHeader(
            "SITERP — CHOIX DU METIER",
            false,
            "Choisis ton departement puis ton role. Les acces reserves sont controles cote serveur."));
        page.Add(_categorySetting);

        ServerSpecificSettingBase? roleDropdown = _ownedSettings.FirstOrDefault(x => x.SettingId == dropdownId);
        if (jobs.Count > 0 && roleDropdown is not null)
            page.Add(roleDropdown);

        page.Add(new SSTextArea(JobInfoId, info));
        page.Add(new SSButton(JoinJobId, "Deploiement", "REJOINDRE CE METIER", 0.35f));
        page.Add(new SSButton(RefreshJobsId, "Actualiser", "ACTUALISER LES PLACES"));
        Send(player, page);
    }

    public static void ShowStaff(Player player)
    {
        if (player is null || !player.IsReady || !JobRuntime.IsStaff(player))
        {
            if (player is not null)
                Prompt(player, "Acces staff refuse.");
            return;
        }

        BuildDefinitions();
        PlayerUiState state = GetState(player);
        state.StaffTargetIndex = ClampIndex(state.StaffTargetIndex, StaffTargetIds.Count);
        state.StaffRoleIndex = ClampIndex(state.StaffRoleIndex, StaffRoles.Count);

        string targetId = StaffTargetIds.Count == 0 ? string.Empty : StaffTargetIds[state.StaffTargetIndex];
        JobDefinition? selectedRole = StaffRoles.Count == 0 ? null : StaffRoles[state.StaffRoleIndex];
        bool has = selectedRole is not null && targetId.Length > 0 && JobWhitelistRepository.IsWhitelisted(targetId, selectedRole.UcrRoleId);

        string info = selectedRole is null
            ? "Aucun role whitelistable charge."
            : $"Joueur: <b>{Escape(targetId)}</b>\nRole: <b>{Escape(selectedRole.Name)}</b> ({selectedRole.UcrRoleId})\nEtat: {(has ? "<color=#73D673>WHITELIST PRESENTE</color>" : "<color=#FFB84D>NON WHITELISTE</color>")}";

        List<ServerSpecificSettingBase> page = Navigation(player);
        page.Add(new SSGroupHeader("SITERP — GESTION DES WHITELISTS", false, "Ajout/retrait immediat et persistant."));
        if (_staffTargetSetting is not null)
            page.Add(_staffTargetSetting);
        if (_staffRoleSetting is not null)
            page.Add(_staffRoleSetting);
        page.Add(new SSTextArea(StaffInfoId, info));
        page.Add(new SSButton(StaffGrantId, "Autoriser", "AJOUTER LA WHITELIST", 0.35f));
        page.Add(new SSButton(StaffRevokeId, "Retirer", "RETIRER LA WHITELIST", 0.35f));
        page.Add(new SSButton(StaffRefreshId, "Actualiser", "ACTUALISER JOUEURS / ROLES"));
        Send(player, page);
    }

    private static void BuildDefinitions()
    {
        Categories.Clear();
        Categories.AddRange(
            JobCatalog.All
                .Where(x => x.AccessMode != JobAccessMode.StaffOnly)
                .Select(x => x.Category)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x));

        JobsByDropdown.Clear();
        List<ServerSpecificSettingBase> owned = new()
        {
            new SSButton(RulesNavId, "Navigation", "REGLEMENT"),
            new SSButton(JobsNavId, "Navigation", "METIERS"),
            new SSButton(StaffNavId, "Navigation", "WHITELISTS STAFF"),
            new SSTextArea(RulesTextId, string.Empty),
            new SSButton(AcceptRulesId, "Acceptation", "J'ACCEPTE"),
            new SSTextArea(JobInfoId, string.Empty),
            new SSButton(JoinJobId, "Deploiement", "REJOINDRE"),
            new SSButton(RefreshJobsId, "Actualiser", "ACTUALISER"),
            new SSTextArea(StaffInfoId, string.Empty),
            new SSButton(StaffGrantId, "Autoriser", "AJOUTER"),
            new SSButton(StaffRevokeId, "Retirer", "RETIRER"),
            new SSButton(StaffRefreshId, "Actualiser", "ACTUALISER"),
        };

        _categorySetting = new SSDropdownSetting(
            CategoryId,
            "Departement / unite",
            Categories.Count == 0 ? new[] { "Aucun metier" } : Categories.ToArray(),
            0,
            SSDropdownSetting.DropdownEntryType.Hybrid);
        owned.Add(_categorySetting);

        for (int i = 0; i < Categories.Count; i++)
        {
            List<JobDefinition> jobs = JobCatalog.All
                .Where(x => x.AccessMode != JobAccessMode.StaffOnly && string.Equals(x.Category, Categories[i], StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.SortOrder)
                .ToList();

            int id = FirstCategoryJobId + i;
            JobsByDropdown[id] = jobs;
            string[] options = jobs.Select(JobOption).ToArray();
            owned.Add(new SSDropdownSetting(
                id,
                "Metier / grade",
                options.Length == 0 ? new[] { "Aucun role" } : options,
                0,
                SSDropdownSetting.DropdownEntryType.Hybrid));
        }

        StaffTargetIds.Clear();
        List<Player> online = Player.ReadyList.OrderBy(x => x.PlayerId).ToList();
        StaffTargetIds.AddRange(online.Select(JobRuntime.GetPersistentUserId));
        string[] targets = online
            .Select(x => $"#{x.PlayerId} — {x.Nickname} — {JobRuntime.GetPersistentUserId(x)}")
            .ToArray();

        _staffTargetSetting = new SSDropdownSetting(
            StaffTargetId,
            "Joueur",
            targets.Length == 0 ? new[] { "Aucun joueur en ligne" } : targets,
            0,
            SSDropdownSetting.DropdownEntryType.Hybrid);
        owned.Add(_staffTargetSetting);

        StaffRoles.Clear();
        StaffRoles.AddRange(JobCatalog.All.Where(x => x.AccessMode == JobAccessMode.Whitelist).OrderBy(x => x.SortOrder));
        string[] staffRoleOptions = StaffRoles.Select(x => $"{x.UcrRoleId} — {x.Category} — {x.Name}").ToArray();

        _staffRoleSetting = new SSDropdownSetting(
            StaffRoleId,
            "Role a autoriser",
            staffRoleOptions.Length == 0 ? new[] { "Aucun role" } : staffRoleOptions,
            0,
            SSDropdownSetting.DropdownEntryType.Hybrid);
        owned.Add(_staffRoleSetting);

        _ownedSettings = owned.ToArray();
        ServerSpecificSettingsSync.DefinedSettings = _foreignSettings.Concat(_ownedSettings).ToArray();
    }

    private static List<ServerSpecificSettingBase> Navigation(Player player)
    {
        List<ServerSpecificSettingBase> result = new()
        {
            _ownedSettings.First(x => x.SettingId == RulesNavId),
            _ownedSettings.First(x => x.SettingId == JobsNavId),
        };

        if (JobRuntime.IsStaff(player))
            result.Add(_ownedSettings.First(x => x.SettingId == StaffNavId));
        return result;
    }

    private static void Send(Player player, List<ServerSpecificSettingBase> page)
    {
        ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub, page.ToArray());
    }

    private static void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        Player? player = Player.Get(hub);
        if (player is null || !player.IsReady)
            return;

        PlayerUiState state = GetState(player);
        int id = setting.SettingId;

        if (id == RulesNavId && setting is SSButton)
        {
            ShowRules(player);
            return;
        }
        if (id == JobsNavId && setting is SSButton)
        {
            ShowJobs(player);
            return;
        }
        if (id == StaffNavId && setting is SSButton)
        {
            ShowStaff(player);
            return;
        }

        if (id == AcceptRulesId && setting is SSButton)
        {
            if (SiteRpRulesRepository.HasAccepted(player))
            {
                ShowJobs(player);
                return;
            }

            int elapsed = (int)(DateTime.UtcNow - state.RulesOpenedAtUtc).TotalSeconds;
            if (elapsed < SiteRpRulesRepository.MinimumReadSeconds)
            {
                Prompt(player, $"Lis le reglement avant d'accepter : encore {SiteRpRulesRepository.MinimumReadSeconds - elapsed}s minimum.");
                return;
            }

            SiteRpRulesRepository.Accept(player);
            Prompt(player, "Reglement accepte et sauvegarde. Choisis maintenant ton metier dans M.", true);
            ShowJobs(player);
            return;
        }

        if (id == CategoryId && setting is SSDropdownSetting categoryDropdown)
        {
            int next = ClampIndex(categoryDropdown.SyncSelectionIndexValidated, Categories.Count);
            if (next == state.CategoryIndex)
                return;

            state.CategoryIndex = next;
            state.SelectedRoleId = 0;
            ShowJobs(player);
            return;
        }

        if (JobsByDropdown.TryGetValue(id, out List<JobDefinition>? jobs) && setting is SSDropdownSetting jobDropdown)
        {
            if (jobs.Count > 0)
            {
                int index = ClampIndex(jobDropdown.SyncSelectionIndexValidated, jobs.Count);
                state.SelectedRoleId = jobs[index].UcrRoleId;
            }
            return;
        }

        if (id == JoinJobId && setting is SSButton)
        {
            if (!SiteRpRulesRepository.HasAccepted(player))
            {
                ShowRules(player);
                return;
            }

            if (state.SelectedRoleId <= 0)
            {
                int dropdownId = FirstCategoryJobId + state.CategoryIndex;
                List<JobDefinition> current = GetJobsForDropdown(dropdownId);
                if (current.Count > 0)
                    state.SelectedRoleId = current[0].UcrRoleId;
            }

            bool initial = !SiteRpInteractiveUi.IsDeployed(player);
            bool ok = JobRuntime.TryJoin(player, state.SelectedRoleId, out string response, initial);
            Prompt(player, response, ok);
            if (ok && initial)
                SiteRpInteractiveUi.MarkDeployed(player);
            ShowJobs(player);
            return;
        }

        if (id == RefreshJobsId && setting is SSButton)
        {
            JobCatalog.Reload();
            BuildDefinitions();
            ShowJobs(player);
            return;
        }

        if (!JobRuntime.IsStaff(player))
            return;

        if (id == StaffTargetId && setting is SSDropdownSetting targetDropdown)
        {
            state.StaffTargetIndex = ClampIndex(targetDropdown.SyncSelectionIndexValidated, StaffTargetIds.Count);
            return;
        }

        if (id == StaffRoleId && setting is SSDropdownSetting roleDropdownSetting)
        {
            state.StaffRoleIndex = ClampIndex(roleDropdownSetting.SyncSelectionIndexValidated, StaffRoles.Count);
            return;
        }

        if (id == StaffRefreshId && setting is SSButton)
        {
            ShowStaff(player);
            return;
        }

        if ((id == StaffGrantId || id == StaffRevokeId) && setting is SSButton)
        {
            if (StaffTargetIds.Count == 0 || StaffRoles.Count == 0)
            {
                Prompt(player, "Joueur ou role introuvable.");
                return;
            }

            string targetId = StaffTargetIds[ClampIndex(state.StaffTargetIndex, StaffTargetIds.Count)];
            JobDefinition selectedRole = StaffRoles[ClampIndex(state.StaffRoleIndex, StaffRoles.Count)];

            bool changed = id == StaffGrantId
                ? JobWhitelistRepository.Grant(targetId, selectedRole.UcrRoleId, $"{player.Nickname} ({JobRuntime.GetPersistentUserId(player)})")
                : JobWhitelistRepository.Revoke(targetId, selectedRole.UcrRoleId);

            Prompt(player, changed ? "Whitelist modifiee et sauvegardee." : "Aucun changement.", changed);
            ShowStaff(player);
        }
    }

    private static PlayerUiState GetState(Player player)
    {
        string id = JobRuntime.GetPersistentUserId(player);
        if (!States.TryGetValue(id, out PlayerUiState? state))
        {
            state = new PlayerUiState { RulesOpenedAtUtc = DateTime.UtcNow };
            States[id] = state;
        }
        return state;
    }

    private static List<JobDefinition> GetJobsForDropdown(int dropdownId)
    {
        return JobsByDropdown.TryGetValue(dropdownId, out List<JobDefinition>? jobs)
            ? jobs
            : new List<JobDefinition>();
    }

    private static int ClampIndex(int value, int count)
    {
        if (count <= 0)
            return 0;
        if (value < 0)
            return 0;
        return value >= count ? count - 1 : value;
    }

    private static string JobOption(JobDefinition job)
    {
        string access = job.AccessMode == JobAccessMode.Public ? "PUBLIC" : "WHITELIST";
        return $"{job.UcrRoleId} — {job.Name} — {access}";
    }

    private static string DescribeJob(Player player, JobDefinition job)
    {
        int current = JobRuntime.CountPlayersOnRole(job.UcrRoleId);
        string slots = job.MaxPlayers <= 0 ? $"{current}/∞" : $"{current}/{job.MaxPlayers}";
        bool whitelisted = job.AccessMode != JobAccessMode.Whitelist ||
                           JobRuntime.IsStaff(player) ||
                           JobWhitelistRepository.IsWhitelisted(JobRuntime.GetPersistentUserId(player), job.UcrRoleId);

        string access = job.AccessMode switch
        {
            JobAccessMode.Public => "<color=#73D673>PUBLIC</color>",
            JobAccessMode.Whitelist when whitelisted => "<color=#73D673>WHITELIST AUTORISEE</color>",
            JobAccessMode.Whitelist => "<color=#FF6B6B>ACCES RESERVE — WHITELIST REQUISE</color>",
            _ => "<color=#FF6B6B>STAFF UNIQUEMENT</color>",
        };

        string skin = string.IsNullOrWhiteSpace(job.WardrobeName) ? "Standard" : job.WardrobeName;
        return $"<size=24><b>{Escape(job.Name)}</b></size>\n{Escape(job.Category)}\n\n" +
               $"Acces : {access}\nPlaces : <b>{slots}</b>\nSkin : <b>{Escape(skin)}</b>\n\n" +
               Escape(job.Description);
    }

    private static void Prompt(Player player, string message, bool success = false)
    {
        string color = success ? "#73D673" : "#FFB84D";
        player.SendHint(
            $"<align=center><size=24><color={color}><b>SITERP</b></color></size>\n" +
            $"<size=18>{Escape(message)}</size>\n" +
            "<size=15>Interface : touche <b>M</b> → Server Specific Settings.</size></align>",
            6f);
    }

    private static string Escape(string text)
    {
        return (text ?? string.Empty).Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private sealed class PlayerUiState
    {
        public int CategoryIndex { get; set; }
        public int SelectedRoleId { get; set; }
        public int StaffTargetIndex { get; set; }
        public int StaffRoleIndex { get; set; }
        public DateTime RulesOpenedAtUtc { get; set; }
    }
}
