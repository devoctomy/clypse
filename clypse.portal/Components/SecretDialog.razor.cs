using Microsoft.AspNetCore.Components;
using clypse.core.Secrets;
using clypse.core.Secrets.Interfaces;
using clypse.core.Enums;
using clypse.core.Extensions;
using clypse.portal.Components.Fields;
using System.Reflection;

namespace clypse.portal.Components;

public partial class SecretDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public Secret? Secret { get; set; }
    [Parameter] public SecretDialogMode Mode { get; set; } = SecretDialogMode.Create;
    [Parameter] public EventCallback<Secret> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private Secret? EditableSecret { get; set; }
    private Dictionary<PropertyInfo, SecretFieldAttribute>? secretFields;
    private bool isSaving = false;

    public enum SecretDialogMode
    {
        Create,
        Edit,
        View
    }

    protected override void OnParametersSet()
    {
        if (Show && Secret != null)
        {
            // Create a working copy of the secret for editing
            EditableSecret = CreateWorkingCopy(Secret);
            
            // Get the ordered fields from the secret
            secretFields = EditableSecret?.GetOrderedSecretFields();
        }
        else if (!Show)
        {
            // Clear state when dialog is hidden
            EditableSecret = null;
            secretFields = null;
            isSaving = false;
        }
    }

    private Secret CreateWorkingCopy(Secret original)
    {
        // Create a working copy and cast it to the correct type based on its SecretType
        var workingCopy = (Secret)Activator.CreateInstance(original.GetType())!;
        workingCopy.SetAllData(original.Data);
        
        // Cast to the correct type based on the SecretType property
        return workingCopy.CastSecretToCorrectType();
    }

    private async Task HandleSave()
    {
        if (EditableSecret == null || Mode == SecretDialogMode.View)
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

    private string GetModeIcon()
    {
        // Use consistent secret/credential icon for all modes
        return "person-badge";
    }

    private string GetModeTitle()
    {
        return Mode switch
        {
            SecretDialogMode.Create => "Create Secret",
            SecretDialogMode.Edit => "Edit Secret",
            SecretDialogMode.View => "View Secret",
            _ => "Secret"
        };
    }

    private void OnSecretTypeChanged(ChangeEventArgs e)
    {
        if (EditableSecret == null || e.Value == null)
            return;

        // Parse the new secret type
        if (Enum.TryParse<SecretType>(e.Value.ToString(), out var newSecretType))
        {
            // Update the secret type
            EditableSecret.SecretType = newSecretType;
            
            // Cast to the correct type using the extension method
            EditableSecret = EditableSecret.CastSecretToCorrectType();
            
            // Rebuild the fields for the new secret type
            secretFields = EditableSecret?.GetOrderedSecretFields();
            
            // Trigger re-render
            StateHasChanged();
        }
    }

    private RenderFragment RenderField(PropertyInfo property, SecretFieldAttribute attribute)
    {
        return builder =>
        {
            var propertyValue = property.GetValue(EditableSecret)?.ToString();
            var isReadOnly = Mode == SecretDialogMode.View;

            switch (attribute.FieldType)
            {
                case SecretFieldType.SingleLineText:
                    builder.OpenComponent<SingleLineTextField>(0);
                    builder.AddAttribute(1, nameof(SingleLineTextField.Label), property.Name);
                    builder.AddAttribute(2, nameof(SingleLineTextField.Value), propertyValue);
                    builder.AddAttribute(3, nameof(SingleLineTextField.ValueChanged), 
                        EventCallback.Factory.Create<string?>(this, value => property.SetValue(EditableSecret, value)));
                    builder.AddAttribute(4, nameof(SingleLineTextField.IsReadOnly), isReadOnly);
                    builder.AddAttribute(5, nameof(SingleLineTextField.Placeholder), $"Enter {property.Name.ToLower()}");
                    builder.CloseComponent();
                    break;

                case SecretFieldType.MultiLineText:
                    builder.OpenComponent<MultiLineTextField>(0);
                    builder.AddAttribute(1, nameof(MultiLineTextField.Label), property.Name);
                    builder.AddAttribute(2, nameof(MultiLineTextField.Value), propertyValue);
                    builder.AddAttribute(3, nameof(MultiLineTextField.ValueChanged), 
                        EventCallback.Factory.Create<string?>(this, value => property.SetValue(EditableSecret, value)));
                    builder.AddAttribute(4, nameof(MultiLineTextField.IsReadOnly), isReadOnly);
                    builder.AddAttribute(5, nameof(MultiLineTextField.Placeholder), $"Enter {property.Name.ToLower()}");
                    builder.AddAttribute(6, nameof(MultiLineTextField.Rows), 4);
                    builder.CloseComponent();
                    break;

                case SecretFieldType.TagList:
                    // Handle tags specially using ITaggedObject interface
                    var tags = property.GetValue(EditableSecret) as List<string>;
                    builder.OpenComponent<TagListField>(0);
                    builder.AddAttribute(1, nameof(TagListField.Label), property.Name);
                    builder.AddAttribute(2, nameof(TagListField.Tags), tags);
                    builder.AddAttribute(3, nameof(TagListField.TagsChanged), 
                        EventCallback.Factory.Create<List<string>>(this, newTags => 
                        {
                            if (EditableSecret is ITaggedObject taggedObject)
                            {
                                taggedObject.UpdateTags(newTags);
                                StateHasChanged();
                            }
                        }));
                    builder.AddAttribute(4, nameof(TagListField.IsReadOnly), isReadOnly);
                    builder.CloseComponent();
                    break;

                case SecretFieldType.Password:
                    builder.OpenComponent<PasswordField>(0);
                    builder.AddAttribute(1, nameof(PasswordField.Label), property.Name);
                    builder.AddAttribute(2, nameof(PasswordField.Value), propertyValue);
                    builder.AddAttribute(3, nameof(PasswordField.ValueChanged), 
                        EventCallback.Factory.Create<string?>(this, value => property.SetValue(EditableSecret, value)));
                    builder.AddAttribute(4, nameof(PasswordField.IsReadOnly), isReadOnly);
                    builder.AddAttribute(5, nameof(PasswordField.ShowGeneratorButton), !isReadOnly);
                    builder.AddAttribute(6, nameof(PasswordField.ShowStrengthIndicator), !isReadOnly);
                    builder.CloseComponent();
                    break;

                default:
                    // Fallback to single line text for unknown field types
                    builder.OpenComponent<SingleLineTextField>(0);
                    builder.AddAttribute(1, nameof(SingleLineTextField.Label), property.Name);
                    builder.AddAttribute(2, nameof(SingleLineTextField.Value), propertyValue);
                    builder.AddAttribute(3, nameof(SingleLineTextField.ValueChanged), 
                        EventCallback.Factory.Create<string?>(this, value => property.SetValue(EditableSecret, value)));
                    builder.AddAttribute(4, nameof(SingleLineTextField.IsReadOnly), isReadOnly);
                    builder.CloseComponent();
                    break;
            }
        };
    }
}