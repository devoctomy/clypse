/**
 * Input validation utilities for SimpleWebAuthn
 */
export class InputValidator {
    /**
     * Validate createCredential options
     */
    static validateCreateOptions(options) {
        // Check required fields
        if (!options.rpName || options.rpName.trim().length === 0) {
            return { valid: false, error: "rpName is required and cannot be empty" };
        }
        if (!options.userName || options.userName.trim().length === 0) {
            return { valid: false, error: "userName is required and cannot be empty" };
        }
        if (!options.userDisplayName || options.userDisplayName.trim().length === 0) {
            return { valid: false, error: "userDisplayName is required and cannot be empty" };
        }
        // Validate optional rpId if provided
        if (options.rpId !== undefined) {
            if (typeof options.rpId !== 'string' || options.rpId.trim().length === 0) {
                return { valid: false, error: "rpId must be a non-empty string if provided" };
            }
            // Basic validation - must be a valid hostname format
            const rpIdRegex = /^[a-zA-Z0-9.-]+$/;
            if (!rpIdRegex.test(options.rpId)) {
                return { valid: false, error: "rpId must be a valid hostname format" };
            }
        }
        // Validate timeout if provided
        if (options.timeout !== undefined) {
            if (typeof options.timeout !== 'number' || options.timeout <= 0 || options.timeout > 600000) {
                return { valid: false, error: "timeout must be a positive number between 1 and 600000 (10 minutes)" };
            }
        }
        // Validate userVerification if provided
        if (options.userVerification !== undefined) {
            const validValues = ["required", "preferred", "discouraged"];
            if (!validValues.includes(options.userVerification)) {
                return { valid: false, error: "userVerification must be 'required', 'preferred', or 'discouraged'" };
            }
        }
        // Validate authenticatorAttachment if provided
        if (options.authenticatorAttachment !== undefined) {
            const validValues = ["platform", "cross-platform"];
            if (!validValues.includes(options.authenticatorAttachment)) {
                return { valid: false, error: "authenticatorAttachment must be 'platform' or 'cross-platform'" };
            }
        }
        // Validate residentKey if provided
        if (options.residentKey !== undefined) {
            const validValues = ["required", "preferred", "discouraged"];
            if (!validValues.includes(options.residentKey)) {
                return { valid: false, error: "residentKey must be 'required', 'preferred', or 'discouraged'" };
            }
        }
        // Validate encryption salt if provided
        if (options.encryptionSalt !== undefined) {
            if (typeof options.encryptionSalt !== 'string' || options.encryptionSalt.trim().length === 0) {
                return { valid: false, error: "encryptionSalt must be a non-empty string if provided" };
            }
        }
        return { valid: true };
    }
    /**
     * Validate authenticate options
     */
    static validateAuthOptions(options) {
        // Check required fields
        if (!options.credentialId || options.credentialId.trim().length === 0) {
            return { valid: false, error: "credentialId is required and cannot be empty" };
        }
        // Validate credentialId format (should be base64)
        try {
            atob(options.credentialId);
        }
        catch (error) {
            return { valid: false, error: "credentialId must be a valid base64 string" };
        }
        // Validate timeout if provided
        if (options.timeout !== undefined) {
            if (typeof options.timeout !== 'number' || options.timeout <= 0 || options.timeout > 600000) {
                return { valid: false, error: "timeout must be a positive number between 1 and 600000 (10 minutes)" };
            }
        }
        // Validate userVerification if provided
        if (options.userVerification !== undefined) {
            const validValues = ["required", "preferred", "discouraged"];
            if (!validValues.includes(options.userVerification)) {
                return { valid: false, error: "userVerification must be 'required', 'preferred', or 'discouraged'" };
            }
        }
        // Validate encryptedData if provided (should be base64)
        if (options.encryptedData !== undefined) {
            if (typeof options.encryptedData !== 'string' || options.encryptedData.trim().length === 0) {
                return { valid: false, error: "encryptedData must be a non-empty string if provided" };
            }
            try {
                atob(options.encryptedData);
            }
            catch (error) {
                return { valid: false, error: "encryptedData must be a valid base64 string" };
            }
        }
        // Validate encryption salt if provided
        if (options.encryptionSalt !== undefined) {
            if (typeof options.encryptionSalt !== 'string' || options.encryptionSalt.trim().length === 0) {
                return { valid: false, error: "encryptionSalt must be a non-empty string if provided" };
            }
        }
        return { valid: true };
    }
}
//# sourceMappingURL=input-validator.js.map