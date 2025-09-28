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
      
      console.log("=== Credential Creation Debug ===");
      console.log("Origin:", window.location.origin);
      console.log("Challenge:", new Uint8Array(challenge));
      console.log("User ID:", new Uint8Array(userId));
      
      // Build credential creation options
      const defaultPubKeyParams = [
        {
          alg: -7,  // ES256
          type: "public-key"
        },
        {
          alg: -257,  // RS256
          type: "public-key"
        }
      ];

      const credentialCreationOptions: PublicKeyCredentialCreationOptions = {
        challenge: challenge.buffer,  // Use ArrayBuffer
        rp: {
          name: options.rp.name,
          id: options.rp.id || window.location.hostname
        },
        user: {
          id: userId.buffer,  // Use ArrayBuffer
          name: options.user.name,
          displayName: options.user.displayName
        },
        pubKeyCredParams: options.pubKeyCredParams || defaultPubKeyParams,
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

      // Serialize options for debugging
      const serializedOptions = JSON.stringify(credentialCreationOptions, (_key, value) => {
        if (value instanceof Uint8Array) {
          return Array.from(value);
        }
        if (value instanceof ArrayBuffer) {
          return Array.from(new Uint8Array(value));
        }
        return value;
      }, 2);
      console.log("Creation options for breakpoint:", serializedOptions);

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
      console.log("=== Created Credential Debug ===");
      console.log("Raw credential ID:", new Uint8Array(credential.rawId));
      console.log("Credential ID length:", credential.rawId.byteLength);
      
      // Store raw credential ID bytes directly
      const credentialResult: CredentialResult = {
        id: Array.from(new Uint8Array(credential.rawId)),  // Store as number array
        rawId: credential.rawId,
        publicKey: Array.from(new Uint8Array(credential.rawId)),  // Store as number array
        attestationObject: Array.from(new Uint8Array((credential.response as AuthenticatorAttestationResponse).attestationObject))
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
      console.log("=== Authentication Request Debug ===");
      console.log("Origin:", window.location.origin);
      console.log("Incoming credential bytes:", options.allowCredentials[0].id);
      console.log("Incoming credential length:", options.allowCredentials[0].id.length);
      
      // Generate challenge
      const challenge = crypto.getRandomValues(new Uint8Array(32));
      console.log("Challenge:", new Uint8Array(challenge));
      
      // Convert credential ID from number array to Uint8Array
      const credentialIdBytes = new Uint8Array(options.allowCredentials[0].id);
      console.log("Credential bytes:", credentialIdBytes);
      console.log("Credential length:", credentialIdBytes.length);
      
      // Build authentication options using rpId from options or fallback to hostname
      const rpId = options.rpId || window.location.hostname;
      console.log("Using rpId:", rpId);
      
      const getOptions: PublicKeyCredentialRequestOptions = {
        challenge: challenge.buffer,  // Use ArrayBuffer
        rpId: rpId,
        allowCredentials: [{
          type: "public-key",
          id: credentialIdBytes.buffer  // Use ArrayBuffer
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

      // Log final options before WebAuthn call
      console.log("=== WebAuthn Call Debug ===");
      console.log("PublicKeyCredentialRequestOptions:", {
        rpId: getOptions.rpId,
        challenge: Array.from(new Uint8Array(getOptions.challenge as ArrayBuffer)),
        allowCredentials: getOptions.allowCredentials?.map(cred => ({
          type: cred.type,
          id: Array.from(cred.id as Uint8Array),
          transports: cred.transports
        })),
        timeout: getOptions.timeout,
        userVerification: getOptions.userVerification,
        extensions: getOptions.extensions
      });
      
      // Log the credential bytes for comparison
      console.log("Credential ID check:", {
        incoming: options.allowCredentials[0].id,
        converted: Array.from(credentialIdBytes),
        matches: Array.from(credentialIdBytes).join(',') === 
                 Array.from(credentialIdBytes).join(',')  // Compare with our converted bytes
      });

      // Serialize options for debugging
      const serializedGetOptions = JSON.stringify(getOptions, (_key, value) => {
        if (value instanceof Uint8Array) {
          return Array.from(value);
        }
        if (value instanceof ArrayBuffer) {
          return Array.from(new Uint8Array(value));
        }
        return value;
      }, 2);
      console.log("Get options for breakpoint:", serializedGetOptions);

      // Authenticate
      console.log("Calling navigator.credentials.get()...");
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
      
      console.log("=== Authentication Response Debug ===");
      console.log("Raw credential ID:", new Uint8Array(credential.rawId));
      console.log("Raw credential length:", credential.rawId.byteLength);
      
      // Extract authentication information  
      // Convert rawId to standard base64 (not URL-safe base64 like credential.id)
      const credentialId = btoa(String.fromCharCode(...new Uint8Array(credential.rawId)));
      console.log("Converted base64 credential:", credentialId);
      console.log("Converted base64 length:", credentialId.length);
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