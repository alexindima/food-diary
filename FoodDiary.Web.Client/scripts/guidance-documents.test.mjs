import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { inspectDocument } from '../quality/modern-web-guidance/documents.mjs';

for (const file of ['../src/index.html', '../projects/fooddiary-admin/src/index.html']) {
    test(`${file} declares OS text scaling and usable browser colors before Angular startup`, () => {
        assert.deepEqual(
            inspectDocument(readFileSync(new URL(file, import.meta.url), 'utf8'), file),
            [],
            'Platform metadata must work before Angular startup',
        );
    });
}
test('every account theme provides a concrete browser panel color', () => {
    const config = readFileSync(new URL('../src/app/theme/app-theme.config.ts', import.meta.url), 'utf8');
    const values = [...config.matchAll(/browserThemeColor:\s*'([^']+)'/gu)].map(match => match[1]);
    assert.equal(values.length, 3);
    assert.ok(
        values.every(value => /^#[\da-f]{6}$/iu.test(value)),
        'Theme metadata cannot resolve CSS variables',
    );
});
