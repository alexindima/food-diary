import parser from '@typescript-eslint/parser';
import tsPlugin from '@typescript-eslint/eslint-plugin';
import prettierPlugin from 'eslint-plugin-prettier';
import eslintConfigPrettier from 'eslint-config-prettier';

export default [
    {
        ignores: ['node_modules/', 'dist/', '*.min.js', '.angular/**/*'],
        files: ['**/*.js', '**/*.ts'],
        languageOptions: {
            parser,
            parserOptions: {
                ecmaVersion: 'latest',
                sourceType: 'module',
                ecmaFeatures: {
                    jsx: false,
                },
            },
        },
        plugins: {
            '@typescript-eslint': tsPlugin,
            prettier: prettierPlugin,
        },
        rules: {
            ...eslintConfigPrettier.rules,
            'no-unused-vars': 'error',
            'object-shorthand': ['error', 'always'],
            curly: ['error', 'all'],
            'no-redeclare': 'error',
            quotes: ['error', 'single'],
            'keyword-spacing': ['error', { after: true }],
            'prefer-const': 'error',
            eqeqeq: ['error', 'always'],
            'no-unreachable': 'error',
            '@typescript-eslint/explicit-member-accessibility': [
                'error',
                {
                    accessibility: 'explicit',
                },
            ],
            '@typescript-eslint/explicit-function-return-type': [
                'error',
                {
                    allowExpressions: false,
                    allowTypedFunctionExpressions: true,
                    allowHigherOrderFunctions: true,
                    allowDirectConstAssertionInArrowFunctions: true,
                },
            ],
            'no-multiple-empty-lines': ['error', { max: 1, maxEOF: 1 }],
            'no-trailing-spaces': 'error',
            '@typescript-eslint/consistent-type-assertions': [
                'error',
                {
                    assertionStyle: 'as',
                    objectLiteralTypeAssertions: 'never',
                },
            ],
        },
    },
];
