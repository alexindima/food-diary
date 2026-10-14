import { existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { sqliteDatabasePath } from './code-graph-database-path.mjs';
import { queryStandaloneIndexSnapshot } from './code-graph-index-query.mjs';
import { queryStandaloneIndexCache } from './code-graph-standalone-cache.mjs';

const [root, ...argumentsList] = process.argv.slice(2);
const options = Object.fromEntries(argumentsList.map(argument => {
  const separator = argument.indexOf('=');
  return [argument.slice(2, separator), argument.slice(separator + 1)];
}));
const { index, query = '', limit: rawLimit = '12' } = options;
const databasePath = resolve(root, '.artifacts/llm-wiki/code-graph/code-graph.sqlite');
let database;
try {
  let result;
  if (!existsSync(databasePath)) result = { ready: false, index, unavailableReason: 'standalone-index-projection-missing' };
  else {
    try {
      database = new DatabaseSync(sqliteDatabasePath(databasePath), { readOnly: true });
      result = queryStandaloneIndexSnapshot(database, root, { index, query, limit: Number(rawLimit) });
    } catch (error) {
      if (options['prepare-cache'] !== 'true') throw error;
      result = { ready: false, index, unavailableReason: 'standalone-index-reader-unavailable' };
    } finally { database?.close(); database = undefined; }
  }
  if (!result.ready && options['prepare-cache'] === 'true') {
    result = queryStandaloneIndexCache(root, { index, query, limit: Number(rawLimit) });
  }
  process.stdout.write(JSON.stringify(result) + '\n');
} finally { database?.close(); }
