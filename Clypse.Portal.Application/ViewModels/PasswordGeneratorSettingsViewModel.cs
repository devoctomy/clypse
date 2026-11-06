using Blazing.Mvvm.ComponentModel;
using Clypse.Core.Enums;
using Clypse.Core.Password;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Settings;
using CommunityToolkit.Mvvm.Input;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the password generator's section in Settings: what the generator starts with, and the user's own
/// memorable templates, each tried before it is added. Kept on this device.
/// </summary>
public partial class PasswordGeneratorSettingsViewModel : ViewModelBase
{
    private readonly IPasswordGeneratorPreferencesService preferencesService;
    private readonly IPasswordGeneratorService generator;
    private readonly ClypseSettings settings;
    private readonly IToastService toasts;
    private readonly IStringLocalizer<ClypseText> text;

    private PasswordGeneratorPreferences preferences = new PasswordGeneratorPreferences();
    private string newTemplateName = string.Empty;
    private string newTemplate = string.Empty;
    private string? example;
    private string? error;
    private bool isLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordGeneratorSettingsViewModel"/> class.
    /// </summary>
    /// <param name="preferencesService">Keeps the preferences.</param>
    /// <param name="generator">Tries templates.</param>
    /// <param name="settings">Clypse's settings, with the built in templates.</param>
    /// <param name="toasts">Says when the preferences are saved.</param>
    /// <param name="text">Clypse's text.</param>
    public PasswordGeneratorSettingsViewModel(
        IPasswordGeneratorPreferencesService preferencesService,
        IPasswordGeneratorService generator,
        ClypseSettings settings,
        IToastService toasts,
        IStringLocalizer<ClypseText> text)
    {
        this.preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Gets a value indicating whether the preferences have been read.
    /// </summary>
    public bool IsLoaded
    {
        get => isLoaded;
        private set => SetProperty(ref isLoaded, value);
    }

    /// <summary>
    /// Gets the built in templates.
    /// </summary>
    public IReadOnlyList<MemorablePasswordTemplate> BuiltInTemplates => settings.MemorablePasswordTemplates
        .Where(t => !string.IsNullOrWhiteSpace(t.Name) && !string.IsNullOrWhiteSpace(t.Template))
        .ToList();

    /// <summary>
    /// Gets the user's own templates.
    /// </summary>
    public IReadOnlyList<MemorablePasswordTemplate> UserTemplates => preferences.UserTemplates;

    /// <summary>
    /// Gets every template, for the one the generator starts with.
    /// </summary>
    public IReadOnlyList<MemorablePasswordTemplate> Templates => preferencesService.GetTemplates(preferences);

    /// <summary>
    /// Gets or sets a value indicating whether the generator starts with memorable passwords.
    /// </summary>
    public bool IsMemorable
    {
        get => preferences.IsMemorable;
        set
        {
            preferences.IsMemorable = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets a random password's length.
    /// </summary>
    public int Length
    {
        get => preferences.Length;
        set
        {
            preferences.Length = Math.Clamp(value, PasswordGeneratorViewModel.MinimumLength, PasswordGeneratorViewModel.MaximumLength);
            OnPropertyChanged();
            OnPropertyChanged(nameof(LengthText));
        }
    }

    /// <summary>
    /// Gets the length label, e.g. "Length: 20".
    /// </summary>
    public string LengthText => text["Generator.Length", Length];

    /// <summary>
    /// Gets or sets a value indicating whether a random password has at least one character of each kind.
    /// </summary>
    public bool AtLeastOneOfEach
    {
        get => preferences.AtLeastOneOfEach;
        set
        {
            preferences.AtLeastOneOfEach = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the name of the template the generator starts with.
    /// </summary>
    public string? Template
    {
        get => Templates.FirstOrDefault(t => t.Name == preferences.Template)?.Name ?? Templates.FirstOrDefault()?.Name;
        set
        {
            preferences.Template = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the new template's name.
    /// </summary>
    public string NewTemplateName
    {
        get => newTemplateName;
        set => SetProperty(ref newTemplateName, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the new template.
    /// </summary>
    public string NewTemplate
    {
        get => newTemplate;
        set
        {
            if (SetProperty(ref newTemplate, value ?? string.Empty))
            {
                Example = null;
            }
        }
    }

    /// <summary>
    /// Gets a password made from the new template, once it is tried.
    /// </summary>
    public string? Example
    {
        get => example;
        private set => SetProperty(ref example, value);
    }

    /// <summary>
    /// Gets why the template was not added, or null.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <inheritdoc/>
    public override async Task OnInitializedAsync()
    {
        preferences = await preferencesService.GetAsync();
        IsLoaded = true;
        RaiseAll();
    }

    /// <summary>
    /// Gets a value indicating whether a kind of character is used in random passwords.
    /// </summary>
    /// <param name="group">The kind of character.</param>
    /// <returns>True if it is used.</returns>
    public bool Uses(CharacterGroup group) => preferences.Groups.HasFlag(group);

    /// <summary>
    /// Uses a kind of character in random passwords, or stops using it. At least one kind is always used.
    /// </summary>
    /// <param name="group">The kind of character.</param>
    /// <param name="use">True to use it.</param>
    public void SetGroup(CharacterGroup group, bool use)
    {
        var groups = use ? preferences.Groups | group : preferences.Groups & ~group;
        if (groups != CharacterGroup.None)
        {
            preferences.Groups = groups;
        }

        OnPropertyChanged(nameof(Uses));
    }

    /// <summary>
    /// Makes a password from the new template, to show what it makes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task TryTemplateAsync()
    {
        Error = null;
        Example = await TryAsync(NewTemplate);
        if (Example == null)
        {
            Error = text["GeneratorSettings.Template.Invalid"];
        }
    }

    /// <summary>
    /// Adds the new template, once it has a name no other template has and makes a password.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task AddTemplateAsync()
    {
        Error = null;
        var name = NewTemplateName.Trim();
        if (name.Length == 0)
        {
            Error = text["GeneratorSettings.Template.NameRequired"];
            return;
        }

        if (Templates.Any(t => string.Equals(t.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            Error = text["GeneratorSettings.Template.NameTaken", name];
            return;
        }

        if (await TryAsync(NewTemplate) == null)
        {
            Error = text["GeneratorSettings.Template.Invalid"];
            return;
        }

        preferences.UserTemplates.Add(new MemorablePasswordTemplate { Name = name, Template = NewTemplate.Trim() });
        NewTemplateName = string.Empty;
        NewTemplate = string.Empty;
        RaiseAll();
    }

    /// <summary>
    /// Removes one of the user's own templates.
    /// </summary>
    /// <param name="name">The template's name.</param>
    [RelayCommand]
    public void RemoveTemplate(string name)
    {
        preferences.UserTemplates.RemoveAll(t => t.Name == name);
        RaiseAll();
    }

    /// <summary>
    /// Saves the preferences.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task SaveAsync()
    {
        preferences.Template = Template;
        await preferencesService.SaveAsync(preferences);
        toasts.Show(text["GeneratorSettings.Saved"], ToastTone.Success);
    }

    private async Task<string?> TryAsync(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        try
        {
            var password = await generator.GenerateMemorablePasswordAsync(template.Trim(), false, CancellationToken.None);
            return string.IsNullOrEmpty(password) ? null : password;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(UserTemplates));
        OnPropertyChanged(nameof(Templates));
        OnPropertyChanged(nameof(IsMemorable));
        OnPropertyChanged(nameof(Length));
        OnPropertyChanged(nameof(LengthText));
        OnPropertyChanged(nameof(AtLeastOneOfEach));
        OnPropertyChanged(nameof(Template));
    }
}
