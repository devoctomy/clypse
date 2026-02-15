import js from '@eslint/js';

export default [
    js.configs.recommended,
    {
        languageOptions: {
            ecmaVersion: 2022,
            sourceType: 'module',
            globals: {
                // Browser globals
                window: 'readonly',
                document: 'readonly',
                navigator: 'readonly',
                console: 'readonly',
                Promise: 'readonly',
                setTimeout: 'readonly',
                clearTimeout: 'readonly',
                setInterval: 'readonly',
                clearInterval: 'readonly',
                fetch: 'readonly',
                location: 'readonly',
                btoa: 'readonly',
                atob: 'readonly',
                URLSearchParams: 'readonly',
                Blob: 'readonly',
                FileReader: 'readonly',
                TextEncoder: 'readonly',
                crypto: 'readonly',
                requestAnimationFrame: 'readonly',
                self: 'readonly',
                // Third-party libraries
                AWS: 'readonly',
                AmazonCognitoIdentity: 'readonly',
                SimpleWebAuthnBrowser: 'readonly'
            }
        },
        rules: {
            // Code quality
            'no-unused-vars': ['error', { 
                argsIgnorePattern: '^_',
                varsIgnorePattern: '^_'
            }],
            'no-console': 'off', // We use console for logging
            'no-debugger': 'error',
            'no-alert': 'warn',
            
            // Best practices
            'eqeqeq': ['error', 'always'],
            'curly': ['error', 'all'],
            'no-eval': 'error',
            'no-implied-eval': 'error',
            'no-with': 'error',
            'no-var': 'error',
            'prefer-const': 'error',
            'prefer-arrow-callback': 'warn',
            
            // Style (basic)
            'indent': ['error', 4, { SwitchCase: 1 }],
            'quotes': ['error', 'single', { avoidEscape: true }],
            'semi': ['error', 'always'],
            'comma-dangle': ['error', 'never'],
            'no-trailing-spaces': 'error',
            'eol-last': ['error', 'always']
        }
    },
    {
        // Test files configuration
        files: ['tests/**/*.js'],
        languageOptions: {
            globals: {
                // Jest globals
                describe: 'readonly',
                test: 'readonly',
                expect: 'readonly',
                beforeEach: 'readonly',
                afterEach: 'readonly',
                beforeAll: 'readonly',
                afterAll: 'readonly',
                jest: 'readonly',
                global: 'readonly',
                // Node.js globals for tests
                require: 'readonly',
                Buffer: 'readonly'
            }
        },
        rules: {
            'no-undef': 'error'
        }
    },
    {
        ignores: [
            'node_modules/**',
            'coverage/**',
            'dist/**',
            'build/**'
        ]
    }
];
