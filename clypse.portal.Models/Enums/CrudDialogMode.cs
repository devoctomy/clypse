namespace clypse.portal.Models.Enums;

/// <summary>
/// Specifies the mode of a CRUD dialog.
/// </summary>
public enum CrudDialogMode
{
    /// <summary>
    /// Dialog is in create mode for adding new items.
    /// </summary>
    Create,

    /// <summary>
    /// Dialog is in update mode for editing existing items.
    /// </summary>
    Update,

    /// <summary>
    /// Dialog is in delete mode for removing items.
    /// </summary>
    Delete,

    /// <summary>
    /// Dialog is in view mode for displaying items without editing.
    /// </summary>
    View,
}
