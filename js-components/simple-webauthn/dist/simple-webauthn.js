var SimpleWebAuthn = (function (exports) {
    'use strict';

    /**
     * Platform detection utilities for WebAuthn optimization
     */
    class PlatformDetector {
        /**
         * Detect current platform and return optimized configuration
         */
        static detectPlatform() {
            const userAgent = navigator.userAgent;
            const isSamsung = userAgent.includes('Samsung') || userAgent.includes('SM-');
            const isIOS = /iPad|iPhone|iPod/.test(userAgent);
            const isWindows = userAgent.includes('Windows');
            // Platform-specific timeout optimization
            let timeout = 60000; // Default 60 seconds
            if (isSamsung) {
                timeout = 300000; // Samsung Pass needs 5 minutes
            }
            // Platform-specific resident key requirement
            let residentKey = "preferred"; // Default
            if (isSamsung) {
                residentKey = "required"; // Samsung Pass requires this
            }
            return {
                isSamsung,
                isIOS,
                isWindows,
                timeout,
                residentKey
            };
        }
        /**
         * Log platform detection information for diagnostics
         */
        static logPlatformInfo() {
            const config = this.detectPlatform();
            console.log("=== Platform Detection ===");
            console.log("User Agent:", navigator.userAgent);
            console.log("Platform:", navigator.platform);
            console.log("Samsung:", config.isSamsung);
            console.log("iOS:", config.isIOS);
            console.log("Windows:", config.isWindows);
            console.log("Timeout:", config.timeout, "ms");
            console.log("Resident Key:", config.residentKey);
            console.log("========================");
        }
    }

    /**
     * Encryption utilities using Web Crypto API
     */
    class EncryptionUtils {
        /**
         * Encrypt plaintext using AES-GCM with HKDF key derivation
         * Note: salt parameter is for PRF extension, HKDF uses fixed salt for compatibility
         */
        static async encryptData(plaintext, keyMaterial, _salt // PRF salt, not used in HKDF for compatibility with working code
        ) {
            try {
                // Derive AES-GCM key from the key material using HKDF (exact format from working code)
                const aesKey = await crypto.subtle.deriveKey({
                    name: "HKDF",
                    hash: "SHA-256",
                    salt: new TextEncoder().encode("clypse-encryption-salt-v1"),
                    info: new TextEncoder().encode("clypse-encryption-key")
                }, keyMaterial, { name: "AES-GCM", length: 256 }, false, ["encrypt"]);
                // Generate random IV
                const iv = crypto.getRandomValues(new Uint8Array(12));
                // Encrypt the plaintext
                const plaintextBytes = new TextEncoder().encode(plaintext);
                const encryptedData = await crypto.subtle.encrypt({ name: "AES-GCM", iv: iv }, aesKey, plaintextBytes);
                // Combine IV and encrypted data
                const combined = new Uint8Array(iv.length + encryptedData.byteLength);
                combined.set(iv, 0);
                combined.set(new Uint8Array(encryptedData), iv.length);
                // Convert to base64
                const base64 = btoa(String.fromCharCode(...combined));
                return {
                    success: true,
                    encryptedData: base64
                };
            }
            catch (error) {
                return {
                    success: false,
                    error: `Encryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`
                };
            }
        }
        /**
         * Decrypt data using AES-GCM with HKDF key derivation
         * Note: salt parameter is for PRF extension, HKDF uses fixed salt for compatibility
         */
        static async decryptData(encryptedDataBase64, keyMaterial, _salt // PRF salt, not used in HKDF for compatibility with working code
        ) {
            try {
                // Derive AES-GCM key from the key material (same as encryption - exact format from working code)
                const aesKey = await crypto.subtle.deriveKey({
                    name: "HKDF",
                    hash: "SHA-256",
                    salt: new TextEncoder().encode("clypse-encryption-salt-v1"),
                    info: new TextEncoder().encode("clypse-encryption-key")
                }, keyMaterial, { name: "AES-GCM", length: 256 }, false, ["decrypt"]);
                // Decode base64
                const encryptedBytes = Uint8Array.from(atob(encryptedDataBase64), c => c.charCodeAt(0));
                // Extract IV and encrypted data
                const iv = encryptedBytes.slice(0, 12);
                const encryptedData = encryptedBytes.slice(12);
                // Decrypt the data
                const decryptedData = await crypto.subtle.decrypt({ name: "AES-GCM", iv: iv }, aesKey, encryptedData);
                // Convert decrypted bytes to text
                const plaintext = new TextDecoder().decode(decryptedData);
                return {
                    success: true,
                    plaintext: plaintext
                };
            }
            catch (error) {
                return {
                    success: false,
                    error: `Decryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`
                };
            }
        }
        /**
         * Import raw bytes as HKDF key material
         */
        static async importKeyMaterial(keyBytes) {
            return await crypto.subtle.importKey("raw", keyBytes, "HKDF", false, ["deriveKey"]);
        }
    }

    /**
     * Core WebAuthn functionality with PRF support
     */
    class WebAuthnCore {
        /**
         * Create a new WebAuthn credential with PRF extension
         */
        static async createCredential(options, platformConfig) {
            try {
                // Generate challenge and user ID
                const challenge = crypto.getRandomValues(new Uint8Array(32));
                const userId = crypto.getRandomValues(new Uint8Array(32));
                // Build credential creation options
                const credentialCreationOptions = {
                    challenge: challenge,
                    rp: {
                        name: options.rpName,
                        id: options.rpId || window.location.hostname
                    },
                    user: {
                        id: userId,
                        name: options.userName,
                        displayName: options.userDisplayName
                    },
                    pubKeyCredParams: [
                        { alg: -7, type: "public-key" }, // ES256
                        { alg: -257, type: "public-key" } // RS256
                    ],
                    timeout: options.timeout || platformConfig.timeout,
                    authenticatorSelection: {
                        authenticatorAttachment: options.authenticatorAttachment || "platform",
                        userVerification: options.userVerification || "required",
                        residentKey: options.residentKey || platformConfig.residentKey
                    },
                    extensions: {
                        prf: {
                            eval: {
                                first: new TextEncoder().encode(options.encryptionSalt || "webauthn-prf-salt-v1")
                            }
                        }
                    }
                };
                // Create the credential
                const credential = await navigator.credentials.create({
                    publicKey: credentialCreationOptions
                });
                if (!credential) {
                    return {
                        success: false,
                        error: "Failed to create credential - user may have cancelled"
                    };
                }
                // Extract credential information
                const credentialResult = {
                    id: credential.id,
                    rawId: credential.rawId,
                    publicKey: btoa(String.fromCharCode(...new Uint8Array(credential.rawId))), // For now, using credential ID
                    attestationObject: btoa(String.fromCharCode(...new Uint8Array(credential.response.attestationObject)))
                };
                // Handle PRF extension results
                const prfResult = credential.getClientExtensionResults().prf;
                let keyMaterial;
                let keyDerivationMethod;
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
                    }
                    else {
                        console.log("⚠️ PRF enabled but no results - attempting get() operation...");
                        // Need to do a get() operation to get PRF results
                        const prfKeyMaterial = await this.attemptPRFGet(credential.rawId, options.encryptionSalt || "webauthn-prf-salt-v1", platformConfig);
                        if (prfKeyMaterial.success) {
                            keyMaterial = prfKeyMaterial.keyMaterial;
                            keyDerivationMethod = "PRF";
                        }
                        else {
                            console.log("❌ PRF get() operation failed, falling back to credential ID:", prfKeyMaterial.error);
                            keyMaterial = await EncryptionUtils.importKeyMaterial(credential.rawId);
                            keyDerivationMethod = "CredentialID";
                        }
                    }
                }
                else {
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
            }
            catch (error) {
                return {
                    success: false,
                    error: `WebAuthn credential creation failed: ${error instanceof Error ? error.message : 'Unknown error'}`
                };
            }
        }
        /**
         * Authenticate with an existing WebAuthn credential
         */
        static async authenticate(options, platformConfig) {
            try {
                // Generate challenge
                const challenge = crypto.getRandomValues(new Uint8Array(32));
                // Convert credential ID from base64
                const credentialIdBytes = Uint8Array.from(atob(options.credentialId), c => c.charCodeAt(0));
                // Build authentication options
                const getOptions = {
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
                                first: new TextEncoder().encode(options.encryptionSalt || "webauthn-prf-salt-v1")
                            }
                        }
                    }
                };
                // Authenticate
                const credential = await navigator.credentials.get({
                    publicKey: getOptions
                });
                if (!credential) {
                    return {
                        success: false,
                        error: "Authentication failed - user may have cancelled"
                    };
                }
                const response = credential.response;
                // Extract authentication information
                const credentialId = credential.id;
                const signature = btoa(String.fromCharCode(...new Uint8Array(response.signature)));
                const authenticatorData = btoa(String.fromCharCode(...new Uint8Array(response.authenticatorData)));
                // Handle PRF extension results
                const prfResult = credential.getClientExtensionResults().prf;
                let keyMaterial;
                let keyDerivationMethod;
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
                }
                else if (prfResult && prfResult.enabled && !prfResult.results && platformConfig.isSamsung) {
                    console.log("⚠️ Samsung Pass PRF enabled but no results - this is expected behavior");
                    console.log("❌ PRF not available - falling back to credential ID derivation");
                    keyMaterial = await EncryptionUtils.importKeyMaterial(credential.rawId);
                    keyDerivationMethod = "CredentialID";
                }
                else {
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
            }
            catch (error) {
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
        static async attemptPRFGet(credentialId, salt, _platformConfig) {
            try {
                const getChallenge = crypto.getRandomValues(new Uint8Array(32));
                const getOptions = {
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
                });
                const getPrfResult = getCredential.getClientExtensionResults().prf;
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
                }
                else {
                    return {
                        success: false,
                        error: "PRF get() operation failed - no results"
                    };
                }
            }
            catch (error) {
                return {
                    success: false,
                    error: `PRF get() operation failed: ${error instanceof Error ? error.message : 'Unknown error'}`
                };
            }
        }
    }

    /**
     * Input validation utilities for SimpleWebAuthn
     */
    class InputValidator {
        /**
         * Validate createCredential options
         */
        static validateCreateOptions(options) {
            // Check required fields
            if (!options.rpName || options.rpName.trim().length === 0) {
                return { valid: false, error: "rpName is required and cannot be empty" };
            }
            if (!options.userName || options.userName.trim().length === 0) {
                return { valid: false, error: "userName is required and cannot be empty" };
            }
            if (!options.userDisplayName || options.userDisplayName.trim().length === 0) {
                return { valid: false, error: "userDisplayName is required and cannot be empty" };
            }
            // Validate optional rpId if provided
            if (options.rpId !== undefined) {
                if (typeof options.rpId !== 'string' || options.rpId.trim().length === 0) {
                    return { valid: false, error: "rpId must be a non-empty string if provided" };
                }
                // Basic validation - must be a valid hostname format
                const rpIdRegex = /^[a-zA-Z0-9.-]+$/;
                if (!rpIdRegex.test(options.rpId)) {
                    return { valid: false, error: "rpId must be a valid hostname format" };
                }
            }
            // Validate timeout if provided
            if (options.timeout !== undefined) {
                if (typeof options.timeout !== 'number' || options.timeout <= 0 || options.timeout > 600000) {
                    return { valid: false, error: "timeout must be a positive number between 1 and 600000 (10 minutes)" };
                }
            }
            // Validate userVerification if provided
            if (options.userVerification !== undefined) {
                const validValues = ["required", "preferred", "discouraged"];
                if (!validValues.includes(options.userVerification)) {
                    return { valid: false, error: "userVerification must be 'required', 'preferred', or 'discouraged'" };
                }
            }
            // Validate authenticatorAttachment if provided
            if (options.authenticatorAttachment !== undefined) {
                const validValues = ["platform", "cross-platform"];
                if (!validValues.includes(options.authenticatorAttachment)) {
                    return { valid: false, error: "authenticatorAttachment must be 'platform' or 'cross-platform'" };
                }
            }
            // Validate residentKey if provided
            if (options.residentKey !== undefined) {
                const validValues = ["required", "preferred", "discouraged"];
                if (!validValues.includes(options.residentKey)) {
                    return { valid: false, error: "residentKey must be 'required', 'preferred', or 'discouraged'" };
                }
            }
            // Validate encryption salt if provided
            if (options.encryptionSalt !== undefined) {
                if (typeof options.encryptionSalt !== 'string' || options.encryptionSalt.trim().length === 0) {
                    return { valid: false, error: "encryptionSalt must be a non-empty string if provided" };
                }
            }
            return { valid: true };
        }
        /**
         * Validate authenticate options
         */
        static validateAuthOptions(options) {
            // Check required fields
            if (!options.credentialId || options.credentialId.trim().length === 0) {
                return { valid: false, error: "credentialId is required and cannot be empty" };
            }
            // Validate credentialId format (should be base64)
            try {
                atob(options.credentialId);
            }
            catch (error) {
                return { valid: false, error: "credentialId must be a valid base64 string" };
            }
            // Validate timeout if provided
            if (options.timeout !== undefined) {
                if (typeof options.timeout !== 'number' || options.timeout <= 0 || options.timeout > 600000) {
                    return { valid: false, error: "timeout must be a positive number between 1 and 600000 (10 minutes)" };
                }
            }
            // Validate userVerification if provided
            if (options.userVerification !== undefined) {
                const validValues = ["required", "preferred", "discouraged"];
                if (!validValues.includes(options.userVerification)) {
                    return { valid: false, error: "userVerification must be 'required', 'preferred', or 'discouraged'" };
                }
            }
            // Validate encryptedData if provided (should be base64)
            if (options.encryptedData !== undefined) {
                if (typeof options.encryptedData !== 'string' || options.encryptedData.trim().length === 0) {
                    return { valid: false, error: "encryptedData must be a non-empty string if provided" };
                }
                try {
                    atob(options.encryptedData);
                }
                catch (error) {
                    return { valid: false, error: "encryptedData must be a valid base64 string" };
                }
            }
            // Validate encryption salt if provided
            if (options.encryptionSalt !== undefined) {
                if (typeof options.encryptionSalt !== 'string' || options.encryptionSalt.trim().length === 0) {
                    return { valid: false, error: "encryptionSalt must be a non-empty string if provided" };
                }
            }
            return { valid: true };
        }
    }

    /**
     * SimpleWebAuthn - A reusable library for WebAuthn credential management
     * with optional PRF-based data encryption
     */
    class SimpleWebAuthn {
        /**
         * Creates a new WebAuthn credential and optionally encrypts data
         * @param options - Credential creation options
         * @returns Promise<CreateCredentialResult>
         */
        static async createCredential(options) {
            try {
                // Validate inputs
                const validationResult = InputValidator.validateCreateOptions(options);
                if (!validationResult.valid) {
                    return {
                        success: false,
                        error: validationResult.error,
                        diagnostics: this.getBasicDiagnostics()
                    };
                }
                // Check WebAuthn support
                const supportCheck = await this.checkWebAuthnSupport();
                if (!supportCheck.supported) {
                    return {
                        success: false,
                        error: supportCheck.error,
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
                        error: webAuthnResult.error,
                        diagnostics: this.buildDiagnostics(platformConfig, null, webAuthnResult.keyDerivationMethod || "unknown")
                    };
                }
                const result = {
                    success: true,
                    credential: webAuthnResult.credential,
                    diagnostics: this.buildDiagnostics(platformConfig, webAuthnResult.prfResult || null, webAuthnResult.keyDerivationMethod)
                };
                // Handle optional encryption
                if (options.plaintextToEncrypt) {
                    const encryptionResult = await EncryptionUtils.encryptData(options.plaintextToEncrypt, webAuthnResult.keyMaterial, options.encryptionSalt || "webauthn-prf-salt-v1");
                    if (encryptionResult.success) {
                        result.encryption = {
                            encryptedData: encryptionResult.encryptedData,
                            keyDerivationMethod: webAuthnResult.keyDerivationMethod
                        };
                    }
                    else {
                        return {
                            success: false,
                            error: `Encryption failed: ${encryptionResult.error}`,
                            diagnostics: result.diagnostics
                        };
                    }
                }
                return result;
            }
            catch (error) {
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
        static async authenticate(options) {
            try {
                // Validate inputs
                const validationResult = InputValidator.validateAuthOptions(options);
                if (!validationResult.valid) {
                    return {
                        success: false,
                        error: validationResult.error,
                        diagnostics: this.getBasicDiagnostics()
                    };
                }
                // Check WebAuthn support
                const supportCheck = await this.checkWebAuthnSupport();
                if (!supportCheck.supported) {
                    return {
                        success: false,
                        error: supportCheck.error,
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
                        error: webAuthnResult.error,
                        diagnostics: this.buildDiagnostics(platformConfig, null, webAuthnResult.keyDerivationMethod || "unknown")
                    };
                }
                const result = {
                    success: true,
                    authentication: {
                        credentialId: webAuthnResult.credentialId,
                        signature: webAuthnResult.signature,
                        authenticatorData: webAuthnResult.authenticatorData,
                        keyDerivationMethod: webAuthnResult.keyDerivationMethod
                    },
                    diagnostics: this.buildDiagnostics(platformConfig, webAuthnResult.prfResult || null, webAuthnResult.keyDerivationMethod)
                };
                // Handle optional decryption
                if (options.encryptedData) {
                    const decryptionResult = await EncryptionUtils.decryptData(options.encryptedData, webAuthnResult.keyMaterial, options.encryptionSalt || "webauthn-prf-salt-v1");
                    if (decryptionResult.success) {
                        result.decryption = {
                            plaintext: decryptionResult.plaintext,
                            keyDerivationMethod: webAuthnResult.keyDerivationMethod
                        };
                    }
                    else {
                        return {
                            success: false,
                            error: `Decryption failed: ${decryptionResult.error}`,
                            diagnostics: result.diagnostics
                        };
                    }
                }
                return result;
            }
            catch (error) {
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
        static async checkWebAuthnSupport() {
            // DIAGNOSTIC LOGGING - Check WebAuthn capabilities (from working code)
            console.log("=== WebAuthn Diagnostic Information ===");
            console.log("User Agent:", navigator.userAgent);
            console.log("Platform:", navigator.platform);
            console.log("WebAuthn Support:", !!window.PublicKeyCredential);
            console.log("isUserVerifyingPlatformAuthenticatorAvailable:", !!window.PublicKeyCredential?.isUserVerifyingPlatformAuthenticatorAvailable);
            // Check for PRF extension support (from working code)
            if (window.PublicKeyCredential && window.PublicKeyCredential.getClientCapabilities) {
                try {
                    const capabilities = await window.PublicKeyCredential.getClientCapabilities();
                    console.log("Client Capabilities:", capabilities);
                }
                catch (e) {
                    console.log("getClientCapabilities not supported or failed:", e);
                }
            }
            else {
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
            }
            catch (error) {
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
        static buildDiagnostics(platformConfig, prfResult, keyDerivationMethod) {
            return {
                userAgent: navigator.userAgent,
                platform: navigator.platform,
                prfSupported: prfResult?.enabled || false,
                prfResultsAvailable: !!(prfResult?.results?.first),
                authenticatorType: this.getAuthenticatorType(keyDerivationMethod, platformConfig, prfResult),
                keyDerivationMethod: keyDerivationMethod,
                credentialIdLength: 0 // Will be set by caller with actual credential
            };
        }
        /**
         * Get basic diagnostics when detailed info isn't available
         */
        static getBasicDiagnostics() {
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
        static getAuthenticatorType(keyDerivationMethod, platformConfig, prfResult) {
            if (keyDerivationMethod === "PRF") {
                if (platformConfig.isSamsung)
                    return "Samsung Pass (PRF-enabled)";
                if (platformConfig.isIOS)
                    return "Face ID/Touch ID (PRF-enabled)";
                if (platformConfig.isWindows)
                    return "Windows Hello (PRF-enabled)";
                return "Platform Authenticator (PRF-enabled)";
            }
            else {
                // Even if PRF didn't work, some platforms might still support it (from working code)
                if (platformConfig.isSamsung && prfResult?.enabled)
                    return "Samsung Pass (PRF detection issue)";
                if (platformConfig.isSamsung)
                    return "Samsung Pass (PIN fallback)";
                if (platformConfig.isIOS)
                    return "Face ID/Touch ID (fallback mode)";
                if (platformConfig.isWindows)
                    return "Windows Hello (fallback mode)";
                return "Platform Authenticator (credential ID fallback)";
            }
        }
    }
    // Attach to window for browser usage
    if (typeof window !== 'undefined') {
        window.SimpleWebAuthn = SimpleWebAuthn;
    }

    exports.SimpleWebAuthn = SimpleWebAuthn;

    return exports;

})({});
//# sourceMappingURL=simple-webauthn.js.map
