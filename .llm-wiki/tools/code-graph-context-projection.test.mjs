import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';
import { test } from 'node:test';
import { createContextSourceReader, replaceContextSearchRecords } from './code-graph-context-projection.mjs';

test('indexed source reads preserve duplicate symbols, binary ordering, empty files and live file changes', () => {
  const db = new DatabaseSync(':memory:');
  try {
    db.exec(`CREATE TABLE symbols(file_id INTEGER,name TEXT);
      CREATE INDEX ix_symbols_file ON symbols(file_id);
      CREATE TABLE file_tokens(file_id INTEGER,token TEXT,PRIMARY KEY(file_id,token)) WITHOUT ROWID;
      INSERT INTO symbols VALUES (1,'z'),(1,'Alpha'),(2,'unrelated'),(1,'Alpha'),(1,'alpha'),(1,'ёж');
      INSERT INTO file_tokens VALUES (1,'z'),(2,'unrelated'),(1,'Alpha'),(1,'alpha'),(1,'ёж');`);
    const reader = createContextSourceReader(db);
    assert.deepEqual(reader.symbols(1), ['Alpha', 'Alpha', 'alpha', 'z', 'ёж']);
    assert.deepEqual(reader.tokens(1), ['Alpha', 'alpha', 'z', 'ёж']);
    assert.deepEqual(reader.symbols(3), []);
    assert.deepEqual(reader.tokens(3), []);
    // A reader must not reuse a prior empty result after the writer changes a file.
    db.prepare('INSERT INTO file_tokens VALUES (?,?)').run(3, 'новый');
    assert.deepEqual(reader.tokens(3), ['новый']);
    db.prepare('DELETE FROM symbols WHERE file_id=?').run(1);
    assert.deepEqual(reader.symbols(1), []);
    const statements = [];
    const observed = createContextSourceReader({ prepare(sql) { statements.push(sql); return db.prepare(sql); } });
    assert.deepEqual(observed.tokens(2), ['unrelated']);
    for (const sql of statements) {
      const plan = db.prepare('EXPLAIN QUERY PLAN ' + sql).all(1).map(row => row.detail).join('\n');
      assert.match(plan, /SEARCH .* (?:INDEX|PRIMARY KEY).*file_id=\?/);
      assert.doesNotMatch(plan, /SCAN (?:symbols|file_tokens)/);
    }
  } finally { db.close(); }
});

const hash = value => createHash('sha256').update(JSON.stringify(value)).digest('hex');
const fixture = () => {
  const database = new DatabaseSync(':memory:');
  database.exec(`CREATE TABLE metadata(key TEXT PRIMARY KEY, value TEXT);
    CREATE VIRTUAL TABLE context_search USING fts5(record_type UNINDEXED,record_key UNINDEXED,
      path,source_path UNINDEXED,category UNINDEXED,title,body);
    CREATE VIRTUAL TABLE context_search_identity USING fts5(path,title);
    CREATE TABLE context_search_features(context_rowid INTEGER PRIMARY KEY,record_type,path,layer,module,role,is_test,extension);`);
  return database;
};
const descriptor = (key, body = 'star orchard') => {
  const value = ['code', key, 'Area/Shared.cs', 'Area/Shared.cs', 'fixture', 'звёздный orchard', body];
  return { fingerprint: hash(value), value };
};
const refresh = (database, records, environmentFingerprint = 'environment', failAt = -1) => {
  database.exec('BEGIN IMMEDIATE');
  try {
    const fingerprint = hash([environmentFingerprint, records.map(record => record.fingerprint)]);
    const result = replaceContextSearchRecords(database, records, { fingerprint, environmentFingerprint }, (record, rowId) => {
      database.prepare('INSERT INTO context_search(rowid,record_type,record_key,path,source_path,category,title,body) VALUES (?,?,?,?,?,?,?,?)')
        .run(rowId, ...record.value);
      if (rowId === failAt) throw new Error('injected interrupted projection');
      database.prepare('INSERT INTO context_search_identity(rowid,path,title) VALUES (?,?,?)').run(rowId, record.value[2], record.value[5]);
      database.prepare('INSERT INTO context_search_features VALUES (?,?,?,?,?,?,?,?)')
        .run(rowId, record.value[0], record.value[2], 'application', 'Area', 'handler', 0, '.cs');
    });
    database.prepare('INSERT OR REPLACE INTO metadata VALUES (?,?)').run('context_search_fingerprint', fingerprint);
    database.exec('COMMIT');
    return result;
  } catch (error) { database.exec('ROLLBACK'); throw error; }
};
const snapshot = database => ({
  records: database.prepare('SELECT rowid,* FROM context_search ORDER BY rowid').all(),
  identity: database.prepare('SELECT rowid,* FROM context_search_identity ORDER BY rowid').all(),
  features: database.prepare('SELECT * FROM context_search_features ORDER BY context_rowid').all(),
  matches: database.prepare(`SELECT rowid,record_key,bm25(context_search) score FROM context_search
    WHERE context_search MATCH '"orchard"* OR "star"*' ORDER BY score,path,rowid`).all(),
});

test('incremental projection matches full replacement including dense ordinals, ties and FTS scores', () => {
  const db = fixture();
  const reference = fixture();
  try {
    let records = [descriptor('a'), descriptor('b'), descriptor('c', 'star orchard ' + 'detail '.repeat(10000))];
    const compare = () => {
      reference.prepare("DELETE FROM metadata WHERE key='context_search_record_cache'").run();
      refresh(reference, records);
      assert.deepEqual(snapshot(db), snapshot(reference));
    };
    assert.equal(refresh(db, records).mode, 'full');
    compare();
    assert.deepEqual(refresh(db, records), { mode: 'incremental', writtenRecords: 0, deletedRecords: 0 });
    records = [records[0], descriptor('b', 'orchard updated'), records[2]];
    assert.deepEqual(refresh(db, records), { mode: 'incremental', writtenRecords: 1, deletedRecords: 1 });
    compare();
    records = [descriptor('inserted'), ...records];
    assert.equal(refresh(db, records).writtenRecords, 4);
    compare();
    records.splice(1, 1);
    assert.equal(refresh(db, records).writtenRecords, 2);
    compare();
    records = records.toReversed();
    refresh(db, records);
    compare();
    records = [];
    assert.equal(refresh(db, records).deletedRecords, 3);
    compare();
  } finally { db.close(); reference.close(); }
});

test('legacy, stale or invalid cache and incomplete projections force complete recovery', () => {
  const db = fixture();
  const reference = fixture();
  const records = [descriptor('a'), descriptor('b'), descriptor('c')];
  try {
    refresh(reference, records);
    const corruptions = [
      () => db.prepare("DELETE FROM metadata WHERE key='context_search_record_cache'").run(),
      () => db.prepare("UPDATE metadata SET value='invalid JSON' WHERE key='context_search_record_cache'").run(),
      () => db.prepare("UPDATE metadata SET value='stale' WHERE key='context_search_fingerprint'").run(),
      () => db.prepare("UPDATE metadata SET value=json_set(value,'$.schemaVersion',2) WHERE key='context_search_record_cache'").run(),
      () => db.prepare("UPDATE metadata SET value=json_set(value,'$.records[0]','invalid') WHERE key='context_search_record_cache'").run(),
      () => db.prepare('DELETE FROM context_search_features WHERE context_rowid=2').run(),
      () => db.prepare('INSERT INTO context_search_features VALUES (99,?,?,?,?,?,?,?)').run('code','orphan','other','other','other',0,'.cs'),
      () => db.prepare('DELETE FROM context_search_identity WHERE rowid=2').run(),
      () => db.prepare('UPDATE context_search SET rowid=4 WHERE rowid=2').run(),
    ];
    for (const corrupt of corruptions) {
      refresh(db, records);
      corrupt();
      assert.deepEqual(refresh(db, records), { mode: 'full', writtenRecords: 3, deletedRecords: 3 });
      assert.deepEqual(snapshot(db), snapshot(reference));
    }
    assert.equal(refresh(db, records, 'changed ownership or schema').mode, 'full');
    assert.deepEqual(snapshot(db), snapshot(reference));
  } finally { db.close(); reference.close(); }
});

test('interrupted incremental writes roll back all mirrors and cache state before a successful retry', () => {
  const db = fixture();
  try {
    const records = [descriptor('a'), descriptor('b'), descriptor('c')];
    refresh(db, records);
    const before = snapshot(db);
    const metadata = db.prepare('SELECT * FROM metadata ORDER BY key').all();
    const changed = [descriptor('a', 'orchard changed'), records[1], descriptor('c', 'star changed')];
    assert.throws(() => refresh(db, changed, 'environment', 3), /injected interrupted projection/);
    assert.deepEqual(snapshot(db), before);
    assert.deepEqual(db.prepare('SELECT * FROM metadata ORDER BY key').all(), metadata);
    assert.deepEqual(refresh(db, changed), { mode: 'incremental', writtenRecords: 2, deletedRecords: 2 });
  } finally { db.close(); }
});
