/**
 * @see https://prettier.io/docs/en/configuration.html
 * @type {import("prettier").Config}
 */
const config = {
    printWidth: 140,
    singleQuote: true,
    trailingComma: 'all',
    tabWidth: 4,
    semi: true,
    bracketSpacing: true,
    arrowParens: 'avoid',
    overrides: [
        {
            files: '*.html',
            options: {
                parser: 'angular',
            },
        },
    ],
};

export default config;
