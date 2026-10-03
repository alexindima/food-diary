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

const controlFixture = `import { Component, model } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';
@Component({ template: '' })
class GuardrailControl implements FormValueControl<string | null> {
    public readonly value = model<string | null>(null);
    public reset(): void {}
}
@Component({ template: '' })
class GuardrailComponent {
    public reset(): void {}
}`;
const controlResults = await eslint.lintText(controlFixture, {
    filePath: 'projects/fd-ui-kit/src/lib/date-input/fd-ui-date-input.ts',
});
const publicResetErrors = controlResults.flatMap(result =>
    result.messages.filter(message => message.ruleId === 'local/prefer-protected-template-members'),
);
const ordinaryResetLine = controlFixture.slice(0, controlFixture.lastIndexOf('public reset')).split('\n').length;
if (publicResetErrors.length !== 1 || publicResetErrors[0].line !== ordinaryResetLine) {
    throw new Error('Signal Forms reset hooks must remain public while ordinary component internals stay protected.');
}
console.log('Signal Forms reset hook lint guardrails are active.');
