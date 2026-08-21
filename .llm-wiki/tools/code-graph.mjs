import { createHash, randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { basename, dirname, extname, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { DatabaseSync } from 'node:sqlite';

const repositoryRoot = resolve(import.meta.dirname, '../..');
const defaultDatabasePath = resolve(repositoryRoot, '.artifacts/llm-wiki/code-graph/code-graph.sqlite');
const parserVersion = '11-javascript-context-v1';
const contextSearchSchemaVersion = '1';
const roslynProject = resolve(repositoryRoot, '.llm-wiki/tools/roslyn-extractor/LlmWiki.RoslynExtractor.csproj');
const roslynDll = resolve(repositoryRoot, '.llm-wiki/tools/roslyn-extractor/bin/Release/net10.0/LlmWiki.RoslynExtractor.dll');
const typescriptExtractor = resolve(repositoryRoot, '.llm-wiki/tools/typescript-extractor.mjs');
const contextSearchRankingPath = resolve(repositoryRoot, '.llm-wiki/policies/context-search-ranking.json');
const contextSearchRankingText = readFileSync(contextSearchRankingPath, 'utf8');
const contextSearchRanking = JSON.parse(contextSearchRankingText);
if (contextSearchRanking.schemaVersion !== 1) throw new Error(`Unsupported context-search ranking schema: ${contextSearchRanking.schemaVersion}`);

function publishGraphDependencyFingerprint(databasePath, result) {
  const fingerprint = sha256(JSON.stringify({
    parserVersion,
    contextSearchSchemaVersion,
    files: result.files,
    symbols: result.symbols,
    tokens: result.tokens,
    typedEdges: result.typedEdges,
    contextSearchFingerprint: result.contextSearch?.fingerprint ?? null,
    contextSearchRankingFingerprint: sha256(contextSearchRankingText),
    changeSetFingerprint: result.changeSetFingerprint ?? null,
  }));
  writeFileSync(graphDependencyFingerprintPath(databasePath), `${fingerprint}\n`, 'utf8');
  return fingerprint;
}

function graphDependencyFingerprintPath(databasePath) {
  return databasePath === defaultDatabasePath
    ? resolve(dirname(databasePath), 'code-graph.fingerprint')
    : `${databasePath}.fingerprint`;
}

function withBuildLock(callback) {
  const lockPath = resolve(repositoryRoot, '.artifacts/llm-wiki/code-graph/build.lock');
  const ownerPath = resolve(lockPath, 'owner.json');
  const ownerToken = randomUUID();
  mkdirSync(dirname(lockPath), { recursive: true });
  const timeoutMs = Number(process.env.LLM_WIKI_GRAPH_LOCK_TIMEOUT_MS ?? 300_000);
  const staleMs = Number(process.env.LLM_WIKI_GRAPH_LOCK_STALE_MS ?? 300_000);
  const deadline = Date.now() + timeoutMs;
  while (true) {
    try {
      mkdirSync(lockPath);
      writeFileSync(ownerPath, JSON.stringify({ pid: process.pid, token: ownerToken, createdAtUtc: new Date().toISOString() }));
      break;
    } catch (error) {
      if (error.code !== 'EEXIST') throw error;
      try {
        let owner;
        try { owner = JSON.parse(readFileSync(ownerPath, 'utf8')); } catch { owner = undefined; }
        const ownerAlive = Number.isInteger(owner?.pid) && isProcessAlive(owner.pid);
        const staleWithoutLiveOwner = !ownerAlive && (Number.isInteger(owner?.pid) || Date.now() - statSync(lockPath).mtimeMs > staleMs);
        if (staleWithoutLiveOwner) {
          rmSync(lockPath, { recursive: true, force: true });
          continue;
        }
      } catch (statError) {
        if (statError.code !== 'ENOENT') throw statError;
        continue;
      }
      if (Date.now() >= deadline) throw new Error(`Timed out waiting for code graph build lock: ${lockPath}`);
      Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 100);
    }
  }
  try {
    return callback();
  } finally {
    try {
      const owner = JSON.parse(readFileSync(ownerPath, 'utf8'));
      if (owner.token === ownerToken && owner.pid === process.pid) rmSync(lockPath, { recursive: true, force: true });
    } catch (error) {
      if (error.code !== 'ENOENT') throw error;
    }
  }
}

function isProcessAlive(pid) {
  try {
    process.kill(pid, 0);
    return true;
  } catch (error) {
    return error.code === 'EPERM';
  }
}

function ensureRoslynExtractor() {
  const sources = [roslynProject, resolve(dirname(roslynProject), 'Program.cs')];
  const requiresBuild = !existsSync(roslynDll)
    || sources.some((path) => statSync(path).mtimeMs > statSync(roslynDll).mtimeMs);
  if (requiresBuild) execFileSync('dotnet', ['build', roslynProject, '-c', 'Release', '--nologo', '--verbosity', 'quiet'], {
    cwd: repositoryRoot,
    stdio: ['ignore', 'ignore', 'pipe'],
  });
}

function extractCSharp(database, candidates, knownPaths) {
  const paths = candidates.map((item) => item.path);
  if (paths.length === 0) return new Map();
  ensureRoslynExtractor();
  let contextPaths;
  if (paths.length > 500) {
    contextPaths = [...knownPaths].filter((path) => path.endsWith('.cs'));
  } else {
    const identifiers = [...new Set(candidates.flatMap((item) => [...item.text.matchAll(/\b[A-Za-z_]\w{2,}\b/g)].map((match) => match[0])))];
    const declarationPaths = new Set();
    for (let offset = 0; offset < identifiers.length; offset += 400) {
      const batch = identifiers.slice(offset, offset + 400);
      const placeholders = batch.map(() => '?').join(',');
      for (const row of database.prepare(`SELECT DISTINCT f.path FROM symbols s JOIN files f ON f.id=s.file_id WHERE s.name IN (${placeholders}) COLLATE NOCASE`).all(...batch)) {
        declarationPaths.add(row.path);
        if (declarationPaths.size >= 300) break;
      }
      if (declarationPaths.size >= 300) break;
    }
    contextPaths = [...new Set([...paths, ...declarationPaths])].filter((path) => existsSync(resolve(repositoryRoot, path)));
  }
  const process = spawnSync('dotnet', [roslynDll, '--stdin'], {
    cwd: repositoryRoot,
    input: JSON.stringify({ paths, contextPaths, semantic: paths.length <= 500 }),
    encoding: 'utf8',
    maxBuffer: 128 * 1024 * 1024,
  });
  if (process.status !== 0) throw new Error(`Roslyn extractor failed (${process.status}): ${process.stderr}`);
  return new Map(JSON.parse(process.stdout).map((result) => [result.path, { language: 'csharp', ...result, projectReferences: [] }]));
}

function extractTypeScript(candidates) {
  const paths = candidates.map((item) => item.path);
  if (paths.length === 0) return new Map();
  const child = spawnSync(process.execPath, [typescriptExtractor], {
    cwd: repositoryRoot,
    input: JSON.stringify({ paths }),
    encoding: 'utf8',
    maxBuffer: 128 * 1024 * 1024,
  });
  if (child.status !== 0) throw new Error(`TypeScript extractor failed (${child.status}): ${child.stderr}`);
  return new Map(JSON.parse(child.stdout).map((result) => [result.path, result]));
}

let repositoryPathsCache;

function repositoryPaths() {
  if (repositoryPathsCache) return repositoryPathsCache;
  const output = execFileSync('git', ['-C', repositoryRoot, 'ls-files', '-z', '--cached', '--others', '--exclude-standard'], {
    encoding: 'utf8',
    maxBuffer: 64 * 1024 * 1024,
  });
  repositoryPathsCache = output
    .split('\0')
    .map((path) => path.replaceAll('\\', '/'))
    .filter(Boolean);
  return repositoryPathsCache;
}

function changeSetSnapshot() {
  const head = execFileSync('git', ['-C', repositoryRoot, 'rev-parse', 'HEAD'], { encoding: 'utf8' }).trim();
  const status = execFileSync(
    'git',
    ['-c', 'core.fsmonitor=false', '-C', repositoryRoot, 'status', '--porcelain=v1', '-z', '--untracked-files=all'],
    { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
  const records = status.split('\0').filter(Boolean);
  const changedPaths = new Set();
  for (let index = 0; index < records.length; index += 1) {
    const record = records[index];
    if (record.length < 4) continue;
    const changeStatus = record.slice(0, 2);
    changedPaths.add(record.slice(3).replaceAll('\\', '/'));
    if ((['R', 'C'].includes(changeStatus[0]) || ['R', 'C'].includes(changeStatus[1])) && index + 1 < records.length) {
      index += 1;
    }
  }
  const orderedPaths = [...changedPaths].sort((left, right) => {
    const normalizedLeft = left.toLowerCase();
    const normalizedRight = right.toLowerCase();
    if (normalizedLeft < normalizedRight) return -1;
    if (normalizedLeft > normalizedRight) return 1;
    return left < right ? -1 : left > right ? 1 : 0;
  });
  const hash = createHash('sha256');
  hash.update(head, 'utf8');
  hash.update(status, 'utf8');
  for (const path of orderedPaths) {
    hash.update(path, 'utf8');
    const absolutePath = resolve(repositoryRoot, path);
    if (existsSync(absolutePath)) hash.update(createHash('sha256').update(readFileSync(absolutePath)).digest());
    else hash.update('<missing>', 'utf8');
  }
  return { head, fingerprint: hash.digest('hex'), changedPaths: orderedPaths };
}

function gitPaths() {
  return repositoryPaths()
    .filter((path) => /\.(?:cs|csproj|props|targets|ts|js|mjs|cjs|html|ps1)$/.test(path)
      || /(^|\/)(?:appsettings(?:\.[^.\/]+)?|package|angular|backend-modules|module-dependencies)\.json$/.test(path)
      || /^\.github\/workflows\/[^/]+\.ya?ml$/.test(path))
    .filter((path) => !/(^|\/)(?:bin|obj|node_modules|\.artifacts|TestResults)(\/|$)/.test(path))
    .filter((path) => !/\.(?:Designer|g)\.cs$/.test(path) && !/ModelSnapshot\.cs$/.test(path));
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
  if (extension === '.ps1') return 'powershell';
  if (['.csproj', '.props', '.targets'].includes(extension)) return 'msbuild';
  if (extension === '.html') return 'html';
  if (extension === '.json') return 'json';
  if (extension === '.yml' || extension === '.yaml') return 'yaml';
  return 'typescript';
}

function extract(path, text) {
  const language = languageOf(path);
  const symbols = [];
  const tokens = new Set();
  const projectReferences = [];
  const edges = [];
  const addEdges = (pattern, kind, targetGroup = 'target', confidence = 'high') => {
    for (const match of text.matchAll(pattern)) {
      const target = match.groups?.[targetGroup];
      if (!target) continue;
      edges.push({ kind, target, line: lineAt(text, match.index), evidence: match[0].trim(), confidence });
    }
  };
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
    addEdges(/^\s*using\s+(?<target>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)+)\s*;/gm, 'namespace-import');
    addEdges(/\.Add(?:Scoped|Transient|Singleton|KeyedScoped|KeyedTransient|KeyedSingleton)\s*<\s*(?<target>[A-Za-z_]\w*)/g, 'di-service');
    addEdges(/\.Add(?:Scoped|Transient|Singleton|KeyedScoped|KeyedTransient|KeyedSingleton)\s*<\s*[A-Za-z_]\w*\s*,\s*(?<target>[A-Za-z_]\w*)/g, 'di-implementation');
    addEdges(/(?:IRequestHandler|ICommandHandler|IQueryHandler|INotificationHandler)\s*<\s*(?<target>[A-Za-z_]\w*)/g, 'mediator-handler');
    addEdges(/\b(?:Send|Publish)\s*\(\s*new\s+(?<target>[A-Za-z_]\w*)/g, 'mediator-dispatch', 'target', 'medium');
    addEdges(/\.(?:MapGet|MapPost|MapPut|MapPatch|MapDelete)\s*\(\s*["'`](?<target>[^"'`]+)["'`]/g, 'http-route');
    addEdges(/\[(?:HttpGet|HttpPost|HttpPut|HttpPatch|HttpDelete)(?:\(\s*["'`](?<target>[^"'`]+)["'`])?/g, 'http-attribute', 'target', 'medium');
    addEdges(/migrationBuilder\.(?:CreateTable|DropTable|RenameTable)\s*\(\s*name:\s*["'](?<target>[^"']+)["']/g, 'migration-table');
    addEdges(/migrationBuilder\.(?:AddColumn|DropColumn|RenameColumn|AlterColumn)[^;]*?table:\s*["'](?<target>[^"']+)["']/gs, 'migration-column');
  } else if (language === 'typescript') {
    addMatches(/^[ \t]*(?:export\s+)?(?:default\s+)?(?:abstract\s+|declare\s+)?(?<kind>class|interface|type|enum|function)\s+(?<name>[A-Za-z_$][\w$]*)/gm);
    for (const match of text.matchAll(/selector\s*:\s*['"](?<name>[^'"]+)['"]/g)) {
      symbols.push({ kind: 'selector', name: match.groups.name, line: lineAt(text, match.index) });
    }
    addEdges(/from\s+['"](?<target>[^'"]+)['"]/g, 'module-import');
    addEdges(/(?:loadComponent|loadChildren)\s*:\s*\(\)\s*=>\s*import\(['"](?<target>[^'"]+)['"]\)/g, 'angular-lazy-route');
    addEdges(/\.(?:get|post|put|patch|delete)\s*(?:<[^>]+>)?\s*\(\s*[`'"](?<target>[^`'"]+)[`'"]/g, 'http-client', 'target', 'medium');
  } else if (language === 'html') {
    for (const match of text.matchAll(/<(?<name>(?:fd|app)-[a-z0-9-]+)/g)) {
      tokens.add(match.groups.name);
      edges.push({ kind: 'template-component', target: match.groups.name, line: lineAt(text, match.index), evidence: match[0], confidence: 'high' });
    }
  } else if (language === 'msbuild') {
    for (const match of text.matchAll(/<ProjectReference\s+Include="(?<name>[^"]+)"/g)) {
      projectReferences.push(match.groups.name.replaceAll('\\', '/'));
      edges.push({ kind: 'project-reference', target: match.groups.name.replaceAll('\\', '/'), line: lineAt(text, match.index), evidence: match[0].trim(), confidence: 'high' });
    }
  } else if (language === 'json') {
    addEdges(/"(?<target>[A-Za-z_][A-Za-z0-9_.:-]*)"\s*:/g, 'configuration-key');
  } else if (language === 'yaml') {
    addEdges(/^\s*(?<target>[A-Za-z_][A-Za-z0-9_.-]*)\s*:/gm, 'configuration-key');
    addEdges(/\buses\s*:\s*(?<target>[^\s#]+)/g, 'workflow-action');
  } else if (language === 'powershell') {
    for (const match of text.matchAll(/^\s*function\s+(?<name>[A-Za-z_][\w-]*)/gmi)) {
      symbols.push({ kind: 'function', name: match.groups.name, line: lineAt(text, match.index) });
    }
  }

  for (const match of text.matchAll(/\b[A-Za-z_$][A-Za-z0-9_$]{2,}\b/g)) {
    const token = match[0];
    if (/^[A-ZI]/.test(token) || token.endsWith('Async')) tokens.add(token);
  }
  if (/(^|\/)(?:tests?|[^/]+\.Tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js)$/.test(path)) {
    edges.push({ kind: 'test-ownership', target: path, line: 1, evidence: path, confidence: 'high' });
  }
  return { language, symbols, tokens: [...tokens], projectReferences, edges };
}

function openDatabase(databasePath) {
  mkdirSync(dirname(databasePath), { recursive: true });
  let database;
  try {
    database = new DatabaseSync(databasePath);
    database.exec(`
    PRAGMA journal_mode = WAL;
    PRAGMA synchronous = NORMAL;
    PRAGMA foreign_keys = ON;
    PRAGMA busy_timeout = 5000;
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
    CREATE TABLE IF NOT EXISTS typed_edges(
      file_id INTEGER NOT NULL REFERENCES files(id) ON DELETE CASCADE,
      kind TEXT NOT NULL,
      target TEXT NOT NULL,
      line INTEGER NOT NULL,
      evidence TEXT NOT NULL,
      confidence TEXT NOT NULL,
      PRIMARY KEY(file_id, kind, target, line)
    ) WITHOUT ROWID;
    CREATE TABLE IF NOT EXISTS query_documents(
      category TEXT NOT NULL,
      record_key TEXT NOT NULL,
      path TEXT NOT NULL,
      source_path TEXT NOT NULL,
      payload_json TEXT NOT NULL,
      PRIMARY KEY(category, record_key, path)
    ) WITHOUT ROWID;
    CREATE INDEX IF NOT EXISTS ix_symbols_name ON symbols(name COLLATE NOCASE);
    CREATE INDEX IF NOT EXISTS ix_symbols_file ON symbols(file_id);
    CREATE INDEX IF NOT EXISTS ix_tokens_token ON file_tokens(token COLLATE NOCASE);
    CREATE INDEX IF NOT EXISTS ix_edges_kind_target ON typed_edges(kind, target COLLATE NOCASE);
    CREATE INDEX IF NOT EXISTS ix_query_documents_category_path ON query_documents(category, path COLLATE NOCASE);
  `);
    const storedSearchSchemaVersion = database.prepare("SELECT value FROM metadata WHERE key='context_search_schema_version'").get()?.value;
    if (storedSearchSchemaVersion !== contextSearchSchemaVersion) {
      database.exec('DROP TABLE IF EXISTS context_search');
    }
    database.exec(`
    CREATE VIRTUAL TABLE IF NOT EXISTS context_search USING fts5(
      record_type UNINDEXED,
      record_key UNINDEXED,
      path,
      source_path UNINDEXED,
      category UNINDEXED,
      title,
      body,
      tokenize = 'unicode61 remove_diacritics 2'
    );
  `);
    if (storedSearchSchemaVersion !== contextSearchSchemaVersion) {
      database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)')
        .run('context_search_schema_version', contextSearchSchemaVersion);
    }
    const ensureColumn = (table, column, definition) => {
      if (!database.prepare(`PRAGMA table_info(${table})`).all().some((item) => item.name === column)) database.exec(`ALTER TABLE ${table} ADD COLUMN ${column} ${definition}`);
    };
    ensureColumn('symbols', 'symbol_id', 'TEXT');
    ensureColumn('typed_edges', 'target_id', 'TEXT');
    database.exec('CREATE INDEX IF NOT EXISTS ix_symbols_symbol_id ON symbols(symbol_id); CREATE INDEX IF NOT EXISTS ix_edges_target_id ON typed_edges(target_id);');
    return database;
  } catch (error) {
    try { database?.close(); } catch { /* Preserve the original SQLite failure. */ }
    throw error;
  }
}

function isDatabaseCorruption(error) {
  const sqliteCode = Number(error?.errcode ?? error?.sqliteErrorCode);
  return sqliteCode === 11
    || sqliteCode === 26
    || /database disk image is malformed|database corruption|file is not a database/i.test(String(error?.message ?? ''));
}

function quarantineCorruptDatabase(databasePath) {
  const suffix = `.corrupt-${new Date().toISOString().replaceAll(/[^0-9]/g, '')}-${randomUUID()}`;
  const quarantinedPaths = [];
  try {
    for (const sourcePath of [
      databasePath,
      `${databasePath}-wal`,
      `${databasePath}-shm`,
      graphDependencyFingerprintPath(databasePath),
    ]) {
      if (!existsSync(sourcePath)) continue;
      const quarantinePath = `${sourcePath}${suffix}`;
      renameSync(sourcePath, quarantinePath);
      quarantinedPaths.push(quarantinePath);
    }
  } catch (error) {
    for (const quarantinePath of quarantinedPaths.toReversed()) {
      const sourcePath = quarantinePath.slice(0, -suffix.length);
      if (!existsSync(sourcePath) && existsSync(quarantinePath)) renameSync(quarantinePath, sourcePath);
    }
    throw error;
  }
  return quarantinedPaths;
}

function openDatabaseForBuild(databasePath) {
  try {
    return { database: openDatabase(databasePath), recoveredFromCorruption: false, quarantinedPaths: [] };
  } catch (error) {
    if (!isDatabaseCorruption(error) || dirname(databasePath) !== dirname(defaultDatabasePath)) throw error;
    const quarantinedPaths = quarantineCorruptDatabase(databasePath);
    return {
      database: openDatabase(databasePath),
      recoveredFromCorruption: true,
      quarantinedPaths,
    };
  }
}

function refreshQueryLayer(database) {
  const sources = [
    ['modules', 'docs/architecture/backend-modules.json'],
    ['contracts', '.llm-wiki/generated/backend-contract-index.json'],
    ['risks', '.llm-wiki/generated/quality-index.json'],
  ];
  const replaceCategory = database.prepare('DELETE FROM query_documents WHERE category = ?');
  const insert = database.prepare('INSERT OR REPLACE INTO query_documents(category, record_key, path, source_path, payload_json) VALUES (?, ?, ?, ?, ?)');
  let refreshed = 0;
  for (const [category, sourcePath] of sources) {
    const absolutePath = resolve(repositoryRoot, sourcePath);
    if (!existsSync(absolutePath)) continue;
    const text = readFileSync(absolutePath, 'utf8');
    const contentHash = sha256(text);
    const metadataKey = `query_source:${category}`;
    if (database.prepare('SELECT value FROM metadata WHERE key = ?').get(metadataKey)?.value === contentHash) continue;
    const document = JSON.parse(text);
    const records = [];
    if (category === 'modules') {
      for (const [name, value] of Object.entries(document.modules ?? {})) records.push({ key: name, path: value?.sourceMappings?.applicationProjects?.[0] ?? '', value: { name, ...value } });
    } else if (category === 'contracts') {
      for (const value of document.contracts ?? []) records.push({ key: value.id ?? value.name ?? value.symbol ?? JSON.stringify(value), path: value.path ?? value.sourcePath ?? '', value });
      for (const value of document.consumerEdges ?? []) records.push({ key: `consumer:${value.contract ?? ''}:${value.consumerPath ?? ''}`, path: value.consumerPath ?? '', value: { recordKind: 'consumer', ...value } });
    } else {
      for (const [recordKind, values] of Object.entries({ hotspot: document.hotspots ?? [], file: document.files ?? [], criticalSymbol: document.criticalSymbols ?? [], debtMarker: document.debtMarkers ?? [] })) {
        for (const value of values) records.push({ key: `${recordKind}:${value.path ?? ''}:${value.name ?? value.symbol ?? value.line ?? ''}`, path: value.path ?? value.sourcePath ?? '', value: { recordKind, ...value } });
      }
    }
    replaceCategory.run(category);
    for (const record of records) insert.run(category, String(record.key), String(record.path), sourcePath, JSON.stringify(record.value));
    database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)').run(metadataKey, contentHash);
    refreshed += 1;
  }
  return refreshed;
}

function expandSearchText(value) {
  const text = String(value ?? '');
  const expanded = text
    .replace(/([\p{Ll}\p{N}])([\p{Lu}])/gu, '$1 $2')
    .replace(/[_./\\-]+/g, ' ');
  return expanded === text ? text : `${text} ${expanded}`;
}

function contextDocumentPaths() {
  return repositoryPaths().filter((path) =>
    /(^|\/)AGENTS\.md$/i.test(path)
    || /^\.llm-wiki\/.+\.md$/i.test(path)
    || /^docs\/.+\.md$/i.test(path));
}

function refreshContextSearch(database) {
  const files = database.prepare('SELECT path, language, content_hash contentHash FROM files ORDER BY path').all();
  const queryDocuments = database.prepare(`
    SELECT category, record_key recordKey, path, source_path sourcePath, payload_json payloadJson
    FROM query_documents ORDER BY category, record_key, path
  `).all();
  const documentation = contextDocumentPaths().map((path) => {
    const text = readFileSync(resolve(repositoryRoot, path), 'utf8');
    return { path, text, contentHash: sha256(text) };
  });
  const fingerprint = sha256(JSON.stringify({
    schema: contextSearchSchemaVersion,
    files: files.map((item) => [item.path, item.contentHash]),
    queryDocuments: queryDocuments.map((item) => [item.category, item.recordKey, item.path, sha256(item.payloadJson)]),
    documentation: documentation.map((item) => [item.path, item.contentHash]),
  }));
  const metadata = database.prepare("SELECT value FROM metadata WHERE key='context_search_fingerprint'").get()?.value;
  const existingCount = database.prepare('SELECT COUNT(*) count FROM context_search').get().count;
  if (metadata === fingerprint && existingCount > 0) {
    return { refreshed: false, documents: existingCount, fingerprint };
  }

  const symbolsByPath = new Map();
  for (const row of database.prepare('SELECT f.path, s.name FROM symbols s JOIN files f ON f.id=s.file_id ORDER BY f.path, s.name').all()) {
    if (!symbolsByPath.has(row.path)) symbolsByPath.set(row.path, []);
    symbolsByPath.get(row.path).push(row.name);
  }
  const tokensByPath = new Map();
  for (const row of database.prepare('SELECT f.path, t.token FROM file_tokens t JOIN files f ON f.id=t.file_id ORDER BY f.path, t.token').all()) {
    if (!tokensByPath.has(row.path)) tokensByPath.set(row.path, []);
    tokensByPath.get(row.path).push(row.token);
  }

  database.exec('DELETE FROM context_search');
  const insert = database.prepare(`
    INSERT INTO context_search(record_type, record_key, path, source_path, category, title, body)
    VALUES (?, ?, ?, ?, ?, ?, ?)
  `);
  for (const file of files) {
    const sourceBody = file.language === 'powershell'
      ? readFileSync(resolve(repositoryRoot, file.path), 'utf8')
      : (tokensByPath.get(file.path) ?? []).join(' ');
    insert.run(
      'code',
      file.path,
      file.path,
      file.path,
      file.language,
      expandSearchText((symbolsByPath.get(file.path) ?? []).join(' ')),
      expandSearchText(sourceBody));
  }
  for (const item of queryDocuments) {
    insert.run(
      'query-document',
      item.recordKey,
      item.path || item.sourcePath,
      item.sourcePath,
      item.category,
      expandSearchText(item.recordKey),
      expandSearchText(item.payloadJson));
  }
  for (const item of documentation) {
    const recordType = /(^|\/)AGENTS\.md$/i.test(item.path)
      ? 'agent-guide'
      : item.path.startsWith('.llm-wiki/') ? 'wiki-page' : 'documentation';
    const title = item.text.match(/^#{1,3}\s+(.+)$/m)?.[1] ?? item.path;
    insert.run(recordType, item.path, item.path, item.path, recordType, expandSearchText(title), expandSearchText(item.text));
  }
  database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)')
    .run('context_search_fingerprint', fingerprint);
  database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)')
    .run('context_search_updated_at_utc', new Date().toISOString());
  return {
    refreshed: true,
    documents: database.prepare('SELECT COUNT(*) count FROM context_search').get().count,
    fingerprint,
  };
}

function build(database, force = false) {
  const started = performance.now();
  const startingChangeSet = changeSetSnapshot();
  const storedParserVersion = database.prepare("SELECT value FROM metadata WHERE key='parser_version'").get()?.value;
  if (storedParserVersion !== parserVersion) force = true;
  const knownPaths = new Set(gitPaths().filter((path) => existsSync(resolve(repositoryRoot, path))));
  const existing = new Map(database.prepare('SELECT id, path, size, mtime_ms, content_hash FROM files').all().map((item) => [item.path, item]));
  const deleteFile = database.prepare('DELETE FROM files WHERE id = ?');
  const insertFile = database.prepare('INSERT INTO files(path, language, size, mtime_ms, content_hash) VALUES (?, ?, ?, ?, ?)');
  const insertSymbol = database.prepare('INSERT INTO symbols(file_id, kind, name, line, symbol_id) VALUES (?, ?, ?, ?, ?)');
  const insertToken = database.prepare('INSERT OR IGNORE INTO file_tokens(file_id, token) VALUES (?, ?)');
  const insertReference = database.prepare('INSERT OR IGNORE INTO project_references(file_id, target_path) VALUES (?, ?)');
  const insertEdge = database.prepare('INSERT OR IGNORE INTO typed_edges(file_id, kind, target, line, evidence, confidence, target_id) VALUES (?, ?, ?, ?, ?, ?, ?)');
  let scanned = 0;
  let updated = 0;
  let unchanged = 0;
  let removed = 0;
  let queryCategoriesRefreshed = 0;
  let contextSearch;
  const candidates = [];

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
      candidates.push({ path, stat, prior, text: null, contentHash, metadataOnly: true });
      unchanged += 1;
      continue;
    }
    candidates.push({ path, stat, prior, text, contentHash, metadataOnly: false });
  }
  const roslynResults = extractCSharp(database, candidates.filter((item) => !item.metadataOnly && languageOf(item.path) === 'csharp'), knownPaths);
  const typescriptResults = extractTypeScript(candidates.filter((item) => !item.metadataOnly && languageOf(item.path) === 'typescript'));

  database.exec('BEGIN IMMEDIATE');
  try {
    for (const [path, row] of existing) {
      if (!knownPaths.has(path)) {
        deleteFile.run(row.id);
        removed += 1;
      }
    }
    for (const candidate of candidates) {
      const { path, stat, prior, text, contentHash, metadataOnly } = candidate;
      if (metadataOnly) {
        database.prepare('UPDATE files SET size = ?, mtime_ms = ? WHERE id = ?').run(stat.size, stat.mtimeMs, prior.id);
        continue;
      }
      if (prior) deleteFile.run(prior.id);
      const extracted = roslynResults.get(path) ?? typescriptResults.get(path) ?? extract(path, text);
      const fileId = Number(insertFile.run(path, extracted.language, stat.size, stat.mtimeMs, contentHash).lastInsertRowid);
      for (const symbol of extracted.symbols) insertSymbol.run(fileId, symbol.kind, symbol.name, symbol.line, symbol.symbolId ?? null);
      for (const token of extracted.tokens) insertToken.run(fileId, token);
      for (const reference of extracted.projectReferences) insertReference.run(fileId, reference);
      for (const edge of extracted.edges) insertEdge.run(fileId, edge.kind, edge.target, edge.line, edge.evidence, edge.confidence, edge.targetId ?? null);
      updated += 1;
    }
    database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)').run('parser_version', parserVersion);
    database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)').run('updated_at_utc', new Date().toISOString());
    queryCategoriesRefreshed = refreshQueryLayer(database);
    contextSearch = refreshContextSearch(database);
    const completedChangeSet = changeSetSnapshot();
    if (completedChangeSet.fingerprint !== startingChangeSet.fingerprint) {
      throw new Error('Worktree changed while the code graph was being refreshed; retry the build.');
    }
    database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)')
      .run('change_set_fingerprint', completedChangeSet.fingerprint);
    database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)')
      .run('change_set_git_head', completedChangeSet.head);
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
    typedEdges: database.prepare('SELECT COUNT(*) count FROM typed_edges').get().count,
    queryCategoriesRefreshed,
    contextSearch,
    changeSetFingerprint: startingChangeSet.fingerprint,
    changeSetGitHead: startingChangeSet.head,
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
}

function buildPlan(database, force = false) {
  const storedParserVersion = database.prepare("SELECT value FROM metadata WHERE key='parser_version'").get()?.value;
  const parserChanged = storedParserVersion !== parserVersion;
  const knownPaths = new Set(gitPaths().filter((path) => existsSync(resolve(repositoryRoot, path))));
  const existing = new Map(database.prepare('SELECT path, size, mtime_ms FROM files').all().map((item) => [item.path, item]));
  let candidateFiles = 0;
  for (const path of knownPaths) {
    const prior = existing.get(path);
    const stat = statSync(resolve(repositoryRoot, path));
    if (force || parserChanged || !prior || prior.size !== stat.size || Math.abs(prior.mtime_ms - stat.mtimeMs) >= 0.001) candidateFiles += 1;
  }
  const removedFiles = [...existing.keys()].filter((path) => !knownPaths.has(path)).length;
  const reason = force
    ? 'forced rebuild'
    : parserChanged
      ? `parser version changed (${storedParserVersion ?? 'missing'} -> ${parserVersion})`
      : existing.size === 0
        ? 'code graph database is empty'
        : candidateFiles > 0 || removedFiles > 0
          ? `${candidateFiles} changed/new and ${removedFiles} removed graph input(s)`
          : 'cache valid; metadata refresh only';
  return {
    action: 'build-plan',
    databasePath: database.filename ?? defaultDatabasePath,
    cacheMiss: force || parserChanged || existing.size === 0 || candidateFiles > 0 || removedFiles > 0,
    reason,
    candidateFiles,
    removedFiles,
    totalFiles: knownPaths.size,
    estimatedSeconds: Math.max(2, Math.round((candidateFiles * 0.12 + (parserChanged ? 20 : 0)) * 10) / 10),
  };
}

function queryDocuments(database, category, query, limit) {
  if (category === 'tests') {
    const terms = `%${query}%`;
    const rows = database.prepare(`
      SELECT DISTINCT f.path, e.kind, e.target, e.confidence
      FROM typed_edges e JOIN files f ON f.id=e.file_id
      WHERE e.kind='test-ownership' AND (?='' OR f.path LIKE ? COLLATE NOCASE OR e.target LIKE ? COLLATE NOCASE)
      ORDER BY f.path LIMIT ?
    `).all(query, terms, terms, limit);
    return { category, query, records: rows };
  }
  const term = `%${query}%`;
  const rows = database.prepare(`
    SELECT category, record_key recordKey, path, source_path sourcePath, payload_json payloadJson
    FROM query_documents
    WHERE category=? AND (?='' OR record_key LIKE ? COLLATE NOCASE OR path LIKE ? COLLATE NOCASE OR payload_json LIKE ? COLLATE NOCASE)
    ORDER BY path, record_key LIMIT ?
  `).all(category, query, term, term, term, limit).map((row) => ({ ...row, payload: JSON.parse(row.payloadJson) }));
  for (const row of rows) delete row.payloadJson;
  return { category, query, records: rows };
}

function findSymbols(database, query, limit) {
  const exactCount = database.prepare('SELECT COUNT(*) count FROM symbols WHERE name = ? COLLATE NOCASE').get(query).count;
  return database.prepare(`
    SELECT s.id, s.name, s.kind, s.line, s.symbol_id symbolId, f.path, f.language
    FROM symbols s JOIN files f ON f.id = s.file_id
    WHERE ${exactCount > 0 ? 's.name = ? COLLATE NOCASE' : 's.name LIKE ? COLLATE NOCASE'}
    ORDER BY CASE WHEN s.kind = 'method' THEN 1 ELSE 0 END, length(s.name), f.path, s.line
    LIMIT ?
  `).all(exactCount > 0 ? query : `%${query}%`, limit);
}

function consumers(database, query, limit) {
  const symbols = findSymbols(database, query, 50);
  const names = [...new Set(symbols.map((symbol) => symbol.name))];
  if (names.length === 0) {
    const rows = database.prepare(`
      SELECT DISTINCT e.target symbol, f.path, f.language, e.kind relationKind, 'typed' source
      FROM typed_edges e JOIN files f ON f.id = e.file_id
      WHERE e.target = ? COLLATE NOCASE OR e.target LIKE ? COLLATE NOCASE
      ORDER BY CASE WHEN e.target = ? COLLATE NOCASE THEN 0 ELSE 1 END, f.path LIMIT ?
    `).all(query, `${query}.%`, query, limit);
    return { query, symbols: [], consumers: rows };
  }
  const placeholders = names.map(() => '?').join(',');
  const symbolIds = [...new Set(symbols.map((symbol) => symbol.symbolId).filter(Boolean))];
  const idClause = symbolIds.length > 0 ? ` OR e.target_id IN (${symbolIds.map(() => '?').join(',')})` : '';
  const paths = new Set(symbols.map((symbol) => symbol.path));
  const typedRows = database.prepare(`
    SELECT DISTINCT e.target symbol, f.path, f.language, e.kind relationKind, 'typed' source
    FROM typed_edges e JOIN files f ON f.id = e.file_id
    WHERE (e.target IN (${placeholders}) COLLATE NOCASE ${idClause})
    ORDER BY f.path LIMIT ?
  `).all(...names, ...symbolIds, limit * 2).filter((row) => !paths.has(row.path));
  const typedPaths = new Set(typedRows.map((row) => row.path));
  const tokenRows = database.prepare(`
    SELECT DISTINCT t.token symbol, f.path, f.language
    FROM file_tokens t JOIN files f ON f.id = t.file_id
    WHERE t.token IN (${placeholders}) COLLATE NOCASE
      AND EXISTS (
        SELECT 1 FROM symbols declaration
        JOIN files declaration_file ON declaration_file.id = declaration.file_id
        WHERE declaration.name = t.token COLLATE NOCASE AND declaration_file.language = f.language
      )
    ORDER BY f.path LIMIT ?
  `).all(...names, limit * 3)
    .filter((row) => !paths.has(row.path) && !typedPaths.has(row.path))
    .map((row) => ({ ...row, relationKind: null, source: 'token' }));
  return { query, symbols, consumers: [...typedRows, ...tokenRows].slice(0, limit) };
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
        AND EXISTS (
          SELECT 1 FROM symbols declaration
          JOIN files declaration_file ON declaration_file.id = declaration.file_id
          WHERE declaration.name = t.token COLLATE NOCASE AND declaration_file.language = f.language
        )
      ORDER BY f.path LIMIT ?
    `).all(...names, ...normalized, limit);
  }
  const references = database.prepare(`
    SELECT DISTINCT t.token symbol, s.kind, sf.path declarationPath, f.path sourcePath
    FROM files f JOIN file_tokens t ON t.file_id = f.id
    JOIN symbols s ON s.name = t.token COLLATE NOCASE AND s.kind <> 'method'
    JOIN files sf ON sf.id = s.file_id
    WHERE f.path IN (${placeholders}) AND sf.path NOT IN (${placeholders}) AND sf.language = f.language
    ORDER BY sf.path LIMIT ?
  `).all(...normalized, ...normalized, limit);
  return { requestedPaths: requested, paths: normalized, declaredSymbols, consumers: downstream, references };
}

function trace(database, query, limit) {
  const direct = consumers(database, query, limit);
  const paths = direct.symbols.map((symbol) => symbol.path);
  const namespaceFilters = database.prepare(`
    SELECT f.path, e.line, e.target namespace,
      (SELECT COUNT(DISTINCT declaration_file.id)
       FROM typed_edges declaration
       JOIN files declaration_file ON declaration_file.id = declaration.file_id
       WHERE declaration.kind = 'declared-namespace'
         AND (declaration.target = e.target COLLATE NOCASE OR declaration.target LIKE (e.target || '.%') COLLATE NOCASE)) matchedDeclarations
    FROM typed_edges e JOIN files f ON f.id = e.file_id
    WHERE e.kind = 'namespace-filter' AND (e.target = ? COLLATE NOCASE OR e.target LIKE ? COLLATE NOCASE OR ? LIKE (e.target || '.%') COLLATE NOCASE)
    ORDER BY f.path, e.line LIMIT ?
  `).all(query, `${query}.%`, query, limit);
  return { query, ...direct, namespaceFilters, impact: impact(database, paths, limit) };
}

function rankedTrace(database, query, limit, filters = {}) {
  const direct = trace(database, query, limit);
  if (direct.symbols.length > 0 || direct.consumers.length > 0 || direct.namespaceFilters.length > 0) return { ...direct, candidates: [] };
  const terms = [...new Set(query.toLowerCase().match(/[a-z0-9]+/g) ?? [])].filter((term) => term.length >= 3);
  const backendTerms = new Set(['smtp', 'persistence', 'repository', 'readiness', 'telemetry', 'outbox', 'hosted', 'service', 'handler', 'worker', 'database', 'infrastructure']);
  const desiredKind = (filters.symbolKind ?? '').toLowerCase();
  const moduleTerm = (filters.module ?? '').toLowerCase();
  const prefix = (filters.pathPrefix ?? '').replaceAll('\\', '/').toLowerCase();
  const rows = database.prepare('SELECT s.name, s.kind, s.line, f.path, f.language FROM symbols s JOIN files f ON f.id=s.file_id').all();
  const candidates = rows.flatMap((row) => {
    const path = row.path.toLowerCase();
    const name = row.name.toLowerCase();
    if (prefix && !path.startsWith(prefix)) return [];
    if (moduleTerm && !path.includes(moduleTerm)) return [];
    if (desiredKind && desiredKind !== 'any' && !name.includes(desiredKind) && row.kind.toLowerCase() !== desiredKind) return [];
    const matched = terms.filter((term) => name.includes(term) || path.includes(term));
    if (matched.length === 0) return [];
    const reasons = [];
    let score = matched.length * 12;
    if (matched.length >= 2) { score += matched.length * 8; reasons.push(`${matched.length} query tokens match one candidate`); }
    if (moduleTerm && path.includes(moduleTerm)) { score += 40; reasons.push(`bounded context ${filters.module}`); }
    if (prefix && path.startsWith(prefix)) { score += 50; reasons.push(`path prefix ${filters.pathPrefix}`); }
    if ([...backendTerms].some((term) => terms.includes(term))) {
      if (row.language === 'csharp') { score += 25; reasons.push('backend intent and C# symbol'); }
      if (path.startsWith('fooddiary.web.client/')) { score -= 40; reasons.push('frontend penalty for backend intent'); }
    }
    if (/(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)/.test(path)) { score -= 45; reasons.push('test candidate ranked after production'); }
    if (/hostedservice|backgroundservice|worker/.test(name)) score += 18;
    if (/service|handler|repository|controller/.test(name)) score += 12;
    if (reasons.length === 0) reasons.push(`matched tokens: ${matched.join(', ')}`);
    return [{ ...row, score, matchedTerms: matched, reasons }];
  }).sort((left, right) => right.score - left.score || left.path.localeCompare(right.path)).slice(0, limit);
  return {
    ...direct,
    candidates: candidates.map((item, index) => ({ ...item, confidence: item.score >= 80 ? 'high' : item.score >= 40 ? 'medium' : 'low', rank: index + 1 })),
    ranking: { queryTerms: terms, layer: filters.layer ?? 'Auto', module: filters.module ?? null, symbolKind: filters.symbolKind ?? 'Any', pathPrefix: filters.pathPrefix ?? null },
  };
}

function searchTerms(query) {
  const direct = directSearchTerms(query);
  const expanded = [...direct];
  for (const term of direct) expanded.push(...englishMorphologicalVariants(term));
  expanded.push(...configuredSearchTermExpansions(direct));
  return [...new Set(expanded)].slice(0, Number(contextSearchRanking.maximumQueryTerms ?? 24));
}

function rankingTerms(query) {
  const direct = directSearchTerms(query);
  return [...new Set([...direct, ...configuredSearchTermExpansions(direct)])]
    .slice(0, Number(contextSearchRanking.maximumQueryTerms ?? 24));
}

function directSearchTerms(query) {
  const stopTerms = new Set(contextSearchRanking.stopTerms ?? []);
  return [...new Set(expandSearchText(query)
    .toLowerCase()
    .match(/[\p{L}\p{N}][\p{L}\p{N}_-]*/gu) ?? [])]
    .filter((term) => term.length >= 2 && !stopTerms.has(term));
}

function configuredSearchTermExpansions(direct) {
  const expanded = [];
  for (const term of direct) {
    expanded.push(...(contextSearchRanking.queryTermExpansions?.[term] ?? []));
    for (const [prefix, expansions] of Object.entries(contextSearchRanking.queryPrefixExpansions ?? {})) {
      if (term.startsWith(prefix)) expanded.push(...expansions);
    }
  }
  return expanded;
}

function englishMorphologicalVariants(term) {
  if (!/^[a-z]+$/.test(term)) return [];
  const variants = [];
  if (term.length > 4 && term.endsWith('ies')) variants.push(`${term.slice(0, -3)}y`);
  else if (term.length > 3 && term.endsWith('s') && !term.endsWith('ss')) variants.push(term.slice(0, -1));
  if (term.length > 5 && term.endsWith('ing')) {
    const stem = term.slice(0, -3);
    variants.push(stem, `${stem}e`);
    if (stem.length > 2 && stem.at(-1) === stem.at(-2)) variants.push(stem.slice(0, -1));
  }
  if (term.length > 4 && term.endsWith('ed')) variants.push(term.slice(0, -2), term.slice(0, -1));
  return variants;
}

function searchContext(database, query, limit, filters = {}) {
  const started = performance.now();
  const terms = searchTerms(query);
  const directTerms = directSearchTerms(query);
  const boostTerms = rankingTerms(query);
  const fingerprint = database.prepare("SELECT value FROM metadata WHERE key='context_search_fingerprint'").get()?.value ?? null;
  const indexedDocuments = database.prepare('SELECT COUNT(*) count FROM context_search').get().count;
  if (terms.length === 0 || !fingerprint || indexedDocuments === 0) {
    return {
      query,
      queryTerms: terms,
      ready: false,
      indexedDocuments,
      fingerprint,
      records: [],
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }
  const match = terms.map((term) => `"${term.replaceAll('"', '""')}"*`).join(' OR ');
  const candidateLimit = Math.min(Math.max(limit * 8, 40), 500);
  const candidates = database.prepare(`
    SELECT record_type recordType, record_key recordKey, path, source_path sourcePath,
      category, title, bm25(context_search, 0.0, 0.0, 6.0, 0.0, 0.0, 4.0, 1.0) lexicalRank
    FROM context_search
    WHERE context_search MATCH ?
    ORDER BY lexicalRank, path
    LIMIT ?
  `).all(match, candidateLimit);
  const identityMatch = terms.flatMap((term) => {
    const escaped = term.replaceAll('"', '""');
    return [`path : "${escaped}"*`, `title : "${escaped}"*`];
  }).join(' OR ');
  const identityLimit = Math.min(Math.max(limit * 2, 20), 100);
  const identityCandidates = database.prepare(`
    SELECT record_type recordType, record_key recordKey, path, source_path sourcePath,
      category, title, bm25(context_search, 0.0, 0.0, 6.0, 0.0, 0.0, 4.0, 1.0) lexicalRank
    FROM context_search
    WHERE context_search MATCH ?
    ORDER BY lexicalRank, path
    LIMIT ?
  `).all(identityMatch, identityLimit);
  const candidateIndexes = new Map(candidates.map((item, index) => [
    `${item.recordType}\0${item.recordKey}\0${item.path}`,
    index,
  ]));
  for (const identityCandidate of identityCandidates) {
    const key = `${identityCandidate.recordType}\0${identityCandidate.recordKey}\0${identityCandidate.path}`;
    const existingIndex = candidateIndexes.get(key);
    if (existingIndex === undefined) {
      candidateIndexes.set(key, candidates.length);
      candidates.push(identityCandidate);
    }
  }
  const normalizedQuery = expandSearchText(query).toLowerCase();
  const moduleTerm = String(filters.module ?? '').toLowerCase();
  const scopePaths = String(filters.path ?? '').split(';').filter(Boolean).map((path) => path.replaceAll('\\', '/').toLowerCase());
  const changeType = String(filters.changeType ?? 'Any').toLowerCase();
  const ranked = candidates.map((item, index) => {
    const path = String(item.path ?? '').replaceAll('\\', '/');
    const normalizedPath = path.toLowerCase();
    const normalizedTitle = expandSearchText(item.title).toLowerCase();
    const reasons = ['SQLite FTS5 lexical match'];
    let score = candidateLimit - index;
    if (normalizedPath.includes(normalizedQuery) || normalizedTitle.includes(normalizedQuery)) {
      score += 80;
      reasons.push('exact normalized query match');
    }
    if (moduleTerm && normalizedPath.includes(moduleTerm)) {
      score += 50;
      reasons.push(`module ${filters.module}`);
    }
    if (scopePaths.some((scope) => normalizedPath === scope || normalizedPath.startsWith(`${scope}/`) || scope.startsWith(`${normalizedPath}/`))) {
      score += 70;
      reasons.push('planned scope affinity');
    }
    const affinity = contextSearchRanking.pathTermAffinity ?? {};
    const searchablePath = expandSearchText(path).toLowerCase();
    const searchableIdentity = `${searchablePath} ${normalizedTitle}`;
    const searchableFileIdentity = expandSearchText(basename(path)).toLowerCase();
    let matchedRankingPolicy = false;
    const identityMatches = terms.filter((term) =>
      term.length >= Number(affinity.minimumTermLength ?? 3) && searchableIdentity.includes(term));
    const identityScore = Math.min(
      identityMatches.length * Number(affinity.scorePerMatch ?? 0),
      Number(affinity.maximumScore ?? 0));
    if (identityScore > 0) {
      score += identityScore;
      reasons.push(`path/title affinity ${identityMatches.join(', ')}`);
    }
    for (const boost of contextSearchRanking.identityBoosts ?? []) {
      const matchesChangeType = !(boost.changeTypes?.length)
        || boost.changeTypes.some((candidate) => String(candidate).toLowerCase() === changeType);
      if (!matchesChangeType) continue;
      const eligibleQueryTerms = boost.directOnly ? directTerms : boostTerms;
      const eligibleIdentity = boost.identityScope === 'file' ? searchableFileIdentity : searchablePath;
      const queryMatches = (boost.queryTerms ?? []).filter((term) => eligibleQueryTerms.includes(String(term).toLowerCase()));
      const identityMatchesBoost = (boost.identityTerms ?? []).filter((term) =>
        eligibleIdentity.includes(String(term).toLowerCase()));
      if (queryMatches.length >= Number(boost.minimumMatches ?? 1)
        && identityMatchesBoost.length >= Number(boost.minimumIdentityMatches ?? 1)) {
        score += Number(boost.score ?? 0);
        matchedRankingPolicy ||= boost.identityScope === 'file';
        reasons.push(`ranking policy ${boost.id}`);
      }
    }
    for (const boost of contextSearchRanking.pathBoosts ?? []) {
      const eligibleQueryTerms = boost.directOnly ? directTerms : boostTerms;
      const matchedTerms = (boost.queryTerms ?? []).filter((term) => eligibleQueryTerms.includes(String(term).toLowerCase()));
      const matchesIntent = matchedTerms.length >= Number(boost.minimumMatches ?? 1);
      const matchesPath = (boost.pathPrefixes ?? []).some((pathPrefix) =>
        normalizedPath.startsWith(String(pathPrefix).replaceAll('\\', '/').toLowerCase()));
      if (matchesIntent && matchesPath) {
        score += Number(boost.score ?? 0);
        matchedRankingPolicy = true;
        reasons.push(`ranking policy ${boost.id}`);
      }
    }
    if (matchedRankingPolicy) {
      const roleAffinity = contextSearchRanking.matchedPolicyFileNameAffinity ?? {};
      const fileNameMatches = boostTerms.filter((term) =>
        term.length >= Number(roleAffinity.minimumTermLength ?? 3) && searchableFileIdentity.includes(term));
      const roleAffinityScore = Math.min(
        fileNameMatches.length * Number(roleAffinity.scorePerMatch ?? 0),
        Number(roleAffinity.maximumScore ?? 0));
      if (roleAffinityScore > 0) {
        score += roleAffinityScore;
        reasons.push(`matched-role file-name affinity ${fileNameMatches.join(', ')}`);
      }
    }
    const isTest = /(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/i.test(path);
    if (isTest && changeType !== 'tests') score -= Number(contextSearchRanking.nonTestPenalty ?? 25);
    const isFrontendPath = normalizedPath.startsWith('fooddiary.web.client/');
    const isCode = item.recordType === 'code';
    if (isCode && changeType === 'frontend' && !isFrontendPath) {
      score -= Number(contextSearchRanking.crossLayerPenalty ?? 0);
      reasons.push('backend candidate penalty for frontend intent');
    } else if (isFrontendPath && ['api', 'backend', 'database'].includes(changeType)) {
      score -= Number(contextSearchRanking.crossLayerPenalty ?? 0);
      reasons.push('frontend candidate penalty for backend intent');
    }
    const requestsAbstraction = terms.some((term) => ['interface', 'contract', 'abstraction'].includes(term));
    if (!requestsAbstraction && normalizedPath.startsWith('fooddiary.application.abstractions/')) {
      score -= Number(contextSearchRanking.applicationAbstractionPenalty ?? 0);
    }
    if (!requestsAbstraction && /\/I[A-Z][^/]*\.cs$/.test(path)) {
      score -= Number(contextSearchRanking.interfacePathPenalty ?? 0);
    }
    if (/\.[^./]+\.cs$/i.test(path)) {
      score -= Number(contextSearchRanking.companionFilePenalty ?? 0);
      reasons.push('companion file ranked after primary declaration');
    }
    if (isCode) score += 20;
    if (item.recordType === 'agent-guide') score += Number(contextSearchRanking.agentGuideBoost ?? 15);
    return { ...item, score, lexicalRank: Math.round(item.lexicalRank * 1_000_000) / 1_000_000, reasons };
  }).sort((left, right) => right.score - left.score || left.lexicalRank - right.lexicalRank || left.path.localeCompare(right.path));
  const records = [];
  const seenPaths = new Set();
  for (const item of ranked) {
    const identity = item.path || `${item.recordType}:${item.recordKey}`;
    if (seenPaths.has(identity)) continue;
    seenPaths.add(identity);
    records.push({ ...item, rank: records.length + 1 });
    if (records.length >= limit) break;
  }
  return {
    query,
    queryTerms: terms,
    ready: true,
    indexedDocuments,
    fingerprint,
    updatedAtUtc: database.prepare("SELECT value FROM metadata WHERE key='context_search_updated_at_utc'").get()?.value ?? null,
    records,
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
}

function relations(database, paths, kinds, limit) {
  const requested = paths.map((path) => path.replaceAll('\\', '/').replace(/\/$/, ''));
  const pathClauses = requested.length > 0 ? requested.map(() => '(f.path = ? OR f.path LIKE ?)').join(' OR ') : '1=1';
  const pathArguments = requested.flatMap((path) => [path, `${path}/%`]);
  const kindList = kinds.filter(Boolean);
  const kindClause = kindList.length > 0 ? `AND e.kind IN (${kindList.map(() => '?').join(',')})` : '';
  const rows = database.prepare(`
    SELECT f.path, e.kind, e.target, e.target_id targetId, e.line, e.evidence, e.confidence
    FROM typed_edges e JOIN files f ON f.id = e.file_id
    WHERE (${pathClauses}) ${kindClause}
    ORDER BY f.path, e.line, e.kind LIMIT ?
  `).all(...pathArguments, ...kindList, limit);
  return { requestedPaths: requested, kinds: kindList, relations: rows };
}

function coverage(database) {
  const symbolExists = database.prepare('SELECT 1 found FROM symbols s JOIN files f ON f.id=s.file_id WHERE s.name=? COLLATE NOCASE AND f.path=? LIMIT 1');
  const compareIndex = (relativePath) => {
    try {
      const index = JSON.parse(readFileSync(resolve(repositoryRoot, relativePath), 'utf8'));
      const symbols = Array.isArray(index.symbols) ? index.symbols.filter((item) => item?.name && item?.path) : [];
      const missing = symbols.filter((item) => !symbolExists.get(item.name, item.path));
      return { index: relativePath, total: symbols.length, covered: symbols.length - missing.length, missing: missing.length, missingSamples: missing.slice(0, 10).map((item) => ({ name: item.name, path: item.path })) };
    } catch (error) {
      return { index: relativePath, error: error.message, total: 0, covered: 0, missing: 0, missingSamples: [] };
    }
  };
  const byKind = database.prepare('SELECT kind, COUNT(*) count, COUNT(DISTINCT file_id) files FROM typed_edges GROUP BY kind ORDER BY kind').all();
  const languages = database.prepare('SELECT language, COUNT(*) files FROM files GROUP BY language ORDER BY language').all();
  return {
    parserVersion,
    files: database.prepare('SELECT COUNT(*) count FROM files').get().count,
    symbols: database.prepare('SELECT COUNT(*) count FROM symbols').get().count,
    typedEdges: database.prepare('SELECT COUNT(*) count FROM typed_edges').get().count,
    languages,
    relationKinds: byKind,
    legacySymbolCoverage: [
      compareIndex('.llm-wiki/generated/csharp-symbol-index.json'),
      compareIndex('.llm-wiki/generated/frontend-index.json'),
    ],
  };
}

function fingerprint(database, paths) {
  const requested = paths.map((path) => path.replaceAll('\\', '/').replace(/\/$/, ''));
  let rows;
  if (requested.length === 0) {
    rows = database.prepare('SELECT path, content_hash FROM files ORDER BY path').all();
  } else {
    const clauses = requested.map(() => '(path = ? OR path LIKE ?)').join(' OR ');
    rows = database.prepare(`SELECT path, content_hash FROM files WHERE ${clauses} ORDER BY path`).all(...requested.flatMap((path) => [path, `${path}/%`]));
  }
  return { requestedPaths: requested, fileCount: rows.length, fingerprint: sha256(`${parserVersion}\n${rows.map((row) => `${row.path}\0${row.content_hash}`).join('\n')}`) };
}

const [action = 'status', ...argumentsList] = process.argv.slice(2);
const options = Object.fromEntries(argumentsList.map((argument) => {
  const separator = argument.indexOf('=');
  return separator < 0 ? [argument.replace(/^--/, ''), 'true'] : [argument.slice(2, separator), argument.slice(separator + 1)];
}));
const databasePath = resolve(repositoryRoot, options.database ?? '.artifacts/llm-wiki/code-graph/code-graph.sqlite');
let database;
try {
  let result;
  if (action === 'build') {
    result = withBuildLock(() => {
      let opened = openDatabaseForBuild(databasePath);
      database = opened.database;
      try {
        const completeBuild = () => {
          const buildResult = build(database, options.force === 'true');
          return {
            ...buildResult,
            graphDependencyFingerprint: publishGraphDependencyFingerprint(databasePath, buildResult),
            recoveredFromCorruption: opened.recoveredFromCorruption,
            quarantinedPaths: opened.quarantinedPaths,
          };
        };
        try {
          return completeBuild();
        } catch (error) {
          if (opened.recoveredFromCorruption || !isDatabaseCorruption(error)) throw error;
          try { database.close(); } finally { database = undefined; }
          const quarantinedPaths = quarantineCorruptDatabase(databasePath);
          opened = {
            database: openDatabase(databasePath),
            recoveredFromCorruption: true,
            quarantinedPaths,
          };
          database = opened.database;
          return completeBuild();
        }
      } finally {
        database?.close();
        database = undefined;
      }
    });
  } else {
    database = openDatabase(databasePath);
  }
  if (action === 'build') { /* result was produced while holding the build lock */ }
  else if (action === 'build-plan') result = buildPlan(database, options.force === 'true');
  else if (action === 'symbol') result = { query: options.query ?? '', symbols: findSymbols(database, options.query ?? '', Number(options.limit ?? 20)) };
  else if (action === 'consumers') result = consumers(database, options.query ?? '', Number(options.limit ?? 50));
  else if (action === 'impact') result = impact(database, (options.path ?? '').split(';').filter(Boolean), Number(options.limit ?? 100));
  else if (action === 'trace') result = rankedTrace(database, options.query ?? '', Number(options.limit ?? 50), {
    module: options.module, pathPrefix: options['path-prefix'], symbolKind: options['symbol-kind'], layer: options.layer,
  });
  else if (action === 'search') result = searchContext(database, options.query ?? '', Number(options.limit ?? 20), {
    module: options.module, path: options.path, changeType: options['change-type'],
  });
  else if (action === 'relations') result = relations(database, (options.path ?? '').split(';').filter(Boolean), (options.kind ?? '').split(';').filter(Boolean), Number(options.limit ?? 100));
  else if (action === 'coverage') result = coverage(database);
  else if (action === 'fingerprint') result = fingerprint(database, (options.path ?? '').split(';').filter(Boolean));
  else if (action === 'query') result = queryDocuments(database, options.category ?? 'modules', options.query ?? '', Number(options.limit ?? 50));
  else result = {
    action: 'status',
    databasePath,
    parserVersion: database.prepare("SELECT value FROM metadata WHERE key='parser_version'").get()?.value ?? null,
    files: database.prepare('SELECT COUNT(*) count FROM files').get().count,
    symbols: database.prepare('SELECT COUNT(*) count FROM symbols').get().count,
    tokens: database.prepare('SELECT COUNT(*) count FROM file_tokens').get().count,
    typedEdges: database.prepare('SELECT COUNT(*) count FROM typed_edges').get().count,
    searchDocuments: database.prepare('SELECT COUNT(*) count FROM context_search').get().count,
    searchFingerprint: database.prepare("SELECT value FROM metadata WHERE key='context_search_fingerprint'").get()?.value ?? null,
  };
  process.stdout.write(`${JSON.stringify(result)}\n`);
} finally {
  database?.close();
}
