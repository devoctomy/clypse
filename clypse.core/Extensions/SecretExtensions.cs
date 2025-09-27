using System.Reflection;
using clypse.core.Secrets;

namespace clypse.core.Extensions;

/// <summary>
/// Extension methods for working with secrets.
/// </summary>
public static class SecretExtensions
{
    /// <summary>
    /// Gets the properties of a secret type that are marked with the SecretFieldAttribute.
    /// </summary>
    /// <param name="secret">The secret instance.</param>
    /// <returns>A dictionary of SecretFieldAttribute and corresponding PropertyInfo, ordered by DisplayOrder.</returns>
    public static Dictionary<PropertyInfo, SecretFieldAttribute>? GetOrderedSecretFields(this Secret secret)
    {
        var secretType = secret.GetType();
        var secretFields = new Dictionary<PropertyInfo, SecretFieldAttribute>();
        foreach (var prop in secretType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attribute = prop.GetCustomAttribute<SecretFieldAttribute>();
            if (attribute != null)
            {
                secretFields.Add(prop, attribute);
            }
        }

        var orderedFields = secretFields
            .OrderBy(kv => kv.Value.DisplayOrder < 0 ? int.MaxValue : kv.Value.DisplayOrder)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return orderedFields;
    }

    /// <summary>
    /// Casts a Secret instance to its correct derived type based on the SecretType property.
    /// </summary>
    /// <param name="secret">The secret instance to cast.</param>
    /// <returns>The secret instance cast to its correct derived type.</returns>
    public static Secret CastSecretToCorrectType(this Secret secret)
    {
        var castSecret = secret;

        switch (secret.SecretType)
        {
            case Enums.SecretType.None:
                {
                    // Do nothing;
                    break;
                }

            case Enums.SecretType.Web:
                {
                    castSecret = WebSecret.FromSecret(secret);
                    break;
                }

            case Enums.SecretType.Aws:
                {
                    castSecret = AwsCredentials.FromSecret(secret);
                    break;
                }
        }

        return castSecret;
    }
}
