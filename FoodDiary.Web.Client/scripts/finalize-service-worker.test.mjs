import assert from 'node:assert/strict';
import { test } from 'node:test';

import { collectStartupJavascript, splitServiceWorkerJavascript } from './finalize-service-worker.mjs';

const stats = {
    outputs: {
        'main-A.js': {
            entryPoint: 'src/main.ts',
            imports: [
                { path: 'shared-B.js', kind: 'import-statement' },
                { path: 'private-C.js', kind: 'dynamic-import' },
                { path: 'https://example.com/external.js', kind: 'import-statement', external: true },
            ],
        },
        'landing-D.js': {
            entryPoint: 'src/app/features/public/pages/landing/main.ts',
            imports: [{ path: 'shared-B.js', kind: 'import-statement' }],
        },
        'shared-B.js': { imports: [{ path: 'main-A.js', kind: 'import-statement' }] },
        'polyfills-E.js': { entryPoint: 'angular:polyfills', imports: [] },
        'private-C.js': { imports: [] },
    },
};

const createManifest = () => ({
    index: '/index.html',
    assetGroups: [
        {
            name: 'app',
            installMode: 'prefetch',
            updateMode: 'prefetch',
            urls: ['/index.html', '/styles.css', ...Object.keys(stats.outputs).map(name => `/${name}`)],
        },
        { name: 'lazy-javascript', installMode: 'lazy', updateMode: 'prefetch', urls: [] },
        { name: 'images', urls: ['/logo.svg'] },
    ],
    hashTable: Object.fromEntries(
        ['/index.html', '/styles.css', '/logo.svg', ...Object.keys(stats.outputs).map(name => `/${name}`)].map(name => [
            name,
            `hash:${name}`,
        ]),
    ),
});

test('keeps recursive startup imports and the offline landing, while deferring dynamic routes', () => {
    // Arrange
    const buildStats = structuredClone(stats);
    // Act
    const startup = collectStartupJavascript(buildStats);
    // Assert
    assert.deepEqual([...startup].sort(), ['/landing-D.js', '/main-A.js', '/polyfills-E.js', '/shared-B.js']);
});

test('preserves version hashes, non-JavaScript assets and update behavior', () => {
    // Arrange
    const manifest = createManifest();
    // Act
    const result = splitServiceWorkerJavascript(manifest, stats);
    // Assert
    assert.deepEqual(result.assetGroups[1].urls, ['/private-C.js']);
    assert.equal(result.assetGroups[1].updateMode, 'prefetch');
    assert.ok(result.assetGroups[0].urls.includes('/styles.css'));
    assert.deepEqual(result.assetGroups[2], manifest.assetGroups[2]);
    assert.deepEqual(result.hashTable, manifest.hashTable);
    assert.equal(manifest.assetGroups[1].urls.length, 0);
});

test('fails the build when startup metadata or a versioned dependency is missing', () => {
    // Arrange
    const incompleteStats = structuredClone(stats);
    delete incompleteStats.outputs['shared-B.js'];
    const manifest = createManifest();
    delete manifest.hashTable['/shared-B.js'];
    // Act / Assert
    assert.throws(() => collectStartupJavascript(incompleteStats), /Missing startup JavaScript dependency/u);
    assert.throws(() => splitServiceWorkerJavascript(manifest, stats), /absent from the versioned/u);
});

test('can run twice without changing the cached application version', () => {
    // Arrange
    const first = splitServiceWorkerJavascript(createManifest(), stats);
    // Act
    const second = splitServiceWorkerJavascript(first, stats);
    // Assert
    assert.deepEqual(second, first);
});
