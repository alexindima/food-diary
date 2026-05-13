/* eslint-disable @typescript-eslint/explicit-function-return-type -- Stylelint plugins are plain JavaScript functions. */
import stylelint from 'stylelint';

const ruleName = 'food-diary/no-sass-use-as-wildcard';
const messages = stylelint.utils.ruleMessages(ruleName, {
    rejected: 'Avoid Sass wildcard namespaces. Use an explicit namespace instead of `as *`.',
});

const ruleFunction = primaryOption => {
    return (root, result) => {
        if (primaryOption !== true) {
            return;
        }

        root.walkAtRules('use', atRule => {
            if (/\bas\s+\*\s*$/.test(atRule.params)) {
                stylelint.utils.report({
                    ruleName,
                    result,
                    node: atRule,
                    message: messages.rejected,
                });
            }
        });
    };
};

ruleFunction.ruleName = ruleName;
ruleFunction.messages = messages;

export default stylelint.createPlugin(ruleName, ruleFunction);
export { messages, ruleName };
