using clypse.core.Enums;

namespace clypse.core.Cryptography;

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
        options.Parameters[KeyDerivationParameterKeys.Algorithm] = KeyDerivationAlgorithm.Rfc2898.ToString();
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
#if DEBUG
        options.Parameters[KeyDerivationParameterKeys.Algorithm] = KeyDerivationAlgorithm.Argon2id.ToString();
        options.Parameters[KeyDerivationParameterKeys.Argon2id_KeyLength] = 32;
        options.Parameters[KeyDerivationParameterKeys.Argon2id_Parallelism] = 1;
        options.Parameters[KeyDerivationParameterKeys.Argon2id_MemorySizeKb] = 65536;
        options.Parameters[KeyDerivationParameterKeys.Argon2id_Iterations] = 1;
#else
        options.Parameters[KeyDerivationParameterKeys.Algorithm] = KeyDerivationAlgorithm.Argon2id.ToString();
        options.Parameters[KeyDerivationParameterKeys.Argon2id_KeyLength] = 32;
        options.Parameters[KeyDerivationParameterKeys.Argon2id_Parallelism] = 1; // Bitwarden uses 4
        options.Parameters[KeyDerivationParameterKeys.Argon2id_MemorySizeKb] = 262144; // Bitwarden uses 64mb
        options.Parameters[KeyDerivationParameterKeys.Argon2id_Iterations] = 1; // Bitwarden uses 3
#endif
        return options;
    }

    /// <summary>
    /// Gets test options for Argon2id key derivation algorithm, suitable for testing Blazor applications. DO NOT USE THIS IN PRODUCTION.
    /// </summary>
    /// <returns>KeyDerivationServiceOptions for Argon2id suitable for Blazor testing.</returns>
    public static KeyDerivationServiceOptions Blazor_Argon2id_Test()
    {
        var options = new KeyDerivationServiceOptions();
        options.Parameters[KeyDerivationParameterKeys.Algorithm] = KeyDerivationAlgorithm.Argon2id.ToString();
        options.Parameters[KeyDerivationParameterKeys.Argon2id_KeyLength] = 32;
        options.Parameters[KeyDerivationParameterKeys.Argon2id_Parallelism] = 1;
        options.Parameters[KeyDerivationParameterKeys.Argon2id_MemorySizeKb] = 1024;
        options.Parameters[KeyDerivationParameterKeys.Argon2id_Iterations] = 1;
        return options;
    }
}
