# AWS Cognito Setup Instructions

This document outlines the steps needed to set up AWS Cognito for the Clypse Portal application.

## Prerequisites

1. AWS Account with appropriate permissions to create Cognito resources
2. AWS CLI configured (optional but recommended)

## Step 1: Create a Cognito User Pool

1. Go to the AWS Console and navigate to Amazon Cognito
2. Click "Create user pool"
3. Configure the following settings:

### Authentication providers
- Select "Cognito user pool"

### Cognito user pool sign-in options
- Check "Username" 
- Optionally check "Email" if you want email-based login

### Security requirements
- Password policy: Use default or customize as needed
- Multi-factor authentication: Optional (can be set to "Optional" or "Required")

### Sign-up experience
- Configure as needed for your use case
- For testing, you can disable email verification

### Message delivery
- For testing: Choose "Send email with Cognito"
- For production: Configure SES if needed

### Review and create
- Give your user pool a name (e.g., "clypse-user-pool")
- Click "Create user pool"

4. **Important**: Note down the following values:
   - User Pool ID (format: us-east-1_xxxxxxxxx)
   - User Pool ARN

## Step 2: Create a User Pool Client

1. In your newly created User Pool, go to the "App integration" tab
2. Scroll down to "App clients and analytics"
3. Click "Create app client"
4. Configure the following:

### App client information
- App client name: "clypse-portal-client"
- Client secret: **DO NOT** generate a client secret (leave unchecked)

### Authentication flows
- Check "ALLOW_USER_PASSWORD_AUTH"
- Check "ALLOW_REFRESH_TOKEN_AUTH"
- Uncheck others unless specifically needed

### Refresh token expiration
- Set as needed (default 30 days is usually fine)

### Access token expiration
- Set as needed (default 60 minutes is usually fine)

### ID token expiration
- Set as needed (default 60 minutes is usually fine)

5. Click "Create app client"
6. **Important**: Note down the Client ID

## Step 3: Create a Cognito Identity Pool

1. Go back to the main Cognito console
2. Click "Create identity pool"
3. Configure the following:

### Identity pool configuration
- Identity pool name: "clypse-identity-pool"
- Enable access to unauthenticated identities: **No** (uncheck this)

### Authentication providers
- Select "Cognito"
- User pool ID: Enter the User Pool ID from Step 1
- App client ID: Enter the Client ID from Step 2

### Roles
- AWS will automatically create roles for authenticated users
- You can customize these roles later if needed

4. Click "Create identity pool"
5. **Important**: Note down the Identity Pool ID

## Step 4: Configure IAM Roles (Optional)

The automatically created roles should work for basic functionality, but you may want to customize them:

1. Go to IAM in the AWS Console
2. Find the roles created by Cognito (they'll have names like "Cognito_clypseidentitypool_Auth_Role")
3. Attach additional policies as needed for your application

## Step 5: Update Configuration

Update the `appsettings.json` file in your `wwwroot` folder with the values you collected:

```json
{
  "AwsCognito": {
    "UserPoolId": "us-east-1_xxxxxxxxx",
    "UserPoolClientId": "your-client-id-here",
    "Region": "us-east-1",
    "IdentityPoolId": "us-east-1:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  }
}
```

Replace the placeholder values with your actual AWS Cognito configuration.

## Step 6: Create Test Users

1. Go to your User Pool in the AWS Console
2. Navigate to "Users" tab
3. Click "Create user"
4. Fill in the required information:
   - Username: Choose a username
   - Password: Set a temporary password
   - Email: Provide an email (if email is enabled)
5. Click "Create user"

### Setting Permanent Password

If you set a temporary password, the user will need to change it on first login. For testing purposes, you can:

1. Use the AWS CLI to set a permanent password:
   ```bash
   aws cognito-idp admin-set-user-password \
     --user-pool-id us-east-1_xxxxxxxxx \
     --username your-test-username \
     --password your-permanent-password \
     --permanent
   ```

## Step 7: Test the Application

1. Build and run your Blazor WebAssembly application
2. Navigate to the home page
3. Use the test credentials you created to log in
4. Verify that AWS credentials are displayed after successful login

## Troubleshooting

### Common Issues

1. **"User does not exist" error**: Make sure the user is created in the correct User Pool
2. **"NotAuthorized" error**: Check that the User Pool Client allows USER_PASSWORD_AUTH
3. **"Invalid ClientId" error**: Verify the Client ID in your configuration
4. **CORS errors**: Make sure your Blazor app domain is allowed (this shouldn't be an issue for local development)

### Enabling Debug Logging

You can add logging to help debug issues. Add this to your `Program.cs`:

```csharp
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

## Security Considerations

1. **Never commit real AWS credentials** to version control
2. Use environment variables or secure configuration management for production
3. Implement proper token refresh logic for production applications
4. Consider implementing logout functionality that invalidates tokens
5. Review IAM permissions regularly and follow the principle of least privilege

## Next Steps

1. Implement token refresh functionality
2. Add proper error handling and user feedback
3. Implement secure storage for tokens in the browser
4. Add logout functionality
5. Consider implementing additional authentication flows (social login, etc.)
