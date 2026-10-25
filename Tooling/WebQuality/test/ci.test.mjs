import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { spawnSync } from 'node:child_process';

const require = createRequire(new URL('../../../FoodDiary.Web.Client/package.json', import.meta.url));
const { parse } = require('yaml');
const workflow = file => parse(readFileSync(new URL(`../../../.github/workflows/${file}`, import.meta.url), 'utf8'));

test('PR, mainline and scheduled CI automatically run every web quality engine', () => {
    const caller = workflow('ci-tests.yml');
    assert.ok(Object.hasOwn(caller.on, 'pull_request'));
    assert.ok(caller.on.push.branches.includes('master'));
    assert.ok(caller.on.schedule.length);
    assert.equal(caller.jobs['web-guidance'].uses, './.github/workflows/web-quality.yml');
    const called = workflow('web-quality.yml');
    assert.ok(Object.hasOwn(called.on, 'workflow_call'));
    assert.equal(called.on.workflow_call?.inputs, undefined);
    const { prepare, audit, 'web-quality': gate } = called.jobs;
    assert.ok(prepare, 'One shared production build must precede parallel audits.');
    const prepareCommands = prepare.steps.map(step => step.run ?? '').join('\n');
    for (const profile of ['source', 'contracts']) assert.match(prepareCommands, new RegExp(`--profile ${profile}`, 'u'));
    assert.match(prepareCommands, /cli\.mjs build/u);
    assert.match(prepareCommands, /check:seo-prerender/u);
    assert.equal(audit.needs, 'prepare');
    assert.equal(audit.strategy['fail-fast'], false);
    assert.ok(audit.strategy['max-parallel'] > 1);
    const workloads = audit.strategy.matrix.include;
    assert.deepEqual(workloads.map(item => item.id).sort(), ['chromium', 'firefox', 'fixtures', 'journeys', 'performance', 'webkit']);
    assert.deepEqual(workloads.filter(item => item.profile === 'browser').map(item => item.browser).sort(), ['chromium', 'firefox', 'webkit']);
    assert.equal(workloads.filter(item => item.profile === 'performance').length, 1, 'Lighthouse owns an isolated runner.');
    const lock = JSON.parse(readFileSync(new URL('../../../FoodDiary.Web.Client/package-lock.json', import.meta.url), 'utf8'));
    assert.equal(audit.container.image, `mcr.microsoft.com/playwright:v${lock.packages['node_modules/@playwright/test'].version}-noble`);
    assert.match(audit.container.options, /(?:^|\s)--user\s+1001(?:\s|$)/u, 'Container user must match the GitHub workspace and HOME owner.');
    const commands = audit.steps.map(step => step.run ?? '').join('\n');
    assert.doesNotMatch(commands, /install --with-deps/u, 'The image already contains browser engines and OS dependencies.');
    assert.match(commands, /verify-browser\.mjs/u);
    assert.match(commands, /verify-platform-browser\.mjs/u);
    assert.match(commands, /--profile browser.*--browser/u);
    assert.match(commands, /--profile journeys/u);
    assert.match(commands, /--profile performance/u);
    const upload = prepare.steps.find(step => step.uses?.startsWith('actions/upload-artifact@') && step.with.name === 'web-guidance-build');
    assert.match(upload.with.path, /dist\/browser/u);
    assert.match(upload.with.path, /\.artifacts\/web-quality\/build\.json/u);
    assert.equal(upload.with['include-hidden-files'], true);
    assert.ok(audit.steps.some(step => step.uses?.startsWith('actions/download-artifact@') && step.with.name === upload.with.name));
    for (const job of [prepare, audit]) {
        assert.equal(job.if, undefined, 'Required checks cannot depend on manual opt-in.');
        assert.ok(job.steps.some(step => step.if === 'always()' && step.uses?.startsWith('actions/upload-artifact@')));
    }
    assert.deepEqual(gate.needs, ['prepare', 'audit']);
    assert.equal(gate.if, 'always()');
    assert.equal(gate.steps[0].env.WEB_QUALITY_JOBS, '${{ toJSON(needs) }}');
});

test('the final Google gate rejects failed, cancelled, skipped and missing required execution', () => {
    const gate = workflow('web-quality.yml').jobs['web-quality'];
    const command = gate.steps[0].run?.match(/^node -e "([^"]+)"\s*$/u)?.[1];
    assert.ok(command, 'The aggregate gate must have an executable result check.');
    const execute = results => spawnSync(process.execPath, ['-e', command], {
        env: { ...process.env, WEB_QUALITY_JOBS: JSON.stringify(results) },
        encoding: 'utf8',
    });
    const passed = Object.fromEntries(gate.needs.map(id => [id, { result: 'success' }]));
    assert.equal(execute(passed).status, 0);
    for (const id of gate.needs)
        for (const result of ['failure', 'cancelled', 'skipped'])
            assert.equal(execute({ ...passed, [id]: { result } }).status, 1, `${id}: ${result} cannot pass`);
    assert.equal(execute({}).status, 1);
});
