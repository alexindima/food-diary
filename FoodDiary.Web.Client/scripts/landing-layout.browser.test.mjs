import assert from 'node:assert/strict';
import { after, before, test } from 'node:test';
import { readFileSync } from 'node:fs';
import { createServer, request } from 'node:http';
import { fileURLToPath } from 'node:url';

import { chromium } from '@playwright/test';
import { JSDOM } from 'jsdom';

import { startStaticServer } from './lighthouse-public-server.mjs';

const root = fileURLToPath(new URL('../dist/browser/', import.meta.url));
const documents = new Map(
    ['/', '/ru'].map(path => {
        let html = readFileSync(`${root}${path === '/' ? 'index.html' : 'ru/index.html'}`, 'utf8');
        const dom = new JSDOM(html);
        try {
            const stylesheets = new Set(
                [...dom.window.document.querySelectorAll('link[rel="stylesheet"]')].map(link => link.getAttribute('href')),
            );
            // Hold CSS/font state constant so this regression isolates HTML arriving after the SVG.
            const css = [...stylesheets].map(href => readFileSync(new URL(`../dist/browser/${href}`, import.meta.url), 'utf8')).join('\n');
            html = html.replace('</head>', `<style>${css}</style></head>`);
        } finally {
            dom.window.close();
        }
        const summary = html.indexOf('<fd-day-nutrition-summary');
        const details = html.indexOf('day-summary__details', summary);
        const split = html.lastIndexOf('<div', details);
        assert.ok(summary >= 0 && details > summary && split > summary, `Missing prerendered day summary at ${path}. Build first.`);
        return [path, { html, split }];
    }),
);
let browser;
let assets;
let server;
let origin;
let responseTail;

before(async () => {
    assets = await startStaticServer(root);
    server = createServer((req, res) => {
        const document = documents.get(new URL(req.url, 'http://localhost').pathname);
        if (req.method === 'GET' && document) {
            res.writeHead(200, {
                'Content-Type': 'text/html; charset=utf-8',
                'Content-Length': Buffer.byteLength(document.html),
                'Cache-Control': 'no-store',
            });
            res.write(document.html.slice(0, document.split));
            void responseTail.then(() => res.end(document.html.slice(document.split)));
            return;
        }
        const upstream = request(new URL(req.url, assets.origin), { method: req.method, headers: req.headers }, incoming => {
            res.writeHead(incoming.statusCode, incoming.headers);
            incoming.pipe(res);
        });
        upstream.on('error', () => res.writeHead(502).end());
        req.pipe(upstream);
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    origin = `http://127.0.0.1:${server.address().port}`;
    browser = await chromium.launch({ channel: process.env.FD_WEB_QUALITY_CHANNEL ?? 'chrome', headless: true });
});

after(async () => {
    await browser?.close();
    if (server) await new Promise(resolve => server.close(resolve));
    await assets?.close();
});

async function titlePosition(page) {
    return page.locator('.hero__title').evaluate(async title => {
        await document.fonts.ready;
        await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
        return { top: title.getBoundingClientRect().top, text: title.textContent };
    });
}

for (const width of [1024, 1365, 390]) {
    for (const path of ['/', '/ru']) {
        test(`streamed landing preserves its first-screen position at ${path}, ${width}px`, async () => {
            const context = await browser.newContext({ viewport: { width, height: 900 }, serviceWorkers: 'block' });
            const page = await context.newPage();
            const errors = [];
            page.on('pageerror', error => errors.push(error.message));
            await page.route('**/api/v1/**', async route => {
                const pathname = new URL(route.request().url()).pathname;
                assert.ok(['/api/v1/auth/session', '/api/v1/logs', '/api/v1/marketing/attribution-events'].includes(pathname));
                await route.fulfill({ status: 204 });
            });
            let release;
            responseTail = new Promise(resolve => {
                release = resolve;
            });
            try {
                await page.goto(origin + path, { waitUntil: 'commit' });
                await page.waitForFunction(() => document.querySelector('.hero__title')?.getBoundingClientRect().height > 0);
                const initial = await titlePosition(page);
                release();
                await page.waitForFunction(() => document.documentElement.hasAttribute('data-app-ready'));
                const settled = await titlePosition(page);
                assert.equal(settled.text, initial.text, 'The real localized heading must survive hydration.');
                assert.ok(
                    Math.abs(settled.top - initial.top) < 2,
                    `Streamed hero moved ${Math.abs(settled.top - initial.top).toFixed(2)}px when the summary rows arrived.`,
                );
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
                assert.deepEqual(errors, []);
            } finally {
                release();
                await context.close();
            }
        });
    }
}
