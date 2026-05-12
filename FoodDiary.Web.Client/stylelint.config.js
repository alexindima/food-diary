/** @type {import('stylelint').Config} */
const componentRawColorProperties =
    '/^(?:color|background|background-color|border|border-color|border-top|border-right|border-bottom|border-left|box-shadow|text-shadow|outline|outline-color|fill|stroke)$/';

const rawColorValues = [/#[0-9a-fA-F]{3,8}\b/, /\brgba?\(/, /\bhsla?\(/];
const fdVarFallbackValue = /var\(--fd-[^,)]+,\s*/;

const restrictedValueRules = {
    '/^.*$/': [fdVarFallbackValue],
    animation: [/\ball\b/],
    transition: [/\ball\b/],
    'z-index': [/^[1-9]\d{2,}$/],
    [componentRawColorProperties]: rawColorValues,
};

export default {
    customSyntax: 'postcss-scss',
    ignoreFiles: ['dist/**/*.css', 'dist-admin/**/*.css', 'dist-storybook/**/*.css'],
    rules: {
        'block-no-empty': true,
        'color-named': 'never',
        'declaration-block-no-duplicate-properties': [
            true,
            {
                ignore: ['consecutive-duplicates-with-different-values'],
            },
        ],
        'declaration-no-important': true,
        'declaration-property-value-disallowed-list': restrictedValueRules,
        'font-family-no-duplicate-names': true,
        'font-weight-notation': 'numeric',
        'function-url-quotes': 'always',
        'length-zero-no-unit': true,
        'max-nesting-depth': [
            4,
            {
                ignore: ['blockless-at-rules'],
            },
        ],
        'no-duplicate-selectors': true,
        'no-descending-specificity': null,
        'property-disallowed-list': ['float'],
        'selector-class-pattern': [
            '^[a-z][a-z0-9]*(?:-[a-z0-9]+)*(?:(?:__|--)[a-z0-9]+(?:-[a-z0-9]+)*)*$',
            {
                resolveNestedSelectors: true,
            },
        ],
        'selector-max-compound-selectors': 5,
        'selector-max-id': 0,
        'selector-disallowed-list': [/::ng-deep/],
        'shorthand-property-no-redundant-values': true,
        'rule-empty-line-before': ['always-multi-line'],
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
            },
        },
    ],
};
