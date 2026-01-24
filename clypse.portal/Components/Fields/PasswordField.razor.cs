using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using clypse.core.Password;
using clypse.core.Enums;
using System.Timers;

namespace clypse.portal.Components.Fields;

public partial class PasswordField : ComponentBase, IDisposable
{
    [Parameter] public string Label { get; set; } = "Password";
    [Parameter] public string Placeholder { get; set; } = "Enter password";
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public bool ShowRevealButton { get; set; } = true;
    [Parameter] public bool ShowGeneratorButton { get; set; } = true;
    [Parameter] public bool ShowStrengthIndicator { get; set; } = true;

    [Inject] private IPasswordComplexityEstimatorService? PasswordComplexityEstimator { get; set; }

    private bool showPassword;
    private bool showPasswordGenerator;
    private PasswordComplexityEstimatorResults? passwordComplexityResults;
    private string? lastAnalyzedPassword;
    private System.Timers.Timer? passwordUpdateTimer;

    protected override void OnInitialized()
    {
        // Initialize password complexity analysis for existing values
        if (!string.IsNullOrEmpty(Value))
        {
            _ = UpdatePasswordComplexityAsync();
        }
    }

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }

    private void ShowPasswordGenerator()
    {
        if (!IsReadOnly)
        {
            showPasswordGenerator = true;
            StateHasChanged();
        }
    }

    private async Task HandlePasswordGenerated(string password)
    {
        Value = password;
        await ValueChanged.InvokeAsync(Value);
        // For generated passwords, update immediately since user didn't type it
        await UpdatePasswordComplexityAsync();
        showPasswordGenerator = false;
        StateHasChanged();
    }

    private void HandlePasswordGeneratorCancel()
    {
        showPasswordGenerator = false;
        StateHasChanged();
    }

    private async Task OnPasswordChanged()
    {
        await ValueChanged.InvokeAsync(Value);
        await UpdatePasswordComplexityAsync();
    }

    private void OnPasswordInput(ChangeEventArgs e)
    {
        Value = e.Value?.ToString();
        
        // Debounce password complexity estimation
        passwordUpdateTimer?.Stop();
        passwordUpdateTimer?.Dispose();
        
        passwordUpdateTimer = new System.Timers.Timer(500); // 500ms delay
        passwordUpdateTimer.Elapsed += async (sender, args) =>
        {
            passwordUpdateTimer?.Stop();
            await InvokeAsync(async () =>
            {
                await UpdatePasswordComplexityAsync();
                StateHasChanged();
            });
        };
        passwordUpdateTimer.Start();
    }

    private async Task UpdatePasswordComplexityAsync()
    {
        if (PasswordComplexityEstimator == null || !ShowStrengthIndicator)
        {
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(Value))
            {
                passwordComplexityResults = null;
                lastAnalyzedPassword = null;
                return;
            }

            // Only analyze if password has changed
            if (Value != lastAnalyzedPassword)
            {
                passwordComplexityResults = await PasswordComplexityEstimator.EstimateAsync(Value, true, CancellationToken.None);
                lastAnalyzedPassword = Value;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error estimating password complexity: {ex.Message}");
            passwordComplexityResults = null;
            lastAnalyzedPassword = null;
        }
    }

    private string GetComplexityColorClass()
    {
        if (passwordComplexityResults == null)
            return "text-dark";

        return passwordComplexityResults.ComplexityEstimation switch
        {
            PasswordComplexityEstimation.None => "text-dark",
            PasswordComplexityEstimation.Unknown => "text-dark",
            PasswordComplexityEstimation.VeryWeak => "text-danger",
            PasswordComplexityEstimation.Weak => "text-warning",
            PasswordComplexityEstimation.Medium => "text-warning",
            PasswordComplexityEstimation.Strong => "text-success",
            PasswordComplexityEstimation.VeryStrong => "text-primary",
            _ => "text-dark"
        };
    }

    private string GetComplexityBackgroundColorClass()
    {
        if (passwordComplexityResults == null)
            return "bg-dark";

        return passwordComplexityResults.ComplexityEstimation switch
        {
            PasswordComplexityEstimation.None => "bg-dark",
            PasswordComplexityEstimation.Unknown => "bg-dark",
            PasswordComplexityEstimation.VeryWeak => "bg-danger",
            PasswordComplexityEstimation.Weak => "bg-warning",
            PasswordComplexityEstimation.Medium => "bg-warning",
            PasswordComplexityEstimation.Strong => "bg-success",
            PasswordComplexityEstimation.VeryStrong => "bg-primary",
            _ => "bg-dark"
        };
    }

    private string GetComplexityText()
    {
        if (passwordComplexityResults == null)
            return "Unknown";

        return passwordComplexityResults.ComplexityEstimation switch
        {
            PasswordComplexityEstimation.None => "None",
            PasswordComplexityEstimation.Unknown => "Unknown",
            PasswordComplexityEstimation.VeryWeak => "Very Weak",
            PasswordComplexityEstimation.Weak => "Weak",
            PasswordComplexityEstimation.Medium => "Medium",
            PasswordComplexityEstimation.Strong => "Strong",
            PasswordComplexityEstimation.VeryStrong => "Very Strong",
            _ => "Unknown"
        };
    }

    private int GetComplexityPercentage()
    {
        if (passwordComplexityResults == null)
            return 0;

        return passwordComplexityResults.ComplexityEstimation switch
        {
            PasswordComplexityEstimation.None => 0,
            PasswordComplexityEstimation.Unknown => 0,
            PasswordComplexityEstimation.VeryWeak => 20,
            PasswordComplexityEstimation.Weak => 40,
            PasswordComplexityEstimation.Medium => 60,
            PasswordComplexityEstimation.Strong => 80,
            PasswordComplexityEstimation.VeryStrong => 100,
            _ => 0
        };
    }

    public void Dispose()
    {
        passwordUpdateTimer?.Stop();
        passwordUpdateTimer?.Dispose();
    }
}