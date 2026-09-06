namespace SiteRP.Core.Jobs;

public enum SiteRpUiStage
{
    Closed,
    Rules,
    Categories,
    Jobs,
    StaffPlayers,
    StaffRoles,
}

public sealed class JobMenuState
{
    public SiteRpUiStage Stage { get; set; } = SiteRpUiStage.Closed;
    public bool ForcedOnboarding { get; set; }
    public bool ReviewingRules { get; set; }
    public int Index { get; set; }
    public int RulesPage { get; set; }
    public int RulesSeenMask { get; set; }
    public DateTime RulesOpenedAtUtc { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public Item? TemporaryRadio { get; set; }
    public Item? PreviousItem { get; set; }
    public DateTime LastInputUtc { get; set; }
}
