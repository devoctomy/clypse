/**
 * Encryption utilities using Web Crypto API
 */
export declare class EncryptionUtils {
    /**
     * Encrypt plaintext using AES-GCM with HKDF key derivation
     * Note: salt parameter is for PRF extension, HKDF uses fixed salt for compatibility
     */
    static encryptData(plaintext: string, keyMaterial: CryptoKey, _salt: string): Promise<{
        success: boolean;
        encryptedData?: string;
        error?: string;
    }>;
    /**
     * Decrypt data using AES-GCM with HKDF key derivation
     * Note: salt parameter is for PRF extension, HKDF uses fixed salt for compatibility
     */
    static decryptData(encryptedDataBase64: string, keyMaterial: CryptoKey, _salt: string): Promise<{
        success: boolean;
        plaintext?: string;
        error?: string;
    }>;
    /**
     * Import raw bytes as HKDF key material
     */
    static importKeyMaterial(keyBytes: ArrayBuffer): Promise<CryptoKey>;
}
//# sourceMappingURL=encryption-utils.d.ts.map