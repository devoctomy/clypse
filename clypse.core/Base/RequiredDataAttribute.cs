namespace clypse.core.Base;

/// <summary>
/// Attribute used to mark properties as required data that must be present for object validation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RequiredDataAttribute : Attribute
{
}
