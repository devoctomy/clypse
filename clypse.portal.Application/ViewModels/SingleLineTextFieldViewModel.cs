using Blazing.Mvvm.ComponentModel;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the single-line text field component.
/// </summary>
public partial class SingleLineTextFieldViewModel : ViewModelBase
{
    private string? value;

    /// <summary>Gets or sets the text value.</summary>
    public string? Value { get => value; set => SetProperty(ref this.value, value); }

    /// <summary>Gets or sets the callback invoked when the value changes.</summary>
    public Func<string?, Task>? ValueChangedCallback { get; set; }

    /// <summary>
    /// Called when the input value changes; propagates to the callback.
    /// </summary>
    public async Task OnValueChangedAsync(string? newValue)
    {
        Value = newValue;
        if (ValueChangedCallback != null)
        {
            await ValueChangedCallback(newValue);
        }
    }
}
