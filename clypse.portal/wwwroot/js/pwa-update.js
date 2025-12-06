/**
 * PWA Update Service
 * 
 * Handles PWA updates, service worker lifecycle events, and provides
 * manual update functionality for users.
 */

window.PWAUpdateService = {
    serviceWorkerRegistration: null,
    updateAvailable: false,
    callbacks: {
        onUpdateAvailable: null,
        onUpdateInstalled: null,
        onUpdateError: null
    },

    /**
     * Initialize the PWA update service
     * @param {ServiceWorkerRegistration} registration - The service worker registration
     */
    initialize: function(registration) {
        console.log('PWAUpdateService: Initializing with registration');
        this.serviceWorkerRegistration = registration;
        
        if (!registration) {
            console.warn('PWAUpdateService: No service worker registration provided');
            return;
        }

        // Listen for updates
        this.setupUpdateListeners();
        
        // Check for immediate updates
        this.checkForUpdate();
    },

    /**
     * Set up service worker event listeners for updates
     */
    setupUpdateListeners: function() {
        const registration = this.serviceWorkerRegistration;
        
        // Listen for new service worker waiting
        if (registration.waiting) {
            console.log('PWAUpdateService: Update available (waiting worker found)');
            this.handleUpdateAvailable();
        }

        // Listen for new service worker installing
        if (registration.installing) {
            console.log('PWAUpdateService: Service worker installing');
            this.trackInstalling(registration.installing);
        }

        // Listen for updatefound event
        registration.addEventListener('updatefound', () => {
            console.log('PWAUpdateService: Update found event');
            const newWorker = registration.installing;
            if (newWorker) {
                this.trackInstalling(newWorker);
            }
        });

        // Listen for controller change (new SW activated)
        navigator.serviceWorker.addEventListener('controllerchange', () => {
            console.log('PWAUpdateService: Controller changed - reloading page');
            if (this.callbacks.onUpdateInstalled) {
                this.callbacks.onUpdateInstalled();
            }
            // Reload the page to use the new service worker
            window.location.reload();
        });
    },

    /**
     * Track a service worker through its installation process
     * @param {ServiceWorker} worker - The installing service worker
     */
    trackInstalling: function(worker) {
        worker.addEventListener('statechange', () => {
            console.log('PWAUpdateService: Worker state changed to', worker.state);
            
            if (worker.state === 'installed') {
                if (navigator.serviceWorker.controller) {
                    // New update available
                    console.log('PWAUpdateService: New update installed and waiting');
                    this.handleUpdateAvailable();
                } else {
                    // First install
                    console.log('PWAUpdateService: App installed for first time');
                }
            }
        });
    },

    /**
     * Handle when an update becomes available
     */
    handleUpdateAvailable: function() {
        this.updateAvailable = true;
        console.log('PWAUpdateService: Update available flag set to true');
        
        if (this.callbacks.onUpdateAvailable) {
            this.callbacks.onUpdateAvailable();
        }
    },

    /**
     * Manually check for updates
     * @returns {Promise<boolean>} True if update check was initiated
     */
    checkForUpdate: async function() {
        try {
            console.log('PWAUpdateService: Manually checking for updates');
            
            if (!this.serviceWorkerRegistration) {
                console.warn('PWAUpdateService: No service worker registration available');
                return false;
            }

            await this.serviceWorkerRegistration.update();
            console.log('PWAUpdateService: Update check completed');
            return true;
        } catch (error) {
            console.error('PWAUpdateService: Error checking for updates:', error);
            if (this.callbacks.onUpdateError) {
                this.callbacks.onUpdateError(error);
            }
            return false;
        }
    },

    /**
     * Install the waiting update (skip waiting and activate)
     * @returns {Promise<boolean>} True if update installation was initiated
     */
    installUpdate: async function() {
        try {
            console.log('PWAUpdateService: Installing update');
            
            if (!this.serviceWorkerRegistration || !this.serviceWorkerRegistration.waiting) {
                console.warn('PWAUpdateService: No waiting service worker to install');
                return false;
            }

            // Tell the waiting service worker to skip waiting and become active
            this.serviceWorkerRegistration.waiting.postMessage({ type: 'SKIP_WAITING' });
            
            console.log('PWAUpdateService: Sent SKIP_WAITING message to service worker');
            return true;
        } catch (error) {
            console.error('PWAUpdateService: Error installing update:', error);
            if (this.callbacks.onUpdateError) {
                this.callbacks.onUpdateError(error);
            }
            return false;
        }
    },

    /**
     * Force check for update and install if available
     * @returns {Promise<boolean>} True if operation was successful
     */
    forceUpdate: async function() {
        console.log('PWAUpdateService: Force update requested');
        
        // First check for updates
        const checkResult = await this.checkForUpdate();
        if (!checkResult) {
            return false;
        }

        // Wait a moment for the update check to process
        await new Promise(resolve => setTimeout(resolve, 1000));

        // If an update is available, install it
        if (this.updateAvailable && this.serviceWorkerRegistration && this.serviceWorkerRegistration.waiting) {
            return await this.installUpdate();
        } else {
            console.log('PWAUpdateService: No update available after check');
            return false;
        }
    },

    /**
     * Check if an update is currently available
     * @returns {boolean} True if update is available
     */
    isUpdateAvailable: function() {
        return this.updateAvailable;
    },

    /**
     * Set callback functions for update events
     * @param {Object} callbacks - Object containing callback functions
     */
    setCallbacks: function(callbacks) {
        this.callbacks = { ...this.callbacks, ...callbacks };
        console.log('PWAUpdateService: Callbacks updated', this.callbacks);
    },

    /**
     * Get the current app version from the service worker
     * @returns {Promise<string>} The current app version
     */
    getCurrentVersion: async function() {
        try {
            if (this.serviceWorkerRegistration && this.serviceWorkerRegistration.active) {
                // Try to get version from service worker assets manifest
                return self.assetsManifest?.version || 'unknown';
            }
            return 'unknown';
        } catch (error) {
            console.warn('PWAUpdateService: Could not get current version:', error);
            return 'unknown';
        }
    }
};

// Auto-initialize if service worker is already registered
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.ready.then(registration => {
        window.PWAUpdateService.initialize(registration);
    }).catch(error => {
        console.warn('PWAUpdateService: Service worker not ready:', error);
    });
}