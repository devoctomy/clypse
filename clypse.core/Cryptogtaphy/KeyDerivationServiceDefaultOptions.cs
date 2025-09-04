namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Provides default configuration options for the KeyDerivationService.
/// </summary>
public class KeyDerivationServiceDefaultOptions
{
    /// <summary>
    /// Gets default options for the RFC2898 key derivation algorithm, suitable for Blazor applications.
    /// </summary>
    /// <returns>KeyDerivationServiceOptions for RFC2898 suitable for Blazor.</returns>
    public static KeyDerivationServiceOptions Blazor_Rfc2898()
    {
        var options = new KeyDerivationServiceOptions();
        options.Parameters[KeyDerivationParameterKeys.Rfc2898_KeyLength] = 32;
        options.Parameters[KeyDerivationParameterKeys.Rfc2898_Iterations] = 100000;
        return options;
    }

    /// <summary>
    /// Gets default options for Argon2id key derivation algorithm, suitable for Blazor applications.
    /// </summary>
    /// <returns>KeyDerivationServiceOptions for Argon2id suitable for Blazor.</returns>
    public static KeyDerivationServiceOptions Blazor_Argon2id()
    {
        var options = new KeyDerivationServiceOptions();
        options.Parameters[KeyDerivationParameterKeys.Argon2_KeyLength] = 32;
        options.Parameters[KeyDerivationParameterKeys.Argon2_Parallelism] = 2;
        options.Parameters[KeyDerivationParameterKeys.Argon2_MemorySizeKb] = 65536;
        options.Parameters[KeyDerivationParameterKeys.Argon2_Iterations] = 1;
        return options;
    }

}
