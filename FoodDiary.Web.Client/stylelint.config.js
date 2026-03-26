/** @type {import('stylelint').Config} */
export default {
    customSyntax: 'postcss-scss',
    ignoreFiles: ['dist/**/*.css', 'dist-admin/**/*.css'],
    rules: {
        'color-named': 'never',
        'no-duplicate-selectors': true,
        'no-descending-specificity': null,
        'rule-empty-line-before': ['always-multi-line'],
        'declaration-empty-line-before': ['never'],
    },
};
