using Blazing.Mvvm.ComponentModel;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for text field components (single-line and multi-line).
/// </summary>
public partial class TextFieldViewModel : ViewModelBase
{
    private string? value;

    /// <summary>Gets or sets the text value.</summary>
    public string? Value { get => value; set => SetProperty(ref this.value, value); }

    /// <summary>Gets or sets the callback invoked when the value changes.</summary>
    public Func<string?, Task>? ValueChangedCallback { get; set; }

    /// <summary>
    /// Called when the field value changes; propagates to the callback.
    /// </summary>
    /// <param name="newValue">The new value entered by the user.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task OnValueChangedAsync(string? newValue)
    {
        Value = newValue;
        if (ValueChangedCallback != null)
        {
            await ValueChangedCallback(newValue);
        }
    }
}
