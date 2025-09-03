using Microsoft.AspNetCore.Components;
using clypse.portal.Models;

namespace clypse.portal.Components;

public partial class VaultDeleteConfirmDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public VaultMetadata? VaultToDelete { get; set; }
    [Parameter] public bool IsDeleting { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    
    private string confirmationText = string.Empty;

    protected override void OnParametersSet()
    {
        if (!Show)
        {
            confirmationText = string.Empty;
        }
    }

    private string GetConfirmationText()
    {
        if (VaultToDelete == null) return "";
        
        // If vault has a name, use that, otherwise use ID
        return !string.IsNullOrEmpty(VaultToDelete.Name) ? VaultToDelete.Name : VaultToDelete.Id;
    }
    
    private bool IsConfirmationValid()
    {
        if (VaultToDelete == null) return false;
        
        var expectedText = GetConfirmationText();
        return confirmationText.Trim().Equals(expectedText, StringComparison.Ordinal);
    }
}
