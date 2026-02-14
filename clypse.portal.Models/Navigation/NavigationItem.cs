namespace clypse.portal.Models.Navigation;

public class NavigationItem
{
    public string Text { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi bi-circle";
    public string ButtonClass { get; set; } = "btn-primary";
}
