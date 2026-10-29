/* eslint-disable @typescript-eslint/explicit-function-return-type -- Stylelint plugins operate on PostCSS nodes. */
import stylelint from 'stylelint';

const ruleName = 'food-diary/reduced-motion';
const messages = stylelint.utils.ruleMessages(ruleName, {
    rejected: property => `Enable ${property} inside @media (prefers-reduced-motion: no-preference); keep the default state static.`,
});
const nonMovingProperties = new Set([
    'color',
    'background',
    'background-color',
    'border-color',
    'border-top-color',
    'border-right-color',
    'border-bottom-color',
    'border-left-color',
    'outline-color',
    'text-decoration-color',
    'box-shadow',
    'text-shadow',
    'opacity',
    'fill',
    'stroke',
    'caret-color',
    'accent-color',
    'visibility',
]);
const timingWord =
    /^(?:-?\d*\.?\d+(?:ms|s)|ease(?:-in)?(?:-out)?|linear|step-start|step-end|normal|allow-discrete|(?:cubic-bezier|steps|linear)\()/u;

// Commas and whitespace inside functions do not separate shorthand entries.
function splitValue(value, separator) {
    const parts = [];
    let depth = 0;
    let start = 0;
    for (let index = 0; index < value.length; index++) {
        if (value[index] === '(') {
            depth++;
        }
        if (value[index] === ')') {
            depth--;
        }
        if (depth === 0 && separator.test(value[index])) {
            if (index > start) {
                parts.push(value.slice(start, index));
            }
            start = index + 1;
        }
    }
    if (start < value.length) {
        parts.push(value.slice(start));
    }
    return parts.map(part => part.trim()).filter(Boolean);
}

export function permitsMotion(node) {
    for (let parent = node.parent; parent; parent = parent.parent) {
        if (parent.type !== 'atrule' || parent.name.toLowerCase() !== 'media') {
            continue;
        }
        const queries = splitValue(parent.params.toLowerCase(), /,/u);
        if (
            queries.length > 0 &&
            queries.every(
                query =>
                    /^not\s*\(\s*prefers-reduced-motion\s*:\s*reduce\s*\)$/u.test(query) ||
                    (!/\b(?:not|or)\b/u.test(query) && /\(\s*prefers-reduced-motion\s*:\s*no-preference\s*\)/u.test(query)),
            )
        ) {
            return true;
        }
    }
    return false;
}

function instant(value) {
    const duration = /(?:^|\s)(\d*\.?\d+)(ms|s)(?=\s|$)/u.exec(value);
    return duration !== null && Number(duration[1]) === 0;
}

function movingTransition(value) {
    return splitValue(value, /,/u).some(entry => {
        if (instant(entry)) {
            return false;
        }
        const properties = splitValue(entry, /\s/u).filter(word => !timingWord.test(word));
        return properties.length === 0 || properties.some(property => !nonMovingProperties.has(property));
    });
}

function movingDuration(node, value) {
    const properties = node.parent.nodes.find(sibling => sibling.type === 'decl' && sibling.prop === 'transition-property');
    return !properties || requiresMotionPreference(properties) ? splitValue(value, /,/u).some(entry => !instant(entry)) : false;
}

export function requiresMotionPreference(node) {
    const property = node.prop.toLowerCase();
    const value = node.value.toLowerCase();
    if (/^(?:none|initial|unset)$/u.test(value)) {
        return false;
    }
    if (property === 'scroll-behavior') {
        return value === 'smooth' || /\bvar\(/u.test(value);
    }
    if (['animation', 'animation-name'].includes(property)) {
        return splitValue(value, /,/u).some(entry => entry !== 'none' && !instant(entry));
    }
    if (property === 'animation-duration') {
        return splitValue(value, /,/u).some(entry => !instant(entry));
    }
    if (property === 'transition') {
        return movingTransition(value);
    }
    if (property === 'transition-property') {
        return splitValue(value, /,/u).some(entry => !nonMovingProperties.has(entry));
    }
    if (property === 'transition-duration') {
        return movingDuration(node, value);
    }
    return false;
}

const ruleFunction = primary => (root, result) => {
    if (!stylelint.utils.validateOptions(result, ruleName, { actual: primary, possible: [true] })) {
        return;
    }
    root.walkDecls(node => {
        if (requiresMotionPreference(node) && !permitsMotion(node)) {
            stylelint.utils.report({ ruleName, result, node, message: messages.rejected(node.prop) });
        }
    });
};

ruleFunction.ruleName = ruleName;
ruleFunction.messages = messages;
export default stylelint.createPlugin(ruleName, ruleFunction);
