using Microsoft.AspNetCore.Components;

namespace clypse.portal.Components;

public partial class ConfirmDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public string Message { get; set; } = "Are you sure you want to delete this item?";
    [Parameter] public bool IsProcessing { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task HandleBackdropClick()
    {
        // Only close if not currently processing
        if (!IsProcessing)
        {
            await OnCancel.InvokeAsync();
        }
    }
}
