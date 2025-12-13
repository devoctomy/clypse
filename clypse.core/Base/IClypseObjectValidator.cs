namespace clypse.core.Base;

/// <summary>
/// Defines the contract for validating Clypse objects to ensure they meet required data constraints.
/// </summary>
public interface IClypseObjectValidator
{
    /// <summary>
    /// Validates the object to ensure all required properties are present and valid.
    /// </summary>
    /// <exception cref="Exceptions.ClypseObjectValidatorException">Thrown when validation fails due to missing or invalid required properties.</exception>
    void Validate();
}
