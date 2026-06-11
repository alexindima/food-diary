/** @type {import('stylelint').Config} */
import designTokenValues from './stylelint-rules/design-token-values.js';
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
    extends: ['stylelint-config-standard'],
    customSyntax: 'postcss-scss',
    ignoreFiles: ['dist/**/*.css', 'dist-admin/**/*.css', 'dist-storybook/**/*.css'],
    plugins: [designTokenValues, noComponentFileSuffix, noSassUseAsWildcard],
    rules: {
        'alpha-value-notation': null,
        'at-rule-no-unknown': null,
        'at-rule-empty-line-before': null,
        'block-no-empty': true,
        'color-function-alias-notation': null,
        'color-function-notation': null,
        'color-hex-length': null,
        'color-named': 'never',
        'custom-property-empty-line-before': null,
        'declaration-block-no-duplicate-custom-properties': null,
        'declaration-block-no-duplicate-properties': [
            true,
            {
                ignore: ['consecutive-duplicates-with-different-values'],
            },
        ],
        'declaration-block-no-redundant-longhand-properties': null,
        'declaration-no-important': true,
        'declaration-property-value-disallowed-list': restrictedValueRules,
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
        'selector-class-pattern': [
            '^[a-z][a-z0-9]*(?:-[a-z0-9]+)*(?:(?:__|--)[a-z0-9]+(?:-[a-z0-9]+)*)*$',
            {
                resolveNestedSelectors: true,
            },
        ],
        'selector-max-compound-selectors': 5,
        'selector-max-id': 0,
        'selector-max-type': [
            2,
            {
                ignore: ['child', 'compounded'],
            },
        ],
        'selector-disallowed-list': [/::ng-deep/],
        'selector-not-notation': null,
        'selector-pseudo-class-no-unknown': [
            true,
            {
                ignorePseudoClasses: ['deep'],
            },
        ],
        'keyframes-name-pattern': null,
        'food-diary/design-token-values': true,
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
                    transition: restrictedValueRules.transition,
                },
                'food-diary/design-token-values': null,
            },
        },
        {
            files: [
                'src/app/features/public/**/*.scss',
                'src/styles/design-tokens.scss',
                'src/styles/mixins.scss',
                'src/styles/utilities.scss',
                'src/styles/variables.scss',
                'projects/fd-ui-kit/src/lib/**/*.scss',
            ],
            rules: {
                'max-nesting-depth': [
                    4,
                    {
                        ignore: ['blockless-at-rules'],
                    },
                ],
                'selector-max-type': null,
            },
        },
    ],
};
