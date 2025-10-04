/**
 * WebAuthn wrapper for Clypse Portal
 * Clean abstraction over SimpleWebAuthn library
 */
class WebAuthnWrapper {
    constructor() {
        this.RP_ID = location.hostname || 'localhost';
        this.RP_NAME = 'Clypse Portal';
        this.enc = new TextEncoder();
        
        // Check if SimpleWebAuthnBrowser is available
        if (typeof SimpleWebAuthnBrowser === 'undefined') {
            throw new Error('SimpleWebAuthnBrowser library not loaded');
        }
        
        const {
            startRegistration,
            startAuthentication,
            browserSupportsWebAuthn,
            platformAuthenticatorIsAvailable,
        } = SimpleWebAuthnBrowser;
        
        this.startRegistration = startRegistration;
        this.startAuthentication = startAuthentication;
        this.browserSupportsWebAuthn = browserSupportsWebAuthn;
        this.platformAuthenticatorIsAvailable = platformAuthenticatorIsAvailable;
    }

    randomBytes(len = 32) {
        const a = new Uint8Array(len);
        crypto.getRandomValues(a);
        return a;
    }
    
    toB64URL(bytes) {
        let str = btoa(String.fromCharCode(...bytes));
        return str.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
    }

    async checkSupport() {
        const result = {
            supported: false,
            platformAuthenticator: false,
            error: null
        };

        try {
            if (!this.browserSupportsWebAuthn?.()) {
                result.error = 'WebAuthn not supported in this browser';
                return result;
            }

            result.supported = true;
            result.platformAuthenticator = await this.platformAuthenticatorIsAvailable();
            
        } catch (err) {
            console.error('WebAuthn support check failed:', err);
            result.error = err.message;
        }

        return result;
    }

    async register(username, existingCredentialId = null) {
        if (!username?.trim()) {
            throw new Error('Username is required');
        }

        try {
            const challenge = this.toB64URL(this.randomBytes(32));
            const userIdBytes = this.enc.encode(username + '-' + Date.now());
            
            const optionsJSON = {
                rp: { 
                    id: this.RP_ID, 
                    name: this.RP_NAME 
                },
                user: {
                    id: this.toB64URL(userIdBytes),
                    name: username,
                    displayName: username,
                },
                challenge,
                pubKeyCredParams: [
                    { type: 'public-key', alg: -7 },
                    { type: 'public-key', alg: -257 },
                    { type: 'public-key', alg: -35 },
                    { type: 'public-key', alg: -36 },
                ],
                timeout: 60_000,
                attestation: 'none',
                authenticatorSelection: {
                    residentKey: 'preferred',
                    requireResidentKey: false,
                    userVerification: 'preferred',
                },
                excludeCredentials: existingCredentialId ? [{
                    id: existingCredentialId,
                    type: 'public-key',
                    transports: ['internal', 'usb', 'ble', 'nfc', 'hybrid'],
                }] : [],
                extensions: {
                    prf: {}
                }
            };

            const attResp = await this.startRegistration({ optionsJSON });
            const clientExtensionResults = attResp.clientExtensionResults || {};

            return {
                credentialID: attResp.id,
                userID: optionsJSON.user.id,
                username: username,
                response: attResp,
                extensions: clientExtensionResults,
                prfEnabled: clientExtensionResults.prf?.enabled || false
            };

        } catch (err) {
            console.error('WebAuthn registration failed:', err);
            
            if (err.message?.includes('NotAllowedError')) {
                throw new Error('Registration was cancelled or timed out');
            } else if (err.message?.includes('InvalidStateError')) {
                throw new Error('A credential for this user might already exist');
            }
            
            throw new Error(`Registration failed: ${err.message}`);
        }
    }

    async authenticate(credentialID) {
        if (!credentialID) {
            throw new Error('Credential ID is required');
        }

        try {
            const challenge = this.toB64URL(this.randomBytes(32));
            
            const optionsJSON = {
                challenge,
                rpId: this.RP_ID,
                userVerification: 'preferred',
                allowCredentials: [
                    {
                        id: credentialID,
                        type: 'public-key',
                        transports: ['internal', 'usb', 'ble', 'nfc', 'hybrid'],
                    },
                ],
                timeout: 60_000,
                extensions: {
                    prf: {
                        eval: {
                            first: this.enc.encode('WebAuthn PRF Registration Salt')
                        }
                    }
                }
            };

            const asseResp = await this.startAuthentication({ optionsJSON });
            const flags = asseResp.response?.authenticatorDataFlags || {};
            const clientExtensionResults = asseResp.clientExtensionResults || {};

            let prfOutput = null;
            if (clientExtensionResults.prf?.results?.first) {
                const prfResult = new Uint8Array(clientExtensionResults.prf.results.first);
                prfOutput = Array.from(prfResult).map(b => b.toString(16).padStart(2, '0')).join('');
            }

            return {
                response: asseResp,
                userPresent: flags.up || false,
                userVerified: flags.uv || false,
                extensions: clientExtensionResults,
                prfOutput: prfOutput,
                authenticatorData: asseResp.response?.authenticatorData
            };

        } catch (err) {
            console.error('WebAuthn authentication failed:', err);
            
            if (err.message?.includes('NotAllowedError')) {
                throw new Error('Authentication was cancelled or timed out');
            } else if (err.message?.includes('InvalidStateError')) {
                throw new Error('The credential is not available on this device');
            }
            
            throw new Error(`Authentication failed: ${err.message}`);
        }
    }
}

// Export for use in modules or make globally available
if (typeof module !== 'undefined' && module.exports) {
    module.exports = WebAuthnWrapper;
} else {
    window.WebAuthnWrapper = WebAuthnWrapper;
}