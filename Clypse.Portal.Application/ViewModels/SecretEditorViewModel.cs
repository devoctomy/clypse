using Blazing.Mvvm.ComponentModel;
using Clypse.Core.Base;
using Clypse.Core.Enums;
using Clypse.Core.Extensions;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.UI.Text.Interfaces;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the secret editor: a new secret of a chosen kind, or a copy of one of the open vault's secrets. Its
/// fields come from the secret's type (<see cref="SecretFieldAttribute"/>), and their values are edited in a copy of the
/// secret's data, so nothing changes until it is saved.
/// </summary>
public partial class SecretEditorViewModel : ViewModelBase
{
    /// <summary>
    /// The kinds of secret that can be created, in the order they are offered.
    /// </summary>
    public static readonly IReadOnlyList<SecretType> SecretTypes = [SecretType.Web, SecretType.Aws, SecretType.None];

    private readonly IVaultSession session;
    private readonly IPasswordStrengthService strengthService;
    private readonly IErrorText errorText;
    private readonly Dictionary<string, string> values = [];
    private readonly Dictionary<string, PasswordStrength> strengths = [];
    private readonly HashSet<string> revealed = [];

    private Secret? original;
    private SecretType secretType = SecretType.Web;
    private IReadOnlyList<SecretField> fields = [];
    private bool isWorking;
    private bool isConfirmingDelete;
    private string? error;
    private string? generatingFor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretEditorViewModel"/> class.
    /// </summary>
    /// <param name="session">The open vault, which saves and deletes the secret.</param>
    /// <param name="strengthService">Estimates passwords' strength.</param>
    /// <param name="errorText">Describes errors.</param>
    public SecretEditorViewModel(IVaultSession session, IPasswordStrengthService strengthService, IErrorText errorText)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.strengthService = strengthService ?? throw new ArgumentNullException(nameof(strengthService));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        Fields = FieldsOf(SecretType.Web);
    }

    /// <summary>
    /// Gets a value indicating whether the secret is new.
    /// </summary>
    public bool IsNew => original == null;

    /// <summary>
    /// Gets the secret's kind. Only a new secret's can be changed.
    /// </summary>
    public SecretType SecretType
    {
        get => secretType;
        private set => SetProperty(ref secretType, value);
    }

    /// <summary>
    /// Gets the fields of the secret's kind, in order.
    /// </summary>
    public IReadOnlyList<SecretField> Fields
    {
        get => fields;
        private set => SetProperty(ref fields, value);
    }

    /// <summary>
    /// Gets when the secret was last saved (UTC), or null if it is new.
    /// </summary>
    public DateTime? LastUpdatedAt => original == null ? null : DateTime.SpecifyKind(original.LastUpdatedAt, DateTimeKind.Utc);

    /// <summary>
    /// Gets a value indicating whether the secret is being saved or deleted.
    /// </summary>
    public bool IsWorking
    {
        get => isWorking;
        private set => SetProperty(ref isWorking, value);
    }

    /// <summary>
    /// Gets a value indicating whether the editor is asking to confirm deleting the secret.
    /// </summary>
    public bool IsConfirmingDelete
    {
        get => isConfirmingDelete;
        private set => SetProperty(ref isConfirmingDelete, value);
    }

    /// <summary>
    /// Gets why the secret was not saved or deleted, or null.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>
    /// Gets the field the password generator is open for, or null if it is closed.
    /// </summary>
    public string? GeneratingFor
    {
        get => generatingFor;
        private set => SetProperty(ref generatingFor, value);
    }

    /// <summary>
    /// Starts editing: a copy of one of the open vault's secrets, or a new secret.
    /// </summary>
    /// <param name="secretId">The secret's id, or null for a new secret.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task LoadAsync(string? secretId)
    {
        values.Clear();
        strengths.Clear();
        revealed.Clear();
        Error = null;
        IsConfirmingDelete = false;
        GeneratingFor = null;

        original = secretId == null ? null : session.Secrets.FirstOrDefault(s => s.Id == secretId);
        if (original != null)
        {
            foreach (var (key, value) in original.Data)
            {
                values[key] = value;
            }
        }

        SecretType = original?.SecretType ?? SecretType.Web;
        Fields = FieldsOf(SecretType);
        OnPropertyChanged(nameof(IsNew));
        OnPropertyChanged(nameof(LastUpdatedAt));
        foreach (var field in Fields.Where(f => f.FieldType == SecretFieldType.Password))
        {
            await UpdateStrengthAsync(field.Name);
        }
    }

    /// <summary>
    /// Changes a new secret's kind, keeping the values of the fields both kinds have.
    /// </summary>
    /// <param name="type">The kind of secret.</param>
    public void SelectType(SecretType type)
    {
        if (!IsNew || type == SecretType)
        {
            return;
        }

        SecretType = type;
        Fields = FieldsOf(type);
    }

    /// <summary>
    /// Gets a field's value.
    /// </summary>
    /// <param name="field">The field's name.</param>
    /// <returns>The value, or an empty string.</returns>
    public string GetValue(string field) => values.GetValueOrDefault(field) ?? string.Empty;

    /// <summary>
    /// Sets a field's value, and its strength if it is a password.
    /// </summary>
    /// <param name="field">The field's name.</param>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SetValueAsync(string field, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(field);

        values[field] = value ?? string.Empty;
        if (Fields.Any(f => f.Name == field && f.FieldType == SecretFieldType.Password))
        {
            await UpdateStrengthAsync(field);
        }
    }

    /// <summary>
    /// Gets a password field's strength.
    /// </summary>
    /// <param name="field">The field's name.</param>
    /// <returns>The strength.</returns>
    public PasswordStrength GetStrength(string field) => strengths.GetValueOrDefault(field) ?? PasswordStrength.None;

    /// <summary>
    /// Words a password field's strength.
    /// </summary>
    /// <param name="field">The field's name.</param>
    /// <returns>The text.</returns>
    public string GetStrengthText(string field) => strengthService.Describe(GetStrength(field));

    /// <summary>
    /// Gets a value indicating whether a password field is shown in plain text.
    /// </summary>
    /// <param name="field">The field's name.</param>
    /// <returns>True if it is shown.</returns>
    public bool IsRevealed(string field) => revealed.Contains(field);

    /// <summary>
    /// Shows a password field in plain text, or hides it again.
    /// </summary>
    /// <param name="field">The field's name.</param>
    public void ToggleReveal(string field)
    {
        if (!revealed.Remove(field))
        {
            revealed.Add(field);
        }
    }

    /// <summary>
    /// Opens the password generator for a field.
    /// </summary>
    /// <param name="field">The field's name.</param>
    public void OpenGenerator(string field)
    {
        GeneratingFor = field;
    }

    /// <summary>
    /// Closes the password generator, putting the password it made in its field (if one was chosen).
    /// </summary>
    /// <param name="password">The password chosen, or null if none was.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task CloseGeneratorAsync(string? password)
    {
        var field = GeneratingFor;
        GeneratingFor = null;
        if (field != null && password != null)
        {
            revealed.Add(field);
            await SetValueAsync(field, password);
        }
    }

    /// <summary>
    /// Saves the secret to the open vault.
    /// </summary>
    /// <returns>True if it was saved.</returns>
    public async Task<bool> SaveAsync()
    {
        if (IsWorking)
        {
            return false;
        }

        IsWorking = true;
        Error = null;
        try
        {
            await session.SaveAsync(Build());
            return true;
        }
        catch (Exception ex)
        {
            Error = errorText.Describe(ex);
            return false;
        }
        finally
        {
            IsWorking = false;
        }
    }

    /// <summary>
    /// Asks to confirm deleting the secret, or stops asking.
    /// </summary>
    /// <param name="confirming">True to ask.</param>
    public void ConfirmDelete(bool confirming)
    {
        IsConfirmingDelete = confirming && !IsNew;
    }

    /// <summary>
    /// Deletes the secret from the open vault.
    /// </summary>
    /// <returns>True if it was deleted.</returns>
    public async Task<bool> DeleteAsync()
    {
        if (IsWorking || original == null)
        {
            return false;
        }

        IsWorking = true;
        Error = null;
        try
        {
            await session.DeleteAsync(original.Id);
            return true;
        }
        catch (Exception ex)
        {
            Error = errorText.Describe(ex);
            return false;
        }
        finally
        {
            IsWorking = false;
            IsConfirmingDelete = false;
        }
    }

    private static Secret Create(SecretType type) => type switch
    {
        SecretType.Web => new WebSecret(),
        SecretType.Aws => new AwsCredentials(),
        _ => new Secret(),
    };

    private static IReadOnlyList<SecretField> FieldsOf(SecretType type)
    {
        var fields = Create(type).GetOrderedSecretFields() ?? [];
        return fields
            .Select(f => new SecretField
            {
                Name = f.Key.Name,
                FieldType = f.Value.FieldType,
                IsRequired = f.Key.GetCustomAttributes(typeof(RequiredDataAttribute), true).Length > 0,
            })
            .ToList();
    }

    private static string NormaliseTags(string value) => string.Join(
        ',',
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.CurrentCultureIgnoreCase));

    private Secret Build()
    {
        // A copy: the secret in the vault session changes only once it is saved.
        var secret = Create(SecretType);
        if (original != null)
        {
            secret.SetAllData(new Dictionary<string, string>(original.Data));
        }

        foreach (var field in Fields)
        {
            // Passwords and multi-line text are kept exactly as entered.
            var value = GetValue(field.Name);
            var stored = field.FieldType switch
            {
                SecretFieldType.TagList => NormaliseTags(value),
                SecretFieldType.SingleLineText => value.Trim(),
                _ => value,
            };
            secret.SetData(field.Name, stored);
        }

        return secret;
    }

    private async Task UpdateStrengthAsync(string field)
    {
        strengths[field] = await strengthService.EstimateAsync(GetValue(field));
    }
}
