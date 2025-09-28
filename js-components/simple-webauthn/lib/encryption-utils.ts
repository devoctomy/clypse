/**
 * Encryption utilities using Web Crypto API
 */
export class EncryptionUtils {
  
  /**
   * Encrypt plaintext using AES-GCM with HKDF key derivation
   * Note: salt parameter is for PRF extension, HKDF uses fixed salt for compatibility
   */
  static async encryptData(
    plaintext: string, 
    keyMaterial: CryptoKey, 
    _salt: string // PRF salt, not used in HKDF for compatibility with working code
  ): Promise<{success: boolean, encryptedData?: string, error?: string}> {
    try {
      // Derive AES-GCM key from the key material using HKDF (exact format from working code)
      const aesKey = await crypto.subtle.deriveKey(
        {
          name: "HKDF",
          hash: "SHA-256",
          salt: new TextEncoder().encode("clypse-encryption-salt-v1"),
          info: new TextEncoder().encode("clypse-encryption-key")
        },
        keyMaterial,
        { name: "AES-GCM", length: 256 },
        false,
        ["encrypt"]
      );

      // Generate random IV
      const iv = crypto.getRandomValues(new Uint8Array(12));
      
      // Encrypt the plaintext
      const plaintextBytes = new TextEncoder().encode(plaintext);
      const encryptedData = await crypto.subtle.encrypt(
        { name: "AES-GCM", iv: iv },
        aesKey,
        plaintextBytes
      );

      // Combine IV and encrypted data
      const combined = new Uint8Array(iv.length + encryptedData.byteLength);
      combined.set(iv, 0);
      combined.set(new Uint8Array(encryptedData), iv.length);

      // Convert to base64
      const base64 = btoa(String.fromCharCode(...combined));

      return {
        success: true,
        encryptedData: base64
      };
      
    } catch (error) {
      return {
        success: false,
        error: `Encryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`
      };
    }
  }
  
  /**
   * Decrypt data using AES-GCM with HKDF key derivation
   * Note: salt parameter is for PRF extension, HKDF uses fixed salt for compatibility
   */
  static async decryptData(
    encryptedDataBase64: string,
    keyMaterial: CryptoKey,
    _salt: string // PRF salt, not used in HKDF for compatibility with working code
  ): Promise<{success: boolean, plaintext?: string, error?: string}> {
    try {
      // Derive AES-GCM key from the key material (same as encryption - exact format from working code)
      const aesKey = await crypto.subtle.deriveKey(
        {
          name: "HKDF",
          hash: "SHA-256",
          salt: new TextEncoder().encode("clypse-encryption-salt-v1"),
          info: new TextEncoder().encode("clypse-encryption-key")
        },
        keyMaterial,
        { name: "AES-GCM", length: 256 },
        false,
        ["decrypt"]
      );

      // Decode base64
      const encryptedBytes = Uint8Array.from(atob(encryptedDataBase64), c => c.charCodeAt(0));
      
      // Extract IV and encrypted data
      const iv = encryptedBytes.slice(0, 12);
      const encryptedData = encryptedBytes.slice(12);
      
      // Decrypt the data
      const decryptedData = await crypto.subtle.decrypt(
        { name: "AES-GCM", iv: iv },
        aesKey,
        encryptedData
      );

      // Convert decrypted bytes to text
      const plaintext = new TextDecoder().decode(decryptedData);

      return {
        success: true,
        plaintext: plaintext
      };
      
    } catch (error) {
      return {
        success: false,
        error: `Decryption failed: ${error instanceof Error ? error.message : 'Unknown error'}`
      };
    }
  }
  
  /**
   * Import raw bytes as HKDF key material
   */
  static async importKeyMaterial(keyBytes: ArrayBuffer): Promise<CryptoKey> {
    return await crypto.subtle.importKey(
      "raw",
      keyBytes,
      "HKDF",
      false,
      ["deriveKey"]
    );
  }
}