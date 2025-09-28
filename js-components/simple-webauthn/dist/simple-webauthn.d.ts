import { CreateCredentialOptions, AuthenticateOptions, CreateCredentialResult, AuthenticateResult } from './types.js';
/**
 * SimpleWebAuthn - A reusable library for WebAuthn credential management
 * with optional PRF-based data encryption
 */
declare class SimpleWebAuthnClass {
    /**
     * Creates a new WebAuthn credential and optionally encrypts data
     * @param options - Credential creation options
     * @returns Promise<CreateCredentialResult>
     */
    static createCredential(options: CreateCredentialOptions): Promise<CreateCredentialResult>;
    /**
     * Authenticates with an existing WebAuthn credential and optionally decrypts data
     * @param options - Authentication options
     * @returns Promise<AuthenticateResult>
     */
    static authenticate(options: AuthenticateOptions): Promise<AuthenticateResult>;
    /**
     * Check if WebAuthn is supported on this platform
     */
    private static checkWebAuthnSupport;
    /**
     * Build comprehensive diagnostics information
     */
    private static buildDiagnostics;
    /**
     * Get basic diagnostics when detailed info isn't available
     */
    private static getBasicDiagnostics;
    /**
     * Convert CryptoKey to base64 string for transport
     */
    private static keyToBase64;
    /**
     * Determine authenticator type based on platform and PRF support (from working code)
     */
    private static getAuthenticatorType;
}
declare const SimpleWebAuthn: {
    createCredential: typeof SimpleWebAuthnClass.createCredential;
    authenticate: typeof SimpleWebAuthnClass.authenticate;
};
declare global {
    interface Window {
        SimpleWebAuthn: typeof SimpleWebAuthn;
    }
}
export default SimpleWebAuthn;
//# sourceMappingURL=simple-webauthn.d.ts.map