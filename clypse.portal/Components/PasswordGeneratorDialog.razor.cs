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

    protected override async Task OnParametersSetAsync()
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
            await GeneratePasswordAsync();
        }
    }

    protected async Task SetPasswordTypeAsync(PasswordType passwordType)
    {
        selectedPasswordType = passwordType;
        await GeneratePasswordAsync();
        StateHasChanged();
    }

    protected async Task OnTemplateChangedAsync()
    {
        await GeneratePasswordAsync();
    }

    protected async Task OnOptionsChangedAsync()
    {
        await GeneratePasswordAsync();
    }

    protected async Task RegeneratePasswordAsync()
    {
        await GeneratePasswordAsync();
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
                    var result = await PasswordGeneratorService.GenerateMemorablePasswordAsync(selectedTemplateItem.Template, shuffleTokens, CancellationToken.None);
                    generatedPassword = result ?? "Failed to generate password";
                    if (string.IsNullOrEmpty(generatedPassword))
                    {
                        generatedPassword = "Generated password was empty";
                    }
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
                    generatedPassword = PasswordGeneratorService.GenerateRandomPassword(characterGroups, passwordLength, atLeastOneOfEachGroup);
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

    private async Task HandleBackdropClick()
    {
        await HandleCancel();
    }
}
