using Microsoft.AspNetCore.Components;
using clypse.portal.Models;

namespace clypse.portal.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AppSettings AppSettings { get; set; } = default!;
    
    private string GetCurrentPageName()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var path = uri.AbsolutePath;
        
        return path switch
        {
            "/" => "Login Page",
            "/test" => "Test Page",
            _ => "Unknown Page"
        };
    }
}
