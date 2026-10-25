import test from 'node:test';
import assert from 'node:assert/strict';
import { contractEvidence, tapEvidence } from '../../../FoodDiary.Web.Client/quality/modern-web-guidance/contracts.mjs';
const requirement = { id: 'controls', requiredSuites: ['form.spec.ts'] };
const report = () => ({ numTotalTests: 1, numPassedTests: 1, testResults: [{ name: '/src/form.spec.ts', assertionResults: [{ fullName: 'submit focuses invalid field', status: 'passed' }] }] });
test('owner contract evidence requires actual assertions and required suites, rejecting missing skipped and failed cases', () => {
    assert.equal(contractEvidence(report(), requirement, 0).status, 'pass');
    assert.equal(contractEvidence({}, requirement, 0).status, 'error');
    const failed = report(); failed.numPassedTests = 0; failed.testResults[0].assertionResults[0].status = 'failed';
    assert.equal(contractEvidence(failed, requirement, 1).status, 'fail');
    assert.equal(contractEvidence(report(), { ...requirement, requiredSuites: ['absent.spec.ts'] }, 0).status, 'fail');
    const skipped = report(); skipped.numPassedTests = 0; skipped.testResults[0].assertionResults[0].status = 'pending';
    assert.equal(contractEvidence(skipped, requirement, 0).status, 'fail');
    assert.equal(contractEvidence(report(), requirement, 1).status, 'fail');
});
test('production evidence cannot pass on process success without TAP assertions', () => {
    const tap = '# tests 2\n# pass 2\n# fail 0\n# cancelled 0\n# skipped 0\n# todo 0\n';
    assert.equal(tapEvidence(tap, requirement, 0).status, 'pass');
    assert.equal(tapEvidence('', requirement, 0).status, 'error');
    assert.equal(tapEvidence(tap.replace('# pass 2', '# pass 1').replace('# skipped 0', '# skipped 1'), requirement, 0).status, 'fail');
    assert.equal(tapEvidence(tap, requirement, 1).status, 'fail');
});
