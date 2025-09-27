# WebAuthn PRF C4 Architecture Diagrams

This directory contains PlantUML C4 architecture diagrams for the WebAuthn PRF (Pseudo-Random Function) encryption system implementation in Clypse Portal.

## Diagrams Overview

### 1. System Context (`WebAuthn-PRF-Context.puml`)
Shows the high-level system context with external actors and systems:
- User interaction with the web application
- Browser as the execution environment
- Platform authenticator (Samsung Pass, Face ID, Windows Hello)
- Encrypted data storage

### 2. Container View (`WebAuthn-PRF-Container.puml`) 
Shows the major containers within the browser system:
- Clypse Portal JavaScript SPA
- WebAuthn API (browser native)
- Web Crypto API (browser native)
- Local Storage for credential persistence

### 3. Component View (`WebAuthn-PRF-Component.puml`)
Shows the internal components of the Clypse Portal:
- Encryption UI (Razor/JavaScript)
- WebAuthn Manager (credential lifecycle)
- Crypto Manager (HKDF + AES-GCM)
- Diagnostic Logger (debugging support)
- Storage Manager (credential ID persistence)
- Error Handler (fallback scenarios)

### 4. Code View (`WebAuthn-PRF-Code.puml`)
Shows the detailed sequence diagram of the encryption/decryption flow:
- UI button interaction
- WebAuthn API calls with PRF extension
- Biometric authentication
- Key derivation using HKDF
- AES-GCM encryption/decryption
- Credential storage

## Viewing the Diagrams

### VS Code Extensions
Install one of these PlantUML extensions for VS Code:
- **PlantUML** (`jebbs.plantuml`) - Most popular
- **PlantUML Previewer** (`okazuki.okazukiplantuml`)

### Online Viewers
- [PlantUML Server](http://www.plantuml.com/plantuml/uml/)
- [PlantText](https://www.planttext.com/)

### Local Installation
1. Install Java JRE/JDK
2. Download PlantUML JAR from [plantuml.com](https://plantuml.com/download)
3. Run: `java -jar plantuml.jar diagram.puml`

## Key Architecture Points

### WebAuthn PRF Integration
- Uses PRF extension for deterministic key generation
- Combines authentication with encryption in single operation
- Hardware-backed security through platform authenticators

### Fallback Strategies
- Graceful degradation when PRF not supported
- Credential ID-based storage for non-PRF scenarios  
- Comprehensive error handling and user feedback

### Security Model
- No key storage in browser - keys derived on-demand
- Biometric authentication required for each operation
- Salt-based key derivation for unique encryption keys

### Platform Compatibility
- Samsung Pass: Requires `residentKey: "required"`
- iPad Face ID: Works with standard WebAuthn options
- Windows Hello: Full PRF support available
- Chrome/Edge: Best compatibility across platforms

## Related Documentation
- `WebAuthn-PRF-Implementation.md` - Implementation guide
- `WebAuthn-PRF-Troubleshooting.md` - Common issues and solutions
- `WebAuthn-Platform-Examples.md` - Platform-specific examples