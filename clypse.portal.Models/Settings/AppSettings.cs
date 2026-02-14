using System.Reflection;

namespace clypse.portal.Models.Settings;

/// <summary>
/// Represents application-wide settings and configuration.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether WebAuthn authentication is enabled for portal login.
    /// </summary>
    public bool EnablePortalLoginAuthn { get; set; }

    /// <summary>
    /// Gets or sets the title displayed in the application.
    /// </summary>
    public string ApplicationTitle { get; set; } = "Clypse Portal";

    /// <summary>
    /// Gets or sets the copyright message displayed in the application.
    /// </summary>
    public string CopyrightMessage { get; set; } = "© 2024 Clypse Portal. All rights reserved.";

    /// <summary>
    /// Gets or sets a value indicating whether to display the logo in the title bar.
    /// </summary>
    public bool ShowLogoInTitleBar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application is running in test mode.
    /// </summary>
    public bool TestMode { get; set; }

    /// <summary>
    /// Gets or sets the list of memorable password templates.
    /// </summary>
    public List<MemorablePasswordTemplateItem> MemorablePasswordTemplates { get; set; } = [];

    /// <summary>
    /// Gets the version of the executing assembly.
    /// </summary>
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
}
