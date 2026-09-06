using System.Text;

namespace SiteRP.Core.Jobs;

/// <summary>
/// Mandatory SiteRP rules overlay rendered through HintServiceMeow when available.
/// No client mod is required.
/// </summary>
public static class RulesHudManager
{
    private sealed class RulesHudState
    {
        public int PageIndex { get; set; }
        public DateTime OpenedAtUtc { get; set; }
        public bool Open { get; set; }
    }

    private static readonly Dictionary<string, RulesHudState> States = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsOpen(Player player)
    {
        if (player is null)
            return false;
        return States.TryGetValue(JobRuntime.GetPersistentUserId(player), out RulesHudState? state) && state.Open;
    }

    public static void Open(Player player, string? flash = null)
    {
        if (player is null || !player.IsReady)
            return;

        RulesHudState state = GetState(player);
        state.Open = true;
        if (state.OpenedAtUtc == default && !SiteRpRulesRepository.HasAccepted(player))
            state.OpenedAtUtc = DateTime.UtcNow;
        Normalize(state);
        Render(player, state, flash);
    }

    public static bool Previous(Player player, out string response)
    {
        RulesHudState state = GetState(player);
        state.Open = true;
        if (state.OpenedAtUtc == default)
            state.OpenedAtUtc = DateTime.UtcNow;
        int count = SiteRpRulesRepository.Pages.Count;
        if (count <= 0)
        {
            response = "Aucune page de règlement.";
            Render(player, state, response);
            return false;
        }

        state.PageIndex = Wrap(state.PageIndex - 1, count);
        response = $"Règlement {state.PageIndex + 1}/{count}.";
        Render(player, state);
        return true;
    }

    public static bool Next(Player player, out string response)
    {
        RulesHudState state = GetState(player);
        state.Open = true;
        if (state.OpenedAtUtc == default)
            state.OpenedAtUtc = DateTime.UtcNow;
        int count = SiteRpRulesRepository.Pages.Count;
        if (count <= 0)
        {
            response = "Aucune page de règlement.";
            Render(player, state, response);
            return false;
        }

        state.PageIndex = Wrap(state.PageIndex + 1, count);
        response = $"Règlement {state.PageIndex + 1}/{count}.";
        Render(player, state);
        return true;
    }

    public static bool Select(Player player, out string response)
    {
        RulesHudState state = GetState(player);
        state.Open = true;
        Normalize(state);

        if (SiteRpRulesRepository.HasAccepted(player))
        {
            response = "Règlement déjà accepté. Ouverture du choix de métier.";
            state.Open = false;
            JobHudManager.Open(player, response);
            return true;
        }

        int pageCount = SiteRpRulesRepository.Pages.Count;
        if (pageCount <= 0)
        {
            response = "Le règlement est indisponible.";
            Render(player, state, response);
            return false;
        }

        if (state.PageIndex < pageCount - 1)
        {
            state.PageIndex++;
            response = $"Page {state.PageIndex + 1}/{pageCount}.";
            Render(player, state, "Continue la lecture. Utilise .hud select pour passer à la page suivante.");
            return true;
        }

        if (state.OpenedAtUtc == default)
            state.OpenedAtUtc = DateTime.UtcNow;

        int elapsed = (int)Math.Floor((DateTime.UtcNow - state.OpenedAtUtc).TotalSeconds);
        int remaining = Math.Max(0, SiteRpRulesRepository.MinimumReadSeconds - elapsed);
        if (remaining > 0)
        {
            response = $"Lecture obligatoire: encore {remaining}s avant validation.";
            Render(player, state, response);
            return false;
        }

        SiteRpRulesRepository.Accept(player);
        state.Open = false;
        response = "Règlement accepté et sauvegardé. Choisis maintenant ton affectation.";
        JobHudManager.Open(player, response);
        return true;
    }

    public static void Close(Player player)
    {
        if (player is null)
            return;
        if (States.TryGetValue(JobRuntime.GetPersistentUserId(player), out RulesHudState? state))
            state.Open = false;
        SiteRpHudRenderer.Hide(player);
    }

    public static void Cleanup(Player player)
    {
        if (player is null)
            return;
        States.Remove(JobRuntime.GetPersistentUserId(player));
        SiteRpHudRenderer.Cleanup(player);
    }

    private static void Render(Player player, RulesHudState state, string? flash = null)
    {
        if (player is null || !player.IsReady)
            return;

        IReadOnlyList<string> pages = SiteRpRulesRepository.Pages;
        if (pages.Count == 0)
        {
            SiteRpHudRenderer.Show(player, "<align=center><size=26><color=#FF6961><b>SITERP — RÈGLEMENT INDISPONIBLE</b></color></size></align>", 8f);
            return;
        }

        Normalize(state);
        bool accepted = SiteRpRulesRepository.HasAccepted(player);
        bool last = state.PageIndex == pages.Count - 1;
        int elapsed = state.OpenedAtUtc == default ? 0 : (int)Math.Floor((DateTime.UtcNow - state.OpenedAtUtc).TotalSeconds);
        int remaining = Math.Max(0, SiteRpRulesRepository.MinimumReadSeconds - elapsed);

        StringBuilder sb = new();
        sb.Append("<align=center>");
        sb.Append("<size=32><color=#62A8FF><b>SITERP // DOSSIER D'ADMISSION</b></color></size>\n");
        sb.Append("<size=16><color=#8392A5>RÈGLEMENT v").Append(Escape(SiteRpRulesRepository.CurrentVersion))
            .Append("  •  PAGE ").Append(state.PageIndex + 1).Append('/').Append(pages.Count).Append("</color></size>\n");
        sb.Append("<size=18><color=#DCE6F2>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color></size>\n\n");
        sb.Append("<size=17>").Append(pages[state.PageIndex]).Append("</size>\n\n");
        sb.Append("<size=18><color=#DCE6F2>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color></size>\n");

        if (accepted)
            sb.Append("<size=16><color=#73D673><b>✓ Règlement déjà accepté.</b> .hud select ouvre les métiers.</color></size>\n");
        else if (!last)
            sb.Append("<size=16><color=#62A8FF><b>.hud select</b> : page suivante</color></size>\n");
        else if (remaining > 0)
            sb.Append("<size=16><color=#FFB84D>Dernière page — lecture minimum: encore <b>").Append(remaining).Append("s</b>.</color></size>\n");
        else
            sb.Append("<size=17><color=#73D673><b>.hud select : J'ACCEPTE LE RÈGLEMENT</b></color></size>\n");

        if (!string.IsNullOrWhiteSpace(flash))
            sb.Append("<size=15><color=#FFB84D>").Append(Escape(flash)).Append("</color></size>\n");

        sb.Append("\n<size=14><color=#8EA0B5>.hud prev / next : pages  •  .hud select : continuer / valider  •  .hud close : fermer</color></size>\n");
        sb.Append("<size=13><color=#647487>Choisis librement tes touches avec bind/cmdbind. <b>M reste réservé à l'interface admin.</b></color></size>");
        sb.Append("</align>");

        SiteRpHudRenderer.Show(player, sb.ToString(), 30f);
    }

    private static RulesHudState GetState(Player player)
    {
        string id = JobRuntime.GetPersistentUserId(player);
        if (!States.TryGetValue(id, out RulesHudState? state))
        {
            state = new RulesHudState();
            States[id] = state;
        }
        return state;
    }

    private static void Normalize(RulesHudState state)
    {
        int count = SiteRpRulesRepository.Pages.Count;
        state.PageIndex = count <= 0 ? 0 : Math.Max(0, Math.Min(state.PageIndex, count - 1));
    }

    private static int Wrap(int value, int count)
    {
        if (count <= 0)
            return 0;
        value %= count;
        if (value < 0)
            value += count;
        return value;
    }

    private static string Escape(string value) => (value ?? string.Empty)
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}
