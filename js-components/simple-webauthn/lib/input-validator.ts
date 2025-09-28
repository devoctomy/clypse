import { CreateCredentialOptions, AuthenticateOptions } from './types.js';

/**
 * Input validation utilities for SimpleWebAuthn
 */
export class InputValidator {
  
  /**
   * Validate createCredential options
   */
  static validateCreateOptions(options: CreateCredentialOptions): {valid: boolean, error?: string} {
    // Check required rp object
    if (!options.rp || typeof options.rp !== 'object') {
      return { valid: false, error: "rp object is required" };
    }
    
    if (!options.rp.name || options.rp.name.trim().length === 0) {
      return { valid: false, error: "rp.name is required and cannot be empty" };
    }
    
    // Check required user object
    if (!options.user || typeof options.user !== 'object') {
      return { valid: false, error: "user object is required" };
    }
    
    if (!options.user.id || options.user.id.trim().length === 0) {
      return { valid: false, error: "user.id is required and cannot be empty" };
    }
    
    if (!options.user.name || options.user.name.trim().length === 0) {
      return { valid: false, error: "user.name is required and cannot be empty" };
    }
    
    if (!options.user.displayName || options.user.displayName.trim().length === 0) {
      return { valid: false, error: "user.displayName is required and cannot be empty" };
    }
    
    // Check required challenge
    if (!options.challenge || options.challenge.trim().length === 0) {
      return { valid: false, error: "challenge is required and cannot be empty" };
    }
    
    // Check required pubKeyCredParams
    if (!Array.isArray(options.pubKeyCredParams) || options.pubKeyCredParams.length === 0) {
      return { valid: false, error: "pubKeyCredParams array is required and cannot be empty" };
    }
    
    // Check required encryptionSalt
    if (!options.encryptionSalt || options.encryptionSalt.trim().length === 0) {
      return { valid: false, error: "encryptionSalt is required and cannot be empty" };
    }
    
    // Validate optional rpId if provided
    if (options.rp.id !== undefined) {
      if (typeof options.rp.id !== 'string' || options.rp.id.trim().length === 0) {
        return { valid: false, error: "rp.id must be a non-empty string if provided" };
      }
      
      // Basic validation - must be a valid hostname format
      const rpIdRegex = /^[a-zA-Z0-9.-]+$/;
      if (!rpIdRegex.test(options.rp.id)) {
        return { valid: false, error: "rp.id must be a valid hostname format" };
      }
    }
    
    // Validate timeout if provided
    if (options.timeout !== undefined) {
      if (typeof options.timeout !== 'number' || options.timeout <= 0 || options.timeout > 600000) {
        return { valid: false, error: "timeout must be a positive number between 1 and 600000 (10 minutes)" };
      }
    }
    
    // Validate authenticatorSelection if provided
    if (options.authenticatorSelection !== undefined) {
      if (options.authenticatorSelection.userVerification !== undefined) {
        const validValues = ["required", "preferred", "discouraged"];
        if (!validValues.includes(options.authenticatorSelection.userVerification)) {
          return { valid: false, error: "authenticatorSelection.userVerification must be 'required', 'preferred', or 'discouraged'" };
        }
      }
      
      if (options.authenticatorSelection.authenticatorAttachment !== undefined) {
        const validValues = ["platform", "cross-platform"];
        if (!validValues.includes(options.authenticatorSelection.authenticatorAttachment)) {
          return { valid: false, error: "authenticatorSelection.authenticatorAttachment must be 'platform' or 'cross-platform'" };
        }
      }
      
      if (options.authenticatorSelection.residentKey !== undefined) {
        const validValues = ["required", "preferred", "discouraged"];
        if (!validValues.includes(options.authenticatorSelection.residentKey)) {
          return { valid: false, error: "authenticatorSelection.residentKey must be 'required', 'preferred', or 'discouraged'" };
        }
      }
    }
    
    return { valid: true };
  }
  
  /**
   * Validate authenticate options
   */
  static validateAuthOptions(options: AuthenticateOptions): {valid: boolean, error?: string} {
    // Check required challenge
    if (!options.challenge || options.challenge.trim().length === 0) {
      return { valid: false, error: "challenge is required and cannot be empty" };
    }
    
    // Check required allowCredentials
    if (!Array.isArray(options.allowCredentials) || options.allowCredentials.length === 0) {
      return { valid: false, error: "allowCredentials array is required and cannot be empty" };
    }
    
    // Validate each credential descriptor
    for (const cred of options.allowCredentials) {
      if (!cred.id || cred.id.trim().length === 0) {
        return { valid: false, error: "allowCredentials[].id is required and cannot be empty" };
      }
      
      if (cred.type !== "public-key") {
        return { valid: false, error: "allowCredentials[].type must be 'public-key'" };
      }
      
      // Validate credential ID is valid base64
      try {
        atob(cred.id);
      } catch (e) {
        return { valid: false, error: "allowCredentials[].id must be a valid base64 string" };
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
    
    // Check required encryptionSalt
    if (!options.encryptionSalt || options.encryptionSalt.trim().length === 0) {
      return { valid: false, error: "encryptionSalt is required and cannot be empty" };
    }
    
    return { valid: true };
  }
}