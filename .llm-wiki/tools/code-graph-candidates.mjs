// Sort compact projection rows before reading FTS content. Asking the virtual
// table for a path also materializes its document, including large source bodies.
// Preserve both the original record pool and the additional distinct paths.
export function findLexicalCandidates(database, match, limit) {
  return database.prepare(`
    WITH lexical_matches AS MATERIALIZED (
      SELECT features.path, context_search.rowid sourceOrdinal,
        bm25(context_search, 0.0, 0.0, 6.0, 0.0, 0.0, 4.0, 1.0) lexicalRank
      FROM context_search
      JOIN context_search_features features ON features.context_rowid = context_search.rowid
      WHERE context_search MATCH ?
    ), initial_candidates AS MATERIALIZED (
      SELECT * FROM lexical_matches ORDER BY lexicalRank, path, sourceOrdinal LIMIT ?
    ), distinct_paths AS (
      SELECT sourceOrdinal, path, lexicalRank,
        ROW_NUMBER() OVER (PARTITION BY path ORDER BY lexicalRank, sourceOrdinal) pathOrdinal
      FROM lexical_matches
    ), distinct_candidates AS (
      SELECT sourceOrdinal FROM distinct_paths WHERE pathOrdinal = 1 ORDER BY lexicalRank, path LIMIT ?
    ), pooled_candidates AS (
      SELECT 0 poolOrdinal, * FROM initial_candidates
      UNION ALL
      SELECT 1 poolOrdinal, lexical_matches.* FROM lexical_matches
      JOIN distinct_candidates USING (sourceOrdinal)
      WHERE lexical_matches.path NOT IN (SELECT path FROM initial_candidates)
    )
    SELECT context_search.record_type recordType, record_key recordKey, context_search.path, source_path sourcePath,
      category, title, features.layer, features.module, features.role, features.is_test isTest,
      features.extension, pooled_candidates.lexicalRank
    FROM pooled_candidates
    -- Keep the bounded pool outermost: an unrestricted FTS scan hydrates every document.
    CROSS JOIN context_search ON context_search.rowid = sourceOrdinal
    JOIN context_search_features features ON features.context_rowid = sourceOrdinal
    ORDER BY poolOrdinal, lexicalRank, pooled_candidates.path, sourceOrdinal
  `).all(match, limit, limit);
}

export function findWeightedIdentityCandidates(database, match, limit) {
  return database.prepare(`
    WITH recalled AS MATERIALIZED (
      SELECT context_search.rowid sourceOrdinal, features.path,
        bm25(context_search, 0.0, 0.0, 6.0, 0.0, 0.0, 4.0, 1.0) lexicalRank
      FROM context_search
      JOIN context_search_features features ON features.context_rowid = context_search.rowid
      WHERE context_search MATCH ?
      ORDER BY lexicalRank, features.path, sourceOrdinal LIMIT ?
    )
    SELECT search.record_type recordType, search.record_key recordKey, search.path, search.source_path sourcePath,
      search.category, search.title, features.layer, features.module, features.role, features.is_test isTest,
      features.extension, recalled.lexicalRank
    FROM recalled
    JOIN context_search search ON search.rowid = sourceOrdinal
    JOIN context_search_features features ON features.context_rowid = sourceOrdinal
    ORDER BY lexicalRank, recalled.path, sourceOrdinal
  `).all(match, limit);
}
