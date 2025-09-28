# SimpleWebAuthn Library API Reference

## Overview

SimpleWebAuthn is a reusable JavaScript library that provides a clean, simple interface for WebAuthn credential management with optional data encryption using the PRF (Pseudo-Random Function) extension or credential ID fallback.

## Key Features

- ✅ **Reusable & Portable** - Drop into any project without dependencies on localStorage or specific frameworks
- ✅ **PRF Extension Support** - Uses WebAuthn PRF when available, graceful fallback to credential ID
- ✅ **Platform Optimized** - Smart handling for Samsung Pass, Windows Hello, Touch ID, Face ID
- ✅ **Optional Encryption** - Can be used for pure authentication or authentication + data encryption
- ✅ **Comprehensive Diagnostics** - Detailed platform and capability detection
- ✅ **Security by Design** - Always uses `window.location.hostname` as rpId

## Installation

Simply include the JavaScript file in your HTML:

```html
<script src="js/simple-webauthn.js"></script>
```

## API Reference

### `SimpleWebAuthn.createCredential(options)`

Creates a new WebAuthn credential and optionally encrypts data.

#### Parameters

```javascript
{
    // Required
    rpName: string,              // Display name for your application
    userName: string,            // User identifier (email, username, etc.)
    userDisplayName: string,     // Human-readable user display name
    
    // Optional - Advanced Use Cases
    rpId?: string,               // Relying party ID (default: window.location.hostname)
                                 // Use parent domain for subdomain credential sharing
    
    // Optional Encryption
    plaintextToEncrypt?: string, // Data to encrypt (if omitted, only creates credential)
    encryptionSalt?: string,     // Custom salt (default: "webauthn-prf-salt-v1")
    
    // Optional WebAuthn Settings
    timeout?: number,            // Timeout in milliseconds (default: 60000, Samsung: 300000)
    userVerification?: string,   // "required" | "preferred" | "discouraged" (default: "required")
    authenticatorAttachment?: string, // "platform" | "cross-platform" (default: "platform")
    residentKey?: string        // "required" | "preferred" | "discouraged" (default: "preferred")
}
```

#### Returns

```javascript
{
    success: boolean,
    error?: string,
    
    // Credential data (caller must store this)
    credential?: {
        id: string,                    // Base64-encoded credential ID
        rawId: ArrayBuffer,           // Raw credential ID bytes
        publicKey?: string,           // Base64-encoded public key (if needed)
        attestationObject?: string    // Base64-encoded attestation (if needed)
    },
    
    // Encryption results (if plaintextToEncrypt provided)
    encryption?: {
        encryptedData: string,        // Base64-encoded encrypted data
        keyDerivationMethod: "PRF" | "CredentialID"
    },
    
    // Diagnostics
    diagnostics: {
        userAgent: string,
        platform: string,
        prfSupported: boolean,
        prfResultsAvailable: boolean,
        authenticatorType: string,
        keyDerivationMethod: string,
        credentialIdLength: number
    }
}
```

#### Example Usage

```javascript
// Basic credential with encryption
const result = await SimpleWebAuthn.createCredential({
    rpName: "My Awesome App",
    userName: "user@example.com",
    userDisplayName: "John Doe",
    plaintextToEncrypt: "sensitive user data",
    encryptionSalt: "my-app-salt-v1"
});

// Subdomain credential sharing (advanced)
const subdomainResult = await SimpleWebAuthn.createCredential({
    rpName: "My SaaS Platform",
    rpId: "example.com",  // Works on app.example.com, admin.example.com, etc.
    userName: "user@example.com",
    userDisplayName: "John Doe",
    plaintextToEncrypt: "cross-subdomain data"
});

if (result.success) {
    // Store credential for later use
    localStorage.setItem('app_credential_id', result.credential.id);
    
    // Store encrypted data
    localStorage.setItem('app_encrypted_data', result.encryption.encryptedData);
    
    console.log('Credential created:', result.credential.id);
    console.log('Using:', result.encryption.keyDerivationMethod);
} else {
    console.error('Failed:', result.error);
}
```

### `SimpleWebAuthn.authenticate(options)`

Authenticates with an existing WebAuthn credential and optionally decrypts data.

#### Parameters

```javascript
{
    // Required - caller provides stored credential
    credentialId: string,        // Base64-encoded credential ID from createCredential()
    
    // Optional Decryption
    encryptedData?: string,      // Base64-encoded data to decrypt
    encryptionSalt?: string,     // Must match creation salt (default: "webauthn-prf-salt-v1")
    
    // Optional WebAuthn Settings
    timeout?: number,            // Timeout in milliseconds (default: 60000, Samsung: 300000)
    userVerification?: string    // "required" | "preferred" | "discouraged" (default: "required")
}
```

#### Returns

```javascript
{
    success: boolean,
    error?: string,
    
    // Authentication results
    authentication: {
        credentialId: string,           // Credential ID that was used
        signature: string,              // Base64-encoded assertion signature
        authenticatorData: string,      // Base64-encoded authenticator data
        keyDerivationMethod: "PRF" | "CredentialID"
    },
    
    // Decryption results (if encryptedData provided)
    decryption?: {
        plaintext: string,              // Decrypted data
        keyDerivationMethod: "PRF" | "CredentialID"
    },
    
    // Diagnostics (same structure as createCredential)
    diagnostics: { /* ... */ }
}
```

#### Example Usage

```javascript
// Authenticate and decrypt
const result = await SimpleWebAuthn.authenticate({
    credentialId: localStorage.getItem('app_credential_id'),
    encryptedData: localStorage.getItem('app_encrypted_data'),
    encryptionSalt: "my-app-salt-v1"
});

if (result.success) {
    console.log('Authenticated!');
    
    if (result.decryption) {
        console.log('Decrypted data:', result.decryption.plaintext);
        console.log('Using:', result.decryption.keyDerivationMethod);
    }
} else {
    console.error('Authentication failed:', result.error);
}
```

## Platform Support & Behavior

### PRF Extension Support

| Platform | Authenticator | PRF Support | Fallback Method |
|----------|---------------|-------------|-----------------|
| Windows | Windows Hello | ✅ Yes | Credential ID |
| macOS | Touch ID | ✅ Yes | Credential ID |
| iOS | Face ID/Touch ID | ✅ Yes | Credential ID |
| Android | Biometric | ✅ Yes | Credential ID |
| Samsung | Samsung Pass | ⚠️ Partial* | Credential ID |

*Samsung Pass reports PRF as enabled but may not return results in all scenarios.

### Automatic Platform Optimizations

- **Samsung Pass**: Extended timeout (300s), `residentKey: "required"`
- **iOS Safari**: Optimized for Touch ID/Face ID behavior
- **Windows Hello**: Full PRF support with biometric unlock
- **Generic Platform**: Conservative settings with credential ID fallback

## Security Considerations

### rpId Binding

By default, the library uses `window.location.hostname` as the rpId. The browser enforces strict validation:

- ✅ rpId must be the current domain or a valid parent domain
- ✅ Credentials are cryptographically bound to the rpId
- ✅ Prevents cross-site credential attacks by design

#### Custom rpId Use Cases

You can optionally specify a custom rpId for:

**Subdomain Consolidation:**
```javascript
// On: app.example.com, admin.example.com, dashboard.example.com
// Use: rpId: "example.com" 
// Result: Same credential works across all subdomains
```

**Important Security Notes:**
- ❌ Cannot use unrelated domains (browser will reject)
- ❌ Cannot use `evil.com` when on `myapp.com`  
- ✅ Can use `example.com` when on `app.example.com`
- ✅ Browser validates rpId matches current origin

### Salt Recommendations

When using encryption:

- Use a unique salt per application: `"myapp-encryption-v1"`
- Include version numbers for future upgrades
- Keep salts consistent between create/authenticate operations

### Storage Responsibilities

The library does **NOT** handle storage - you must:

- Store credential IDs securely (localStorage, database, etc.)
- Store encrypted data securely
- Handle credential lifecycle (creation, rotation, deletion)

## Error Handling

### Common Error Scenarios

| Error | Cause | Solution |
|-------|-------|----------|
| "WebAuthn not supported" | Browser/platform lacks WebAuthn | Show fallback UI |
| "Platform authenticator not available" | No biometric setup | Guide user to setup |
| "User cancelled" | User declined biometric prompt | Allow retry |
| "Timeout" | User didn't respond in time | Increase timeout |
| "Invalid credential" | Stored credential no longer valid | Create new credential |

### Error Response Format

All methods return a consistent error format:

```javascript
{
    success: false,
    error: "Human-readable error message"
}
```

## Migration from Legacy Code

If migrating from the original `WebAuthnPrf` library:

### Before (Legacy)
```javascript
// Old way - tied to localStorage
const result = await WebAuthnPrf.encrypt("data");
// Stores in localStorage automatically

const decrypted = await WebAuthnPrf.decrypt();
// Reads from localStorage automatically
```

### After (SimpleWebAuthn)
```javascript
// New way - caller manages storage
const result = await SimpleWebAuthn.createCredential({
    rpName: "My App",
    userName: "user@example.com", 
    userDisplayName: "User",
    plaintextToEncrypt: "data"
});

// You handle storage
localStorage.setItem('cred', result.credential.id);
localStorage.setItem('data', result.encryption.encryptedData);

// You provide stored data
const auth = await SimpleWebAuthn.authenticate({
    credentialId: localStorage.getItem('cred'),
    encryptedData: localStorage.getItem('data')
});
```

## Best Practices

### 1. Credential Lifecycle Management

```javascript
// Create credential once per user
const credential = await SimpleWebAuthn.createCredential({...});

// Store credential ID persistently
await saveToDatabase(user.id, credential.id);

// Use same credential for ongoing authentication
const auth = await SimpleWebAuthn.authenticate({
    credentialId: await loadFromDatabase(user.id)
});
```

### 2. Error Handling

```javascript
try {
    const result = await SimpleWebAuthn.createCredential({...});
    
    if (!result.success) {
        // Handle specific errors
        if (result.error.includes('not supported')) {
            showFallbackAuth();
        } else if (result.error.includes('cancelled')) {
            showRetryOption();
        } else {
            showGenericError(result.error);
        }
        return;
    }
    
    // Success path
    handleSuccess(result);
    
} catch (error) {
    // Unexpected errors
    console.error('Unexpected WebAuthn error:', error);
    showGenericError('Authentication system unavailable');
}
```

### 3. Progressive Enhancement

```javascript
// Check WebAuthn support before using
if (window.PublicKeyCredential) {
    // Use SimpleWebAuthn
    setupWebAuthnAuth();
} else {
    // Fallback to traditional auth
    setupPasswordAuth();
}
```

### 4. User Experience

```javascript
// Provide clear user feedback
showMessage("Please use your fingerprint, face, or PIN to continue...");

const result = await SimpleWebAuthn.authenticate({...});

if (result.success) {
    showMessage("Authentication successful! ✅");
} else {
    showMessage(`Authentication failed: ${result.error}`);
}
```