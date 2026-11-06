using Blazing.Mvvm.ComponentModel;
using Clypse.Core.Enums;
using Clypse.Core.Password;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the password generator: a random password from chosen kinds of character, or a memorable one from a
/// template (the built in <see cref="ClypseSettings.MemorablePasswordTemplates"/> and the user's own). It starts with
/// the user's preferences (<see cref="IPasswordGeneratorPreferencesService"/>), and makes a new password whenever an
/// option changes.
/// </summary>
public partial class PasswordGeneratorViewModel : ViewModelBase
{
    /// <summary>
    /// The shortest random password.
    /// </summary>
    public const int MinimumLength = 8;

    /// <summary>
    /// The longest random password.
    /// </summary>
    public const int MaximumLength = 64;

    /// <summary>
    /// The kinds of character, in the order they are offered.
    /// </summary>
    public static readonly IReadOnlyList<CharacterGroup> Groups =
        [CharacterGroup.Lowercase, CharacterGroup.Uppercase, CharacterGroup.Digits, CharacterGroup.Special];

    private readonly IPasswordGeneratorService generator;
    private readonly IPasswordStrengthService strengthService;
    private readonly IPasswordGeneratorPreferencesService preferencesService;
    private readonly IStringLocalizer<ClypseText> text;

    private IReadOnlyList<MemorablePasswordTemplate> templates = [];
    private bool isMemorable;
    private int length = PasswordGeneratorPreferences.DefaultLength;
    private CharacterGroup groups = CharacterGroup.Lowercase | CharacterGroup.Uppercase | CharacterGroup.Digits | CharacterGroup.Special;
    private bool atLeastOneOfEach = true;
    private string? template;
    private bool shuffle;
    private string password = string.Empty;
    private PasswordStrength strength = PasswordStrength.None;
    private string? error;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordGeneratorViewModel"/> class.
    /// </summary>
    /// <param name="generator">Makes passwords.</param>
    /// <param name="strengthService">Estimates their strength.</param>
    /// <param name="preferencesService">What the generator starts with, and the templates.</param>
    /// <param name="text">Clypse's text.</param>
    public PasswordGeneratorViewModel(
        IPasswordGeneratorService generator,
        IPasswordStrengthService strengthService,
        IPasswordGeneratorPreferencesService preferencesService,
        IStringLocalizer<ClypseText> text)
    {
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        this.strengthService = strengthService ?? throw new ArgumentNullException(nameof(strengthService));
        this.preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Gets the templates for memorable passwords: the built in ones, then the user's own.
    /// </summary>
    public IReadOnlyList<MemorablePasswordTemplate> Templates
    {
        get => templates;
        private set
        {
            if (SetProperty(ref templates, value))
            {
                OnPropertyChanged(nameof(CanMakeMemorable));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether memorable passwords can be made (there is a template).
    /// </summary>
    public bool CanMakeMemorable => Templates.Count > 0;

    /// <summary>
    /// Gets a value indicating whether a memorable password is made, rather than a random one.
    /// </summary>
    public bool IsMemorable
    {
        get => isMemorable;
        private set => SetProperty(ref isMemorable, value);
    }

    /// <summary>
    /// Gets a random password's length.
    /// </summary>
    public int Length
    {
        get => length;
        private set => SetProperty(ref length, value);
    }

    /// <summary>
    /// Gets a value indicating whether a random password has at least one character of each kind chosen.
    /// </summary>
    public bool AtLeastOneOfEach
    {
        get => atLeastOneOfEach;
        private set => SetProperty(ref atLeastOneOfEach, value);
    }

    /// <summary>
    /// Gets the name of the memorable template in use.
    /// </summary>
    public string? Template
    {
        get => template;
        private set => SetProperty(ref template, value);
    }

    /// <summary>
    /// Gets a value indicating whether a memorable password's words are shuffled.
    /// </summary>
    public bool Shuffle
    {
        get => shuffle;
        private set => SetProperty(ref shuffle, value);
    }

    /// <summary>
    /// Gets the password made.
    /// </summary>
    public string Password
    {
        get => password;
        private set => SetProperty(ref password, value);
    }

    /// <summary>
    /// Gets the password's strength.
    /// </summary>
    public PasswordStrength Strength
    {
        get => strength;
        private set
        {
            if (SetProperty(ref strength, value))
            {
                OnPropertyChanged(nameof(StrengthText));
            }
        }
    }

    /// <summary>
    /// Gets the password's strength, as the meter words it.
    /// </summary>
    public string StrengthText => strengthService.Describe(Strength);

    /// <summary>
    /// Gets why no password could be made, or null.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>
    /// Gets the length label, e.g. "Length: 20".
    /// </summary>
    public string LengthText => text["Generator.Length", Length];

    /// <inheritdoc/>
    public override async Task OnInitializedAsync()
    {
        var preferences = await preferencesService.GetAsync();
        Templates = preferencesService.GetTemplates(preferences);
        Length = Math.Clamp(preferences.Length, MinimumLength, MaximumLength);
        groups = preferences.Groups;
        AtLeastOneOfEach = preferences.AtLeastOneOfEach;
        Template = Templates.FirstOrDefault(t => t.Name == preferences.Template)?.Name ?? Templates.FirstOrDefault()?.Name;
        IsMemorable = preferences.IsMemorable && CanMakeMemorable;
        OnPropertyChanged(nameof(LengthText));
        await GenerateAsync();
    }

    /// <summary>
    /// Gets a value indicating whether a kind of character is used in random passwords.
    /// </summary>
    /// <param name="group">The kind of character.</param>
    /// <returns>True if it is used.</returns>
    public bool Uses(CharacterGroup group) => groups.HasFlag(group);

    /// <summary>
    /// Makes memorable passwords, or random ones.
    /// </summary>
    /// <param name="memorable">True for memorable passwords.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SetMemorableAsync(bool memorable)
    {
        IsMemorable = memorable && CanMakeMemorable;
        return GenerateAsync();
    }

    /// <summary>
    /// Sets a random password's length.
    /// </summary>
    /// <param name="value">The length, kept between <see cref="MinimumLength"/> and <see cref="MaximumLength"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SetLengthAsync(int value)
    {
        Length = Math.Clamp(value, MinimumLength, MaximumLength);
        OnPropertyChanged(nameof(LengthText));
        return GenerateAsync();
    }

    /// <summary>
    /// Uses a kind of character in random passwords, or stops using it.
    /// </summary>
    /// <param name="group">The kind of character.</param>
    /// <param name="use">True to use it.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SetGroupAsync(CharacterGroup group, bool use)
    {
        groups = use ? groups | group : groups & ~group;
        return GenerateAsync();
    }

    /// <summary>
    /// Sets whether a random password has at least one character of each kind chosen.
    /// </summary>
    /// <param name="value">True to have at least one of each.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SetAtLeastOneOfEachAsync(bool value)
    {
        AtLeastOneOfEach = value;
        return GenerateAsync();
    }

    /// <summary>
    /// Uses a memorable template.
    /// </summary>
    /// <param name="name">The template's name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SetTemplateAsync(string? name)
    {
        Template = Templates.FirstOrDefault(t => t.Name == name)?.Name ?? Templates.FirstOrDefault()?.Name;
        return GenerateAsync();
    }

    /// <summary>
    /// Sets whether a memorable password's words are shuffled.
    /// </summary>
    /// <param name="value">True to shuffle them.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SetShuffleAsync(bool value)
    {
        Shuffle = value;
        return GenerateAsync();
    }

    /// <summary>
    /// Makes a new password with the options chosen.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task GenerateAsync()
    {
        Error = null;
        if (IsMemorable)
        {
            var chosen = Templates.First(t => t.Name == Template);
            Password = await generator.GenerateMemorablePasswordAsync(chosen.Template, Shuffle, CancellationToken.None);
        }
        else if (groups == CharacterGroup.None)
        {
            Password = string.Empty;
            Error = text["Generator.NoGroups"];
        }
        else
        {
            Password = generator.GenerateRandomPassword(groups, Length, AtLeastOneOfEach);
        }

        Strength = await strengthService.EstimateAsync(Password);
    }
}
