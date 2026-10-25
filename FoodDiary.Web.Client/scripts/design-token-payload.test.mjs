import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';
import { parse } from 'postcss-scss';
import * as sass from 'sass';

const frontend = fileURLToPath(new URL('../', import.meta.url));
const entry = readFileSync(path.join(frontend, 'src/styles/design-tokens.global.scss'), 'utf8');
const compile = source =>
    sass.compileString(source, {
        style: 'compressed',
        loadPaths: [path.join(frontend, 'src/styles'), path.join(frontend, 'projects/fd-ui-kit/src/lib')],
    }).css;

// The complete two-argument mixin is also used by isolated theme consumers.
// Compare its declarations with the grouped entry point, including each scope
// where var() aliases must resolve locally rather than inherit a computed value.
const legacyThemes = `
@include design-tokens.fd-emit-css-variables(ocean, ':root, html[data-theme="ocean"], .fd-root--public-site');
@include design-tokens.fd-emit-css-variables(leaf, 'html[data-theme="leaf"]');
@include design-tokens.fd-emit-css-variables(dark, 'html[data-theme="dark"]');
`;
const legacyEntry = entry.replace(
    /(?:\/\/[^\n]*\n|\s|@include design-tokens\.fd-emit-(?:shared-css-variables|css-variables)\([\s\S]*?\);)+(?=@include design-tokens\.fd-emit-ui-style-variables)/u,
    '\n' + legacyThemes + '\n',
);
const optimized = compile(entry);
const legacy = compile(legacyEntry);

function declarationsByScope(css) {
    const scopes = new Map();
    parse(css).walkRules(rule => {
        const conditions = [];
        for (let parent = rule.parent; parent?.type === 'atrule'; parent = parent.parent) {
            conditions.unshift(`@${parent.name} ${parent.params}`);
        }
        for (const selector of rule.selectors) {
            const key = JSON.stringify([...conditions, selector]);
            const declarations = scopes.get(key) ?? new Map();
            rule.nodes
                .filter(node => node.type === 'decl')
                .forEach(node => {
                    declarations.set(node.prop, { value: node.value, important: node.important });
                });
            scopes.set(key, declarations);
        }
    });
    return scopes;
}

test('grouped theme tokens keep every declaration on every original selector and media condition', () => {
    assert.deepEqual(declarationsByScope(optimized), declarationsByScope(legacy));
});

test('shared theme tokens reduce the global stylesheet payload', () => {
    assert.ok(
        Buffer.byteLength(optimized) < Buffer.byteLength(legacy),
        `Grouped token CSS must be smaller than repeated theme CSS (${Buffer.byteLength(optimized)} vs ${Buffer.byteLength(legacy)} bytes)`,
    );
});

test('the equivalence check catches loss of locally resolved public-site aliases', () => {
    const broken = parse(optimized);
    broken.walkRules(rule => {
        if (rule.nodes.some(node => node.prop === '--fd-bg-surface')) {
            rule.selectors = rule.selectors.filter(selector => selector !== '.fd-root--public-site');
        }
    });
    assert.notDeepEqual(declarationsByScope(broken.toString()), declarationsByScope(legacy));
});
