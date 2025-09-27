# WebAuthn PRF Troubleshooting Guide

## Current Issue Analysis

### Samsung Galaxy S24 Ultra - Incorrect PRF Detection

**Symptoms:**
- Platform: `Linux armv81` 
- Method: `CredentialID (INCORRECT)`
- Authenticator: `Basic platform authenticator` (should be PRF-capable)
- PRF Supported: `No (INCORRECT)`
- Samsung Pass with biometrics should support PRF

### Root Cause Analysis

The issue appears to be in our PRF detection logic and possibly missing required WebAuthn options for high-security authenticators.

## Samsung Pass Specific Requirements

### 1. Correct Platform Detection
Samsung Pass requires specific detection patterns:

```javascript
function detectSamsungPass() {
    const userAgent = navigator.userAgent;
    const isSamsung = /SM-[A-Z]\d+/.test(userAgent) || // Model numbers like SM-S928B
                     /Samsung/.test(userAgent) ||
                     /SAMSUNG/.test(userAgent);
    const isAndroid = /Android/.test(userAgent);
    
    console.log("Samsung Detection:", { userAgent, isSamsung, isAndroid });
    return isSamsung && isAndroid;
}
```

### 2. Required WebAuthn Options for Samsung Pass
Samsung Pass (and other high-security platform authenticators) require specific options:

```javascript
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
        { alg: -7, type: "public-key" },   // ES256 (preferred by Samsung)
        { alg: -257, type: "public-key" }  // RS256
    ],
    authenticatorSelection: {
        authenticatorAttachment: "platform",    // Must be platform
        userVerification: "required",           // Must be required for PRF
        residentKey: "required",               // May be required for Samsung Pass
        requireResidentKey: true               // Legacy fallback
    },
    attestation: "none", // Samsung Pass works better with "none"
    extensions: {
        prf: {
            eval: {
                first: new TextEncoder().encode("clypse-prf-salt-v1")
            }
        }
    },
    timeout: 300000 // 5 minutes - Samsung Pass can be slow
};
```

### 3. Samsung Pass Authentication Options

```javascript
const getOptions = {
    challenge: getChallenge,
    allowCredentials: [{
        type: "public-key",
        id: credentialIdBytes,
        transports: ["internal"] // Samsung Pass uses internal transport
    }],
    userVerification: "required", // Critical for PRF
    extensions: {
        prf: {
            eval: {
                first: new TextEncoder().encode("clypse-prf-salt-v1")
            }
        }
    },
    timeout: 300000 // Samsung Pass can be slow to respond
};
```

## iPad Face ID Specific Requirements

### 1. iOS/iPadOS Detection
```javascript
function detectApplePlatform() {
    const userAgent = navigator.userAgent;
    const isIOS = /iPad|iPhone|iPod/.test(userAgent);
    const isSafari = /Safari/.test(userAgent) && !/Chrome|CriOS|FxiOS/.test(userAgent);
    
    // iPadOS 13+ reports as macOS, need to check for touch support
    const isPadOS = navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1;
    
    return (isIOS || isPadOS) && isSafari;
}
```

### 2. Face ID/Touch ID Requirements
```javascript
const credentialCreationOptions = {
    // ... standard options
    authenticatorSelection: {
        authenticatorAttachment: "platform",
        userVerification: "required", // Face ID/Touch ID requires this
        residentKey: "preferred"      // Works better than "required" on iOS
    },
    extensions: {
        prf: {
            eval: {
                first: new TextEncoder().encode("clypse-prf-salt-v1")
            }
        }
    }
};
```

## Enhanced PRF Detection Logic

### Current Issue in Our Code
The problem is likely in this section:

```javascript
// CURRENT (INCORRECT) - During authentication
if (prfResult && prfResult.enabled && prfResult.results && prfResult.results.first) {
    // This is wrong - 'enabled' property is NOT present during authentication
}
```

### Correct PRF Detection

```javascript
// Registration Phase - Check if PRF is enabled
if (prfResult && prfResult.enabled) {
    console.log("✅ PRF is supported on this authenticator");
    
    if (prfResult.results && prfResult.results.first) {
        // PRF results available immediately (rare)
        keyMaterial = await crypto.subtle.importKey("raw", prfResult.results.first, "HKDF", false, ["deriveKey"]);
        keyDerivationMethod = "PRF";
    } else {
        // Need to perform get() to obtain PRF results (common)
        console.log("PRF enabled, performing get() to obtain results...");
        // ... perform get() operation
    }
} else {
    console.log("❌ PRF not supported on this authenticator");
    // Fall back to credential ID
}
```

```javascript
// Authentication Phase - Only check for PRF results (NO 'enabled' property)
const prfResult = credential.getClientExtensionResults().prf;
if (prfResult && prfResult.results && prfResult.results.first) {
    console.log("✅ PRF results available");
    keyMaterial = await crypto.subtle.importKey("raw", prfResult.results.first, "HKDF", false, ["deriveKey"]);
    keyDerivationMethod = "PRF";
} else {
    console.log("❌ No PRF results - falling back to credential ID");
    keyMaterial = await crypto.subtle.importKey("raw", credential.rawId, "HKDF", false, ["deriveKey"]);
    keyDerivationMethod = "CredentialID";
}
```

## Debugging Steps for Samsung Pass

### 1. Enhanced Logging
Add this comprehensive logging to identify the exact issue:

```javascript
console.log("=== Enhanced Samsung Pass Diagnostics ===");
console.log("User Agent:", navigator.userAgent);
console.log("Platform:", navigator.platform);
console.log("Hardware Concurrency:", navigator.hardwareConcurrency);
console.log("Max Touch Points:", navigator.maxTouchPoints);

// Samsung-specific checks
const isSamsungDevice = /SM-[A-Z]\d+/.test(navigator.userAgent);
const hasSamsungBrand = /Samsung/i.test(navigator.userAgent);
const isAndroid = /Android/.test(navigator.userAgent);

console.log("Samsung Device Pattern Match:", isSamsungDevice);
console.log("Samsung Brand Match:", hasSamsungBrand);
console.log("Android Platform:", isAndroid);
console.log("Likely Samsung Pass:", isSamsungDevice && isAndroid);

// Check WebAuthn capabilities
if (window.PublicKeyCredential) {
    console.log("WebAuthn supported");
    
    if (window.PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable) {
        const available = await window.PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
        console.log("Platform authenticator available:", available);
    }
    
    if (window.PublicKeyCredential.getClientCapabilities) {
        try {
            const capabilities = await window.PublicKeyCredential.getClientCapabilities();
            console.log("WebAuthn Client Capabilities:", capabilities);
        } catch (e) {
            console.log("getClientCapabilities failed:", e);
        }
    }
} else {
    console.log("❌ WebAuthn not supported");
}
```

### 2. Test PRF Extension Separately
Create a minimal test to verify PRF support:

```javascript
async function testPRFSupport() {
    try {
        const challenge = crypto.getRandomValues(new Uint8Array(32));
        const userId = crypto.getRandomValues(new Uint8Array(32));
        
        const options = {
            challenge: challenge,
            rp: { name: "PRF Test", id: window.location.hostname },
            user: { id: userId, name: "test", displayName: "PRF Test" },
            pubKeyCredParams: [{ alg: -7, type: "public-key" }],
            authenticatorSelection: {
                authenticatorAttachment: "platform",
                userVerification: "required",
                residentKey: "required"
            },
            extensions: {
                prf: {
                    eval: {
                        first: new TextEncoder().encode("test-salt")
                    }
                }
            }
        };
        
        console.log("Testing PRF with options:", options);
        const credential = await navigator.credentials.create({ publicKey: options });
        
        const extensionResults = credential.getClientExtensionResults();
        console.log("PRF Test Results:", extensionResults);
        
        return {
            supported: !!extensionResults.prf?.enabled,
            results: extensionResults
        };
    } catch (error) {
        console.error("PRF test failed:", error);
        return { supported: false, error: error.message };
    }
}
```

## Platform-Specific Fixes Needed

### 1. Samsung Pass Fix
- ✅ Add `residentKey: "required"`
- ✅ Add `transports: ["internal"]` to allowCredentials
- ✅ Increase timeout to 300000ms
- ✅ Set `attestation: "none"`
- ✅ Ensure `userVerification: "required"`

### 2. iPad Face ID Fix  
- ✅ Use `residentKey: "preferred"` instead of "required"
- ✅ Ensure Safari compatibility
- ✅ Handle iPadOS 13+ detection properly

### 3. General PRF Fix
- ✅ Remove `prfResult.enabled` check during authentication
- ✅ Only check for `prfResult.results.first` during authentication
- ✅ Add proper error handling for PRF timeouts

## Expected Results After Fix

### Samsung Galaxy S24 Ultra (Expected)
- Platform: `Linux armv81` ✅ (correct)
- Method: `PRF` ✅ (fixed)
- Authenticator: `PRF-capable biometric` ✅ (fixed)  
- PRF Supported: `Yes` ✅ (fixed)
- CredentialID: 32 bytes ✅ (correct)

### iPad Pro Face ID (Expected)
- Platform: `MacIntel` or `iPad` ✅
- Method: `PRF` ✅
- Authenticator: `PRF-capable biometric` ✅
- PRF Supported: `Yes` ✅

## Testing Checklist

- [ ] Test Samsung Pass with biometric unlock
- [ ] Test Samsung Pass with PIN unlock (should fail PRF)
- [ ] Test iPad Face ID
- [ ] Test iPad Touch ID
- [ ] Test Windows Hello (biometric)
- [ ] Test Windows Hello (PIN - should fail PRF)
- [ ] Verify PRF salt consistency between registration and authentication
- [ ] Test credential persistence across browser sessions
- [ ] Test timeout handling for slow authenticators

## References

- [Samsung Pass WebAuthn Implementation Details](https://developer.samsung.com/galaxy-store/samsung-pass)
- [Apple WebAuthn Face ID Documentation](https://developer.apple.com/documentation/authenticationservices/implementing_webauthn_with_face_id_or_touch_id)
- [WebAuthn PRF Extension Specification](https://w3c.github.io/webauthn/#prf-extension)