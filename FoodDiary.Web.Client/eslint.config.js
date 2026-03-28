import parser from '@typescript-eslint/parser';
import tsPlugin from '@typescript-eslint/eslint-plugin';
import prettierPlugin from 'eslint-plugin-prettier';
import eslintConfigPrettier from 'eslint-config-prettier';

export default [
    {
        ignores: [
            '**/node_modules/**',
            '**/dist/**',
            '**/dist-admin/**',
            '**/.angular/**',
            '**/*.min.js',
        ],
    },
    {
        ignores: [
            '**/node_modules/**',
            '**/dist/**',
            '**/dist-admin/**',
            '**/.angular/**',
            '**/*.min.js',
        ],
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
            '@typescript-eslint/no-unused-vars': [
                'error',
                {
                    vars: 'all',
                    args: 'after-used',
                    ignoreRestSiblings: true,
                    argsIgnorePattern: '^_',
                    varsIgnorePattern: '^_',
                    caughtErrors: 'none',
                    destructuredArrayIgnorePattern: '^_',
                },
            ],
        },
    },
    {
        files: ['src/app/shared/models/**/*.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../api/**',
                                '../../components/shared/**',
                                '../../features/**',
                                'src/app/components/shared/**',
                                'src/app/features/**',
                            ],
                            message: 'shared/models must stay pure and must not depend on API, UI, or feature-local code.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/shared/api/**/*.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../../components/shared/**',
                                '../../features/**',
                                'src/app/components/shared/**',
                                'src/app/features/**',
                            ],
                            message: 'shared/api must not depend on UI or feature-local code.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/components/shared/**/*.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../../features/**',
                                '../../../features/**',
                                '../../../../features/**',
                                'src/app/features/**',
                            ],
                            message: 'components/shared should stay feature-agnostic shared UI.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/features/**/*.ts'],
        ignores: ['src/app/features/**/*.routes.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../**/*.routes',
                                '../../**/*.routes',
                                '../../../**/*.routes',
                                '../../../../**/*.routes',
                            ],
                            message: 'Feature code should depend on feature-local API/models/components, not on route configuration files.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/features/**/*.ts'],
        ignores: [
            'src/app/features/**/*.routes.ts',
            'src/app/features/public/**/*.ts',
            'src/app/features/**/*.spec.ts',
        ],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../../*/pages/**',
                                '../../../*/pages/**',
                                '../../../../*/pages/**',
                            ],
                            message: 'Import feature-local models/api/components instead of another feature page.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/features/**/dialogs/**/*.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../../../!(shared)/api/**',
                                '../../../../!(shared)/api/**',
                                '../../../../../!(shared)/api/**',
                            ],
                            message: 'Feature dialogs should use shared APIs or same-feature APIs, not reach into another feature API directly.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/features/**/components/**/*.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../../../!(shared)/api/**',
                                '../../../../!(shared)/api/**',
                                '../../../../../!(shared)/api/**',
                            ],
                            message: 'Feature components should use shared APIs or same-feature APIs, not reach into another feature API directly.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/features/**/lib/**/*.ts', 'src/app/features/**/resolvers/**/*.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../../!(shared)/api/**',
                                '../../../!(shared)/api/**',
                                '../../../../!(shared)/api/**',
                            ],
                            message: 'Feature lib and resolver code should stay within shared APIs or same-feature APIs.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/**/*.ts'],
        ignores: [
            'src/app/app.routes.ts',
            'src/app/features/**/*.routes.ts',
            'src/app/**/*.spec.ts',
        ],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../guards/**',
                                '../../guards/**',
                                '../../../guards/**',
                                'src/app/guards/**',
                            ],
                            message: 'Guards belong to the routing layer and should only be imported from app.routes.ts or feature *.routes.ts files.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['projects/fooddiary-admin/src/app/features/**/*.ts'],
        ignores: ['projects/fooddiary-admin/src/app/features/**/*.routes.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../**/*.routes',
                                '../../**/*.routes',
                                '../../../**/*.routes',
                                '../../../../**/*.routes',
                            ],
                            message: 'Admin feature code should depend on feature-local API/models/components, not on route files.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['projects/fooddiary-admin/src/app/features/**/*.ts'],
        ignores: [
            'projects/fooddiary-admin/src/app/features/**/*.routes.ts',
            'projects/fooddiary-admin/src/app/features/admin-public/**/*.ts',
            'projects/fooddiary-admin/src/app/**/*.spec.ts',
        ],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../../*/pages/**',
                                '../../../*/pages/**',
                                '../../../../*/pages/**',
                            ],
                            message: 'Admin features should import another feature API/models/components instead of a page.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['projects/fooddiary-admin/src/app/**/*.ts'],
        ignores: [
            'projects/fooddiary-admin/src/app/app.routes.ts',
            'projects/fooddiary-admin/src/app/features/**/*.routes.ts',
            'projects/fooddiary-admin/src/app/**/*.spec.ts',
        ],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../pages/**',
                                '../../pages/**',
                                '../../../pages/**',
                                '../services/**',
                                '../../services/**',
                                '../../../services/**',
                            ],
                            message: 'Admin code should use feature-local routes/pages/api or the explicit admin-auth boundary, not legacy global pages/services buckets.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['projects/fooddiary-admin/src/app/**/*.ts'],
        ignores: [
            'projects/fooddiary-admin/src/app/app.routes.ts',
            'projects/fooddiary-admin/src/app/features/**/*.routes.ts',
            'projects/fooddiary-admin/src/app/**/*.spec.ts',
        ],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: [
                                '../guards/**',
                                '../../guards/**',
                                '../../../guards/**',
                                'projects/fooddiary-admin/src/app/guards/**',
                            ],
                            message: 'Admin guards belong to the routing layer and should only be imported from app.routes.ts or admin feature *.routes.ts files.',
                        },
                    ],
                },
            ],
        },
    },
];
