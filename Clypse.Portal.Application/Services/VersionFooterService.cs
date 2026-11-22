using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IVersionFooterService"/>. The footer of the sign-in pages is the framework's, and
/// shows <see cref="AppSettings.CopyrightMessage"/>: this puts the version in that message.
/// </summary>
/// <param name="appSettings">The app's settings, which the framework's pages read.</param>
/// <param name="buildInfo">The app's build, for its version.</param>
/// <param name="text">Clypse's text.</param>
public class VersionFooterService(AppSettings appSettings, IAppBuildInfo buildInfo, IStringLocalizer<ClypseText> text)
    : IVersionFooterService
{
    private readonly AppSettings appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    private readonly IAppBuildInfo buildInfo = buildInfo ?? throw new ArgumentNullException(nameof(buildInfo));
    private readonly IStringLocalizer<ClypseText> text = text ?? throw new ArgumentNullException(nameof(text));
    private bool applied;

    /// <inheritdoc/>
    public void Apply()
    {
        if (applied || string.IsNullOrWhiteSpace(buildInfo.Version))
        {
            return;
        }

        // The build gives the assembly's four part version (0.9.2.0); clypse's own has three (VersionPrefix), so an
        // unused fourth part is left off.
        var version = Version.TryParse(buildInfo.Version, out var parsed) && parsed.Revision == 0
            ? parsed.ToString(3)
            : buildInfo.Version;

        applied = true;
        appSettings.CopyrightMessage = string.IsNullOrWhiteSpace(appSettings.CopyrightMessage)
            ? text["Footer.Version", version]
            : text["Footer.VersionAndCopyright", version, appSettings.CopyrightMessage];
    }
}
