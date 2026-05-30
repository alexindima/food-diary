/* eslint-disable @typescript-eslint/explicit-function-return-type -- Stylelint plugins are plain JavaScript functions. */
import stylelint from 'stylelint';

const ruleName = 'food-diary/no-component-file-suffix';
const componentFileSuffixPattern = /\.component\.(?:css|scss)$/;

const messages = stylelint.utils.ruleMessages(ruleName, {
    rejected: 'Use one component base file name without `.component`, for example `user-profile.scss`.',
});

const ruleFunction = primaryOption => {
    return (root, result) => {
        if (primaryOption !== true) {
            return;
        }

        const fileName = root.source?.input.file?.replaceAll('\\', '/') ?? '';

        if (!componentFileSuffixPattern.test(fileName)) {
            return;
        }

        stylelint.utils.report({
            ruleName,
            result,
            node: root,
            message: messages.rejected,
        });
    };
};

ruleFunction.ruleName = ruleName;
ruleFunction.messages = messages;

export default stylelint.createPlugin(ruleName, ruleFunction);
export { messages, ruleName };
