window.CognitoAuth = {
    userPool: null,
    cognitoUser: null,
    identityPool: null,
    
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
                    var accessToken = result.getAccessToken().getJwtToken();
                    var idToken = result.getIdToken().getJwtToken();
                    
                    try {
                        // Get AWS credentials
                        var credentials = await this.getAwsCredentials(idToken);
                        
                        resolve({
                            success: true,
                            accessToken: accessToken,
                            idToken: idToken,
                            awsCredentials: credentials
                        });
                    } catch (error) {
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
                    // User needs to set a new password
                    // For simplicity, we'll use the same password as the new password
                    this.cognitoUser.completeNewPasswordChallenge(password, {}, {
                        onSuccess: async (result) => {
                            var accessToken = result.getAccessToken().getJwtToken();
                            var idToken = result.getIdToken().getJwtToken();
                            
                            try {
                                // Get AWS credentials
                                var credentials = await this.getAwsCredentials(idToken);
                                
                                resolve({
                                    success: true,
                                    accessToken: accessToken,
                                    idToken: idToken,
                                    awsCredentials: credentials
                                });
                            } catch (error) {
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
                                error: "Failed to set new password: " + (err.message || err)
                            });
                        }
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
                    reject(error);
                } else {
                    resolve({
                        accessKeyId: AWS.config.credentials.accessKeyId,
                        secretAccessKey: AWS.config.credentials.secretAccessKey,
                        sessionToken: AWS.config.credentials.sessionToken,
                        expiration: AWS.config.credentials.expireTime
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
        
        // Clear ALL localStorage data
        localStorage.clear();
        
        return "Logged out";
    },
    
    isAuthenticated: function() {
        return this.cognitoUser && this.cognitoUser.getSignInUserSession() !== null;
    }
};
