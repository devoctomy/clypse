namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Copies a secret's field to the clipboard, and says so with a toast. A secret value (a password or an access key) is
/// cleared from the clipboard again after a while.
/// </summary>
public interface ISecretCopyService
{
    /// <summary>
    /// Copies a field's value to the clipboard.
    /// </summary>
    /// <param name="field">The field's name, e.g. <c>Password</c>.</param>
    /// <param name="value">The value.</param>
    /// <param name="isSecretValue">Whether the value is itself a secret, so it is cleared from the clipboard later.</param>
    /// <returns>True if it was copied.</returns>
    Task<bool> CopyAsync(string field, string? value, bool isSecretValue);
}
