import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { parse } from 'postcss-scss';

const directory = new URL('../src/fonts/inter/', import.meta.url);
const manifest = JSON.parse(readFileSync(new URL('manifest.json', directory), 'utf8'));
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
test('local Inter assets retain pinned WOFF2 data, license and complete font-face metadata', () => {
    assert.equal(manifest.files.length, 7);
    assert.equal(hash(readFileSync(new URL(manifest.license.file, directory))), manifest.license.sha256);
    const faces = [];
    parse(readFileSync(new URL('../src/styles/fonts.scss', import.meta.url), 'utf8')).walkAtRules('font-face', rule => {
        const declarations = {};
        rule.walkDecls(declaration => {
            declarations[declaration.prop] = declaration.value;
        });
        faces.push(declarations);
    });
    assert.equal(faces.length, manifest.files.length);
    for (const font of manifest.files) {
        const bytes = readFileSync(new URL(font.file, directory));
        assert.equal(bytes.subarray(0, 4).toString(), 'wOF2');
        assert.equal(hash(bytes), font.sha256);
        assert.equal(bytes.length, font.bytes);
        assert.ok(font.source.startsWith(`https://fonts.gstatic.com/s/inter/v${manifest.googleFontsVersion}/`));
        const face = faces.find(face => face.src.includes(font.file));
        assert.ok(face, `Missing font face: ${font.subset}`);
        assert.equal(face['font-family'].replaceAll(/['"]/gu, ''), manifest.family);
        assert.equal(face['font-display'], 'swap');
        assert.equal(face['font-weight'], '400 600');
        assert.equal(face['unicode-range'].replaceAll(/\s+/gu, ''), font.unicodeRange.replaceAll(/\s+/gu, ''));
    }
});
test('application entry documents do not depend on a remote Inter stylesheet', () => {
    for (const file of ['../src/index.html', '../projects/fooddiary-admin/src/index.html']) {
        const source = readFileSync(new URL(file, import.meta.url), 'utf8');
        assert.doesNotMatch(source, /fonts\.(?:googleapis|gstatic)\.com/u);
    }
});
