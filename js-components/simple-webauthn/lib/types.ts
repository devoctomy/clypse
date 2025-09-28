// SimpleWebAuthn Library Type Definitions

export interface CreateCredentialOptions {
  // Standard WebAuthn format
  rp: {
    name: string;
    id?: string;
  };
  user: {
    id: string;        // Base64-encoded user ID
    name: string;      // Username
    displayName: string;
  };
  challenge: string;   // Base64-encoded challenge
  pubKeyCredParams: PublicKeyCredentialParameters[];
  
  // Optional WebAuthn Settings
  authenticatorSelection?: {
    authenticatorAttachment?: AuthenticatorAttachment;
    userVerification?: UserVerificationRequirement;
    residentKey?: ResidentKeyRequirement;
  };
  timeout?: number;
  attestation?: AttestationConveyancePreference;
  
  // Optional Encryption
  plaintextToEncrypt?: string;  // Data to encrypt during credential creation
  encryptionSalt: string;       // Required salt for PRF extension
}

export interface AuthenticateOptions {
  // Standard WebAuthn format
  challenge: string;   // Base64-encoded challenge
  allowCredentials: PublicKeyCredentialDescriptor[];
  
  // Optional WebAuthn Settings
  userVerification?: UserVerificationRequirement;
  timeout?: number;
  
  // Optional data to decrypt
  encryptedData?: string;   // Base64-encoded encrypted data to decrypt
  encryptionSalt: string;   // Required salt for PRF extension (must match creation)
}

// Standard WebAuthn types
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
  credentialId?: string;
  encryptedData?: string;  // Base64-encoded encrypted data (if plaintextToEncrypt provided)
  keyDerivationMethod?: KeyDerivationMethod;
  diagnostics?: DiagnosticsInfo;
}

export interface AuthenticateResult {
  success: boolean;
  error?: string;
  credentialId?: string;
  derivedKey?: string;
  decryptedData?: string;  // Plaintext result from decryption
  keyDerivationMethod?: KeyDerivationMethod;
  diagnostics?: DiagnosticsInfo;
}

// Enums and Type Aliases
export type KeyDerivationMethod = "PRF" | "CredentialID";
export type UserVerificationRequirement = "required" | "preferred" | "discouraged";
export type AuthenticatorAttachment = "platform" | "cross-platform";
export type ResidentKeyRequirement = "required" | "preferred" | "discouraged";
export type AttestationConveyancePreference = "none" | "indirect" | "direct" | "enterprise";
export type AuthenticatorTransport = "usb" | "nfc" | "ble" | "smart-card" | "hybrid" | "internal";

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