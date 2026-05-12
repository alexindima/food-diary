/** @type {import('dependency-cruiser').IConfiguration} */
module.exports = {
    forbidden: [
        {
            name: 'no-circular',
            severity: 'error',
            from: {},
            to: {
                circular: true,
            },
        },
        {
            name: 'not-to-unresolvable',
            severity: 'error',
            from: {},
            to: {
                couldNotResolve: true,
            },
        },
        {
            name: 'not-to-spec',
            severity: 'error',
            from: {
                pathNot: '[.](?:spec|test)[.](?:js|mjs|cjs|jsx|ts|mts|cts|tsx)$',
            },
            to: {
                path: '[.](?:spec|test)[.](?:js|mjs|cjs|jsx|ts|mts|cts|tsx)$',
            },
        },
        {
            name: 'not-to-dev-dep-from-production',
            severity: 'error',
            from: {
                path: '^(src/app|projects/fooddiary-admin/src/app|projects/fd-ui-kit/src/lib)',
                pathNot: '[.](?:spec|test|stories)[.](?:js|mjs|cjs|jsx|ts|mts|cts|tsx)$',
            },
            to: {
                dependencyTypes: ['npm-dev'],
                dependencyTypesNot: ['type-only'],
                pathNot: ['node_modules/@types/'],
            },
        },
        {
            name: 'client-must-not-import-admin',
            severity: 'error',
            from: {
                path: '^src/app/',
            },
            to: {
                path: '^projects/fooddiary-admin/src/app/',
            },
        },
        {
            name: 'admin-must-not-import-client-app',
            severity: 'error',
            from: {
                path: '^projects/fooddiary-admin/src/app/',
            },
            to: {
                path: '^src/app/',
            },
        },
        {
            name: 'shared-models-must-stay-pure',
            severity: 'error',
            from: {
                path: '^src/app/shared/models/',
                pathNot: '[.](?:spec|test)[.](?:js|mjs|cjs|jsx|ts|mts|cts|tsx)$',
            },
            to: {
                path: '^src/app/(features|components/shared|shared/api|shared/dialogs)/',
            },
        },
        {
            name: 'shared-api-must-not-import-ui-or-features',
            severity: 'error',
            from: {
                path: '^src/app/shared/api/',
                pathNot: '[.](?:spec|test)[.](?:js|mjs|cjs|jsx|ts|mts|cts|tsx)$',
            },
            to: {
                path: '^src/app/(features|components/shared|shared/dialogs)/',
            },
        },
        {
            name: 'shared-ui-must-not-import-features',
            severity: 'error',
            from: {
                path: '^src/app/components/shared/',
                pathNot: '[.](?:spec|test)[.](?:js|mjs|cjs|jsx|ts|mts|cts|tsx)$',
            },
            to: {
                path: '^src/app/features/',
            },
        },
        {
            name: 'feature-models-must-not-import-feature-implementation',
            severity: 'error',
            from: {
                path: '^src/app/features/[^/]+/models/',
                pathNot: '[.](?:spec|test)[.](?:js|mjs|cjs|jsx|ts|mts|cts|tsx)$',
            },
            to: {
                path: '^src/app/features/[^/]+/(api|components|dialogs|lib|pages)/',
            },
        },
    ],
    options: {
        doNotFollow: {
            path: ['node_modules'],
        },
        exclude: {
            path: ['(^|/)dist(-admin|-storybook)?/', '(^|/).angular/', '(^|/)coverage/'],
        },
        tsConfig: {
            fileName: 'tsconfig.json',
        },
        enhancedResolveOptions: {
            extensions: ['.ts', '.js', '.mjs', '.json'],
            exportsFields: ['exports'],
            conditionNames: ['import', 'require', 'browser', 'node', 'default', 'types'],
            mainFields: ['browser', 'module', 'main', 'types', 'typings'],
        },
        tsPreCompilationDeps: true,
        skipAnalysisNotInRules: true,
    },
};
