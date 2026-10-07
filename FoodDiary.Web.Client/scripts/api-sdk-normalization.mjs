import ts from 'typescript';

/** CSV is binary to the download API even though its media type starts with text. */
export function normalizeBinaryResponseTypes(content, fileName, binaryOperations) {
    const source = ts.createSourceFile(fileName, content, ts.ScriptTarget.Latest, true);
    const edits = [];
    const visit = node => {
        if (ts.isMethodDeclaration(node) && node.body && binaryOperations.has(node.name.getText(source))) {
            const alreadyBinary = node.body.statements.some(statement => {
                if (!ts.isReturnStatement(statement) || !statement.expression || !ts.isCallExpression(statement.expression)) return false;
                const options = statement.expression.arguments[2];
                return (
                    options &&
                    ts.isObjectLiteralExpression(options) &&
                    options.properties.some(
                        property =>
                            ts.isPropertyAssignment(property) &&
                            property.name.getText(source) === 'responseType' &&
                            ts.isStringLiteral(property.initializer) &&
                            property.initializer.text === 'blob',
                    )
                );
            });
            if (alreadyBinary) return;
            const variable = node.body.statements.find(
                statement =>
                    ts.isVariableStatement(statement) &&
                    statement.declarationList.declarations.some(declaration => declaration.name.getText(source) === 'responseType_'),
            );
            const selection = node.body.statements.find(
                statement =>
                    ts.isIfStatement(statement) &&
                    statement.expression.getText(source) === 'localVarHttpHeaderAcceptSelected' &&
                    statement.getText(source).includes('responseType_'),
            );
            if (!variable || !selection) throw new Error(`Unsupported binary SDK template: ${node.name.getText(source)}.`);
            edits.push({ start: variable.getStart(source), end: variable.end, value: "const responseType_ = 'blob' as const;" });
            edits.push({ start: selection.getStart(source), end: selection.end, value: '' });
        }
        ts.forEachChild(node, visit);
    };
    visit(source);
    for (const edit of edits.sort((a, b) => b.start - a.start))
        content = content.slice(0, edit.start) + edit.value + content.slice(edit.end);
    return content;
}
