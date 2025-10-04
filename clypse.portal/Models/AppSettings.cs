using System.Reflection;

namespace clypse.portal.Models;

public class AppSettings
{
    public bool EnablePortalLoginAuthn { get; set; } = false;
    public string ApplicationTitle { get; set; } = "Clypse Portal";
    public string CopyrightMessage { get; set; } = "© 2024 Clypse Portal. All rights reserved.";
    public bool ShowLogoInTitleBar { get; set; } = false;
    public List<MemorablePasswordTemplateItem> MemorablePasswordTemplates { get; set; } = new();
    
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
}
