global.SimpleWebAuthnBrowser = {
    browserSupportsWebAuthn: jest.fn(),
    platformAuthenticatorIsAvailable: jest.fn(),
    startRegistration: jest.fn(),
    startAuthentication: jest.fn()
};

global.TextEncoder = class {
    encode(text) {
        const uint8Array = new Uint8Array(text.length);
        for (let i = 0; i < text.length; i++) {
            // eslint-disable-next-line no-restricted-syntax
            uint8Array[i] = text.charCodeAt(i);
        }
        return uint8Array;
    }
};

global.crypto = {
    getRandomValues: jest.fn((arr) => {
        for (let i = 0; i < arr.length; i++) {
            // eslint-disable-next-line no-restricted-syntax
            arr[i] = Math.floor(Math.random() * 256);
        }
        return arr;
    })
};

global.btoa = (str) => Buffer.from(str, 'binary').toString('base64');

delete global.location;
global.location = {
    hostname: 'testhost.com'
};

require('../src/webauthn-bridge.js');

describe('webAuthnWrapper.checkSupport', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    test('GivenBrowserSupportsWebAuthn_AndPlatformAuthenticatorAvailable_WhenCheckSupport_ThenReturnsSuccessWithPlatformAuthenticator', async () => {
        // Arrange
        global.SimpleWebAuthnBrowser.browserSupportsWebAuthn.mockReturnValue(true);
        global.SimpleWebAuthnBrowser.platformAuthenticatorIsAvailable.mockResolvedValue(true);

        // Act
        const result = await window.webAuthnWrapper.checkSupport();

        // Assert
        expect(result.supported).toBe(true);
        expect(result.platformAuthenticator).toBe(true);
        expect(result.error).toBe(null);
    });

    test('GivenBrowserSupportsWebAuthn_AndNoPlatformAuthenticator_WhenCheckSupport_ThenReturnsSuccessWithoutPlatformAuthenticator', async () => {
        // Arrange
        global.SimpleWebAuthnBrowser.browserSupportsWebAuthn.mockReturnValue(true);
        global.SimpleWebAuthnBrowser.platformAuthenticatorIsAvailable.mockResolvedValue(false);

        // Act
        const result = await window.webAuthnWrapper.checkSupport();

        // Assert
        expect(result.supported).toBe(true);
        expect(result.platformAuthenticator).toBe(false);
        expect(result.error).toBe(null);
    });

    test('GivenBrowserDoesNotSupportWebAuthn_WhenCheckSupport_ThenReturnsNotSupportedWithError', async () => {
        // Arrange
        global.SimpleWebAuthnBrowser.browserSupportsWebAuthn.mockReturnValue(false);

        // Act
        const result = await window.webAuthnWrapper.checkSupport();

        // Assert
        expect(result.supported).toBe(false);
        expect(result.platformAuthenticator).toBe(false);
        expect(result.error).toBe('WebAuthn not supported in this browser');
    });

    test('GivenLibraryNotLoaded_WhenCheckSupport_ThenReturnsNotSupportedWithError', async () => {
        // Arrange
        const originalLib = global.SimpleWebAuthnBrowser;
        global.SimpleWebAuthnBrowser = undefined;

        // Act
        const result = await window.webAuthnWrapper.checkSupport();

        // Assert
        expect(result.supported).toBe(false);
        expect(result.platformAuthenticator).toBe(false);
        expect(result.error).toContain('not loaded');

        global.SimpleWebAuthnBrowser = originalLib;
    });

    test('GivenPlatformAuthenticatorCheckThrows_WhenCheckSupport_ThenReturnsNotSupportedWithError', async () => {
        // Arrange
        global.SimpleWebAuthnBrowser.browserSupportsWebAuthn.mockReturnValue(true);
        global.SimpleWebAuthnBrowser.platformAuthenticatorIsAvailable.mockRejectedValue(new Error('Platform check failed'));

        // Act
        const result = await window.webAuthnWrapper.checkSupport();

        // Assert
        expect(result.supported).toBe(false);
        expect(result.error).toBe('Platform check failed');
    });
});

describe('webAuthnWrapper.register', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        global.SimpleWebAuthnBrowser.startRegistration = jest.fn();
        global.SimpleWebAuthnBrowser.startAuthentication = jest.fn();
    });

    test('GivenValidUsername_WhenRegister_ThenReturnsSuccessWithCredentialID', async () => {
        // Arrange
        const mockResponse = {
            id: 'test-credential-id',
            clientExtensionResults: {}
        };
        global.SimpleWebAuthnBrowser.startRegistration.mockResolvedValue(mockResponse);

        // Act
        const result = await window.webAuthnWrapper.register('testuser');

        // Assert
        expect(result.success).toBe(true);
        expect(result.credentialID).toBe('test-credential-id');
        expect(result.username).toBe('testuser');
        expect(result.error).toBe(null);
    });

    test('GivenValidUsername_WhenRegister_ThenCallsStartRegistrationWithCorrectOptions', async () => {
        // Arrange
        const mockResponse = {
            id: 'test-credential-id',
            clientExtensionResults: {}
        };
        global.SimpleWebAuthnBrowser.startRegistration.mockResolvedValue(mockResponse);

        // Act
        await window.webAuthnWrapper.register('testuser');

        // Assert
        expect(global.SimpleWebAuthnBrowser.startRegistration).toHaveBeenCalledWith(
            expect.objectContaining({
                optionsJSON: expect.objectContaining({
                    rp: expect.objectContaining({
                        id: 'localhost',
                        name: 'Clypse Portal'
                    }),
                    user: expect.objectContaining({
                        name: 'testuser',
                        displayName: 'testuser'
                    }),
                    pubKeyCredParams: expect.arrayContaining([
                        { type: 'public-key', alg: -7 },
                        { type: 'public-key', alg: -257 }
                    ]),
                    attestation: 'none'
                })
            })
        );
    });

    test('GivenEmptyUsername_WhenRegister_ThenReturnsFailureWithError', async () => {
        // Arrange

        // Act
        const result = await window.webAuthnWrapper.register('');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toContain('Username is required');
    });

    test('GivenNullUsername_WhenRegister_ThenReturnsFailureWithError', async () => {
        // Arrange

        // Act
        const result = await window.webAuthnWrapper.register(null);

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toContain('Username is required');
    });

    test('GivenWhitespaceUsername_WhenRegister_ThenReturnsFailureWithError', async () => {
        // Arrange

        // Act
        const result = await window.webAuthnWrapper.register('   ');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toContain('Username is required');
    });

    test('GivenExistingCredentialId_WhenRegister_ThenIncludesExcludeCredentials', async () => {
        // Arrange
        const mockResponse = {
            id: 'new-credential-id',
            clientExtensionResults: {}
        };
        global.SimpleWebAuthnBrowser.startRegistration.mockResolvedValue(mockResponse);

        // Act
        await window.webAuthnWrapper.register('testuser', 'existing-credential-id');

        // Assert
        expect(global.SimpleWebAuthnBrowser.startRegistration).toHaveBeenCalledWith(
            expect.objectContaining({
                optionsJSON: expect.objectContaining({
                    excludeCredentials: [
                        expect.objectContaining({
                            id: 'existing-credential-id',
                            type: 'public-key'
                        })
                    ]
                })
            })
        );
    });

    test('GivenPrfEnabled_WhenRegister_ThenReturnsPrfEnabledTrue', async () => {
        // Arrange
        const mockResponse = {
            id: 'test-credential-id',
            clientExtensionResults: {
                prf: { enabled: true }
            }
        };
        global.SimpleWebAuthnBrowser.startRegistration.mockResolvedValue(mockResponse);

        const mockAuthResponse = {
            clientExtensionResults: {
                prf: {
                    results: {
                        first: new Uint8Array([0x01, 0x02, 0x03, 0x04]).buffer
                    }
                }
            }
        };
        global.SimpleWebAuthnBrowser.startAuthentication.mockResolvedValue(mockAuthResponse);

        // Act
        const result = await window.webAuthnWrapper.register('testuser');

        // Assert
        expect(result.success).toBe(true);
        expect(result.prfEnabled).toBe(true);
        expect(result.prfOutput).toBe('01020304');
    });

    test('GivenPrfNotEnabled_WhenRegister_ThenReturnsPrfEnabledFalse', async () => {
        // Arrange
        const mockResponse = {
            id: 'test-credential-id',
            clientExtensionResults: {}
        };
        global.SimpleWebAuthnBrowser.startRegistration.mockResolvedValue(mockResponse);

        // Act
        const result = await window.webAuthnWrapper.register('testuser');

        // Assert
        expect(result.success).toBe(true);
        expect(result.prfEnabled).toBe(false);
        expect(result.prfOutput).toBe(null);
    });

    test('GivenPrfAuthenticationFails_WhenRegister_ThenReturnsSuccessWithoutPrfOutput', async () => {
        // Arrange
        const mockResponse = {
            id: 'test-credential-id',
            clientExtensionResults: {
                prf: { enabled: true }
            }
        };
        global.SimpleWebAuthnBrowser.startRegistration.mockResolvedValue(mockResponse);
        global.SimpleWebAuthnBrowser.startAuthentication.mockRejectedValue(new Error('PRF auth failed'));

        // Act
        const result = await window.webAuthnWrapper.register('testuser');

        // Assert
        expect(result.success).toBe(true);
        expect(result.prfEnabled).toBe(true);
        expect(result.prfOutput).toBe(null);
    });

    test('GivenRegistrationFails_WhenRegister_ThenReturnsFailureWithTranslatedError', async () => {
        // Arrange
        global.SimpleWebAuthnBrowser.startRegistration.mockRejectedValue(new Error('NotAllowedError: Operation cancelled'));

        // Act
        const result = await window.webAuthnWrapper.register('testuser');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toBe('Operation was cancelled or timed out');
    });

    test('GivenInvalidStateError_WhenRegister_ThenReturnsFailureWithCredentialError', async () => {
        // Arrange
        global.SimpleWebAuthnBrowser.startRegistration.mockRejectedValue(new Error('InvalidStateError: Credential exists'));

        // Act
        const result = await window.webAuthnWrapper.register('testuser');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toContain('credential for this user might already exist');
    });
});

describe('webAuthnWrapper.authenticate', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        global.SimpleWebAuthnBrowser.startAuthentication = jest.fn();
    });

    test('GivenValidCredentialID_WhenAuthenticate_ThenReturnsSuccessWithFlags', async () => {
        // Arrange
        const mockResponse = {
            response: {
                authenticatorDataFlags: {
                    up: true,
                    uv: true
                }
            },
            clientExtensionResults: {}
        };
        global.SimpleWebAuthnBrowser.startAuthentication.mockResolvedValue(mockResponse);

        // Act
        const result = await window.webAuthnWrapper.authenticate('test-credential-id');

        // Assert
        expect(result.success).toBe(true);
        expect(result.userPresent).toBe(true);
        expect(result.userVerified).toBe(true);
        expect(result.error).toBe(null);
    });

    test('GivenValidCredentialID_WhenAuthenticate_ThenCallsStartAuthenticationWithCorrectOptions', async () => {
        // Arrange
        const mockResponse = {
            response: { authenticatorDataFlags: {} },
            clientExtensionResults: {}
        };
        global.SimpleWebAuthnBrowser.startAuthentication.mockResolvedValue(mockResponse);

        // Act
        await window.webAuthnWrapper.authenticate('test-credential-id');

        // Assert
        expect(global.SimpleWebAuthnBrowser.startAuthentication).toHaveBeenCalledWith(
            expect.objectContaining({
                optionsJSON: expect.objectContaining({
                    rpId: 'localhost',
                    userVerification: 'preferred',
                    allowCredentials: [
                        expect.objectContaining({
                            id: 'test-credential-id',
                            type: 'public-key'
                        })
                    ]
                })
            })
        );
    });

    test('GivenNullCredentialID_WhenAuthenticate_ThenReturnsFailureWithError', async () => {
        // Arrange

        // Act
        const result = await window.webAuthnWrapper.authenticate(null);

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toContain('Credential ID is required');
    });

    test('GivenUndefinedCredentialID_WhenAuthenticate_ThenReturnsFailureWithError', async () => {
        // Arrange

        // Act
        const result = await window.webAuthnWrapper.authenticate(undefined);

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toContain('Credential ID is required');
    });

    test('GivenPrfResults_WhenAuthenticate_ThenReturnsPrfOutput', async () => {
        // Arrange
        const mockResponse = {
            response: {
                authenticatorDataFlags: { up: true, uv: true }
            },
            clientExtensionResults: {
                prf: {
                    results: {
                        first: new Uint8Array([0xaa, 0xbb, 0xcc, 0xdd]).buffer
                    }
                }
            }
        };
        global.SimpleWebAuthnBrowser.startAuthentication.mockResolvedValue(mockResponse);

        // Act
        const result = await window.webAuthnWrapper.authenticate('test-credential-id');

        // Assert
        expect(result.success).toBe(true);
        expect(result.prfOutput).toBe('aabbccdd');
    });

    test('GivenNoPrfResults_WhenAuthenticate_ThenReturnsNullPrfOutput', async () => {
        // Arrange
        const mockResponse = {
            response: {
                authenticatorDataFlags: { up: true, uv: false }
            },
            clientExtensionResults: {}
        };
        global.SimpleWebAuthnBrowser.startAuthentication.mockResolvedValue(mockResponse);

        // Act
        const result = await window.webAuthnWrapper.authenticate('test-credential-id');

        // Assert
        expect(result.success).toBe(true);
        expect(result.prfOutput).toBe(null);
    });

    test('GivenMissingAuthenticatorDataFlags_WhenAuthenticate_ThenReturnsFalseFlags', async () => {
        // Arrange
        const mockResponse = {
            response: {},
            clientExtensionResults: {}
        };
        global.SimpleWebAuthnBrowser.startAuthentication.mockResolvedValue(mockResponse);

        // Act
        const result = await window.webAuthnWrapper.authenticate('test-credential-id');

        // Assert
        expect(result.success).toBe(true);
        expect(result.userPresent).toBe(false);
        expect(result.userVerified).toBe(false);
    });

    test('GivenAuthenticationFails_WhenAuthenticate_ThenReturnsFailureWithTranslatedError', async () => {
        // Arrange
        global.SimpleWebAuthnBrowser.startAuthentication.mockRejectedValue(new Error('NotAllowedError: User cancelled'));

        // Act
        const result = await window.webAuthnWrapper.authenticate('test-credential-id');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toBe('Operation was cancelled or timed out');
    });

    test('GivenUnknownError_WhenAuthenticate_ThenReturnsFailureWithErrorMessage', async () => {
        // Arrange
        global.SimpleWebAuthnBrowser.startAuthentication.mockRejectedValue(new Error('Some other error'));

        // Act
        const result = await window.webAuthnWrapper.authenticate('test-credential-id');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toBe('Some other error');
    });
});
