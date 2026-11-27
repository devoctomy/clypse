using Clypse.Portal.Application.Services.Interfaces;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IUnsavedChangesGuard"/>.
/// </summary>
public class UnsavedChangesGuard : IUnsavedChangesGuard
{
    private Func<Action, bool>? holder;

    /// <inheritdoc/>
    public void SetHolder(Func<Action, bool>? hold)
    {
        holder = hold;
    }

    /// <inheritdoc/>
    public void ClearHolder(Func<Action, bool> hold)
    {
        if (holder == hold)
        {
            holder = null;
        }
    }

    /// <inheritdoc/>
    public void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (holder == null || !holder(action))
        {
            action();
        }
    }
}
