using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace clypse.portal.Components.Fields;

public partial class TagListField : ComponentBase
{
    [Parameter] public string Label { get; set; } = "Tags";
    [Parameter] public string Placeholder { get; set; } = "Add a tag and press Enter";
    [Parameter] public List<string>? Tags { get; set; }
    [Parameter] public EventCallback<List<string>> TagsChanged { get; set; }
    [Parameter] public bool IsReadOnly { get; set; } = false;

    private string newTag = string.Empty;

    private async Task HandleTagKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await AddTag();
        }
    }

    private async Task AddTag()
    {
        if (IsReadOnly || string.IsNullOrWhiteSpace(newTag))
            return;

        var trimmedTag = newTag.Trim();
        
        // Initialize Tags if null
        Tags ??= new List<string>();
        
        // Check if tag already exists (case insensitive)
        if (!Tags.Any(t => t.Equals(trimmedTag, StringComparison.OrdinalIgnoreCase)))
        {
            Tags.Add(trimmedTag);
            newTag = string.Empty;
            await TagsChanged.InvokeAsync(Tags);
            StateHasChanged();
        }
    }

    private async Task RemoveTag(string tag)
    {
        if (IsReadOnly || Tags == null)
            return;

        if (Tags.Remove(tag))
        {
            await TagsChanged.InvokeAsync(Tags);
            StateHasChanged();
        }
    }
}