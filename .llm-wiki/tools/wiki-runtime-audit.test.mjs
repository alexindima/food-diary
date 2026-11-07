import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, existsSync, rmSync } from 'node:fs';
import { dirname, join, resolve, basename } from 'node:path';
import { tmpdir } from 'node:os';
import { execFileSync } from 'node:child_process';
import { runtimeDiagnosticState } from '../../scripts/ai/runtime-diagnostics.mjs';
import { probeRenderedRuntime } from '../../scripts/ai/task-runtime-browser.mjs';

function withFixture(action) {
  const parent = resolve(tmpdir());
  const root = mkdtempSync(join(parent, 'fd-wiki-runtime-audit-'));
  return Promise.resolve().then(() => action(root)).finally(() => {
    if (dirname(resolve(root)) !== parent || !basename(root).startsWith('fd-wiki-runtime-audit-')) throw new Error('Unsafe runtime audit cleanup target.');
    rmSync(root, { recursive: true, force: true });
  });
}

test('missing process identity cannot report a dead runtime as alive', () => withFixture(root => {
  const env = { ...process.env };
  for (const key of ['GIT_DIR','GIT_COMMON_DIR','GIT_WORK_TREE','GIT_INDEX_FILE','GIT_OBJECT_DIRECTORY','GIT_ALTERNATE_OBJECT_DIRECTORIES','GIT_PREFIX']) delete env[key];
  const git = (...args) => execFileSync('git', ['-C', root, ...args], { env, windowsHide: true, stdio: 'pipe', timeout: 10000 });
  writeFileSync(join(root, '.gitignore'), '.artifacts/\n');
  git('init', '--quiet'); git('add', '.');
  git('-c','user.name=WikiFixture','-c','user.email=wiki-fixture@example.invalid','commit','--quiet','-m','baseline');
  const directory = join(root, '.artifacts/task-runtime/identity-probe');
  mkdirSync(directory, { recursive: true });
  writeFileSync(join(directory, 'runtime.json'), JSON.stringify({
    checkout: root, ports: { api: 15001, frontend: 15002 }, apiUrl: 'http://127.0.0.1:15001', frontendUrl: 'http://127.0.0.1:15002',
    sourceFingerprint: 'fixture', status: 'failed', processes: [{ role: 'api', pid: 2147483647, identity: null }, { role: 'frontend', pid: 2147483647, identity: '' }],
  }));
  const result = runtimeDiagnosticState('identity-probe', root);
  assert.ok(result.processes.every(item => !item.alive), 'A missing saved identity must never make a nonexistent runtime process alive.');
}));

test('browser closes when diagnostics callback throws', () => withFixture(async root => {
  const packageRoot = join(root, 'FoodDiary.Web.Client/node_modules/@playwright/test');
  mkdirSync(packageRoot, { recursive: true });
  writeFileSync(join(root, 'FoodDiary.Web.Client/package.json'), '{}');
  writeFileSync(join(packageRoot, 'package.json'), JSON.stringify({ main: 'index.cjs' }));
  const closed = join(root, 'browser-closed');
  writeFileSync(join(packageRoot, 'index.cjs'), `module.exports={chromium:{launch:async()=>({newContext:async()=>({}),close:async()=>require('node:fs').writeFileSync(${JSON.stringify(closed)},'closed')})}};`);
  await assert.rejects(probeRenderedRuntime(root, 'http://localhost:4200', 'fixture-user', {
    onDiagnostics() { throw new Error('Controlled diagnostic publication failure.'); },
  }), /Controlled diagnostic publication failure\./u);
  assert.ok(existsSync(closed), 'Owned browser cleanup must run even when diagnostic publication throws.');
}));
