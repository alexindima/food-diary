/** @type {import('stylelint').Config} */
import designTokenValues from './stylelint-rules/design-token-values.js';
import disableCommentReason from './stylelint-rules/disable-comment-reason.js';
import noComponentFileSuffix from './stylelint-rules/no-component-file-suffix.js';
import noSassUseAsWildcard from './stylelint-rules/no-sass-use-as-wildcard.js';

const componentRawColorProperties =
    '/^(?:color|background|background-color|border|border-color|border-top|border-right|border-bottom|border-left|box-shadow|text-shadow|outline|outline-color|fill|stroke)$/';

const rawColorValues = [/#[\dA-Fa-f]{3,8}\b/, /\brgba?\(/, /\bhsla?\(/];
const fdVarFallbackValue = /var\(--fd-[^),]+,\s*/;

const restrictedValueRules = {
    '/^.*$/': [fdVarFallbackValue],
    animation: [/\ball\b/],
    transition: [/\ball\b/],
    'z-index': [/^[1-9]\d{2,}$/],
    [componentRawColorProperties]: rawColorValues,
};

export default {
    extends: ['stylelint-config-standard-scss'],
    ignoreFiles: ['dist/**/*.css', 'dist-admin/**/*.css', 'dist-storybook/**/*.css'],
    plugins: [designTokenValues, disableCommentReason, noComponentFileSuffix, noSassUseAsWildcard],
    rules: {
        'alpha-value-notation': null,
        'at-rule-no-unknown': null,
        'at-rule-empty-line-before': null,
        'block-no-empty': true,
        'color-function-alias-notation': null,
        'color-function-notation': null,
        'color-hex-length': null,
        'color-named': 'never',
        'comment-word-disallowed-list': [
            ['TODO', 'FIXME', 'HACK', 'temporary', 'quick fix'],
            {
                severity: 'error',
            },
        ],
        'custom-property-empty-line-before': null,
        'custom-media-pattern': '^fd-[a-z0-9]+(?:-[a-z0-9]+)*$',
        'declaration-block-no-duplicate-custom-properties': null,
        'declaration-block-no-duplicate-properties': [
            true,
            {
                ignore: ['consecutive-duplicates-with-different-values'],
            },
        ],
        'declaration-block-no-redundant-longhand-properties': null,
        'declaration-no-important': true,
        'declaration-property-value-disallowed-list': {
            ...restrictedValueRules,
            height: [/100vh/],
            position: [/fixed/],
            width: [/100vw/],
        },
        'declaration-property-value-keyword-no-deprecated': true,
        'font-family-no-duplicate-names': true,
        'font-family-no-missing-generic-family-keyword': true,
        'font-weight-notation': 'numeric',
        'function-url-quotes': 'always',
        'length-zero-no-unit': true,
        'max-nesting-depth': [
            4,
            {
                ignore: ['blockless-at-rules'],
            },
        ],
        'media-query-no-invalid': null,
        'media-feature-range-notation': 'prefix',
        'no-empty-source': null,
        'no-duplicate-selectors': true,
        'no-descending-specificity': null,
        'number-max-precision': 4,
        'nesting-selector-no-missing-scoping-root': null,
        'property-no-deprecated': true,
        'property-no-vendor-prefix': null,
        'property-disallowed-list': ['float'],
        'rule-empty-line-before': null,
        'rule-selector-property-disallowed-list': {
            '/^.*$/': ['font'],
        },
        'scss/at-import-partial-extension-disallowed-list': ['scss'],
        'scss/load-no-partial-leading-underscore': true,
        'scss/dollar-variable-pattern': '^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$',
        'scss/percent-placeholder-pattern': '^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$',
        'scss/selector-no-redundant-nesting-selector': true,
        'selector-class-pattern': [
            '^[a-z][a-z0-9]*(?:-[a-z0-9]+)*(?:(?:__|--)[a-z0-9]+(?:-[a-z0-9]+)*)*$',
            {
                resolveNestedSelectors: true,
            },
        ],
        'selector-max-attribute': 2,
        'selector-max-combinators': 3,
        'selector-max-compound-selectors': 5,
        'selector-max-id': 0,
        'selector-max-specificity': '0,3,0',
        'selector-max-type': [
            2,
            {
                ignore: ['child', 'compounded'],
            },
        ],
        'selector-max-universal': 0,
        'selector-disallowed-list': [/::ng-deep/],
        'selector-not-notation': null,
        'selector-pseudo-class-no-unknown': [
            true,
            {
                ignorePseudoClasses: ['deep'],
            },
        ],
        'keyframes-name-pattern': '^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$',
        'food-diary/design-token-values': true,
        'food-diary/disable-comment-reason': true,
        'food-diary/no-component-file-suffix': true,
        'food-diary/no-sass-use-as-wildcard': true,
        'shorthand-property-no-redundant-values': true,
        'value-keyword-case': null,
        'declaration-empty-line-before': ['never'],
    },
    overrides: [
        {
            files: ['src/styles/**/*.scss', 'projects/fd-ui-kit/src/lib/colors.scss'],
            rules: {
                'declaration-property-value-disallowed-list': {
                    '/^.*$/': [fdVarFallbackValue],
                    animation: restrictedValueRules.animation,
                    height: [/100vh/],
                    position: [/fixed/],
                    transition: restrictedValueRules.transition,
                    width: [/100vw/],
                },
                'food-diary/design-token-values': null,
                'selector-max-specificity': null,
                'selector-max-universal': null,
            },
        },
        {
            files: [
                'src/app/shell/**/*.scss',
                'src/app/components/shared/ai-input-bar/ai-input-bar.scss',
                'src/app/components/shared/image-upload-field/image-upload-field.scss',
                'src/app/features/dashboard/pages/_dashboard-shell.scss',
                'src/app/features/meals/components/quick-consumption-drawer/quick-consumption-drawer.scss',
                'projects/fooddiary-admin/src/app/features/admin-billing/pages/admin-billing.scss',
            ],
            rules: {
                'declaration-property-value-disallowed-list': {
                    ...restrictedValueRules,
                    height: [/100vh/],
                    width: [/100vw/],
                },
            },
        },
        {
            files: [
                'src/app/features/public/**/*.scss',
                'src/styles.scss',
                'projects/fooddiary-admin/src/styles.scss',
                'src/styles/design-tokens.scss',
                'src/styles/mixins.scss',
                'src/styles/utilities.scss',
                'src/styles/variables.scss',
                'projects/fd-tour/src/lib/**/*.scss',
                'projects/fd-ui-kit/src/lib/**/*.scss',
            ],
            rules: {
                'declaration-property-value-disallowed-list': restrictedValueRules,
                'max-nesting-depth': [
                    4,
                    {
                        ignore: ['blockless-at-rules'],
                    },
                ],
                'selector-max-type': null,
                'selector-max-universal': null,
            },
        },
    ],
};
