import parser from '@typescript-eslint/parser';
import tsPlugin from '@typescript-eslint/eslint-plugin';
import prettierPlugin from 'eslint-plugin-prettier';
import eslintConfigPrettier from 'eslint-config-prettier';
import templateParser from '@angular-eslint/template-parser';
import templatePlugin from '@angular-eslint/eslint-plugin-template';

export default [
    {
        ignores: [
            '**/node_modules/**',
            '**/dist/**',
            '**/dist-admin/**',
            '**/dist-storybook/**',
            '**/.angular/**',
            '**/*.min.js',
        ],
    },
    {
        ignores: [
            '**/node_modules/**',
            '**/dist/**',
            '**/dist-admin/**',
            '**/dist-storybook/**',
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
            quotes: ['error', 'single', { avoidEscape: true }],
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
        files: ['**/*.ts'],
        ignores: ['**/*.spec.ts'],
        rules: {
            'no-restricted-syntax': [
                'error',
                {
                    selector: 'Decorator[expression.callee.name="HostListener"]',
                    message: 'Use the host metadata property or fromEvent() instead of @HostListener(). See: https://angular.dev/api/core/HostListener',
                },
                {
                    selector: 'Decorator[expression.callee.name="HostBinding"]',
                    message: 'Use the host metadata property instead of @HostBinding(). See: https://angular.dev/api/core/HostBinding',
                },
                {
                    selector:
                        'ClassDeclaration[decorators.length>0] > ClassBody > MethodDefinition[kind=\"constructor\"] > FunctionExpression > TSParameterProperty',
                    message: 'Use inject() instead of constructor parameter DI in Angular-decorated classes.',
                },
                {
                    selector: 'Decorator[expression.callee.name="Input"]',
                    message: 'Use signal-based input() instead of @Input(). See: https://angular.dev/guide/signals/inputs',
                },
                {
                    selector: 'Decorator[expression.callee.name="Output"]',
                    message: 'Use output() instead of @Output(). See: https://angular.dev/guide/components/output-function',
                },
                {
                    selector: 'Decorator[expression.callee.name="ViewChild"]',
                    message: 'Use viewChild() signal query instead of @ViewChild(). See: https://angular.dev/guide/signals/queries',
                },
                {
                    selector: 'Decorator[expression.callee.name="ViewChildren"]',
                    message: 'Use viewChildren() signal query instead of @ViewChildren(). See: https://angular.dev/guide/signals/queries',
                },
                {
                    selector: 'Decorator[expression.callee.name="ContentChild"]',
                    message: 'Use contentChild() signal query instead of @ContentChild(). See: https://angular.dev/guide/signals/queries',
                },
                {
                    selector: 'Decorator[expression.callee.name="NgModule"]',
                    message: 'Use standalone components instead of NgModules.',
                },
                {
                    selector: 'PropertyAssignment[key.name="standalone"][value.value=false]',
                    message: 'Components must be standalone. Remove standalone: false.',
                },
                {
                    selector: 'PropertyAssignment[key.name="changeDetection"][value.property.name="Default"]',
                    message: 'Use ChangeDetectionStrategy.OnPush instead of Default.',
                },
                {
                    selector: 'MethodDefinition[key.name="ngOnInit"]',
                    message: 'Use constructor initialization, effect(), or computed() instead of ngOnInit.',
                },
                {
                    selector: 'MethodDefinition[key.name="ngAfterViewInit"]',
                    message: 'Use viewChild() with effect() and afterNextRender() instead of ngAfterViewInit.',
                },
                {
                    selector: 'MethodDefinition[key.name="ngAfterViewChecked"]',
                    message: 'Avoid ngAfterViewChecked. Use signal queries, effect(), or afterNextRender() for targeted post-render work.',
                },
                {
                    selector: 'MethodDefinition[key.name="ngAfterContentInit"]',
                    message: 'Avoid ngAfterContentInit. Use contentChild()/contentChildren() with effect() instead.',
                },
                {
                    selector: 'MethodDefinition[key.name="ngAfterContentChecked"]',
                    message: 'Avoid ngAfterContentChecked. Use signal queries and computed() instead of repeated content checks.',
                },
                {
                    selector: 'MethodDefinition[key.name="ngDoCheck"]',
                    message: 'Avoid ngDoCheck. Use signals, computed(), and explicit reactive state instead.',
                },
                {
                    selector: 'MethodDefinition[key.name="ngOnDestroy"]',
                    message: 'Use DestroyRef.onDestroy() instead of ngOnDestroy. See: https://angular.dev/api/core/DestroyRef',
                },
                {
                    selector: 'TSClassImplements Identifier[name="OnInit"]',
                    message: 'Use constructor initialization, effect(), or computed() instead of OnInit.',
                },
                {
                    selector: 'TSClassImplements Identifier[name="AfterViewInit"]',
                    message: 'Use viewChild() with effect() and afterNextRender() instead of AfterViewInit.',
                },
                {
                    selector: 'TSClassImplements Identifier[name="AfterViewChecked"]',
                    message: 'Avoid AfterViewChecked. Use signal queries, effect(), or afterNextRender() for targeted post-render work.',
                },
                {
                    selector: 'TSClassImplements Identifier[name="AfterContentInit"]',
                    message: 'Avoid AfterContentInit. Use contentChild()/contentChildren() with effect() instead.',
                },
                {
                    selector: 'TSClassImplements Identifier[name="AfterContentChecked"]',
                    message: 'Avoid AfterContentChecked. Use signal queries and computed() instead of repeated content checks.',
                },
                {
                    selector: 'TSClassImplements Identifier[name="DoCheck"]',
                    message: 'Avoid DoCheck. Use signals, computed(), and explicit reactive state instead.',
                },
                {
                    selector: 'MethodDefinition[key.name="ngOnChanges"]',
                    message: 'Use signal inputs with effect() or computed() instead of ngOnChanges.',
                },
                {
                    selector: 'Decorator[expression.callee.name="ContentChildren"]',
                    message: 'Use contentChildren() signal query instead of @ContentChildren(). See: https://angular.dev/guide/signals/queries',
                },
            ],
            'no-restricted-imports': [
                'error',
                {
                    paths: [
                        {
                            name: '@angular/core',
                            importNames: ['OnInit'],
                            message: 'Use constructor initialization, effect(), or computed() instead of OnInit.',
                        },
                        {
                            name: '@angular/core',
                            importNames: ['AfterViewInit'],
                            message: 'Use viewChild() with effect() and afterNextRender() instead of AfterViewInit.',
                        },
                        {
                            name: '@angular/core',
                            importNames: ['AfterViewChecked'],
                            message: 'Avoid AfterViewChecked. Use signal queries, effect(), or afterNextRender() instead.',
                        },
                        {
                            name: '@angular/core',
                            importNames: ['AfterContentInit'],
                            message: 'Avoid AfterContentInit. Use contentChild()/contentChildren() with effect() instead.',
                        },
                        {
                            name: '@angular/core',
                            importNames: ['AfterContentChecked'],
                            message: 'Avoid AfterContentChecked. Use signal queries and computed() instead.',
                        },
                        {
                            name: '@angular/core',
                            importNames: ['DoCheck'],
                            message: 'Avoid DoCheck. Use signals, computed(), and explicit reactive state instead.',
                        },
                        {
                            name: '@angular/core',
                            importNames: ['HostListener'],
                            message: 'Use the host metadata property or fromEvent() instead of HostListener.',
                        },
                        {
                            name: '@angular/core',
                            importNames: ['HostBinding'],
                            message: 'Use the host metadata property instead of HostBinding.',
                        },
                    ],
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
                    paths: [
                        {
                            name: '@angular/material',
                            message: 'Import UI primitives from fd-ui-kit instead of Angular Material directly.',
                        },
                    ],
                    patterns: [
                        {
                            group: ['@angular/material/**'],
                            message: 'Import UI primitives from fd-ui-kit instead of Angular Material directly.',
                        },
                        {
                            group: ['@angular/cdk/dialog', '@angular/cdk/overlay', '@angular/cdk/portal'],
                            message: 'Use fd-ui-kit dialog/menu/date primitives instead of low-level CDK overlay APIs in app code.',
                        },
                        {
                            group: ['projects/fd-ui-kit/src/lib/**', 'fd-ui-kit/src/lib/**'],
                            message: 'Import from the public fd-ui-kit barrel instead of deep-linking into UI-kit internals.',
                        },
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
                    paths: [
                        {
                            name: '@angular/material',
                            message: 'Import UI primitives from fd-ui-kit instead of Angular Material directly.',
                        },
                    ],
                    patterns: [
                        {
                            group: ['@angular/material/**'],
                            message: 'Import UI primitives from fd-ui-kit instead of Angular Material directly.',
                        },
                        {
                            group: ['@angular/cdk/dialog', '@angular/cdk/overlay', '@angular/cdk/portal'],
                            message: 'Use fd-ui-kit dialog/menu/date primitives instead of low-level CDK overlay APIs in admin feature code.',
                        },
                        {
                            group: ['projects/fd-ui-kit/src/lib/**', 'fd-ui-kit/src/lib/**'],
                            message: 'Import from the public fd-ui-kit barrel instead of deep-linking into UI-kit internals.',
                        },
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

    // Angular template accessibility rules
    {
        files: ['**/*.html'],
        languageOptions: {
            parser: templateParser,
        },
        plugins: {
            '@angular-eslint/template': templatePlugin,
        },
        rules: {
            '@angular-eslint/template/alt-text': 'error',
            '@angular-eslint/template/elements-content': 'error',
            '@angular-eslint/template/click-events-have-key-events': 'error',
            '@angular-eslint/template/interactive-supports-focus': 'error',
            '@angular-eslint/template/valid-aria': 'error',
            '@angular-eslint/template/role-has-required-aria': 'error',
            '@angular-eslint/template/no-positive-tabindex': 'error',
            '@angular-eslint/template/label-has-associated-control': 'warn',
            '@angular-eslint/template/no-autofocus': 'warn',
        },
    },
];
