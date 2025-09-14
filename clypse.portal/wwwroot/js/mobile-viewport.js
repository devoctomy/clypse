/**
 * Mobile Viewport Fix
 * 
 * Addresses the mobile browser viewport height issue where 100vh doesn't account
 * for dynamic browser UI elements (address bar, nav bar, etc.)
 * 
 * This script sets CSS custom properties for accurate viewport height calculation
 * on mobile devices.
 */

(function() {
    'use strict';

    /**
     * Calculate and set the actual viewport height
     * This fixes the mobile browser 100vh issue
     */
    function setMobileViewportHeight() {
        // Calculate the actual viewport height
        const vh = window.innerHeight * 0.01;
        
        // Set the CSS custom property --vh
        document.documentElement.style.setProperty('--vh', `${vh}px`);
        
        // Also set mobile-specific viewport height
        document.documentElement.style.setProperty('--mobile-vh', `${vh}px`);
        
        // For debugging (can be removed in production)
        if (window.console && window.console.debug) {
            console.debug(`Mobile viewport height updated: ${window.innerHeight}px (--vh: ${vh}px)`);
        }
    }

    /**
     * Initialize mobile viewport fixes
     */
    function initMobileViewport() {
        // Set initial viewport height
        setMobileViewportHeight();

        // Update on window resize (orientation change, browser UI changes)
        window.addEventListener('resize', function() {
            // Use requestAnimationFrame to avoid excessive recalculations
            requestAnimationFrame(setMobileViewportHeight);
        });

        // Update on orientationchange event (mobile devices)
        window.addEventListener('orientationchange', function() {
            // Small delay to ensure orientation change is complete
            setTimeout(function() {
                setMobileViewportHeight();
            }, 100);
        });

        // For PWA support - update when viewport changes
        if ('visualViewport' in window) {
            window.visualViewport.addEventListener('resize', function() {
                requestAnimationFrame(setMobileViewportHeight);
            });
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initMobileViewport);
    } else {
        initMobileViewport();
    }

    // Also initialize immediately for faster loading
    initMobileViewport();

})();