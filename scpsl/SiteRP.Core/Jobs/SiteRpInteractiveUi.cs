using MEC;
using PlayerRoles;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Server-only RP interface rendered with SCP:SL hints.
/// Navigation intentionally uses the player's Radio controls so the interface does not live in Escape:
/// change radio range = next item, radio power = validate, SiteRP J keybind = back/open.
/// </summary>
public static class SiteRpInteractiveUi
{
    private const string StaffManagementCategory = "⚙ GESTION WHITELISTS";
    private static readonly Dictionary<string, JobMenuState> States = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> DeployedPlayers = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsOpen(Player player) => player is not null && States.ContainsKey(JobRuntime.GetPersistentUserId(player));

    public static bool IsDeployed(Player player)
    {
        if (player is null)
            return false;

        string id = JobRuntime.GetPersistentUserId(player);
        return DeployedPlayers.Contains(id) || SiteRpUcrBridge.TryGetActiveRoleId(player, out _);
    }

    public static void BeginArrival(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        string id = JobRuntime.GetPersistentUserId(player);
        DeployedPlayers.Remove(id);
        CleanupExistingState(player, restoreItem: false);

        JobMenuState state = new()
        {
            ForcedOnboarding = true,
            ReviewingRules = false,
            Stage = SiteRpRulesRepository.HasAccepted(player) ? SiteRpUiStage.Categories : SiteRpUiStage.Rules,
            RulesPage = 0,
            RulesSeenMask = 1,
            RulesOpenedAtUtc = DateTime.UtcNow,
        };
        States[id] = state;

        SiteRpUcrBridge.ClearCustomRole(player);
        player.SetRole(RoleTypeId.Tutorial);
        player.IsGodModeEnabled = true;
        player.IsNoclipEnabled = false;
        player.CustomInfo = "ENREGISTREMENT SITERP | NON DEPLOYE";
        player.ClearInventory();

        // Role loadouts can arrive a fraction of a second after SetRole. Clear once more and
        // provide the navigation radio only after Tutorial is fully active.
        Timing.CallDelayed(0.45f, () =>
        {
            if (player is null || !player.IsReady || !States.TryGetValue(id, out JobMenuState current) || !ReferenceEquals(current, state))
                return;

            player.ClearInventory();
            EnsureNavigationRadio(player, state);
            Render(player, state);
        });
    }

    public static void OpenJobs(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        if (!SiteRpRulesRepository.HasAccepted(player))
        {
            OpenRules(player, false);
            return;
        }

        string id = JobRuntime.GetPersistentUserId(player);
        CleanupExistingState(player, restoreItem: true);

        JobMenuState state = new()
        {
            ForcedOnboarding = false,
            Stage = SiteRpUiStage.Categories,
            Index = 0,
        };
        States[id] = state;
        EnsureNavigationRadio(player, state);
        Render(player, state);
    }

    public static void OpenRules(Player player, bool reviewOnly = true)
    {
        if (player is null || !player.IsReady)
            return;

        string id = JobRuntime.GetPersistentUserId(player);
        bool forced = !SiteRpRulesRepository.HasAccepted(player) && !IsDeployed(player);
        CleanupExistingState(player, restoreItem: true);

        JobMenuState state = new()
        {
            ForcedOnboarding = forced,
            ReviewingRules = reviewOnly || SiteRpRulesRepository.HasAccepted(player),
            Stage = SiteRpUiStage.Rules,
            RulesPage = 0,
            RulesSeenMask = 1,
            RulesOpenedAtUtc = DateTime.UtcNow,
        };
        States[id] = state;
        EnsureNavigationRadio(player, state);
        Render(player, state);
    }

    public static void HandleMenuKey(Player player)
    {
        if (player is null || !player.IsReady)
            return;

        string id = JobRuntime.GetPersistentUserId(player);
        if (!States.TryGetValue(id, out JobMenuState state))
        {
            if (IsDeployed(player))
                OpenJobs(player);
            else
                BeginArrival(player);
            return;
        }

        if (!Debounce(state))
            return;

        switch (state.Stage)
        {
            case SiteRpUiStage.Rules:
                if (state.RulesPage > 0)
                {
                    state.RulesPage--;
                    Render(player, state);
                }
                else if (!state.ForcedOnboarding)
                {
                    Close(player);
                }
                else
                {
                    Render(player, state, "<color=#FFB84D>Le règlement doit être accepté avant le déploiement.</color>");
                }
                break;

            case SiteRpUiStage.Jobs:
                state.Stage = SiteRpUiStage.Categories;
                state.Index = IndexOfCategory(state.Category, player);
                Render(player, state);
                break;

            case SiteRpUiStage.StaffRoles:
                state.Stage = SiteRpUiStage.StaffCategories;
                state.Index = IndexOfWhitelistCategory(state.Category);
                Render(player, state);
                break;

            case SiteRpUiStage.StaffCategories:
                state.Stage = SiteRpUiStage.StaffPlayers;
                state.Index = 0;
                Render(player, state);
                break;

            case SiteRpUiStage.StaffPlayers:
                state.Stage = SiteRpUiStage.Categories;
                state.Index = Math.Max(0, GetCategories(player).IndexOf(StaffManagementCategory));
                Render(player, state);
                break;

            case SiteRpUiStage.Categories:
                if (state.ForcedOnboarding)
                    Render(player, state, "<color=#FFB84D>Choisis un métier avant de te déployer.</color>");
                else
                    Close(player);
                break;

            default:
                Close(player);
                break;
        }
    }

    public static bool HandleRadioNext(Player player)
    {
        if (!TryGetState(player, out JobMenuState state) || !Debounce(state))
            return false;

        switch (state.Stage)
        {
            case SiteRpUiStage.Rules:
                if (state.RulesPage < SiteRpRulesRepository.Pages.Count - 1)
                    state.RulesPage++;
                else if (state.ReviewingRules)
                    state.RulesPage = 0;
                state.RulesSeenMask |= 1 << state.RulesPage;
                break;

            case SiteRpUiStage.Categories:
                state.Index = NextIndex(state.Index, GetCategories(player).Count);
                break;

            case SiteRpUiStage.Jobs:
                state.Index = NextIndex(state.Index, GetJobs(state.Category, player).Count);
                break;

            case SiteRpUiStage.StaffPlayers:
                state.Index = NextIndex(state.Index, GetStaffTargetPlayers().Count);
                break;

            case SiteRpUiStage.StaffCategories:
                state.Index = NextIndex(state.Index, GetWhitelistCategories().Count);
                break;

            case SiteRpUiStage.StaffRoles:
                state.Index = NextIndex(state.Index, GetWhitelistJobs(state.Category).Count);
                break;
        }

        Render(player, state);
        return true;
    }

    public static bool HandleRadioConfirm(Player player)
    {
        if (!TryGetState(player, out JobMenuState state) || !Debounce(state))
            return false;

        switch (state.Stage)
        {
            case SiteRpUiStage.Rules:
                ConfirmRules(player, state);
                break;
            case SiteRpUiStage.Categories:
                ConfirmCategory(player, state);
                break;
            case SiteRpUiStage.Jobs:
                ConfirmJob(player, state);
                break;
            case SiteRpUiStage.StaffPlayers:
                ConfirmStaffPlayer(player, state);
                break;
            case SiteRpUiStage.StaffCategories:
                ConfirmStaffCategory(player, state);
                break;
            case SiteRpUiStage.StaffRoles:
                ToggleWhitelist(player, state);
                break;
        }

        return true;
    }

    public static void MarkDeployed(Player player)
    {
        if (player is null)
            return;

        string id = JobRuntime.GetPersistentUserId(player);
        DeployedPlayers.Add(id);
        if (States.TryGetValue(id, out JobMenuState state))
            CleanupState(player, state, restoreItem: false);

        States.Remove(id);
        player.IsGodModeEnabled = false;
        player.SendHint("<align=center><size=28><color=#73D673><b>DÉPLOIEMENT AUTORISÉ</b></color></size>\n<size=18>Bienvenue sur le Site. Utilise la touche SiteRP <b>J</b> pour rouvrir les métiers.</size></align>", 6f);
    }

    public static void CleanupPlayer(Player player)
    {
        if (player is null)
            return;

        string id = JobRuntime.GetPersistentUserId(player);
        if (States.TryGetValue(id, out JobMenuState state))
            CleanupState(player, state, restoreItem: false);
        States.Remove(id);
        DeployedPlayers.Remove(id);
    }

    public static void Close(Player player)
    {
        if (player is null)
            return;

        string id = JobRuntime.GetPersistentUserId(player);
        if (!States.TryGetValue(id, out JobMenuState state))
            return;

        if (state.ForcedOnboarding)
        {
            Render(player, state, "<color=#FFB84D>Tu dois accepter les règles et choisir un métier avant de fermer.</color>");
            return;
        }

        CleanupState(player, state, restoreItem: true);
        States.Remove(id);
        player.SendHint(string.Empty, 0.1f);
    }

    private static void ConfirmRules(Player player, JobMenuState state)
    {
        int last = SiteRpRulesRepository.Pages.Count - 1;
        int allMask = (1 << SiteRpRulesRepository.Pages.Count) - 1;

        if (state.ReviewingRules && SiteRpRulesRepository.HasAccepted(player))
        {
            if (state.RulesPage == last)
                Close(player);
            else
                Render(player, state, "<color=#7FC8FF>Utilise PORTÉE pour parcourir les pages. J ferme le règlement.</color>");
            return;
        }

        if (state.RulesPage != last)
        {
            Render(player, state, "<color=#FFB84D>Lis toutes les pages avant d'accepter.</color>");
            return;
        }

        if ((state.RulesSeenMask & allMask) != allMask)
        {
            Render(player, state, "<color=#FFB84D>Toutes les pages doivent avoir été consultées.</color>");
            return;
        }

        int elapsed = (int)(DateTime.UtcNow - state.RulesOpenedAtUtc).TotalSeconds;
        if (elapsed < SiteRpRulesRepository.MinimumReadSeconds)
        {
            Render(player, state, $"<color=#FFB84D>Lecture minimale : encore {SiteRpRulesRepository.MinimumReadSeconds - elapsed}s.</color>");
            return;
        }

        SiteRpRulesRepository.Accept(player);
        state.ReviewingRules = false;
        state.Stage = SiteRpUiStage.Categories;
        state.Index = 0;
        Render(player, state, "<color=#73D673><b>Règlement accepté et sauvegardé.</b></color>");
    }

    private static void ConfirmCategory(Player player, JobMenuState state)
    {
        List<string> categories = GetCategories(player);
        if (categories.Count == 0)
        {
            Render(player, state, "<color=red>Aucune catégorie disponible.</color>");
            return;
        }

        state.Index = ClampIndex(state.Index, categories.Count);
        string category = categories[state.Index];
        if (category == StaffManagementCategory)
        {
            if (!JobRuntime.IsStaff(player))
            {
                Render(player, state, "<color=red>Accès staff refusé.</color>");
                return;
            }

            state.Stage = SiteRpUiStage.StaffPlayers;
            state.Index = 0;
            Render(player, state);
            return;
        }

        state.Category = category;
        state.Stage = SiteRpUiStage.Jobs;
        state.Index = 0;
        Render(player, state);
    }

    private static void ConfirmJob(Player player, JobMenuState state)
    {
        List<JobDefinition> jobs = GetJobs(state.Category, player);
        if (jobs.Count == 0)
        {
            Render(player, state, "<color=red>Aucun métier disponible.</color>");
            return;
        }

        state.Index = ClampIndex(state.Index, jobs.Count);
        JobDefinition job = jobs[state.Index];
        bool initialDeployment = state.ForcedOnboarding;
        bool ok = JobRuntime.TryJoin(player, job.UcrRoleId, out string response, initialDeployment);
        if (!ok)
        {
            Render(player, state, $"<color=#FF6B6B>{Escape(response)}</color>");
            return;
        }

        if (initialDeployment)
            MarkDeployed(player);
        else
        {
            Render(player, state, $"<color=#73D673>{Escape(response)}</color>");
            Timing.CallDelayed(1.3f, () => Close(player));
        }
    }

    private static void ConfirmStaffPlayer(Player staff, JobMenuState state)
    {
        if (!JobRuntime.IsStaff(staff))
        {
            state.Stage = SiteRpUiStage.Categories;
            state.Index = 0;
            Render(staff, state, "<color=red>Accès staff refusé.</color>");
            return;
        }

        List<Player> players = GetStaffTargetPlayers();
        if (players.Count == 0)
        {
            Render(staff, state, "<color=#FFB84D>Aucun joueur en ligne.</color>");
            return;
        }

        state.Index = ClampIndex(state.Index, players.Count);
        Player target = players[state.Index];
        state.TargetUserId = JobRuntime.GetPersistentUserId(target);
        state.TargetName = target.Nickname;
        state.Stage = SiteRpUiStage.StaffCategories;
        state.Index = 0;
        Render(staff, state);
    }

    private static void ConfirmStaffCategory(Player staff, JobMenuState state)
    {
        if (!JobRuntime.IsStaff(staff))
            return;

        List<string> categories = GetWhitelistCategories();
        if (categories.Count == 0)
        {
            Render(staff, state, "<color=red>Aucun rôle whitelisté.</color>");
            return;
        }

        state.Index = ClampIndex(state.Index, categories.Count);
        state.Category = categories[state.Index];
        state.Stage = SiteRpUiStage.StaffRoles;
        state.Index = 0;
        Render(staff, state);
    }

    private static void ToggleWhitelist(Player staff, JobMenuState state)
    {
        if (!JobRuntime.IsStaff(staff) || string.IsNullOrWhiteSpace(state.TargetUserId))
            return;

        List<JobDefinition> roles = GetWhitelistJobs(state.Category);
        if (roles.Count == 0)
        {
            Render(staff, state, "<color=red>Aucun rôle whitelisté dans cette catégorie.</color>");
            return;
        }

        state.Index = ClampIndex(state.Index, roles.Count);
        JobDefinition job = roles[state.Index];
        bool already = JobWhitelistRepository.IsWhitelisted(state.TargetUserId, job.UcrRoleId);
        bool changed;
        string action;

        if (already)
        {
            changed = JobWhitelistRepository.Revoke(state.TargetUserId, job.UcrRoleId);
            action = "RETIRÉE";
        }
        else
        {
            string grantedBy = $"{staff.Nickname} ({JobRuntime.GetPersistentUserId(staff)})";
            changed = JobWhitelistRepository.Grant(state.TargetUserId, job.UcrRoleId, grantedBy);
            action = "AJOUTÉE";
        }

        Render(staff, state, changed
            ? $"<color=#73D673>Whitelist {action} et sauvegardée : {Escape(job.Name)}</color>"
            : "<color=#FFB84D>Aucun changement.</color>");
    }

    private static void Render(Player player, JobMenuState state, string notice = "")
    {
        EnsureNavigationRadio(player, state);
        string content = state.Stage switch
        {
            SiteRpUiStage.Rules => RenderRules(state),
            SiteRpUiStage.Categories => RenderCategories(player, state),
            SiteRpUiStage.Jobs => RenderJobs(player, state),
            SiteRpUiStage.StaffPlayers => RenderStaffPlayers(state),
            SiteRpUiStage.StaffCategories => RenderStaffCategories(state),
            SiteRpUiStage.StaffRoles => RenderStaffRoles(state),
            _ => string.Empty,
        };

        string footer = state.Stage == SiteRpUiStage.Rules
            ? "<color=#8FA8C6>PORTÉE RADIO = PAGE SUIVANTE   •   ON/OFF = VALIDER   •   J = RETOUR</color>"
            : "<color=#8FA8C6>PORTÉE RADIO = SUIVANT   •   ON/OFF = VALIDER   •   J = RETOUR / FERMER</color>";

        string noticeBlock = string.IsNullOrWhiteSpace(notice) ? string.Empty : $"\n<size=18>{notice}</size>\n";
        player.SendHint(
            "<align=center><voffset=5em>" +
            "<size=32><color=#62A8FF><b>SITERP • FONDATION</b></color></size>\n" +
            "<size=16><color=#8494A8>INTERFACE DE DÉPLOIEMENT</color></size>\n\n" +
            content + noticeBlock + "\n<size=16>" + footer + "</size>" +
            "</voffset></align>",
            120f);
    }

    private static string RenderRules(JobMenuState state)
    {
        int page = Math.Max(0, Math.Min(state.RulesPage, SiteRpRulesRepository.Pages.Count - 1));
        int elapsed = (int)(DateTime.UtcNow - state.RulesOpenedAtUtc).TotalSeconds;
        string status = state.ReviewingRules
            ? "<color=#73D673>DÉJÀ ACCEPTÉ</color>"
            : page == SiteRpRulesRepository.Pages.Count - 1
                ? $"<color=#FFB84D>VALIDATION DISPONIBLE APRÈS {Math.Max(0, SiteRpRulesRepository.MinimumReadSeconds - elapsed)}s</color>"
                : "LECTURE OBLIGATOIRE";

        return $"<size=20><b>RÈGLEMENT DARKRP / SCP-RP</b> • v{SiteRpRulesRepository.CurrentVersion} • PAGE {page + 1}/{SiteRpRulesRepository.Pages.Count}</size>\n" +
               $"<size=16>{status}</size>\n\n" +
               $"<size=19>{SiteRpRulesRepository.Pages[page]}</size>";
    }

    private static string RenderCategories(Player player, JobMenuState state)
    {
        List<string> categories = GetCategories(player);
        if (categories.Count == 0)
            return "<size=22><color=red>Aucun métier chargé.</color></size>";

        state.Index = ClampIndex(state.Index, categories.Count);
        return "<size=22><b>CHOISIS TON DÉPARTEMENT / UNITÉ</b></size>\n" +
               $"<size=16>{(state.ForcedOnboarding ? "Déploiement obligatoire" : "Changement de métier")}</size>\n\n" +
               RenderList(categories, state.Index, 9);
    }

    private static string RenderJobs(Player player, JobMenuState state)
    {
        List<JobDefinition> jobs = GetJobs(state.Category, player);
        if (jobs.Count == 0)
            return $"<size=22><b>{Escape(state.Category)}</b></size>\n\n<color=red>Aucun métier disponible.</color>";

        state.Index = ClampIndex(state.Index, jobs.Count);
        JobDefinition selected = jobs[state.Index];
        List<string> labels = jobs.Select(x => $"[{AccessLabel(x, player)}] {x.Name}").ToList();
        int occupied = JobRuntime.CountPlayersOnRole(selected.UcrRoleId);
        string slots = selected.MaxPlayers <= 0 ? $"{occupied}/∞" : $"{occupied}/{selected.MaxPlayers}";
        string skin = string.IsNullOrWhiteSpace(selected.WardrobeName) ? "Standard" : selected.WardrobeName;
        string allowed = JobRuntime.CanJoinIgnoringSameRole(player, selected, out string reason)
            ? "<color=#73D673>ACCÈS AUTORISÉ</color>"
            : $"<color=#FFB84D>{Escape(reason)}</color>";

        return $"<size=22><b>{Escape(state.Category)}</b></size>\n\n" +
               RenderList(labels, state.Index, 7) + "\n" +
               $"<size=16>ID {selected.UcrRoleId} • Places {slots} • Tenue {Escape(skin)}</size>\n" +
               $"<size=17>{allowed}</size>";
    }

    private static string RenderStaffPlayers(JobMenuState state)
    {
        List<Player> players = GetStaffTargetPlayers();
        if (players.Count == 0)
            return "<size=22><b>GESTION WHITELISTS</b></size>\n\nAucun joueur en ligne.";

        state.Index = ClampIndex(state.Index, players.Count);
        List<string> labels = players.Select(x => $"#{x.PlayerId} • {SafeName(x.Nickname)} • {JobRuntime.GetPersistentUserId(x)}").ToList();
        return "<size=22><color=#FFB84D><b>STAFF • WHITELISTS</b></color></size>\n" +
               "<size=16>1/3 — Sélectionne un joueur</size>\n\n" + RenderList(labels, state.Index, 8);
    }

    private static string RenderStaffCategories(JobMenuState state)
    {
        List<string> categories = GetWhitelistCategories();
        if (categories.Count == 0)
            return "<size=22><b>GESTION WHITELISTS</b></size>\n\nAucune catégorie whitelistée.";

        state.Index = ClampIndex(state.Index, categories.Count);
        return $"<size=22><color=#FFB84D><b>STAFF • {SafeName(state.TargetName)}</b></color></size>\n" +
               "<size=16>2/3 — Sélectionne un département / une unité</size>\n\n" + RenderList(categories, state.Index, 8);
    }

    private static string RenderStaffRoles(JobMenuState state)
    {
        List<JobDefinition> roles = GetWhitelistJobs(state.Category);
        if (roles.Count == 0)
            return $"<size=22><b>{Escape(state.Category)}</b></size>\n\nAucun rôle whitelisté.";

        state.Index = ClampIndex(state.Index, roles.Count);
        List<string> labels = roles.Select(x =>
        {
            bool has = JobWhitelistRepository.IsWhitelisted(state.TargetUserId, x.UcrRoleId);
            return $"{(has ? "✓" : "○")} {x.UcrRoleId} • {x.Name}";
        }).ToList();

        JobDefinition selected = roles[state.Index];
        bool granted = JobWhitelistRepository.IsWhitelisted(state.TargetUserId, selected.UcrRoleId);
        string action = granted ? "ON/OFF = RETIRER L'ACCÈS" : "ON/OFF = DONNER L'ACCÈS";
        return $"<size=22><color=#FFB84D><b>STAFF • {SafeName(state.TargetName)}</b></color></size>\n" +
               $"<size=16>3/3 — {Escape(state.Category)}</size>\n\n" + RenderList(labels, state.Index, 7) +
               $"\n<size=17><b>{action}</b> • sauvegarde immédiate</size>";
    }

    private static string RenderList(IReadOnlyList<string> values, int selected, int maxVisible)
    {
        if (values.Count == 0)
            return "<i>Aucun élément.</i>";

        selected = ClampIndex(selected, values.Count);
        int half = maxVisible / 2;
        int start = Math.Max(0, selected - half);
        int end = Math.Min(values.Count, start + maxVisible);
        start = Math.Max(0, end - maxVisible);

        List<string> lines = new();
        for (int i = start; i < end; i++)
        {
            string value = Escape(values[i]);
            if (i == selected)
                lines.Add($"<size=21><color=#62A8FF><b>▶ {value}</b></color></size>");
            else
                lines.Add($"<size=18><color=#D8DEE8>   {value}</color></size>");
        }

        return string.Join("\n", lines);
    }

    private static List<string> GetCategories(Player player)
    {
        List<string> categories = JobCatalog.All
            .Where(x => x.AccessMode != JobAccessMode.StaffOnly || JobRuntime.IsStaff(player))
            .Select(x => x.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (JobRuntime.IsStaff(player))
            categories.Add(StaffManagementCategory);
        return categories;
    }

    private static List<JobDefinition> GetJobs(string category, Player player) => JobCatalog.All
        .Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase))
        .Where(x => x.AccessMode != JobAccessMode.StaffOnly || JobRuntime.IsStaff(player))
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.UcrRoleId)
        .ToList();

    private static List<Player> GetStaffTargetPlayers() => Player.ReadyList
        .Where(x => x is not null && x.IsReady && !string.IsNullOrWhiteSpace(JobRuntime.GetPersistentUserId(x)))
        .OrderBy(x => x.Nickname, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static List<string> GetWhitelistCategories() => JobCatalog.All
        .Where(x => x.AccessMode == JobAccessMode.Whitelist)
        .Select(x => x.Category)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static List<JobDefinition> GetWhitelistJobs(string category) => JobCatalog.All
        .Where(x => x.AccessMode == JobAccessMode.Whitelist && string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.UcrRoleId)
        .ToList();

    private static string AccessLabel(JobDefinition job, Player player)
    {
        if (job.AccessMode == JobAccessMode.Public)
            return "PUBLIC";
        if (job.AccessMode == JobAccessMode.StaffOnly)
            return "STAFF";
        return JobRuntime.IsStaff(player) || JobWhitelistRepository.IsWhitelisted(JobRuntime.GetPersistentUserId(player), job.UcrRoleId)
            ? "WL ✓"
            : "WL 🔒";
    }

    private static void EnsureNavigationRadio(Player player, JobMenuState state)
    {
        if (player is null || !player.IsReady)
            return;

        Item? radio = player.Items.FirstOrDefault(x => x is not null && !x.IsDestroyed && x.Type == ItemType.Radio);
        if (radio is null)
        {
            if (state.PreviousItem is null)
                state.PreviousItem = player.CurrentItem;
            radio = player.AddItem(ItemType.Radio);
            state.TemporaryRadio = radio;
        }
        else if (state.PreviousItem is null && player.CurrentItem is not null && player.CurrentItem.Type != ItemType.Radio)
        {
            state.PreviousItem = player.CurrentItem;
        }

        if (radio is not null && !radio.IsDestroyed)
            player.CurrentItem = radio;
    }

    private static void CleanupExistingState(Player player, bool restoreItem)
    {
        string id = JobRuntime.GetPersistentUserId(player);
        if (!States.TryGetValue(id, out JobMenuState existing))
            return;

        CleanupState(player, existing, restoreItem);
        States.Remove(id);
    }

    private static void CleanupState(Player player, JobMenuState state, bool restoreItem)
    {
        Item? previous = state.PreviousItem;
        Item? temporary = state.TemporaryRadio;

        if (temporary is not null && !temporary.IsDestroyed)
        {
            if (player.CurrentItem?.Serial == temporary.Serial)
                player.CurrentItem = null;
            player.RemoveItem(temporary);
        }

        if (restoreItem && previous is not null && !previous.IsDestroyed && player.Items.Any(x => x.Serial == previous.Serial))
            player.CurrentItem = previous;
    }

    private static bool TryGetState(Player player, out JobMenuState state)
    {
        state = null!;
        if (player is null)
            return false;
        return States.TryGetValue(JobRuntime.GetPersistentUserId(player), out state!);
    }

    private static bool Debounce(JobMenuState state)
    {
        DateTime now = DateTime.UtcNow;
        if ((now - state.LastInputUtc).TotalMilliseconds < 140)
            return false;
        state.LastInputUtc = now;
        return true;
    }

    private static int NextIndex(int index, int count) => count <= 0 ? 0 : (ClampIndex(index, count) + 1) % count;
    private static int ClampIndex(int index, int count) => count <= 0 ? 0 : Math.Max(0, Math.Min(index, count - 1));

    private static int IndexOfCategory(string category, Player player)
    {
        List<string> categories = GetCategories(player);
        int index = categories.FindIndex(x => string.Equals(x, category, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? 0 : index;
    }

    private static int IndexOfWhitelistCategory(string category)
    {
        List<string> categories = GetWhitelistCategories();
        int index = categories.FindIndex(x => string.Equals(x, category, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? 0 : index;
    }

    private static string Escape(string value) => (value ?? string.Empty).Replace("<", "‹").Replace(">", "›");
    private static string SafeName(string value) => Escape(value);
}
