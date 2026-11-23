import assert from 'node:assert/strict';
import { existsSync, mkdirSync, mkdtempSync, realpathSync, rmSync, symlinkSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

import { coverageArgs, planCleanup, prune } from './run-local-coverage.mjs';

function createRunRoot(t) {
    const parent = realpathSync(tmpdir());
    const root = realpathSync(mkdtempSync(join(parent, 'fd-local-coverage-test-')));
    assert.equal(dirname(root), parent);
    t.after(() => {
        assert.equal(realpathSync(root), root);
        rmSync(root, { recursive: true });
    });
    return root;
}

function writeRun(root, id, day, fields = {}) {
    const path = join(root, id);
    mkdirSync(path);
    const finishedAt = new Date(Date.UTC(2026, 0, day)).toISOString();
    writeFileSync(
        join(path, 'run.json'),
        JSON.stringify({ schemaVersion: 1, id, project: 'app', mode: 'summary', exitCode: 0, finishedAt, ...fields }),
    );
    return path;
}

test('preserves npm lifecycle hooks and applies compact reporters only to summary runs', () => {
    assert.deepEqual(coverageArgs('app', false, ['src/app/**/*.spec.ts', 'testing/with space.spec.ts']), [
        'run',
        'test:coverage:app',
        '--',
        '--coverage-reporters=text-summary',
        '--coverage-reporters=json-summary',
        '--include=src/app/**/*.spec.ts',
        '--include=testing/with space.spec.ts',
    ]);
    assert.deepEqual(coverageArgs('webmcp', false, []), [
        'run',
        'test:coverage:webmcp',
        '--',
        '--coverage-reporters=text-summary',
        '--coverage-reporters=json-summary',
    ]);
    assert.deepEqual(coverageArgs('admin', true, []), ['run', 'test:coverage:admin']);
});

test('rotates summary, successful full, and failed full runs independently for each project', t => {
    const root = createRunRoot(t);
    for (const day of [8, 2, 7, 6]) {
        writeRun(root, `run-app00${day}`, day);
        writeRun(root, `run-adm00${day}`, day, { project: 'admin' });
    }
    for (const day of [8, 3, 7]) writeRun(root, `run-ful00${day}`, day, { mode: 'full' });
    for (const day of [8, 4]) writeRun(root, `run-bad00${day}`, day, { mode: 'full', exitCode: 1 });

    const planned = planCleanup(root);
    assert.deepEqual(planned.map(run => run.id).sort(), ['run-adm002', 'run-app002', 'run-bad004', 'run-ful003']);
    assert.deepEqual(
        planCleanup(root, 'admin').map(run => run.id),
        ['run-adm002'],
    );
    assert.deepEqual(
        planCleanup(root, 'app')
            .map(run => run.id)
            .sort(),
        ['run-app002', 'run-bad004', 'run-ful003'],
    );
    assert.ok(planned.every(run => dirname(run.path) === root && existsSync(run.path)));
});

test('skips pinned, unfinished, invalid, foreign, and linked directories', t => {
    const root = createRunRoot(t);
    for (const day of [8, 2, 7, 6]) writeRun(root, `run-app00${day}`, day);
    for (const [id, day] of [
        ['run-keep01', 1],
        ['run-keep31', 31],
    ])
        writeFileSync(join(writeRun(root, id, day), '.keep'), '');

    const pending = join(root, 'run-pend01');
    mkdirSync(pending);
    writeFileSync(join(pending, 'run.json.tmp'), '{}');
    writeRun(root, 'run-tomb01', 1, { id: 'run-other1' });
    writeRun(root, 'run-wild01', 1, { project: 'unknown' });
    writeRun(root, 'run-date01', 1, { finishedAt: 'not a date' });
    writeFileSync(join(writeRun(root, 'run-json01', 1), 'run.json'), '{');
    writeRun(root, 'manual-archive', 1);
    const linkTarget = writeRun(root, 'outside-rotation', 1, { id: 'run-link01' });
    symlinkSync(linkTarget, join(root, 'run-link01'), process.platform === 'win32' ? 'junction' : 'dir');

    assert.deepEqual(
        planCleanup(root).map(run => run.id),
        ['run-app002'],
    );

    prune(root, 'app');
    assert.equal(existsSync(join(root, 'run-app002')), false);
    for (const directory of ['run-app008', 'run-keep01', 'run-pend01', 'run-tomb01', 'manual-archive', 'run-link01']) {
        assert.ok(existsSync(join(root, directory)), directory);
    }
});
