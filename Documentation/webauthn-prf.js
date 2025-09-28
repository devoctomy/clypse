// WebAuthn Hybrid PRF/Credential ID implementation
window.WebAuthnPrf = {
    // Encrypt data using WebAuthn (PRF if supported, credential ID fallback)
    encrypt: async function(plaintext) {
        try {
            // DIAGNOSTIC LOGGING - Check WebAuthn capabilities
            console.log("=== WebAuthn Diagnostic Information ===");
            console.log("User Agent:", navigator.userAgent);
            console.log("Platform:", navigator.platform);
            console.log("WebAuthn Support:", !!window.PublicKeyCredential);
            console.log("isUserVerifyingPlatformAuthenticatorAvailable:", !!window.PublicKeyCredential?.isUserVerifyingPlatformAuthenticatorAvailable);
            
            // Check for PRF extension support
            if (window.PublicKeyCredential && window.PublicKeyCredential.getClientCapabilities) {
                try {
                    const capabilities = await window.PublicKeyCredential.getClientCapabilities();
                    console.log("Client Capabilities:", capabilities);
                } catch (e) {
                    console.log("getClientCapabilities not supported or failed:", e);
                }
            } else {
                console.log("getClientCapabilities not available");
            }
            
            // Check if WebAuthn is supported
            if (!window.PublicKeyCredential || !window.PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable) {
                return {
                    success: false,
                    error: "WebAuthn is not supported on this device"
                };
            }

            // Check if platform authenticator is available
            const available = await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
            console.log("Platform authenticator available:", available);
            if (!available) {
                return {
                    success: false,
                    error: "Platform authenticator (Windows Hello/TouchID/etc.) is not available"
                };
            }

            // Check if we have an existing credential ID stored in localStorage
            let storedCredentialId = localStorage.getItem('clypse_webauthn_credential_id');
            let credential = null;
            let keyMaterial = null;
            let keyDerivationMethod = "unknown";
            let finalPrfResult = null; // Track PRF results for diagnostics

            if (storedCredentialId) {
                // Try to use existing credential
                console.log("Found existing credential ID, attempting authentication...");
                try {
                    const getChallenge = crypto.getRandomValues(new Uint8Array(32));
                    const credentialIdBytes = Uint8Array.from(atob(storedCredentialId), c => c.charCodeAt(0));
                    
                    const getOptions = {
                        challenge: getChallenge,
                        allowCredentials: [{
                            type: "public-key",
                            id: credentialIdBytes
                        }],
                        userVerification: "required",
                        extensions: {
                            prf: {
                                eval: {
                                    first: new TextEncoder().encode("clypse-prf-salt-v1")
                                }
                            }
                        }
                    };

                    credential = await navigator.credentials.get({
                        publicKey: getOptions
                    });
                    console.log("Authentication with existing credential successful:", credential);

                    // Try PRF first
                    const prfResult = credential.getClientExtensionResults().prf;
                    console.log("=== PRF Extension Analysis (Existing Credential) ===");
                    console.log("PRF Result Object:", prfResult);
                    console.log("PRF Enabled:", prfResult?.enabled);
                    console.log("PRF Results Available:", !!prfResult?.results);
                    console.log("PRF First Result:", prfResult?.results?.first ? `${prfResult.results.first.byteLength} bytes` : "none");
                    
                    finalPrfResult = prfResult; // Store for diagnostics
                    if (prfResult && prfResult.enabled && prfResult.results && prfResult.results.first) {
                        console.log("✅ PRF extension successful - using PRF key material");
                        console.log("PRF Output Length:", prfResult.results.first.byteLength, "bytes");
                        keyMaterial = await crypto.subtle.importKey(
                            "raw",
                            prfResult.results.first,
                            "HKDF",
                            false,
                            ["deriveKey"]
                        );
                        keyDerivationMethod = "PRF";
                    } else {
                        console.log("❌ PRF not available - falling back to credential ID derivation");
                        keyMaterial = await crypto.subtle.importKey(
                            "raw",
                            credential.rawId,
                            "HKDF",
                            false,
                            ["deriveKey"]
                        );
                        keyDerivationMethod = "CredentialID";
                    }
                } catch (error) {
                    console.log("Failed to authenticate with existing credential:", error);
                    // Continue to create new credential
                }
            }

            // If we don't have a valid credential yet, create a new one
            if (!credential || !keyMaterial) {
                console.log("Creating new WebAuthn credential...");
                
                // Create a new credential
                const challenge = crypto.getRandomValues(new Uint8Array(32));
                const userId = crypto.getRandomValues(new Uint8Array(32));
                
                const credentialCreationOptions = {
                    challenge: challenge,
                    rp: {
                        name: "Clypse Portal",
                        id: window.location.hostname
                    },
                    user: {
                        id: userId,
                        name: "clypse-encryption-key",
                        displayName: "Clypse Encryption Key"
                    },
                    pubKeyCredParams: [
                        { alg: -7, type: "public-key" }, // ES256
                        { alg: -257, type: "public-key" } // RS256
                    ],
                    authenticatorSelection: {
                        authenticatorAttachment: "platform",
                        userVerification: "required",
                        requireResidentKey: true // Make it discoverable
                    },
                    extensions: {
                        prf: {
                            eval: {
                                first: new TextEncoder().encode("clypse-prf-salt-v1")
                            }
                        }
                    }
                };

                credential = await navigator.credentials.create({
                    publicKey: credentialCreationOptions
                });
                console.log("New credential created successfully:", credential);

                // Store the credential ID for future use
                const credentialIdBase64 = btoa(String.fromCharCode(...new Uint8Array(credential.rawId)));
                localStorage.setItem('clypse_webauthn_credential_id', credentialIdBase64);
                console.log("Stored new credential ID in localStorage");

                // Try PRF first
                const prfResult = credential.getClientExtensionResults().prf;
                finalPrfResult = prfResult; // Store for diagnostics
                console.log("=== PRF Extension Analysis (Creation) ===");
                console.log("PRF Result Object:", prfResult);
                console.log("PRF Enabled:", prfResult?.enabled);
                console.log("PRF Results Available:", !!prfResult?.results);
                console.log("PRF First Result:", prfResult?.results?.first ? `${prfResult.results.first.byteLength} bytes` : "none");
                
                if (prfResult && prfResult.enabled) {
                    // Try to get PRF results from creation
                    if (prfResult.results && prfResult.results.first) {
                        console.log("✅ Got PRF results directly from credential creation");
                        console.log("PRF Output Length:", prfResult.results.first.byteLength, "bytes");
                        keyMaterial = await crypto.subtle.importKey(
                            "raw",
                            prfResult.results.first,
                            "HKDF",
                            false,
                            ["deriveKey"]
                        );
                        keyDerivationMethod = "PRF";
                    } else {
                        console.log("⚠️ PRF enabled but no results - attempting get() operation...");
                        // Need to do a get() operation to get PRF results
                        try {
                            const getChallenge = crypto.getRandomValues(new Uint8Array(32));
                            const getOptions = {
                                challenge: getChallenge,
                                allowCredentials: [{
                                    type: "public-key",
                                    id: credential.rawId
                                }],
                                userVerification: "required",
                                extensions: {
                                    prf: {
                                        eval: {
                                            first: new TextEncoder().encode("clypse-prf-salt-v1")
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
                            console.log("Get PRF Enabled:", getPrfResult?.enabled);
                            console.log("Get PRF Results Available:", !!getPrfResult?.results);
                            
                            if (getPrfResult && getPrfResult.results && getPrfResult.results.first) {
                                console.log("✅ PRF results obtained from get() operation");
                                console.log("PRF Output Length:", getPrfResult.results.first.byteLength, "bytes");
                                keyMaterial = await crypto.subtle.importKey(
                                    "raw",
                                    getPrfResult.results.first,
                                    "HKDF",
                                    false,
                                    ["deriveKey"]
                                );
                                keyDerivationMethod = "PRF";
                            } else {
                                throw new Error("PRF get() operation failed - no results");
                            }
                        } catch (prfError) {
                            console.log("❌ PRF get() operation failed, falling back to credential ID:", prfError.message);
                            keyMaterial = await crypto.subtle.importKey(
                                "raw",
                                credential.rawId,
                                "HKDF",
                                false,
                                ["deriveKey"]
                            );
                            keyDerivationMethod = "CredentialID";
                        }
                    }
                } else {
                    console.log("❌ PRF not supported - using credential ID for key derivation");
                    console.log("Falling back to Credential ID method");
                    console.log("Credential ID Length:", credential.rawId.byteLength, "bytes");
                    keyMaterial = await crypto.subtle.importKey(
                        "raw",
                        credential.rawId,
                        "HKDF",
                        false,
                        ["deriveKey"]
                    );
                    keyDerivationMethod = "CredentialID";
                }
            }
            
            // Derive AES-GCM key from the key material (either PRF output or credential ID)
            const aesKey = await crypto.subtle.deriveKey(
                {
                    name: "HKDF",
                    hash: "SHA-256",
                    salt: new TextEncoder().encode("clypse-encryption-salt-v1"),
                    info: new TextEncoder().encode("clypse-encryption-key")
                },
                keyMaterial,
                { name: "AES-GCM", length: 256 },
                false,
                ["encrypt"]
            );

            // Generate random IV
            const iv = crypto.getRandomValues(new Uint8Array(12));
            
            // Encrypt the plaintext
            const plaintextBytes = new TextEncoder().encode(plaintext);
            const encryptedData = await crypto.subtle.encrypt(
                { name: "AES-GCM", iv: iv },
                aesKey,
                plaintextBytes
            );

            // Combine IV and encrypted data
            const combined = new Uint8Array(iv.length + encryptedData.byteLength);
            combined.set(iv, 0);
            combined.set(new Uint8Array(encryptedData), iv.length);

            // Convert to base64
            const base64 = btoa(String.fromCharCode(...combined));

            // Log the result to console
            console.log("=== WebAuthn Encryption Summary ===");
            console.log("✅ WebAuthn Encryption Successful!");
            console.log("🔑 Key Derivation Method:", keyDerivationMethod);
            console.log("📝 Plaintext:", plaintext);
            console.log("🔒 Base64 Encrypted Data:", base64);
            console.log("🔧 Authenticator Type:", keyDerivationMethod === "PRF" ? "PRF-capable (biometric)" : "Basic platform authenticator");
            console.log("=======================================");

            return {
                success: true,
                encryptedDataBase64: base64,
                keyDerivationMethod: keyDerivationMethod,
                diagnostics: {
                    userAgent: navigator.userAgent,
                    platform: navigator.platform,
                    prfSupported: finalPrfResult?.enabled || false,
                    prfResultsAvailable: !!(finalPrfResult?.results?.first),
                    credentialIdLength: credential.rawId.byteLength,
                    authenticatorType: keyDerivationMethod === "PRF" ? "PRF-capable biometric" : "Basic platform authenticator"
                }
            };

        } catch (error) {
            console.error("WebAuthn encryption error:", error);
            return {
                success: false,
                error: error.message || "Unknown error occurred"
            };
        }
    },

    // Decrypt data using WebAuthn (PRF if supported, credential ID fallback)
    decrypt: async function(encryptedDataBase64) {
        try {
            // Check if WebAuthn is supported
            if (!window.PublicKeyCredential || !window.PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable) {
                return {
                    success: false,
                    error: "WebAuthn is not supported on this device"
                };
            }

            // Check if we have stored encrypted data
            if (!encryptedDataBase64) {
                encryptedDataBase64 = localStorage.getItem('webauthn_encrypted_data');
                if (!encryptedDataBase64) {
                    return {
                        success: false,
                        error: "No encrypted data found to decrypt"
                    };
                }
            }

            // Check if we have an existing credential ID
            const storedCredentialId = localStorage.getItem('clypse_webauthn_credential_id');
            if (!storedCredentialId) {
                return {
                    success: false,
                    error: "No stored credential found. Please encrypt data first."
                };
            }

            console.log("Starting WebAuthn decryption...");

            // Authenticate with existing credential
            const getChallenge = crypto.getRandomValues(new Uint8Array(32));
            const credentialIdBytes = Uint8Array.from(atob(storedCredentialId), c => c.charCodeAt(0));
            
            const getOptions = {
                challenge: getChallenge,
                allowCredentials: [{
                    type: "public-key",
                    id: credentialIdBytes
                }],
                userVerification: "required",
                extensions: {
                    prf: {
                        eval: {
                            first: new TextEncoder().encode("clypse-prf-salt-v1")
                        }
                    }
                }
            };

            const credential = await navigator.credentials.get({
                publicKey: getOptions
            });
            console.log("Authentication successful:", credential);

            let keyMaterial = null;
            let keyDerivationMethod = "unknown";
            let finalPrfResult = null; // Track PRF results for diagnostics

            // Try PRF first
            const prfResult = credential.getClientExtensionResults().prf;
            finalPrfResult = prfResult; // Store for diagnostics
            if (prfResult && prfResult.enabled && prfResult.results && prfResult.results.first) {
                console.log("PRF extension successful - using PRF key material");
                keyMaterial = await crypto.subtle.importKey(
                    "raw",
                    prfResult.results.first,
                    "HKDF",
                    false,
                    ["deriveKey"]
                );
                keyDerivationMethod = "PRF";
            } else {
                console.log("PRF not available - falling back to credential ID derivation");
                keyMaterial = await crypto.subtle.importKey(
                    "raw",
                    credential.rawId,
                    "HKDF",
                    false,
                    ["deriveKey"]
                );
                keyDerivationMethod = "CredentialID";
            }
            
            // Derive AES-GCM key from the key material (same as encryption)
            const aesKey = await crypto.subtle.deriveKey(
                {
                    name: "HKDF",
                    hash: "SHA-256",
                    salt: new TextEncoder().encode("clypse-encryption-salt-v1"),
                    info: new TextEncoder().encode("clypse-encryption-key")
                },
                keyMaterial,
                { name: "AES-GCM", length: 256 },
                false,
                ["decrypt"]
            );

            // Decode base64
            const encryptedBytes = Uint8Array.from(atob(encryptedDataBase64), c => c.charCodeAt(0));
            
            // Extract IV and encrypted data
            const iv = encryptedBytes.slice(0, 12);
            const encryptedData = encryptedBytes.slice(12);
            
            // Decrypt the data
            const decryptedData = await crypto.subtle.decrypt(
                { name: "AES-GCM", iv: iv },
                aesKey,
                encryptedData
            );

            // Convert decrypted bytes to text
            const plaintext = new TextDecoder().decode(decryptedData);

            console.log("WebAuthn Decryption Successful!");
            console.log("Key Derivation Method:", keyDerivationMethod);
            console.log("Decrypted plaintext:", plaintext);

            return {
                success: true,
                plaintext: plaintext,
                keyDerivationMethod: keyDerivationMethod,
                diagnostics: {
                    userAgent: navigator.userAgent,
                    platform: navigator.platform,
                    prfSupported: finalPrfResult?.enabled || false,
                    prfResultsAvailable: !!(finalPrfResult?.results?.first),
                    credentialIdLength: credential.rawId.byteLength,
                    authenticatorType: keyDerivationMethod === "PRF" ? "PRF-capable biometric" : "Basic platform authenticator"
                }
            };

        } catch (error) {
            console.error("WebAuthn decryption error:", error);
            return {
                success: false,
                error: error.message || "Unknown error occurred"
            };
        }
    }
};