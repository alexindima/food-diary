import { createRequire } from 'node:module';
const require = createRequire(new URL('../../package.json', import.meta.url));
const { JSDOM } = require('jsdom');

export function inspectDocument(text, file) {
    const dom = new JSDOM(text, { includeNodeLocations: true });
    const doc = dom.window.document,
        findings = [];
    const fail = (node, message) =>
        findings.push({ check: 'document:platform', file, line: dom.nodeLocation(node)?.startLine ?? 1, message });
    if (doc.doctype?.name !== 'html') fail(doc.documentElement, 'Declare the HTML5 doctype.');
    if (!doc.documentElement.lang) fail(doc.documentElement, 'Declare the document language.');
    const scale = doc.querySelector('meta[name="text-scale"]');
    if (scale?.content !== 'scale') fail(scale ?? doc.head, 'Opt in to OS text scaling with meta name="text-scale" content="scale".');
    if (!doc.querySelector('meta[name="color-scheme"]')) fail(doc.head, 'Declare supported color schemes before the document paints.');
    for (const node of doc.querySelectorAll('meta[name="theme-color"]')) {
        if (/\bvar\(/u.test(node.content))
            fail(node, 'theme-color metadata requires a concrete color; CSS variables do not resolve in HTML metadata.');
    }
    for (const node of doc.querySelectorAll('script[src]')) {
        if (!node.hasAttribute('async') && !node.hasAttribute('defer') && node.type !== 'module')
            fail(node, 'External scripts must not block the HTML parser.');
    }
    for (const node of doc.querySelectorAll('link[rel="preload"]')) {
        if (!node.as && !node.getAttribute('as')) fail(node, 'Preload requires the correct as resource type.');
        if (node.getAttribute('as') === 'font' && !node.hasAttribute('crossorigin')) fail(node, 'Font preload requires crossorigin.');
    }
    for (const node of doc.querySelectorAll('script[importance], link[importance]')) fail(node, 'Use fetchpriority instead of importance.');
    dom.window.close();
    return findings;
}
