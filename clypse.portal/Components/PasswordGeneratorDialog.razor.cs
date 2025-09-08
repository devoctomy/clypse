using Microsoft.AspNetCore.Components;

namespace clypse.portal.Components;

public partial class PasswordGeneratorDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public EventCallback<string> OnPasswordGenerated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task HandleOk()
    {
        // Return "Foobar" as requested
        await OnPasswordGenerated.InvokeAsync("Foobar");
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleBackdropClick()
    {
        await HandleCancel();
    }
}
