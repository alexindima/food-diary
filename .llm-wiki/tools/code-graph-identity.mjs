// Candidate recall must not penalize a title because its document body is long.
export function findIdentityCandidates(database, match, limit, diversePaths = false) {
  if (diversePaths) {
    return database.prepare(`
      WITH matches AS MATERIALIZED (
        SELECT search.rowid rowId, search.record_type recordType, search.record_key recordKey, search.path,
          search.source_path sourcePath, search.category, search.title,
          features.layer, features.module, features.role, features.is_test isTest,
          features.extension, bm25(context_search_identity, 6.0, 4.0) lexicalRank
        FROM context_search_identity
        JOIN context_search search ON search.rowid = context_search_identity.rowid
        JOIN context_search_features features ON features.context_rowid = search.rowid
        WHERE context_search_identity MATCH ?
      ), representatives AS (
        SELECT *, row_number() OVER (PARTITION BY path ORDER BY lexicalRank, rowId) pathRank FROM matches
      )
      SELECT * FROM representatives WHERE pathRank = 1 ORDER BY lexicalRank, path, rowId LIMIT ?
    `).all(match, limit);
  }
  return database.prepare(`
    SELECT search.record_type recordType, search.record_key recordKey, search.path,
      search.source_path sourcePath, search.category, search.title,
      features.layer, features.module, features.role, features.is_test isTest,
      features.extension, bm25(context_search_identity, 6.0, 4.0) lexicalRank
    FROM context_search_identity
    JOIN context_search search ON search.rowid = context_search_identity.rowid
    JOIN context_search_features features ON features.context_rowid = search.rowid
    WHERE context_search_identity MATCH ?
    ORDER BY lexicalRank, search.path, search.rowid
    LIMIT ?
  `).all(match, limit);
}
