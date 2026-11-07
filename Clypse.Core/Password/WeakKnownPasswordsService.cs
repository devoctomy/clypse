using Clypse.Core.Configuration;
using Clypse.Core.Data;
using Microsoft.Extensions.Options;

namespace Clypse.Core.Password;

/// <summary>
/// Checks passwords against the embedded weak known passwords list, or against a small stand-in list when
/// <see cref="TextListOptions.UseFullWeakKnownPasswordsList"/> is off (Debug builds of the portal).
/// </summary>
/// <param name="embeddedResourceLoaderService">Loads, and keeps, the embedded list.</param>
/// <param name="options">Says which list to use.</param>
public class WeakKnownPasswordsService(
    IEmbeddedResourceLoaderService embeddedResourceLoaderService,
    IOptions<TextListOptions> options)
    : IWeakKnownPasswordsService
{
    private readonly IEmbeddedResourceLoaderService embeddedResourceLoaderService = embeddedResourceLoaderService
        ?? throw new ArgumentNullException(nameof(embeddedResourceLoaderService));

    private readonly TextListOptions options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc/>
    public async Task<bool> ContainsAsync(string password, CancellationToken cancellationToken)
    {
        if (!this.options.UseFullWeakKnownPasswordsList)
        {
            return this.options.StandInWeakKnownPasswords.Contains(password);
        }

        var weakKnownPasswords = await this.embeddedResourceLoaderService.LoadCompressedHashedTextSetAsync(
            ResourceKeys.CompressedWeakKnownPasswordsResourceKey,
            typeof(WeakKnownPasswordsService).Assembly,
            cancellationToken);
        return weakKnownPasswords.Contains(password);
    }
}
