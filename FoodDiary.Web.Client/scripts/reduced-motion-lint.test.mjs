import assert from 'node:assert/strict';
import { test } from 'node:test';

import { ESLint, Linter } from 'eslint';
import stylelint from 'stylelint';

import scrollRule from '../eslint-rules/no-unconditional-smooth-scroll.js';
import motionRule from '../stylelint-rules/reduced-motion.js';

async function lintStyles(code) {
    const result = await stylelint.lint({
        code,
        customSyntax: 'postcss-scss',
        config: {
            plugins: [motionRule],
            rules: { 'food-diary/reduced-motion': true },
        },
    });
    return result.results[0].warnings;
}

for (const source of [
    '.card { animation: enter 200ms ease; }',
    '.card { animation-name: enter; animation-duration: 200ms; }',
    '.card { transition: transform 200ms, color 100ms; }',
    '.chart { transition: d 180ms; }',
    '.chart { transition-property: height, y; transition-duration: 180ms; }',
    '.card { transition: var(--fd-button-transition); }',
    '.scroll { scroll-behavior: smooth; }',
    '@media (prefers-reduced-motion: reduce) { .card { animation: enter 1s; } }',
    '@media (prefers-reduced-motion: no-preference), screen { .card { animation: enter 1s; } }',
    '@media not screen and (prefers-reduced-motion: no-preference) { .card { animation: enter 1s; } }',
    '.card { transition: transform 200ms; @media (prefers-reduced-motion: reduce) { .other { transition: none; } } }',
]) {
    test(`Stylelint rejects unguarded movement: ${source}`, async () => assert.ok((await lintStyles(source)).length > 0));
}

for (const source of [
    '.card { animation: none; transition: none; scroll-behavior: auto; }',
    '.card { transition: color 100ms, background-color 100ms, opacity 100ms; }',
    '.card { transition: transform 0s; animation: enter 0s; }',
    '@keyframes enter { from { transform: translateY(8px); } to { transform: none; } }',
    '@media (prefers-reduced-motion: no-preference) { .card { animation: enter 200ms; } }',
    '.card { @media (prefers-reduced-motion: no-preference) { transition: transform 200ms; } }',
    '@media screen and (prefers-reduced-motion: no-preference) and (hover: hover) { .card { transition: transform 200ms; } }',
    '@media not (prefers-reduced-motion: reduce) { .card { animation: enter 200ms; } }',
    '.input { /* stylelint-disable food-diary/reduced-motion -- Nonvisual autofill event. */ animation-name: autofill; animation-duration: 10ms; /* stylelint-enable food-diary/reduced-motion */ }',
]) {
    test(`Stylelint allows static defaults and explicit opt-in: ${source}`, async () => assert.deepEqual(await lintStyles(source), []));
}

function lintScroll(code) {
    return new Linter().verify(code, [{ plugins: { local: { rules: { motion: scrollRule } } }, rules: { 'local/motion': 'error' } }]);
}

for (const source of [
    "element.scrollIntoView({ behavior: 'smooth' });",
    "element['scrollTo']({ behavior: `smooth` });",
    "const options = { behavior: 'smooth' }; element.scrollIntoView(options);",
    "const options = { behavior: 'smooth' }; element.scrollTo({ ...options, top: 0 });",
    "const behavior = 'smooth'; element.scrollBy({ behavior });",
    "const reducedMotion = false; element.scrollIntoView({ behavior: reducedMotion ? 'instant' : 'smooth' });",
    "const reducedMotion = viewport.matchMedia('(prefers-color-scheme: dark)').matches; element.scrollTo({ behavior: reducedMotion ? 'instant' : 'smooth' });",
    "const reduce = viewport.matchMedia('(prefers-reduced-motion: reduce)').matches; element.scrollTo({ behavior: reduce ? 'smooth' : 'instant' });",
    "const reduce = viewport.matchMedia('(prefers-reduced-motion: reduce)').matches; element.scrollTo({ behavior: reduce ? 'smooth' : 'smooth' });",
    "options.behavior = 'smooth';",
    "let behavior = 'instant'; behavior = 'smooth'; element.scrollTo({ behavior });",
    "let options = { behavior: 'smooth' }; element.scrollTo(options);",
    "let reduced = viewport.matchMedia('(prefers-reduced-motion: reduce)').matches; reduced = false; element.scrollTo({ behavior: reduced ? 'instant' : 'smooth' });",
    "const reduced = viewport.matchMedia('(prefers-reduced-motion: reduce)').matches; element.scrollTo({ behavior: reduced ? 'auto' : 'smooth' });",
    "const reduced = viewport.matchMedia('(prefers-reduced-motion: reduce)').matches; element.scrollTo({ behavior: reduced ? getBehavior() : 'smooth' });",
]) {
    test(`ESLint rejects unsafe scrolling: ${source}`, () => assert.ok(lintScroll(source).length > 0));
}

for (const source of [
    "element.scrollIntoView({ behavior: 'instant' });",
    'element.scrollTo({ top: 0 });',
    "const reduced = viewport.matchMedia('(prefers-reduced-motion: reduce)').matches; const behavior = reduced ? 'instant' : 'smooth'; element.scrollTo({ behavior });",
    "const reduced = viewport.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches === true; element.scrollIntoView({ behavior: reduced ? 'instant' : 'smooth' });",
    "const reduced = viewport?.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false; element.scrollTo({ behavior: reduced ? 'instant' : 'smooth' });",
    "const query = viewport.matchMedia('(prefers-reduced-motion: reduce)'); element.scrollIntoView({ behavior: query.matches ? 'instant' : 'smooth' });",
    "const normal = viewport.matchMedia('(prefers-reduced-motion: no-preference)').matches; element.scrollIntoView({ behavior: normal ? 'smooth' : 'instant' });",
    "const reduced = viewport.matchMedia('(prefers-reduced-motion: reduce)').matches; element.scrollIntoView({ behavior: !reduced ? 'smooth' : 'instant' });",
    "const title = 'smooth'; display(title);",
    'for (const build of builders) { build(); }',
]) {
    test(`ESLint allows preference-aware scrolling: ${source}`, () => assert.deepEqual(lintScroll(source), []));
}

test('workspace enables both guards for the client, admin and shared UI', async () => {
    const eslint = new ESLint();
    for (const filePath of [
        'src/app/shell/app.ts',
        'projects/fooddiary-admin/src/app/app.ts',
        'projects/fd-ui-kit/src/lib/input/fd-ui-input.ts',
        'projects/fd-tour/src/lib/fd-tour.service.ts',
    ]) {
        const config = await eslint.calculateConfigForFile(filePath);
        assert.equal(config.rules['local/no-unconditional-smooth-scroll'][0], 2, filePath);
    }
    const result = await stylelint.lint({
        code: '.motion-contract { animation: enter 200ms; }',
        codeFilename: 'src/styles/motion-contract.scss',
    });
    assert.ok(result.results[0].warnings.some(warning => warning.rule === 'food-diary/reduced-motion'));
});
