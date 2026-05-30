/* eslint-disable @typescript-eslint/explicit-function-return-type -- local rule helpers are inferred from ESLint AST shapes */
import angularPlugin from '@angular-eslint/eslint-plugin';
import templatePlugin from '@angular-eslint/eslint-plugin-template';
import templateParser from '@angular-eslint/template-parser';
import eslintCommentsPlugin from '@eslint-community/eslint-plugin-eslint-comments';
import tsPlugin from '@typescript-eslint/eslint-plugin';
import parser from '@typescript-eslint/parser';
import eslintConfigPrettier from 'eslint-config-prettier';
import boundariesPlugin from 'eslint-plugin-boundaries';
import prettierPlugin from 'eslint-plugin-prettier';
import rxjsXPlugin from 'eslint-plugin-rxjs-x';
import securityPlugin from 'eslint-plugin-security';
import simpleImportSortPlugin from 'eslint-plugin-simple-import-sort';
import sonarjsPlugin from 'eslint-plugin-sonarjs';
import unicornPlugin from 'eslint-plugin-unicorn';

const securityRecommendedRules = Object.fromEntries(
    Object.keys(securityPlugin.configs.recommended.rules).map(ruleName => [ruleName, 'error']),
);

const unicornCandidateRules = {
    'unicorn/better-regex': 'error',
    'unicorn/catch-error-name': 'error',
    'unicorn/consistent-date-clone': 'error',
    'unicorn/consistent-empty-array-spread': 'error',
    'unicorn/consistent-existence-index-check': 'error',
    'unicorn/consistent-template-literal-escape': 'error',
    'unicorn/custom-error-definition': 'error',
    'unicorn/error-message': 'error',
    'unicorn/escape-case': 'error',
    'unicorn/explicit-length-check': 'error',
    'unicorn/filename-case': [
        'error',
        {
            case: 'kebabCase',
            ignore: ['^_.*\\.scss$'],
        },
    ],
    'unicorn/new-for-builtins': 'error',
    'unicorn/no-abusive-eslint-disable': 'error',
    'unicorn/no-accessor-recursion': 'error',
    'unicorn/no-await-expression-member': 'error',
    'unicorn/no-await-in-promise-methods': 'error',
    'unicorn/no-console-spaces': 'error',
    'unicorn/no-document-cookie': 'error',
    'unicorn/no-empty-file': 'error',
    'unicorn/no-hex-escape': 'error',
    'unicorn/no-instanceof-builtins': 'error',
    'unicorn/no-invalid-fetch-options': 'error',
    'unicorn/no-invalid-remove-event-listener': 'error',
    'unicorn/no-magic-array-flat-depth': 'error',
    'unicorn/no-negation-in-equality-check': 'error',
    'unicorn/no-new-array': 'error',
    'unicorn/no-new-buffer': 'error',
    'unicorn/no-object-as-default-parameter': 'error',
    'unicorn/no-process-exit': 'error',
    'unicorn/no-single-promise-in-promise-methods': 'error',
    'unicorn/no-static-only-class': 'error',
    'unicorn/no-this-assignment': 'error',
    'unicorn/no-typeof-undefined': 'error',
    'unicorn/no-unnecessary-array-flat-depth': 'error',
    'unicorn/no-unnecessary-array-splice-count': 'error',
    'unicorn/no-unnecessary-await': 'error',
    'unicorn/no-unnecessary-slice-end': 'error',
    'unicorn/no-unreadable-array-destructuring': 'error',
    'unicorn/no-unreadable-iife': 'error',
    'unicorn/no-useless-collection-argument': 'error',
    'unicorn/no-useless-error-capture-stack-trace': 'error',
    'unicorn/no-useless-fallback-in-spread': 'error',
    'unicorn/no-useless-iterator-to-array': 'error',
    'unicorn/no-useless-length-check': 'error',
    'unicorn/no-useless-promise-resolve-reject': 'error',
    'unicorn/no-useless-spread': 'error',
    'unicorn/no-useless-switch-case': 'error',
    'unicorn/no-useless-undefined': 'error',
    'unicorn/no-zero-fractions': 'error',
    'unicorn/prefer-array-find': 'error',
    'unicorn/prefer-array-flat': 'error',
    'unicorn/prefer-array-flat-map': 'error',
    'unicorn/prefer-array-index-of': 'error',
    'unicorn/prefer-array-some': 'error',
    'unicorn/prefer-at': 'error',
    'unicorn/prefer-class-fields': 'error',
    'unicorn/prefer-code-point': 'error',
    'unicorn/prefer-date-now': 'error',
    'unicorn/prefer-default-parameters': 'error',
    'unicorn/prefer-includes': 'error',
    'unicorn/prefer-logical-operator-over-ternary': 'error',
    'unicorn/prefer-math-min-max': 'error',
    'unicorn/prefer-math-trunc': 'error',
    'unicorn/prefer-negative-index': 'error',
    'unicorn/prefer-number-properties': 'error',
    'unicorn/prefer-object-from-entries': 'error',
    'unicorn/prefer-optional-catch-binding': 'error',
    'unicorn/prefer-regexp-test': 'error',
    'unicorn/prefer-set-has': 'error',
    'unicorn/prefer-set-size': 'error',
    'unicorn/prefer-simple-condition-first': 'error',
    'unicorn/prefer-string-replace-all': 'error',
    'unicorn/prefer-string-slice': 'error',
    'unicorn/prefer-string-starts-ends-with': 'error',
    'unicorn/prefer-string-trim-start-end': 'error',
    'unicorn/relative-url-style': 'error',
    'unicorn/require-array-join-separator': 'error',
    'unicorn/require-number-to-fixed-digits-argument': 'error',
    'unicorn/switch-case-braces': 'error',
    'unicorn/switch-case-break-position': 'error',
    'unicorn/text-encoding-identifier-case': 'error',
    'unicorn/throw-new-error': 'error',
};

const getTemplateAttributes = node => [...(node.attributes ?? []), ...(node.inputs ?? [])];

const hasTemplateAttribute = (node, name) => getTemplateAttributes(node).some(attribute => attribute.name === name);

const mojibakeMarkers = [
    { codePoint: 0xfffd, label: 'replacement character' },
    { codePoint: 0x00c2, label: 'U+00C2' },
    { codePoint: 0x00c3, label: 'U+00C3' },
    { codePoint: 0x00d0, label: 'U+00D0' },
    { codePoint: 0x00d1, label: 'U+00D1' },
    { codePoint: 0x00f0, label: 'U+00F0' },
].map(marker => ({
    ...marker,
    value: String.fromCodePoint(marker.codePoint),
}));

const createNoMojibakeRule = context => ({
    Program() {
        const source = context.sourceCode.getText();

        for (const marker of mojibakeMarkers) {
            const index = source.indexOf(marker.value);
            if (index === -1) {
                continue;
            }

            context.report({
                loc: context.sourceCode.getLocFromIndex(index),
                messageId: 'mojibake',
                data: {
                    marker: marker.label,
                },
            });
        }
    },
});

const noMojibakeRule = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Disallow common mojibake and replacement-character artifacts in source files.',
        },
        messages: {
            mojibake: 'Possible mojibake or encoding artifact: `{{marker}}`.',
        },
        schema: [],
    },
    create: createNoMojibakeRule,
};

const componentFileSuffixPattern = /\.component(?:\.spec)?\.(?:html|js|ts)$/;

const createNoComponentFileSuffixRule = context => ({
    Program(node) {
        const fileName = (context.physicalFilename ?? context.filename ?? '').replaceAll('\\', '/');

        if (!componentFileSuffixPattern.test(fileName)) {
            return;
        }

        context.report({
            node,
            messageId: 'componentFileSuffix',
        });
    },
});

const noComponentFileSuffixRule = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Disallow .component in Angular component sidecar file names.',
        },
        messages: {
            componentFileSuffix:
                'Use one component base file name without `.component`, for example `user-profile.ts`, `user-profile.html`, and `user-profile.scss`.',
        },
        schema: [],
    },
    create: createNoComponentFileSuffixRule,
};

const legacyTypeBucketFilePattern =
    /(?:^|\/)(?:src\/app\/(?:components\/(?!shared\/)|directives\/|guards\/|pipes\/|services\/|validators\/)|projects\/fooddiary-admin\/src\/app\/(?:guards\/|pages\/|services\/)).+\.(?:js|ts)$/;

const allowedLegacyTypeBucketFilePatterns = [
    /(?:^|\/)src\/app\/services\/(?:api|auth|error-handler|frontend-logger|frontend-observability|global-loading|jwt-decoder|logging-api|navigation|route-loading|seo|token-storage|unsaved-changes)\.service(?:\.spec)?\.ts$/,
    /(?:^|\/)src\/app\/services\/idle-selective-preloading\.strategy(?:\.spec)?\.ts$/,
    /(?:^|\/)src\/app\/guards\/(?:auth|dietologist|logged-in|unsaved-changes)\.guard(?:\.spec)?\.ts$/,
    /(?:^|\/)projects\/fooddiary-admin\/src\/app\/guards\/admin-auth\.guard(?:\.spec)?\.ts$/,
];

const createNoLegacyTypeBucketFileRule = context => ({
    Program(node) {
        const fileName = (context.physicalFilename ?? context.filename ?? '').replaceAll('\\', '/');

        if (!legacyTypeBucketFilePattern.test(fileName)) {
            return;
        }

        if (allowedLegacyTypeBucketFilePatterns.some(pattern => pattern.test(fileName))) {
            return;
        }

        context.report({
            node,
            messageId: 'legacyTypeBucket',
        });
    },
});

const noLegacyTypeBucketFileRule = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Disallow new Angular files in legacy type bucket folders.',
        },
        messages: {
            legacyTypeBucket:
                'Place Angular code by feature area or an explicit shared/common theme instead of legacy type buckets such as services, guards, directives, pipes, validators, pages, or top-level components.',
        },
        schema: [],
    },
    create: createNoLegacyTypeBucketFileRule,
};

const angularSpecificFieldCallees = new Set([
    'inject',
    'input',
    'output',
    'model',
    'viewChild',
    'viewChildren',
    'contentChild',
    'contentChildren',
]);

const unwrapExpression = expression => {
    let current = expression;

    while (
        current?.type === 'ChainExpression' ||
        current?.type === 'TSAsExpression' ||
        current?.type === 'TSTypeAssertion' ||
        current?.type === 'TSNonNullExpression'
    ) {
        current = current.expression;
    }

    return current;
};

const getAngularSpecificFieldKind = node => {
    const initializer = unwrapExpression(node.value);

    if (initializer?.type !== 'CallExpression' || initializer.callee.type !== 'Identifier') {
        return null;
    }

    return angularSpecificFieldCallees.has(initializer.callee.name) ? initializer.callee.name : null;
};

const isMethodLikeClassElement = node =>
    node.type === 'MethodDefinition' ||
    node.type === 'TSAbstractMethodDefinition' ||
    (node.type === 'PropertyDefinition' && node.value?.type === 'FunctionExpression');

const createAngularSpecificFieldsBeforeMethodsRule = context => ({
    ClassBody(node) {
        let seenMethod = false;

        for (const member of node.body) {
            if (isMethodLikeClassElement(member)) {
                seenMethod = true;
                continue;
            }

            if (member.type !== 'PropertyDefinition' || !seenMethod) {
                continue;
            }

            const fieldKind = getAngularSpecificFieldKind(member);

            if (fieldKind === null) {
                continue;
            }

            context.report({
                node: member,
                messageId: 'angularFieldAfterMethod',
                data: {
                    fieldKind,
                },
            });
        }
    },
});

const angularSpecificFieldsBeforeMethodsRule = {
    meta: {
        type: 'suggestion',
        docs: {
            description: 'Require Angular-specific class fields to be declared before methods.',
        },
        messages: {
            angularFieldAfterMethod:
                'Move this `{{fieldKind}}()` field above constructors, getters, setters, and methods so Angular-specific fields stay grouped at the top of the class.',
        },
        schema: [],
    },
    create: createAngularSpecificFieldsBeforeMethodsRule,
};

const presentationFilePattern =
    /(?:^|\/)(?:src\/app\/(?:components\/shared|shared\/dialogs|features\/[^/]+\/(?:components|dialogs|pages))|projects\/fooddiary-admin\/src\/app\/features\/[^/]+\/(?:components|dialogs|pages))\/.+\.ts$/;

const isPresentationFile = context => {
    const fileName = (context.physicalFilename ?? context.filename ?? '').replaceAll('\\', '/');

    return !/\.(?:spec|stories)\.ts$/.test(fileName) && presentationFilePattern.test(fileName);
};

const importsIdentifier = (node, name) =>
    node.specifiers.some(specifier => {
        if (specifier.type === 'ImportSpecifier') {
            return getPropertyName(specifier.imported) === name;
        }

        return specifier.type === 'ImportNamespaceSpecifier' && name === '*';
    });

const createNoHttpClientInPresentationRule = context => ({
    ImportDeclaration(node) {
        if (!isPresentationFile(context) || node.source.value !== '@angular/common/http') {
            return;
        }

        if (!importsIdentifier(node, 'HttpClient')) {
            return;
        }

        context.report({
            node: node.source,
            messageId: 'httpClientInPresentation',
        });
    },
});

const noHttpClientInPresentationRule = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Disallow direct HttpClient usage in presentation-layer Angular files.',
        },
        messages: {
            httpClientInPresentation:
                'Presentation files should not use HttpClient directly. Move transport code to an api service or feature lib/facade and keep the component focused on presentation.',
        },
        schema: [],
    },
    create: createNoHttpClientInPresentationRule,
};

const allowedPresentationApiImports = new Set();

const getProjectRelativeFileName = context => {
    const fileName = (context.physicalFilename ?? context.filename ?? '').replaceAll('\\', '/');
    const projectStart = fileName.search(/(?:^|\/)(?:src\/app|projects\/fooddiary-admin\/src\/app)\//);

    return projectStart === -1 ? fileName : fileName.slice(fileName[projectStart] === '/' ? projectStart + 1 : projectStart);
};

const isApiImportSource = value => typeof value === 'string' && value.includes('/api/');

const createNoNewApiImportInPresentationRule = context => ({
    ImportDeclaration(node) {
        if (!isPresentationFile(context) || node.importKind === 'type' || !isApiImportSource(node.source.value)) {
            return;
        }

        const importKey = `${getProjectRelativeFileName(context)}::${node.source.value}`;

        if (allowedPresentationApiImports.has(importKey)) {
            return;
        }

        context.report({
            node: node.source,
            messageId: 'apiImportInPresentation',
        });
    },
});

const noNewApiImportInPresentationRule = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Disallow new direct API imports in presentation-layer Angular files.',
        },
        messages: {
            apiImportInPresentation:
                'Do not add new direct API imports to presentation files. Route data and behavior through a feature lib/facade so components, dialogs, and pages stay focused on presentation.',
        },
        schema: [],
    },
    create: createNoNewApiImportInPresentationRule,
};

const noAnyCastSyntax = [
    {
        selector: 'ImportDeclaration[source.value=/^rxjs\\/internal(\\/|$)/]',
        message: 'Do not import from rxjs/internal. Use the public rxjs API.',
    },
    {
        selector: 'ImportDeclaration[source.value="rxjs/operators"]',
        message: 'Import RxJS operators from rxjs instead of rxjs/operators.',
    },
    {
        selector: 'ImportDeclaration[source.value=/^@angular\\/.*\\/src(\\/|$)/]',
        message: 'Do not import Angular internals. Use public Angular APIs.',
    },
    {
        selector: 'TSAsExpression[typeAnnotation.type="TSAnyKeyword"]',
        message: 'Do not cast to any. Fix the type or narrow the value instead.',
    },
    {
        selector: 'TSTypeAssertion[typeAnnotation.type="TSAnyKeyword"]',
        message: 'Do not cast to any. Fix the type or narrow the value instead.',
    },
];

const noRedundantBooleanComparisonSyntax = [
    {
        selector:
            'BinaryExpression[operator="==="][left.type="CallExpression"][left.callee.object.name="Number"][left.callee.property.name="isNaN"][right.value=false]',
        message: 'Use !Number.isNaN(value) instead of comparing Number.isNaN(value) with false.',
    },
    {
        selector:
            'BinaryExpression[operator="==="][left.value=false][right.type="CallExpression"][right.callee.object.name="Number"][right.callee.property.name="isNaN"]',
        message: 'Use !Number.isNaN(value) instead of comparing Number.isNaN(value) with false.',
    },
];

const restrictedBrowserGlobals = new Set(['window', 'document', 'navigator', 'localStorage', 'sessionStorage']);
const importIdentifierDeclarations = new Set(['ImportSpecifier', 'ImportDefaultSpecifier', 'ImportNamespaceSpecifier']);
const namedIdentifierDeclarations = new Set(['VariableDeclarator', 'FunctionDeclaration', 'FunctionExpression']);

const isTypeScriptIdentifier = node => node.parent?.type.startsWith('TS') === true;

const isObjectIdentifierKey = (node, parent) =>
    (parent.type === 'MemberExpression' && parent.property === node && !parent.computed) ||
    ((parent.type === 'Property' || parent.type === 'PropertyDefinition' || parent.type === 'MethodDefinition') && parent.key === node);

const isIdentifierDeclaration = (node, parent) =>
    importIdentifierDeclarations.has(parent.type) ||
    (namedIdentifierDeclarations.has(parent.type) && parent.id === node) ||
    (parent.type === 'AssignmentPattern' && parent.left === node);

const isIdentifierDeclarationOrKey = node => {
    const parent = node.parent;
    if (!parent) {
        return false;
    }

    if (isTypeScriptIdentifier(node)) {
        return true;
    }

    return isObjectIdentifierKey(node, parent) || isIdentifierDeclaration(node, parent);
};

const createNoBrowserGlobalsRule = context => ({
    Identifier(node) {
        if (!restrictedBrowserGlobals.has(node.name) || isIdentifierDeclarationOrKey(node)) {
            return;
        }

        context.report({
            node,
            messageId: 'browserGlobal',
            data: {
                name: node.name,
            },
        });
    },
});

const noBrowserGlobalsRule = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Disallow direct browser globals in Angular runtime code.',
        },
        messages: {
            browserGlobal:
                'Do not use global `{{name}}` directly in runtime code. Inject DOCUMENT, use Renderer2/RendererFactory2 when mutating DOM, and guard browser-only work with isPlatformBrowser.',
        },
        schema: [],
    },
    create: createNoBrowserGlobalsRule,
};

const appBoundaryElements = [
    { type: 'app-shared-models', pattern: 'src/app/shared/models', mode: 'folder' },
    { type: 'app-shared-api', pattern: 'src/app/shared/api', mode: 'folder' },
    { type: 'app-shared-lib', pattern: 'src/app/shared/lib', mode: 'folder' },
    { type: 'app-shared-dialogs', pattern: 'src/app/shared/dialogs', mode: 'folder' },
    { type: 'app-shared-forms', pattern: 'src/app/shared/forms', mode: 'folder' },
    { type: 'app-shared-i18n', pattern: 'src/app/shared/i18n', mode: 'folder' },
    { type: 'app-shared-notifications', pattern: 'src/app/shared/notifications', mode: 'folder' },
    { type: 'app-shared-platform', pattern: 'src/app/shared/platform', mode: 'folder' },
    { type: 'app-shared-theme', pattern: 'src/app/shared/theme', mode: 'folder' },
    { type: 'app-shared-ui-code', pattern: 'src/app/shared/ui', mode: 'folder' },
    { type: 'app-shared-ui', pattern: 'src/app/components/shared', mode: 'folder' },
    { type: 'app-feature-api', pattern: 'src/app/features/(*)/api', mode: 'folder', capture: ['feature'] },
    { type: 'app-feature-models', pattern: 'src/app/features/(*)/models', mode: 'folder', capture: ['feature'] },
    { type: 'app-feature-components', pattern: 'src/app/features/(*)/components', mode: 'folder', capture: ['feature'] },
    { type: 'app-feature-dialogs', pattern: 'src/app/features/(*)/dialogs', mode: 'folder', capture: ['feature'] },
    { type: 'app-feature-lib', pattern: 'src/app/features/(*)/lib', mode: 'folder', capture: ['feature'] },
    { type: 'app-feature-resolvers', pattern: 'src/app/features/(*)/resolvers', mode: 'folder', capture: ['feature'] },
    { type: 'app-feature-pages', pattern: 'src/app/features/(*)/pages', mode: 'folder', capture: ['feature'] },
    { type: 'app-feature-routes', pattern: 'src/app/features/(*)/*.routes.ts', mode: 'file', capture: ['feature', 'file'] },
    { type: 'admin-feature-api', pattern: 'projects/fooddiary-admin/src/app/features/(*)/api', mode: 'folder', capture: ['feature'] },
    {
        type: 'admin-feature-components',
        pattern: 'projects/fooddiary-admin/src/app/features/(*)/components',
        mode: 'folder',
        capture: ['feature'],
    },
    {
        type: 'admin-feature-dialogs',
        pattern: 'projects/fooddiary-admin/src/app/features/(*)/dialogs',
        mode: 'folder',
        capture: ['feature'],
    },
    { type: 'admin-feature-pages', pattern: 'projects/fooddiary-admin/src/app/features/(*)/pages', mode: 'folder', capture: ['feature'] },
    {
        type: 'admin-feature-routes',
        pattern: 'projects/fooddiary-admin/src/app/features/(*)/*.routes.ts',
        mode: 'file',
        capture: ['feature', 'file'],
    },
];

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

const labelWrappedControlNames = new Set(['input', 'select', 'textarea']);

const hasLabelWrappedControl = nodes =>
    (nodes ?? []).some(node => {
        if (node.type === 'Element' && labelWrappedControlNames.has(node.name)) {
            return true;
        }

        if (node.type === 'Element' || node.type === 'Template') {
            return hasLabelWrappedControl(node.children);
        }

        return false;
    });

const genericEventHandlerNamePattern = /^(?:handle[A-Z]\w*|onClick)$/;

const isGenericEventHandlerName = name => typeof name === 'string' && genericEventHandlerNamePattern.test(name);

const getTemplateCallName = node => {
    if (node?.type !== 'Call' || node.receiver?.type !== 'PropertyRead') {
        return null;
    }

    return node.receiver.receiver?.type === 'ImplicitReceiver' ? node.receiver.name : null;
};

const visitTemplateExpression = (node, visitor) => {
    if (!node || typeof node !== 'object') {
        return;
    }

    visitor(node);

    for (const childKey of ['ast', 'expressions', 'receiver', 'args', 'condition', 'trueExp', 'falseExp', 'left', 'right', 'exp']) {
        const child = node[childKey];

        if (Array.isArray(child)) {
            for (const item of child) {
                visitTemplateExpression(item, visitor);
            }
            continue;
        }

        visitTemplateExpression(child, visitor);
    }
};

const createActionOrientedTemplateEventHandlersRule = context => ({
    BoundEvent(node) {
        visitTemplateExpression(node.handler, expression => {
            const handlerName = getTemplateCallName(expression);

            if (!isGenericEventHandlerName(handlerName)) {
                return;
            }

            context.report({
                node: expression,
                messageId: 'genericEventHandler',
                data: {
                    name: handlerName,
                },
            });
        });
    },
});

const actionOrientedTemplateEventHandlersRule = {
    meta: {
        type: 'suggestion',
        docs: {
            description: 'Require Angular template event handlers to be named after the user action.',
        },
        messages: {
            genericEventHandler:
                'Rename `{{name}}` to the user action or outcome, for example `openCard()` instead of `handleClick()` or `handleOpen()`.',
        },
        schema: [],
    },
    create: createActionOrientedTemplateEventHandlersRule,
};

const localTemplatePlugin = {
    rules: {
        'no-mojibake': noMojibakeRule,
        'no-component-file-suffix': noComponentFileSuffixRule,
        'action-oriented-event-handlers': actionOrientedTemplateEventHandlersRule,
        'no-label-wrapped-control': {
            meta: {
                type: 'problem',
                docs: {
                    description: 'Disallow wrapping native form controls inside labels.',
                },
                messages: {
                    wrappedControl: 'Place native form controls outside `<label>` and associate them with explicit `for`/`id` attributes.',
                },
                schema: [],
            },
            create(context) {
                return {
                    Element(node) {
                        if (node.name !== 'label' || !hasLabelWrappedControl(node.children)) {
                            return;
                        }

                        context.report({
                            node,
                            messageId: 'wrappedControl',
                        });
                    },
                };
            },
        },
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

const isSubscribeCall = node =>
    node.type === 'CallExpression' && node.callee.type === 'MemberExpression' && getPropertyName(node.callee.property) === 'subscribe';

const isInjectableCallExpression = node =>
    node?.type === 'CallExpression' &&
    node.callee.type === 'Identifier' &&
    node.callee.name === 'Injectable' &&
    node.arguments.length === 1;

const getSingleObjectProperty = node => {
    if (node?.type !== 'ObjectExpression' || node.properties.length !== 1) {
        return null;
    }

    return node.properties[0];
};

const isRootProvidedInProperty = property => {
    if (property?.type !== 'Property' || property.computed || getPropertyName(property.key) !== 'providedIn') {
        return false;
    }

    return property.value.type === 'Literal' && property.value.value === 'root';
};

const isRootInjectableDecorator = node => {
    if (!isInjectableCallExpression(node.expression)) {
        return false;
    }

    const [metadata] = node.expression.arguments;
    return isRootProvidedInProperty(getSingleObjectProperty(metadata));
};

const angularClassDecoratorNames = new Set(['Component', 'Directive']);
const angularPublicApiInitializerNames = new Set([
    'input',
    'model',
    'output',
    'viewChild',
    'viewChildren',
    'contentChild',
    'contentChildren',
]);
const frameworkPublicMemberNames = new Set([
    'focusFirstItem',
    'focusLastItem',
    'openFilePicker',
    'registerOnChange',
    'registerOnTouched',
    'registerOnValidatorChange',
    'setDisabledState',
    'templateRef',
    'validate',
    'writeValue',
]);

const getDecoratorCallName = decorator => {
    const expression = decorator.expression;

    if (expression.type === 'CallExpression') {
        return getPropertyName(expression.callee);
    }

    return getPropertyName(expression);
};

const hasAngularClassDecorator = node =>
    (node.decorators ?? []).some(decorator => angularClassDecoratorNames.has(getDecoratorCallName(decorator)));

const getCallExpressionBaseName = node => {
    if (node.type === 'Identifier') {
        return node.name;
    }

    if (node.type === 'CallExpression') {
        return getCallExpressionBaseName(node.callee);
    }

    if (node.type === 'MemberExpression') {
        return getCallExpressionBaseName(node.object);
    }

    return null;
};

const hasAngularPublicApiInitializer = node => {
    if (node.type !== 'PropertyDefinition') {
        return false;
    }

    const baseName = node.value ? getCallExpressionBaseName(node.value) : null;
    return baseName !== null && angularPublicApiInitializerNames.has(baseName);
};

const shouldAllowPublicAngularMember = node => {
    if (node.kind === 'constructor') {
        return true;
    }

    const memberName = getPropertyName(node.key);
    return hasAngularPublicApiInitializer(node) || frameworkPublicMemberNames.has(memberName);
};

const createPreferProtectedTemplateMembersRule = context => {
    const sourceCode = context.sourceCode;

    return {
        ClassDeclaration(node) {
            if (!hasAngularClassDecorator(node)) {
                return;
            }

            for (const member of node.body.body) {
                if (member.accessibility !== 'public' || shouldAllowPublicAngularMember(member)) {
                    continue;
                }

                context.report({
                    node: member.key,
                    messageId: 'preferProtected',
                    fix(fixer) {
                        const publicToken = sourceCode.getFirstToken(member, token => token.value === 'public');

                        return publicToken ? fixer.replaceText(publicToken, 'protected') : null;
                    },
                });
            }
        },
    };
};

const preferProtectedTemplateMembersRule = {
    meta: {
        type: 'suggestion',
        fixable: 'code',
        docs: {
            description: 'Prefer protected members for Angular component and directive internals used by templates.',
        },
        messages: {
            preferProtected:
                'Angular component/directive internals should be protected or private. Keep public only for input(), output(), model(), signal queries, or framework contract methods.',
        },
        schema: [],
    },
    create: createPreferProtectedTemplateMembersRule,
};

const hostEventPropertyNamePattern = /^\(.+\)$/;
const genericHostEventHandlerPattern = /\b(?:handle[A-Z]\w*|onClick)\s*\(/;

const createActionOrientedHostEventHandlersRule = context => ({
    Property(node) {
        if (node.computed || !hostEventPropertyNamePattern.test(getPropertyName(node.key) ?? '')) {
            return;
        }

        if (node.value.type !== 'Literal' || typeof node.value.value !== 'string') {
            return;
        }

        const [handlerName] = node.value.value.match(/\b(?:handle[A-Z]\w*|onClick)\b/) ?? [];

        if (!handlerName || !genericHostEventHandlerPattern.test(node.value.value)) {
            return;
        }

        context.report({
            node: node.value,
            messageId: 'genericHostEventHandler',
            data: {
                name: handlerName,
            },
        });
    },
});

const actionOrientedHostEventHandlersRule = {
    meta: {
        type: 'suggestion',
        docs: {
            description: 'Require Angular host event handlers to be named after the user action.',
        },
        messages: {
            genericHostEventHandler:
                'Rename `{{name}}` to the user action or outcome, for example `openMenu()` instead of `handleClick()` or `onClick()`.',
        },
        schema: [],
    },
    create: createActionOrientedHostEventHandlersRule,
};

const createNoLocallyCaughtThrowRule = context => {
    let caughtTryDepth = 0;
    const functionTryDepthStack = [];

    const enterFunction = () => {
        functionTryDepthStack.push(caughtTryDepth);
        caughtTryDepth = 0;
    };

    const exitFunction = () => {
        caughtTryDepth = functionTryDepthStack.pop() ?? 0;
    };

    return {
        FunctionDeclaration: enterFunction,
        'FunctionDeclaration:exit': exitFunction,
        FunctionExpression: enterFunction,
        'FunctionExpression:exit': exitFunction,
        ArrowFunctionExpression: enterFunction,
        'ArrowFunctionExpression:exit': exitFunction,
        TryStatement(node) {
            if (node.handler) {
                caughtTryDepth += 1;
            }
        },
        'TryStatement:exit'(node) {
            if (node.handler) {
                caughtTryDepth -= 1;
            }
        },
        ThrowStatement(node) {
            if (caughtTryDepth === 0) {
                return;
            }

            context.report({
                node,
                messageId: 'locallyCaughtThrow',
            });
        },
    };
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

const isFdUiKitSelfImport = source => source === 'fd-ui-kit' || source.startsWith('fd-ui-kit/');

const createNoFdUiKitSelfImportRule = context => ({
    ImportDeclaration(node) {
        const source = node.source.value;
        if (typeof source !== 'string' || !isFdUiKitSelfImport(source)) {
            return;
        }

        context.report({
            node: node.source,
            messageId: 'selfImport',
        });
    },
});

const noFdUiKitSelfImportRule = {
    meta: {
        type: 'problem',
        docs: {
            description: 'Disallow fd-ui-kit source files from importing the package through its own public alias.',
        },
        messages: {
            selfImport: 'Use a relative import inside fd-ui-kit source files; do not import fd-ui-kit through its own package alias.',
        },
        schema: [],
    },
    create: createNoFdUiKitSelfImportRule,
};

const localTsPlugin = {
    rules: {
        'no-mojibake': noMojibakeRule,
        'no-component-file-suffix': noComponentFileSuffixRule,
        'no-legacy-type-bucket-file': noLegacyTypeBucketFileRule,
        'angular-specific-fields-before-methods': angularSpecificFieldsBeforeMethodsRule,
        'no-http-client-in-presentation': noHttpClientInPresentationRule,
        'no-new-api-import-in-presentation': noNewApiImportInPresentationRule,
        'no-browser-globals': noBrowserGlobalsRule,
        'no-fd-ui-kit-self-import': noFdUiKitSelfImportRule,
        'prefer-protected-template-members': preferProtectedTemplateMembersRule,
        'action-oriented-host-event-handlers': actionOrientedHostEventHandlersRule,
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
        'no-nested-subscribe': {
            meta: {
                type: 'problem',
                docs: {
                    description: 'Disallow subscribe() calls inside another subscribe() callback.',
                },
                messages: {
                    nestedSubscribe: 'Avoid nested subscribe(). Compose observables with switchMap, concatMap, mergeMap, or exhaustMap.',
                },
                schema: [],
            },
            create(context) {
                let subscribeDepth = 0;

                return {
                    CallExpression(node) {
                        if (!isSubscribeCall(node)) {
                            return;
                        }

                        if (subscribeDepth > 0) {
                            context.report({
                                node,
                                messageId: 'nestedSubscribe',
                            });
                        }

                        subscribeDepth += 1;
                    },
                    'CallExpression:exit'(node) {
                        if (isSubscribeCall(node)) {
                            subscribeDepth -= 1;
                        }
                    },
                };
            },
        },
        'injectable-provided-in-root-single-line': {
            meta: {
                type: 'layout',
                docs: {
                    description: "Require @Injectable({ providedIn: 'root' }) decorators to stay on one line.",
                },
                fixable: 'code',
                messages: {
                    multiline: "Use @Injectable({ providedIn: 'root' }) on one line.",
                },
                schema: [],
            },
            create(context) {
                return {
                    Decorator(node) {
                        if (!isRootInjectableDecorator(node)) {
                            return;
                        }

                        const source = context.sourceCode.getText(node);
                        if (!source.includes('\n')) {
                            return;
                        }

                        context.report({
                            node,
                            messageId: 'multiline',
                            fix: fixer => fixer.replaceText(node, "@Injectable({ providedIn: 'root' })"),
                        });
                    },
                };
            },
        },
        'no-locally-caught-throw': {
            meta: {
                type: 'problem',
                docs: {
                    description: 'Disallow throwing exceptions inside try blocks that have a local catch handler.',
                },
                messages: {
                    locallyCaughtThrow: 'Do not throw only to catch locally. Use an explicit control-flow branch instead.',
                },
                schema: [],
            },
            create: createNoLocallyCaughtThrowRule,
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
            'eslint-comments': eslintCommentsPlugin,
            prettier: prettierPlugin,
            'simple-import-sort': simpleImportSortPlugin,
            boundaries: boundariesPlugin,
            security: securityPlugin,
            sonarjs: sonarjsPlugin,
            'rxjs-x': rxjsXPlugin,
            unicorn: unicornPlugin,
            local: localTsPlugin,
        },
        settings: {
            'boundaries/include': ['src/app/**/*.ts', 'projects/fooddiary-admin/src/app/**/*.ts'],
            'boundaries/ignore': ['**/*.spec.ts'],
            'boundaries/elements': appBoundaryElements,
        },
        rules: {
            ...eslintConfigPrettier.rules,
            ...securityRecommendedRules,
            ...unicornCandidateRules,
            'security/detect-object-injection': 'off',
            complexity: ['error', 10],
            'no-alert': 'error',
            'no-console': 'error',
            'no-constant-condition': ['error', { checkLoops: true }],
            'no-debugger': 'error',
            'local/no-component-file-suffix': 'error',
            'local/no-legacy-type-bucket-file': 'error',
            'local/angular-specific-fields-before-methods': 'error',
            'local/no-http-client-in-presentation': 'error',
            'local/no-new-api-import-in-presentation': 'error',
            'local/no-mojibake': 'error',
            'local/no-locally-caught-throw': 'error',
            'no-else-return': 'error',
            'no-implicit-coercion': 'error',
            'no-lonely-if': 'error',
            'no-new': 'error',
            'no-new-wrappers': 'error',
            'no-var': 'error',
            'max-depth': ['error', 4],
            'max-lines-per-function': [
                'error',
                {
                    max: 80,
                    skipBlankLines: true,
                    skipComments: true,
                },
            ],
            'max-params': ['error', 4],
            'object-shorthand': ['error', 'always'],
            curly: ['error', 'all'],
            'no-redeclare': 'error',
            'object-curly-spacing': ['error', 'always'],
            quotes: ['error', 'single', { avoidEscape: true }],
            'keyword-spacing': ['error', { after: true }],
            'prefer-const': 'error',
            'prefer-template': 'error',
            eqeqeq: ['error', 'always'],
            'no-unreachable': 'error',
            'simple-import-sort/imports': 'error',
            'simple-import-sort/exports': 'error',
            'boundaries/dependencies': [
                'error',
                {
                    default: 'allow',
                    rules: [
                        {
                            from: { type: 'app-shared-models' },
                            disallow: {
                                to: {
                                    type: ['app-shared-api', 'app-shared-dialogs', 'app-shared-ui', 'app-feature-*'],
                                },
                            },
                            message: 'shared/models must stay pure and must not depend on API, UI, or feature code.',
                        },
                        {
                            from: { type: 'app-shared-api' },
                            disallow: {
                                to: {
                                    type: ['app-shared-dialogs', 'app-shared-ui', 'app-feature-*'],
                                },
                            },
                            message: 'shared/api must not depend on UI or feature code.',
                        },
                        {
                            from: { type: 'app-shared-ui' },
                            disallow: {
                                to: {
                                    type: 'app-feature-*',
                                },
                            },
                            message: 'shared UI must stay feature-agnostic.',
                        },
                        {
                            from: { type: 'app-feature-models' },
                            disallow: {
                                to: {
                                    type: [
                                        'app-feature-api',
                                        'app-feature-components',
                                        'app-feature-dialogs',
                                        'app-feature-lib',
                                        'app-feature-pages',
                                    ],
                                },
                            },
                            message: 'Feature models must stay data-only and must not depend on API, UI, lib, or pages.',
                        },
                        {
                            from: { type: 'app-feature-api' },
                            disallow: {
                                to: {
                                    type: ['app-feature-components', 'app-feature-dialogs', 'app-feature-pages'],
                                },
                            },
                            message: 'Feature API code must not depend on feature UI or pages.',
                        },
                        {
                            from: { type: ['app-feature-components', 'app-feature-dialogs', 'app-feature-lib', 'app-feature-resolvers'] },
                            disallow: {
                                to: {
                                    type: 'app-feature-api',
                                    captured: {
                                        feature: '!({{ from.captured.feature }})',
                                    },
                                },
                            },
                            message: 'Feature internals must use their own feature API or shared APIs, not another feature API.',
                        },
                        {
                            from: { type: 'app-feature-*' },
                            disallow: {
                                to: {
                                    type: 'app-feature-pages',
                                    captured: {
                                        feature: '!({{ from.captured.feature }})',
                                    },
                                },
                            },
                            message: 'Feature code must not import pages from another feature.',
                        },
                        {
                            from: { type: ['admin-feature-components', 'admin-feature-dialogs', 'admin-feature-pages'] },
                            disallow: {
                                to: {
                                    type: 'admin-feature-api',
                                    captured: {
                                        feature: '!({{ from.captured.feature }})',
                                    },
                                },
                            },
                            message: 'Admin feature code must use its own feature API, not another admin feature API.',
                        },
                        {
                            from: { type: 'admin-feature-*' },
                            disallow: {
                                to: {
                                    type: 'admin-feature-pages',
                                    captured: {
                                        feature: '!({{ from.captured.feature }})',
                                    },
                                },
                            },
                            message: 'Admin feature code must not import pages from another admin feature.',
                        },
                    ],
                },
            ],
            'sonarjs/no-duplicated-branches': 'error',
            'sonarjs/no-identical-functions': 'error',
            'sonarjs/no-identical-expressions': 'error',
            'sonarjs/no-nested-switch': 'error',
            'sonarjs/no-redundant-boolean': 'error',
            'sonarjs/no-small-switch': 'error',
            'sonarjs/prefer-single-boolean-return': 'error',
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
            '@typescript-eslint/array-type': ['error', { default: 'array-simple' }],
            '@typescript-eslint/consistent-indexed-object-style': ['error', 'record'],
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
            '@typescript-eslint/no-shadow': 'error',
            'eslint-comments/no-unlimited-disable': 'error',
            'eslint-comments/no-unused-disable': 'error',
            'eslint-comments/require-description': 'error',
            '@typescript-eslint/no-unused-vars': [
                'error',
                {
                    vars: 'all',
                    args: 'after-used',
                    ignoreRestSiblings: true,
                    argsIgnorePattern: '^_',
                    varsIgnorePattern: '^_',
                    caughtErrors: 'all',
                    caughtErrorsIgnorePattern: '^_',
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
            '@typescript-eslint/await-thenable': 'error',
            '@typescript-eslint/ban-ts-comment': [
                'error',
                {
                    'ts-expect-error': 'allow-with-description',
                    'ts-ignore': true,
                    'ts-nocheck': true,
                    'ts-check': false,
                    minimumDescriptionLength: 10,
                },
            ],
            '@typescript-eslint/consistent-generic-constructors': 'error',
            '@typescript-eslint/consistent-type-definitions': ['error', 'type'],
            '@typescript-eslint/method-signature-style': ['error', 'property'],
            '@typescript-eslint/naming-convention': [
                'error',
                {
                    selector: 'typeLike',
                    format: ['PascalCase'],
                },
                {
                    selector: 'enumMember',
                    format: ['PascalCase', 'UPPER_CASE'],
                },
            ],
            '@typescript-eslint/no-array-delete': 'error',
            '@typescript-eslint/no-base-to-string': 'error',
            '@typescript-eslint/no-confusing-non-null-assertion': 'error',
            '@typescript-eslint/no-deprecated': 'error',
            '@typescript-eslint/no-duplicate-enum-values': 'error',
            '@typescript-eslint/no-duplicate-type-constituents': 'error',
            '@typescript-eslint/no-dynamic-delete': 'error',
            '@typescript-eslint/no-empty-object-type': 'error',
            '@typescript-eslint/no-extraneous-class': [
                'error',
                {
                    allowWithDecorator: true,
                },
            ],
            '@typescript-eslint/no-magic-numbers': [
                'error',
                {
                    ignore: [-1, 0, 1, 2],
                    ignoreArrayIndexes: true,
                    ignoreEnums: true,
                    ignoreReadonlyClassProperties: true,
                    enforceConst: true,
                },
            ],
            '@typescript-eslint/no-for-in-array': 'error',
            '@typescript-eslint/no-implied-eval': 'error',
            '@typescript-eslint/no-misused-spread': 'error',
            '@typescript-eslint/no-mixed-enums': 'error',
            '@typescript-eslint/no-non-null-asserted-optional-chain': 'error',
            '@typescript-eslint/no-non-null-assertion': 'error',
            '@typescript-eslint/no-redundant-type-constituents': 'error',
            '@typescript-eslint/no-unnecessary-template-expression': 'error',
            '@typescript-eslint/no-unnecessary-boolean-literal-compare': 'error',
            '@typescript-eslint/no-unnecessary-condition': 'error',
            '@typescript-eslint/no-confusing-void-expression': 'error',
            '@typescript-eslint/no-unnecessary-type-parameters': 'error',
            '@typescript-eslint/no-unnecessary-type-arguments': 'error',
            '@typescript-eslint/no-unnecessary-type-assertion': 'error',
            '@typescript-eslint/no-unnecessary-type-constraint': 'error',
            '@typescript-eslint/no-unsafe-argument': 'error',
            '@typescript-eslint/no-unsafe-assignment': 'error',
            '@typescript-eslint/no-unsafe-call': 'error',
            '@typescript-eslint/no-unsafe-enum-comparison': 'error',
            '@typescript-eslint/no-unsafe-member-access': 'error',
            '@typescript-eslint/no-unsafe-return': 'error',
            '@typescript-eslint/no-unsafe-type-assertion': 'error',
            '@typescript-eslint/no-wrapper-object-types': 'error',
            '@typescript-eslint/only-throw-error': 'error',
            '@typescript-eslint/prefer-find': 'error',
            '@typescript-eslint/prefer-for-of': 'error',
            '@typescript-eslint/prefer-includes': 'error',
            '@typescript-eslint/prefer-optional-chain': 'error',
            '@typescript-eslint/prefer-nullish-coalescing': 'error',
            '@typescript-eslint/prefer-promise-reject-errors': 'error',
            '@typescript-eslint/prefer-reduce-type-parameter': 'error',
            '@typescript-eslint/prefer-regexp-exec': 'error',
            '@typescript-eslint/prefer-string-starts-ends-with': 'error',
            '@typescript-eslint/promise-function-async': 'error',
            '@typescript-eslint/require-array-sort-compare': 'error',
            '@typescript-eslint/require-await': 'error',
            '@typescript-eslint/restrict-plus-operands': [
                'error',
                {
                    allowAny: false,
                    allowBoolean: false,
                    allowNullish: false,
                    allowNumberAndString: false,
                    allowRegExp: false,
                },
            ],
            '@typescript-eslint/restrict-template-expressions': 'error',
            '@typescript-eslint/return-await': ['error', 'in-try-catch'],
            '@typescript-eslint/strict-boolean-expressions': [
                'error',
                {
                    allowString: false,
                    allowNumber: false,
                    allowNullableObject: false,
                    allowNullableBoolean: false,
                    allowNullableString: false,
                    allowNullableNumber: false,
                },
            ],
            '@typescript-eslint/prefer-readonly': 'error',
            '@typescript-eslint/switch-exhaustiveness-check': 'error',
            '@typescript-eslint/unbound-method': [
                'error',
                {
                    ignoreStatic: true,
                },
            ],
            'no-restricted-syntax': ['error', ...noAnyCastSyntax, ...noRedundantBooleanComparisonSyntax],
            'local/async-function-suffix': 'error',
            'local/injectable-provided-in-root-single-line': 'error',
            'local/no-nested-subscribe': 'error',
            'rxjs-x/no-async-subscribe': 'error',
            'rxjs-x/no-implicit-any-catch': ['error', { allowExplicitAny: false }],
            'rxjs-x/no-nested-subscribe': 'error',
            'rxjs-x/no-subscribe-in-pipe': 'error',
            'rxjs-x/no-unsafe-switchmap': 'error',
            'sonarjs/cognitive-complexity': ['error', 15],
            'sonarjs/no-nested-functions': 'error',
        },
    },
    {
        files: ['**/*.spec.ts', 'src/test-setup.ts', 'src/app/testing/**/*.ts'],
        rules: {
            '@typescript-eslint/no-unsafe-type-assertion': 'off',
        },
    },
    {
        files: ['**/*.d.ts'],
        rules: {
            '@typescript-eslint/consistent-type-definitions': 'off',
            '@typescript-eslint/method-signature-style': 'off',
        },
    },
    {
        files: ['projects/fd-ui-kit/src/lib/**/*.ts'],
        ignores: ['projects/fd-ui-kit/src/lib/**/*.spec.ts', 'projects/fd-ui-kit/src/lib/**/*.stories.ts'],
        rules: {
            'local/no-fd-ui-kit-self-import': 'error',
        },
    },
    {
        files: [
            'src/app/components/shared/ai-input-bar/ai-input-bar.ts',
            'src/app/features/auth/lib/google-identity.service.ts',
            'src/app/features/premium/lib/paddle-checkout.service.ts',
            'src/app/services/auth.service.ts',
        ],
        rules: {
            // These files augment the global Window interface for browser SDKs.
            // Type aliases cannot participate in declaration merging.
            '@typescript-eslint/consistent-type-definitions': 'off',
        },
    },
    {
        files: ['src/app/**/*.ts', 'projects/fooddiary-admin/src/app/**/*.ts', 'projects/fd-ui-kit/src/lib/**/*.ts'],
        ignores: [
            '**/*.spec.ts',
            '**/*.stories.ts',
            'src/app/app.config.ts',
            'src/app/components/shared/ai-input-bar/ai-input-bar.ts',
            'src/app/components/shared/barcode-scanner/barcode-scanner.ts',
            'src/app/components/shared/dashboard-summary-card/dashboard-summary-card.ts',
            'src/app/components/shared/nutrients-summary/nutrients-summary.ts',
            'src/app/constants/chart-colors.ts',
            'src/app/features/auth/components/auth/auth-lib/auth-countdown.utils.ts',
            'src/app/features/auth/components/auth/auth.ts',
            'src/app/features/auth/lib/google-identity.service.ts',
            'src/app/features/auth/pages/email-verification-pending/email-verification-pending.ts',
            'src/app/features/dashboard/lib/dashboard-layout.service.ts',
            'src/app/features/dashboard/lib/dashboard.facade.ts',
            'src/app/features/fasting/components/fasting-checkin-chart-dialog/fasting-checkin-chart-dialog.ts',
            'src/app/features/goals/pages/goals-page.ts',
            'src/app/features/premium/lib/paddle-checkout.service.ts',
            'src/app/features/public/components/landing-preview-tour/landing-preview-tour.ts',
            'src/app/features/statistics/lib/statistics-chart-config.ts',
            'src/app/services/auth.service.ts',
            'src/app/services/error-handler.service.ts',
            'src/app/services/frontend-observability.service.ts',
            'src/app/shared/notifications/push-notification.service.ts',
            'src/app/shared/platform/browser-storage.service.ts',
            'src/app/shared/platform/viewport.service.ts',
            'src/app/shared/theme/theme.service.ts',
            'src/app/shell/app.ts',
            'src/app/shell/sidebar/sidebar.ts',
        ],
        rules: {
            'local/no-browser-globals': 'error',
        },
    },
    {
        files: ['**/*.ts'],
        ignores: ['**/*.spec.ts', '**/*.stories.ts'],
        rules: {
            '@angular-eslint/component-class-suffix': 'error',
            '@angular-eslint/computed-must-return': 'error',
            '@angular-eslint/directive-class-suffix': 'error',
            '@angular-eslint/prefer-signals': 'error',
            '@angular-eslint/prefer-on-push-component-change-detection': 'error',
            '@angular-eslint/no-attribute-decorator': 'error',
            '@angular-eslint/no-duplicates-in-metadata-arrays': 'error',
            '@angular-eslint/no-lifecycle-call': 'error',
            '@angular-eslint/no-inputs-metadata-property': 'error',
            '@angular-eslint/no-pipe-impure': 'error',
            '@angular-eslint/no-outputs-metadata-property': 'error',
            '@angular-eslint/no-queries-metadata-property': 'error',
            '@angular-eslint/no-uncalled-signals': 'error',
            '@angular-eslint/use-injectable-provided-in': 'error',
            '@angular-eslint/no-async-lifecycle-method': 'error',
            '@angular-eslint/no-conflicting-lifecycle': 'error',
            '@angular-eslint/no-empty-lifecycle-method': 'error',
            '@angular-eslint/no-forward-ref': 'error',
            '@angular-eslint/no-implicit-take-until-destroyed': 'error',
            '@angular-eslint/no-input-rename': 'error',
            '@angular-eslint/no-output-native': 'error',
            '@angular-eslint/no-output-on-prefix': 'error',
            '@angular-eslint/no-output-rename': 'error',
            '@angular-eslint/prefer-inject': 'error',
            '@angular-eslint/prefer-output-emitter-ref': 'error',
            '@angular-eslint/prefer-output-readonly': 'error',
            '@angular-eslint/prefer-signal-model': 'error',
            '@angular-eslint/prefer-standalone': 'error',
            '@angular-eslint/relative-url-prefix': 'error',
            '@angular-eslint/use-component-selector': 'error',
            '@angular-eslint/use-component-view-encapsulation': 'error',
            'local/action-oriented-host-event-handlers': 'error',
            'local/prefer-protected-template-members': 'error',
            'no-restricted-syntax': [
                'error',
                ...noAnyCastSyntax,
                ...noRedundantBooleanComparisonSyntax,
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
                    selector: 'PropertyAssignment[key.name="standalone"][value.value=true]',
                    message: 'Remove standalone: true. Standalone is the Angular default.',
                },
                {
                    selector: 'PropertyAssignment[key.name="standalone"][value.value=false]',
                    message: 'Components, directives, and pipes must stay standalone. Remove standalone: false.',
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
        files: ['src/app/shared/{forms,i18n,notifications,platform,theme,ui}/**/*.ts'],
        ignores: ['src/app/shared/**/*.spec.ts'],
        rules: {
            'no-restricted-imports': [
                'error',
                {
                    patterns: [
                        {
                            group: ['../../features/**', '../../../features/**', '../../../../features/**', 'src/app/features/**'],
                            message: 'Shared common-theme code must stay feature-agnostic.',
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
                        {
                            group: [
                                'src/app/features/*',
                                '../features/*',
                                '../../features/*',
                                '../../../features/*',
                                '../../../../features/*',
                            ],
                            message:
                                'Import a concrete feature layer such as models, api, components, dialogs, lib, or pages instead of a feature root.',
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
            'src/app/shared/platform/viewport.service.ts',
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
                        {
                            group: [
                                'projects/fooddiary-admin/src/app/features/*',
                                '../features/*',
                                '../../features/*',
                                '../../../features/*',
                                '../../../../features/*',
                            ],
                            message:
                                'Import a concrete admin feature layer such as models, api, components, dialogs, lib, or pages instead of a feature root.',
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
            '@angular-eslint/template/label-has-associated-control': 'error',
            '@angular-eslint/template/no-autofocus': 'error',
            '@angular-eslint/template/eqeqeq': 'error',
            '@angular-eslint/template/banana-in-box': 'error',
            '@angular-eslint/template/button-has-type': 'error',
            '@angular-eslint/template/conditional-complexity': ['error', { maxComplexity: 3 }],
            '@angular-eslint/template/cyclomatic-complexity': ['error', { maxComplexity: 5 }],
            '@angular-eslint/template/mouse-events-have-key-events': 'error',
            '@angular-eslint/template/no-any': 'error',
            // Disabled after auditing all existing matches: Angular signal, input, model,
            // computed, and query reads are template calls by design, so this rule cannot
            // distinguish the preferred signal style from expensive method calls.
            '@angular-eslint/template/no-call-expression': 'off',
            '@angular-eslint/template/no-distracting-elements': 'error',
            '@angular-eslint/template/no-duplicate-attributes': 'error',
            '@angular-eslint/template/no-empty-control-flow': 'error',
            '@angular-eslint/template/no-inline-styles': [
                'error',
                {
                    allowBindToStyle: true,
                    allowNgStyle: false,
                },
            ],
            '@angular-eslint/template/no-interpolation-in-attributes': 'error',
            '@angular-eslint/template/no-negated-async': 'error',
            '@angular-eslint/template/no-non-null-assertion': 'error',
            '@angular-eslint/template/prefer-at-else': 'error',
            '@angular-eslint/template/prefer-at-empty': 'error',
            '@angular-eslint/template/prefer-built-in-pipes': 'error',
            '@angular-eslint/template/prefer-class-binding': 'error',
            '@angular-eslint/template/prefer-contextual-for-variables': 'error',
            '@angular-eslint/template/prefer-control-flow': 'error',
            '@angular-eslint/template/prefer-ngsrc': 'off',
            '@angular-eslint/template/prefer-self-closing-tags': 'error',
            '@angular-eslint/template/prefer-static-string-properties': 'error',
            '@angular-eslint/template/prefer-template-literal': 'error',
            '@angular-eslint/template/table-scope': 'error',
            '@angular-eslint/template/use-track-by-function': 'error',
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
            'local/action-oriented-event-handlers': 'error',
            'local/no-component-file-suffix': 'error',
            'local/no-mojibake': 'error',
            'local/no-label-wrapped-control': 'error',
        },
    },
];
