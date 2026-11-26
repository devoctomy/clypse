using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for a vault's report. Logic lives in <see cref="VaultReportViewModel"/>; this only says when the dialog
/// is closed, or a secret in it is chosen.
/// </summary>
public partial class VaultReportDialog : MvvmComponentBase<VaultReportViewModel>
{
    /// <summary>
    /// Gets or sets what happens when the dialog is closed.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Gets or sets what happens when a secret in the report is chosen: the page opens it, in place of the report.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnOpenSecret { get; set; }

    private Task OpenSecretAsync(string id) => OnOpenSecret.InvokeAsync(id);

    private Task CloseAsync() => OnClose.InvokeAsync();
}
