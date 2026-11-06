import assert from 'node:assert/strict';
import { mkdtemp, readFile, rm, mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { test } from 'node:test';
import { readWebmcpDiagnostics, runControlledScenarios } from './controlled-browser-scenarios.mjs';

test('the matrix prepares each isolated context, retains diagnostics and preserves the caller browser', async () => {
  const parent = resolve(import.meta.dirname, '../../.artifacts');
  await mkdir(parent, { recursive: true });
  const output = await mkdtemp(resolve(parent, 'controlled-runner-test-'));
  const visits = [], closed = [];
  const browser = { async newContext(options) {
    const id = visits.length;
    const page = { goto: async url => visits.push({ ...options, url }), screenshot: async () => {},
      locator: () => ({ getAttribute: async () => JSON.stringify({ reason: 'browser-api-unavailable', nextAction: 'use-supported-browser', token: 'private-token' }) }) };
    return { newPage: async () => page, close: async () => closed.push(id) };
  } };
  let prepared = 0, exercised = 0;
  try {
    const report = await runControlledScenarios({ browser, baseURL: 'http://localhost:4200', outputDirectory: output,
      scenarios: [{ id: 'card-layout', prepare: async () => prepared++, exercise: async () => exercised++ }] });
    assert.equal(prepared, 4);
    assert.equal(exercised, 4);
    assert.equal(closed.length, 4);
    assert.equal(report.cases.length, 4);
    assert.deepEqual(new Set(visits.map(item => item.locale)), new Set(['ru', 'en']));
    assert.ok(report.cases.every(item => item.webmcp.nextAction === 'use-supported-browser'));
    assert.ok(!(await readFile(resolve(output, 'report.json'), 'utf8')).includes('private-token'));
    await assert.rejects(runControlledScenarios({ browser, baseURL: 'http://localhost:4200', outputDirectory: output,
      scenarios: [{ id: 'card-layout', exercise: async () => {} }] }), { code: 'EEXIST' });
    assert.equal(closed.length, 4, 'A repeated output path must be rejected before creating another browser context');
  } finally { await rm(output, { recursive: true, force: true }); }
});

test('failed preparation closes its context and publishes a failing result without private errors', async () => {
  const output = await mkdtemp(resolve(import.meta.dirname, '../../.artifacts/controlled-failure-test-'));
  let closed = false;
  const browser = { newContext: async () => ({ newPage: async () => ({}), close: async () => { closed = true; } }) };
  try {
    await assert.rejects(runControlledScenarios({ browser, baseURL: 'http://localhost:4200', outputDirectory: output,
      locales: ['en'], viewports: [{ width: 390, height: 844 }],
      scenarios: [{ id: 'failure', prepare: async () => { throw new Error('password=private'); }, exercise: async () => {} }] }), /Controlled scenarios failed/);
    assert.equal(closed, true);
    const report = await readFile(resolve(output, 'report.json'), 'utf8');
    assert.equal(JSON.parse(report).cases[0].status, 'failed');
    assert.ok(!report.includes('private'));
  } finally { await rm(output, { recursive: true, force: true }); }
});

test('remote origins and invalid metadata do not create a supported browser claim', async () => {
  await assert.rejects(runControlledScenarios({ baseURL: 'https://fooddiary.club' }), /local application URL/);
  assert.deepEqual(await readWebmcpDiagnostics({ locator: () => ({ getAttribute: async () => 'not-json' }) }), { status: 'invalid-diagnostics' });
});
