# WebAuthn PRF Platform-Specific Examples

## Samsung Pass Implementation Example

### Complete Samsung Pass PRF Implementation

```javascript
// Samsung Pass specific WebAuthn PRF implementation
class SamsungPassPRF {
    
    // Detect if Samsung Pass is likely available
    static detectSamsungPass() {
        const userAgent = navigator.userAgent;
        const isSamsungDevice = /SM-[A-Z]\d+/.test(userAgent) || // Model pattern like SM-S928B
                               /Samsung/i.test(userAgent) ||
                               /SAMSUNG/i.test(userAgent);
        const isAndroid = /Android/.test(userAgent);
        
        return {
            detected: isSamsungDevice && isAndroid,
            deviceModel: userAgent.match(/SM-[A-Z]\d+/)?.[0] || 'Unknown Samsung',
            androidVersion: userAgent.match(/Android (\d+(?:\.\d+)?)/)?.[1] || 'Unknown',
            chromeVersion: userAgent.match(/Chrome\/(\d+)/)?.[1] || 'Unknown'
        };
    }
    
    // Create credential with Samsung Pass optimized settings
    static async createCredential() {
        const detection = this.detectSamsungPass();
        console.log("Samsung Pass Detection:", detection);
        
        if (!detection.detected) {
            throw new Error("Samsung Pass not detected on this device");
        }
        
        const challenge = crypto.getRandomValues(new Uint8Array(32));
        const userId = crypto.getRandomValues(new Uint8Array(32));
        
        const createOptions = {
            challenge: challenge,
            rp: {
                name: "Clypse Portal",
                id: window.location.hostname
            },
            user: {
                id: userId,
                name: "clypse-samsung-pass-key",
                displayName: "Clypse Samsung Pass Key"
            },
            pubKeyCredParams: [
                { alg: -7, type: "public-key" }   // ES256 - Samsung Pass preferred
            ],
            authenticatorSelection: {
                authenticatorAttachment: "platform",     // Must be platform
                userVerification: "required",            // Required for PRF + biometrics
                residentKey: "required",                 // Samsung Pass requires resident key
                requireResidentKey: true                 // Legacy fallback
            },
            attestation: "none",        // Samsung Pass works better with "none"
            timeout: 300000,           // 5 minutes - Samsung Pass can be slow
            extensions: {
                prf: {
                    eval: {
                        first: new TextEncoder().encode("clypse-samsung-prf-salt-v1")
                    }
                }
            }
        };
        
        console.log("Creating Samsung Pass credential with options:", createOptions);
        
        try {
            const credential = await navigator.credentials.create({
                publicKey: createOptions
            });
            
            console.log("Samsung Pass credential created:", credential);
            
            // Check PRF extension results
            const extensionResults = credential.getClientExtensionResults();
            console.log("Samsung Pass PRF Results:", extensionResults.prf);
            
            if (extensionResults.prf && extensionResults.prf.enabled) {
                console.log("✅ Samsung Pass PRF enabled successfully!");
                return {
                    credential: credential,
                    prfEnabled: true,
                    prfResults: extensionResults.prf.results || null
                };
            } else {
                console.log("❌ Samsung Pass PRF not enabled");
                return {
                    credential: credential,
                    prfEnabled: false,
                    prfResults: null
                };
            }
        } catch (error) {
            console.error("Samsung Pass credential creation failed:", error);
            throw error;
        }
    }
    
    // Authenticate with Samsung Pass
    static async authenticate(credentialId) {
        const getOptions = {
            challenge: crypto.getRandomValues(new Uint8Array(32)),
            allowCredentials: [{
                type: "public-key",
                id: credentialId,
                transports: ["internal"]  // Samsung Pass uses internal transport
            }],
            userVerification: "required", // Required for PRF
            timeout: 300000,             // Samsung Pass can be slow
            extensions: {
                prf: {
                    eval: {
                        first: new TextEncoder().encode("clypse-samsung-prf-salt-v1")
                    }
                }
            }
        };
        
        try {
            const credential = await navigator.credentials.get({
                publicKey: getOptions
            });
            
            console.log("Samsung Pass authentication successful:", credential);
            
            // Check PRF results (note: no 'enabled' property during authentication)
            const extensionResults = credential.getClientExtensionResults();
            const prfResult = extensionResults.prf;
            
            console.log("Samsung Pass Authentication PRF Results:", prfResult);
            
            if (prfResult && prfResult.results && prfResult.results.first) {
                console.log("✅ Samsung Pass PRF results available!");
                console.log("PRF Output Length:", prfResult.results.first.byteLength, "bytes");
                
                return {
                    credential: credential,
                    prfOutput: prfResult.results.first,
                    method: "PRF"
                };
            } else {
                console.log("❌ Samsung Pass PRF results not available, using credential ID");
                return {
                    credential: credential,
                    prfOutput: null,
                    method: "CredentialID"
                };
            }
        } catch (error) {
            console.error("Samsung Pass authentication failed:", error);
            throw error;
        }
    }
}
```

## iPad Face ID Implementation Example

### Complete iPad Face ID PRF Implementation

```javascript
// iPad Face ID specific WebAuthn PRF implementation
class iPadFaceIDPRF {
    
    // Detect if Face ID/Touch ID is available on iPad
    static detectiPadAuthenticator() {
        const userAgent = navigator.userAgent;
        const isIPad = /iPad/.test(userAgent) || 
                      (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
        const isSafari = /Safari/.test(userAgent) && !/Chrome|CriOS|FxiOS/.test(userAgent);
        
        // Detect iPad model and capabilities
        let deviceInfo = {
            detected: isIPad && isSafari,
            model: 'iPad',
            hasFaceID: false,
            hasTouchID: false,
            safariVersion: userAgent.match(/Version\/(\d+(?:\.\d+)?)/)?.[1] || 'Unknown'
        };
        
        // iPad Pro models typically have Face ID
        if (/iPad.*OS 1[3-9]/.test(userAgent)) {
            deviceInfo.hasFaceID = true; // Likely Face ID capable
            deviceInfo.model = 'iPad Pro (Face ID)';
        }
        
        return deviceInfo;
    }
    
    // Create credential with iPad Face ID optimized settings
    static async createCredential() {
        const detection = this.detectiPadAuthenticator();
        console.log("iPad Face ID Detection:", detection);
        
        if (!detection.detected) {
            throw new Error("iPad Face ID/Touch ID not detected");
        }
        
        const challenge = crypto.getRandomValues(new Uint8Array(32));
        const userId = crypto.getRandomValues(new Uint8Array(32));
        
        const createOptions = {
            challenge: challenge,
            rp: {
                name: "Clypse Portal",
                id: window.location.hostname
            },
            user: {
                id: userId,
                name: "clypse-ipad-faceid-key",
                displayName: "Clypse iPad Face ID Key"
            },
            pubKeyCredParams: [
                { alg: -7, type: "public-key" },   // ES256
                { alg: -257, type: "public-key" }  // RS256
            ],
            authenticatorSelection: {
                authenticatorAttachment: "platform",
                userVerification: "required",       // Face ID/Touch ID requires this
                residentKey: "preferred"           // "preferred" works better than "required" on iOS
            },
            timeout: 120000,    // 2 minutes - iPad is usually faster than Samsung
            extensions: {
                prf: {
                    eval: {
                        first: new TextEncoder().encode("clypse-ipad-prf-salt-v1")
                    }
                }
            }
        };
        
        console.log("Creating iPad Face ID credential with options:", createOptions);
        
        try {
            const credential = await navigator.credentials.create({
                publicKey: createOptions
            });
            
            console.log("iPad Face ID credential created:", credential);
            
            // Check PRF extension results
            const extensionResults = credential.getClientExtensionResults();
            console.log("iPad Face ID PRF Results:", extensionResults.prf);
            
            if (extensionResults.prf && extensionResults.prf.enabled) {
                console.log("✅ iPad Face ID PRF enabled successfully!");
                return {
                    credential: credential,
                    prfEnabled: true,
                    prfResults: extensionResults.prf.results || null,
                    authenticatorType: detection.hasFaceID ? "Face ID" : "Touch ID"
                };
            } else {
                console.log("❌ iPad Face ID PRF not enabled");
                return {
                    credential: credential,
                    prfEnabled: false,
                    prfResults: null,
                    authenticatorType: detection.hasFaceID ? "Face ID" : "Touch ID"
                };
            }
        } catch (error) {
            console.error("iPad Face ID credential creation failed:", error);
            throw error;
        }
    }
    
    // Authenticate with iPad Face ID
    static async authenticate(credentialId) {
        const getOptions = {
            challenge: crypto.getRandomValues(new Uint8Array(32)),
            allowCredentials: [{
                type: "public-key",
                id: credentialId
                // Note: No specific transports needed for iPad
            }],
            userVerification: "required", // Required for Face ID/Touch ID PRF
            timeout: 120000,             // iPad is usually fast
            extensions: {
                prf: {
                    eval: {
                        first: new TextEncoder().encode("clypse-ipad-prf-salt-v1")
                    }
                }
            }
        };
        
        try {
            const credential = await navigator.credentials.get({
                publicKey: getOptions
            });
            
            console.log("iPad Face ID authentication successful:", credential);
            
            // Check PRF results
            const extensionResults = credential.getClientExtensionResults();
            const prfResult = extensionResults.prf;
            
            console.log("iPad Face ID Authentication PRF Results:", prfResult);
            
            if (prfResult && prfResult.results && prfResult.results.first) {
                console.log("✅ iPad Face ID PRF results available!");
                console.log("PRF Output Length:", prfResult.results.first.byteLength, "bytes");
                
                return {
                    credential: credential,
                    prfOutput: prfResult.results.first,
                    method: "PRF"
                };
            } else {
                console.log("❌ iPad Face ID PRF results not available, using credential ID");
                return {
                    credential: credential,
                    prfOutput: null,
                    method: "CredentialID"
                };
            }
        } catch (error) {
            console.error("iPad Face ID authentication failed:", error);
            throw error;
        }
    }
}
```

## Universal Platform Detection and PRF Implementation

### Comprehensive Platform-Aware PRF Implementation

```javascript
// Universal WebAuthn PRF implementation that adapts to different platforms
class UniversalWebAuthnPRF {
    
    // Detect platform and authenticator capabilities
    static detectPlatform() {
        const userAgent = navigator.userAgent;
        
        // Samsung detection
        const isSamsung = /SM-[A-Z]\d+/.test(userAgent) || /Samsung/i.test(userAgent);
        const isAndroid = /Android/.test(userAgent);
        const samsungPass = isSamsung && isAndroid;
        
        // Apple detection  
        const isIPad = /iPad/.test(userAgent) || 
                      (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
        const isIPhone = /iPhone/.test(userAgent);
        const isSafari = /Safari/.test(userAgent) && !/Chrome|CriOS|FxiOS/.test(userAgent);
        const appleDevice = (isIPad || isIPhone) && isSafari;
        
        // Windows detection
        const isWindows = /Windows/.test(userAgent) || navigator.platform.startsWith('Win');
        const windowsHello = isWindows;
        
        return {
            samsungPass,
            appleDevice,
            windowsHello,
            platform: samsungPass ? 'Samsung Pass' : 
                     appleDevice ? (isIPad ? 'iPad' : 'iPhone') : 
                     windowsHello ? 'Windows Hello' : 'Unknown',
            userAgent,
            details: {
                isSamsung, isAndroid, isIPad, isIPhone, isSafari, isWindows
            }
        };
    }
    
    // Get platform-optimized credential creation options
    static getCreateOptions(platform) {
        const base = {
            challenge: crypto.getRandomValues(new Uint8Array(32)),
            rp: {
                name: "Clypse Portal",
                id: window.location.hostname
            },
            user: {
                id: crypto.getRandomValues(new Uint8Array(32)),
                name: "clypse-encryption-key",
                displayName: "Clypse Encryption Key"
            },
            pubKeyCredParams: [
                { alg: -7, type: "public-key" },   // ES256
                { alg: -257, type: "public-key" }  // RS256
            ],
            extensions: {
                prf: {
                    eval: {
                        first: new TextEncoder().encode("clypse-prf-salt-v1")
                    }
                }
            }
        };
        
        // Platform-specific optimizations
        if (platform.samsungPass) {
            return {
                ...base,
                authenticatorSelection: {
                    authenticatorAttachment: "platform",
                    userVerification: "required",
                    residentKey: "required",         // Samsung Pass requires this
                    requireResidentKey: true
                },
                attestation: "none",                 // Samsung Pass preference
                timeout: 300000                      // Samsung Pass can be slow
            };
        } else if (platform.appleDevice) {
            return {
                ...base,
                authenticatorSelection: {
                    authenticatorAttachment: "platform",
                    userVerification: "required",
                    residentKey: "preferred"         // Works better than "required" on iOS
                },
                timeout: 120000                      // iPad/iPhone usually fast
            };
        } else if (platform.windowsHello) {
            return {
                ...base,
                authenticatorSelection: {
                    authenticatorAttachment: "platform",
                    userVerification: "required",
                    residentKey: "preferred"
                },
                timeout: 180000                      // Windows Hello moderate speed
            };
        } else {
            // Generic/unknown platform
            return {
                ...base,
                authenticatorSelection: {
                    authenticatorAttachment: "platform",
                    userVerification: "required",
                    residentKey: "preferred"
                },
                timeout: 300000                      // Conservative timeout
            };
        }
    }
    
    // Get platform-optimized authentication options
    static getAuthOptions(platform, credentialId) {
        const base = {
            challenge: crypto.getRandomValues(new Uint8Array(32)),
            allowCredentials: [{
                type: "public-key",
                id: credentialId
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
        
        // Platform-specific optimizations
        if (platform.samsungPass) {
            base.allowCredentials[0].transports = ["internal"];
            base.timeout = 300000;
        } else if (platform.appleDevice) {
            base.timeout = 120000;
        } else if (platform.windowsHello) {
            base.timeout = 180000;
        } else {
            base.timeout = 300000;
        }
        
        return base;
    }
    
    // Universal encrypt function
    static async encrypt(plaintext) {
        const platform = this.detectPlatform();
        console.log("Detected platform:", platform);
        
        try {
            // Check WebAuthn support
            if (!window.PublicKeyCredential) {
                throw new Error("WebAuthn not supported");
            }
            
            const available = await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
            if (!available) {
                throw new Error("Platform authenticator not available");
            }
            
            // Try to use existing credential first
            let storedCredentialId = localStorage.getItem('clypse_webauthn_credential_id');
            let credential = null;
            let prfOutput = null;
            
            if (storedCredentialId) {
                try {
                    console.log("Attempting authentication with existing credential...");
                    const credentialIdBytes = Uint8Array.from(atob(storedCredentialId), c => c.charCodeAt(0));
                    const authOptions = this.getAuthOptions(platform, credentialIdBytes);
                    
                    credential = await navigator.credentials.get({ publicKey: authOptions });
                    
                    const prfResult = credential.getClientExtensionResults().prf;
                    if (prfResult && prfResult.results && prfResult.results.first) {
                        prfOutput = prfResult.results.first;
                        console.log("✅ PRF authentication successful!");
                    }
                } catch (error) {
                    console.log("Failed to authenticate with existing credential:", error.message);
                    credential = null;
                }
            }
            
            // Create new credential if needed
            if (!credential) {
                console.log("Creating new credential...");
                const createOptions = this.getCreateOptions(platform);
                
                credential = await navigator.credentials.create({ publicKey: createOptions });
                
                // Store credential ID
                const credentialIdBase64 = btoa(String.fromCharCode(...new Uint8Array(credential.rawId)));
                localStorage.setItem('clypse_webauthn_credential_id', credentialIdBase64);
                
                // Check PRF results
                const prfResult = credential.getClientExtensionResults().prf;
                if (prfResult && prfResult.enabled) {
                    if (prfResult.results && prfResult.results.first) {
                        prfOutput = prfResult.results.first;
                        console.log("✅ PRF creation successful!");
                    } else {
                        // Need to do get() operation for PRF results
                        console.log("PRF enabled, performing get() for results...");
                        try {
                            const authOptions = this.getAuthOptions(platform, credential.rawId);
                            const getCredential = await navigator.credentials.get({ publicKey: authOptions });
                            
                            const getPrfResult = getCredential.getClientExtensionResults().prf;
                            if (getPrfResult && getPrfResult.results && getPrfResult.results.first) {
                                prfOutput = getPrfResult.results.first;
                                console.log("✅ PRF get() operation successful!");
                            }
                        } catch (getError) {
                            console.log("PRF get() operation failed:", getError.message);
                        }
                    }
                }
            }
            
            // Derive encryption key
            let keyMaterial;
            let keyMethod;
            
            if (prfOutput) {
                keyMaterial = await crypto.subtle.importKey("raw", prfOutput, "HKDF", false, ["deriveKey"]);
                keyMethod = "PRF";
                console.log("Using PRF-derived key material");
            } else {
                keyMaterial = await crypto.subtle.importKey("raw", credential.rawId, "HKDF", false, ["deriveKey"]);
                keyMethod = "CredentialID";
                console.log("Using Credential ID for key derivation");
            }
            
            // Derive AES key and encrypt
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
            
            const iv = crypto.getRandomValues(new Uint8Array(12));
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
            
            const base64 = btoa(String.fromCharCode(...combined));
            
            return {
                success: true,
                encryptedDataBase64: base64,
                keyDerivationMethod: keyMethod,
                platform: platform.platform,
                prfSupported: keyMethod === "PRF"
            };
            
        } catch (error) {
            console.error("Encryption failed:", error);
            return {
                success: false,
                error: error.message,
                platform: platform.platform
            };
        }
    }
}
```

## Testing Your Implementation

### Simple Test Function

```javascript
// Test function to validate PRF support on your devices
async function testPRFOnDevice() {
    console.log("=== PRF Support Test ===");
    
    const platform = UniversalWebAuthnPRF.detectPlatform();
    console.log("Platform Detection:", platform);
    
    // Expected results for your devices:
    // Samsung Galaxy S24 Ultra: platform.samsungPass should be true
    // iPad: platform.appleDevice should be true
    
    try {
        const result = await UniversalWebAuthnPRF.encrypt("test data");
        console.log("Encryption Result:", result);
        
        if (result.success && result.prfSupported) {
            console.log("✅ PRF is working correctly!");
            console.log("Platform:", result.platform);
            console.log("Key Method:", result.keyDerivationMethod);
        } else {
            console.log("❌ PRF not working, using fallback");
            console.log("Platform:", result.platform);
            console.log("Key Method:", result.keyDerivationMethod);
        }
    } catch (error) {
        console.error("Test failed:", error);
    }
}
```

Call `testPRFOnDevice()` in your browser console on both devices to see if PRF is properly detected and working.