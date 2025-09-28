# SimpleWebAuthn Library Documentation

This folder contains comprehensive documentation for the SimpleWebAuthn library - a reusable, portable JavaScript library for WebAuthn credential management with optional data encryption.

## 📚 Documentation Files

### API Documentation
- **[API-Reference.md](API-Reference.md)** - Complete API reference with examples, platform support, and best practices

### C4 Architecture Diagrams

The C4 model provides a hierarchical view of the system architecture:

#### Level 1: System Context
- **[SimpleWebAuthn-Context.puml](SimpleWebAuthn-Context.puml)** - High-level view showing how SimpleWebAuthn fits into the broader ecosystem of web applications, browsers, and platform authenticators

#### Level 2: Container Diagram  
- **[SimpleWebAuthn-Container.puml](SimpleWebAuthn-Container.puml)** - Shows the major containers (frontend, library, storage, backend) and their interactions

#### Level 3: Component Diagram
- **[SimpleWebAuthn-Component.puml](SimpleWebAuthn-Component.puml)** - Internal structure of the SimpleWebAuthn library showing its key components and responsibilities

#### Sequence Diagrams
- **[SimpleWebAuthn-Sequence-Create.puml](SimpleWebAuthn-Sequence-Create.puml)** - Step-by-step flow for credential creation and encryption
- **[SimpleWebAuthn-Sequence-Auth.puml](SimpleWebAuthn-Sequence-Auth.puml)** - Step-by-step flow for authentication and decryption

#### Technical Architecture
- **[SimpleWebAuthn-Key-Derivation.puml](SimpleWebAuthn-Key-Derivation.puml)** - Detailed view of PRF vs Credential ID key derivation strategies and platform behaviors

## 🔑 Key Concepts

### WebAuthn PRF Extension
The Pseudo-Random Function (PRF) extension provides cryptographically strong key material derived from user authentication. SimpleWebAuthn intelligently uses PRF when available and falls back to credential ID derivation.

### Platform Optimization
SimpleWebAuthn automatically detects and optimizes for different platforms:
- **Windows Hello**: Full PRF support with biometric authentication
- **Touch ID/Face ID**: Reliable PRF implementation  
- **Samsung Pass**: Partial PRF support with intelligent fallback
- **Android Biometric**: Variable PRF support by device

### Security Model
- **rpId Binding**: Uses `window.location.hostname` by default to prevent cross-site attacks
- **Key Derivation**: HKDF-SHA256 ensures strong keys regardless of source material
- **Encryption**: AES-GCM-256 with random IVs for authenticated encryption

## 🚀 Quick Start

```javascript
// Create credential with encryption
const result = await SimpleWebAuthn.createCredential({
    rpName: "My App",
    userName: "user@example.com",
    userDisplayName: "John Doe", 
    plaintextToEncrypt: "sensitive data"
});

// Store credential for later use
if (result.success) {
    localStorage.setItem('credentialId', result.credential.id);
    localStorage.setItem('encryptedData', result.encryption.encryptedData);
}

// Later: authenticate and decrypt
const auth = await SimpleWebAuthn.authenticate({
    credentialId: localStorage.getItem('credentialId'),
    encryptedData: localStorage.getItem('encryptedData')
});

if (auth.success) {
    console.log('Decrypted:', auth.decryption.plaintext);
}
```

## 📋 Migration Guide

### From Legacy WebAuthnPrf Library

The new SimpleWebAuthn library removes localStorage dependencies and provides a cleaner API:

**Old (WebAuthnPrf):**
```javascript
await WebAuthnPrf.encrypt("data");        // Auto-stores in localStorage
await WebAuthnPrf.decrypt();              // Auto-reads from localStorage
```

**New (SimpleWebAuthn):**
```javascript
const result = await SimpleWebAuthn.createCredential({
    rpName: "App", userName: "user", userDisplayName: "User",
    plaintextToEncrypt: "data"
});
// Developer handles storage
localStorage.setItem('cred', result.credential.id);

const auth = await SimpleWebAuthn.authenticate({
    credentialId: localStorage.getItem('cred'),
    encryptedData: result.encryption.encryptedData
});
```

## 🏗️ Architecture Principles

### 1. **Reusability**
- No hardcoded application-specific logic
- Configurable through parameters
- No external dependencies

### 2. **Portability** 
- Works in any modern web application
- No localStorage or framework dependencies
- Clean separation of concerns

### 3. **Security**
- Always uses secure defaults
- Proper error handling
- Comprehensive input validation

### 4. **Platform Compatibility**
- Handles platform quirks transparently
- Graceful degradation when features unavailable
- Extensive platform testing and optimization

## 🔍 Viewing the Diagrams

To view the PlantUML diagrams:

1. **Online**: Copy diagram content to [PlantUML Online Server](http://www.plantuml.com/plantuml/uml/)
2. **VS Code**: Install the "PlantUML" extension 
3. **Local**: Install PlantUML locally with Java

## 📞 Support

For questions about the SimpleWebAuthn library:
1. Check the API reference documentation
2. Review the sequence diagrams for implementation details  
3. Examine platform-specific behaviors in the key derivation diagram
4. See the original WebAuthn-PRF documentation for background context