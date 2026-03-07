using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the tag list field component.
/// </summary>
public partial class TagListFieldViewModel : ViewModelBase
{
    private string newTag = string.Empty;
    private List<string> tags = [];

    /// <summary>Gets or sets the text currently typed in the new-tag input.</summary>
    public string NewTag { get => newTag; set => SetProperty(ref newTag, value); }

    /// <summary>Gets or sets the list of tags.</summary>
    public List<string> Tags { get => tags; set => SetProperty(ref tags, value); }

    /// <summary>Gets or sets whether the field is in read-only mode.</summary>
    public bool IsReadOnly { get; set; }

    /// <summary>Gets or sets the callback invoked when the tags collection changes.</summary>
    public Func<List<string>, Task>? TagsChangedCallback { get; set; }

    /// <summary>Adds the <see cref="NewTag"/> to the tags list.</summary>
    [RelayCommand]
    public async Task AddTagAsync()
    {
        if (IsReadOnly || string.IsNullOrWhiteSpace(NewTag))
        {
            return;
        }

        var trimmedTag = NewTag.Trim();
        if (!Tags.Any(t => t.Equals(trimmedTag, StringComparison.OrdinalIgnoreCase)))
        {
            Tags = [.. Tags, trimmedTag];
            NewTag = string.Empty;

            if (TagsChangedCallback != null)
            {
                await TagsChangedCallback(Tags);
            }
        }
    }

    /// <summary>Removes a tag from the tags list.</summary>
    /// <param name="tag">The tag string to remove.</param>
    [RelayCommand]
    public async Task RemoveTagAsync(string tag)
    {
        if (IsReadOnly)
        {
            return;
        }

        var updated = Tags.Where(t => t != tag).ToList();
        if (updated.Count != Tags.Count)
        {
            Tags = updated;

            if (TagsChangedCallback != null)
            {
                await TagsChangedCallback(Tags);
            }
        }
    }
}
