// Test helpers for mocking WebAuthn APIs in Jest
declare global {
  namespace jest {
    interface Global {
      navigator: any;
      PublicKeyCredential: any;
      crypto: any;
      window: any;
    }
  }
}

// Mock WebAuthn APIs
global.navigator = {
  credentials: {
    create: jest.fn(),
    get: jest.fn()
  },
  userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
  platform: 'Win32'
};

global.PublicKeyCredential = {
  isUserVerifyingPlatformAuthenticatorAvailable: jest.fn(() => Promise.resolve(true)),
  getClientCapabilities: jest.fn(() => Promise.resolve({}))
};

global.crypto = {
  getRandomValues: jest.fn((arr: Uint8Array) => {
    // Fill with mock random data
    for (let i = 0; i < arr.length; i++) {
      arr[i] = Math.floor(Math.random() * 256);
    }
    return arr;
  }),
  subtle: {
    importKey: jest.fn(() => Promise.resolve({} as CryptoKey)),
    deriveKey: jest.fn(() => Promise.resolve({} as CryptoKey)),
    encrypt: jest.fn(() => Promise.resolve(new ArrayBuffer(32))),
    decrypt: jest.fn(() => Promise.resolve(new ArrayBuffer(16)))
  }
};

global.window = {
  location: {
    hostname: 'localhost'
  },
  PublicKeyCredential: global.PublicKeyCredential
};

// Helper functions for tests
export const mockCredential = {
  id: 'mock-credential-id',
  rawId: new ArrayBuffer(32),
  response: {
    attestationObject: new ArrayBuffer(64),
    signature: new ArrayBuffer(32),
    authenticatorData: new ArrayBuffer(48)
  },
  getClientExtensionResults: jest.fn(() => ({
    prf: {
      enabled: true,
      results: {
        first: new ArrayBuffer(32)
      }
    }
  }))
};

export const mockCreateCredentialSuccess = () => {
  (global.navigator.credentials.create as jest.Mock).mockResolvedValue(mockCredential);
};

export const mockAuthenticateSuccess = () => {
  (global.navigator.credentials.get as jest.Mock).mockResolvedValue(mockCredential);
};

// Reset mocks between tests
beforeEach(() => {
  jest.clearAllMocks();
});