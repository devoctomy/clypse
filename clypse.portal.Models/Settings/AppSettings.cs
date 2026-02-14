using System.Reflection;

namespace clypse.portal.Models.Settings;

public class AppSettings
{
    public bool EnablePortalLoginAuthn { get; set; }

    public string ApplicationTitle { get; set; } = "Clypse Portal";

    public string CopyrightMessage { get; set; } = "© 2024 Clypse Portal. All rights reserved.";

    public bool ShowLogoInTitleBar { get; set; }

    public bool TestMode { get; set; }

    public List<MemorablePasswordTemplateItem> MemorablePasswordTemplates { get; set; } = [];
 
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
}
