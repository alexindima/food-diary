/* eslint-disable @typescript-eslint/explicit-function-return-type -- Stylelint plugins are plain JavaScript functions. */
import stylelint from 'stylelint';

const ruleName = 'food-diary/disable-comment-reason';

const messages = stylelint.utils.ruleMessages(ruleName, {
    rejected: 'Add a reason to stylelint disable comments with `-- <reason>`.',
});

const disableCommentPattern = /\bstylelint-disable(?:-line|-next-line)?\b/;
const reasonPattern = /--\s*\S+/;

const ruleFunction = primaryOption => {
    return (root, result) => {
        if (primaryOption !== true) {
            return;
        }

        root.walkComments(comment => {
            if (!disableCommentPattern.test(comment.text) || reasonPattern.test(comment.text)) {
                return;
            }

            stylelint.utils.report({
                ruleName,
                result,
                node: comment,
                message: messages.rejected,
            });
        });
    };
};

ruleFunction.ruleName = ruleName;
ruleFunction.messages = messages;

export default stylelint.createPlugin(ruleName, ruleFunction);
export { messages, ruleName };
