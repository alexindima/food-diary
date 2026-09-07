import { ESLint } from 'eslint';

// Separate bundled Angular compiler instances can make instanceof-based rules
// silently ignore parser nodes. Exercise real rules before trusting a green lint.
const eslint = new ESLint();
const fixture = '<div>{{ $any(value) }}</div>' + '@if (visible) { <span>Visible</span> }'.repeat(6);
const results = await eslint.lintText(fixture, { filePath: 'src/app/template-lint-guardrail.html' });
const reportedRules = new Set(results.flatMap(result => result.messages.map(message => message.ruleId)));
for (const rule of ['@angular-eslint/template/no-any', '@angular-eslint/template/cyclomatic-complexity']) {
    if (!reportedRules.has(rule)) {
        throw new Error(`Template lint guardrail did not report ${rule}. Check the Angular ESLint compiler dependency tree.`);
    }
}
console.log('Angular template lint guardrails are active.');
