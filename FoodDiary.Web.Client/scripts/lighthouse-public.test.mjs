import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createServer, request } from 'node:http';
import { connect } from 'node:http2';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { gunzipSync } from 'node:zlib';

import {
    aggregate,
    assertLocalHttp2,
    checkSummary,
    collectionPlan,
    makeBaseline,
    median,
    parseArgs,
    readRoutes,
    renderSummary,
    reportName,
    sampleFromLhr,
    validateConfig,
    validateOrigin,
    validateSummary,
} from './lighthouse-public.mjs';
import { prerenderFile, startStaticServer } from './lighthouse-public-server.mjs';
import { createAuditProfile, removeAuditProfile } from './lighthouse-public-chrome.mjs';

const workspace = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const config = JSON.parse(readFileSync(path.join(workspace, 'lighthouse.public.config.json'), 'utf8'));
const context = {
    lighthouseVersion: '13.5.0',
    chromeMajor: '140',
    platform: 'win32',
    arch: 'x64',
    cpu: 'test-cpu',
    memoryGiB: 16,
    target: { mode: 'local-gzip', origin: null, apiOrigin: null },
    profiles: { mobile: { formFactor: 'mobile', viewport: 412 }, desktop: { formFactor: 'desktop', viewport: 1350 } },
};
const baseMetrics = {
    performance: 95,
    accessibility: 100,
    bestPractices: 100,
    seo: 100,
    fcp: 1500,
    lcp: 2000,
    tbt: 100,
    cls: 0.02,
    bytes: 500000,
};
function summary(overrides = {}, runs = 3, routes = ['/'], devices = ['mobile']) {
    const plan = { routes, devices, runs };
    return aggregate(
        plan,
        routes.flatMap(route =>
            devices.flatMap(device =>
                Array.from({ length: runs }, (_, index) => ({
                    route,
                    device,
                    run: index + 1,
                    metrics: { ...baseMetrics, ...overrides },
                    report: `runs/test/index-${device}-${index + 1}.html`,
                })),
            ),
        ),
        structuredClone(context),
    );
}
function temporary(t) {
    const directory = mkdtempSync(path.join(tmpdir(), 'fd-lighthouse-test-'));
    t.after(() => rmSync(directory, { recursive: true, force: true }));
    return directory;
}
function rawGet(origin, urlPath, headers = {}, method = 'GET') {
    return new Promise((resolve, reject) => {
        const outgoing = request(origin, { path: urlPath, headers, method }, response => {
            const parts = [];
            response.on('data', part => parts.push(part));
            response.on('end', () => resolve({ status: response.statusCode, headers: response.headers, body: Buffer.concat(parts) }));
        });
        outgoing.on('error', reject);
        outgoing.end();
    });
}

test('inventory drives full and smoke plans including both languages and future routes', () => {
    const routes = readRoutes(readFileSync(path.join(workspace, config.routesFile), 'utf8'));
    for (const route of ['/', '/ru', '/pricing', '/ru/pricing']) assert.ok(routes.includes(route));
    const full = collectionPlan(config, {}, [...routes, '/new-public-page']);
    assert.equal(full.routes.length * full.devices.length * full.runs, (routes.length + 1) * 6);
    assert.deepEqual(collectionPlan(config, { smoke: true }, routes).routes, config.smokeRoutes);
    assert.deepEqual(collectionPlan(config, parseArgs(['--routes=/,/ru', '--devices', 'mobile', '--runs=1']), routes), {
        routes: ['/', '/ru'],
        devices: ['mobile'],
        runs: 1,
    });
});

test('invalid inventories and CLI selections fail before Chrome starts', () => {
    for (const input of ['', '/\n/', '/a/../b', '/a?x=1', '/%2e%2e', '/a\\b', '//', '//foreign']) assert.throws(() => readRoutes(input));
    assert.deepEqual(readRoutes('# comment\r\n/\r\n/ru/\r\n'), ['/', '/ru']);
    for (const args of [
        ['--unknown'],
        ['--runs'],
        ['--runs='],
        ['--runs=3', '--runs=4'],
        ['--smoke=true'],
        ['--smoke', '--routes=/'],
        ['--check', '--runs=3'],
        ['--check', '--save-baseline'],
        ['--base-url=http://site.test', '--api-origin=http://api.test'],
        ['--summary=foo'],
    ])
        assert.throws(() => parseArgs(args));
    for (const overrides of [{ runs: '0' }, { runs: '1.5' }, { devices: 'tablet' }, { devices: 'mobile,mobile' }, { routes: '/missing' }])
        assert.throws(() => collectionPlan(config, overrides, ['/']));
});

test('origins disallow embedded credentials and paths', () => {
    assert.equal(validateOrigin('https://fooddiary.club/'), 'https://fooddiary.club');
    for (const value of [
        'file:///foo',
        'https://user:password@site.test',
        'https://site.test/ru',
        'https://site.test/?token=123',
        'https://site.test/#x',
    ])
        assert.throws(() => validateOrigin(value));
});

test('Chrome cleanup only removes a dedicated temporary profile and handles Windows locks', t => {
    const profile = createAuditProfile();
    t.after(() => removeAuditProfile(profile));
    assert.equal(
        removeAuditProfile(profile, () => {
            throw Object.assign(new Error('Locked'), { code: 'EPERM' });
        }),
        false,
    );
    assert.throws(() =>
        removeAuditProfile(profile, () => {
            throw Object.assign(new Error('Unexpected'), { code: 'EIO' });
        }),
    );
    assert.throws(() => removeAuditProfile(workspace));
    assert.equal(removeAuditProfile(profile), true);
    assert.equal(removeAuditProfile(profile), true);
});

test('config rejects unknown metrics and negative tolerances', () => {
    assert.equal(validateConfig(config), config);
    assert.throws(() => validateConfig({ ...config, budgets: { mobile: { performance: 101 } } }));
    assert.throws(() => validateConfig({ ...config, budgets: { mobile: { unknown: 5 } } }));
    assert.throws(() => validateConfig({ ...config, regressions: { lcp: { absolute: -1, relative: 0.1 } } }));
});

test('medians include every observation and ranges expose noise', () => {
    assert.equal(median([3000, 1000, 2000]), 2000);
    assert.equal(median([4, 1, 3, 2]), 2.5);
    assert.throws(() => median([]));
    assert.throws(() => median([null]));
    const input = summary();
    input.cases[0].samples[0].metrics.lcp = 6000;
    const output = aggregate(input.plan, input.cases[0].samples, context);
    assert.deepEqual(output.cases[0].metrics.lcp, { median: 2000, min: 2000, max: 6000 });
});

test('failed, missing and duplicate runs cannot become a passing median', () => {
    const input = summary();
    const samples = input.cases[0].samples;
    assert.throws(() => aggregate(input.plan, [...samples, samples[0]], context));
    for (const altered of [
        samples.slice(1),
        samples.map((item, index) => (index ? item : { ...item, error: 'timeout' })),
        samples.map((item, index) => (index ? item : { ...item, metrics: { ...baseMetrics, lcp: null } })),
    ]) {
        const output = aggregate(input.plan, altered, context);
        assert.equal(output.complete, false);
        assert.equal(checkSummary(output, config).passed, false);
        assert.throws(() => makeBaseline(output));
    }
});

test('saved summaries fail closed for missing cases, metrics and tampered aggregates', () => {
    for (const mutate of [
        value => value.cases.pop(),
        value => (value.cases[0].metrics.lcp.median = null),
        value => value.cases.push(value.cases[0]),
        value => (value.cases[0].metrics.lcp.median = 2200),
        value => (value.context.chromeMajor = null),
    ]) {
        const value = summary();
        mutate(value);
        assert.equal(checkSummary(value, config).passed, false);
    }
});

test('budgets use correct score direction and device-specific limits', () => {
    assert.equal(checkSummary(summary(), config).passed, true);
    for (const metrics of [{ performance: 84 }, { lcp: 3501 }, { tbt: 301 }, { cls: 0.101 }])
        assert.equal(checkSummary(summary(metrics), config).passed, false);
    assert.equal(checkSummary(summary({ performance: 89 }, 3, ['/'], ['desktop']), config).passed, false);
    assert.equal(checkSummary(summary({ lcp: 3500, performance: 85, tbt: 300, cls: 0.1 }), config).passed, true);
});

test('HTTP/2 evidence and comparison context cannot silently inherit HTTP/1 results', () => {
    const origin = 'https://127.0.0.1:1234';
    const report = protocol => ({ audits: { 'network-requests': { details: { items: [{ url: `${origin}/`, protocol }] } } } });
    assert.doesNotThrow(() => assertLocalHttp2(report('h2'), origin));
    assert.throws(() => assertLocalHttp2(report('http/1.1'), origin), /non-h2/u);
    assert.throws(() => assertLocalHttp2({}, origin), /missing/u);
    const changed = summary();
    changed.context.target.mode = 'local-gzip-http2';
    assert.match(checkSummary(changed, config, makeBaseline(summary())).failures[0].message, /context differs: target/u);
});

test('baseline tolerances use larger absolute or relative allowance', () => {
    const baseline = makeBaseline(summary());
    assert.equal(
        checkSummary(summary({ performance: 90, lcp: 2300, fcp: 1725, tbt: 175, cls: 0.05, bytes: 551200 }), config, baseline).passed,
        true,
    );
    for (const metrics of [{ performance: 89 }, { lcp: 2301 }, { fcp: 1726 }, { tbt: 176 }, { cls: 0.0501 }, { bytes: 551201 }]) {
        const result = checkSummary(summary(metrics), config, baseline);
        assert.equal(result.passed, false, JSON.stringify(metrics));
        assert.match(result.failures[0].message, /allowed change/);
    }
    assert.equal(checkSummary(summary({ lcp: 3000 }), config, makeBaseline(summary({ lcp: 2700 }))).passed, true);
    assert.equal(checkSummary(summary({ tbt: 75 }), config, makeBaseline(summary({ tbt: 0 }))).passed, true);
    assert.equal(checkSummary(summary({ tbt: 76 }), config, makeBaseline(summary({ tbt: 0 }))).passed, false);
});

test('improvements pass and baseline subsets require corresponding route/device evidence', () => {
    assert.equal(checkSummary(summary({ performance: 100, lcp: 1600, tbt: 0 }), config, makeBaseline(summary())).passed, true);
    assert.equal(checkSummary(summary(), config, makeBaseline(summary({}, 3, ['/', '/ru'], ['mobile', 'desktop']))).passed, true);
    const missing = checkSummary(summary({}, 3, ['/', '/ru']), config, makeBaseline(summary()));
    assert.equal(missing.passed, false);
    assert.match(missing.failures[0].message, /No baseline/);
});

test('baseline checks require repeat runs and matching environment and official profile settings', () => {
    assert.throws(() => makeBaseline(summary({}, 1)));
    assert.equal(checkSummary(summary({}, 1), config, makeBaseline(summary())).passed, false);
    for (const key of ['lighthouseVersion', 'chromeMajor', 'cpu', 'memoryGiB', 'target', 'profiles']) {
        const baseline = makeBaseline(summary());
        baseline.context[key] = key === 'profiles' ? { mobile: { formFactor: 'mobile', viewport: 100 } } : 'different';
        assert.equal(checkSummary(summary(), config, baseline).passed, false, key);
    }
    const baseline = makeBaseline(summary());
    assert.equal(baseline.cases[0].samples, undefined);
    validateSummary(baseline, 3);
});

test('Lighthouse runtime errors, wrong redirects and missing critical audits are rejected', () => {
    const lhr = {
        finalDisplayedUrl: 'http://localhost/ru/',
        configSettings: { formFactor: 'mobile' },
        categories: { performance: { score: 0.95 } },
        audits: {
            'first-contentful-paint': { numericValue: 1500 },
            'largest-contentful-paint': { numericValue: 2000 },
            'total-blocking-time': { numericValue: 100 },
            'cumulative-layout-shift': { numericValue: 0.02 },
            'total-byte-weight': { numericValue: 500000 },
        },
    };
    assert.equal(sampleFromLhr(lhr, '/ru', 'mobile', 1, 'report.html').metrics.performance, 95);
    assert.throws(() => sampleFromLhr({ ...lhr, runtimeError: { code: 'NO_FCP', message: 'failed' } }, '/ru', 'mobile', 1));
    assert.throws(() => sampleFromLhr(lhr, '/', 'mobile', 1));
    assert.throws(() => sampleFromLhr(lhr, '/ru', 'desktop', 1));
    assert.throws(() => sampleFromLhr({ ...lhr, audits: {} }, '/ru', 'mobile', 1));
});

test('report names avoid route collisions; HTML escapes external text and rejects unsafe links', () => {
    assert.notEqual(reportName('/a/b', 'mobile', 1), reportName('/a-b', 'mobile', 1));
    const value = summary();
    value.cases[0].samples[0].error = '<script>alert(1)</script>';
    value.cases[0].samples[0].report = 'javascript:alert(1)';
    value.context.cpu = '</pre><script>alert(2)</script>';
    const html = renderSummary(value, { passed: false, failures: [] });
    assert.ok(!html.includes('<script>') && !html.includes('javascript:'));
    assert.ok(html.includes('&lt;script&gt;') && html.includes('href="runs/test/index-mobile-2.html"'));
});

test('static server serves actual deep prerenders with gzip, HEAD and no SPA fallback', async t => {
    const root = temporary(t);
    mkdirSync(path.join(root, 'ru', 'pricing'), { recursive: true });
    writeFileSync(path.join(root, 'index.html'), '<h1>English</h1>');
    writeFileSync(path.join(root, 'ru', 'pricing', 'index.html'), '<h1>Тарифы</h1>');
    writeFileSync(path.join(root, 'app.js'), 'export const value = 1;');
    assert.equal(prerenderFile(root, '/ru/pricing'), path.join(root, 'ru', 'pricing', 'index.html'));
    assert.throws(() => prerenderFile(root, '/missing'));
    const server = await startStaticServer(root);
    t.after(() => server.close());
    const response = await rawGet(server.origin, '/ru/pricing', { 'accept-encoding': 'gzip' });
    assert.equal(response.status, 200);
    assert.equal(response.headers['content-encoding'], 'gzip');
    assert.equal(gunzipSync(response.body).toString(), '<h1>Тарифы</h1>');
    assert.equal(response.headers['cache-control'], 'no-store');
    assert.equal((await rawGet(server.origin, '/app.js')).body.toString(), 'export const value = 1;');
    assert.equal((await rawGet(server.origin, '/ru/pricing', {}, 'HEAD')).body.length, 0);
    assert.equal((await rawGet(server.origin, '/missing')).status, 404);
    assert.equal((await rawGet(server.origin, '/missing.js')).status, 404);
    assert.equal((await rawGet(server.origin, '/%2e%2e/secret')).status, 400);
    assert.equal((await rawGet(server.origin, '/%5csecret')).status, 400);
    assert.equal((await rawGet(server.origin, '/app.js', {}, 'POST')).status, 405);
});

test('explicit API proxy preserves real status/body/query and removes credentials', async t => {
    const root = temporary(t);
    writeFileSync(path.join(root, 'index.html'), 'public');
    const api = createServer((request, response) => {
        assert.equal(request.url, '/api/example?limit=2&literal=a%26b');
        assert.equal(request.headers.authorization, undefined);
        assert.equal(request.headers.cookie, undefined);
        response.writeHead(422, { 'content-type': 'application/json', 'set-cookie': 'secret=test' });
        response.end('{"actual":"upstream"}');
    });
    await new Promise(resolve => api.listen(0, '127.0.0.1', resolve));
    t.after(() => new Promise(resolve => api.close(resolve)));
    const server = await startStaticServer(root, `http://127.0.0.1:${api.address().port}`);
    t.after(() => server.close());
    const response = await rawGet(server.origin, '/api/example?limit=2&literal=a%26b', {
        authorization: 'Bearer fake',
        cookie: 'fake=test',
    });
    assert.equal(response.status, 422);
    assert.equal(response.body.toString(), '{"actual":"upstream"}');
    assert.equal(response.headers['set-cookie'], undefined);
});

test('HTTP/2 hosting negotiates h2 with an ephemeral loopback certificate and preserves safe API forwarding', async t => {
    const root = temporary(t);
    writeFileSync(path.join(root, 'index.html'), '<h1>HTTP/2 document</h1>');
    const api = createServer((request, response) => {
        assert.equal(request.url, '/api/example?limit=2');
        assert.equal(request.headers.authorization, undefined);
        assert.equal(request.headers.cookie, undefined);
        response.writeHead(422, { 'content-type': 'application/json', 'set-cookie': 'private=test' });
        response.end('{"actual":"upstream"}');
    });
    await new Promise(resolve => api.listen(0, '127.0.0.1', resolve));
    t.after(() => new Promise(resolve => api.close(resolve)));
    const server = await startStaticServer(root, `http://127.0.0.1:${api.address().port}`, { transport: 'http2' });
    t.after(() => server.close());
    assert.match(server.certificateSPKI, /^[A-Za-z0-9+/]{43}=$/u);
    const get = (pathname, headers = {}) =>
        new Promise((resolve, reject) => {
            const session = connect(server.origin, { ca: server.certificate });
            session.on('error', reject);
            const stream = session.request({ ':path': pathname, ...headers });
            const chunks = [];
            let responseHeaders;
            stream.on('response', headers => {
                responseHeaders = headers;
            });
            stream.on('data', chunk => chunks.push(chunk));
            stream.on('error', error => {
                session.destroy();
                reject(error);
            });
            stream.on('end', () => {
                const protocol = session.alpnProtocol;
                session.close();
                resolve({ headers: responseHeaders, body: Buffer.concat(chunks), protocol });
            });
        });
    const document = await get('/', { 'accept-encoding': 'gzip' });
    assert.equal(document.protocol, 'h2');
    assert.equal(document.headers[':status'], 200);
    assert.equal(gunzipSync(document.body).toString(), '<h1>HTTP/2 document</h1>');
    const forwarded = await get('/api/example?limit=2', { authorization: 'Bearer fake', cookie: 'fake=test' });
    assert.equal(forwarded.headers[':status'], 422);
    assert.equal(forwarded.headers['set-cookie'], undefined);
    assert.equal(forwarded.body.toString(), '{"actual":"upstream"}');
});

test('CLI saves a reviewed baseline, passes same results, rejects regression and missing baseline', t => {
    const root = temporary(t);
    const reports = path.join(root, 'reports');
    mkdirSync(reports);
    const summaryFile = path.join(reports, 'summary.json');
    const baselineFile = path.join(root, 'baseline.json');
    const command = ['scripts/run-lighthouse.mjs', `--reports-dir=${reports}`, `--baseline=${baselineFile}`];
    writeFileSync(summaryFile, JSON.stringify(summary()));
    assert.throws(
        () => execFileSync(process.execPath, [...command, '--check'], { cwd: workspace, stdio: 'pipe' }),
        error => error.status === 1 && error.stderr.toString().includes('Baseline missing'),
    );
    execFileSync(process.execPath, [...command, '--save-baseline'], { cwd: workspace });
    const output = execFileSync(process.execPath, [...command, '--check'], { cwd: workspace }).toString();
    assert.match(output, /PASS: 1 cases/);
    writeFileSync(summaryFile, JSON.stringify(summary({ lcp: 2400 })));
    assert.throws(
        () => execFileSync(process.execPath, [...command, '--check'], { cwd: workspace, stdio: 'pipe' }),
        error => error.status === 1 && error.stderr.toString().includes('allowed change'),
    );
    assert.equal(JSON.parse(readFileSync(baselineFile)).cases[0].metrics.lcp.median, 2000);
    writeFileSync(summaryFile, JSON.stringify(summary({}, 1)));
    assert.throws(
        () => execFileSync(process.execPath, [...command, '--save-baseline'], { cwd: workspace, stdio: 'pipe' }),
        error => error.status === 1,
    );
});
