# SimpleWebAuthn Test Application

A simple one-page application to test WebAuthn credential registration and authentication using the SimpleWebAuthn library.

## Features

- 🔐 WebAuthn credential registration
- 🔑 WebAuthn credential authentication  
- 🔑 **PRF Extension Support** - Derive cryptographic keys from credentials
- 🌐 Works with platform authenticators (Touch ID, Windows Hello, etc.)
- 🔌 Compatible with security keys and external authenticators
- 📱 Responsive design for mobile and desktop
- 📋 Detailed activity logging
- ⚡ No server required - runs entirely in the browser

## Quick Start

1. Install dependencies:
   ```bash
   npm install
   ```

2. Start the application:
   ```bash
   npm start
   ```

3. Open your browser to `http://localhost:3000`

## How to Use

1. **Enter a username** - Can be any string (e.g., "user@example.com")
2. **Register Credential** - Click the register button and follow your browser's prompts
3. **Authenticate** - Once registered, test authentication with the same credential
4. **Test PRF Extension** - If supported, derive cryptographic keys from your credential

## Browser Support

This application requires a modern browser that supports WebAuthn:
- Chrome 67+
- Firefox 60+
- Safari 14+
- Edge 18+

## Authenticator Support

Works with:
- **Platform authenticators**: Touch ID (macOS), Windows Hello, Android fingerprint
- **Security keys**: YubiKey, Titan Security Key, and other FIDO2/WebAuthn devices
- **Hybrid transport**: QR code scanning for mobile devices

## Technical Details

- Uses `@simplewebauthn/browser` via CDN
- Client-side only (no server verification)
- Stores credential data in JavaScript variables (demo purposes only)
- Implements both registration and authentication flows
- **PRF Extension**: Supports Pseudo-Random Function for deriving cryptographic keys
- Includes comprehensive error handling and user feedback

## PRF Extension

The PRF (Pseudo-Random Function) extension allows you to derive deterministic cryptographic outputs from your WebAuthn credentials. This is useful for:

- **Client-side encryption**: Derive keys to encrypt data locally
- **Key derivation**: Generate symmetric keys for secure communication
- **Deterministic tokens**: Create consistent outputs for the same inputs
- **Key rotation**: Use different inputs to derive different keys

PRF outputs are:
- **Deterministic**: Same input always produces the same output
- **Cryptographically secure**: 32-byte outputs suitable for encryption keys
- **Credential-bound**: Each credential produces unique outputs
- **Input-dependent**: Different inputs produce completely different outputs

## Production Considerations

This is a **demo application** for testing WebAuthn functionality. In a production environment, you should:

- Generate options on your server using `@simplewebauthn/server`
- Verify all responses cryptographically on your server
- Store credential data securely in a database
- Implement proper user sessions and authentication flows
- Use HTTPS in production (required for WebAuthn)

## Development

The application uses a simple HTTP server for local development. You can also open `index.html` directly in your browser, but some features may work better when served over HTTP.

## License

MIT