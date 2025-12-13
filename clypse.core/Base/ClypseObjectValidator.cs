using System.Reflection;
using clypse.core.Base.Exceptions;

namespace clypse.core.Base;

/// <summary>
/// Provides validation functionality for Clypse objects by checking required data properties.
/// </summary>
/// <param name="clypseObject">The Clypse object to validate.</param>
public class ClypseObjectValidator(ClypseObject clypseObject)
{
    /// <summary>
    /// Validates the Clypse object by checking that all properties marked with RequiredDataAttribute have non-null values.
    /// </summary>
    /// <exception cref="ClypseObjectValidatorException">Thrown when one or more required properties are missing or null.</exception>
    public void Validate()
    {
        var requiredProperties = clypseObject.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.GetCustomAttribute<RequiredDataAttribute>() != null).ToList();

        var missingProperties = new List<string>();

        foreach (var property in requiredProperties)
        {
            var value = property.GetValue(clypseObject);
            if (value == null)
            {
                missingProperties.Add(property.Name);
            }
        }

        if (missingProperties.Count > 0)
        {
            throw new ClypseObjectValidatorException(
                $"Required properties are missing or null: {string.Join(", ", missingProperties)}",
                missingProperties);
        }
    }
}
