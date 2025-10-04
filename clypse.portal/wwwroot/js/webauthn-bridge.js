/**
 * JavaScript bridge for WebAuthn wrapper in Blazor
 */
window.webAuthnWrapper = (function() {
    let webAuthnWrapper;

    function initWrapper() {
        if (!webAuthnWrapper) {
            if (typeof WebAuthnWrapper === 'undefined') {
                throw new Error('WebAuthnWrapper not loaded. Please include webauthn-wrapper.js');
            }
            webAuthnWrapper = new WebAuthnWrapper();
        }
        return webAuthnWrapper;
    }

    async function checkSupport() {
        try {
            const wrapper = initWrapper();
            return await wrapper.checkSupport();
        } catch (error) {
            return {
                supported: false,
                platformAuthenticator: false,
                error: error.message
            };
        }
    }

    async function register(username, existingCredentialId = null) {
        try {
            const wrapper = initWrapper();
            const result = await wrapper.register(username, existingCredentialId);
            
            return {
                success: true,
                credentialID: result.credentialID,
                userID: result.userID,
                username: result.username,
                prfEnabled: result.prfEnabled,
                error: null
            };
        } catch (error) {
            return {
                success: false,
                credentialID: null,
                userID: null,
                username: null,
                prfEnabled: false,
                error: error.message
            };
        }
    }

    async function authenticate(credentialID) {
        try {
            const wrapper = initWrapper();
            const result = await wrapper.authenticate(credentialID);
            
            return {
                success: true,
                userPresent: result.userPresent,
                userVerified: result.userVerified,
                prfOutput: result.prfOutput,
                error: null
            };
        } catch (error) {
            return {
                success: false,
                userPresent: false,
                userVerified: false,
                prfOutput: null,
                error: error.message
            };
        }
    }

    // Public API
    return {
        checkSupport: checkSupport,
        register: register,
        authenticate: authenticate
    };
})();