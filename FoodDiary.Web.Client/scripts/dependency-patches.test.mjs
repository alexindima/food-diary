import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { test } from 'node:test';
import { applyDependencyPatches } from './apply-dependency-patches.mjs';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const braces = require('braces');
const micromatch = require('micromatch');

test('every installed braces copy has the reproducible mitigation', async () => {
    assert.ok(await applyDependencyPatches(fileURLToPath(new URL('..', import.meta.url)), true));
});

for (const [name, pattern] of [
    ['nested braces', '{'.repeat(4000) + 'x' + '}'.repeat(4000)],
    ['unclosed braces', '{'.repeat(4000) + 'x'],
    ['parenthesis AST nodes', '('.repeat(4000) + 'x' + ')'.repeat(4000)],
]) {
    test(`${name} rejects excessive depth before recursive walking`, () => {
        for (const operation of [braces, braces.parse, braces.compile, braces.expand, braces.stringify]) {
            assert.throws(() => operation(pattern), { name: 'SyntaxError', message: /nesting exceeds/ });
        }
    });
}

test('all AST entry points reject deep trees and cycles', () => {
    let ast = { type: 'text', value: 'x' };
    for (let i = 0; i < 200; i++) ast = { type: 'root', nodes: [ast] };
    const cyclic = { type: 'root', nodes: [] };
    cyclic.nodes.push(cyclic);
    for (const operation of [braces.compile, braces.expand, braces.stringify]) {
        for (const input of [ast, cyclic]) assert.throws(() => operation(input), { name: 'SyntaxError' });
    }
});

test('legitimate ranges, nested alternatives and lint matcher behavior remain intact', () => {
    assert.deepEqual(braces.expand('src/{a,{b,c}}/{1..3}.ts'), [
        'src/a/1.ts',
        'src/a/2.ts',
        'src/a/3.ts',
        'src/b/1.ts',
        'src/b/2.ts',
        'src/b/3.ts',
        'src/c/1.ts',
        'src/c/2.ts',
        'src/c/3.ts',
    ]);
    assert.equal(braces.stringify(braces.parse('src/{a,b}.ts')), 'src/{a,b}.ts');
    assert.deepEqual(micromatch(['src/a.ts', 'src/b.html', 'src/c.scss', 'test/a.ts'], 'src/*.{ts,html}'), ['src/a.ts', 'src/b.html']);
});
