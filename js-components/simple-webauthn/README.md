# SimpleWebAuthn Library

A reusable, portable TypeScript/JavaScript library for WebAuthn credential management with optional PRF-based data encryption.

## 🚀 Features

- ✅ **TypeScript First** - Full type safety and IntelliSense support
- ✅ **Reusable & Portable** - Drop into any project without dependencies
- ✅ **PRF Extension Support** - Uses WebAuthn PRF when available, graceful fallback to credential ID
- ✅ **Platform Optimized** - Smart handling for Samsung Pass, Windows Hello, Touch ID, Face ID
- ✅ **Optional Encryption** - Can be used for pure authentication or authentication + data encryption
- ✅ **Comprehensive Diagnostics** - Detailed platform and capability detection
- ✅ **Security by Design** - Uses `window.location.hostname` as rpId by default

## 📁 Project Structure

```
simple-webauthn/
├── lib/                    # TypeScript source files
│   ├── simple-webauthn.ts     # Main library entry point
│   ├── types.ts               # TypeScript type definitions
│   ├── webauthn-core.ts       # Core WebAuthn functionality
│   ├── encryption-utils.ts    # AES-GCM encryption utilities
│   ├── platform-detector.ts   # Platform detection logic
│   └── input-validator.ts     # Input validation utilities
├── tests/                  # Jest test files
│   ├── simple-webauthn.test.ts
│   ├── webauthn-core.test.ts
│   └── test-helpers.ts
├── dist/                   # Compiled JavaScript output
│   ├── simple-webauthn.js     # Development build
│   └── simple-webauthn.min.js # Production build (minified)
├── docs/                   # Documentation and diagrams
│   ├── README.md
│   ├── API-Reference.md
│   └── *.puml              # C4 architecture diagrams
├── tsconfig.json           # TypeScript configuration
├── rollup.config.js        # Build configuration
└── package.json           # Build tooling dependencies
```

## 🛠️ Development Setup

### Prerequisites
- Node.js 18+ 
- npm or yarn

### Install Dependencies
```bash
cd js-components/simple-webauthn
npm install
```

### Build Commands
```bash
# Build once
npm run build

# Build and watch for changes
npm run build:watch

# Run tests
npm run test

# Run tests in watch mode
npm run test:watch

# Development mode (build + test watch)
npm run dev

# Copy built file to portal
npm run copy-to-portal
```

## 🔨 Build Process

1. **TypeScript Compilation** - `tsc` compiles TypeScript to JavaScript with declarations
2. **Rollup Bundling** - Combines modules into single IIFE bundle for browser use
3. **Minification** - Terser creates production-ready minified version
4. **Source Maps** - Generated for debugging support

## 🧪 Testing

The project uses Jest with jsdom for comprehensive testing:

- **Unit Tests** - Test individual functions and classes
- **Integration Tests** - Test complete workflows
- **Mock WebAuthn APIs** - Simulate browser WebAuthn behavior
- **Platform Testing** - Verify platform-specific optimizations

## 🚢 Integration with Main Project

### Option 1: Build Script Integration
Add to main project's build process:
```json
{
  "scripts": {
    "build-webauthn": "cd js-components/simple-webauthn && npm run build && npm run copy-to-portal"
  }
}
```

### Option 2: Watch Mode for Development
Run in development:
```bash
cd js-components/simple-webauthn
npm run build:watch
```

### Option 3: Manual Copy
```bash
copy dist/simple-webauthn.js ../../clypse.portal/wwwroot/js/
```

## 📖 Usage

### Basic Authentication
```typescript
const result = await SimpleWebAuthn.createCredential({
  rpName: "My App",
  userName: "user@example.com",
  userDisplayName: "John Doe"
});

if (result.success) {
  // Store result.credential.id for later use
}
```

### With Encryption
```typescript
const result = await SimpleWebAuthn.createCredential({
  rpName: "My App", 
  userName: "user@example.com",
  userDisplayName: "John Doe",
  plaintextToEncrypt: "sensitive data",
  encryptionSalt: "my-app-v1"
});

// Later authenticate and decrypt
const auth = await SimpleWebAuthn.authenticate({
  credentialId: storedCredentialId,
  encryptedData: storedEncryptedData,
  encryptionSalt: "my-app-v1"
});
```

## 🏗️ Architecture

The library is built with clean architecture principles:

- **SimpleWebAuthn** - Main API facade
- **WebAuthnCore** - Core WebAuthn operations with PRF handling  
- **EncryptionUtils** - AES-GCM encryption with HKDF key derivation
- **PlatformDetector** - Platform-specific optimizations
- **InputValidator** - Comprehensive input validation
- **Types** - Complete TypeScript type definitions

## 📋 Contributing

1. Make changes in `lib/` TypeScript files
2. Add/update tests in `tests/`
3. Run `npm run build` to compile
4. Run `npm run test` to verify
5. Update documentation if needed

## 🔍 Debugging

- Source maps are generated for debugging compiled code
- Console logging shows PRF detection and platform optimization
- Comprehensive diagnostic information in all responses

## 📚 Documentation

See `docs/` folder for:
- Complete API reference
- C4 architecture diagrams  
- Platform compatibility matrix
- Migration guides
- Best practices