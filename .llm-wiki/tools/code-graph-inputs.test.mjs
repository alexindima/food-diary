import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createHash } from 'node:crypto';
import { randomUUID } from 'node:crypto';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { collectGraphInputs } from './code-graph-inputs.mjs';

const hash = text => createHash('sha256').update(text).digest('hex');
const missing = () => Object.assign(new Error('missing'), { code: 'ENOENT' });

function serialReference(paths, files, previousFiles, dirtyPaths, force = false, skipRead = () => false) {
  const result = { knownPaths: new Set(), candidates: [], scanned: 0, unchanged: 0, verifiedDirtyFiles: 0 };
  for (const path of new Set(paths)) {
    const file = files.get(path);
    if (!file) continue;
    result.knownPaths.add(path);
    if (skipRead(path)) continue;
    const prior = previousFiles.get(path);
    const metadataMatches = prior && prior.size === file.stat.size && Math.abs(prior.mtime_ms - file.stat.mtimeMs) < 0.001;
    if (!force && metadataMatches && !dirtyPaths.has(path)) { result.unchanged++; continue; }
    const contentHash = hash(file.text);
    if (dirtyPaths.has(path)) result.verifiedDirtyFiles++;
    if (!force && prior && prior.content_hash === contentHash) {
      if (!metadataMatches) {
        result.scanned++;
        result.candidates.push({ path, stat: file.stat, prior, text: null, contentHash, metadataOnly: true });
      }
      result.unchanged++;
      continue;
    }
    result.scanned++;
    result.candidates.push({ path, stat: file.stat, prior, text: file.text, contentHash, metadataOnly: false });
  }
  return result;
}

test('bounded input reads preserve every serial candidate and counter in inventory order', async () => {
  const paths = ['new.cs', 'unchanged.cs', 'same-content.cs', 'dirty.cs', 'deleted.cs', 'Юникод с пробелами.cs', 'frontend.ts', 'new.cs'];
  const files = new Map([...new Set(paths)].filter(path => path !== 'deleted.cs').map((path, index) =>
    [path, { text: index === 3 ? 'public class Bravo {}' : 'public class Alpha {}', stat: { size: 21, mtimeMs: index + 1 } }]));
  const previousFiles = new Map([
    ['unchanged.cs', { id: 1, size: 21, mtime_ms: 2, content_hash: hash('public class Alpha {}') }],
    ['same-content.cs', { id: 2, size: 21, mtime_ms: 0, content_hash: hash('public class Alpha {}') }],
    ['dirty.cs', { id: 3, size: 21, mtime_ms: 4, content_hash: hash('public class Alpha {}') }],
  ]);
  const dirtyPaths = new Set(['dirty.cs', 'same-content.cs']);
  const completed = [];
  const fileSystem = {
    stat: async path => {
      const relative = paths.find(value => path.endsWith(value));
      await new Promise(resolve => setTimeout(resolve, relative === paths[0] ? 12 : 1));
      completed.push(relative);
      const file = files.get(relative);
      if (!file) throw missing();
      return file.stat;
    },
    readFile: async (path, encoding) => {
      assert.equal(encoding, 'utf8');
      const relative = paths.find(value => path.endsWith(value));
      return files.get(relative).text;
    },
  };
  for (const force of [false, true]) {
    for (const skipRead of [() => false, path => path.endsWith('.ts')]) {
      const actual = await collectGraphInputs({ repositoryRoot: '/', paths, previousFiles, dirtyPaths, force, skipRead, concurrency: 3, fileSystem });
      assert.deepEqual(actual, serialReference(paths, files, previousFiles, dirtyPaths, force, skipRead));
    }
  }
  assert.notEqual(completed[0], paths[0], 'fixture must finish out of order');
});

test('dirty same-size and same-time edits are read again in each pass; clean metadata avoids reading', async () => {
  const previousFiles = new Map([['file.cs', { size: 4, mtime_ms: 1, content_hash: hash('two!') }]]);
  let text = 'six!';
  let reads = 0;
  const common = { repositoryRoot: '/', paths: ['file.cs'], previousFiles, fileSystem: {
    stat: async () => ({ size: 4, mtimeMs: 1 }),
    readFile: async () => { reads++; return text; },
  } };
  assert.equal((await collectGraphInputs({ ...common, dirtyPaths: new Set() })).unchanged, 1);
  assert.equal(reads, 0);
  for (text of ['six!', 'ten!']) {
    const result = await collectGraphInputs({ ...common, dirtyPaths: new Set(['file.cs']) });
    assert.equal(result.verifiedDirtyFiles, 1);
    assert.equal(result.candidates[0].contentHash, hash(text));
    assert.equal(result.candidates[0].text, text);
  }
  assert.equal(reads, 2);
});

test('input workers are bounded and a failure drains pending reads before rejection', async () => {
  let active = 0;
  let maximum = 0;
  let reads = 0;
  const failure = new Error('read failed');
  const fileSystem = {
    stat: async () => ({ size: 1, mtimeMs: 1 }),
    readFile: async () => {
      const number = reads++;
      active++;
      maximum = Math.max(maximum, active);
      try {
        await new Promise(resolve => setTimeout(resolve, number === 0 ? 2 : 15));
        if (number === 0) throw failure;
        return 'x';
      } finally { active--; }
    },
  };
  await assert.rejects(collectGraphInputs({
    repositoryRoot: '/', paths: Array.from({ length: 50 }, (_, index) => index + '.cs'),
    previousFiles: new Map(), dirtyPaths: new Set(), concurrency: 4, fileSystem,
  }), error => error === failure);
  assert.equal(active, 0);
  assert.equal(maximum, 4);
  assert.equal(reads, 4, 'no new reads may be dispatched after a failure');
});

test('missing paths are omitted; filesystem failures and invalid bounds reject', async () => {
  const options = { repositoryRoot: '/', paths: ['missing.cs'], previousFiles: new Map(), dirtyPaths: new Set() };
  assert.deepEqual((await collectGraphInputs({ ...options, fileSystem: { stat: async () => { throw missing(); } } })).knownPaths, new Set());
  const failure = Object.assign(new Error('denied'), { code: 'EACCES' });
  await assert.rejects(collectGraphInputs({ ...options, fileSystem: { stat: async () => { throw failure; } } }), error => error === failure);
  for (const concurrency of [0, -1, 1.5, NaN]) await assert.rejects(collectGraphInputs({ ...options, concurrency }), /positive/);
  assert.equal((await collectGraphInputs({ ...options, paths: [] })).candidates.length, 0);
});

test('the real graph writer lock survives an awaited callback and releases on success and failure', async () => {
  const source = readFileSync(new URL('./code-graph.mjs', import.meta.url), 'utf8');
  const start = source.indexOf('async function withBuildLock(');
  const end = source.indexOf('function isProcessAlive(', start);
  assert.ok(start >= 0 && end > start, 'writer lock must retain its asynchronous callback');
  const root = mkdtempSync(join(tmpdir(), 'wiki-async-writer-lock-'));
  const lock = join(root, '.artifacts/llm-wiki/code-graph/build.lock');
  const withLock = new Function('repositoryRoot', 'resolve', 'dirname', 'randomUUID', 'mkdirSync', 'writeFileSync',
    'readFileSync', 'rmSync', 'statSync', 'isProcessAlive', source.slice(start, end) + '; return withBuildLock;')(
    root, resolve, dirname, randomUUID, mkdirSync, writeFileSync, readFileSync, rmSync, statSync, () => true);
  try {
    let entered;
    let release;
    const started = new Promise(resolve => { entered = resolve; });
    const barrier = new Promise(resolve => { release = resolve; });
    const pending = withLock(async () => { entered(); await barrier; return 'completed'; });
    await started;
    await new Promise(resolve => setTimeout(resolve, 10));
    assert.ok(existsSync(lock), 'writer lock was released while input reads were pending');
    assert.equal(JSON.parse(readFileSync(join(lock, 'owner.json'), 'utf8')).pid, process.pid);
    release();
    assert.equal(await pending, 'completed');
    assert.ok(!existsSync(lock));
    const failure = new Error('asynchronous input failure');
    await assert.rejects(withLock(async () => { await Promise.resolve(); throw failure; }), error => error === failure);
    assert.ok(!existsSync(lock));
  } finally { rmSync(root, { recursive: true, force: true }); }
});
