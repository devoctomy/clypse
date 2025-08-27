using System.Reflection;
using clypse.core.Base.Exceptions;

namespace clypse.core.Base;

public class ClypseObjectValidator(ClypseObject clypseObject)
{
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
