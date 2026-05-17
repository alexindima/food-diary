/* eslint-disable @typescript-eslint/explicit-function-return-type -- Stylelint plugins are plain JavaScript functions. */
/* eslint-disable security/detect-unsafe-regex -- CSS declaration values are short Stylelint inputs, not unbounded user payloads. */
import stylelint from 'stylelint';

const ruleName = 'food-diary/design-token-values';

const messages = stylelint.utils.ruleMessages(ruleName, {
    spacing: property => `Use --fd-space-* tokens for non-zero ${property} values.`,
    radius: 'Use --fd-radius-* tokens for border-radius values.',
    repeatedSizing: property => `Use an existing --fd-size-* token or local custom property for repeated ${property} values.`,
    shadow: 'Use --fd-shadow-* or effect tokens for box-shadow values.',
    typography: property => `Use --fd-text-* or --fd-font-* tokens for ${property} values.`,
    transitionTime: property => `Use --fd-duration-* or --fd-transition-* tokens for ${property} values.`,
});

const zeroValuePattern = /(?:^|\s|[(,])-?0(?:\.0+)?(?:[a-z%]+)?(?=$|\s|[),])/i;
const lengthPattern = /(?:^|[\s(,])(?:0?\.\d+|[1-9]\d*(?:\.\d+)?)(?:px|r?em|vh|vw|vmin|vmax|ch|ex|%)\b/i;
const transitionTimePattern = /(?:^|[\s(,])(?:0?\.\d+|[1-9]\d*(?:\.\d+)?)(?:ms|s)\b/i;
const numericPattern = /(?:^|[\s(,])(?:0?\.\d+|[1-9]\d*(?:\.\d+)?)\b/i;
const hardcodedShadowPattern = /(?:^|[\s(,])-?(?:0?\.\d+|[1-9]\d*(?:\.\d+)?)(?:px|r?em)\b/i;

const spacingProperties = new Set([
    'gap',
    'row-gap',
    'column-gap',
    'margin',
    'margin-block',
    'margin-block-end',
    'margin-block-start',
    'margin-bottom',
    'margin-inline',
    'margin-inline-end',
    'margin-inline-start',
    'margin-left',
    'margin-right',
    'margin-top',
    'padding',
    'padding-block',
    'padding-block-end',
    'padding-block-start',
    'padding-bottom',
    'padding-inline',
    'padding-inline-end',
    'padding-inline-start',
    'padding-left',
    'padding-right',
    'padding-top',
]);

const repeatedSizingProperties = new Set(['height', 'min-height', 'min-width', 'width']);
const repeatedRawSizeValues = new Set([
    '18px',
    '22px',
    '24px',
    '28px',
    '32px',
    '34px',
    '36px',
    '38px',
    '40px',
    '42px',
    '44px',
    '48px',
    '56px',
    '64px',
]);

const isTokenValue = (value, tokenPrefixes) => tokenPrefixes.some(prefix => value.includes(`var(${prefix}`));

const hasNonZeroLength = value => lengthPattern.test(value) && !zeroValuePattern.test(value.trim());

const hasHardcodedCalc = value => /\b(?:calc|clamp|min|max)\(/.test(value) && lengthPattern.test(value);

const isCustomProperty = property => property.startsWith('--');

const isSassVariable = property => property.startsWith('$');

const report = ({ result, declaration, message }) => {
    stylelint.utils.report({
        ruleName,
        result,
        node: declaration,
        message,
    });
};

const checkSpacing = (property, value) => {
    if (!spacingProperties.has(property)) {
        return null;
    }

    if (hasNonZeroLength(value) && !isTokenValue(value, ['--fd-space-'])) {
        return messages.spacing(property);
    }

    if (hasHardcodedCalc(value) && !isTokenValue(value, ['--fd-space-'])) {
        return messages.spacing(property);
    }

    return null;
};

const checkRadius = (property, value) => {
    if (property !== 'border-radius') {
        return null;
    }

    return hasNonZeroLength(value) && !isTokenValue(value, ['--fd-radius-']) ? messages.radius : null;
};

const checkRepeatedSizing = (property, value) => {
    if (!repeatedSizingProperties.has(property)) {
        return null;
    }

    return repeatedRawSizeValues.has(value.trim().toLowerCase()) ? messages.repeatedSizing(property) : null;
};

const checkShadow = (property, value) => {
    if (property !== 'box-shadow') {
        return null;
    }

    return hardcodedShadowPattern.test(value) && !isTokenValue(value, ['--fd-shadow-', '--fd-effect-']) ? messages.shadow : null;
};

const checkTypography = (property, value) => {
    if (property === 'font-size' && hasNonZeroLength(value) && !isTokenValue(value, ['--fd-text-', '--fd-size-'])) {
        return messages.typography(property);
    }

    if (property === 'font-weight' && numericPattern.test(value) && !isTokenValue(value, ['--fd-text-', '--fd-font-'])) {
        return messages.typography(property);
    }

    return null;
};

const checkTransitionTime = (property, value) => {
    if (property !== 'transition-delay' && property !== 'transition-duration') {
        return null;
    }

    return transitionTimePattern.test(value) && !isTokenValue(value, ['--fd-duration-', '--fd-transition-'])
        ? messages.transitionTime(property)
        : null;
};

const getDeclarationMessage = (property, value) =>
    checkSpacing(property, value) ??
    checkRadius(property, value) ??
    checkRepeatedSizing(property, value) ??
    checkShadow(property, value) ??
    checkTypography(property, value) ??
    checkTransitionTime(property, value);

const ruleFunction = primaryOption => {
    return (root, result) => {
        if (primaryOption !== true) {
            return;
        }

        root.walkDecls(declaration => {
            const property = declaration.prop.toLowerCase();
            const value = declaration.value;

            if (isCustomProperty(property) || isSassVariable(property)) {
                return;
            }

            const message = getDeclarationMessage(property, value);
            if (message) {
                report({ result, declaration, message });
            }
        });
    };
};

ruleFunction.ruleName = ruleName;
ruleFunction.messages = messages;

export default stylelint.createPlugin(ruleName, ruleFunction);
export { messages, ruleName };
