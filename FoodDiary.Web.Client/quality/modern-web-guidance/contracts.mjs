import { createRequire } from 'node:module';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { runNode } from '../../../Tooling/WebQuality/lib/process.mjs';
import { readJson } from '../../../Tooling/WebQuality/lib/files.mjs';

const require = createRequire(new URL('../../package.json', import.meta.url));
const groups = {
    'contracts:controls': { project: 'fd-ui-kit' },
    'contracts:tour': { project: 'fd-tour' },
    'contracts:webmcp': { project: 'fd-webmcp' },
    'contracts:calendar': {
        project: 'food-diary-web-client',
        include: [
            '**/shared/lib/*date*.spec.ts',
            '**/shared/lib/*calendar*.spec.ts',
            '**/*calendar-timezone.spec.ts',
            '**/meal-date-input.utils.spec.ts',
            '**/user-manage-date.mapper.spec.ts',
            '**/localized-date.pipe.spec.ts',
        ],
    },
    'contracts:app-platform': {
        project: 'food-diary-web-client',
        include: [
            '**/token-storage.service.spec.ts',
            '**/auth.service.spec.ts',
            '**/frontend-observability.service.spec.ts',
            '**/browser-performance.service.spec.ts',
            '**/browser-window.service.spec.ts',
            '**/logging-api.service.spec.ts',
            '**/theme.service.spec.ts',
            '**/idle-selective-preloading.strategy.spec.ts',
            '**/shared/i18n/*.spec.ts',
            '**/shared/bootstrap/*.spec.ts',
            '**/shared/service-worker/*.spec.ts',
            '**/shared/notifications/notification-realtime*.spec.ts',
        ],
    },
    'contracts:admin-platform': {
        project: 'fooddiary-admin',
        include: ['**/admin-auth*.spec.ts', '**/app.spec.ts', '**/admin-theme*.spec.ts'],
    },
    'contracts:production': {
        scripts: [
            'scripts/finalize-service-worker.test.mjs',
            'scripts/lighthouse-public.test.mjs',
            'scripts/startup-document.test.mjs',
            'scripts/guidance-documents.test.mjs',
            'scripts/prerender-translations.test.mjs',
            'scripts/check-production-bundles.test.mjs',
            'scripts/font-assets.test.mjs',
            'scripts/design-token-payload.test.mjs',
        ],
    },
};
export const contractChecks = new Set(Object.keys(groups));

export function contractEvidence(report, requirement, exitCode) {
    const tests = report.testResults?.flatMap(file => file.assertionResults ?? []) ?? [];
    const failures = tests
        .filter(test => test.status !== 'passed')
        .map(test => ({ message: `${test.fullName ?? test.title}: ${test.status}` }));
    const missing = (requirement.requiredSuites ?? []).filter(
        name => !report.testResults?.some(file => file.name.replaceAll('\\', '/').endsWith(name)),
    );
    failures.push(...missing.map(name => ({ message: `Required owner suite did not execute: ${name}` })));
    if (exitCode !== 0) failures.push({ message: `Owner contract runner exited ${exitCode}.` });
    const valid =
        tests.length > 0 &&
        report.numTotalTests === tests.length &&
        report.numPassedTests === tests.filter(test => test.status === 'passed').length;
    return {
        requirement: requirement.id,
        engine: 'contracts',
        observations: tests.length,
        status: !valid ? 'error' : failures.length ? 'fail' : 'pass',
        findings: failures,
        detail: valid
            ? `${tests.length} owner assertions executed; ${failures.length} failures. Browser fixtures and field measurements are reported separately.`
            : 'Missing or inconsistent executed owner-test evidence.',
    };
}

export function tapEvidence(text, requirement, exitCode) {
    const counts = Object.fromEntries(
        [...text.matchAll(/^# (tests|pass|fail|cancelled|skipped|todo) (\d+)\s*$/gmu)].map(match => [match[1], Number(match[2])]),
    );
    const valid = counts.tests > 0 && counts.tests === counts.pass + counts.fail + counts.cancelled + counts.skipped + counts.todo;
    const failures = [];
    if (exitCode !== 0 || counts.pass !== counts.tests)
        failures.push({ message: `Production contracts: ${JSON.stringify(counts)}, exit ${exitCode}.` });
    return {
        requirement: requirement.id,
        engine: 'contracts',
        observations: counts.tests ?? 0,
        status: !valid ? 'error' : failures.length ? 'fail' : 'pass',
        findings: failures,
        detail: valid ? `${counts.tests} executed production/PWA/document contracts.` : 'No trustworthy TAP test summary was emitted.',
    };
}

export async function runContracts({ repositoryRoot, policy, artifactDirectory }) {
    const frontend = path.join(repositoryRoot, 'FoodDiary.Web.Client');
    const ng = path.join(path.dirname(require.resolve('@angular/cli/package.json')), 'bin/ng.js');
    const requirements = policy.requirements.filter(item => item.engine === 'contracts'),
        results = [],
        executions = [];
    for (const check of new Set(requirements.map(item => item.check))) {
        const group = groups[check];
        if (!group) throw new Error(`Unknown owner contract group: ${check}`);
        const name = check.slice('contracts:'.length),
            log = path.join(artifactDirectory, `${name}.log`),
            reportPath = path.join(artifactDirectory, `${name}.json`);
        const args = group.project
            ? [
                  ng,
                  'test',
                  group.project,
                  '--watch=false',
                  '--reporters=json',
                  `--output-file=${reportPath}`,
                  ...(group.include ?? []).map(pattern => `--include=${pattern}`),
              ]
            : ['--test', '--test-reporter=tap', ...group.scripts];
        const code = await runNode(args, { cwd: frontend, log, timeout: 900_000 });
        for (const requirement of requirements.filter(item => item.check === check)) {
            results.push(
                group.project
                    ? contractEvidence(await readJson(reportPath), requirement, code)
                    : tapEvidence(await readFile(log, 'utf8'), requirement, code),
            );
        }
        executions.push({ check, exitCode: code, log: `${name}.log`, report: group.project ? `${name}.json` : undefined });
    }
    return { results, metadata: { executions } };
}
