import assert from 'node:assert/strict';
import { test } from 'node:test';
import { DatabaseSync } from 'node:sqlite';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { searchContextBatch } from './code-graph-batch.mjs';
import { runGraphProcess } from './code-graph-process.mjs';

test('batch metadata and rows share one snapshot; next batch sees committed changes', () => {
  const root = mkdtempSync(join(tmpdir(), 'wiki-batch-'));
  const reader = new DatabaseSync(join(root, 'graph.sqlite'));
  const writer = new DatabaseSync(join(root, 'graph.sqlite'));
  try {
    reader.exec('PRAGMA journal_mode=WAL; CREATE VIRTUAL TABLE context_search USING fts5(body); INSERT INTO context_search VALUES (\'first\')');
    let countReads = 0;
    const observed = {
      exec: sql => reader.exec(sql),
      prepare: sql => { countReads++; return reader.prepare(sql); },
    };
    const first = searchContextBatch(observed, [{ query: 'a', limit: 10 }, { query: 'b' }], (db, query, limit, filters, state) => {
      if (query === 'a') writer.exec("INSERT INTO context_search VALUES ('second')");
      return { count: state.indexedDocuments, rows: reader.prepare('SELECT body FROM context_search').all().length, limit };
    });
    assert.deepEqual(first.results, [{ count: 1, rows: 1, limit: 10 }, { count: 1, rows: 1, limit: 20 }]);
    assert.equal(countReads, 1);
    assert.equal(searchContextBatch(reader, [{}], (db, q, l, f, state) => state.indexedDocuments).results[0], 2);
    const failure = new Error('original search error');
    assert.throws(() => searchContextBatch(reader, [{}], () => { throw failure; }), error => error === failure);
    assert.equal(searchContextBatch(reader, [{}], (db, q, l, f, state) => state.indexedDocuments).results[0], 2);
  } finally {
    writer.close(); reader.close(); rmSync(root, { recursive: true, force: true });
  }
});

test('child failures preserve stdout start, stderr and exit status', () => {
  assert.throws(() => runGraphProcess(process.execPath, ['-e',
    "process.stdout.write('COMPILER_ERROR_START\\n' + 'x'.repeat(20000)); process.stderr.write('STDERR_END'); process.exitCode=7;",
  ]), error => error.message.includes('COMPILER_ERROR_START') && error.message.includes('STDERR_END') && error.message.includes('exit=7'));
  assert.equal(runGraphProcess(process.execPath, ['-e', "process.stdout.write('ok')"]), 'ok');
  assert.throws(() => runGraphProcess(join(tmpdir(), 'nonexistent-wiki-command'), []), /ENOENT/);
});

test('snapshot cleanup cannot hide the original search failure', () => {
  const original = new Error('search failed');
  const cleanup = new Error('rollback failed');
  const database = {
    exec: sql => { if (sql === 'ROLLBACK') throw cleanup; },
    prepare: () => ({ get: () => ({ count: 0 }) }),
  };
  assert.throws(() => searchContextBatch(database, [{}], () => { throw original; }),
    error => error === original && error.rollbackError === cleanup);
});
