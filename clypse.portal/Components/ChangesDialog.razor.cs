using clypse.portal.Models.Changes;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace clypse.portal.Components;

public partial class ChangesDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public bool ShowUpdateButton { get; set; }
    [Parameter] public string? AvailableVersion { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnUpdate { get; set; }
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private ILogger<ChangesDialog> Logger { get; set; } = default!;

    private ChangeLog? changeLog;
    private bool isLoading;
    private bool isUpdating;
    private string? errorMessage;

    protected override async Task OnParametersSetAsync()
    {
        if (Show && changeLog == null)
        {
            await LoadChangelog();
        }
    }

    private async Task LoadChangelog()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var json = await HttpClient.GetStringAsync("changes.json");
            changeLog = JsonSerializer.Deserialize<ChangeLog>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading changelog");
            errorMessage = "Failed to load version history. Please try again later.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private async Task HandleBackdropClick()
    {
        await HandleClose();
    }

    private async Task HandleUpdateClick()
    {
        isUpdating = true;
        StateHasChanged();

        try
        {
            await OnUpdate.InvokeAsync();
        }
        finally
        {
            isUpdating = false;
            StateHasChanged();
        }
    }
}
