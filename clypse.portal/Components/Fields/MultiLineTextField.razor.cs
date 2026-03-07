using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components.Fields;

/// <summary>
/// Code-behind for the multi-line text field component. Logic is in <see cref="MultiLineTextFieldViewModel"/>.
/// </summary>
public partial class MultiLineTextField : MvvmComponentBase<MultiLineTextFieldViewModel>
{
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public string Placeholder { get; set; } = string.Empty;
    [Parameter] public bool IsReadOnly { get; set; } = false;
    [Parameter] public int Rows { get; set; } = 3;

    protected override void OnParametersSet()
    {
        ViewModel.Value = Value;
        ViewModel.ValueChangedCallback = v => ValueChanged.InvokeAsync(v);
    }
}
