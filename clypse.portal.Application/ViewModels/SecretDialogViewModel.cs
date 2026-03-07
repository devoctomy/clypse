using System.Reflection;
using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using clypse.core.Enums;
using clypse.core.Extensions;
using clypse.core.Secrets;
using clypse.portal.Models.Enums;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the secret create/view/edit dialog.
/// </summary>
public partial class SecretDialogViewModel : ViewModelBase
{
    private Secret? editableSecret;
    private Dictionary<PropertyInfo, SecretFieldAttribute>? secretFields;
    private bool isSaving;
    private CrudDialogMode mode = CrudDialogMode.Create;

    /// <summary>Gets or sets the working copy of the secret being edited.</summary>
    public Secret? EditableSecret { get => editableSecret; private set => SetProperty(ref editableSecret, value); }

    /// <summary>Gets or sets the ordered fields for the current secret type.</summary>
    public Dictionary<PropertyInfo, SecretFieldAttribute>? SecretFields { get => secretFields; private set => SetProperty(ref secretFields, value); }

    /// <summary>Gets a value indicating whether a save operation is in progress.</summary>
    public bool IsSaving { get => isSaving; private set => SetProperty(ref isSaving, value); }

    /// <summary>Gets or sets the current dialog mode (Create/Update/View).</summary>
    public CrudDialogMode Mode { get => mode; set => SetProperty(ref mode, value); }

    /// <summary>Gets or sets the callback invoked when the secret is saved.</summary>
    public Func<Secret, Task>? OnSaveCallback { get; set; }

    /// <summary>Gets or sets the callback invoked when the dialog is cancelled.</summary>
    public Func<Task>? OnCancelCallback { get; set; }

    /// <summary>
    /// Initializes the dialog for editing a secret.
    /// </summary>
    public void InitializeForSecret(Secret secret, CrudDialogMode dialogMode)
    {
        Mode = dialogMode;
        EditableSecret = CreateWorkingCopy(secret);
        SecretFields = EditableSecret?.GetOrderedSecretFields();
    }

    /// <summary>
    /// Clears the dialog state (called when dialog is hidden).
    /// </summary>
    public void Clear()
    {
        EditableSecret = null;
        SecretFields = null;
        IsSaving = false;
    }

    private static Secret CreateWorkingCopy(Secret original)
    {
        var workingCopy = (Secret)Activator.CreateInstance(original.GetType())!;
        workingCopy.SetAllData(original.Data);
        return workingCopy.CastSecretToCorrectType();
    }

    /// <summary>
    /// Called when the secret type selection changes.
    /// </summary>
    public void OnSecretTypeChanged(SecretType newSecretType)
    {
        if (EditableSecret == null)
        {
            return;
        }

        EditableSecret.SecretType = newSecretType;
        EditableSecret = EditableSecret.CastSecretToCorrectType();
        SecretFields = EditableSecret?.GetOrderedSecretFields();
    }

    /// <summary>Returns the mode-specific icon name (without 'bi-' prefix).</summary>
    public static string GetModeIcon() => "person-badge";

    /// <summary>Returns the mode-specific dialog title.</summary>
    public string GetModeTitle()
    {
        return Mode switch
        {
            CrudDialogMode.Create => "Create Secret",
            CrudDialogMode.Update => "Update Secret",
            CrudDialogMode.View => "View Secret",
            _ => "Secret"
        };
    }

    /// <summary>Saves the edited secret.</summary>
    [RelayCommand]
    public async Task HandleSaveAsync()
    {
        if (EditableSecret == null || Mode == CrudDialogMode.View)
        {
            return;
        }

        try
        {
            IsSaving = true;
            if (OnSaveCallback != null)
            {
                await OnSaveCallback(EditableSecret);
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Cancels the dialog.</summary>
    [RelayCommand]
    public async Task HandleCancelAsync()
    {
        if (OnCancelCallback != null)
        {
            await OnCancelCallback();
        }
    }
}
