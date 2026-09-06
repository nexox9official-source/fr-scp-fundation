using System.Text;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Full-screen jobs overlay rendered with SCP:SL hints.
/// It requires no client mod. Navigation is available through bindable .hud commands.
/// </summary>
public static class JobHudManager
{
    private sealed class HudState
    {
        public int CategoryIndex { get; set; }
        public int JobIndex { get; set; }
        public bool Open { get; set; }
    }

    private static readonly Dictionary<string, HudState> States = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsOpen(Player player)
    {
        if (player is null)
            return false;

        return States.TryGetValue(JobRuntime.GetPersistentUserId(player), out HudState? state) && state.Open;
    }

    public static void Open(Player player, string? flash = null)
    {
        if (player is null || !player.IsReady)
            return;

        if (!SiteRpRulesRepository.HasAccepted(player))
        {
            SiteRpInteractiveUi.OpenRules(player, false);
            player.SendHint(
                "<align=center><size=30><color=#62A8FF><b>SITERP — REGLEMENT</b></color></size>\n" +
                "<size=18>Le règlement doit être accepté avant le choix du métier.</size>\n" +
                "<size=16>Utilise <b>.hud rules</b> ou la touche que tu as liée à cette commande.</size></align>",
                8f);
            return;
        }

        HudState state = GetState(player);
        state.Open = true;
        Normalize(state);
        Render(player, flash);
    }

    public static void Close(Player player)
    {
        if (player is null)
            return;

        if (States.TryGetValue(JobRuntime.GetPersistentUserId(player), out HudState? state))
            state.Open = false;

        player.SendHint("<align=center><size=20><color=#AEB9C6>Menu métiers SiteRP fermé.</color></size></align>", 2f);
    }

    public static void Cleanup(Player player)
    {
        if (player is null)
            return;
        States.Remove(JobRuntime.GetPersistentUserId(player));
    }

    public static bool PreviousJob(Player player, out string response)
    {
        HudState state = GetState(player);
        List<JobDefinition> jobs = CurrentJobs(state);
        if (jobs.Count == 0)
        {
            response = "Aucun métier dans cette catégorie.";
            Open(player, response);
            return false;
        }

        state.JobIndex = Wrap(state.JobIndex - 1, jobs.Count);
        state.Open = true;
        response = jobs[state.JobIndex].Name;
        Render(player);
        return true;
    }

    public static bool NextJob(Player player, out string response)
    {
        HudState state = GetState(player);
        List<JobDefinition> jobs = CurrentJobs(state);
        if (jobs.Count == 0)
        {
            response = "Aucun métier dans cette catégorie.";
            Open(player, response);
            return false;
        }

        state.JobIndex = Wrap(state.JobIndex + 1, jobs.Count);
        state.Open = true;
        response = jobs[state.JobIndex].Name;
        Render(player);
        return true;
    }

    public static bool PreviousCategory(Player player, out string response)
    {
        HudState state = GetState(player);
        List<string> categories = Categories();
        if (categories.Count == 0)
        {
            response = "Aucune catégorie chargée.";
            Open(player, response);
            return false;
        }

        state.CategoryIndex = Wrap(state.CategoryIndex - 1, categories.Count);
        state.JobIndex = 0;
        state.Open = true;
        response = categories[state.CategoryIndex];
        Render(player);
        return true;
    }

    public static bool NextCategory(Player player, out string response)
    {
        HudState state = GetState(player);
        List<string> categories = Categories();
        if (categories.Count == 0)
        {
            response = "Aucune catégorie chargée.";
            Open(player, response);
            return false;
        }

        state.CategoryIndex = Wrap(state.CategoryIndex + 1, categories.Count);
        state.JobIndex = 0;
        state.Open = true;
        response = categories[state.CategoryIndex];
        Render(player);
        return true;
    }

    public static bool Select(Player player, out string response)
    {
        if (!SiteRpRulesRepository.HasAccepted(player))
        {
            response = "Règlement non accepté.";
            Open(player, response);
            return false;
        }

        HudState state = GetState(player);
        Normalize(state);
        List<JobDefinition> jobs = CurrentJobs(state);
        if (jobs.Count == 0)
        {
            response = "Aucun métier sélectionné.";
            Render(player, response);
            return false;
        }

        JobDefinition job = jobs[state.JobIndex];
        bool initial = !SiteRpInteractiveUi.IsDeployed(player);
        bool ok = JobRuntime.TryJoin(player, job.UcrRoleId, out response, initial);
        if (!ok)
        {
            Render(player, response);
            return false;
        }

        state.Open = false;
        if (initial)
            SiteRpInteractiveUi.MarkDeployed(player);
        else
            player.SendHint($"<align=center><size=28><color=#73D673><b>{Escape(job.Name)}</b></color></size>\n<size=18>{Escape(response)}</size></align>", 6f);
        return true;
    }

    public static void Refresh(Player player)
    {
        if (player is null || !player.IsReady)
            return;
        HudState state = GetState(player);
        state.Open = true;
        Normalize(state);
        Render(player);
    }

    public static void Render(Player player, string? flash = null)
    {
        if (player is null || !player.IsReady)
            return;

        HudState state = GetState(player);
        Normalize(state);
        List<string> categories = Categories();

        if (categories.Count == 0)
        {
            player.SendHint("<align=center><size=26><color=#FF6961><b>SITERP — AUCUN METIER CHARGE</b></color></size></align>", 8f);
            return;
        }

        string category = categories[state.CategoryIndex];
        List<JobDefinition> jobs = JobsFor(category);
        if (jobs.Count == 0)
        {
            player.SendHint("<align=center><size=26><color=#FFB84D><b>SITERP — CATEGORIE VIDE</b></color></size></align>", 8f);
            return;
        }

        state.JobIndex = Math.Max(0, Math.Min(state.JobIndex, jobs.Count - 1));
        JobDefinition selected = jobs[state.JobIndex];
        StringBuilder sb = new();

        sb.Append("<align=center><voffset=-310><size=31><color=#62A8FF><b>SITERP // AFFECTATION DU PERSONNEL</b></color></size>\n");
        sb.Append("<size=17><color=#AEB9C6>HUD serveur — aucun téléchargement client requis</color></size>\n");
        sb.Append("<size=20><b>").Append(Escape(category)).Append("</b>  <color=#7E8A99>(")
            .Append(state.CategoryIndex + 1).Append('/').Append(categories.Count).Append(")</color></size>\n\n");

        int start = Math.Max(0, state.JobIndex - 2);
        int end = Math.Min(jobs.Count - 1, start + 4);
        if (end - start < 4)
            start = Math.Max(0, end - 4);

        for (int i = start; i <= end; i++)
        {
            JobDefinition job = jobs[i];
            int occupied = JobRuntime.CountPlayersOnRole(job.UcrRoleId);
            string max = job.MaxPlayers <= 0 ? "∞" : job.MaxPlayers.ToString();
            string access = AccessLabel(player, job);
            bool selectedLine = i == state.JobIndex;

            sb.Append(selectedLine ? "<size=21><color=#FFFFFF><b>▶ " : "<size=18><color=#B9C7D8>   ");
            sb.Append(job.UcrRoleId).Append(" — ").Append(Escape(job.Name))
                .Append("  <color=#7E8A99>").Append(occupied).Append('/').Append(max).Append("</color> ")
                .Append(access);
            sb.Append(selectedLine ? "</b></color></size>\n" : "</color></size>\n");
        }

        string selectedAccess = AccessDetails(player, selected);
        sb.Append("\n<size=18><color=#62A8FF><b>").Append(Escape(selected.Name)).Append("</b></color></size>\n");
        sb.Append("<size=16>").Append(Escape(selected.Description)).Append("</size>\n");
        sb.Append("<size=16>").Append(selectedAccess).Append("</size>\n");
        if (!string.IsNullOrWhiteSpace(selected.WardrobeName))
            sb.Append("<size=15><color=#AEB9C6>Tenue: ").Append(Escape(selected.WardrobeName)).Append("</color></size>\n");

        if (!string.IsNullOrWhiteSpace(flash))
            sb.Append("\n<size=17><color=#FFB84D>").Append(Escape(flash)).Append("</color></size>\n");

        sb.Append("\n<size=15><color=#8EA0B5>")
            .Append(".hud prev / next   •   .hud catprev / catnext   •   <b>.hud select</b>   •   .hud close")
            .Append("</color></size>\n");
        sb.Append("<size=14><color=#647487>Les touches sont choisies par le joueur avec bind/cmdbind. M reste réservé à l'interface admin.</color></size></voffset></align>");

        player.SendHint(sb.ToString(), 25f);
    }

    private static HudState GetState(Player player)
    {
        string id = JobRuntime.GetPersistentUserId(player);
        if (!States.TryGetValue(id, out HudState? state))
        {
            state = new HudState();
            States[id] = state;
        }
        return state;
    }

    private static void Normalize(HudState state)
    {
        List<string> categories = Categories();
        if (categories.Count == 0)
        {
            state.CategoryIndex = 0;
            state.JobIndex = 0;
            return;
        }

        state.CategoryIndex = Math.Max(0, Math.Min(state.CategoryIndex, categories.Count - 1));
        List<JobDefinition> jobs = JobsFor(categories[state.CategoryIndex]);
        state.JobIndex = jobs.Count == 0 ? 0 : Math.Max(0, Math.Min(state.JobIndex, jobs.Count - 1));
    }

    private static List<string> Categories() => JobCatalog.All
        .Where(x => x.AccessMode != JobAccessMode.StaffOnly)
        .Select(x => x.Category)
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x)
        .ToList();

    private static List<JobDefinition> CurrentJobs(HudState state)
    {
        List<string> categories = Categories();
        if (categories.Count == 0)
            return new List<JobDefinition>();
        state.CategoryIndex = Math.Max(0, Math.Min(state.CategoryIndex, categories.Count - 1));
        return JobsFor(categories[state.CategoryIndex]);
    }

    private static List<JobDefinition> JobsFor(string category) => JobCatalog.All
        .Where(x => x.AccessMode != JobAccessMode.StaffOnly && string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x.SortOrder)
        .ToList();

    private static int Wrap(int value, int count)
    {
        if (count <= 0)
            return 0;
        value %= count;
        if (value < 0)
            value += count;
        return value;
    }

    private static string AccessLabel(Player player, JobDefinition job)
    {
        if (job.AccessMode == JobAccessMode.Public)
            return "<color=#73D673>[PUBLIC]</color>";
        if (job.AccessMode == JobAccessMode.StaffOnly)
            return "<color=#FF6961>[STAFF]</color>";

        bool allowed = JobRuntime.IsStaff(player) || JobWhitelistRepository.IsWhitelisted(JobRuntime.GetPersistentUserId(player), job.UcrRoleId);
        return allowed ? "<color=#73D673>[AUTORISE]</color>" : "<color=#FFB84D>[WHITELIST]</color>";
    }

    private static string AccessDetails(Player player, JobDefinition job)
    {
        if (JobRuntime.CanJoin(player, job, out string reason, ignoreCooldown: !SiteRpInteractiveUi.IsDeployed(player)))
            return "<color=#73D673>✓ Accès autorisé — sélection possible.</color>";

        return "<color=#FFB84D>⚠ " + Escape(reason) + "</color>";
    }

    private static string Escape(string value) => (value ?? string.Empty)
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}
