import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, extname, resolve } from 'node:path';
import { DatabaseSync } from 'node:sqlite';

const repositoryRoot = resolve(import.meta.dirname, '../..');
const defaultDatabasePath = resolve(repositoryRoot, '.artifacts/llm-wiki/code-graph/code-graph.sqlite');
const parserVersion = '1';

function gitPaths() {
  const output = execFileSync('git', ['-C', repositoryRoot, 'ls-files', '-z', '--cached', '--others', '--exclude-standard'], {
    encoding: 'utf8',
    maxBuffer: 64 * 1024 * 1024,
  });
  return output
    .split('\0')
    .map((path) => path.replaceAll('\\', '/'))
    .filter(Boolean)
    .filter((path) => /\.(?:cs|csproj|ts|html)$/.test(path))
    .filter((path) => !/(^|\/)(?:bin|obj|node_modules|\.artifacts|Migrations|TestResults)(\/|$)/.test(path))
    .filter((path) => !/\.(?:Designer|g)\.cs$/.test(path));
}

function sha256(text) {
  return createHash('sha256').update(text).digest('hex');
}

function lineAt(text, offset) {
  let line = 1;
  for (let index = 0; index < offset; index += 1) if (text.charCodeAt(index) === 10) line += 1;
  return line;
}

function languageOf(path) {
  const extension = extname(path);
  if (extension === '.cs') return 'csharp';
  if (extension === '.csproj') return 'msbuild';
  if (extension === '.html') return 'html';
  return 'typescript';
}

function extract(path, text) {
  const language = languageOf(path);
  const symbols = [];
  const tokens = new Set();
  const projectReferences = [];
  const addMatches = (pattern, kindGroup = 'kind', nameGroup = 'name') => {
    for (const match of text.matchAll(pattern)) {
      symbols.push({ kind: match.groups[kindGroup], name: match.groups[nameGroup], line: lineAt(text, match.index) });
    }
  };

  if (language === 'csharp') {
    addMatches(/^[ \t]*(?:(?:public|internal|protected|private|file)\s+)?(?:(?:sealed|abstract|static|partial|readonly|required)\s+)*(?<kind>class|interface|record(?:\s+struct)?|struct|enum)\s+(?<name>[A-Za-z_]\w*)/gm);
    for (const match of text.matchAll(/^[ \t]*(?:public|internal|protected|private)\s+(?:static\s+|virtual\s+|override\s+|async\s+|sealed\s+|new\s+)*(?:[\w.<>,?\[\]]+\s+)+(?<name>[A-Za-z_]\w*)\s*\(/gm)) {
      symbols.push({ kind: 'method', name: match.groups.name, line: lineAt(text, match.index) });
    }
  } else if (language === 'typescript') {
    addMatches(/^[ \t]*(?:export\s+)?(?:default\s+)?(?<kind>class|interface|type|enum|function)\s+(?<name>[A-Za-z_$][\w$]*)/gm);
    for (const match of text.matchAll(/selector\s*:\s*['"](?<name>[^'"]+)['"]/g)) {
      symbols.push({ kind: 'selector', name: match.groups.name, line: lineAt(text, match.index) });
    }
  } else if (language === 'html') {
    for (const match of text.matchAll(/<(?<name>(?:fd|app)-[a-z0-9-]+)/g)) {
      tokens.add(match.groups.name);
    }
  } else if (language === 'msbuild') {
    for (const match of text.matchAll(/<ProjectReference\s+Include="(?<name>[^"]+)"/g)) {
      projectReferences.push(match.groups.name.replaceAll('\\', '/'));
    }
  }

  for (const match of text.matchAll(/\b[A-Za-z_$][A-Za-z0-9_$]{2,}\b/g)) {
    const token = match[0];
    if (/^[A-ZI]/.test(token) || token.endsWith('Async')) tokens.add(token);
  }
  return { language, symbols, tokens: [...tokens], projectReferences };
}

function openDatabase(databasePath) {
  mkdirSync(dirname(databasePath), { recursive: true });
  const database = new DatabaseSync(databasePath);
  database.exec(`
    PRAGMA journal_mode = WAL;
    PRAGMA synchronous = NORMAL;
    PRAGMA foreign_keys = ON;
    CREATE TABLE IF NOT EXISTS metadata(key TEXT PRIMARY KEY, value TEXT NOT NULL);
    CREATE TABLE IF NOT EXISTS files(
      id INTEGER PRIMARY KEY,
      path TEXT NOT NULL UNIQUE,
      language TEXT NOT NULL,
      size INTEGER NOT NULL,
      mtime_ms REAL NOT NULL,
      content_hash TEXT NOT NULL
    );
    CREATE TABLE IF NOT EXISTS symbols(
      id INTEGER PRIMARY KEY,
      file_id INTEGER NOT NULL REFERENCES files(id) ON DELETE CASCADE,
      kind TEXT NOT NULL,
      name TEXT NOT NULL,
      line INTEGER NOT NULL
    );
    CREATE TABLE IF NOT EXISTS file_tokens(
      file_id INTEGER NOT NULL REFERENCES files(id) ON DELETE CASCADE,
      token TEXT NOT NULL,
      PRIMARY KEY(file_id, token)
    ) WITHOUT ROWID;
    CREATE TABLE IF NOT EXISTS project_references(
      file_id INTEGER NOT NULL REFERENCES files(id) ON DELETE CASCADE,
      target_path TEXT NOT NULL,
      PRIMARY KEY(file_id, target_path)
    ) WITHOUT ROWID;
    CREATE INDEX IF NOT EXISTS ix_symbols_name ON symbols(name COLLATE NOCASE);
    CREATE INDEX IF NOT EXISTS ix_symbols_file ON symbols(file_id);
    CREATE INDEX IF NOT EXISTS ix_tokens_token ON file_tokens(token COLLATE NOCASE);
  `);
  return database;
}

function build(database, force = false) {
  const started = performance.now();
  const knownPaths = new Set(gitPaths());
  const existing = new Map(database.prepare('SELECT id, path, size, mtime_ms, content_hash FROM files').all().map((item) => [item.path, item]));
  const deleteFile = database.prepare('DELETE FROM files WHERE id = ?');
  const insertFile = database.prepare('INSERT INTO files(path, language, size, mtime_ms, content_hash) VALUES (?, ?, ?, ?, ?)');
  const insertSymbol = database.prepare('INSERT INTO symbols(file_id, kind, name, line) VALUES (?, ?, ?, ?)');
  const insertToken = database.prepare('INSERT OR IGNORE INTO file_tokens(file_id, token) VALUES (?, ?)');
  const insertReference = database.prepare('INSERT OR IGNORE INTO project_references(file_id, target_path) VALUES (?, ?)');
  let scanned = 0;
  let updated = 0;
  let unchanged = 0;
  let removed = 0;

  database.exec('BEGIN IMMEDIATE');
  try {
    for (const [path, row] of existing) {
      if (!knownPaths.has(path)) {
        deleteFile.run(row.id);
        removed += 1;
      }
    }
    for (const path of knownPaths) {
      const absolutePath = resolve(repositoryRoot, path);
      const stat = statSync(absolutePath);
      const prior = existing.get(path);
      if (!force && prior && prior.size === stat.size && Math.abs(prior.mtime_ms - stat.mtimeMs) < 0.001) {
        unchanged += 1;
        continue;
      }
      const text = readFileSync(absolutePath, 'utf8');
      const contentHash = sha256(text);
      scanned += 1;
      if (!force && prior && prior.content_hash === contentHash) {
        database.prepare('UPDATE files SET size = ?, mtime_ms = ? WHERE id = ?').run(stat.size, stat.mtimeMs, prior.id);
        unchanged += 1;
        continue;
      }
      if (prior) deleteFile.run(prior.id);
      const extracted = extract(path, text);
      const fileId = Number(insertFile.run(path, extracted.language, stat.size, stat.mtimeMs, contentHash).lastInsertRowid);
      for (const symbol of extracted.symbols) insertSymbol.run(fileId, symbol.kind, symbol.name, symbol.line);
      for (const token of extracted.tokens) insertToken.run(fileId, token);
      for (const reference of extracted.projectReferences) insertReference.run(fileId, reference);
      updated += 1;
    }
    database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)').run('parser_version', parserVersion);
    database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)').run('updated_at_utc', new Date().toISOString());
    database.exec('COMMIT');
  } catch (error) {
    database.exec('ROLLBACK');
    throw error;
  }
  return {
    action: 'build',
    databasePath: database.filename ?? defaultDatabasePath,
    files: knownPaths.size,
    scanned,
    updated,
    unchanged,
    removed,
    symbols: database.prepare('SELECT COUNT(*) count FROM symbols').get().count,
    tokens: database.prepare('SELECT COUNT(*) count FROM file_tokens').get().count,
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
}

function findSymbols(database, query, limit) {
  const exactCount = database.prepare('SELECT COUNT(*) count FROM symbols WHERE name = ? COLLATE NOCASE').get(query).count;
  return database.prepare(`
    SELECT s.id, s.name, s.kind, s.line, f.path, f.language
    FROM symbols s JOIN files f ON f.id = s.file_id
    WHERE ${exactCount > 0 ? 's.name = ? COLLATE NOCASE' : 's.name LIKE ? COLLATE NOCASE'}
    ORDER BY CASE WHEN s.kind = 'method' THEN 1 ELSE 0 END, length(s.name), f.path, s.line
    LIMIT ?
  `).all(exactCount > 0 ? query : `%${query}%`, limit);
}

function consumers(database, query, limit) {
  const symbols = findSymbols(database, query, 50);
  const names = [...new Set(symbols.map((symbol) => symbol.name))];
  if (names.length === 0) return { query, symbols: [], consumers: [] };
  const placeholders = names.map(() => '?').join(',');
  const paths = new Set(symbols.map((symbol) => symbol.path));
  const rows = database.prepare(`
    SELECT DISTINCT t.token symbol, f.path, f.language
    FROM file_tokens t JOIN files f ON f.id = t.file_id
    WHERE t.token IN (${placeholders}) COLLATE NOCASE
    ORDER BY f.path LIMIT ?
  `).all(...names, limit * 3).filter((row) => !paths.has(row.path)).slice(0, limit);
  return { query, symbols, consumers: rows };
}

function impact(database, paths, limit) {
  const requested = paths.map((path) => path.replaceAll('\\', '/').replace(/\/$/, ''));
  const normalized = [...new Set(requested.flatMap((path) => {
    const rows = database.prepare('SELECT path FROM files WHERE path = ? OR path LIKE ? ORDER BY path').all(path, `${path}/%`);
    return rows.length > 0 ? rows.map((row) => row.path) : [path];
  }))];
  if (normalized.length === 0) return { paths: [], declaredSymbols: [], consumers: [], references: [] };
  const placeholders = normalized.map(() => '?').join(',');
  const declaredSymbols = database.prepare(`
    SELECT s.name, s.kind, s.line, f.path FROM symbols s JOIN files f ON f.id = s.file_id
    WHERE f.path IN (${placeholders}) ORDER BY f.path, s.line
  `).all(...normalized);
  const boundarySymbols = declaredSymbols.filter((symbol) => symbol.kind !== 'method');
  const names = [...new Set(boundarySymbols.map((symbol) => symbol.name))];
  let downstream = [];
  if (names.length > 0) {
    const nameSlots = names.map(() => '?').join(',');
    downstream = database.prepare(`
      SELECT DISTINCT t.token symbol, f.path, f.language FROM file_tokens t JOIN files f ON f.id = t.file_id
      WHERE t.token IN (${nameSlots}) COLLATE NOCASE AND f.path NOT IN (${placeholders})
      ORDER BY f.path LIMIT ?
    `).all(...names, ...normalized, limit);
  }
  const references = database.prepare(`
    SELECT DISTINCT t.token symbol, s.kind, sf.path declarationPath, f.path sourcePath
    FROM files f JOIN file_tokens t ON t.file_id = f.id
    JOIN symbols s ON s.name = t.token COLLATE NOCASE AND s.kind <> 'method'
    JOIN files sf ON sf.id = s.file_id
    WHERE f.path IN (${placeholders}) AND sf.path NOT IN (${placeholders})
    ORDER BY sf.path LIMIT ?
  `).all(...normalized, ...normalized, limit);
  return { requestedPaths: requested, paths: normalized, declaredSymbols, consumers: downstream, references };
}

function trace(database, query, limit) {
  const direct = consumers(database, query, limit);
  const paths = direct.symbols.map((symbol) => symbol.path);
  return { query, ...direct, impact: impact(database, paths, limit) };
}

const [action = 'status', ...argumentsList] = process.argv.slice(2);
const options = Object.fromEntries(argumentsList.map((argument) => {
  const separator = argument.indexOf('=');
  return separator < 0 ? [argument.replace(/^--/, ''), 'true'] : [argument.slice(2, separator), argument.slice(separator + 1)];
}));
const databasePath = resolve(repositoryRoot, options.database ?? '.artifacts/llm-wiki/code-graph/code-graph.sqlite');
const database = openDatabase(databasePath);
try {
  let result;
  if (action === 'build') result = build(database, options.force === 'true');
  else if (action === 'symbol') result = { query: options.query ?? '', symbols: findSymbols(database, options.query ?? '', Number(options.limit ?? 20)) };
  else if (action === 'consumers') result = consumers(database, options.query ?? '', Number(options.limit ?? 50));
  else if (action === 'impact') result = impact(database, (options.path ?? '').split(';').filter(Boolean), Number(options.limit ?? 100));
  else if (action === 'trace') result = trace(database, options.query ?? '', Number(options.limit ?? 50));
  else result = {
    action: 'status',
    databasePath,
    parserVersion: database.prepare("SELECT value FROM metadata WHERE key='parser_version'").get()?.value ?? null,
    files: database.prepare('SELECT COUNT(*) count FROM files').get().count,
    symbols: database.prepare('SELECT COUNT(*) count FROM symbols').get().count,
    tokens: database.prepare('SELECT COUNT(*) count FROM file_tokens').get().count,
  };
  process.stdout.write(`${JSON.stringify(result)}\n`);
} finally {
  database.close();
}
