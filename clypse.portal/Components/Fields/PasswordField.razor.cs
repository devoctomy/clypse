using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using Microsoft.AspNetCore.Components.Web;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components.Fields;

/// <summary>
/// Code-behind for the password field component. Business logic is in <see cref="PasswordFieldViewModel"/>.
/// </summary>
public partial class PasswordField : MvvmComponentBase<PasswordFieldViewModel>, IDisposable
{
    [Parameter] public string Label { get; set; } = "Password";
    [Parameter] public string Placeholder { get; set; } = "Enter password";
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public bool ShowRevealButton { get; set; } = true;
    [Parameter] public bool ShowGeneratorButton { get; set; } = true;
    [Parameter] public bool ShowStrengthIndicator { get; set; } = true;

    protected override void OnParametersSet()
    {
        ViewModel.Value = Value;
        ViewModel.ValueChangedCallback = v => ValueChanged.InvokeAsync(v);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (!string.IsNullOrEmpty(Value))
        {
            _ = ViewModel.OnPasswordChangedAsync();
        }
    }

    private void OnPasswordInput(ChangeEventArgs e)
    {
        ViewModel.OnPasswordInput(e.Value?.ToString());
    }

    private async Task OnPasswordChanged()
    {
        await ViewModel.OnPasswordChangedAsync();
    }

    private async Task HandlePasswordGenerated(string password)
    {
        Value = password;
        await ViewModel.HandlePasswordGeneratedAsync(password);
        await ValueChanged.InvokeAsync(Value);
    }

    public new void Dispose()
    {
        base.Dispose();
        ViewModel?.Dispose();
    }
}
