import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { cpus, platform, arch, totalmem } from 'node:os';
import path from 'node:path';

import { launch } from 'chrome-launcher';
import lighthouse from 'lighthouse';
import desktopConfig from 'lighthouse/core/config/desktop-config.js';

import { createAuditProfile, removeAuditProfile } from './lighthouse-public-chrome.mjs';
import {
    aggregate,
    checkSummary,
    collectionPlan,
    formatMetric,
    makeBaseline,
    parseArgs,
    readRoutes,
    renderSummary,
    reportName,
    sampleFromLhr,
    validateConfig,
    validateOrigin,
} from './lighthouse-public.mjs';
import { prerenderFile, startStaticServer } from './lighthouse-public-server.mjs';

const readJson = file => JSON.parse(readFileSync(file, 'utf8'));
const writeJson = (file, value) => writeFileSync(file, `${JSON.stringify(value, null, 2)}\n`);

async function main() {
    const options = parseArgs(process.argv.slice(2));
    if (options.help) {
        console.log(`Public Lighthouse: all prerendered routes, mobile + desktop, three cold runs.
  npm run audit:lighthouse                     Build production and collect all routes
  npm run audit:lighthouse:smoke               Collect four representative routes
  npm run audit:lighthouse:collect -- --routes=/,/ru --devices=mobile --runs=3
  npm run audit:lighthouse:check               Check budgets and saved baseline
  npm run audit:lighthouse:baseline            Save reviewed summary as baseline
Options: --dry-run, --config=<file>, --reports-dir=<dir>, --static-root=<dir>,
  --base-url=<origin>, --api-origin=<origin>, --summary=<file>, --baseline=<file>.
See ../docs/frontend/PUBLIC_LIGHTHOUSE.md. Check/baseline modes do not launch Chrome.`);
        return;
    }
    const root = process.cwd();
    const config = validateConfig(readJson(path.resolve(root, options.config ?? 'lighthouse.public.config.json')));
    const reportsRoot = path.resolve(root, options['reports-dir'] ?? 'reports/lighthouse');
    const summaryFile = path.resolve(root, options.summary ?? path.join(reportsRoot, 'summary.json'));
    const baselineFile = path.resolve(root, options.baseline ?? 'lighthouse.baseline.json');
    if (options['save-baseline']) {
        if (baselineFile === summaryFile) throw new Error('Baseline and summary must use different files.');
        const summary = readJson(summaryFile);
        const result = checkSummary(summary, config);
        if (!result.passed)
            console.warn('Baseline contains budget violations. Saving acknowledges these values; budget checks will still fail.');
        mkdirSync(path.dirname(baselineFile), { recursive: true });
        writeJson(baselineFile, makeBaseline(summary));
        console.log(`Baseline saved: ${baselineFile}`);
        return;
    }
    if (options.check) {
        const summary = readJson(summaryFile);
        let baseline;
        try {
            baseline = readJson(baselineFile);
        } catch (error) {
            if (error.code === 'ENOENT')
                throw new Error(`Baseline missing: ${baselineFile}. Review a three-run report, then run audit:lighthouse:baseline.`);
            throw error;
        }
        const result = checkSummary(summary, config, baseline);
        const checkedFile = path.join(path.dirname(summaryFile), 'summary.checked.html');
        const reportPrefix = path.relative(path.dirname(checkedFile), reportsRoot).split(path.sep).join('/');
        writeFileSync(checkedFile, renderSummary(summary, result, reportPrefix ? `${reportPrefix}/` : ''));
        for (const failure of result.failures) console.error(`${failure.route ?? 'Run'} ${failure.device ?? ''}: ${failure.message}`);
        console.log(
            `${result.passed ? 'PASS' : 'FAIL'}: ${summary.cases.length} cases; ${result.failures.length} failures. Report: ${checkedFile}`,
        );
        process.exitCode = result.passed ? 0 : 1;
        return;
    }
    const inventory = readRoutes(readFileSync(path.resolve(root, config.routesFile), 'utf8'));
    const plan = collectionPlan(config, options, inventory);
    const staticRoot = path.resolve(root, options['static-root'] ?? 'dist/browser');
    const remoteOrigin = options['base-url'] ? validateOrigin(options['base-url']) : undefined;
    const apiOrigin = options['api-origin'] ? validateOrigin(options['api-origin']) : undefined;
    if (!remoteOrigin) for (const route of plan.routes) prerenderFile(staticRoot, route);
    console.log(
        `${plan.routes.length} routes × ${plan.devices.length} devices × ${plan.runs} runs = ${plan.routes.length * plan.devices.length * plan.runs} audits.`,
    );
    if (options['dry-run']) {
        console.log(JSON.stringify({ ...plan, target: remoteOrigin ?? staticRoot, apiOrigin }, null, 2));
        return;
    }

    mkdirSync(reportsRoot, { recursive: true });
    const runId = `${new Date().toISOString().replace(/[:.]/g, '-')}-${process.pid}`;
    const runRoot = path.join(reportsRoot, 'runs', runId);
    mkdirSync(runRoot, { recursive: true });
    const samples = [];
    const context = {
        lighthouseVersion: null,
        chromeMajor: null,
        platform: platform(),
        arch: arch(),
        cpu: cpus()[0]?.model ?? 'unknown',
        memoryGiB: Math.round(totalmem() / 1024 ** 3),
        target: { mode: remoteOrigin ? 'remote' : 'local-gzip', origin: remoteOrigin ?? null, apiOrigin: apiOrigin ?? null },
        profiles: {},
    };
    let server;
    let chrome;
    let chromeProfile;
    const persist = () => {
        const summary = aggregate(plan, samples, context);
        const result = checkSummary(summary, config);
        summary.budgetCheck = result;
        writeJson(path.join(runRoot, 'summary.json'), summary);
        writeJson(summaryFile, summary);
        writeFileSync(path.join(reportsRoot, 'summary.html'), renderSummary(summary, result));
        return summary;
    };
    persist();
    try {
        server = remoteOrigin ? undefined : await startStaticServer(staticRoot, apiOrigin);
        chromeProfile = createAuditProfile();
        // An explicit task-owned profile prevents chrome-launcher's asynchronous
        // Windows temp-removal callback from throwing outside the cleanup handler.
        chrome = await launch({ userDataDir: chromeProfile, chromeFlags: ['--headless', '--no-sandbox'], logLevel: 'error' });
        const version = await (await fetch(`http://127.0.0.1:${chrome.port}/json/version`)).json();
        context.chromeMajor = /\/(\d+)/.exec(version.Browser)?.[1] ?? 'unknown';
        const origin = remoteOrigin ?? server.origin;
        const total = plan.routes.length * plan.devices.length * plan.runs;
        let index = 0;
        for (const route of plan.routes)
            for (const device of plan.devices)
                for (let run = 1; run <= plan.runs; run++) {
                    index++;
                    const name = reportName(route, device, run);
                    const report = `runs/${runId}/${name}.html`;
                    try {
                        const result = await lighthouse(
                            `${origin}${route}`,
                            {
                                port: chrome.port,
                                output: ['html', 'json'],
                                logLevel: 'error',
                                disableStorageReset: false,
                            },
                            device === 'desktop' ? desktopConfig : undefined,
                        );
                        if (!result || !Array.isArray(result.report)) throw new Error('Lighthouse did not produce HTML and JSON reports.');
                        writeFileSync(path.join(runRoot, `${name}.html`), result.report[0]);
                        writeFileSync(path.join(runRoot, `${name}.json`), result.report[1]);
                        context.lighthouseVersion = result.lhr.lighthouseVersion;
                        const settings = result.lhr.configSettings;
                        const profile = {
                            formFactor: settings.formFactor,
                            screenEmulation: settings.screenEmulation,
                            throttling: settings.throttling,
                            throttlingMethod: settings.throttlingMethod,
                            emulatedUserAgent: settings.emulatedUserAgent,
                            disableStorageReset: settings.disableStorageReset,
                        };
                        if (context.profiles[device] && JSON.stringify(context.profiles[device]) !== JSON.stringify(profile))
                            throw new Error('Device settings changed during collection.');
                        context.profiles[device] = profile;
                        const sample = sampleFromLhr(result.lhr, route, device, run, report);
                        samples.push(sample);
                        console.log(
                            `[${index}/${total}] ${route} ${device} #${run}: ${sample.metrics.performance} / LCP ${formatMetric('lcp', sample.metrics.lcp)} / TBT ${formatMetric('tbt', sample.metrics.tbt)}`,
                        );
                    } catch (error) {
                        samples.push({
                            route,
                            device,
                            run,
                            report: existsSync(path.join(runRoot, `${name}.html`)) ? report : undefined,
                            error: error.message,
                        });
                        console.error(`[${index}/${total}] ${route} ${device} #${run}: FAILED: ${error.message}`);
                    }
                    persist();
                }
        const summary = persist();
        console.log(
            `Reports: ${path.join(reportsRoot, 'summary.html')}\nSaved summary: ${summaryFile}\nBudget violations: ${summary.budgetCheck.failures.length}. Run audit:lighthouse:check to enforce budgets and regressions.`,
        );
        if (!summary.complete) process.exitCode = 1;
    } finally {
        try {
            if (chrome) {
                try {
                    await chrome.kill();
                } catch (error) {
                    if (error.code !== 'EPERM') throw error;
                    console.warn('Chrome exited; Windows retained its temporary profile directory.');
                }
            }
        } finally {
            if (server) await server.close();
            if (chromeProfile && !removeAuditProfile(chromeProfile))
                console.warn('Windows retained a locked file in the task-owned temporary Chrome profile.');
        }
    }
}

try {
    await main();
} catch (error) {
    console.error(error.message);
    process.exitCode = 1;
}
