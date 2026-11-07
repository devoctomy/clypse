using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="ISecretCopyService"/> over the framework's clipboard and toasts.
/// </summary>
/// <param name="clipboard">Copies, and clears secret values again.</param>
/// <param name="toasts">Says what was copied.</param>
/// <param name="preferences">Whether, and when, secret values are cleared from the clipboard.</param>
/// <param name="text">Clypse's text.</param>
public class SecretCopyService(
    IClipboardService clipboard,
    IToastService toasts,
    IClipboardPreferencesService preferences,
    IStringLocalizer<ClypseText> text)
    : ISecretCopyService
{
    private readonly IClipboardService clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
    private readonly IToastService toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
    private readonly IClipboardPreferencesService preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    private readonly IStringLocalizer<ClypseText> text = text ?? throw new ArgumentNullException(nameof(text));

    /// <inheritdoc/>
    public async Task<bool> CopyAsync(string field, string? value, bool isSecretValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(field);
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var name = text["Secret.Field." + field];
        var chosen = await preferences.GetAsync();
        var clearAfter = isSecretValue && chosen.ClearSecrets ? TimeSpan.FromSeconds(chosen.ClearAfterSeconds) : (TimeSpan?)null;
        try
        {
            await clipboard.CopyAsync(value, clearAfter);
        }
        catch (Exception)
        {
            // The browser refused (e.g. the page lost focus): say so rather than pretend it worked.
            toasts.Show(text["Copy.Failed", name], ToastTone.Danger);
            return false;
        }

        toasts.Show(
            clearAfter is { } after ? text["Copy.CopiedSecret", name, (int)after.TotalSeconds] : text["Copy.Copied", name],
            ToastTone.Success);
        return true;
    }
}
