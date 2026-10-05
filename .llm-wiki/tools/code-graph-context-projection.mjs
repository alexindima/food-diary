import { inspectProjectionCompleteness } from './code-graph-maintenance.mjs';

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
