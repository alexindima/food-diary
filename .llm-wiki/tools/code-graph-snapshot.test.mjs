import assert from 'node:assert/strict';
import { test } from 'node:test';
import { DatabaseSync } from 'node:sqlite';
import { existsSync, mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { snapshotCodeGraph } from './code-graph-snapshot.mjs';

test('snapshot includes committed WAL rows and survives the source WAL disappearing', async () => {
  const directory = mkdtempSync(join(tmpdir(), 'wiki-snapshot-'));
  const sourcePath = join(directory, 'source.sqlite');
  const destinationPath = join(directory, 'snapshot', 'graph.sqlite');
  let source = new DatabaseSync(sourcePath);
  try {
    source.exec('PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; CREATE TABLE entries (value TEXT);');
    source.exec("INSERT INTO entries VALUES ('committed in WAL');");
    assert.ok(existsSync(`${sourcePath}-wal`));
    await snapshotCodeGraph(sourcePath, destinationPath);
    source.close();
    source = null;
    assert.equal(existsSync(`${sourcePath}-wal`), false);
    const snapshot = new DatabaseSync(destinationPath, { readOnly: true });
    try {
      assert.equal(snapshot.prepare('SELECT value FROM entries').get().value, 'committed in WAL');
      assert.equal(snapshot.prepare('PRAGMA integrity_check').get().integrity_check, 'ok');
    } finally { snapshot.close(); }
    // A subsequent snapshot also works after SQLite removes the sidecar.
    await snapshotCodeGraph(sourcePath, join(directory, 'after-checkpoint.sqlite'));
  } finally {
    source?.close();
    rmSync(directory, { recursive: true, force: true });
  }
});

test('missing source fails without publishing an empty snapshot', async () => {
  const directory = mkdtempSync(join(tmpdir(), 'wiki-snapshot-'));
  const destinationPath = join(directory, 'snapshot.sqlite');
  try {
    await assert.rejects(snapshotCodeGraph(join(directory, 'missing.sqlite'), destinationPath));
    assert.equal(existsSync(destinationPath), false);
  } finally { rmSync(directory, { recursive: true, force: true }); }
});
