using Microsoft.AspNetCore.Components;
using clypse.core.Password;
using clypse.core.Enums;
using clypse.portal.Models;

namespace clypse.portal.Components;

public partial class PasswordGeneratorDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public EventCallback<string> OnPasswordGenerated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Inject] public IPasswordGeneratorService PasswordGeneratorService { get; set; } = default!;
    [Inject] public AppSettings AppSettings { get; set; } = default!;

    protected enum PasswordType
    {
        Memorable,
        Random
    }

    // UI State
    protected string generatedPassword = string.Empty;
    protected PasswordType selectedPasswordType = PasswordType.Memorable;
    
    // Memorable Password Options
    protected List<MemorablePasswordTemplateItem>? memorablePasswordTemplates;
    protected string selectedTemplateName = string.Empty;
    protected bool shuffleTokens = true;
    
    // Random Password Options
    protected int passwordLength = 16;
    protected bool includeLowercase = true;
    protected bool includeUppercase = true;
    protected bool includeDigits = true;
    protected bool includeSpecial = true;

    protected override void OnParametersSet()
    {
        if (Show && memorablePasswordTemplates == null)
        {
            // Initialize templates from AppSettings
            memorablePasswordTemplates = AppSettings.MemorablePasswordTemplates?.ToList() ?? new List<MemorablePasswordTemplateItem>();
            
            // Set default template if available
            if (memorablePasswordTemplates.Count > 0)
            {
                selectedTemplateName = memorablePasswordTemplates[0].Name;
            }
            
            // Generate initial password
            _ = InvokeAsync(GeneratePasswordAsync);
        }
    }

    protected void SetPasswordType(PasswordType passwordType)
    {
        selectedPasswordType = passwordType;
        _ = InvokeAsync(GeneratePasswordAsync);
        StateHasChanged();
    }

    protected void OnTemplateChanged()
    {
        _ = InvokeAsync(GeneratePasswordAsync);
    }

    protected void OnOptionsChanged()
    {
        _ = InvokeAsync(GeneratePasswordAsync);
    }

    protected void RegeneratePassword()
    {
        _ = InvokeAsync(GeneratePasswordAsync);
    }

    private async Task GeneratePasswordAsync()
    {
        try
        {
            if (selectedPasswordType == PasswordType.Memorable)
            {
                var selectedTemplateItem = memorablePasswordTemplates?.FirstOrDefault(t => t.Name == selectedTemplateName);
                if (selectedTemplateItem != null && !string.IsNullOrEmpty(selectedTemplateItem.Template))
                {
                    generatedPassword = await PasswordGeneratorService.GenerateMemorablePasswordAsync(selectedTemplateItem.Template, shuffleTokens, CancellationToken.None);
                }
                else
                {
                    generatedPassword = "No template selected";
                }
            }
            else // Random
            {
                var characterGroups = CharacterGroup.None;
                
                if (includeLowercase) characterGroups |= CharacterGroup.Lowercase;
                if (includeUppercase) characterGroups |= CharacterGroup.Uppercase;
                if (includeDigits) characterGroups |= CharacterGroup.Digits;
                if (includeSpecial) characterGroups |= CharacterGroup.Special;

                if (characterGroups == CharacterGroup.None)
                {
                    generatedPassword = "Please select at least one character type";
                }
                else
                {
                    // The 'atLeastOneOfEachGroup' variable does not exist. You need to define it or replace it with a boolean value as required by the method signature.
                    // For now, set to 'true' to ensure at least one character from each group is included:
                    generatedPassword = PasswordGeneratorService.GenerateRandomPassword(characterGroups, passwordLength, true);
                }
            }
        }
        catch (Exception ex)
        {
            generatedPassword = $"Error: {ex.Message}";
        }

        StateHasChanged();
    }

    private async Task HandleOk()
    {
        await OnPasswordGenerated.InvokeAsync(generatedPassword);
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }
}
