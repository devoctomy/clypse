using Blazing.Mvvm.ComponentModel;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the loading dialog — holds display message only.
/// </summary>
public partial class LoadingDialogViewModel : ViewModelBase
{
    private string message = "Loading...";

    /// <summary>Gets or sets the loading message to display.</summary>
    public string Message { get => message; set => SetProperty(ref message, value); }
}
