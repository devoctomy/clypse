using Microsoft.AspNetCore.Components;

namespace clypse.portal.Components;

public partial class LoadingDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public string Message { get; set; } = "Loading...";
}
