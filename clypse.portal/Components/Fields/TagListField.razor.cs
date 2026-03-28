using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using Microsoft.AspNetCore.Components.Web;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components.Fields;

/// <summary>
/// Code-behind for the tag list field component. Business logic is in <see cref="TagListFieldViewModel"/>.
/// </summary>
public partial class TagListField : MvvmComponentBase<TagListFieldViewModel>
{
    [Parameter] public string Label { get; set; } = "Tags";
    [Parameter] public string Placeholder { get; set; } = "Add a tag and press Enter";
    [Parameter] public List<string>? Tags { get; set; }
    [Parameter] public EventCallback<List<string>> TagsChanged { get; set; }
    [Parameter] public bool IsReadOnly { get; set; } = false;

    protected override void OnParametersSet()
    {
        ViewModel.Tags = Tags ?? [];
        ViewModel.IsReadOnly = IsReadOnly;
        ViewModel.TagsChangedCallback = tags => TagsChanged.InvokeAsync(tags);
    }

    private async Task HandleTagKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ViewModel.AddTagCommand.ExecuteAsync(null);
        }
    }
}
