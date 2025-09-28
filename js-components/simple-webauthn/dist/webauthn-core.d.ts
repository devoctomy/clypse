import { CreateCredentialOptions, AuthenticateOptions, PlatformConfig, PRFResult, CredentialResult, KeyDerivationMethod } from './types.js';
/**
 * Core WebAuthn functionality with PRF support
 */
export declare class WebAuthnCore {
    /**
     * Create a new WebAuthn credential with PRF extension
     */
    static createCredential(options: CreateCredentialOptions, platformConfig: PlatformConfig): Promise<{
        success: boolean;
        error?: string;
        credential?: CredentialResult;
        keyMaterial?: CryptoKey;
        keyDerivationMethod?: KeyDerivationMethod;
        prfResult?: PRFResult;
    }>;
    /**
     * Authenticate with an existing WebAuthn credential
     */
    static authenticate(options: AuthenticateOptions, platformConfig: PlatformConfig): Promise<{
        success: boolean;
        error?: string;
        credentialId?: string;
        signature?: string;
        authenticatorData?: string;
        keyMaterial?: CryptoKey;
        keyDerivationMethod?: KeyDerivationMethod;
        prfResult?: PRFResult;
    }>;
    /**
     * Attempt to get PRF results via get() operation
     * This is needed for some authenticators that don't return PRF results during creation
     */
    private static attemptPRFGet;
}
//# sourceMappingURL=webauthn-core.d.ts.map