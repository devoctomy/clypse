# WebAuthn PRF (Pseudo-Random Function) Implementation Guide

## Overview

The WebAuthn PRF extension provides a way to derive cryptographic keys from authenticator hardware, enabling secure client-side encryption without storing keys. This is particularly useful for high-security authenticators like:

- **Samsung Pass** (Samsung Galaxy devices)
- **Face ID / Touch ID** (Apple devices)  
- **Windows Hello** (Windows devices)
- **Hardware Security Keys** (YubiKey, etc.)

## Current Implementation Issues

Our current implementation incorrectly assumes all platform authenticators are "basic" when they should support PRF. The issue appears to be in detection logic and possibly extension request format.

### Observed Problems:
- Samsung Pass (S24 Ultra) showing as "Basic platform authenticator" instead of PRF-capable
- iPad Face ID not utilizing PRF despite hardware capability
- All devices falling back to credential ID derivation

## WebAuthn PRF Specification

### Extension Format

#### Registration (create)
```javascript
const createOptions = {
    // ... standard WebAuthn options
    extensions: {
        prf: {
            eval: {
                first: new TextEncoder().encode("application-specific-salt"),
                second: new TextEncoder().encode("optional-second-salt") // Optional
            }
        }
    }
};
```

#### Authentication (get)
```javascript
const getOptions = {
    // ... standard WebAuthn options
    extensions: {
        prf: {
            eval: {
                first: new TextEncoder().encode("same-application-salt"),
                second: new TextEncoder().encode("optional-second-salt")
            }
        }
    }
};
```

### Response Handling

#### Registration Response
```javascript
const result = credential.getClientExtensionResults();
if (result.prf) {
    console.log("PRF enabled:", result.prf.enabled); // boolean
    if (result.prf.results) {
        const key1 = result.prf.results.first; // ArrayBuffer (32 bytes)
        const key2 = result.prf.results.second; // ArrayBuffer (optional)
    }
}
```

#### Authentication Response
```javascript
const result = credential.getClientExtensionResults();
if (result.prf && result.prf.results) {
    // NOTE: 'enabled' property is NOT present during authentication
    const key1 = result.prf.results.first; // ArrayBuffer (32 bytes)
    const key2 = result.prf.results.second; // ArrayBuffer (optional)
}
```

## Platform-Specific Implementation Notes

### Samsung Pass (Android)
Samsung Pass on Galaxy devices supports WebAuthn PRF through the Android WebAuthn API. Key requirements:

1. **User Verification Required**: PRF typically requires `userVerification: "required"`
2. **Resident Key**: May require `residentKey: "preferred"` or `"required"`
3. **Authenticator Attachment**: Should detect as `"platform"`

#### Samsung Pass Detection
```javascript
// Check for Samsung Pass capabilities
const isAndroid = /Android/.test(navigator.userAgent);
const isSamsung = /SM-/.test(navigator.userAgent) || /Samsung/.test(navigator.userAgent);

if (isAndroid && isSamsung) {
    // Likely Samsung Pass - should support PRF
    console.log("Samsung Pass detected - PRF should be supported");
}
```

### Apple Face ID / Touch ID (iOS/iPadOS)
Apple's platform authenticators have strong PRF support:

1. **Face ID**: Available on iPad Pro, iPhone X and later
2. **Touch ID**: Available on various iPhone and iPad models
3. **Requires**: `userVerification: "required"` for PRF access

#### Apple Platform Detection
```javascript
const isApple = /iPad|iPhone|iPod/.test(navigator.userAgent) || 
                (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);

if (isApple) {
    // Apple platform - PRF should be supported with Face ID/Touch ID
    console.log("Apple platform detected - PRF should be supported");
}
```

### Windows Hello
Windows Hello supports PRF through the Windows WebAuthn API:

1. **PIN**: Basic authentication (may not support PRF)
2. **Biometrics**: Fingerprint/Face recognition (should support PRF)
3. **Hardware Keys**: External authenticators

## Correct PRF Implementation Pattern

### 1. Enhanced Extension Request
```javascript
const prfSalt = new TextEncoder().encode("clypse-prf-salt-v1");

// For both create() and get()
const extensions = {
    prf: {
        eval: {
            first: prfSalt
        }
    }
};

// Ensure proper options
const options = {
    // ... other options
    userVerification: "required", // Critical for PRF
    authenticatorSelection: {
        authenticatorAttachment: "platform",
        residentKey: "preferred", // May be required for PRF
        userVerification: "required"
    },
    extensions: extensions
};
```

### 2. Proper Response Validation

#### Registration
```javascript
const extensionResults = credential.getClientExtensionResults();
const prfResult = extensionResults.prf;

if (prfResult) {
    console.log("PRF extension processed");
    
    if (prfResult.enabled) {
        console.log("PRF is enabled on this authenticator");
        
        if (prfResult.results && prfResult.results.first) {
            console.log("PRF results available immediately");
            // Use prfResult.results.first for key derivation
        } else {
            console.log("PRF enabled but results not available - need authentication");
        }
    } else {
        console.log("PRF not enabled on this authenticator");
    }
} else {
    console.log("PRF extension not supported or not processed");
}
```

#### Authentication
```javascript
const extensionResults = credential.getClientExtensionResults();
const prfResult = extensionResults.prf;

if (prfResult && prfResult.results && prfResult.results.first) {
    console.log("PRF results available");
    const prfOutput = prfResult.results.first; // 32-byte ArrayBuffer
    
    // This is your cryptographic key material
    const cryptoKey = await crypto.subtle.importKey(
        "raw",
        prfOutput,
        "HKDF",
        false,
        ["deriveKey"]
    );
} else {
    console.log("PRF results not available");
    // Fall back to credential ID based derivation
}
```

## Debugging PRF Issues

### 1. Check Authenticator Capabilities
```javascript
// Log all extension results
const extensionResults = credential.getClientExtensionResults();
console.log("All extension results:", extensionResults);

// Check if PRF was even attempted
if (!extensionResults.hasOwnProperty('prf')) {
    console.log("PRF extension was not processed at all");
} else {
    console.log("PRF extension was processed:", extensionResults.prf);
}
```

### 2. Validate Request Options
```javascript
console.log("Request options:", {
    userVerification: options.userVerification,
    authenticatorSelection: options.authenticatorSelection,
    extensions: options.extensions
});
```

### 3. Check Browser Support
```javascript
// Check if PRF is supported by the browser
if (typeof PublicKeyCredential !== 'undefined' && PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable) {
    const available = await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
    console.log("Platform authenticator available:", available);
}
```

## Common Issues and Solutions

### Issue 1: PRF Shows as "Not Supported" on Samsung Pass
**Cause**: Incorrect extension request or missing required options
**Solution**: Ensure `userVerification: "required"` and proper extension format

### Issue 2: PRF Works on Registration but Not Authentication
**Cause**: Different salt values or missing extension in get() request
**Solution**: Use identical salt values for both create() and get()

### Issue 3: All Authenticators Show as "Basic"
**Cause**: Fallback logic triggering too early or incorrect PRF detection
**Solution**: Fix response parsing to check for PRF results properly

## Testing Matrix

| Platform | Authenticator | Expected PRF Support | User Verification |
|----------|---------------|---------------------|-------------------|
| Samsung Galaxy S24 Ultra | Samsung Pass | ✅ Yes | Biometric |
| iPad Pro | Face ID | ✅ Yes | Face Recognition |
| iPhone 13+ | Face ID | ✅ Yes | Face Recognition |
| iPad Air | Touch ID | ✅ Yes | Fingerprint |
| Windows 11 | Windows Hello | ✅ Yes | Biometric |
| Windows 11 | Windows Hello PIN | ❌ No | PIN |
| Chrome OS | Platform Auth | ✅ Maybe | Varies |

## References

- [WebAuthn Level 3 Specification - PRF Extension](https://w3c.github.io/webauthn/#prf-extension)
- [FIDO2 PRF Extension Documentation](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-hmac-secret-extension)
- [MDN WebAuthn Extensions Guide](https://developer.mozilla.org/en-US/docs/Web/API/Web_Authentication_API/WebAuthn_extensions)
- [Chrome WebAuthn PRF Implementation](https://chromium.googlesource.com/chromium/src/+/main/content/browser/webauth/)

## Next Steps for Our Implementation

1. **Fix extension request format** - Ensure proper PRF extension structure
2. **Add platform detection** - Identify Samsung Pass, Face ID, etc.
3. **Improve response parsing** - Handle PRF results correctly per spec
4. **Add comprehensive logging** - Debug why PRF is not being enabled
5. **Test on multiple devices** - Validate across Samsung, Apple, Windows platforms