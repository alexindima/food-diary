// Metadata belongs to this read transaction, never to a process-wide cache.
export function searchContextBatch(database, requests, search) {
  database.exec('BEGIN');
  let failure;
  try {
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
    try { database.exec('ROLLBACK'); } catch (error) {
      if (!failure) throw error;
      failure.rollbackError = error;
    }
  }
}
