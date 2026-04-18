/** @type {import('stylelint').Config} */
export default {
    customSyntax: 'postcss-scss',
    ignoreFiles: ['dist/**/*.css', 'dist-admin/**/*.css', 'dist-storybook/**/*.css'],
    rules: {
        'block-no-empty': true,
        'color-named': 'never',
        'no-duplicate-selectors': true,
        'no-descending-specificity': null,
        'selector-disallowed-list': [/::ng-deep/],
        'rule-empty-line-before': ['always-multi-line'],
        'declaration-empty-line-before': ['never'],
    },
};
