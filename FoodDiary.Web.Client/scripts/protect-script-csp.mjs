import { createHash } from 'node:crypto';
import { mkdir, readFile, readdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { JSDOM } from 'jsdom';

const sources = JSON.parse(await readFile(new URL('../src/app/shared/security/script-csp-sources.json', import.meta.url), 'utf8'));
const hash = (algorithm, text) =>
    createHash(algorithm)
        .update(text)
        .digest(algorithm === 'sha1' ? 'hex' : 'base64');

/** Authorize only inline content in trusted build documents, never rendered user content. */
export function protectDocument(source) {
    let html = source;
    const dom = new JSDOM(source, { includeNodeLocations: true });
    try {
        const document = dom.window.document;
        const owned = [...document.querySelectorAll('meta[data-fd-script-csp]')];
        const locations = owned.map(meta => dom.nodeLocation(meta)).sort((a, b) => b.startOffset - a.startOffset);
        for (const location of locations) html = html.slice(0, location.startOffset) + html.slice(location.endOffset);
        owned.forEach(meta => meta.remove());
        if ([...document.querySelectorAll('meta[http-equiv]')].some(meta => meta.httpEquiv.toLowerCase() === 'content-security-policy')) {
            throw new Error('A build document already has an unmanaged CSP policy.');
        }
        if (document.querySelector('script[nonce], [ngcspnonce]'))
            throw new Error('Static documents must not contain a reusable CSP nonce.');
        for (const element of document.querySelectorAll('*')) {
            if ([...element.attributes].some(attribute => /^on/iu.test(attribute.name))) {
                throw new Error('Inline event attributes are incompatible with the script CSP.');
            }
        }
        const hashes = [
            ...new Set(
                [...document.scripts]
                    .filter(script => !script.hasAttribute('src') && !/^application\/(?:ld\+)?json$/iu.test(script.type.trim()))
                    .filter(script => script.textContent.trim().length > 0)
                    .map(script => `'sha256-${hash('sha256', script.textContent)}'`),
            ),
        ].sort();
        const policy = `script-src 'self' ${[...hashes, ...sources].join(' ')};`;
        const meta = `<meta data-fd-script-csp="" http-equiv="Content-Security-Policy" content="${policy}">`;
        const head = /<head\b[^>]*>/iu.exec(html);
        if (head === null) throw new Error('A protected build document must have an explicit head.');
        const start = head.index + head[0].length;
        const firstScript = html.slice(start).search(/<script\b/iu);
        const charset = /<meta\b[^>]*\scharset\s*=[^>]*>/iu.exec(html.slice(start, firstScript < 0 ? undefined : start + firstScript));
        const insertion = charset === null ? start : start + charset.index + charset[0].length;
        return { html: html.slice(0, insertion) + meta + html.slice(insertion), hashes, policy };
    } finally {
        dom.window.close();
    }
}

export async function protectBuild(dist) {
    const browser = path.join(dist, 'browser');
    const entries = await readdir(browser, { recursive: true, withFileTypes: true });
    const documents = entries
        .filter(entry => entry.isFile() && entry.name.endsWith('.html'))
        .map(entry => path.join(entry.parentPath, entry.name))
        .sort();
    if (documents.length === 0) throw new Error('No built HTML documents were found for script CSP.');
    const allHashes = new Set();
    const updated = new Map();
    for (const file of documents) {
        const source = await readFile(file, 'utf8');
        const result = protectDocument(source);
        result.hashes.forEach(value => allHashes.add(value));
        if (source !== result.html) await writeFile(file, result.html);
        updated.set(`/${path.relative(browser, file).replaceAll('\\', '/')}`, hash('sha1', result.html));
    }
    const manifestPath = path.join(browser, 'ngsw.json');
    let manifest;
    try {
        manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
    } catch (error) {
        if (error.code !== 'ENOENT') throw error;
    }
    if (manifest !== undefined) {
        if (manifest.hashTable === null || typeof manifest.hashTable !== 'object') throw new Error('Invalid service worker hash table.');
        for (const [url, digest] of updated) {
            if (Object.hasOwn(manifest.hashTable, url)) manifest.hashTable[url] = digest;
        }
        await writeFile(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`);
    }
    await mkdir(path.join(dist, 'server'), { recursive: true });
    await writeFile(path.join(dist, 'server/script-csp-hashes.txt'), `${[...allHashes].sort().join(' ')}\n`);
    return { documents: documents.length, hashes: allHashes.size };
}

if (process.argv[1] !== undefined && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    const result = await protectBuild(path.resolve('dist'));
    console.log(`Script CSP: ${result.documents} trusted HTML documents, ${result.hashes} inline script hashes.`);
}
