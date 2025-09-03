using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using clypse.core.Secrets;

namespace clypse.portal.Components;

public partial class WebSecretForm : ComponentBase
{
    [Parameter] public WebSecret? Secret { get; set; }
    [Parameter] public bool IsEditMode { get; set; } = true;
    [Parameter] public EventCallback<WebSecret> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private WebSecret? EditableSecret { get; set; }
    private bool showPassword = false;
    private bool isSaving = false;
    private string newTag = string.Empty;

    protected override void OnParametersSet()
    {
        if (IsEditMode && Secret != null)
        {
            // Edit mode: Create a copy of the secret for editing to avoid modifying the original
            EditableSecret = WebSecret.FromSecret(Secret);
        }
        else if (!IsEditMode)
        {
            // Create mode: Create a new empty secret
            EditableSecret = new WebSecret();
        }
    }

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }

    private async Task HandleSave()
    {
        if (EditableSecret == null)
            return;

        try
        {
            isSaving = true;
            StateHasChanged();

            await OnSave.InvokeAsync(EditableSecret);
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleBackdropClick()
    {
        // Only close if not currently saving
        if (!isSaving)
        {
            await HandleCancel();
        }
    }

    private void HandleTagKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            AddTag();
        }
    }

    private void AddTag()
    {
        if (EditableSecret == null || string.IsNullOrWhiteSpace(newTag))
            return;

        var trimmedTag = newTag.Trim();
        if (EditableSecret.AddTag(trimmedTag))
        {
            newTag = string.Empty;
            StateHasChanged();
        }
    }

    private void RemoveTag(string tag)
    {
        if (EditableSecret == null)
            return;

        if (EditableSecret.RemoveTag(tag))
        {
            StateHasChanged();
        }
    }
}
