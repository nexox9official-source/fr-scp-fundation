namespace SiteRP.Core.Jobs;

public sealed class JobDefinition
{
    public int UcrRoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JobAccessMode AccessMode { get; set; } = JobAccessMode.Public;
    public int MaxPlayers { get; set; }
    public string WardrobeName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
