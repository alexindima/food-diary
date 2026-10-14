import { inspectProjectionCompleteness } from './code-graph-maintenance.mjs';

// Both lookups use existing file_id indexes. Read source features only when a
// code record is rendered, including on full rebuilds, without hydrating maps
// of every token and symbol in the repository.
export function createContextSourceReader(database) {
  const symbols = database.prepare('SELECT name FROM symbols WHERE file_id = ? ORDER BY name');
  const tokens = database.prepare('SELECT token FROM file_tokens WHERE file_id = ? ORDER BY token');
  return {
    symbols: fileId => symbols.all(fileId).map(row => row.name),
    tokens: fileId => tokens.all(fileId).map(row => row.token),
  };
}

// Repair only missing mirrors of an unchanged, densely ordered projection.
// The caller owns the transaction and provides the same complete feature
// derivation as a full refresh. Any uncertain cache or structural damage falls
// back to the ordinary rebuild; healthy FTS bodies are never rewritten here.
export function repairMissingContextMirrors(database, records, { fingerprint, environmentFingerprint }, featuresFor) {
  const metadata = database.prepare("SELECT value FROM metadata WHERE key='context_search_fingerprint'").get()?.value;
  if (metadata !== fingerprint) return null;
  let previous;
  try { previous = JSON.parse(database.prepare("SELECT value FROM metadata WHERE key='context_search_record_cache'").get()?.value); }
  catch { return null; }
  if (previous?.schemaVersion !== 1 || previous.fingerprint !== fingerprint || previous.environmentFingerprint !== environmentFingerprint
      || !Array.isArray(previous.records) || previous.records.length !== records.length
      || previous.records.some((value, index) => typeof value !== 'string' || !/^[a-f0-9]{64}$/.test(value) || value !== records[index].fingerprint)) return null;
  const current = database.prepare('SELECT COUNT(*) count, MIN(rowid) first, MAX(rowid) last FROM context_search').get();
  if (current.count !== records.length || current.count === 0 || current.first !== 1 || current.last !== current.count) return null;
  if (inspectProjectionCompleteness(database).some(finding => finding.kind !== 'missing-projection-features')) return null;
  if (database.prepare(`SELECT 1 FROM context_search_identity identity LEFT JOIN context_search search ON search.rowid=identity.rowid
      WHERE search.rowid IS NULL LIMIT 1`).get()) return null;
  const missingFeatures = database.prepare(`SELECT search.rowid id, search.record_type recordType, search.path
    FROM context_search search LEFT JOIN context_search_features features ON features.context_rowid=search.rowid
    WHERE features.context_rowid IS NULL ORDER BY search.rowid`).all();
  const missingIdentity = database.prepare(`SELECT search.rowid id, search.path, search.title
    FROM context_search search LEFT JOIN context_search_identity identity ON identity.rowid=search.rowid
    WHERE identity.rowid IS NULL ORDER BY search.rowid`).all();
  const insertFeatures = database.prepare(`INSERT INTO context_search_features(context_rowid,record_type,path,layer,module,role,is_test,extension)
    VALUES (?,?,?,?,?,?,?,?)`);
  const insertIdentity = database.prepare('INSERT INTO context_search_identity(rowid,path,title) VALUES (?,?,?)');
  for (const row of missingFeatures) {
    const features = featuresFor(row.path, row.recordType);
    insertFeatures.run(row.id,row.recordType,row.path,features.layer,features.module,features.role,features.isTest,features.extension);
  }
  for (const row of missingIdentity) insertIdentity.run(row.id,row.path,row.title);
  return { mode: 'mirror-repair', repairedFeatures: missingFeatures.length, repairedIdentity: missingIdentity.length };
}

// The graph writer owns the transaction. Cache state is published with the
// projection, so a failed write cannot make a later refresh skip missing rows.
export function replaceContextSearchRecords(database, records, { fingerprint, environmentFingerprint }, writeRecord) {
  const metadata = database.prepare("SELECT value FROM metadata WHERE key='context_search_fingerprint'").get()?.value;
  const cached = database.prepare("SELECT value FROM metadata WHERE key='context_search_record_cache'").get()?.value;
  let previous;
  try { previous = cached ? JSON.parse(cached) : null; } catch { previous = null; }
  const current = database.prepare('SELECT COUNT(*) count, MIN(rowid) first, MAX(rowid) last FROM context_search').get();
  const identity = database.prepare('SELECT COUNT(*) count, MIN(rowid) first, MAX(rowid) last FROM context_search_identity').get();
  const incremental = previous?.schemaVersion === 1 && typeof metadata === 'string' && previous.fingerprint === metadata
    && previous.environmentFingerprint === environmentFingerprint
    && Array.isArray(previous.records) && previous.records.length === current.count
    && previous.records.every(value => typeof value === 'string' && /^[a-f0-9]{64}$/.test(value))
    && current.count > 0 && current.first === 1 && current.last === current.count
    && identity.count === current.count && identity.first === 1 && identity.last === current.count
    && inspectProjectionCompleteness(database).length === 0;
  let writtenRecords = 0;
  let deletedRecords = 0;
  if (!incremental) {
    database.exec('DELETE FROM context_search_features; DELETE FROM context_search_identity; DELETE FROM context_search');
    deletedRecords = current.count;
  }
  const deleteFeatures = database.prepare('DELETE FROM context_search_features WHERE context_rowid = ?');
  const deleteIdentity = database.prepare('DELETE FROM context_search_identity WHERE rowid = ?');
  const deleteRecord = database.prepare('DELETE FROM context_search WHERE rowid = ?');
  for (const [index, record] of records.entries()) {
    if (incremental && previous.records[index] === record.fingerprint) continue;
    const rowId = index + 1;
    if (incremental && rowId <= current.count) {
      deleteFeatures.run(rowId);
      deleteIdentity.run(rowId);
      deleteRecord.run(rowId);
      deletedRecords++;
    }
    // Canonical dense ordinals preserve tie breaking when records move or vanish.
    writeRecord(record, rowId);
    writtenRecords++;
  }
  if (incremental && current.count > records.length) {
    database.prepare('DELETE FROM context_search_features WHERE context_rowid > ?').run(records.length);
    database.prepare('DELETE FROM context_search_identity WHERE rowid > ?').run(records.length);
    database.prepare('DELETE FROM context_search WHERE rowid > ?').run(records.length);
    deletedRecords += current.count - records.length;
  }
  database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)')
    .run('context_search_record_cache', JSON.stringify({ schemaVersion: 1, fingerprint, environmentFingerprint,
      records: records.map(record => record.fingerprint) }));
  return { mode: incremental ? 'incremental' : 'full', writtenRecords, deletedRecords };
}
