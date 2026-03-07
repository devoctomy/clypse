using Blazing.Mvvm.ComponentModel;
using clypse.core.Enums;
using clypse.core.Password;
using clypse.portal.Models.Enums;
using clypse.portal.Models.Settings;
using CommunityToolkit.Mvvm.Input;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the password generator dialog.
/// </summary>
public partial class PasswordGeneratorDialogViewModel : ViewModelBase
{
    private readonly IPasswordGeneratorService passwordGeneratorService;
    private readonly AppSettings appSettings;

    private string generatedPassword = string.Empty;
    private PasswordType selectedPasswordType = PasswordType.Memorable;
    private List<MemorablePasswordTemplateItem> memorablePasswordTemplates = [];
    private string selectedTemplateName = string.Empty;
    private bool shuffleTokens = true;
    private int passwordLength = 16;
    private bool includeLowercase = true;
    private bool includeUppercase = true;
    private bool includeDigits = true;
    private bool includeSpecial = true;
    private bool atLeastOneOfEachGroup = true;
    private bool isInitialized;

    /// <summary>
    /// Initializes a new instance of <see cref="PasswordGeneratorDialogViewModel"/>.
    /// </summary>
    public PasswordGeneratorDialogViewModel(IPasswordGeneratorService passwordGeneratorService, AppSettings appSettings)
    {
        this.passwordGeneratorService = passwordGeneratorService ?? throw new ArgumentNullException(nameof(passwordGeneratorService));
        this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    /// <summary>Gets the currently generated password.</summary>
    public string GeneratedPassword { get => generatedPassword; private set => SetProperty(ref generatedPassword, value); }

    /// <summary>Gets or sets the selected password type.</summary>
    public PasswordType SelectedPasswordType
    {
        get => selectedPasswordType;
        set
        {
            SetProperty(ref selectedPasswordType, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets the list of memorable password templates.</summary>
    public List<MemorablePasswordTemplateItem> MemorablePasswordTemplates { get => memorablePasswordTemplates; private set => SetProperty(ref memorablePasswordTemplates, value); }

    /// <summary>Gets or sets the selected template name.</summary>
    public string SelectedTemplateName
    {
        get => selectedTemplateName;
        set
        {
            SetProperty(ref selectedTemplateName, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets or sets a value indicating whether to shuffle tokens.</summary>
    public bool ShuffleTokens
    {
        get => shuffleTokens;
        set
        {
            SetProperty(ref shuffleTokens, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets or sets the desired password length.</summary>
    public int PasswordLength
    {
        get => passwordLength;
        set
        {
            SetProperty(ref passwordLength, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets or sets whether to include lowercase characters.</summary>
    public bool IncludeLowercase
    {
        get => includeLowercase;
        set
        {
            SetProperty(ref includeLowercase, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets or sets whether to include uppercase characters.</summary>
    public bool IncludeUppercase
    {
        get => includeUppercase;
        set
        {
            SetProperty(ref includeUppercase, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets or sets whether to include digit characters.</summary>
    public bool IncludeDigits
    {
        get => includeDigits;
        set
        {
            SetProperty(ref includeDigits, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets or sets whether to include special characters.</summary>
    public bool IncludeSpecial
    {
        get => includeSpecial;
        set
        {
            SetProperty(ref includeSpecial, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets or sets whether to require at least one character from each group.</summary>
    public bool AtLeastOneOfEachGroup
    {
        get => atLeastOneOfEachGroup;
        set
        {
            SetProperty(ref atLeastOneOfEachGroup, value);
            TriggerGenerateAsync();
        }
    }

    /// <summary>Gets or sets the callback invoked when a password is accepted.</summary>
    public Func<string, Task>? OnPasswordGeneratedCallback { get; set; }

    /// <summary>Gets or sets the callback invoked when the dialog is cancelled.</summary>
    public Func<Task>? OnCancelCallback { get; set; }

    /// <summary>
    /// Initializes the ViewModel for first display.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        MemorablePasswordTemplates = appSettings.MemorablePasswordTemplates?.ToList() ?? [];

        if (MemorablePasswordTemplates.Count > 0)
        {
            selectedTemplateName = MemorablePasswordTemplates[0].Name;
            OnPropertyChanged(nameof(SelectedTemplateName));
        }

        await GeneratePasswordAsync();
    }

    /// <summary>
    /// Resets initialization so the dialog can re-initialize on next show.
    /// </summary>
    public void Reset()
    {
        isInitialized = false;
    }

    /// <summary>Regenerates the password.</summary>
    [RelayCommand]
    public Task RegenerateAsync() => GeneratePasswordAsync();

    /// <summary>Accepts the generated password and notifies the caller.</summary>
    [RelayCommand]
    public async Task AcceptPasswordAsync()
    {
        if (OnPasswordGeneratedCallback != null)
        {
            await OnPasswordGeneratedCallback(GeneratedPassword);
        }
    }

    /// <summary>Cancels the dialog.</summary>
    [RelayCommand]
    public async Task CancelAsync()
    {
        if (OnCancelCallback != null)
        {
            await OnCancelCallback();
        }
    }

    private void TriggerGenerateAsync()
    {
        _ = GeneratePasswordAsync();
    }

    private async Task GeneratePasswordAsync()
    {
        try
        {
            if (SelectedPasswordType == PasswordType.Memorable)
            {
                var template = MemorablePasswordTemplates.FirstOrDefault(t => t.Name == SelectedTemplateName);
                if (template != null && !string.IsNullOrEmpty(template.Template))
                {
                    GeneratedPassword = await passwordGeneratorService.GenerateMemorablePasswordAsync(template.Template, ShuffleTokens, CancellationToken.None);
                }
                else
                {
                    GeneratedPassword = "No template selected";
                }
            }
            else
            {
                var characterGroups = CharacterGroup.None;
                if (IncludeLowercase)
                {
                    characterGroups |= CharacterGroup.Lowercase;
                }

                if (IncludeUppercase)
                {
                    characterGroups |= CharacterGroup.Uppercase;
                }

                if (IncludeDigits)
                {
                    characterGroups |= CharacterGroup.Digits;
                }

                if (IncludeSpecial)
                {
                    characterGroups |= CharacterGroup.Special;
                }

                if (characterGroups == CharacterGroup.None)
                {
                    GeneratedPassword = "Please select at least one character type";
                }
                else
                {
                    GeneratedPassword = passwordGeneratorService.GenerateRandomPassword(characterGroups, PasswordLength, AtLeastOneOfEachGroup);
                }
            }
        }
        catch (Exception ex)
        {
            GeneratedPassword = $"Error: {ex.Message}";
        }
    }
}
