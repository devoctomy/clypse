let navigatorServiceWorkerListeners = {};

Object.defineProperty(global, 'navigator', {
    writable: true,
    value: {
        serviceWorker: {
            ready: Promise.resolve(),
            addEventListener: jest.fn((event, handler) => {
                navigatorServiceWorkerListeners[event] = handler;
            }),
            controller: null
        }
    }
});

global.console = {
    log: jest.fn(),
    warn: jest.fn(),
    error: jest.fn()
};

delete global.window.location;
global.window.location = {
    reload: jest.fn()
};

require('../src/pwa-update.js');

describe('PWAUpdateService.initialize', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        navigatorServiceWorkerListeners = {};
        window.PWAUpdateService.serviceWorkerRegistration = null;
        window.PWAUpdateService.updateAvailable = false;
    });

    test('GivenValidRegistration_WhenInitialize_ThenSetsServiceWorkerRegistration', () => {
        // Arrange
        const mockRegistration = {
            addEventListener: jest.fn(),
            waiting: null,
            installing: null,
            update: jest.fn()
        };

        // Act
        window.PWAUpdateService.initialize(mockRegistration);

        // Assert
        expect(window.PWAUpdateService.serviceWorkerRegistration).toBe(mockRegistration);
    });

    test('GivenNullRegistration_WhenInitialize_ThenLogsWarning', () => {
        // Arrange

        // Act
        window.PWAUpdateService.initialize(null);

        // Assert
        expect(console.warn).toHaveBeenCalledWith('PWAUpdateService: No service worker registration provided');
    });

    test('GivenRegistrationWithWaitingWorker_WhenInitialize_ThenHandlesUpdateAvailable', () => {
        // Arrange
        const mockRegistration = {
            addEventListener: jest.fn(),
            waiting: {},
            installing: null,
            update: jest.fn().mockResolvedValue(undefined)
        };
        window.PWAUpdateService.callbacks.onUpdateAvailable = jest.fn();

        // Act
        window.PWAUpdateService.initialize(mockRegistration);

        // Assert
        expect(window.PWAUpdateService.updateAvailable).toBe(true);
        expect(window.PWAUpdateService.callbacks.onUpdateAvailable).toHaveBeenCalled();
    });

    test('GivenRegistrationWithInstallingWorker_WhenInitialize_ThenTracksInstalling', () => {
        // Arrange
        const mockInstallingWorker = {
            addEventListener: jest.fn(),
            state: 'installing'
        };
        const mockRegistration = {
            addEventListener: jest.fn(),
            waiting: null,
            installing: mockInstallingWorker,
            update: jest.fn().mockResolvedValue(undefined)
        };

        // Act
        window.PWAUpdateService.initialize(mockRegistration);

        // Assert
        expect(mockInstallingWorker.addEventListener).toHaveBeenCalledWith('statechange', expect.any(Function));
    });
});

describe('PWAUpdateService.handleUpdateAvailable', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        window.PWAUpdateService.updateAvailable = false;
        window.PWAUpdateService.callbacks = {
            onUpdateAvailable: null,
            onUpdateInstalled: null,
            onUpdateError: null
        };
    });

    test('GivenNoCallback_WhenHandleUpdateAvailable_ThenSetsUpdateAvailableFlag', () => {
        // Arrange

        // Act
        window.PWAUpdateService.handleUpdateAvailable();

        // Assert
        expect(window.PWAUpdateService.updateAvailable).toBe(true);
    });

    test('GivenCallbackRegistered_WhenHandleUpdateAvailable_ThenInvokesCallback', () => {
        // Arrange
        const mockCallback = jest.fn();
        window.PWAUpdateService.callbacks.onUpdateAvailable = mockCallback;

        // Act
        window.PWAUpdateService.handleUpdateAvailable();

        // Assert
        expect(mockCallback).toHaveBeenCalled();
    });
});

describe('PWAUpdateService.checkForUpdate', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    test('GivenValidRegistration_WhenCheckForUpdate_ThenCallsUpdateAndReturnsTrue', async () => {
        // Arrange
        const mockRegistration = {
            update: jest.fn().mockResolvedValue(undefined)
        };
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;

        // Act
        const result = await window.PWAUpdateService.checkForUpdate();

        // Assert
        expect(mockRegistration.update).toHaveBeenCalled();
        expect(result).toBe(true);
    });

    test('GivenNoRegistration_WhenCheckForUpdate_ThenReturnsFalse', async () => {
        // Arrange
        window.PWAUpdateService.serviceWorkerRegistration = null;

        // Act
        const result = await window.PWAUpdateService.checkForUpdate();

        // Assert
        expect(result).toBe(false);
        expect(console.warn).toHaveBeenCalledWith('PWAUpdateService: No service worker registration available');
    });

    test('GivenUpdateThrowsError_WhenCheckForUpdate_ThenReturnsFalseAndInvokesErrorCallback', async () => {
        // Arrange
        const error = new Error('Update failed');
        const mockRegistration = {
            update: jest.fn().mockRejectedValue(error)
        };
        const mockErrorCallback = jest.fn();
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;
        window.PWAUpdateService.callbacks.onUpdateError = mockErrorCallback;

        // Act
        const result = await window.PWAUpdateService.checkForUpdate();

        // Assert
        expect(result).toBe(false);
        expect(mockErrorCallback).toHaveBeenCalledWith(error);
    });
});

describe('PWAUpdateService.installUpdate', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    test('GivenWaitingWorker_WhenInstallUpdate_ThenPostsSkipWaitingMessageAndReturnsTrue', async () => {
        // Arrange
        const mockWaitingWorker = {
            postMessage: jest.fn()
        };
        const mockRegistration = {
            waiting: mockWaitingWorker
        };
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;

        // Act
        const result = await window.PWAUpdateService.installUpdate();

        // Assert
        expect(mockWaitingWorker.postMessage).toHaveBeenCalledWith({ type: 'SKIP_WAITING' });
        expect(result).toBe(true);
    });

    test('GivenNoWaitingWorker_WhenInstallUpdate_ThenReturnsFalse', async () => {
        // Arrange
        const mockRegistration = {
            waiting: null
        };
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;

        // Act
        const result = await window.PWAUpdateService.installUpdate();

        // Assert
        expect(result).toBe(false);
        expect(console.warn).toHaveBeenCalledWith('PWAUpdateService: No waiting service worker to install');
    });

    test('GivenNoRegistration_WhenInstallUpdate_ThenReturnsFalse', async () => {
        // Arrange
        window.PWAUpdateService.serviceWorkerRegistration = null;

        // Act
        const result = await window.PWAUpdateService.installUpdate();

        // Assert
        expect(result).toBe(false);
    });

    test('GivenPostMessageThrowsError_WhenInstallUpdate_ThenReturnsFalseAndInvokesErrorCallback', async () => {
        // Arrange
        const error = new Error('PostMessage failed');
        const mockWaitingWorker = {
            postMessage: jest.fn(() => { throw error; })
        };
        const mockRegistration = {
            waiting: mockWaitingWorker
        };
        const mockErrorCallback = jest.fn();
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;
        window.PWAUpdateService.callbacks.onUpdateError = mockErrorCallback;

        // Act
        const result = await window.PWAUpdateService.installUpdate();

        // Assert
        expect(result).toBe(false);
        expect(mockErrorCallback).toHaveBeenCalledWith(error);
    });
});

describe('PWAUpdateService.forceUpdate', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    afterEach(() => {
        jest.clearAllTimers();
    });

    test('GivenUpdateAvailable_WhenForceUpdate_ThenInstallsUpdateAndReturnsTrue', async () => {
        // Arrange
        jest.useFakeTimers();
        const mockWaitingWorker = {
            postMessage: jest.fn()
        };
        const mockRegistration = {
            update: jest.fn().mockResolvedValue(undefined),
            waiting: mockWaitingWorker
        };
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;
        window.PWAUpdateService.updateAvailable = true;

        // Act
        const resultPromise = window.PWAUpdateService.forceUpdate();
        await jest.advanceTimersByTimeAsync(1000);
        const result = await resultPromise;

        // Assert
        expect(result).toBe(true);
        expect(mockWaitingWorker.postMessage).toHaveBeenCalledWith({ type: 'SKIP_WAITING' });

        jest.useRealTimers();
    });

    test('GivenNoUpdateAvailable_WhenForceUpdate_ThenReturnsFalse', async () => {
        // Arrange
        jest.useFakeTimers();
        const mockRegistration = {
            update: jest.fn().mockResolvedValue(undefined),
            waiting: null
        };
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;
        window.PWAUpdateService.updateAvailable = false;

        // Act
        const resultPromise = window.PWAUpdateService.forceUpdate();
        await jest.advanceTimersByTimeAsync(1000);
        const result = await resultPromise;

        // Assert
        expect(result).toBe(false);

        jest.useRealTimers();
    });

    test('GivenCheckForUpdateFails_WhenForceUpdate_ThenReturnsFalse', async () => {
        // Arrange
        window.PWAUpdateService.serviceWorkerRegistration = null;

        // Act
        const result = await window.PWAUpdateService.forceUpdate();

        // Assert
        expect(result).toBe(false);
    });
});

describe('PWAUpdateService.isUpdateAvailable', () => {
    beforeEach(() => {
        window.PWAUpdateService.updateAvailable = false;
    });

    test('GivenUpdateAvailableTrue_WhenIsUpdateAvailable_ThenReturnsTrue', () => {
        // Arrange
        window.PWAUpdateService.updateAvailable = true;

        // Act
        const result = window.PWAUpdateService.isUpdateAvailable();

        // Assert
        expect(result).toBe(true);
    });

    test('GivenUpdateAvailableFalse_WhenIsUpdateAvailable_ThenReturnsFalse', () => {
        // Arrange
        window.PWAUpdateService.updateAvailable = false;

        // Act
        const result = window.PWAUpdateService.isUpdateAvailable();

        // Assert
        expect(result).toBe(false);
    });
});

describe('PWAUpdateService.setCallbacks', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        window.PWAUpdateService.callbacks = {
            onUpdateAvailable: null,
            onUpdateInstalled: null,
            onUpdateError: null
        };
    });

    test('GivenNewCallbacks_WhenSetCallbacks_ThenMergesWithExistingCallbacks', () => {
        // Arrange
        const mockUpdateAvailableCallback = jest.fn();
        const mockUpdateInstalledCallback = jest.fn();

        // Act
        window.PWAUpdateService.setCallbacks({
            onUpdateAvailable: mockUpdateAvailableCallback,
            onUpdateInstalled: mockUpdateInstalledCallback
        });

        // Assert
        expect(window.PWAUpdateService.callbacks.onUpdateAvailable).toBe(mockUpdateAvailableCallback);
        expect(window.PWAUpdateService.callbacks.onUpdateInstalled).toBe(mockUpdateInstalledCallback);
        expect(window.PWAUpdateService.callbacks.onUpdateError).toBe(null);
    });

    test('GivenPartialCallbacks_WhenSetCallbacks_ThenUpdatesOnlyProvidedCallbacks', () => {
        // Arrange
        const mockErrorCallback = jest.fn();
        window.PWAUpdateService.callbacks.onUpdateAvailable = jest.fn();

        // Act
        window.PWAUpdateService.setCallbacks({
            onUpdateError: mockErrorCallback
        });

        // Assert
        expect(window.PWAUpdateService.callbacks.onUpdateError).toBe(mockErrorCallback);
        expect(window.PWAUpdateService.callbacks.onUpdateAvailable).not.toBe(null);
    });
});

describe('PWAUpdateService.getCurrentVersion', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    test('GivenNoRegistration_WhenGetCurrentVersion_ThenReturnsUnknown', async () => {
        // Arrange
        window.PWAUpdateService.serviceWorkerRegistration = null;

        // Act
        const result = await window.PWAUpdateService.getCurrentVersion();

        // Assert
        expect(result).toBe('unknown');
    });

    test('GivenRegistrationWithoutActiveWorker_WhenGetCurrentVersion_ThenReturnsUnknown', async () => {
        // Arrange
        window.PWAUpdateService.serviceWorkerRegistration = {
            active: null
        };

        // Act
        const result = await window.PWAUpdateService.getCurrentVersion();

        // Assert
        expect(result).toBe('unknown');
    });

    test('GivenRegistrationWithActiveWorker_WhenGetCurrentVersion_ThenReturnsUnknown', async () => {
        // Arrange
        window.PWAUpdateService.serviceWorkerRegistration = {
            active: {}
        };

        // Act
        const result = await window.PWAUpdateService.getCurrentVersion();

        // Assert
        expect(result).toBe('unknown');
    });
});

describe('PWAUpdateService.trackInstalling', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        window.PWAUpdateService.updateAvailable = false;
        window.PWAUpdateService.callbacks = {
            onUpdateAvailable: jest.fn(),
            onUpdateInstalled: null,
            onUpdateError: null
        };
        global.navigator.serviceWorker.controller = null;
    });

    test('GivenWorkerStateChangesToInstalled_AndControllerExists_WhenTrackInstalling_ThenHandlesUpdateAvailable', () => {
        // Arrange
        let stateChangeHandler;
        const mockWorker = {
            addEventListener: jest.fn((event, handler) => {
                if (event === 'statechange') {
                    stateChangeHandler = handler;
                }
            }),
            state: 'installing'
        };
        global.navigator.serviceWorker.controller = {};

        // Act
        window.PWAUpdateService.trackInstalling(mockWorker);
        mockWorker.state = 'installed';
        stateChangeHandler();

        // Assert
        expect(window.PWAUpdateService.updateAvailable).toBe(true);
        expect(window.PWAUpdateService.callbacks.onUpdateAvailable).toHaveBeenCalled();
    });

    test('GivenWorkerStateChangesToInstalled_AndNoController_WhenTrackInstalling_ThenLogsFirstInstall', () => {
        // Arrange
        let stateChangeHandler;
        const mockWorker = {
            addEventListener: jest.fn((event, handler) => {
                if (event === 'statechange') {
                    stateChangeHandler = handler;
                }
            }),
            state: 'installing'
        };
        global.navigator.serviceWorker.controller = null;

        // Act
        window.PWAUpdateService.trackInstalling(mockWorker);
        mockWorker.state = 'installed';
        stateChangeHandler();

        // Assert
        expect(console.log).toHaveBeenCalledWith('PWAUpdateService: App installed for first time');
        expect(window.PWAUpdateService.updateAvailable).toBe(false);
    });

    test('GivenWorkerStateChangesToActivating_WhenTrackInstalling_ThenDoesNotHandleUpdate', () => {
        // Arrange
        let stateChangeHandler;
        const mockWorker = {
            addEventListener: jest.fn((event, handler) => {
                if (event === 'statechange') {
                    stateChangeHandler = handler;
                }
            }),
            state: 'installing'
        };

        // Act
        window.PWAUpdateService.trackInstalling(mockWorker);
        mockWorker.state = 'activating';
        stateChangeHandler();

        // Assert
        expect(window.PWAUpdateService.updateAvailable).toBe(false);
    });
});

describe('PWAUpdateService.setupUpdateListeners', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        navigatorServiceWorkerListeners = {};
        global.navigator.serviceWorker.controller = null;
    });

    test('GivenUpdateFoundEvent_WhenSetupUpdateListeners_ThenTracksNewWorker', () => {
        // Arrange
        const mockInstallingWorker = {
            addEventListener: jest.fn(),
            state: 'installing'
        };
        let updateFoundHandler;
        const mockRegistration = {
            addEventListener: jest.fn((event, handler) => {
                if (event === 'updatefound') {
                    updateFoundHandler = handler;
                }
            }),
            waiting: null,
            installing: null,
            update: jest.fn().mockResolvedValue(undefined)
        };
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;

        // Act
        window.PWAUpdateService.setupUpdateListeners();
        mockRegistration.installing = mockInstallingWorker;
        updateFoundHandler();

        // Assert
        expect(mockInstallingWorker.addEventListener).toHaveBeenCalledWith('statechange', expect.any(Function));
    });

    test('GivenControllerChange_WhenSetupUpdateListeners_ThenInvokesCallback', () => {
        // Arrange
        const mockOnUpdateInstalled = jest.fn();
        window.PWAUpdateService.callbacks.onUpdateInstalled = mockOnUpdateInstalled;

        const mockRegistration = {
            addEventListener: jest.fn(),
            waiting: null,
            installing: null,
            update: jest.fn().mockResolvedValue(undefined)
        };
        window.PWAUpdateService.serviceWorkerRegistration = mockRegistration;

        // Act
        window.PWAUpdateService.setupUpdateListeners();
        const controllerChangeHandler = navigatorServiceWorkerListeners['controllerchange'];
        controllerChangeHandler();

        // Assert
        expect(mockOnUpdateInstalled).toHaveBeenCalled();
    });
});
