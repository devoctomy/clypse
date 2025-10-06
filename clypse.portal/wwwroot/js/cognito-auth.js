window.CognitoAuth = {
    userPool: null,
    cognitoUser: null,
    identityPool: null,
    pendingPasswordResetUser: null,
    
    initialize: function(config) {
        AWS.config.region = config.region;
        
        var poolData = {
            UserPoolId: config.userPoolId,
            ClientId: config.userPoolClientId
        };
        
        this.userPool = new AmazonCognitoIdentity.CognitoUserPool(poolData);
        this.identityPool = new AWS.CognitoIdentity({
            region: config.region
        });
        
        return "Cognito initialized";
    },
    
    login: async function(username, password) {
        return new Promise((resolve, reject) => {
            var authenticationData = {
                Username: username,
                Password: password,
            };
            
            var authenticationDetails = new AmazonCognitoIdentity.AuthenticationDetails(authenticationData);
            
            var userData = {
                Username: username,
                Pool: this.userPool
            };
            
            this.cognitoUser = new AmazonCognitoIdentity.CognitoUser(userData);
            
            this.cognitoUser.authenticateUser(authenticationDetails, {
                onSuccess: async (result) => {
                    console.log('CognitoAuth.login: Authentication successful');
                    var accessToken = result.getAccessToken().getJwtToken();
                    var idToken = result.getIdToken().getJwtToken();
                    
                    try {
                        // Get AWS credentials
                        console.log('CognitoAuth.login: Attempting to get AWS credentials');
                        var credentials = await this.getAwsCredentials(idToken);
                        console.log('CognitoAuth.login: AWS credentials received:', credentials);
                        
                        resolve({
                            success: true,
                            accessToken: accessToken,
                            idToken: idToken,
                            awsCredentials: credentials
                        });
                    } catch (error) {
                        console.error('CognitoAuth.login: Failed to get AWS credentials:', error);
                        resolve({
                            success: true,
                            accessToken: accessToken,
                            idToken: idToken,
                            awsCredentials: null,
                            error: "Failed to get AWS credentials: " + error.message
                        });
                    }
                },
                onFailure: (err) => {
                    resolve({
                        success: false,
                        error: err.message || err
                    });
                },
                newPasswordRequired: (userAttributes, requiredAttributes) => {
                    // Store the cognitoUser for later password completion
                    this.pendingPasswordResetUser = this.cognitoUser;
                    
                    // User needs to set a new password - return flag to UI
                    resolve({
                        success: false,
                        passwordResetRequired: true,
                        error: "Password reset required"
                    });
                }
            });
        });
    },
    
    getAwsCredentials: function(idToken) {
        return new Promise((resolve, reject) => {      
            var loginKey = `cognito-idp.${AWS.config.region}.amazonaws.com/${this.userPool.getUserPoolId()}`;
            var loginData = {};
            loginData[loginKey] = idToken;
            
            AWS.config.credentials = new AWS.CognitoIdentityCredentials({
                IdentityPoolId: window.cognitoConfig.identityPoolId,
                Logins: loginData
            });
            
            AWS.config.credentials.refresh((error) => {
                if (error) {
                    console.error('CognitoAuth.getAwsCredentials: Error during refresh:', error);
                    reject(error);
                } else {
                    console.log('CognitoAuth.getAwsCredentials: Credentials refreshed successfully');
                    console.log('CognitoAuth.getAwsCredentials: AccessKeyId:', AWS.config.credentials.accessKeyId);
                    console.log('CognitoAuth.getAwsCredentials: IdentityId:', AWS.config.credentials.identityId);
                    console.log('CognitoAuth.getAwsCredentials: ExpireTime:', AWS.config.credentials.expireTime);
                    
                    resolve({
                        accessKeyId: AWS.config.credentials.accessKeyId,
                        secretAccessKey: AWS.config.credentials.secretAccessKey,
                        sessionToken: AWS.config.credentials.sessionToken,
                        expiration: AWS.config.credentials.expireTime,
                        identityId: AWS.config.credentials.identityId || ''
                    });
                }
            });
        });
    },
    
    logout: function() {
        if (this.cognitoUser) {
            this.cognitoUser.signOut();
        }
        AWS.config.credentials = null;
        
        
        return "Logged out";
    },
    
    isAuthenticated: function() {
        return this.cognitoUser && this.cognitoUser.getSignInUserSession() !== null;
    },

    completePasswordReset: function(username, newPassword) {
        return new Promise((resolve, reject) => {
            if (!this.pendingPasswordResetUser) {
                resolve({
                    success: false,
                    error: "No pending password reset found"
                });
                return;
            }

            this.pendingPasswordResetUser.completeNewPasswordChallenge(newPassword, {}, {
                onSuccess: async (result) => {
                    console.log('CognitoAuth.completePasswordReset: Password reset successful');
                    var accessToken = result.getAccessToken().getJwtToken();
                    var idToken = result.getIdToken().getJwtToken();
                    
                    // Clear the pending user
                    this.pendingPasswordResetUser = null;
                    this.cognitoUser = result.user || this.cognitoUser;
                    
                    try {
                        // Get AWS credentials
                        console.log('CognitoAuth.completePasswordReset: Attempting to get AWS credentials');
                        var credentials = await this.getAwsCredentials(idToken);
                        console.log('CognitoAuth.completePasswordReset: AWS credentials received:', credentials);
                        
                        resolve({
                            success: true,
                            accessToken: accessToken,
                            idToken: idToken,
                            awsCredentials: credentials
                        });
                    } catch (error) {
                        console.error('CognitoAuth.completePasswordReset: Failed to get AWS credentials:', error);
                        resolve({
                            success: true,
                            accessToken: accessToken,
                            idToken: idToken,
                            awsCredentials: null,
                            error: "Failed to get AWS credentials: " + error.message
                        });
                    }
                },
                onFailure: (err) => {
                    console.error('CognitoAuth.completePasswordReset: Password reset failed:', err);
                    this.pendingPasswordResetUser = null;
                    resolve({
                        success: false,
                        error: "Failed to reset password: " + (err.message || err)
                    });
                }
            });
        });
    },

    forgotPassword: function(username) {
        return new Promise((resolve, reject) => {
            var userData = {
                Username: username,
                Pool: this.userPool
            };
            
            var cognitoUser = new AmazonCognitoIdentity.CognitoUser(userData);
            
            cognitoUser.forgotPassword({
                onSuccess: (result) => {
                    console.log('CognitoAuth.forgotPassword: Success - code delivery details:', result);
                    resolve({
                        success: true,
                        message: "Verification code sent to your registered email/phone number",
                        codeDeliveryDetails: result
                    });
                },
                onFailure: (err) => {
                    console.error('CognitoAuth.forgotPassword: Error:', err);
                    resolve({
                        success: false,
                        error: err.message || err
                    });
                }
            });
        });
    },

    confirmForgotPassword: function(username, verificationCode, newPassword) {
        return new Promise((resolve, reject) => {
            var userData = {
                Username: username,
                Pool: this.userPool
            };
            
            var cognitoUser = new AmazonCognitoIdentity.CognitoUser(userData);
            
            cognitoUser.confirmPassword(verificationCode, newPassword, {
                onSuccess: () => {
                    console.log('CognitoAuth.confirmForgotPassword: Password reset successful');
                    resolve({
                        success: true,
                        message: "Password reset successfully. You can now login with your new password."
                    });
                },
                onFailure: (err) => {
                    console.error('CognitoAuth.confirmForgotPassword: Error:', err);
                    resolve({
                        success: false,
                        error: err.message || err
                    });
                }
            });
        });
    }
};
