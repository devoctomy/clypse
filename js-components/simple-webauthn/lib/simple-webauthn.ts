import { 
  CreateCredentialOptions, 
  AuthenticateOptions, 
  CreateCredentialResult, 
  AuthenticateResult,
  PlatformConfig,
  KeyDerivationMethod,
  PRFResult
} from './types.js';

import { PlatformDetector } from './platform-detector.js';
import { WebAuthnCore } from './webauthn-core.js';
import { EncryptionUtils } from './encryption-utils.js';
import { InputValidator } from './input-validator.js';

/**
 * SimpleWebAuthn - A reusable library for WebAuthn credential management 
 * with optional PRF-based data encryption
 */
export class SimpleWebAuthn {
  
  /**
   * Creates a new WebAuthn credential and optionally encrypts data
   * @param options - Credential creation options
   * @returns Promise<CreateCredentialResult>
   */
  static async createCredential(options: CreateCredentialOptions): Promise<CreateCredentialResult> {
    try {
      // Validate inputs
      const validationResult = InputValidator.validateCreateOptions(options);
      if (!validationResult.valid) {
        return {
          success: false,
          error: validationResult.error!,
          diagnostics: this.getBasicDiagnostics()
        };
      }

      // Check WebAuthn support
      const supportCheck = await this.checkWebAuthnSupport();
      if (!supportCheck.supported) {
        return {
          success: false,
          error: supportCheck.error!,
          diagnostics: this.getBasicDiagnostics()
        };
      }

      // Detect platform configuration
      const platformConfig = PlatformDetector.detectPlatform();

      // Create credential using WebAuthn
      const webAuthnResult = await WebAuthnCore.createCredential(options, platformConfig);
      if (!webAuthnResult.success) {
        return {
          success: false,
          error: webAuthnResult.error!,
          diagnostics: this.buildDiagnostics(platformConfig, null, webAuthnResult.keyDerivationMethod || "unknown")
        };
      }

      const result: CreateCredentialResult = {
        success: true,
        credential: webAuthnResult.credential!,
        diagnostics: this.buildDiagnostics(platformConfig, webAuthnResult.prfResult || null, webAuthnResult.keyDerivationMethod!)
      };

      // Handle optional encryption
      if (options.plaintextToEncrypt) {
        const encryptionResult = await EncryptionUtils.encryptData(
          options.plaintextToEncrypt,
          webAuthnResult.keyMaterial!,
          options.encryptionSalt || "webauthn-prf-salt-v1"
        );

        if (encryptionResult.success) {
          result.encryption = {
            encryptedData: encryptionResult.encryptedData!,
            keyDerivationMethod: webAuthnResult.keyDerivationMethod!
          };
        } else {
          return {
            success: false,
            error: `Encryption failed: ${encryptionResult.error}`,
            diagnostics: result.diagnostics
          };
        }
      }

      return result;

    } catch (error) {
      return {
        success: false,
        error: `Unexpected error: ${error instanceof Error ? error.message : 'Unknown error'}`,
        diagnostics: this.getBasicDiagnostics()
      };
    }
  }

  /**
   * Authenticates with an existing WebAuthn credential and optionally decrypts data
   * @param options - Authentication options
   * @returns Promise<AuthenticateResult>
   */
  static async authenticate(options: AuthenticateOptions): Promise<AuthenticateResult> {
    try {
      // Validate inputs
      const validationResult = InputValidator.validateAuthOptions(options);
      if (!validationResult.valid) {
        return {
          success: false,
          error: validationResult.error!,
          diagnostics: this.getBasicDiagnostics()
        };
      }

      // Check WebAuthn support
      const supportCheck = await this.checkWebAuthnSupport();
      if (!supportCheck.supported) {
        return {
          success: false,
          error: supportCheck.error!,
          diagnostics: this.getBasicDiagnostics()
        };
      }

      // Detect platform configuration
      const platformConfig = PlatformDetector.detectPlatform();

      // Authenticate using WebAuthn
      const webAuthnResult = await WebAuthnCore.authenticate(options, platformConfig);
      if (!webAuthnResult.success) {
        return {
          success: false,
          error: webAuthnResult.error!,
          diagnostics: this.buildDiagnostics(platformConfig, null, webAuthnResult.keyDerivationMethod || "unknown")
        };
      }

      const result: AuthenticateResult = {
        success: true,
        authentication: {
          credentialId: webAuthnResult.credentialId!,
          signature: webAuthnResult.signature!,
          authenticatorData: webAuthnResult.authenticatorData!,
          keyDerivationMethod: webAuthnResult.keyDerivationMethod!
        },
        diagnostics: this.buildDiagnostics(platformConfig, webAuthnResult.prfResult || null, webAuthnResult.keyDerivationMethod!)
      };

      // Handle optional decryption
      if (options.encryptedData) {
        const decryptionResult = await EncryptionUtils.decryptData(
          options.encryptedData,
          webAuthnResult.keyMaterial!,
          options.encryptionSalt || "webauthn-prf-salt-v1"
        );

        if (decryptionResult.success) {
          result.decryption = {
            plaintext: decryptionResult.plaintext!,
            keyDerivationMethod: webAuthnResult.keyDerivationMethod!
          };
        } else {
          return {
            success: false,
            error: `Decryption failed: ${decryptionResult.error}`,
            diagnostics: result.diagnostics
          };
        }
      }

      return result;

    } catch (error) {
      return {
        success: false,
        error: `Unexpected error: ${error instanceof Error ? error.message : 'Unknown error'}`,
        diagnostics: this.getBasicDiagnostics()
      };
    }
  }

  /**
   * Check if WebAuthn is supported on this platform
   */
  private static async checkWebAuthnSupport(): Promise<{supported: boolean, error?: string}> {
    // DIAGNOSTIC LOGGING - Check WebAuthn capabilities (from working code)
    console.log("=== WebAuthn Diagnostic Information ===");
    console.log("User Agent:", navigator.userAgent);
    console.log("Platform:", navigator.platform);
    console.log("WebAuthn Support:", !!window.PublicKeyCredential);
    console.log("isUserVerifyingPlatformAuthenticatorAvailable:", !!window.PublicKeyCredential?.isUserVerifyingPlatformAuthenticatorAvailable);
    
    // Check for PRF extension support (from working code)
    if (window.PublicKeyCredential && (window.PublicKeyCredential as any).getClientCapabilities) {
      try {
        const capabilities = await (window.PublicKeyCredential as any).getClientCapabilities();
        console.log("Client Capabilities:", capabilities);
      } catch (e) {
        console.log("getClientCapabilities not supported or failed:", e);
      }
    } else {
      console.log("getClientCapabilities not available");
    }

    if (!window.PublicKeyCredential || !window.PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable) {
      return {
        supported: false,
        error: "WebAuthn is not supported on this device"
      };
    }

    try {
      const available = await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
      console.log("Platform authenticator available:", available);
      if (!available) {
        return {
          supported: false,
          error: "Platform authenticator (Windows Hello/TouchID/etc.) is not available"
        };
      }
    } catch (error) {
      return {
        supported: false,
        error: "Failed to check platform authenticator availability"
      };
    }

    return { supported: true };
  }

  /**
   * Build comprehensive diagnostics information
   */
  private static buildDiagnostics(
    platformConfig: PlatformConfig, 
    prfResult: PRFResult | null, 
    keyDerivationMethod: string
  ) {
    return {
      userAgent: navigator.userAgent,
      platform: navigator.platform,
      prfSupported: prfResult?.enabled || false,
      prfResultsAvailable: !!(prfResult?.results?.first),
      authenticatorType: this.getAuthenticatorType(keyDerivationMethod as KeyDerivationMethod, platformConfig, prfResult),
      keyDerivationMethod: keyDerivationMethod,
      credentialIdLength: 0 // Will be set by caller with actual credential
    };
  }

  /**
   * Get basic diagnostics when detailed info isn't available
   */
  private static getBasicDiagnostics() {
    return {
      userAgent: navigator.userAgent,
      platform: navigator.platform,
      prfSupported: false,
      prfResultsAvailable: false,
      authenticatorType: "Unknown",
      keyDerivationMethod: "unknown",
      credentialIdLength: 0
    };
  }

  /**
   * Determine authenticator type based on platform and PRF support (from working code)
   */
  private static getAuthenticatorType(keyDerivationMethod: KeyDerivationMethod, platformConfig: PlatformConfig, prfResult?: PRFResult | null): string {
    if (keyDerivationMethod === "PRF") {
      if (platformConfig.isSamsung) return "Samsung Pass (PRF-enabled)";
      if (platformConfig.isIOS) return "Face ID/Touch ID (PRF-enabled)";
      if (platformConfig.isWindows) return "Windows Hello (PRF-enabled)";
      return "Platform Authenticator (PRF-enabled)";
    } else {
      // Even if PRF didn't work, some platforms might still support it (from working code)
      if (platformConfig.isSamsung && prfResult?.enabled) return "Samsung Pass (PRF detection issue)";
      if (platformConfig.isSamsung) return "Samsung Pass (PIN fallback)";
      if (platformConfig.isIOS) return "Face ID/Touch ID (fallback mode)";
      if (platformConfig.isWindows) return "Windows Hello (fallback mode)";
      return "Platform Authenticator (credential ID fallback)";
    }
  }
}

// Export for global window usage (IIFE format)
declare global {
  interface Window {
    SimpleWebAuthn: typeof SimpleWebAuthn;
  }
}

// Attach to window for browser usage
if (typeof window !== 'undefined') {
  window.SimpleWebAuthn = SimpleWebAuthn;
}