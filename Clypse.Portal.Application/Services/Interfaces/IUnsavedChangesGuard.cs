namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Stands between the page and anything that would lose unsaved changes, such as showing another tab while a text file
/// is being edited. The editor says what holds such an action back; whatever is about to lose the changes runs its
/// action through <see cref="Run"/>.
/// </summary>
public interface IUnsavedChangesGuard
{
    /// <summary>
    /// Sets what holds actions back while there are unsaved changes, or clears it.
    /// </summary>
    /// <param name="hold">Given an action, returns false if it can go ahead now. Otherwise it keeps the action, asks
    /// the user, and runs it later if they save or discard their changes. Null when nothing is being edited.</param>
    void SetHolder(Func<Action, bool>? hold);

    /// <summary>
    /// Clears what holds actions back, if it is still the one given: an editor that has closed must not clear the
    /// holder of the editor that replaced it.
    /// </summary>
    /// <param name="hold">The holder to clear.</param>
    void ClearHolder(Func<Action, bool> hold);

    /// <summary>
    /// Runs an action that would lose unsaved changes: at once if there are none, otherwise only once the user has
    /// saved or discarded them. If they choose to keep editing, it is never run.
    /// </summary>
    /// <param name="action">The action.</param>
    void Run(Action action);
}
