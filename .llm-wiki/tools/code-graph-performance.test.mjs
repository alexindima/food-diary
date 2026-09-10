import assert from 'node:assert/strict';
import { test } from 'node:test';
import { DatabaseSync } from 'node:sqlite';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { searchContextBatch } from './code-graph-batch.mjs';
import { runGraphProcess } from './code-graph-process.mjs';
import { englishMorphologicalVariants } from './code-graph-query-terms.mjs';
import { findIdentityCandidates } from './code-graph-identity.mjs';

test('English plural recall preserves previous alternatives and recognizes es plurals', () => {
  for (const [plural, singular] of [['indexes', 'index'], ['boxes', 'box'], ['classes', 'class'], ['watches', 'watch'], ['brushes', 'brush'], ['buzzes', 'buzz']]) {
    assert.ok(englishMorphologicalVariants(plural).includes(singular));
    assert.ok(englishMorphologicalVariants(plural).includes(plural.slice(0, -1)));
  }
  assert.deepEqual(englishMorphologicalVariants('policies'), ['policy']);
  assert.deepEqual(englishMorphologicalVariants('users'), ['user']);
  assert.deepEqual(englishMorphologicalVariants('class'), []);
  assert.deepEqual(englishMorphologicalVariants('индексы'), []);
  assert.ok(englishMorphologicalVariants('running').includes('run'));
  assert.ok(englishMorphologicalVariants('retried').includes('retry'));
  assert.ok(englishMorphologicalVariants('verified').includes('verify'));
});

test('bounded identity recall is independent of body length and uses stable ties', () => {
  const db = new DatabaseSync(':memory:');
  try {
    db.exec(`
      CREATE TABLE context_search(record_type, record_key, path, source_path, category, title, body);
      CREATE VIRTUAL TABLE context_search_identity USING fts5(path, title);
      CREATE TABLE context_search_features(context_rowid, layer, module, role, is_test, extension);
    `);
    for (const [id, path] of [[1, 'b/index.md'], [2, 'a/index.md']]) {
      db.prepare('INSERT INTO context_search(rowid, record_type, record_key, path, source_path, category, title, body) VALUES (?, ?, ?, ?, ?, ?, ?, ?)')
        .run(id, 'wiki-page', path, path, path, 'wiki-page', 'Index', 'unrelated detail '.repeat(id * 10000));
      db.prepare('INSERT INTO context_search_identity(rowid, path, title) VALUES (?, ?, ?)').run(id, path, 'Index');
      db.prepare('INSERT INTO context_search_features VALUES (?, ?, ?, ?, ?, ?)').run(id, '', '', 'workflow', 0, '.md');
    }
    const before = findIdentityCandidates(db, 'title : "index"*', 1);
    assert.equal(before.length, 1);
    assert.equal(before[0].path, 'a/index.md');
    db.exec("UPDATE context_search SET body = 'short'");
    assert.deepEqual(findIdentityCandidates(db, 'title : "index"*', 1), before);
    assert.deepEqual(findIdentityCandidates(db, 'title : "absent"*', 1), []);
  } finally { db.close(); }
});

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
