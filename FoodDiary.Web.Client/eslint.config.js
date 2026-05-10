/* eslint-disable @typescript-eslint/explicit-function-return-type */
import angularPlugin from '@angular-eslint/eslint-plugin';
import templatePlugin from '@angular-eslint/eslint-plugin-template';
import templateParser from '@angular-eslint/template-parser';
import tsPlugin from '@typescript-eslint/eslint-plugin';
import parser from '@typescript-eslint/parser';
import eslintConfigPrettier from 'eslint-config-prettier';
import prettierPlugin from 'eslint-plugin-prettier';
import simpleImportSortPlugin from 'eslint-plugin-simple-import-sort';

const getTemplateAttributes = node => [...(node.attributes ?? []), ...(node.inputs ?? [])];

const hasTemplateAttribute = (node, name) => getTemplateAttributes(node).some(attribute => attribute.name === name);

const isAriaHidden = node =>
    getTemplateAttributes(node).some(
        attribute => attribute.name === 'aria-hidden' && (attribute.value === 'true' || attribute.value?.source === 'true'),
    );

const hasProjectedText = nodes =>
    (nodes ?? []).some(node => {
        if (node.type === 'Text') {
            return Boolean(node.value?.trim());
        }

        if (node.type === 'BoundText') {
            return true;
        }

        if ((node.type === 'Element' || node.type === 'Template') && !isAriaHidden(node)) {
            return hasProjectedText(node.children);
        }

        return false;
    });

const localTemplatePlugin = {
    rules: {
        'fd-ui-button-accessible-name': {
            meta: {
                type: 'problem',
                docs: {
                    description: 'Require accessible names to be passed through the fd-ui-button ariaLabel input.',
                },
                messages: {
                    hostAriaLabel: 'Use the fd-ui-button `ariaLabel` input instead of setting `aria-label` on the component host.',
                    missingName: 'Icon-only fd-ui-button needs an `ariaLabel` input or visible projected text.',
                },
                schema: [],
            },
            create(context) {
                return {
                    Element(node) {
                        if (node.name !== 'fd-ui-button') {
                            return;
                        }

                        const hostAriaLabel = getTemplateAttributes(node).find(attribute => attribute.name === 'aria-label');

                        if (hostAriaLabel) {
                            context.report({
                                node: hostAriaLabel,
                                messageId: 'hostAriaLabel',
                            });
                        }

                        if (
                            hasTemplateAttribute(node, 'icon') &&
                            !hasTemplateAttribute(node, 'ariaLabel') &&
                            !hasProjectedText(node.children)
                        ) {
                            context.report({
                                node,
                                messageId: 'missingName',
                            });
                        }
                    },
                };
            },
        },
    },
};

const getParserServices = context => context.sourceCode.parserServices ?? context.parserServices;

const getFunctionName = node => {
    if (node.id?.name) {
        return node.id.name;
    }

    const parent = node.parent;
    if (!parent) {
        return null;
    }

    if (parent.type === 'VariableDeclarator' && parent.id.type === 'Identifier') {
        return parent.id.name;
    }

    if (parent.type === 'MethodDefinition' || parent.type === 'PropertyDefinition') {
        return getPropertyName(parent.key);
    }

    if (parent.type === 'Property' && parent.value === node) {
        return getPropertyName(parent.key);
    }

    return null;
};

const isFrameworkNamedCallback = node => {
    const parent = node.parent;
    if (parent?.type !== 'Property' || parent.value !== node) {
        return false;
    }

    const propertyName = getPropertyName(parent.key);

    return (
        propertyName === 'loadComponent' ||
        propertyName === 'loadChildren' ||
        propertyName === 'loader' ||
        propertyName === 'bootstrap' ||
        propertyName === 'resolve' ||
        propertyName === 'canActivate' ||
        propertyName === 'canMatch'
    );
};

const isFrameworkFunctionName = name => name === 'bootstrap' || name === 'loader' || name.endsWith('Guard');

const getPropertyName = key => {
    if (key.type === 'Identifier') {
        return key.name;
    }

    if (key.type === 'Literal' && typeof key.value === 'string') {
        return key.value;
    }

    return null;
};

const isThenableType = (checker, type) => {
    if (checker.getPromisedTypeOfPromise(type)) {
        return true;
    }

    const symbolName = type.aliasSymbol?.escapedName ?? type.symbol?.escapedName;
    if (symbolName === 'Promise' || symbolName === 'PromiseLike') {
        return true;
    }

    return checker.typeToString(type).startsWith('Promise<');
};

const isAsyncLikeFunction = (context, node) => {
    if (node.async) {
        return true;
    }

    const services = getParserServices(context);
    if (!services?.program || !services.esTreeNodeToTSNodeMap) {
        return false;
    }

    const checker = services.program.getTypeChecker();
    const tsNode = services.esTreeNodeToTSNodeMap.get(node);
    const signature = checker.getSignatureFromDeclaration(tsNode);
    if (!signature) {
        return false;
    }

    return isThenableType(checker, checker.getReturnTypeOfSignature(signature));
};

const localTsPlugin = {
    rules: {
        'async-function-suffix': {
            meta: {
                type: 'problem',
                docs: {
                    description: 'Require Async suffix only for functions that return Promise-like values.',
                },
                messages: {
                    missingSuffix: 'Async function `{{name}}` should use the Async suffix.',
                    unexpectedSuffix: 'Synchronous function `{{name}}` should not use the Async suffix.',
                },
                schema: [],
            },
            create(context) {
                const checkFunction = node => {
                    if (isFrameworkNamedCallback(node)) {
                        return;
                    }

                    const name = getFunctionName(node);
                    if (!name) {
                        return;
                    }

                    if (isFrameworkFunctionName(name)) {
                        return;
                    }

                    const hasAsyncSuffix = name.endsWith('Async');
                    const isAsyncLike = isAsyncLikeFunction(context, node);
                    if (isAsyncLike && !hasAsyncSuffix) {
                        context.report({
                            node,
                            messageId: 'missingSuffix',
                            data: { name },
                        });
                    }

                    if (!isAsyncLike && hasAsyncSuffix) {
                        context.report({
                            node,
                            messageId: 'unexpectedSuffix',
                            data: { name },
                        });
                    }
                };

                return {
                    FunctionDeclaration: checkFunction,
                    FunctionExpression: checkFunction,
                    ArrowFunctionExpression: checkFunction,
                };
            },
        },
    },
};

export default [
    {
        ignores: ['**/node_modules/**', '**/dist/**', '**/dist-admin/**', '**/dist-storybook/**', '**/.angular/**', '**/*.min.js'],
    },
    {
        ignores: ['**/node_modules/**', '**/dist/**', '**/dist-admin/**', '**/dist-storybook/**', '**/.angular/**', '**/*.min.js'],
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
            '@angular-eslint': angularPlugin,
            prettier: prettierPlugin,
            'simple-import-sort': simpleImportSortPlugin,
            local: localTsPlugin,
        },
        rules: {
            ...eslintConfigPrettier.rules,
            'no-console': 'warn',
            'object-shorthand': ['error', 'always'],
            curly: ['error', 'all'],
            'no-redeclare': 'error',
            quotes: ['error', 'single', { avoidEscape: true }],
            'keyword-spacing': ['error', { after: true }],
            'prefer-const': 'error',
            eqeqeq: ['error', 'always'],
            'no-unreachable': 'error',
            'simple-import-sort/imports': 'error',
            'simple-import-sort/exports': 'error',
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
            '@typescript-eslint/consistent-type-imports': [
                'error',
                {
                    prefer: 'type-imports',
                    fixStyle: 'inline-type-imports',
                },
            ],
            '@typescript-eslint/no-import-type-side-effects': 'error',
            '@typescript-eslint/no-explicit-any': 'error',
            '@typescript-eslint/no-require-imports': 'error',
            '@typescript-eslint/no-shadow': 'warn',
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
        languageOptions: {
            parserOptions: {
                projectService: true,
                tsconfigRootDir: import.meta.dirname,
            },
        },
        rules: {
            '@typescript-eslint/no-floating-promises': [
                'error',
                {
                    ignoreVoid: true,
                },
            ],
            '@typescript-eslint/no-misused-promises': [
                'error',
                {
                    checksVoidReturn: {
                        attributes: false,
                    },
                },
            ],
            '@typescript-eslint/no-unnecessary-condition': 'error',
            '@typescript-eslint/no-confusing-void-expression': 'error',
            '@typescript-eslint/no-unnecessary-type-assertion': 'error',
            '@typescript-eslint/prefer-optional-chain': 'error',
            '@typescript-eslint/prefer-nullish-coalescing': 'warn',
            '@typescript-eslint/require-await': 'error',
            '@typescript-eslint/prefer-readonly': 'error',
            'local/async-function-suffix': 'error',
        },
    },
    {
        files: ['**/*.ts'],
        ignores: ['**/*.spec.ts', '**/*.stories.ts'],
        rules: {
            '@angular-eslint/computed-must-return': 'error',
            '@angular-eslint/prefer-signals': 'error',
            '@angular-eslint/prefer-on-push-component-change-detection': 'error',
            '@angular-eslint/no-attribute-decorator': 'error',
            '@angular-eslint/no-duplicates-in-metadata-arrays': 'error',
            '@angular-eslint/no-lifecycle-call': 'error',
            '@angular-eslint/no-pipe-impure': 'error',
            '@angular-eslint/no-uncalled-signals': 'error',
            '@angular-eslint/use-injectable-provided-in': 'warn',
            'no-restricted-syntax': [
                'error',
                {
                    selector: 'Decorator[expression.callee.name="HostListener"]',
                    message:
                        'Use the host metadata property or fromEvent() instead of @HostListener(). See: https://angular.dev/api/core/HostListener',
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
                    message:
                        'Use contentChildren() signal query instead of @ContentChildren(). See: https://angular.dev/guide/signals/queries',
                },
                {
                    selector:
                        'Decorator[expression.callee.name="Component"] CallExpression[callee.name="Component"] > ObjectExpression > Property[key.name="template"]',
                    message:
                        'Use templateUrl with a dedicated .html file instead of inline component templates. Specs may keep inline templates.',
                },
                {
                    selector:
                        'Decorator[expression.callee.name="Component"] CallExpression[callee.name="Component"] > ObjectExpression > Property[key.name="styles"]',
                    message: 'Use styleUrls with dedicated .scss files instead of inline component styles. Specs may keep inline styles.',
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
                            group: ['../../features/**', '../../../features/**', '../../../../features/**', 'src/app/features/**'],
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
                            group: ['../**/*.routes', '../../**/*.routes', '../../../**/*.routes', '../../../../**/*.routes'],
                            message: 'Feature code should depend on feature-local API/models/components, not on route configuration files.',
                        },
                    ],
                },
            ],
        },
    },
    {
        files: ['src/app/features/**/*.ts'],
        ignores: ['src/app/features/**/*.routes.ts', 'src/app/features/public/**/*.ts', 'src/app/features/**/*.spec.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: ['../../*/pages/**', '../../../*/pages/**', '../../../../*/pages/**'],
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
                            group: ['../../../!(shared)/api/**', '../../../../!(shared)/api/**', '../../../../../!(shared)/api/**'],
                            message:
                                'Feature dialogs should use shared APIs or same-feature APIs, not reach into another feature API directly.',
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
                            group: ['../../../!(shared)/api/**', '../../../../!(shared)/api/**', '../../../../../!(shared)/api/**'],
                            message:
                                'Feature components should use shared APIs or same-feature APIs, not reach into another feature API directly.',
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
                            group: ['../../!(shared)/api/**', '../../../!(shared)/api/**', '../../../../!(shared)/api/**'],
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
            'src/app/services/viewport.service.ts',
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
                            group: ['@angular/cdk/layout'],
                            message: 'Use ViewportService instead of injecting BreakpointObserver directly in feature/app code.',
                        },
                        {
                            group: ['projects/fd-ui-kit/src/lib/**', 'fd-ui-kit/src/lib/**'],
                            message: 'Import from the public fd-ui-kit barrel instead of deep-linking into UI-kit internals.',
                        },
                        {
                            group: ['../guards/**', '../../guards/**', '../../../guards/**', 'src/app/guards/**'],
                            message:
                                'Guards belong to the routing layer and should only be imported from app.routes.ts or feature *.routes.ts files.',
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
                            group: ['../**/*.routes', '../../**/*.routes', '../../../**/*.routes', '../../../../**/*.routes'],
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
                            group: ['../../*/pages/**', '../../../*/pages/**', '../../../../*/pages/**'],
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
                            message:
                                'Use fd-ui-kit dialog/menu/date primitives instead of low-level CDK overlay APIs in admin feature code.',
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
                            message:
                                'Admin code should use feature-local routes/pages/api or the explicit admin-auth boundary, not legacy global pages/services buckets.',
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
                            group: ['../guards/**', '../../guards/**', '../../../guards/**', 'projects/fooddiary-admin/src/app/guards/**'],
                            message:
                                'Admin guards belong to the routing layer and should only be imported from app.routes.ts or admin feature *.routes.ts files.',
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
            local: localTemplatePlugin,
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
            '@angular-eslint/template/banana-in-box': 'error',
            '@angular-eslint/template/button-has-type': 'error',
            '@angular-eslint/template/mouse-events-have-key-events': 'error',
            '@angular-eslint/template/no-duplicate-attributes': 'warn',
            '@angular-eslint/template/no-interpolation-in-attributes': 'warn',
            '@angular-eslint/template/prefer-self-closing-tags': 'error',
            '@angular-eslint/template/attributes-order': [
                'error',
                {
                    alphabetical: false,
                    order: [
                        'STRUCTURAL_DIRECTIVE',
                        'TEMPLATE_REFERENCE',
                        'ATTRIBUTE_BINDING',
                        'INPUT_BINDING',
                        'TWO_WAY_BINDING',
                        'OUTPUT_BINDING',
                    ],
                },
            ],
            'local/fd-ui-button-accessible-name': 'error',
        },
    },
];
