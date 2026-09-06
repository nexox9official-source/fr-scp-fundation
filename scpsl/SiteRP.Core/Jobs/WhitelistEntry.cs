namespace SiteRP.Core.Jobs;

public sealed class WhitelistEntry
{
    public string SteamId64 { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string GrantedBy { get; set; } = string.Empty;
    public string GrantedAtUtc { get; set; } = string.Empty;
}
