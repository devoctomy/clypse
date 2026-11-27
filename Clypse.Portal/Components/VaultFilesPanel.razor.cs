using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for the Files tab of a vault's page. Logic lives in <see cref="VaultFilesViewModel"/>; this only hands
/// over the files the user picks, each with a way to read its content.
/// </summary>
public partial class VaultFilesPanel : MvvmComponentBase<VaultFilesViewModel>
{
    /// <summary>
    /// The name of the section, in the page's header, that the tab's buttons are shown in.
    /// </summary>
    public const string HeaderSection = "vault-files-header";

    private int inputKey;

    private static async Task<byte[]> ReadAsync(IBrowserFile file, CancellationToken ct)
    {
        // The view model has already refused a file over the limit, so the file's own size is what may be read.
        var content = new byte[file.Size];
        await using var stream = file.OpenReadStream(file.Size, ct);
        await stream.ReadExactlyAsync(content, ct);
        return content;
    }

    private async Task AddFilesAsync(InputFileChangeEventArgs e)
    {
        var picked = e.FileCount == 0 ? [] : e.GetMultipleFiles(e.FileCount);
        var files = picked
            .Select(f => new FileToAdd
            {
                Name = f.Name,
                ContentType = f.ContentType,
                Size = f.Size,
                ReadAsync = ct => ReadAsync(f, ct),
            })
            .ToList();
        await ViewModel.AddFilesAsync(files);
        inputKey++;
    }
}
