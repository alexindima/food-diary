import assert from 'node:assert/strict';
import { test } from 'node:test';
import { DatabaseSync } from 'node:sqlite';
import { findLexicalCandidates, findWeightedIdentityCandidates } from './code-graph-candidates.mjs';
import { findIdentityCandidates } from './code-graph-identity.mjs';

test('deferred content hydration preserves records, path diversity, ties and missing-feature exclusion', () => {
  const db = new DatabaseSync(':memory:');
  try {
    db.exec(`CREATE VIRTUAL TABLE context_search USING fts5(record_type UNINDEXED,record_key UNINDEXED,
      path,source_path UNINDEXED,category UNINDEXED,title,body);
      CREATE VIRTUAL TABLE context_search_identity USING fts5(path,title);
      CREATE TABLE context_search_features(context_rowid INTEGER PRIMARY KEY,path,layer,module,role,is_test,extension);`);
    const insert = db.prepare('INSERT INTO context_search VALUES (?,?,?,?,?,?,?)');
    for (let index = 0; index < 160; index++) {
      const path = index < 140 ? 'Area/StarOrchard.cs' : `Area/StarOrchard${index}.cs`;
      const type = index % 2 === 0 ? 'code' : 'wiki-page';
      const row = insert.run(type, String(index), path, path, 'fixture', 'star orchard',
        index % 3 === 0 ? 'star orchard ' + 'unrelated detail '.repeat(10000) : 'star orchard');
      db.prepare('INSERT INTO context_search_identity(rowid,path,title) VALUES (?,?,?)').run(row.lastInsertRowid,path,'star orchard');
      // A missing feature must not consume a bounded candidate slot.
      if (index !== 159) db.prepare('INSERT INTO context_search_features VALUES (?,?,?,?,?,?,?)')
        .run(row.lastInsertRowid,path,'application','Area','handler',0,'.cs');
    }
    const readAll = (identity, match) => db.prepare(`
      SELECT search.record_type recordType, search.record_key recordKey, search.path, search.source_path sourcePath,
        search.category, search.title, features.layer, features.module, features.role, features.is_test isTest,
        features.extension, bm25(${identity ? 'context_search_identity,6.0,4.0' : 'context_search,0.0,0.0,6.0,0.0,0.0,4.0,1.0'}) lexicalRank,
        search.rowid rowId
      FROM ${identity ? 'context_search_identity JOIN context_search search ON search.rowid=context_search_identity.rowid' : 'context_search search'}
      JOIN context_search_features features ON features.context_rowid=search.rowid
      WHERE ${identity ? 'context_search_identity' : 'context_search'} MATCH ?
      ORDER BY lexicalRank, search.path, search.rowid
    `).all(match);
    const distinct = rows => [...new Map(rows.toReversed().map(row => [row.path,row])).values()]
      .sort((a,b) => a.lexicalRank-b.lexicalRank || a.path.localeCompare(b.path) || a.rowId-b.rowId);
    const withoutOrdinal = ({rowId,...row}) => row;
    for (const limit of [1,2,10,100]) {
      const match = '"star"* OR "orchard"*';
      const all = readAll(false,match);
      const initial = all.slice(0,limit);
      const initialPaths = new Set(initial.map(row => row.path));
      const expected = [...initial,...distinct(all).slice(0,limit).filter(row => !initialPaths.has(row.path))].map(withoutOrdinal);
      assert.deepEqual(findLexicalCandidates(db,match,limit).map(row => ({...row})),expected);
      const named = 'title : "star"* OR path : "orchard"*';
      assert.deepEqual(findWeightedIdentityCandidates(db,named,limit).map(row => ({...row})),readAll(false,named).slice(0,limit).map(withoutOrdinal));
      const identity = readAll(true,named);
      assert.deepEqual(findIdentityCandidates(db,named,limit).map(row => ({...row})),identity.slice(0,limit).map(withoutOrdinal));
      assert.deepEqual(findIdentityCandidates(db,named,limit,true).map(row => ({...row})),distinct(identity).slice(0,limit).map(row => ({...row,pathRank:1})));
    }
    for (const find of [findLexicalCandidates,findWeightedIdentityCandidates,findIdentityCandidates]) {
      assert.deepEqual(find(db,'"absent"*',2),[]);
    }
  } finally { db.close(); }
});
