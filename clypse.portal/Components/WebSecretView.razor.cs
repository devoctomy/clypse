using Microsoft.AspNetCore.Components;
using clypse.core.Secrets;

namespace clypse.portal.Components;

public partial class WebSecretView : ComponentBase
{
    [Parameter] public WebSecret? Secret { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    
    private bool showPassword = false;

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }
}
