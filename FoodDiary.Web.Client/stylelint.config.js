/** @type {import('stylelint').Config} */
export default {
    rules: {
        'color-named': 'never',
        'no-duplicate-selectors': true,
        'no-descending-specificity': true,
        'rule-empty-line-before': ['always-multi-line'],
        'declaration-empty-line-before': ['never'],
    },
};
