import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';

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
    const steps = called.jobs['web-quality'].steps;
    const commands = steps.map(step => step.run ?? '').join('\n');
    for (const profile of ['source', 'contracts', 'browser', 'journeys', 'performance']) {
        const step = steps.find(step => step.run?.includes(`--profile ${profile}`));
        assert.ok(step, `${profile} must execute`);
        assert.equal(step.if, undefined, `${profile} cannot depend on manual opt-in`);
    }
    assert.match(commands, /install --with-deps chromium firefox webkit/u);
    assert.match(commands, /--browser firefox/u);
    assert.match(commands, /--browser webkit/u);
    assert.match(commands, /verify-platform-browser\.mjs/u);
    assert.ok(steps.some(step => step.if === 'always()' && step.uses?.startsWith('actions/upload-artifact@')));
});
