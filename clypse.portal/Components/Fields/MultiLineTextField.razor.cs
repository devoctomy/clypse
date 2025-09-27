using Microsoft.AspNetCore.Components;

namespace clypse.portal.Components.Fields;

public partial class MultiLineTextField : ComponentBase
{
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public string Placeholder { get; set; } = string.Empty;
    [Parameter] public bool IsReadOnly { get; set; } = false;
    [Parameter] public int Rows { get; set; } = 3;
}