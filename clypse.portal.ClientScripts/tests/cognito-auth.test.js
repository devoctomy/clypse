global.AmazonCognitoIdentity = {
    CognitoUserPool: jest.fn(),
    CognitoUser: jest.fn(),
    AuthenticationDetails: jest.fn()
};

global.AWS = {
    config: {
        region: null,
        credentials: null
    },
    CognitoIdentity: jest.fn(),
    CognitoIdentityCredentials: jest.fn()
};

global.window.cognitoConfig = {
    identityPoolId: 'us-east-1:test-identity-pool-id'
};

require('../src/cognito-auth.js');

describe('CognitoAuth.initialize', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        global.AWS.config.region = null;
        window.CognitoAuth.userPool = null;
        window.CognitoAuth.identityPool = null;
    });

    test('GivenValidConfig_WhenInitialize_ThenSetsAwsRegion', () => {
        // Arrange
        const config = {
            region: 'us-west-2',
            userPoolId: 'us-west-2_TestPool',
            userPoolClientId: 'test-client-id'
        };

        // Act
        window.CognitoAuth.initialize(config);

        // Assert
        expect(global.AWS.config.region).toBe('us-west-2');
    });

    test('GivenValidConfig_WhenInitialize_ThenCreatesUserPoolWithCorrectParameters', () => {
        // Arrange
        const config = {
            region: 'us-east-1',
            userPoolId: 'us-east-1_TestPool',
            userPoolClientId: 'test-client-id'
        };

        // Act
        window.CognitoAuth.initialize(config);

        // Assert
        expect(global.AmazonCognitoIdentity.CognitoUserPool).toHaveBeenCalledWith({
            UserPoolId: 'us-east-1_TestPool',
            ClientId: 'test-client-id'
        });
    });

    test('GivenValidConfig_WhenInitialize_ThenCreatesIdentityPoolWithRegion', () => {
        // Arrange
        const config = {
            region: 'eu-west-1',
            userPoolId: 'eu-west-1_TestPool',
            userPoolClientId: 'test-client-id'
        };

        // Act
        window.CognitoAuth.initialize(config);

        // Assert
        expect(global.AWS.CognitoIdentity).toHaveBeenCalledWith({
            region: 'eu-west-1'
        });
    });

    test('GivenValidConfig_WhenInitialize_ThenReturnsSuccessMessage', () => {
        // Arrange
        const config = {
            region: 'us-east-1',
            userPoolId: 'us-east-1_TestPool',
            userPoolClientId: 'test-client-id'
        };

        // Act
        const result = window.CognitoAuth.initialize(config);

        // Assert
        expect(result).toBe('Cognito initialized');
    });
});

describe('CognitoAuth.login', () => {
    let mockAuthenticateUser;
    let mockCognitoUser;
    let mockGetAccessToken;
    let mockGetIdToken;
    let originalGetAwsCredentials;

    beforeEach(() => {
        jest.clearAllMocks();

        originalGetAwsCredentials = window.CognitoAuth.getAwsCredentials;

        mockAuthenticateUser = jest.fn();
        mockGetAccessToken = jest.fn(() => ({ getJwtToken: () => 'test-access-token' }));
        mockGetIdToken = jest.fn(() => ({ getJwtToken: () => 'test-id-token' }));

        mockCognitoUser = {
            authenticateUser: mockAuthenticateUser,
            getAccessToken: mockGetAccessToken,
            getIdToken: mockGetIdToken
        };

        global.AmazonCognitoIdentity.CognitoUser.mockReturnValue(mockCognitoUser);
        global.AmazonCognitoIdentity.AuthenticationDetails.mockReturnValue({});

        window.CognitoAuth.userPool = {
            getUserPoolId: () => 'us-east-1_TestPool'
        };

        window.CognitoAuth.getAwsCredentials = jest.fn().mockResolvedValue({
            accessKeyId: 'test-access-key',
            secretAccessKey: 'test-secret-key',
            sessionToken: 'test-session-token'
        });

        global.AWS.config.region = 'us-east-1';
    });

    afterEach(() => {
        window.CognitoAuth.getAwsCredentials = originalGetAwsCredentials;
    });

    test('GivenValidCredentials_WhenLogin_ThenReturnsSuccessWithTokensAndAwsCredentials', async () => {
        // Arrange
        mockAuthenticateUser.mockImplementation((authDetails, callbacks) => {
            callbacks.onSuccess({
                getAccessToken: mockGetAccessToken,
                getIdToken: mockGetIdToken
            });
        });

        // Act
        const result = await window.CognitoAuth.login('testuser', 'testpass');

        // Assert
        expect(result.success).toBe(true);
        expect(result.accessToken).toBe('test-access-token');
        expect(result.idToken).toBe('test-id-token');
        expect(result.awsCredentials.accessKeyId).toBe('test-access-key');
    });

    test('GivenValidCredentials_WhenLogin_ThenCreatesAuthenticationDetails', async () => {
        // Arrange
        mockAuthenticateUser.mockImplementation((authDetails, callbacks) => {
            callbacks.onSuccess({
                getAccessToken: mockGetAccessToken,
                getIdToken: mockGetIdToken
            });
        });

        // Act
        await window.CognitoAuth.login('testuser', 'password123');

        // Assert
        expect(global.AmazonCognitoIdentity.AuthenticationDetails).toHaveBeenCalledWith({
            Username: 'testuser',
            Password: 'password123'
        });
    });

    test('GivenValidCredentials_WhenLogin_ThenCreatesCognitoUser', async () => {
        // Arrange
        mockAuthenticateUser.mockImplementation((authDetails, callbacks) => {
            callbacks.onSuccess({
                getAccessToken: mockGetAccessToken,
                getIdToken: mockGetIdToken
            });
        });

        // Act
        await window.CognitoAuth.login('testuser', 'password123');

        // Assert
        expect(global.AmazonCognitoIdentity.CognitoUser).toHaveBeenCalledWith({
            Username: 'testuser',
            Pool: window.CognitoAuth.userPool
        });
    });

    test('GivenInvalidCredentials_WhenLogin_ThenReturnsFailureWithError', async () => {
        // Arrange
        mockAuthenticateUser.mockImplementation((authDetails, callbacks) => {
            callbacks.onFailure({ message: 'Incorrect username or password.' });
        });

        // Act
        const result = await window.CognitoAuth.login('wronguser', 'wrongpass');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toBe('Incorrect username or password.');
    });

    test('GivenPasswordResetRequired_WhenLogin_ThenReturnsPasswordResetRequiredFlag', async () => {
        // Arrange
        mockAuthenticateUser.mockImplementation((authDetails, callbacks) => {
            callbacks.newPasswordRequired({}, []);
        });

        // Act
        const result = await window.CognitoAuth.login('testuser', 'temppass');

        // Assert
        expect(result.success).toBe(false);
        expect(result.passwordResetRequired).toBe(true);
        expect(result.error).toBe('Password reset required');
    });

    test('GivenPasswordResetRequired_WhenLogin_ThenStoresPendingPasswordResetUser', async () => {
        // Arrange
        mockAuthenticateUser.mockImplementation((authDetails, callbacks) => {
            callbacks.newPasswordRequired({}, []);
        });
        window.CognitoAuth.pendingPasswordResetUser = null;

        // Act
        await window.CognitoAuth.login('testuser', 'temppass');

        // Assert
        expect(window.CognitoAuth.pendingPasswordResetUser).toBe(mockCognitoUser);
    });

    test('GivenAwsCredentialsFailure_WhenLogin_ThenReturnsSuccessWithTokensButNullCredentials', async () => {
        // Arrange
        window.CognitoAuth.getAwsCredentials = jest.fn().mockRejectedValue(new Error('Credentials error'));
        mockAuthenticateUser.mockImplementation((authDetails, callbacks) => {
            callbacks.onSuccess({
                getAccessToken: mockGetAccessToken,
                getIdToken: mockGetIdToken
            });
        });

        // Act
        const result = await window.CognitoAuth.login('testuser', 'testpass');

        // Assert
        expect(result.success).toBe(true);
        expect(result.accessToken).toBe('test-access-token');
        expect(result.awsCredentials).toBe(null);
        expect(result.error).toContain('Failed to get AWS credentials');
    });
});

describe('CognitoAuth.getAwsCredentials', () => {
    let mockRefresh;
    let mockCredentials;
    let originalGetAwsCredentials;

    beforeEach(() => {
        jest.clearAllMocks();

        originalGetAwsCredentials = window.CognitoAuth.getAwsCredentials;

        mockRefresh = jest.fn();
        mockCredentials = {
            accessKeyId: 'test-access-key-id',
            secretAccessKey: 'test-secret-access-key',
            sessionToken: 'test-session-token',
            expireTime: new Date('2026-12-31T23:59:59Z'),
            identityId: 'us-east-1:test-identity-id',
            refresh: mockRefresh
        };

        global.AWS.CognitoIdentityCredentials.mockReturnValue(mockCredentials);
        global.AWS.config.credentials = mockCredentials;
        global.AWS.config.region = 'us-east-1';

        window.CognitoAuth.userPool = {
            getUserPoolId: () => 'us-east-1_TestPool'
        };
    });

    afterEach(() => {
        window.CognitoAuth.getAwsCredentials = originalGetAwsCredentials;
    });

    test('GivenValidIdToken_WhenGetAwsCredentials_ThenCreatesCredentialsWithCorrectLoginKey', async () => {
        // Arrange
        mockRefresh.mockImplementation((callback) => callback(null));

        // Act
        await window.CognitoAuth.getAwsCredentials('test-id-token');

        // Assert
        expect(global.AWS.CognitoIdentityCredentials).toHaveBeenCalledWith({
            IdentityPoolId: 'us-east-1:test-identity-pool-id',
            Logins: {
                'cognito-idp.us-east-1.amazonaws.com/us-east-1_TestPool': 'test-id-token'
            }
        });
    });

    test('GivenValidIdToken_WhenGetAwsCredentials_ThenReturnsCredentialsObject', async () => {
        // Arrange
        mockRefresh.mockImplementation((callback) => callback(null));

        // Act
        const result = await window.CognitoAuth.getAwsCredentials('test-id-token');

        // Assert
        expect(result.accessKeyId).toBe('test-access-key-id');
        expect(result.secretAccessKey).toBe('test-secret-access-key');
        expect(result.sessionToken).toBe('test-session-token');
        expect(result.expiration).toEqual(new Date('2026-12-31T23:59:59Z'));
        expect(result.identityId).toBe('us-east-1:test-identity-id');
    });

    test('GivenRefreshError_WhenGetAwsCredentials_ThenRejectsWithError', async () => {
        // Arrange
        const error = new Error('Refresh failed');
        mockRefresh.mockImplementation((callback) => callback(error));

        // Act & Assert
        await expect(window.CognitoAuth.getAwsCredentials('test-id-token')).rejects.toThrow('Refresh failed');
    });
});

describe('CognitoAuth.logout', () => {
    let mockSignOut;

    beforeEach(() => {
        jest.clearAllMocks();
        mockSignOut = jest.fn();
        window.CognitoAuth.cognitoUser = {
            signOut: mockSignOut,
            getSignInUserSession: () => ({ isValid: () => true })
        };
        global.AWS.config.credentials = { accessKeyId: 'test' };
    });

    test('GivenAuthenticatedUser_WhenLogout_ThenCallsSignOut', () => {
        // Arrange

        // Act
        window.CognitoAuth.logout();

        // Assert
        expect(mockSignOut).toHaveBeenCalled();
    });

    test('GivenAuthenticatedUser_WhenLogout_ThenClearsAwsCredentials', () => {
        // Arrange

        // Act
        window.CognitoAuth.logout();

        // Assert
        expect(global.AWS.config.credentials).toBe(null);
    });

    test('GivenAuthenticatedUser_WhenLogout_ThenReturnsLoggedOutMessage', () => {
        // Arrange

        // Act
        const result = window.CognitoAuth.logout();

        // Assert
        expect(result).toBe('Logged out');
    });

    test('GivenNoAuthenticatedUser_WhenLogout_ThenDoesNotThrowError', () => {
        // Arrange
        window.CognitoAuth.cognitoUser = null;

        // Act & Assert
        expect(() => window.CognitoAuth.logout()).not.toThrow();
    });
});

describe('CognitoAuth.isAuthenticated', () => {
    test('GivenAuthenticatedUserWithValidSession_WhenIsAuthenticated_ThenReturnsTrue', () => {
        // Arrange
        window.CognitoAuth.cognitoUser = {
            getSignInUserSession: () => ({ isValid: () => true })
        };

        // Act
        const result = window.CognitoAuth.isAuthenticated();

        // Assert
        expect(result).toBe(true);
    });

    test('GivenNoAuthenticatedUser_WhenIsAuthenticated_ThenReturnsFalse', () => {
        // Arrange
        window.CognitoAuth.cognitoUser = null;

        // Act
        const result = window.CognitoAuth.isAuthenticated();

        // Assert
        expect(result).toBeFalsy();
    });

    test('GivenAuthenticatedUserWithNullSession_WhenIsAuthenticated_ThenReturnsFalse', () => {
        // Arrange
        window.CognitoAuth.cognitoUser = {
            getSignInUserSession: () => null
        };

        // Act
        const result = window.CognitoAuth.isAuthenticated();

        // Assert
        expect(result).toBe(false);
    });
});

describe('CognitoAuth.completePasswordReset', () => {
    let mockCompleteNewPasswordChallenge;
    let mockGetAccessToken;
    let mockGetIdToken;
    let originalGetAwsCredentials;

    beforeEach(() => {
        jest.clearAllMocks();

        originalGetAwsCredentials = window.CognitoAuth.getAwsCredentials;

        mockCompleteNewPasswordChallenge = jest.fn();
        mockGetAccessToken = jest.fn(() => ({ getJwtToken: () => 'new-access-token' }));
        mockGetIdToken = jest.fn(() => ({ getJwtToken: () => 'new-id-token' }));

        window.CognitoAuth.pendingPasswordResetUser = {
            completeNewPasswordChallenge: mockCompleteNewPasswordChallenge
        };

        window.CognitoAuth.getAwsCredentials = jest.fn().mockResolvedValue({
            accessKeyId: 'test-access-key',
            secretAccessKey: 'test-secret-key',
            sessionToken: 'test-session-token'
        });
    });

    afterEach(() => {
        window.CognitoAuth.getAwsCredentials = originalGetAwsCredentials;
    });

    test('GivenPendingPasswordReset_AndValidNewPassword_WhenCompletePasswordReset_ThenReturnsSuccessWithTokens', async () => {
        // Arrange
        mockCompleteNewPasswordChallenge.mockImplementation((newPassword, attrs, callbacks) => {
            callbacks.onSuccess({
                getAccessToken: mockGetAccessToken,
                getIdToken: mockGetIdToken
            });
        });

        // Act
        const result = await window.CognitoAuth.completePasswordReset('testuser', 'NewPassword123!');

        // Assert
        expect(result.success).toBe(true);
        expect(result.accessToken).toBe('new-access-token');
        expect(result.idToken).toBe('new-id-token');
        expect(result.awsCredentials.accessKeyId).toBe('test-access-key');
    });

    test('GivenPendingPasswordReset_WhenCompletePasswordReset_ThenClearsPendingUser', async () => {
        // Arrange
        mockCompleteNewPasswordChallenge.mockImplementation((newPassword, attrs, callbacks) => {
            callbacks.onSuccess({
                getAccessToken: mockGetAccessToken,
                getIdToken: mockGetIdToken
            });
        });

        // Act
        await window.CognitoAuth.completePasswordReset('testuser', 'NewPassword123!');

        // Assert
        expect(window.CognitoAuth.pendingPasswordResetUser).toBe(null);
    });

    test('GivenNoPendingPasswordReset_WhenCompletePasswordReset_ThenReturnsFailure', async () => {
        // Arrange
        window.CognitoAuth.pendingPasswordResetUser = null;

        // Act
        const result = await window.CognitoAuth.completePasswordReset('testuser', 'NewPassword123!');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toBe('No pending password reset found');
    });

    test('GivenPasswordResetFailure_WhenCompletePasswordReset_ThenReturnsFailureAndClearsPendingUser', async () => {
        // Arrange
        mockCompleteNewPasswordChallenge.mockImplementation((newPassword, attrs, callbacks) => {
            callbacks.onFailure({ message: 'Invalid password format' });
        });

        // Act
        const result = await window.CognitoAuth.completePasswordReset('testuser', 'weak');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toContain('Invalid password format');
        expect(window.CognitoAuth.pendingPasswordResetUser).toBe(null);
    });

    test('GivenAwsCredentialsFailure_WhenCompletePasswordReset_ThenReturnsSuccessWithNullCredentials', async () => {
        // Arrange
        window.CognitoAuth.getAwsCredentials = jest.fn().mockRejectedValue(new Error('Credentials error'));
        mockCompleteNewPasswordChallenge.mockImplementation((newPassword, attrs, callbacks) => {
            callbacks.onSuccess({
                getAccessToken: mockGetAccessToken,
                getIdToken: mockGetIdToken
            });
        });

        // Act
        const result = await window.CognitoAuth.completePasswordReset('testuser', 'NewPassword123!');

        // Assert
        expect(result.success).toBe(true);
        expect(result.awsCredentials).toBe(null);
        expect(result.error).toContain('Failed to get AWS credentials');
    });
});

describe('CognitoAuth.forgotPassword', () => {
    let mockForgotPassword;
    let mockCognitoUser;

    beforeEach(() => {
        jest.clearAllMocks();
        mockForgotPassword = jest.fn();
        mockCognitoUser = {
            forgotPassword: mockForgotPassword
        };

        global.AmazonCognitoIdentity.CognitoUser.mockReturnValue(mockCognitoUser);
        window.CognitoAuth.userPool = { getUserPoolId: () => 'test-pool' };
    });

    test('GivenValidUsername_WhenForgotPassword_ThenReturnsSuccessWithMessage', async () => {
        // Arrange
        mockForgotPassword.mockImplementation((callbacks) => {
            callbacks.onSuccess({ CodeDeliveryDetails: { DeliveryMedium: 'EMAIL' } });
        });

        // Act
        const result = await window.CognitoAuth.forgotPassword('testuser');

        // Assert
        expect(result.success).toBe(true);
        expect(result.message).toContain('Verification code sent');
    });

    test('GivenValidUsername_WhenForgotPassword_ThenCreatesCognitoUser', async () => {
        // Arrange
        mockForgotPassword.mockImplementation((callbacks) => {
            callbacks.onSuccess({});
        });

        // Act
        await window.CognitoAuth.forgotPassword('testuser');

        // Assert
        expect(global.AmazonCognitoIdentity.CognitoUser).toHaveBeenCalledWith({
            Username: 'testuser',
            Pool: window.CognitoAuth.userPool
        });
    });

    test('GivenInvalidUsername_WhenForgotPassword_ThenReturnsFailureWithError', async () => {
        // Arrange
        mockForgotPassword.mockImplementation((callbacks) => {
            callbacks.onFailure({ message: 'User not found' });
        });

        // Act
        const result = await window.CognitoAuth.forgotPassword('nonexistent');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toBe('User not found');
    });
});

describe('CognitoAuth.confirmForgotPassword', () => {
    let mockConfirmPassword;
    let mockCognitoUser;

    beforeEach(() => {
        jest.clearAllMocks();
        mockConfirmPassword = jest.fn();
        mockCognitoUser = {
            confirmPassword: mockConfirmPassword
        };

        global.AmazonCognitoIdentity.CognitoUser.mockReturnValue(mockCognitoUser);
        window.CognitoAuth.userPool = { getUserPoolId: () => 'test-pool' };
    });

    test('GivenValidCodeAndPassword_WhenConfirmForgotPassword_ThenReturnsSuccessWithMessage', async () => {
        // Arrange
        mockConfirmPassword.mockImplementation((code, password, callbacks) => {
            callbacks.onSuccess();
        });

        // Act
        const result = await window.CognitoAuth.confirmForgotPassword('testuser', '123456', 'NewPassword123!');

        // Assert
        expect(result.success).toBe(true);
        expect(result.message).toContain('Password reset successfully');
    });

    test('GivenValidParameters_WhenConfirmForgotPassword_ThenCallsConfirmPasswordWithCorrectArguments', async () => {
        // Arrange
        mockConfirmPassword.mockImplementation((code, password, callbacks) => {
            callbacks.onSuccess();
        });

        // Act
        await window.CognitoAuth.confirmForgotPassword('testuser', '123456', 'NewPassword123!');

        // Assert
        expect(mockConfirmPassword).toHaveBeenCalledWith('123456', 'NewPassword123!', expect.any(Object));
    });

    test('GivenInvalidCode_WhenConfirmForgotPassword_ThenReturnsFailureWithError', async () => {
        // Arrange
        mockConfirmPassword.mockImplementation((code, password, callbacks) => {
            callbacks.onFailure({ message: 'Invalid verification code' });
        });

        // Act
        const result = await window.CognitoAuth.confirmForgotPassword('testuser', '000000', 'NewPassword123!');

        // Assert
        expect(result.success).toBe(false);
        expect(result.error).toBe('Invalid verification code');
    });

    test('GivenValidParameters_WhenConfirmForgotPassword_ThenCreatesCognitoUser', async () => {
        // Arrange
        mockConfirmPassword.mockImplementation((code, password, callbacks) => {
            callbacks.onSuccess();
        });

        // Act
        await window.CognitoAuth.confirmForgotPassword('testuser', '123456', 'NewPassword123!');

        // Assert
        expect(global.AmazonCognitoIdentity.CognitoUser).toHaveBeenCalledWith({
            Username: 'testuser',
            Pool: window.CognitoAuth.userPool
        });
    });
});
