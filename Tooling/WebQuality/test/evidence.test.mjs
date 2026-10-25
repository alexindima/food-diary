import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { aggregate } from '../../../FoodDiary.Web.Client/scripts/lighthouse-public.mjs';
import { journeyEvidence, performanceEvidence } from '../../../FoodDiary.Web.Client/quality/modern-web-guidance/evidence.mjs';

const requirement = { id: 'evidence' };

test('skipped, missing and failing journey tests cannot establish accessibility evidence', () => {
    const report = { suites: [{ suites: [{ specs: [{ tests: [{ status: 'expected', results: [{ status: 'passed' }] }] }] }] }] };
    assert.equal(journeyEvidence(report, requirement).status, 'pass');
    report.suites[0].suites[0].specs[0].tests[0] = { status: 'skipped', results: [{ status: 'skipped' }] };
    assert.equal(journeyEvidence(report, requirement).status, 'fail');
    assert.equal(journeyEvidence({ suites: [] }, requirement).status, 'error');
    report.errors = [{ message: 'Server did not start.' }];
    assert.ok(journeyEvidence(report, requirement).findings.some(item => item.message === 'Server did not start.'));
});

test('a green client project cannot stand in for omitted admin and platform journeys', () => {
    const requirement = { id: 'all-journeys', requiredProjects: ['client', 'admin', 'platform'] };
    const report = { suites: [{ specs: [{ tests: [{ projectName: 'client', status: 'expected', results: [{ status: 'passed' }] }] }] }] };
    assert.equal(journeyEvidence(report, requirement).status, 'fail');
    for (const projectName of ['admin', 'platform']) report.suites[0].specs[0].tests.push({ projectName, status: 'expected', results: [{ status: 'passed' }] });
    assert.equal(journeyEvidence(report, requirement).status, 'pass');
});

function performanceSummary(overrides = {}, runs = 3) {
    const metrics = {
        performance: 95,
        accessibility: 100,
        bestPractices: 100,
        seo: 100,
        fcp: 1000,
        lcp: 1500,
        tbt: 100,
        cls: 0.01,
        bytes: 100000,
        ...overrides,
    };
    const context = {
        lighthouseVersion: '13.5.0',
        chromeMajor: '140',
        platform: 'test',
        arch: 'x64',
        cpu: 'controlled',
        memoryGiB: 16,
        target: { mode: 'local-gzip', origin: null, apiOrigin: null },
        profiles: { desktop: {} },
    };
    return aggregate(
        { routes: ['/'], devices: ['desktop'], runs },
        Array.from({ length: runs }, (_, index) => ({ route: '/', device: 'desktop', run: index + 1, metrics })),
        context,
    );
}

test('performance evidence validates comparable actual samples and applies existing repository budgets', () => {
    const config = JSON.parse(
        readFileSync(new URL('../../../FoodDiary.Web.Client/lighthouse.public.config.json', import.meta.url), 'utf8'),
    );
    assert.equal(performanceEvidence(performanceSummary(), config, requirement).status, 'pass');
    const failing = performanceEvidence(performanceSummary({ lcp: 8000 }), config, requirement);
    assert.equal(failing.status, 'fail');
    assert.ok(failing.findings.some(item => item.message.includes('LCP')));
    assert.throws(() => performanceEvidence(performanceSummary({}, 1), config, requirement), /3 runs/u);
    assert.throws(() => performanceEvidence({ complete: true, cases: [] }, config, requirement), /invalid/u);
});
