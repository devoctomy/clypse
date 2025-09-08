namespace clypse.portal.Models;

public class AppSettings
{
    public string ApplicationTitle { get; set; } = "Clypse Portal";
    public string CopyrightMessage { get; set; } = "© 2024 Clypse Portal. All rights reserved.";
    public List<string> MemorablePasswordTemplates { get; set; } = new();
}
