using Microsoft.AspNetCore.Components;
using clypse.core.Vault;

namespace clypse.portal.Components;

public partial class VerifyDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public VaultVerifyResults? Results { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
