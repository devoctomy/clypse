namespace clypse.portal.UITests;

/// <summary>
/// Represents a device profile for UI testing with specific viewport and display characteristics.
/// </summary>
public class DeviceProfile
{
    /// <summary>
    /// Gets or sets the name of the device profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CSS viewport width in logical pixels.
    /// </summary>
    public int ViewportWidth { get; set; }

    /// <summary>
    /// Gets or sets the CSS viewport height in logical pixels.
    /// </summary>
    public int ViewportHeight { get; set; }

    /// <summary>
    /// Gets or sets the device pixel ratio (scale factor).
    /// </summary>
    public float DeviceScaleFactor { get; set; }

    /// <summary>
    /// Gets or sets optional description of the profile.
    /// </summary>
    public string? Description { get; set; }
}
