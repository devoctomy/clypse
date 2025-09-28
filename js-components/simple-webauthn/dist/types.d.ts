export interface CreateCredentialOptions {
    rpName: string;
    userName: string;
    userDisplayName: string;
    rpId?: string;
    plaintextToEncrypt?: string;
    encryptionSalt?: string;
    timeout?: number;
    userVerification?: UserVerificationRequirement;
    authenticatorAttachment?: AuthenticatorAttachment;
    residentKey?: ResidentKeyRequirement;
}
export interface AuthenticateOptions {
    credentialId: string;
    encryptedData?: string;
    encryptionSalt?: string;
    timeout?: number;
    userVerification?: UserVerificationRequirement;
}
export interface CredentialResult {
    id: string;
    rawId: ArrayBuffer;
    publicKey?: string;
    attestationObject?: string;
}
export interface EncryptionResult {
    encryptedData: string;
    keyDerivationMethod: KeyDerivationMethod;
}
export interface AuthenticationResult {
    credentialId: string;
    signature: string;
    authenticatorData: string;
    keyDerivationMethod: KeyDerivationMethod;
}
export interface DecryptionResult {
    plaintext: string;
    keyDerivationMethod: KeyDerivationMethod;
}
export interface DiagnosticsInfo {
    userAgent: string;
    platform: string;
    prfSupported: boolean;
    prfResultsAvailable: boolean;
    authenticatorType: string;
    keyDerivationMethod: string;
    credentialIdLength: number;
}
export interface CreateCredentialResult {
    success: boolean;
    error?: string;
    credential?: CredentialResult;
    encryption?: EncryptionResult;
    diagnostics: DiagnosticsInfo;
}
export interface AuthenticateResult {
    success: boolean;
    error?: string;
    authentication?: AuthenticationResult;
    decryption?: DecryptionResult;
    diagnostics: DiagnosticsInfo;
}
export type KeyDerivationMethod = "PRF" | "CredentialID";
export type UserVerificationRequirement = "required" | "preferred" | "discouraged";
export type AuthenticatorAttachment = "platform" | "cross-platform";
export type ResidentKeyRequirement = "required" | "preferred" | "discouraged";
export interface PlatformConfig {
    isSamsung: boolean;
    isIOS: boolean;
    isWindows: boolean;
    timeout: number;
    residentKey: ResidentKeyRequirement;
}
export interface PRFResult {
    enabled?: boolean;
    results?: {
        first?: ArrayBuffer;
    };
}
//# sourceMappingURL=types.d.ts.map