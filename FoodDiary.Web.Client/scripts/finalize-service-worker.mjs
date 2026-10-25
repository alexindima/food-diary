import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { assertNoDevelopmentRuntime } from './check-production-bundles.mjs';

const startupEntryPoints = ['src/main.ts', 'src/app/features/public/pages/landing/main.ts'];
const normalizePath = value => value.replaceAll('\\', '/').replace(/^\.\//u, '');

export function collectStartupJavascript(stats, entryPoints = startupEntryPoints) {
    const outputs = new Map(Object.entries(stats.outputs).map(([name, output]) => [normalizePath(name), output]));
    const roots = [];
    for (const entryPoint of entryPoints) {
        const output = [...outputs.entries()].find(([, value]) => normalizePath(value.entryPoint ?? '') === entryPoint);
        if (output === undefined) {
            throw new Error(`Missing browser startup entry point: ${entryPoint}`);
        }
        roots.push(output[0]);
    }
    for (const [name, output] of outputs) {
        if (/^polyfills(?:-|\.)/u.test(name) || output.entryPoint?.startsWith('angular:script/')) {
            roots.push(name);
        }
    }

    const visited = new Set();
    const visit = name => {
        if (visited.has(name)) {
            return;
        }
        const output = outputs.get(name);
        if (output === undefined) {
            throw new Error(`Missing startup JavaScript dependency: ${name}`);
        }
        visited.add(name);
        for (const dependency of output.imports ?? []) {
            if (dependency.external || dependency.kind === 'dynamic-import' || !dependency.path.endsWith('.js')) {
                continue;
            }
            const dependencyName = normalizePath(dependency.path);
            const resolved = outputs.has(dependencyName) ? dependencyName : path.posix.join(path.posix.dirname(name), dependencyName);
            visit(resolved);
        }
    };
    roots.forEach(visit);
    return new Set([...visited].filter(name => name.endsWith('.js')).map(name => `/${name}`));
}

export function splitServiceWorkerJavascript(manifest, stats) {
    const result = structuredClone(manifest);
    const shell = result.assetGroups.find(group => group.name === 'app');
    const lazy = result.assetGroups.find(group => group.name === 'lazy-javascript');
    if (shell === undefined || lazy === undefined) {
        throw new Error('Service worker requires app and lazy-javascript asset groups.');
    }
    const startup = collectStartupJavascript(stats);
    const allJavascript = new Set([...shell.urls, ...lazy.urls].filter(url => url.endsWith('.js')));
    for (const url of startup) {
        if (!allJavascript.has(url) || result.hashTable[url] === undefined) {
            throw new Error(`Startup dependency is absent from the versioned service worker assets: ${url}`);
        }
    }
    shell.urls = [...new Set([...shell.urls.filter(url => !url.endsWith('.js')), ...startup])].sort();
    lazy.urls = [...allJavascript].filter(url => !startup.has(url)).sort();
    return result;
}

export function assertDeferredPublicStartup(stats) {
    const startup = collectStartupJavascript(stats);
    for (const [output, metadata] of Object.entries(stats.outputs)) {
        if (!startup.has(`/${normalizePath(output)}`)) continue;
        for (const [input, contribution] of Object.entries(metadata.inputs ?? {})) {
            const source = normalizePath(input);
            if (
                contribution.bytesInOutput > 0 &&
                (source.includes('projects/fd-tour/src/') ||
                    source.endsWith('src/app/components/shared/app-update-notice/app-update-notice.ts'))
            )
                throw new Error(`Deferred feature in public startup: ${output}: ${input}`);
        }
    }
}

if (process.argv[1] !== undefined && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    const distDir = path.resolve('dist');
    const manifestPath = path.join(distDir, 'browser/ngsw.json');
    if (!fs.existsSync(manifestPath)) {
        console.log('Service worker finalization skipped: this build has no service worker.');
    } else {
        const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
        const stats = JSON.parse(fs.readFileSync(path.join(distDir, 'browser-stats.json'), 'utf8'));
        assertNoDevelopmentRuntime(stats);
        assertDeferredPublicStartup(stats);
        const result = splitServiceWorkerJavascript(manifest, stats);
        fs.writeFileSync(manifestPath, `${JSON.stringify(result, null, 2)}\n`);
        const shell = result.assetGroups.find(group => group.name === 'app');
        const lazy = result.assetGroups.find(group => group.name === 'lazy-javascript');
        console.log(
            `Service worker: ${shell.urls.filter(url => url.endsWith('.js')).length} startup JS files, ${lazy.urls.length} on demand.`,
        );
    }
}
