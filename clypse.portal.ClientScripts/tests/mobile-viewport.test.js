describe('mobile-viewport', () => {
    let mockSetProperty;
    let mockAddEventListener;
    let mockRequestAnimationFrame;
    let mockConsoleDebug;
    let windowResizeHandler;
    let orientationChangeHandler;
    let visualViewportResizeHandler;

    beforeEach(() => {
        mockSetProperty = jest.fn();
        mockAddEventListener = jest.fn((event, handler) => {
            if (event === 'resize') {
                windowResizeHandler = handler;
            } else if (event === 'orientationchange') {
                orientationChangeHandler = handler;
            }
        });
        mockRequestAnimationFrame = jest.fn(cb => cb());
        mockConsoleDebug = jest.fn();

        Object.defineProperty(global.window, 'innerHeight', {
            writable: true,
            configurable: true,
            value: 800
        });

        Object.defineProperty(global.document, 'documentElement', {
            writable: true,
            configurable: true,
            value: {
                style: {
                    setProperty: mockSetProperty
                }
            }
        });

        Object.defineProperty(global.document, 'readyState', {
            writable: true,
            configurable: true,
            value: 'complete'
        });

        global.window.addEventListener = mockAddEventListener;
        global.window.requestAnimationFrame = mockRequestAnimationFrame;
        global.window.console = {
            debug: mockConsoleDebug
        };

        delete global.window.visualViewport;

        jest.resetModules();
    });

    afterEach(() => {
        jest.clearAllMocks();
    });

    test('GivenWindowInnerHeight800_WhenInitializing_ThenSetsVhCustomPropertyTo8px', () => {
        // Arrange
        global.window.innerHeight = 800;

        // Act
        require('../src/mobile-viewport.js');

        // Assert
        expect(mockSetProperty).toHaveBeenCalledWith('--vh', '8px');
    });

    test('GivenWindowInnerHeight800_WhenInitializing_ThenSetsMobileVhCustomPropertyTo8px', () => {
        // Arrange
        global.window.innerHeight = 800;

        // Act
        require('../src/mobile-viewport.js');

        // Assert
        expect(mockSetProperty).toHaveBeenCalledWith('--mobile-vh', '8px');
    });

    test('GivenWindowInnerHeight1000_WhenInitializing_ThenSetsVhCustomPropertyTo10px', () => {
        // Arrange
        global.window.innerHeight = 1000;

        // Act
        require('../src/mobile-viewport.js');

        // Assert
        expect(mockSetProperty).toHaveBeenCalledWith('--vh', '10px');
        expect(mockSetProperty).toHaveBeenCalledWith('--mobile-vh', '10px');
    });

    test('GivenConsoleDebug_WhenInitializing_ThenLogsViewportHeight', () => {
        // Arrange
        global.window.innerHeight = 800;

        // Act
        require('../src/mobile-viewport.js');

        // Assert
        expect(mockConsoleDebug).toHaveBeenCalledWith('Mobile viewport height updated: 800px (--vh: 8px)');
    });

    test('GivenInitialized_WhenWindowResized_ThenUpdatesVhProperty', () => {
        // Arrange
        global.window.innerHeight = 800;
        require('../src/mobile-viewport.js');
        mockSetProperty.mockClear();
        global.window.innerHeight = 900;

        // Act
        windowResizeHandler();

        // Assert
        expect(mockRequestAnimationFrame).toHaveBeenCalled();
        expect(mockSetProperty).toHaveBeenCalledWith('--vh', '9px');
        expect(mockSetProperty).toHaveBeenCalledWith('--mobile-vh', '9px');
    });

    test('GivenInitialized_WhenOrientationChanged_ThenUpdatesVhPropertyAfterDelay', (done) => {
        // Arrange
        global.window.innerHeight = 800;
        require('../src/mobile-viewport.js');
        mockSetProperty.mockClear();
        global.window.innerHeight = 600;

        // Act
        orientationChangeHandler();

        // Assert
        setTimeout(() => {
            expect(mockSetProperty).toHaveBeenCalledWith('--vh', '6px');
            expect(mockSetProperty).toHaveBeenCalledWith('--mobile-vh', '6px');
            done();
        }, 150);
    });

    test('GivenVisualViewportSupported_WhenInitializing_ThenRegistersVisualViewportResizeHandler', () => {
        // Arrange
        const mockVisualViewportAddEventListener = jest.fn((event, handler) => {
            if (event === 'resize') {
                visualViewportResizeHandler = handler;
            }
        });

        Object.defineProperty(global.window, 'visualViewport', {
            writable: true,
            configurable: true,
            value: {
                addEventListener: mockVisualViewportAddEventListener
            }
        });

        // Act
        require('../src/mobile-viewport.js');

        // Assert
        expect(mockVisualViewportAddEventListener).toHaveBeenCalledWith('resize', expect.any(Function));
    });

    test('GivenVisualViewportSupported_WhenVisualViewportResized_ThenUpdatesVhProperty', () => {
        // Arrange
        const mockVisualViewportAddEventListener = jest.fn((event, handler) => {
            if (event === 'resize') {
                visualViewportResizeHandler = handler;
            }
        });

        Object.defineProperty(global.window, 'visualViewport', {
            writable: true,
            configurable: true,
            value: {
                addEventListener: mockVisualViewportAddEventListener
            }
        });

        global.window.innerHeight = 800;
        require('../src/mobile-viewport.js');
        mockSetProperty.mockClear();
        global.window.innerHeight = 750;

        // Act
        visualViewportResizeHandler();

        // Assert
        expect(mockRequestAnimationFrame).toHaveBeenCalled();
        expect(mockSetProperty).toHaveBeenCalledWith('--vh', '7.5px');
        expect(mockSetProperty).toHaveBeenCalledWith('--mobile-vh', '7.5px');
    });

    test('GivenDocumentLoading_WhenInitializing_ThenRegistersDOMContentLoadedHandler', () => {
        // Arrange
        const mockDocumentAddEventListener = jest.fn();
        Object.defineProperty(global.document, 'readyState', {
            writable: true,
            configurable: true,
            value: 'loading'
        });
        global.document.addEventListener = mockDocumentAddEventListener;

        // Act
        require('../src/mobile-viewport.js');

        // Assert
        expect(mockDocumentAddEventListener).toHaveBeenCalledWith('DOMContentLoaded', expect.any(Function));
    });

    test('GivenWindowResize_WhenHandlerCalled_ThenUsesRequestAnimationFrame', () => {
        // Arrange
        global.window.innerHeight = 800;
        require('../src/mobile-viewport.js');
        mockRequestAnimationFrame.mockClear();

        // Act
        windowResizeHandler();

        // Assert
        expect(mockRequestAnimationFrame).toHaveBeenCalledWith(expect.any(Function));
    });

    test('GivenNoConsoleDebug_WhenInitializing_ThenDoesNotThrowError', () => {
        // Arrange
        global.window.console = {};
        global.window.innerHeight = 800;

        // Act & Assert
        expect(() => require('../src/mobile-viewport.js')).not.toThrow();
    });

    test('GivenDifferentViewportSizes_WhenCalculating_ThenCorrectlyCalculatesOnePercentOfHeight', () => {
        // Arrange
        const testCases = [
            { height: 500, expected: '5px' },
            { height: 1024, expected: '10.24px' },
            { height: 667, expected: '6.67px' },
            { height: 1366, expected: '13.66px' }
        ];

        testCases.forEach(testCase => {
            jest.resetModules();
            mockSetProperty.mockClear();
            global.window.innerHeight = testCase.height;

            // Act
            require('../src/mobile-viewport.js');

            // Assert
            expect(mockSetProperty).toHaveBeenCalledWith('--vh', testCase.expected);
            expect(mockSetProperty).toHaveBeenCalledWith('--mobile-vh', testCase.expected);
        });
    });
});
