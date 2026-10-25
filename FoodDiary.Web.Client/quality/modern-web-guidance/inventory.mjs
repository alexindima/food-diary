import { createRequire } from 'node:module';
const require = createRequire(new URL('../../package.json', import.meta.url));
const ts = require('typescript');

// Keep strings: platform names in computed properties, templates and feature detection are relevant too.
export function searchableSource(text, file) {
    if (/\.[cm]?[jt]s$/u.test(file)) {
        const scanner = ts.createScanner(ts.ScriptTarget.Latest, false, ts.LanguageVariant.Standard, text);
        let output = '',
            token;
        while ((token = scanner.scan()) !== ts.SyntaxKind.EndOfFileToken)
            output += [ts.SyntaxKind.SingleLineCommentTrivia, ts.SyntaxKind.MultiLineCommentTrivia].includes(token)
                ? scanner.getTokenText().replace(/[^\r\n]/gu, ' ')
                : scanner.getTokenText();
        return output;
    }
    return text.replace(/<!--[\s\S]*?-->|\/\*[\s\S]*?\*\//gu, match => match.replace(/[^\r\n]/gu, ' '));
}

export function inspectApplicability(records, reviews) {
    const findings = [];
    for (const review of reviews.filter(item => item.whenAbsent)) {
        for (const record of records)
            for (const pattern of review.whenAbsent.patterns) {
                const matcher = new RegExp(pattern, 'gmu');
                for (const match of record.searchable.matchAll(matcher))
                    findings.push({
                        file: record.file,
                        line: record.searchable.slice(0, match.index).split('\n').length,
                        message: `${review.guide}: ${review.whenAbsent.reason} Found ${JSON.stringify(match[0])}. Review this guide and replace its absence decision with applicable assertions.`,
                    });
            }
    }
    return findings;
}

export function inspectScript(text, file) {
    const source = ts.createSourceFile(file, text, ts.ScriptTarget.Latest, true),
        findings = [];
    if (source.parseDiagnostics.length) throw new Error(`Cannot inspect invalid TypeScript: ${file}`);
    const fail = (node, check, message) =>
        findings.push({ check, file, line: source.getLineAndCharacterOfPosition(node.getStart(source)).line + 1, message });
    const property = expression =>
        ts.isPropertyAccessExpression(expression)
            ? expression.name.text
            : ts.isElementAccessExpression(expression) && expression.argumentExpression && ts.isStringLiteral(expression.argumentExpression)
              ? expression.argumentExpression.text
              : undefined;
    const literalEmpty = node => node && (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) && node.text === '';
    const visit = node => {
        if (
            ts.isBinaryExpression(node) &&
            node.operatorToken.kind === ts.SyntaxKind.EqualsToken &&
            ['innerHTML', 'outerHTML'].includes(property(node.left)) &&
            !literalEmpty(node.right)
        )
            fail(node, 'script:dom-sinks', 'Use textContent, DOM nodes or a sanitizing HTML API instead of raw HTML assignment.');
        if (ts.isCallExpression(node)) {
            const name = property(node.expression) ?? (ts.isIdentifier(node.expression) ? node.expression.text : '');
            if (
                ['insertAdjacentHTML', 'bypassSecurityTrustHtml'].includes(name) ||
                (['write', 'writeln'].includes(name) && /(?:^|\.)document\.(?:write|writeln)$/u.test(node.expression.getText(source)))
            )
                fail(
                    node,
                    'script:dom-sinks',
                    'This HTML injection sink requires a sanitizing boundary; do not bypass Angular sanitization.',
                );
            if (name === 'setProperty' && node.arguments[1]?.text === 'innerHTML' && !literalEmpty(node.arguments[2]))
                fail(node, 'script:dom-sinks', 'Renderer innerHTML assignments require sanitized content.');
            if (name === 'eval' || (['setTimeout', 'setInterval'].includes(name) && ts.isStringLiteral(node.arguments[0] ?? source)))
                fail(node, 'script:dynamic-code', 'Do not execute strings as JavaScript.');
            if (name === 'postMessage' && node.arguments[1]?.text === '*')
                fail(node, 'script:message-origin', 'Use an explicit trusted target origin for cross-window messages.');
            if (name === 'setItem') {
                const receiver = ts.isPropertyAccessExpression(node.expression) ? node.expression.expression.getText(source) : '';
                const persistent = /\blocalStorage(?:Ref)?\b|^local$/u.test(receiver);
                const wrappedLocal = node.arguments[0]?.text === 'local';
                const key = node.arguments[wrappedLocal ? 1 : 0]?.text;
                if ((persistent || wrappedLocal) && /^(?:authToken|accessToken|refreshToken|password)$/iu.test(key ?? ''))
                    fail(
                        node,
                        'script:credential-storage',
                        'Keep bearer credentials out of localStorage; use the owned tab session and HttpOnly refresh-cookie flow.',
                    );
            }
        }
        if (ts.isNewExpression(node) && node.expression.getText(source) === 'Function')
            fail(node, 'script:dynamic-code', 'Do not compile untrusted JavaScript strings with Function.');
        ts.forEachChild(node, visit);
    };
    visit(source);
    return findings;
}
