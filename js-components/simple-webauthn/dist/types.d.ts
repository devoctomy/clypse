export interface CreateCredentialOptions {
    rp: {
        name: string;
        id?: string;
    };
    user: {
        id: string;
        name: string;
        displayName: string;
    };
    challenge: string;
    pubKeyCredParams: PublicKeyCredentialParameters[];
    authenticatorSelection?: {
        authenticatorAttachment?: AuthenticatorAttachment;
        userVerification?: UserVerificationRequirement;
        residentKey?: ResidentKeyRequirement;
    };
    timeout?: number;
    attestation?: AttestationConveyancePreference;
    userData?: string;
    encryptionSalt: string;
}
export interface AuthenticateOptions {
    challenge: string;
    allowCredentials: PublicKeyCredentialDescriptor[];
    userVerification?: UserVerificationRequirement;
    timeout?: number;
    userData?: string;
    encryptionSalt: string;
}
export interface PublicKeyCredentialParameters {
    alg: number;
    type: "public-key";
}
export interface PublicKeyCredentialDescriptor {
    id: string;
    type: "public-key";
    transports?: AuthenticatorTransport[];
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
    credentialId?: string;
    keyDerivationMethod?: KeyDerivationMethod;
    diagnostics?: DiagnosticsInfo;
}
export interface AuthenticateResult {
    success: boolean;
    error?: string;
    credentialId?: string;
    derivedKey?: string;
    encryptedUserData?: string;
    keyDerivationMethod?: KeyDerivationMethod;
    diagnostics?: DiagnosticsInfo;
}
export type KeyDerivationMethod = "PRF" | "CredentialID";
export type UserVerificationRequirement = "required" | "preferred" | "discouraged";
export type AuthenticatorAttachment = "platform" | "cross-platform";
export type ResidentKeyRequirement = "required" | "preferred" | "discouraged";
export type AttestationConveyancePreference = "none" | "indirect" | "direct" | "enterprise";
export type AuthenticatorTransport = "usb" | "nfc" | "ble" | "smart-card" | "hybrid" | "internal";
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