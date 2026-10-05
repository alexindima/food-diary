// Metadata belongs to this read transaction, never to a process-wide cache.
export function searchContextBatch(database, requests, search) {
  const previousCacheSize = database.prepare('PRAGMA cache_size').get().cache_size;
  database.exec('BEGIN');
  let failure;
  try {
    // Repeated FTS reads otherwise churn SQLite's default ~2 MiB page cache.
    // This connection-local target grows on demand and lives only for the batch.
    database.exec('PRAGMA cache_size = -65536');
    const state = Object.freeze({
      indexedDocuments: database.prepare('SELECT COUNT(*) count FROM context_search').get().count,
    });
    return {
      requestCount: requests.length,
      results: requests.map(request => search(database, request.query ?? '', Number(request.limit ?? 20), {
        module: request.module, path: request.path, changeType: request.changeType,
      }, state)),
    };
  } catch (error) {
    failure = error;
    throw error;
  } finally {
    // This is a read-only transaction: release its snapshot even when search fails.
    try {
      try { database.exec('ROLLBACK'); } catch (error) {
        if (!failure) { failure = error; throw error; }
        failure.rollbackError = error;
      }
    } finally {
      try { database.exec(`PRAGMA cache_size = ${previousCacheSize}`); } catch (error) {
        if (!failure) throw error;
        failure.cacheRestoreError = error;
      }
    }
  }
}
