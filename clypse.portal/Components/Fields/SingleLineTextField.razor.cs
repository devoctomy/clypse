using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components.Fields;

/// <summary>
/// Code-behind for the single-line text field component. Logic is in <see cref="TextFieldViewModel"/>.
/// </summary>
public partial class SingleLineTextField : MvvmComponentBase<TextFieldViewModel>
{
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public string Placeholder { get; set; } = string.Empty;
    [Parameter] public bool IsReadOnly { get; set; } = false;

    protected override void OnParametersSet()
    {
        ViewModel.Value = Value;
        ViewModel.ValueChangedCallback = v => ValueChanged.InvokeAsync(v);
    }
}
