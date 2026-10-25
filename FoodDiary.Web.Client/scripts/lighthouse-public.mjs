import { createHash } from 'node:crypto';

export const METRICS = {
    performance: { label: 'Performance', score: true },
    accessibility: { label: 'Accessibility', score: true },
    bestPractices: { label: 'Best practices', score: true },
    seo: { label: 'SEO', score: true },
    fcp: { label: 'FCP', unit: 'ms' },
    lcp: { label: 'LCP', unit: 'ms' },
    tbt: { label: 'TBT', unit: 'ms' },
    cls: { label: 'CLS', unit: '' },
    bytes: { label: 'Transfer', unit: 'KiB' },
};
const requiredMetrics = ['performance', 'fcp', 'lcp', 'tbt', 'cls', 'bytes'];
const switches = new Set(['smoke', 'dry-run', 'check', 'save-baseline', 'help']);
const values = new Set([
    'routes',
    'devices',
    'runs',
    'config',
    'reports-dir',
    'static-root',
    'base-url',
    'api-origin',
    'summary',
    'baseline',
    'transport',
]);

export function parseArgs(args) {
    const options = {};
    for (let i = 0; i < args.length; i++) {
        const match = /^--([a-z-]+)(?:=(.*))?$/.exec(args[i]);
        if (!match || (!switches.has(match[1]) && !values.has(match[1]))) throw new Error(`Unknown argument: ${args[i]}`);
        const [, key, inline] = match;
        if (key in options) throw new Error(`Duplicate option: --${key}`);
        if (switches.has(key)) {
            if (inline !== undefined) throw new Error(`--${key} does not accept a value.`);
            options[key] = true;
        } else {
            const value = inline ?? args[++i];
            if (!value || value.startsWith('--')) throw new Error(`--${key} requires a value.`);
            options[key] = value;
        }
    }
    if (options.check && options['save-baseline']) throw new Error('Choose either --check or --save-baseline.');
    if (options.smoke && options.routes) throw new Error('Choose either --smoke or --routes.');
    if (options.summary && !options.check && !options['save-baseline'])
        throw new Error('--summary selects a saved report for check/baseline modes only.');
    if (options['base-url'] && options['api-origin']) throw new Error('--api-origin is only available for local static audits.');
    if (
        (options.check || options['save-baseline']) &&
        ['smoke', 'routes', 'devices', 'runs', 'base-url', 'api-origin', 'static-root', 'dry-run', 'transport'].some(key => key in options)
    ) {
        throw new Error('Check and baseline modes use the saved summary; collection options do not apply.');
    }
    return options;
}

export function assertLocalHttp2(lhr, origin) {
    const requests = lhr.audits?.['network-requests']?.details?.items?.filter(item => item.url?.startsWith(`${origin}/`)) ?? [];
    if (!requests.length || requests.some(item => item.protocol !== 'h2'))
        throw new Error('Local HTTP/2 measurement has missing or non-h2 network evidence.');
}

export function normalizeRoute(route) {
    if (!/^\/(?:[a-zA-Z0-9_-]+(?:\/[a-zA-Z0-9_-]+)*\/?)?$/.test(route)) throw new Error(`Invalid public route: ${route}`);
    return route === '/' ? route : route.replace(/\/$/, '');
}

export function readRoutes(text) {
    const routes = text
        .split(/\r?\n/)
        .map(line => line.trim())
        .filter(line => line && !line.startsWith('#'))
        .map(normalizeRoute);
    if (!routes.length || new Set(routes).size !== routes.length) throw new Error('Route inventory must be nonempty and unique.');
    return routes;
}

export function validateConfig(config) {
    if (!config.routesFile || !Array.isArray(config.smokeRoutes)) throw new Error('Config must define routesFile and smokeRoutes.');
    for (const [device, budgets] of Object.entries(config.budgets ?? {})) {
        if (!['mobile', 'desktop'].includes(device)) throw new Error(`Unknown budget device: ${device}`);
        for (const [metric, value] of Object.entries(budgets)) {
            if (!Object.hasOwn(METRICS, metric) || !validMetric(metric, value)) throw new Error(`Invalid ${device} budget: ${metric}`);
        }
    }
    for (const [metric, tolerance] of Object.entries(config.regressions ?? {})) {
        if (
            !Object.hasOwn(METRICS, metric) ||
            !tolerance ||
            ![tolerance.absolute, tolerance.relative].every(value => Number.isFinite(value) && value >= 0)
        ) {
            throw new Error(`Invalid regression tolerance: ${metric}`);
        }
    }
    return config;
}

export function collectionPlan(config, options, inventory) {
    const routes = options.routes
        ? readRoutes(options.routes.split(',').join('\n'))
        : options.smoke
          ? config.smokeRoutes.map(normalizeRoute)
          : inventory;
    const devices = options.devices ? options.devices.split(',') : config.devices;
    const runs = Number(options.runs ?? config.runs);
    if (!Number.isInteger(runs) || runs < 1 || runs > 20) throw new Error('Runs must be an integer from 1 to 20.');
    if (
        !Array.isArray(devices) ||
        !devices.length ||
        new Set(devices).size !== devices.length ||
        devices.some(device => !['mobile', 'desktop'].includes(device))
    ) {
        throw new Error('Devices must be mobile, desktop, or mobile,desktop.');
    }
    if (
        !Array.isArray(routes) ||
        !routes.length ||
        new Set(routes).size !== routes.length ||
        routes.some(route => normalizeRoute(route) !== route || !inventory.includes(route))
    ) {
        throw new Error('Selected routes must be unique members of the prerender inventory.');
    }
    return { routes, devices, runs };
}

export function validateOrigin(value) {
    const url = new URL(value);
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password || url.pathname !== '/' || url.search || url.hash) {
        throw new Error('Target must be an HTTP(S) origin without credentials, path, query or fragment.');
    }
    return url.origin;
}

export function reportName(route, device, run) {
    const slug = (route === '/' ? 'index' : route.slice(1).replaceAll('/', '-')).slice(0, 80);
    return `${slug}-${createHash('sha256').update(route).digest('hex').slice(0, 8)}-${device}-${run}`;
}

function validMetric(metric, value) {
    return Number.isFinite(value) && value >= 0 && (!METRICS[metric]?.score || value <= 100);
}

export function sampleFromLhr(lhr, route, device, run, report) {
    if (lhr.runtimeError) throw new Error(`${lhr.runtimeError.code}: ${lhr.runtimeError.message}`);
    if (normalizeRoute(new URL(lhr.finalDisplayedUrl ?? lhr.finalUrl).pathname) !== route)
        throw new Error(`Unexpected final URL: ${lhr.finalDisplayedUrl ?? lhr.finalUrl}`);
    if (lhr.configSettings.formFactor !== device) throw new Error('Lighthouse used an unexpected device profile.');
    const score = id => (lhr.categories[id]?.score == null ? null : lhr.categories[id].score * 100);
    const metrics = {
        performance: score('performance'),
        accessibility: score('accessibility'),
        bestPractices: score('best-practices'),
        seo: score('seo'),
        fcp: lhr.audits['first-contentful-paint']?.numericValue,
        lcp: lhr.audits['largest-contentful-paint']?.numericValue,
        tbt: lhr.audits['total-blocking-time']?.numericValue,
        cls: lhr.audits['cumulative-layout-shift']?.numericValue,
        bytes: lhr.audits['total-byte-weight']?.numericValue,
    };
    for (const metric of requiredMetrics)
        if (!validMetric(metric, metrics[metric])) throw new Error(`Missing or invalid metric: ${metric}`);
    return { route, device, run, metrics, report, warnings: lhr.runWarnings ?? [] };
}

export function median(values) {
    if (!values.length || values.some(value => !Number.isFinite(value))) throw new Error('Median requires finite samples.');
    const sorted = [...values].sort((a, b) => a - b);
    const middle = Math.floor(sorted.length / 2);
    return sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
}

export function aggregate(plan, samples, context, createdAt = new Date().toISOString()) {
    const seen = new Set();
    for (const sample of samples) {
        const key = JSON.stringify([sample.route, sample.device, sample.run]);
        if (
            seen.has(key) ||
            !plan.routes.includes(sample.route) ||
            !plan.devices.includes(sample.device) ||
            !Number.isInteger(sample.run) ||
            sample.run < 1 ||
            sample.run > plan.runs
        ) {
            throw new Error('Duplicate or unexpected audit sample.');
        }
        seen.add(key);
    }
    const cases = plan.routes.flatMap(route =>
        plan.devices.map(device => {
            const selected = samples.filter(sample => sample.route === route && sample.device === device);
            const valid = selected.filter(
                sample => !sample.error && requiredMetrics.every(metric => validMetric(metric, sample.metrics?.[metric])),
            );
            const metrics = {};
            for (const metric of Object.keys(METRICS)) {
                const values = valid.map(sample => sample.metrics[metric]).filter(value => validMetric(metric, value));
                metrics[metric] =
                    values.length === plan.runs ? { median: median(values), min: Math.min(...values), max: Math.max(...values) } : null;
            }
            return { route, device, validRuns: valid.length, metrics, samples: selected };
        }),
    );
    return { schemaVersion: 1, createdAt, context, plan, complete: cases.every(row => row.validRuns === plan.runs), cases };
}

export function validateSummary(summary, minimumRuns = 1) {
    if (summary?.schemaVersion !== 1 || !summary.complete || !summary.context || !summary.plan || !Array.isArray(summary.cases))
        throw new Error('Summary is missing, invalid or incomplete.');
    const { plan, context } = summary;
    collectionPlan({ ...plan, smokeRoutes: [] }, {}, plan.routes);
    if (plan.runs < minimumRuns) throw new Error(`At least ${minimumRuns} runs per case are required.`);
    for (const key of ['lighthouseVersion', 'chromeMajor', 'platform', 'arch', 'cpu', 'memoryGiB', 'target', 'profiles']) {
        if (context[key] == null) throw new Error(`Missing comparison context: ${key}`);
    }
    const keys = new Set();
    for (const row of summary.cases) {
        const key = JSON.stringify([row.route, row.device]);
        if (keys.has(key) || !plan.routes.includes(row.route) || !plan.devices.includes(row.device) || row.validRuns !== plan.runs)
            throw new Error('Summary has duplicate, unexpected or incomplete cases.');
        keys.add(key);
        for (const metric of requiredMetrics) {
            const stats = row.metrics?.[metric];
            if (
                !stats ||
                ![stats.min, stats.median, stats.max].every(value => validMetric(metric, value)) ||
                stats.min > stats.median ||
                stats.median > stats.max
            )
                throw new Error(`Invalid summary metric: ${metric}`);
        }
        if (!context.profiles[row.device]) throw new Error(`Missing device profile: ${row.device}`);
    }
    if (keys.size !== plan.routes.length * plan.devices.length) throw new Error('Summary does not cover its entire plan.');
    if (summary.cases.some(row => row.samples !== undefined)) {
        if (summary.cases.some(row => !Array.isArray(row.samples))) throw new Error('Summary has inconsistent sample evidence.');
        const recomputed = aggregate(
            plan,
            summary.cases.flatMap(row => row.samples),
            context,
        );
        if (
            !recomputed.complete ||
            recomputed.cases.some((row, index) => JSON.stringify(row.metrics) !== JSON.stringify(summary.cases[index].metrics))
        ) {
            throw new Error('Summary metrics do not match their audit samples.');
        }
    }
    return summary;
}

export function checkSummary(summary, config, baseline) {
    const failures = [];
    try {
        validateConfig(config);
        validateSummary(summary, baseline ? 3 : 1);
    } catch (error) {
        return { passed: false, failures: [{ message: error.message }] };
    }
    if (baseline) {
        try {
            validateSummary(baseline, 3);
            for (const key of ['lighthouseVersion', 'chromeMajor', 'platform', 'arch', 'cpu', 'memoryGiB', 'target']) {
                if (JSON.stringify(summary.context[key]) !== JSON.stringify(baseline.context[key]))
                    throw new Error(`Baseline context differs: ${key}. Collect a comparable run or review a new baseline.`);
            }
            for (const device of summary.plan.devices) {
                if (JSON.stringify(summary.context.profiles[device]) !== JSON.stringify(baseline.context.profiles[device]))
                    throw new Error(`Baseline profile differs: ${device}.`);
            }
        } catch (error) {
            return { passed: false, failures: [{ message: error.message }] };
        }
    }
    for (const row of summary.cases) {
        const fail = message => failures.push({ route: row.route, device: row.device, message });
        for (const [metric, limit] of Object.entries(config.budgets?.[row.device] ?? {})) {
            const value = row.metrics[metric]?.median;
            if (!validMetric(metric, value)) fail(`${METRICS[metric].label}: metric missing.`);
            else if (METRICS[metric].score ? value < limit : value > limit)
                fail(
                    `${METRICS[metric].label}: ${formatMetric(metric, value)} ${METRICS[metric].score ? 'falls below minimum' : 'exceeds maximum'} ${formatMetric(metric, limit)}.`,
                );
        }
        if (!baseline) continue;
        const previous = baseline.cases.find(item => item.route === row.route && item.device === row.device);
        if (!previous) {
            fail('No baseline for this route/device. Review and save an expanded baseline.');
            continue;
        }
        for (const [metric, tolerance] of Object.entries(config.regressions ?? {})) {
            const current = row.metrics[metric]?.median;
            const before = previous.metrics[metric]?.median;
            if (!validMetric(metric, current) || !validMetric(metric, before)) {
                fail(`${METRICS[metric].label}: missing comparison metric.`);
                continue;
            }
            const delta = METRICS[metric].score ? before - current : current - before;
            const allowed = Math.max(tolerance.absolute, before * tolerance.relative);
            if (delta > allowed + Number.EPSILON * 100)
                fail(
                    `${METRICS[metric].label}: ${formatMetric(metric, before)} → ${formatMetric(metric, current)}; allowed change ${formatMetric(metric, allowed)}.`,
                );
        }
    }
    return { passed: failures.length === 0, failures };
}

export function makeBaseline(summary) {
    validateSummary(summary, 3);
    const { schemaVersion, createdAt, context, plan, complete } = summary;
    return {
        schemaVersion,
        createdAt,
        context,
        plan,
        complete,
        cases: summary.cases.map(({ samples, ...row }) => row),
        baselineSavedAt: new Date().toISOString(),
    };
}

export function formatMetric(metric, value) {
    if (!Number.isFinite(value)) return '—';
    if (metric === 'bytes') return `${(value / 1024).toFixed(1)} KiB`;
    if (metric === 'cls') return value.toFixed(3);
    return `${Math.round(value)}${METRICS[metric].unit === 'ms' ? ' ms' : ''}`;
}

const escapeHtml = value =>
    String(value).replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);

export function renderSummary(summary, check, reportPrefix = '') {
    const headings = Object.values(METRICS)
        .map(metric => `<th scope="col">${metric.label}</th>`)
        .join('');
    const rows = summary.cases
        .map(row => {
            const failures = check.failures.filter(failure => failure.route === row.route && failure.device === row.device);
            const reports = (row.samples ?? [])
                .map(sample => {
                    const safeLink =
                        typeof sample.report === 'string' &&
                        /^runs\/[a-zA-Z0-9_./-]+\.html$/.test(sample.report) &&
                        !sample.report.split('/').includes('..');
                    return safeLink
                        ? `<a href="${escapeHtml(reportPrefix + sample.report)}">#${escapeHtml(sample.run)}</a>`
                        : `#${escapeHtml(sample.run)}`;
                })
                .join(' ');
            const cells = Object.keys(METRICS)
                .map(metric => {
                    const stats = row.metrics[metric];
                    return `<td>${formatMetric(metric, stats?.median)}${stats ? `<small>${formatMetric(metric, stats.min)} – ${formatMetric(metric, stats.max)}</small>` : ''}</td>`;
                })
                .join('');
            const errors = (row.samples ?? []).filter(sample => sample.error).map(sample => sample.error);
            if (row.validRuns !== summary.plan.runs) errors.push('Incomplete measurements');
            return `<tr><th scope="row">${escapeHtml(row.route)}</th><td>${escapeHtml(row.device)}</td>${cells}<td>${escapeHtml(row.validRuns)}/${escapeHtml(summary.plan.runs)}<small>${reports}</small></td><td>${[...failures.map(failure => failure.message), ...errors].map(escapeHtml).join('<br>') || 'OK'}</td></tr>`;
        })
        .join('');
    return `<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Public Lighthouse report</title><style>
body{font:15px/1.5 system-ui,sans-serif;margin:24px;color:#17202a;background:#fafafa}h1{font-size:24px}table{border-collapse:collapse;background:white;width:100%;font-size:13px}th,td{border:1px solid #d5d8dc;padding:8px;text-align:left;vertical-align:top;white-space:nowrap}thead{background:#eaf0f5}small{display:block;color:#566573;font-size:11px}td:last-child{white-space:normal;min-width:180px}.scroll{overflow:auto}pre{white-space:pre-wrap;overflow-wrap:anywhere}a{color:#075ca0}
</style><h1>Public Lighthouse report</h1><p>${escapeHtml(summary.createdAt)} · ${summary.cases.length} cases · ${summary.plan.runs} cold runs per case · ${check.passed ? 'Checks passed' : 'Checks failed'}</p>
<p>Values are medians; smaller text shows min–max. Scores use 0–100. Transfer is Lighthouse total byte weight. A one-run report is exploratory.</p>
${summary.context.target?.mode?.startsWith('local-gzip') && !summary.context.target.apiOrigin ? '<p>Static hosting audit without an API proxy: API requests fail (404/405). API-dependent startup behavior requires a separate run with a real backend.</p>' : ''}
${check.failures
    .filter(failure => !failure.route)
    .map(failure => `<p>${escapeHtml(failure.message)}</p>`)
    .join('')}
<div class="scroll"><table><thead><tr><th scope="col">Route</th><th scope="col">Device</th>${headings}<th scope="col">Runs / reports</th><th scope="col">Checks / errors</th></tr></thead><tbody>${rows}</tbody></table></div>
<details><summary>Measurement context</summary><pre>${escapeHtml(JSON.stringify(summary.context, null, 2))}</pre></details></html>`;
}
