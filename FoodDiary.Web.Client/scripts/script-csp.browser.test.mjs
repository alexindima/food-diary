import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { cp, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { test } from 'node:test';

import { chromium } from '@playwright/test';

import { assertPwaCachePolicy } from './pwa-cache.assertions.mjs';

const execute = promisify(execFile);
const frontend = fileURLToPath(new URL('../', import.meta.url));
const repository = path.dirname(frontend);
const docker = async (...args) => (await execute('docker', args, { maxBuffer: 8 * 1024 * 1024 })).stdout.trim();

async function waitFor(url) {
    const deadline = Date.now() + 30_000;
    while (Date.now() < deadline) {
        try {
            if ((await fetch(url)).ok) return;
        } catch {
            /* Container is still starting. */
        }
        await delay(200);
    }
    throw new Error(`Owned CSP test service did not become ready: ${url}`);
}

test('deployed public Nginx revalidates PWA install metadata', { timeout: 60_000 }, async () => {
    const artifacts = path.join(repository, '.artifacts/pwa-telegram-investigation', randomUUID());
    await mkdir(artifacts, { recursive: true });
    const source = await readFile(path.join(repository, 'nginx/sites-enabled/fooddiary.club'), 'utf8');
    const start = source.lastIndexOf('server {', source.indexOf('root /var/www/fooddiary.club/current;'));
    assert.ok(start >= 0, 'The deployed public site must have an explicit server block.');
    let end = start + 'server {'.length;
    let depth = 1;
    while (depth > 0 && end < source.length) {
        depth += Number(source[end] === '{') - Number(source[end] === '}');
        end++;
    }
    assert.equal(depth, 0, 'The deployed server block must be complete.');
    // Preserve every production location and header; substitute only TLS,
    // filesystem paths and upstream addresses for the isolated HTTP fixture.
    const server = source
        .slice(start, end)
        .replace(/^\s*(?:listen|http2|ssl_\w+|access_log|error_log)\s+[^;]+;/gmu, '')
        .replace(/root\s+[^;]+;/gu, 'root /usr/share/nginx/html;')
        .replace(/proxy_pass\s+[^;]+;/gu, 'proxy_pass http://127.0.0.1:9;')
        .replace('server {', 'server {\nlisten 80;');
    await writeFile(path.join(artifacts, 'nginx.conf'), `map $http_upgrade $connection_upgrade { default upgrade; '' close; }\n${server}`);
    const dockerfile = await readFile(path.join(frontend, 'Dockerfile'), 'utf8');
    const nginxImage = /^FROM (nginx:\S+)/mu.exec(dockerfile)[1];
    let container;
    try {
        container = await docker(
            'run',
            '--detach',
            '--publish',
            '127.0.0.1::80',
            '--mount',
            `type=bind,source=${path.join(frontend, 'dist/browser')},target=/usr/share/nginx/html,readonly`,
            '--mount',
            `type=bind,source=${path.join(artifacts, 'nginx.conf')},target=/etc/nginx/conf.d/default.conf,readonly`,
            nginxImage,
        );
        const boundPort = await docker('port', container, '80/tcp');
        const origin = `http://localhost:${boundPort.split(':').at(-1)}`;
        await waitFor(`${origin}/index.csr.html`);
        const observations = await assertPwaCachePolicy(origin);
        await writeFile(path.join(artifacts, 'cache-observations.json'), JSON.stringify(observations, null, 2));
    } finally {
        if (container) await docker('rm', '--force', container);
    }
});

test(
    'production scripts remain usable while unapproved inline code is blocked and PWA install assets revalidate in static and SSR documents',
    { timeout: 180_000 },
    async () => {
        const runId = randomUUID();
        const artifacts = path.join(repository, '.artifacts/session-hardening/csp-browser', runId);
        await mkdir(artifacts, { recursive: true });
        const browserRoot = path.join(artifacts, 'browser');
        await cp(path.join(frontend, 'dist/browser'), browserRoot, { recursive: true });
        const probe = path.join(browserRoot, 'index.csr.html');
        const probeSource = await readFile(probe, 'utf8');
        await writeFile(probe, probeSource.replace('</body>', '<script>window.__unapprovedInlineRan=true</script></body>'));
        const dockerfile = await readFile(path.join(frontend, 'Dockerfile'), 'utf8');
        const nodeImage = /^FROM (node:\S+)/mu.exec(dockerfile)[1];
        const nginxImage = /^FROM (nginx:\S+)/mu.exec(dockerfile)[1];
        const ownedContainers = [];
        let network;
        let browser;
        const observations = { violations: [], pages: [] };
        try {
            network = await docker('network', 'create', `fd-csp-${runId}`);
            ownedContainers.push(
                await docker(
                    'run',
                    '--detach',
                    '--network',
                    network,
                    '--network-alias',
                    'client-ssr',
                    '--mount',
                    `type=bind,source=${path.join(frontend, 'dist/server')},target=/app/server,readonly`,
                    '--mount',
                    `type=bind,source=${path.join(frontend, 'dist/browser')},target=/app/browser,readonly`,
                    '--env',
                    'SSR_API_ORIGIN=http://127.0.0.1:9',
                    nodeImage,
                    'node',
                    '/app/server/server.mjs',
                ),
            );
            ownedContainers.push(
                await docker(
                    'run',
                    '--detach',
                    '--network',
                    network,
                    '--publish',
                    '127.0.0.1::80',
                    '--mount',
                    `type=bind,source=${browserRoot},target=/usr/share/nginx/html,readonly`,
                    '--mount',
                    `type=bind,source=${path.join(frontend, 'nginx.conf')},target=/etc/nginx/conf.d/default.conf,readonly`,
                    nginxImage,
                ),
            );
            const boundPort = await docker('port', ownedContainers[1], '80/tcp');
            const origin = `http://localhost:${boundPort.split(':').at(-1)}`;
            await waitFor(`${origin}/index.csr.html`);
            observations.installAssets = await assertPwaCachePolicy(origin);
            assert.equal((await fetch(`${origin}/explore`)).status, 200, 'SSR fixture must render before CSP checks.');
            browser = await chromium.launch(process.platform === 'win32' ? { channel: 'chrome' } : {});
            const context = await browser.newContext({ serviceWorkers: 'block' });
            await context.route('**/*', route =>
                new URL(route.request().url()).origin === origin
                    ? route.continue()
                    : route.fulfill({ status: 503, contentType: 'application/json', body: '{}' }),
            );
            await context.addInitScript(() => {
                window.__scriptViolations = [];
                document.addEventListener('securitypolicyviolation', event => {
                    if (event.effectiveDirective.startsWith('script-src')) {
                        window.__scriptViolations.push({ directive: event.effectiveDirective, blocked: event.blockedURI });
                    }
                });
            });
            const page = await context.newPage();
            await page.goto(`${origin}/dashboard`, { waitUntil: 'networkidle' });
            await page.waitForFunction(() => document.querySelector('fd-root')?.children.length > 0);
            observations.pages.push({
                url: page.url(),
                policy: await page
                    .locator('meta[data-fd-script-csp]')
                    .getAttribute('content', { timeout: 1000 })
                    .catch(() => null),
            });
            await page.screenshot({ path: path.join(artifacts, 'static.png'), fullPage: true });
            assert.equal(
                await page.evaluate(() => window.__unapprovedInlineRan),
                undefined,
                'Unapproved inline script must not execute under the production CSP.',
            );
            const staticViolations = await page.evaluate(() => window.__scriptViolations);
            assert.deepEqual(
                staticViolations.map(value => value.blocked),
                ['inline'],
            );
            observations.violations.push(...staticViolations);
            let previousNonce;
            for (let index = 0; index < 2; index++) {
                const response = await page.goto(`${origin}/explore`, { waitUntil: 'networkidle' });
                assert.ok(response.ok());
                const policy = await response.headerValue('content-security-policy');
                const nonce = /'nonce-([A-Za-z0-9+/]{32})'/u.exec(policy ?? '')?.[1];
                assert.ok(nonce, 'SSR must send a cryptographically generated script nonce.');
                assert.notEqual(nonce, previousNonce, 'SSR nonce must be unique per response.');
                previousNonce = nonce;
                assert.match(await response.headerValue('cache-control'), /no-store/u);
                assert.ok(await page.locator('script[nonce]').evaluateAll(scripts => scripts.some(script => script.nonce.length > 0)));
                assert.deepEqual(
                    await page.evaluate(() => window.__scriptViolations),
                    [],
                    'Trusted SSR scripts must pass the enforced CSP.',
                );
                assert.equal(
                    await page.evaluate(() => {
                        const script = document.createElement('script');
                        script.textContent = 'window.__unapprovedSsrInlineRan=true';
                        document.body.append(script);
                        return window.__unapprovedSsrInlineRan;
                    }),
                    undefined,
                    'SSR must block an inline script that has no trusted hash or nonce.',
                );
                observations.pages.push({ url: page.url(), policy });
            }
            await page.screenshot({ path: path.join(artifacts, 'ssr.png'), fullPage: true });
        } finally {
            await browser?.close();
            for (const container of ownedContainers.reverse()) {
                assert.match(container, /^[a-f0-9]{64}$/u);
                await writeFile(
                    path.join(artifacts, `${container.slice(0, 12)}.log`),
                    await docker('logs', container).catch(() => 'Container log unavailable'),
                );
                await docker('rm', '--force', container);
            }
            if (network !== undefined) {
                assert.match(network, /^[a-f0-9]{64}$/u);
                await docker('network', 'rm', network);
            }
            await writeFile(path.join(artifacts, 'observations.json'), JSON.stringify(observations, null, 2));
        }
    },
);
