import assert from 'node:assert/strict';
import { test } from 'node:test';
import { DatabaseSync, backup } from 'node:sqlite';
import { mkdtempSync, rmSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { execFileSync } from 'node:child_process';
import { inspectProjectionCompleteness } from './code-graph-maintenance.mjs';

test('ordinary graph build repairs missing metadata despite an unchanged source fingerprint', async () => {
  const root = resolve(import.meta.dirname, '../..');
  const directory = mkdtempSync(join(root, '.artifacts/llm-wiki/recovery-regression-'));
  const path = join(directory, 'graph.sqlite');
  const source = new DatabaseSync(join(root, '.artifacts/llm-wiki/code-graph/code-graph.sqlite'), { readOnly: true });
  try { await backup(source, path); } finally { source.close(); }
  try {
    // First bring the private fixture to precisely the current source fingerprint.
    const build = () => JSON.parse(execFileSync(process.execPath,
      [join(import.meta.dirname, 'code-graph.mjs'), 'build', `--database=${path}`], { cwd: root, encoding: 'utf8', timeout: 120000 }));
    build();
    let db = new DatabaseSync(path);
    const count = db.prepare('SELECT COUNT(*) count FROM context_search_features').get().count;
    const original = db.prepare('SELECT * FROM context_search_features ORDER BY context_rowid LIMIT 1').get();
    db.prepare('DELETE FROM context_search_features WHERE context_rowid = ?').run(original.context_rowid);
    assert.equal(inspectProjectionCompleteness(db)[0].kind, 'missing-projection-features');
    db.close();
    build();
    db = new DatabaseSync(path);
    try {
      assert.equal(inspectProjectionCompleteness(db).length, 0);
      assert.equal(db.prepare('SELECT COUNT(*) count FROM context_search_features').get().count, count);
      assert.deepEqual(db.prepare('SELECT * FROM context_search_features WHERE context_rowid = ?').get(original.context_rowid), original);
    } finally { db.close(); }
  } finally { rmSync(directory, { recursive: true, force: true }); }
});
