import { CreateCredentialOptions, AuthenticateOptions } from './types.js';
/**
 * Input validation utilities for SimpleWebAuthn
 */
export declare class InputValidator {
    /**
     * Validate createCredential options
     */
    static validateCreateOptions(options: CreateCredentialOptions): {
        valid: boolean;
        error?: string;
    };
    /**
     * Validate authenticate options
     */
    static validateAuthOptions(options: AuthenticateOptions): {
        valid: boolean;
        error?: string;
    };
}
//# sourceMappingURL=input-validator.d.ts.map