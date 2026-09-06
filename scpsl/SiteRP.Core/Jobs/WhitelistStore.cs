namespace SiteRP.Core.Jobs;

public sealed class WhitelistStore
{
    public List<WhitelistEntry> Entries { get; set; } = new();
}
