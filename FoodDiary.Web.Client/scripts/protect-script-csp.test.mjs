import assert from 'node:assert/strict';
import { createHash, randomUUID } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

import { JSDOM } from 'jsdom';
import { protectBuild, protectDocument } from './protect-script-csp.mjs';

const script = 'window.trusted = true;';
const expectedHash = `'sha256-${createHash('sha256').update(script).digest('base64')}'`;
const documentSource = `<html><head><meta charset="utf-8"><script>${script}</script><script src="/main.js" type="module"></script></head><body><script type="application/ld+json">{"name":"data"}</script></body></html>`;

test('hashes trusted inline scripts, excludes data and places protection before executable content', () => {
    const result = protectDocument(documentSource);
    assert.deepEqual(result.hashes, [expectedHash]);
    assert.ok(result.html.indexOf('data-fd-script-csp') < result.html.indexOf('<script>'));
    assert.ok(result.html.indexOf('charset=') < result.html.indexOf('data-fd-script-csp'));
    assert.ok(result.html.includes(`<script>${script}</script>`), 'Build protection must preserve script bytes.');
    const parsed = new JSDOM(result.html);
    const policy = parsed.window.document.querySelector('meta[data-fd-script-csp]').content;
    assert.ok(policy.includes(expectedHash));
    assert.ok(!policy.includes('unsafe-inline') && !policy.includes('unsafe-eval'));
    for (const origin of ['https://accounts.google.com', 'https://cdn.paddle.com', 'https://telegram.org'])
        assert.ok(policy.includes(origin));
    parsed.window.close();
    assert.equal(protectDocument(result.html).html, result.html, 'Protection must be idempotent.');
});

test('never rewrites script strings that happen to contain a generated meta marker', () => {
    const code = 'window.example = `<meta data-fd-script-csp="">`;';
    const result = protectDocument(`<html><head><script>${code}</script></head><body></body></html>`);
    assert.ok(result.html.includes(`<script>${code}</script>`));
    assert.equal(protectDocument(result.html).html, result.html);
});

test('rejects reusable nonces, inline event attributes and unmanaged policies at build time', () => {
    for (const content of [
        '<script nonce="fixed">alert(1)</script>',
        '<div onclick="alert(1)"></div>',
        '<meta http-equiv="Content-Security-Policy" content="script-src *">',
        '<div ngCspNonce="fixed"></div>',
    ]) {
        assert.throws(() => protectDocument(`<html><head>${content}</head><body></body></html>`));
    }
});

test('updates PWA document hashes and publishes only build-owned SSR hashes outside browser assets', async () => {
    const root = fileURLToPath(new URL('../../.artifacts/session-hardening/csp-unit/', import.meta.url));
    const dist = path.join(root, randomUUID());
    await mkdir(path.join(dist, 'browser'), { recursive: true });
    await writeFile(path.join(dist, 'browser/index.html'), documentSource);
    await writeFile(path.join(dist, 'browser/ngsw.json'), JSON.stringify({ hashTable: { '/index.html': 'old', '/main.js': 'keep' } }));
    const result = await protectBuild(dist);
    assert.deepEqual(result, { documents: 1, hashes: 1 });
    const html = await readFile(path.join(dist, 'browser/index.html'), 'utf8');
    const manifest = JSON.parse(await readFile(path.join(dist, 'browser/ngsw.json'), 'utf8'));
    assert.equal(manifest.hashTable['/index.html'], createHash('sha1').update(html).digest('hex'));
    assert.equal(manifest.hashTable['/main.js'], 'keep');
    assert.equal((await readFile(path.join(dist, 'server/script-csp-hashes.txt'), 'utf8')).trim(), expectedHash);
    await protectBuild(dist);
    assert.equal(await readFile(path.join(dist, 'browser/index.html'), 'utf8'), html);
});
