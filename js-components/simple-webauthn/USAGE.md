# SimpleWebAuthn Library Usage Guide

This document explains how to use both the old WebAuthnPrf implementation and the new SimpleWebAuthn library.

## Overview

- **Old Implementation**: `webauthn-prf.js` - localStorage-based encrypt/decrypt API
- **New Implementation**: `simple-webauthn.js` - Reusable library with credential management API

## New SimpleWebAuthn Library

### Installation

Include the script in your HTML:
```html
<script src="js/simple-webauthn.js"></script>
```

### API Reference

#### Creating a Credential

```javascript
const result = await SimpleWebAuthn.createCredential({
    rp: { 
        name: "Your App Name", 
        id: "localhost" // or your domain
    },
    user: { 
        id: btoa("user-id-123"), // base64 encoded user ID
        name: "user@example.com",
        displayName: "User Name"
    },
    challenge: btoa("random-challenge-16-bytes"), // base64 encoded challenge
    pubKeyCredParams: [
        { alg: -7, type: "public-key" },    // ES256
        { alg: -257, type: "public-key" }   // RS256
    ],
    authenticatorSelection: {
        authenticatorAttachment: "platform",
        userVerification: "required",
        residentKey: "preferred"
    },
    timeout: 60000,
    attestation: "none"
});

if (result.success) {
    console.log("Credential ID:", result.credentialId);
    console.log("Method:", result.keyDerivationMethod); // "PRF" or "Credential ID"
    // Store the credential ID for later authentication
}
```

#### Authenticating and Deriving Keys

```javascript
const result = await SimpleWebAuthn.authenticate({
    challenge: btoa("auth-challenge-16-bytes"), // base64 encoded challenge
    allowCredentials: [{
        id: credentialId, // from createCredential result
        type: "public-key",
        transports: ["internal", "hybrid"]
    }],
    userVerification: "required",
    timeout: 60000,
    userData: "data to encrypt" // optional
});

if (result.success) {
    console.log("Derived Key:", result.derivedKey);
    console.log("Encrypted Data:", result.encryptedUserData);
    console.log("Method:", result.keyDerivationMethod);
}
```

### Key Features

1. **Automatic Platform Detection**: Optimizes for Samsung Pass, iOS, Windows Hello
2. **Fallback Support**: Uses PRF when available, credential ID as fallback
3. **Comprehensive Diagnostics**: Detailed logging and error reporting
4. **TypeScript Support**: Full type definitions included
5. **Modern API**: Promise-based, no localStorage dependency

## Old WebAuthnPrf Implementation

Still available for reference and comparison:

### Encrypt Data
```javascript
const result = await WebAuthnPrf.encrypt("hello world");
if (result.success) {
    // Data automatically stored in localStorage
    console.log("Encrypted data:", result.encryptedDataBase64);
}
```

### Decrypt Data
```javascript
const result = await WebAuthnPrf.decrypt();
if (result.success) {
    console.log("Decrypted text:", result.plaintext);
}
```

## Migration Guide

### From WebAuthnPrf to SimpleWebAuthn

**Old Pattern:**
```javascript
// Create and encrypt
await WebAuthnPrf.encrypt("my secret data");

// Later, decrypt
const result = await WebAuthnPrf.decrypt();
```

**New Pattern:**
```javascript
// Create credential once
const credential = await SimpleWebAuthn.createCredential(options);
const credentialId = credential.credentialId;

// Authenticate and get key material
const auth = await SimpleWebAuthn.authenticate({
    allowCredentials: [{ id: credentialId, type: "public-key" }],
    userData: "my secret data"
});

// Use the derived key for your own encryption
const key = auth.derivedKey;
const encryptedData = auth.encryptedUserData;
```

## Testing in Clypse Portal

The test page includes both implementations:

1. **Cryptography Tests** section: Tests old WebAuthnPrf implementation
2. **SimpleWebAuthn Library Tests** section: Tests new SimpleWebAuthn library

### Test Flow

1. Navigate to `/test`
2. Create a credential with "Create Credential" button
3. Use "Authenticate & Encrypt" to test key derivation
4. Compare results between old and new implementations

## Browser Support

### PRF Extension Support
- ✅ Windows Hello (Windows 11)
- ✅ Touch ID (macOS Safari)
- ✅ Samsung Pass (Samsung Internet)
- ❌ Chrome on Android (uses credential ID fallback)

### Credential ID Fallback
- ✅ All modern browsers with WebAuthn support
- ✅ Platform authenticators (Windows Hello, Touch ID, Android biometrics)
- ✅ Security keys and external authenticators

## Security Considerations

1. **Challenge Generation**: Always use cryptographically random challenges
2. **Origin Validation**: Ensure RP ID matches your domain
3. **Key Storage**: Derived keys are ephemeral and not stored
4. **User Verification**: Required for both PRF and credential ID methods
5. **Timeout Handling**: Generous timeouts for Samsung Pass compatibility

## Troubleshooting

### Common Issues

1. **"No PRF results"**: Normal on some platforms, library will use credential ID fallback
2. **Timeout errors**: Increase timeout for Samsung Pass (5 minutes recommended)
3. **Credential not found**: Ensure allowCredentials includes the correct credential ID
4. **User verification failed**: Check that user completed biometric/PIN verification

### Debugging

Enable verbose logging:
```javascript
// The library automatically logs to console in development
// Check browser developer console for detailed diagnostics
```

## Performance Notes

- **PRF Method**: Slightly faster, generates consistent keys
- **Credential ID Method**: Universal compatibility, hash-based key derivation
- **Samsung Pass**: Requires longer timeouts (up to 5 minutes)
- **Platform Authenticators**: Generally faster than external security keys