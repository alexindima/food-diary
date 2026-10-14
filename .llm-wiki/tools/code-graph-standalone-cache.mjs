import { createHash, randomUUID } from 'node:crypto';
import { existsSync, mkdirSync, readFileSync, readdirSync, renameSync, rmSync, statSync } from 'node:fs';
import { resolve } from 'node:path';
import { DatabaseSync } from 'node:sqlite';
import { sqliteDatabasePath } from './code-graph-database-path.mjs';
import { readStandaloneIndexSource, refreshStandaloneIndexes, queryStandaloneIndexSnapshot } from './code-graph-index-query.mjs';

function pruneCaches(directory, index, current) {
  const pattern = new RegExp(`^${index}-[a-f0-9]{32}\\.sqlite$`);
  const previous = readdirSync(directory, { withFileTypes: true }).filter(entry => entry.isFile() && pattern.test(entry.name))
    .map(entry => resolve(directory, entry.name)).filter(path => path !== current);
  const newest = previous.map(path => { try { return { path, time: statSync(path).mtimeMs }; } catch { return null; } })
    .filter(Boolean).sort((left, right) => right.time - left.time);
  for (const { path } of newest.slice(1)) {
    // Retain the current generation and one predecessor. A Windows reader can
    // keep an older file busy; a later successful publication retries pruning.
    try { rmSync(path); } catch { /* Derived cache retention is best effort. */ }
  }
}

// A derived, immutable projection of one published source. This Node writer
// never opens the repository's code graph for writing or requires source parsing.
export function queryStandaloneIndexCache(root, options) {
  if (!Number.isInteger(options.limit) || options.limit < 1 || options.limit > 50) throw new Error('Compiled-index limit must be between 1 and 50.');
  const source = readStandaloneIndexSource(root, options.index);
  if (!source) return { ready: false, index: options.index, unavailableReason: 'compiled-source-missing' };
  const cacheKey = createHash('sha256').update(readFileSync(new URL('./code-graph-index-query.mjs', import.meta.url)))
    .update(readFileSync(import.meta.filename))
    .update(`${process.versions.node}\0${process.versions.unicode}\0${options.index}\0${source.contentHash}`).digest('hex');
  const directory = resolve(root, '.artifacts/llm-wiki/standalone-index-cache');
  mkdirSync(directory, { recursive: true });
  const target = resolve(directory, `${options.index}-${cacheKey.slice(0, 32)}.sqlite`);
  const readCache = () => {
    let db;
    try {
      db = new DatabaseSync(sqliteDatabasePath(target), { readOnly: true });
      if (db.prepare("SELECT value FROM metadata WHERE key='standalone_cache_key'").get()?.value !== cacheKey) return null;
      const result = queryStandaloneIndexSnapshot(db, root, options);
      return result.ready ? { ...result, cacheDatabasePath: target } : null;
    } catch { return null; }
    finally { db?.close(); }
  };
  if (existsSync(target)) {
    const cached = readCache();
    if (cached) return cached;
    // Preserve an unreadable derived cache for diagnosis; never quarantine the
    // shared production graph. An active reader may instead require a retry.
    renameSync(target, `${target}.corrupt-${randomUUID().slice(0, 8)}`);
  }
  const temporary = `${target}.${process.pid}-${randomUUID().slice(0, 8)}.tmp`;
  let db;
  try {
    db = new DatabaseSync(sqliteDatabasePath(temporary));
    db.exec('CREATE TABLE metadata(key TEXT PRIMARY KEY,value TEXT NOT NULL); BEGIN IMMEDIATE');
    refreshStandaloneIndexes(db, root, [options.index]);
    db.prepare('INSERT INTO metadata VALUES (?,?)').run('standalone_cache_key', cacheKey);
    db.exec('COMMIT');
    const result = queryStandaloneIndexSnapshot(db, root, options);
    if (!result.ready || result.index.compiledIndex.sourceHash !== source.contentHash) {
      return { ready: false, index: options.index, unavailableReason: 'standalone-index-source-changed' };
    }
    db.close(); db = undefined;
    try { renameSync(temporary, target); }
    catch (error) {
      // Concurrent equivalent writers can publish this exact content first.
      const cached = existsSync(target) ? readCache() : null;
      if (cached) return cached;
      throw error;
    }
    pruneCaches(directory, options.index, target);
    return { ...result, cacheDatabasePath: target };
  } finally {
    db?.close();
    // Only the unique file created by this invocation is eligible for removal.
    if (existsSync(temporary)) rmSync(temporary);
  }
}
