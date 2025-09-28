import { 
  CreateCredentialOptions,
  AuthenticateOptions,
  PlatformConfig, 
  PRFResult,
  CredentialResult,
  KeyDerivationMethod
} from './types.js';
import { EncryptionUtils } from './encryption-utils.js';

/**
 * Core WebAuthn functionality with PRF support
 */
export class WebAuthnCore {
  
  /**
   * Create a new WebAuthn credential with PRF extension
   */
  static async createCredential(
    options: CreateCredentialOptions,
    platformConfig: PlatformConfig
  ): Promise<{
    success: boolean,
    error?: string,
    credential?: CredentialResult,
    keyMaterial?: CryptoKey,
    keyDerivationMethod?: KeyDerivationMethod,
    prfResult?: PRFResult
  }> {
    try {
      // Validate required encryptionSalt
      if (!options.encryptionSalt || options.encryptionSalt.trim() === '') {
        return {
          success: false,
          error: "encryptionSalt is required and cannot be empty"
        };
      }
      // Generate challenge and user ID
      const challenge = crypto.getRandomValues(new Uint8Array(32));
      const userId = crypto.getRandomValues(new Uint8Array(32));
      
      // Build credential creation options
      const credentialCreationOptions: PublicKeyCredentialCreationOptions = {
        challenge: challenge,
        rp: {
          name: options.rp.name,
          id: options.rp.id || window.location.hostname
        },
        user: {
          id: userId,
          name: options.user.name,
          displayName: options.user.displayName
        },
        pubKeyCredParams: options.pubKeyCredParams,
        timeout: options.timeout || platformConfig.timeout,
        authenticatorSelection: {
          authenticatorAttachment: options.authenticatorSelection?.authenticatorAttachment || "platform",
          userVerification: options.authenticatorSelection?.userVerification || "required",
          residentKey: options.authenticatorSelection?.residentKey || platformConfig.residentKey
        },
        extensions: {
          prf: {
            eval: {
              first: new TextEncoder().encode(options.encryptionSalt)
            }
          }
        }
      };

      // Create the credential
      const credential = await navigator.credentials.create({
        publicKey: credentialCreationOptions
      }) as PublicKeyCredential;

      if (!credential) {
        return {
          success: false,
          error: "Failed to create credential - user may have cancelled"
        };
      }

      // Extract credential information
      const credentialResult: CredentialResult = {
        id: credential.id,
        rawId: credential.rawId,
        publicKey: btoa(String.fromCharCode(...new Uint8Array(credential.rawId))), // For now, using credential ID
        attestationObject: btoa(String.fromCharCode(...new Uint8Array((credential.response as AuthenticatorAttestationResponse).attestationObject)))
      };

      // Handle PRF extension results
      const prfResult = credential.getClientExtensionResults().prf as PRFResult;
      let keyMaterial: CryptoKey;
      let keyDerivationMethod: KeyDerivationMethod;

      console.log("=== PRF Extension Analysis (Creation) ===");
      console.log("PRF Result Object:", prfResult);
      console.log("PRF Enabled:", prfResult?.enabled);
      console.log("PRF Results Available:", !!prfResult?.results);
      console.log("PRF First Result:", prfResult?.results?.first ? `${prfResult.results.first.byteLength} bytes` : "none");

      // For registration: check if PRF is enabled (per WebAuthn spec)
      if (prfResult && prfResult.enabled) {
        // Try to get PRF results from creation
        if (prfResult.results && prfResult.results.first) {
          console.log("✅ Got PRF results directly from credential creation");
          console.log("PRF Output Length:", prfResult.results.first.byteLength, "bytes");
          keyMaterial = await EncryptionUtils.importKeyMaterial(prfResult.results.first);
          keyDerivationMethod = "PRF";
        } else {
          console.log("⚠️ PRF enabled but no results - attempting get() operation...");
          // Need to do a get() operation to get PRF results
          const prfKeyMaterial = await this.attemptPRFGet(credential.rawId, options.encryptionSalt, platformConfig);
          
          if (prfKeyMaterial.success) {
            keyMaterial = prfKeyMaterial.keyMaterial!;
            keyDerivationMethod = "PRF";
          } else {
            console.log("❌ PRF get() operation failed, falling back to credential ID:", prfKeyMaterial.error);
            keyMaterial = await EncryptionUtils.importKeyMaterial(credential.rawId);
            keyDerivationMethod = "CredentialID";
          }
        }
      } else {
        console.log("❌ PRF not supported - using credential ID for key derivation");
        console.log("Falling back to Credential ID method");
        console.log("Credential ID Length:", credential.rawId.byteLength, "bytes");
        keyMaterial = await EncryptionUtils.importKeyMaterial(credential.rawId);
        keyDerivationMethod = "CredentialID";
      }

      return {
        success: true,
        credential: credentialResult,
        keyMaterial,
        keyDerivationMethod,
        prfResult
      };

    } catch (error) {
      return {
        success: false,
        error: `WebAuthn credential creation failed: ${error instanceof Error ? error.message : 'Unknown error'}`
      };
    }
  }

  /**
   * Authenticate with an existing WebAuthn credential
   */
  static async authenticate(
    options: AuthenticateOptions,
    platformConfig: PlatformConfig
  ): Promise<{
    success: boolean,
    error?: string,
    credentialId?: string,
    signature?: string,
    authenticatorData?: string,
    keyMaterial?: CryptoKey,
    keyDerivationMethod?: KeyDerivationMethod,
    prfResult?: PRFResult
  }> {
    try {
      // Validate required encryptionSalt
      if (!options.encryptionSalt || options.encryptionSalt.trim() === '') {
        return {
          success: false,
          error: "encryptionSalt is required and cannot be empty"
        };
      }
      // Generate challenge
      const challenge = crypto.getRandomValues(new Uint8Array(32));
      
      // Convert credential ID from base64
      const credentialIdBytes = Uint8Array.from(atob(options.allowCredentials[0].id), c => c.charCodeAt(0));
      
      // Build authentication options
      const getOptions: PublicKeyCredentialRequestOptions = {
        challenge: challenge,
        allowCredentials: [{
          type: "public-key",
          id: credentialIdBytes
        }],
        timeout: options.timeout || platformConfig.timeout,
        userVerification: options.userVerification || "required",
        extensions: {
          prf: {
            eval: {
              first: new TextEncoder().encode(options.encryptionSalt)
            }
          }
        }
      };

      // Authenticate
      const credential = await navigator.credentials.get({
        publicKey: getOptions
      }) as PublicKeyCredential;

      if (!credential) {
        return {
          success: false,
          error: "Authentication failed - user may have cancelled"
        };
      }

      const response = credential.response as AuthenticatorAssertionResponse;
      
      // Extract authentication information
      const credentialId = credential.id;
      const signature = btoa(String.fromCharCode(...new Uint8Array(response.signature)));
      const authenticatorData = btoa(String.fromCharCode(...new Uint8Array(response.authenticatorData)));

      // Handle PRF extension results
      const prfResult = credential.getClientExtensionResults().prf as PRFResult;
      let keyMaterial: CryptoKey;
      let keyDerivationMethod: KeyDerivationMethod;

      console.log("=== PRF Extension Analysis (Authentication) ===");
      console.log("PRF Result Object:", prfResult);
      console.log("PRF Enabled:", prfResult?.enabled);
      console.log("PRF Results Available:", !!prfResult?.results);
      console.log("PRF First Result:", prfResult?.results?.first ? `${prfResult.results.first.byteLength} bytes` : "none");

      // Enhanced PRF detection for Samsung Pass and other platforms (from working code)
      if (prfResult && prfResult.results && prfResult.results.first && prfResult.results.first.byteLength > 0) {
        console.log("✅ PRF extension successful - using PRF key material");
        console.log("PRF Output Length:", prfResult.results.first.byteLength, "bytes");
        keyMaterial = await EncryptionUtils.importKeyMaterial(prfResult.results.first);
        keyDerivationMethod = "PRF";
      } else if (prfResult && prfResult.enabled && !prfResult.results && platformConfig.isSamsung) {
        console.log("⚠️ Samsung Pass PRF enabled but no results - this is expected behavior");
        console.log("❌ PRF not available - falling back to credential ID derivation");
        keyMaterial = await EncryptionUtils.importKeyMaterial(credential.rawId);
        keyDerivationMethod = "CredentialID";
      } else {
        console.log("❌ PRF not available - falling back to credential ID derivation");
        keyMaterial = await EncryptionUtils.importKeyMaterial(credential.rawId);
        keyDerivationMethod = "CredentialID";
      }

      return {
        success: true,
        credentialId,
        signature,
        authenticatorData,
        keyMaterial,
        keyDerivationMethod,
        prfResult
      };

    } catch (error) {
      return {
        success: false,
        error: `WebAuthn authentication failed: ${error instanceof Error ? error.message : 'Unknown error'}`
      };
    }
  }

  /**
   * Attempt to get PRF results via get() operation
   * This is needed for some authenticators that don't return PRF results during creation
   */
  private static async attemptPRFGet(
    credentialId: ArrayBuffer,
    salt: string,
    _platformConfig: PlatformConfig
  ): Promise<{success: boolean, keyMaterial?: CryptoKey, error?: string}> {
    try {
      const getChallenge = crypto.getRandomValues(new Uint8Array(32));
      const getOptions: PublicKeyCredentialRequestOptions = {
        challenge: getChallenge,
        allowCredentials: [{
          type: "public-key",
          id: credentialId
        }],
        userVerification: "required",
        extensions: {
          prf: {
            eval: {
              first: new TextEncoder().encode(salt)
            }
          }
        }
      };

      const getCredential = await navigator.credentials.get({
        publicKey: getOptions
      }) as PublicKeyCredential;

      const getPrfResult = getCredential.getClientExtensionResults().prf as PRFResult;
      console.log("=== PRF Extension Analysis (Get Operation) ===");
      console.log("Get PRF Result:", getPrfResult);
      console.log("Get PRF Results Available:", !!getPrfResult?.results);

      if (getPrfResult && getPrfResult.results && getPrfResult.results.first) {
        console.log("✅ PRF results obtained from get() operation");
        const keyMaterial = await EncryptionUtils.importKeyMaterial(getPrfResult.results.first);
        return {
          success: true,
          keyMaterial
        };
      } else {
        return {
          success: false,
          error: "PRF get() operation failed - no results"
        };
      }
    } catch (error) {
      return {
        success: false,
        error: `PRF get() operation failed: ${error instanceof Error ? error.message : 'Unknown error'}`
      };
    }
  }
}