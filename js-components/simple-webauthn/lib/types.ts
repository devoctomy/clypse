// SimpleWebAuthn Library Type Definitions

export interface CreateCredentialOptions {
  // Required
  rpName: string;
  userName: string;
  userDisplayName: string;
  
  // Optional - Advanced Use Cases
  rpId?: string;
  
  // Optional Encryption
  plaintextToEncrypt?: string;
  encryptionSalt?: string;
  
  // Optional WebAuthn Settings
  timeout?: number;
  userVerification?: UserVerificationRequirement;
  authenticatorAttachment?: AuthenticatorAttachment;
  residentKey?: ResidentKeyRequirement;
}

export interface AuthenticateOptions {
  // Required
  credentialId: string;
  
  // Optional Decryption
  encryptedData?: string;
  encryptionSalt?: string;
  
  // Optional WebAuthn Settings
  timeout?: number;
  userVerification?: UserVerificationRequirement;
}

export interface CredentialResult {
  id: string;                    // Base64-encoded credential ID
  rawId: ArrayBuffer;           // Raw credential ID bytes
  publicKey?: string;           // Base64-encoded public key (if needed)
  attestationObject?: string;   // Base64-encoded attestation (if needed)
}

export interface EncryptionResult {
  encryptedData: string;        // Base64-encoded encrypted data
  keyDerivationMethod: KeyDerivationMethod;
}

export interface AuthenticationResult {
  credentialId: string;           // Credential ID that was used
  signature: string;              // Base64-encoded assertion signature
  authenticatorData: string;      // Base64-encoded authenticator data
  keyDerivationMethod: KeyDerivationMethod;
}

export interface DecryptionResult {
  plaintext: string;              // Decrypted data
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

// Enums and Type Aliases
export type KeyDerivationMethod = "PRF" | "CredentialID";
export type UserVerificationRequirement = "required" | "preferred" | "discouraged";
export type AuthenticatorAttachment = "platform" | "cross-platform";
export type ResidentKeyRequirement = "required" | "preferred" | "discouraged";

// Internal Types
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