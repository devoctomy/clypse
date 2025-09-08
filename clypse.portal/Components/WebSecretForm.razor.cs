using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using clypse.core.Secrets;
using clypse.core.Password;
using clypse.core.Enums;
using System.Timers;

namespace clypse.portal.Components;

public partial class WebSecretForm : ComponentBase, IDisposable
{
    [Parameter] public WebSecret? Secret { get; set; }
    [Parameter] public bool IsEditMode { get; set; } = true;
    [Parameter] public EventCallback<WebSecret> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Inject] private IPasswordComplexityEstimatorService? PasswordComplexityEstimator { get; set; }

    private WebSecret? EditableSecret { get; set; }
    private bool showPassword = false;
    private bool isSaving = false;
    private string newTag = string.Empty;
    private bool showPasswordGenerator = false;
    private PasswordComplexityEstimatorResults? passwordComplexityResults;
    private string? lastAnalyzedPassword;
    private System.Timers.Timer? passwordUpdateTimer;

    protected override async Task OnParametersSetAsync()
    {
        if (IsEditMode && Secret != null)
        {
            // Edit mode: Create a copy of the secret for editing to avoid modifying the original
            EditableSecret = WebSecret.FromSecret(Secret);
        }
        else if (!IsEditMode)
        {
            // Create mode: Create a new empty secret
            EditableSecret = new WebSecret();
        }
        
        // Analyze password complexity after setting up the editable secret
        await UpdatePasswordComplexity();
    }

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }

    private async Task HandleSave()
    {
        if (EditableSecret == null)
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

    private void HandleTagKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            AddTag();
        }
    }

    private void AddTag()
    {
        if (EditableSecret == null || string.IsNullOrWhiteSpace(newTag))
            return;

        var trimmedTag = newTag.Trim();
        if (EditableSecret.AddTag(trimmedTag))
        {
            newTag = string.Empty;
            StateHasChanged();
        }
    }

    private void RemoveTag(string tag)
    {
        if (EditableSecret == null)
            return;

        if (EditableSecret.RemoveTag(tag))
        {
            StateHasChanged();
        }
    }

    private void ShowPasswordGenerator()
    {
        showPasswordGenerator = true;
        StateHasChanged();
    }

    private async Task HandlePasswordGenerated(string password)
    {
        if (EditableSecret != null)
        {
            EditableSecret.Password = password;
            // For generated passwords, update immediately since user didn't type it
            await UpdatePasswordComplexity();
        }
        showPasswordGenerator = false;
        StateHasChanged();
    }

    private void HandlePasswordGeneratorCancel()
    {
        showPasswordGenerator = false;
        StateHasChanged();
    }

    private async Task UpdatePasswordComplexity()
    {
        Console.WriteLine($"UpdatePasswordComplexity called for password: '{EditableSecret?.Password}'");
        
        if (PasswordComplexityEstimator == null || EditableSecret == null)
        {
            passwordComplexityResults = null;
            lastAnalyzedPassword = null;
            return;
        }

        var currentPassword = EditableSecret.Password ?? string.Empty;
        
        // Only recalculate if password has changed
        if (currentPassword != lastAnalyzedPassword)
        {
            try
            {
                passwordComplexityResults = await PasswordComplexityEstimator.EstimateAsync(currentPassword, CancellationToken.None);
                lastAnalyzedPassword = currentPassword;
                Console.WriteLine($"Password complexity updated: {passwordComplexityResults.ComplexityEstimation}");
                StateHasChanged();
            }
            catch (Exception ex)
            {
                // In case of any error, clear the results
                Console.WriteLine($"Error estimating password complexity: {ex.Message}");
                passwordComplexityResults = null;
                lastAnalyzedPassword = null;
            }
        }
    }

    private void OnPasswordChanged()
    {
        Console.WriteLine("OnPasswordChanged called");
        
        // Initialize timer if not already created
        if (passwordUpdateTimer == null)
        {
            Console.WriteLine("Creating new timer");
            passwordUpdateTimer = new System.Timers.Timer(2000); // 2 seconds
            passwordUpdateTimer.AutoReset = false; // Only fire once
            passwordUpdateTimer.Elapsed += async (sender, e) =>
            {
                Console.WriteLine("Timer elapsed, calling UpdatePasswordComplexity");
                await InvokeAsync(async () =>
                {
                    await UpdatePasswordComplexity();
                });
            };
        }
        
        // Stop the timer if it's running and restart it
        Console.WriteLine("Stopping and starting timer");
        passwordUpdateTimer.Stop();
        passwordUpdateTimer.Start();
    }

    private void OnPasswordInput(ChangeEventArgs e)
    {
        if (EditableSecret != null)
        {
            EditableSecret.Password = e.Value?.ToString() ?? string.Empty;
            OnPasswordChanged();
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
            PasswordComplexityEstimation.VeryWeak => 16,
            PasswordComplexityEstimation.Weak => 33,
            PasswordComplexityEstimation.Medium => 50,
            PasswordComplexityEstimation.Strong => 83,
            PasswordComplexityEstimation.VeryStrong => 100,
            _ => 0
        };
    }

    public void Dispose()
    {
        passwordUpdateTimer?.Stop();
        passwordUpdateTimer?.Dispose();
        passwordUpdateTimer = null;
    }
}
