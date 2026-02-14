namespace clypse.portal.Models.Changes;

public class VersionEntry
{
    public string Version { get; set; } = string.Empty;

    public List<ChangeEntry> Changes { get; set; } = [];
}
