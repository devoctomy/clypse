using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for the password generator. Logic lives in <see cref="PasswordGeneratorViewModel"/>; this only says
/// which password was chosen.
/// </summary>
public partial class PasswordGeneratorDialog : MvvmComponentBase<PasswordGeneratorViewModel>
{
    /// <summary>
    /// Gets or sets what happens when a password is chosen.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnUse { get; set; }

    /// <summary>
    /// Gets or sets what happens when the generator is closed without choosing one.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }
}
