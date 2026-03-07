using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the generic confirmation dialog.
/// </summary>
public partial class ConfirmDialogViewModel : ViewModelBase
{
    private bool isProcessing;
    private string message = "Are you sure you want to delete this item?";

    /// <summary>Gets or sets a value indicating whether processing is in progress.</summary>
    public bool IsProcessing { get => isProcessing; set => SetProperty(ref isProcessing, value); }

    /// <summary>Gets or sets the confirmation message.</summary>
    public string Message { get => message; set => SetProperty(ref message, value); }

    /// <summary>Gets or sets the callback invoked when the user confirms.</summary>
    public Func<Task>? OnConfirmCallback { get; set; }

    /// <summary>Gets or sets the callback invoked when the user cancels.</summary>
    public Func<Task>? OnCancelCallback { get; set; }

    /// <summary>Handles backdrop click (cancel if not processing).</summary>
    [RelayCommand]
    public async Task HandleBackdropClickAsync()
    {
        if (!IsProcessing && OnCancelCallback != null)
        {
            await OnCancelCallback();
        }
    }
}
