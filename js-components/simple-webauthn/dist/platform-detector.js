/**
 * Platform detection utilities for WebAuthn optimization
 */
export class PlatformDetector {
    /**
     * Detect current platform and return optimized configuration
     */
    static detectPlatform() {
        const userAgent = navigator.userAgent;
        const isSamsung = userAgent.includes('Samsung') || userAgent.includes('SM-');
        const isIOS = /iPad|iPhone|iPod/.test(userAgent);
        const isWindows = userAgent.includes('Windows');
        // Platform-specific timeout optimization
        let timeout = 60000; // Default 60 seconds
        if (isSamsung) {
            timeout = 300000; // Samsung Pass needs 5 minutes
        }
        // Platform-specific resident key requirement
        let residentKey = "preferred"; // Default
        if (isSamsung) {
            residentKey = "required"; // Samsung Pass requires this
        }
        return {
            isSamsung,
            isIOS,
            isWindows,
            timeout,
            residentKey
        };
    }
    /**
     * Log platform detection information for diagnostics
     */
    static logPlatformInfo() {
        const config = this.detectPlatform();
        console.log("=== Platform Detection ===");
        console.log("User Agent:", navigator.userAgent);
        console.log("Platform:", navigator.platform);
        console.log("Samsung:", config.isSamsung);
        console.log("iOS:", config.isIOS);
        console.log("Windows:", config.isWindows);
        console.log("Timeout:", config.timeout, "ms");
        console.log("Resident Key:", config.residentKey);
        console.log("========================");
    }
}
//# sourceMappingURL=platform-detector.js.map