import { createRequire } from 'node:module';
import path from 'node:path';
import { runNode } from '../../../Tooling/WebQuality/lib/process.mjs';
import { readJson } from '../../../Tooling/WebQuality/lib/files.mjs';
import { checkSummary, validateConfig, validateSummary } from '../../scripts/lighthouse-public.mjs';

const require = createRequire(new URL('../../package.json', import.meta.url));
const frontend = path.resolve(import.meta.dirname, '../..');

export function journeyEvidence(report, requirement) {
    const tests = [];
    function visit(suite) {
        for (const spec of suite.specs ?? []) tests.push(...spec.tests);
        for (const nested of suite.suites ?? []) visit(nested);
    }
    for (const suite of report.suites ?? []) visit(suite);
    const passed = tests.filter(test => test.status === 'expected' && test.results?.at(-1)?.status === 'passed');
    const failures = tests
        .filter(test => !passed.includes(test))
        .map(test => ({ message: `Journey status: ${test.status}; final result: ${test.results?.at(-1)?.status ?? 'missing'}` }));
    if (report.errors?.length) failures.push(...report.errors.map(error => ({ message: error.message ?? 'Playwright reporter error.' })));
    return {
        requirement: requirement.id,
        engine: 'journeys',
        status: !tests.length ? 'error' : failures.length ? 'fail' : 'pass',
        observations: tests.length,
        findings: failures,
        detail: `${passed.length}/${tests.length} authenticated accessibility tests passed. API fixtures are mocked; this is not a live-backend audit.`,
    };
}

export async function runJourneys({ policy, artifactDirectory }) {
    const requirements = policy.requirements.filter(requirement => requirement.engine === 'journeys');
    if (requirements.some(requirement => requirement.check !== 'journeys:authenticated-axe')) throw new Error('Unknown journey check.');
    const reportPath = path.join(artifactDirectory, 'journeys.json');
    const cli = path.join(path.dirname(require.resolve('@playwright/test/package.json')), 'cli.js');
    const code = await runNode([cli, 'test', '--config=quality/modern-web-guidance/journeys.config.mjs', '--reporter=json'], {
        cwd: frontend,
        log: path.join(artifactDirectory, 'journeys.log'),
        env: { PLAYWRIGHT_JSON_OUTPUT_NAME: reportPath, FD_WEB_QUALITY_RESULTS: path.join(artifactDirectory, 'journey-results') },
    });
    const report = await readJson(reportPath);
    const results = requirements.map(requirement => journeyEvidence(report, requirement));
    if (code !== 0 && results.every(result => result.status === 'pass'))
        throw new Error(`Playwright exited ${code} despite a passing report.`);
    return { results, metadata: { exitCode: code, fixtures: 'mocked API', report: 'journeys.json' } };
}

export function performanceEvidence(summary, config, requirement) {
    validateSummary(summary, 3);
    const checked = checkSummary(summary, config);
    return {
        requirement: requirement.id,
        engine: 'performance',
        status: checked.passed ? 'pass' : 'fail',
        observations: summary.cases?.length ?? 0,
        findings: checked.failures.map(failure => ({
            case: `${failure.route ?? 'collection'}/${failure.device ?? 'all'}`,
            message: failure.message,
        })),
        detail: `Repository laboratory budgets on ${summary.cases?.length ?? 0} route/device pairs. This does not establish field INP or full Core Web Vitals conformance.`,
        cases: summary.cases?.map(item => ({ id: `${item.route}/${item.device}`, metrics: item.metrics })),
    };
}

export async function runPerformance({ policy, artifactDirectory, baseURL, routes, devices, staticRoot }) {
    const requirements = policy.requirements.filter(requirement => requirement.engine === 'performance');
    if (requirements.some(requirement => requirement.check !== 'performance:public-budgets')) throw new Error('Unknown performance check.');
    const config = validateConfig(await readJson(path.join(frontend, 'lighthouse.public.config.json')));
    const reports = path.join(artifactDirectory, 'lighthouse');
    const args = ['scripts/run-lighthouse.mjs', `--reports-dir=${reports}`, '--runs=3'];
    if (routes) args.push(`--routes=${routes}`);
    else args.push('--smoke');
    if (devices) args.push(`--devices=${devices}`);
    if (baseURL) args.push(`--base-url=${baseURL}`);
    if (staticRoot) args.push(`--static-root=${staticRoot}`);
    const code = await runNode(args, { cwd: frontend, log: path.join(artifactDirectory, 'lighthouse.log'), timeout: 1_800_000 });
    if (code !== 0) throw new Error(`Lighthouse collection failed (${code}); see lighthouse.log.`);
    const summary = await readJson(path.join(reports, 'summary.json'));
    return {
        results: requirements.map(requirement => performanceEvidence(summary, config, requirement)),
        metadata: { context: summary.context, plan: summary.plan },
    };
}
