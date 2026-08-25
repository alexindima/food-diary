import { createHash, randomUUID } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { basename, dirname, extname, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { DatabaseSync } from 'node:sqlite';

const repositoryRoot = resolve(import.meta.dirname, '../..');
const defaultDatabasePath = resolve(repositoryRoot, '.artifacts/llm-wiki/code-graph/code-graph.sqlite');
const parserVersion = '12-wiki-policy-context-v1';
const contextSearchSchemaVersion = '2';
const compiledIndexSchemaVersion = '4';
const queryDocumentSchemaVersion = '8';
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
    compiledIndexFingerprint: result.compiledIndexes?.fingerprint ?? null,
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
      || /^\.llm-wiki\/policies\/[^/]+\.json$/.test(path)
      || /^\.github\/workflows\/[^/]+\.ya?ml$/.test(path))
    .filter((path) => !/(^|\/)(?:bin|obj|node_modules|\.artifacts|TestResults)(\/|$)/.test(path))
    .filter((path) => !/\.(?:Designer|g)\.cs$/.test(path) && !/ModelSnapshot\.cs$/.test(path));
}

function sha256(text) {
  return createHash('sha256').update(text).digest('hex');
}

function normalizedTextHash(text) {
  return sha256(text.replaceAll('\r\n', '\n'));
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
    PRAGMA busy_timeout = 5000;
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
      record_kind TEXT NOT NULL DEFAULT '',
      source_ordinal INTEGER NOT NULL DEFAULT 0,
      payload_json TEXT NOT NULL,
      PRIMARY KEY(category, record_key, path)
    ) WITHOUT ROWID;
    CREATE TABLE IF NOT EXISTS compiled_indexes(
      index_name TEXT PRIMARY KEY,
      source_path TEXT NOT NULL,
      content_hash TEXT NOT NULL,
      payload_json TEXT NOT NULL
    ) WITHOUT ROWID;
    CREATE TABLE IF NOT EXISTS compiled_index_records(
      index_name TEXT NOT NULL,
      record_kind TEXT NOT NULL,
      record_key TEXT NOT NULL,
      path TEXT NOT NULL,
      source_ordinal INTEGER NOT NULL,
      search_text TEXT NOT NULL,
      payload_json TEXT NOT NULL,
      PRIMARY KEY(index_name, record_kind, record_key, path)
    ) WITHOUT ROWID;
    CREATE INDEX IF NOT EXISTS ix_symbols_name ON symbols(name COLLATE NOCASE);
    CREATE INDEX IF NOT EXISTS ix_symbols_file ON symbols(file_id);
    CREATE INDEX IF NOT EXISTS ix_tokens_token ON file_tokens(token COLLATE NOCASE);
    CREATE INDEX IF NOT EXISTS ix_edges_kind_target ON typed_edges(kind, target COLLATE NOCASE);
    CREATE INDEX IF NOT EXISTS ix_query_documents_category_path ON query_documents(category, path COLLATE NOCASE);
    CREATE INDEX IF NOT EXISTS ix_compiled_index_records_kind_path ON compiled_index_records(index_name, record_kind, path COLLATE NOCASE);
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
    ensureColumn('query_documents', 'record_kind', "TEXT NOT NULL DEFAULT ''");
    ensureColumn('query_documents', 'source_ordinal', 'INTEGER NOT NULL DEFAULT 0');
    ensureColumn('compiled_index_records', 'source_ordinal', 'INTEGER NOT NULL DEFAULT 0');
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
    ['frontend-contracts', '.llm-wiki/generated/frontend-contract-index.json'],
    ['risks', '.llm-wiki/generated/quality-index.json'],
    ['runtime', '.llm-wiki/generated/runtime-topology.json'],
    ['sensitive', '.llm-wiki/generated/sensitive-data-index.json'],
    ['domain', '.llm-wiki/generated/domain-data-index.json'],
    ['architecture-health', '.llm-wiki/generated/architecture-health-index.json'],
  ];
  const replaceCategory = database.prepare('DELETE FROM query_documents WHERE category = ?');
  const insert = database.prepare(`
    INSERT OR REPLACE INTO query_documents(category, record_key, path, source_path, record_kind, source_ordinal, payload_json)
    VALUES (?, ?, ?, ?, ?, ?, ?)
  `);
  const schemaChanged = database.prepare("SELECT value FROM metadata WHERE key = 'query_document_schema_version'").get()?.value !== queryDocumentSchemaVersion;
  let refreshed = 0;
  for (const [category, sourcePath] of sources) {
    const absolutePath = resolve(repositoryRoot, sourcePath);
    if (!existsSync(absolutePath)) continue;
    const text = readFileSync(absolutePath, 'utf8');
    const contentHash = normalizedTextHash(text);
    const metadataKey = `query_source:${category}`;
    if (!schemaChanged && database.prepare('SELECT value FROM metadata WHERE key = ?').get(metadataKey)?.value === contentHash) continue;
    const document = JSON.parse(text);
    const records = [];
    if (category === 'modules') {
      for (const [ordinal, [name, value]] of Object.entries(document.modules ?? {}).entries()) records.push({ key: name, path: value?.sourceMappings?.applicationProjects?.[0] ?? '', kind: 'module', ordinal, value: { name, ...value } });
    } else if (category === 'contracts') {
      let ordinal = 0;
      for (const value of document.contracts ?? []) records.push({ key: value.id ?? value.name ?? value.symbol ?? JSON.stringify(value), path: value.path ?? value.sourcePath ?? '', kind: 'contract', ordinal: ordinal++, value });
      for (const value of document.consumerEdges ?? []) records.push({ key: `consumer:${value.contract ?? ''}:${value.consumerPath ?? ''}:${ordinal}`, path: value.consumerPath ?? '', kind: 'consumer', ordinal: ordinal++, value });
    } else if (category === 'frontend-contracts') {
      let ordinal = 0;
      for (const value of document.components ?? []) records.push({ key: `component:${value.class ?? ''}:${value.path ?? ''}`, path: value.path ?? '', kind: 'component', ordinal: ordinal++, value });
      for (const value of document.consumerEdges ?? []) records.push({ key: `consumer:${value.component ?? ''}:${value.consumerPath ?? ''}:${ordinal}`, path: value.consumerPath ?? '', kind: 'consumer', ordinal: ordinal++, value });
      for (const value of document.apiCalls ?? []) records.push({ key: `api-call:${value.path ?? ''}:${value.method ?? ''}:${value.line ?? ordinal}`, path: value.path ?? '', kind: 'api-call', ordinal: ordinal++, value });
      for (const value of document.translationUsage ?? []) records.push({ key: `translation:${value.path ?? value.templatePath ?? ''}:${ordinal}`, path: value.path ?? value.templatePath ?? '', kind: 'translation', ordinal: ordinal++, value });
    } else if (category === 'risks') {
      let ordinal = 0;
      for (const [recordKind, values] of Object.entries({ hotspot: document.hotspots ?? [], file: document.files ?? [], criticalSymbol: document.criticalSymbols ?? [], debtMarker: document.debtMarkers ?? [] })) {
        for (const value of values) records.push({ key: `${recordKind}:${value.path ?? ''}:${value.name ?? value.symbol ?? value.line ?? ''}:${ordinal}`, path: value.path ?? value.sourcePath ?? '', kind: recordKind, ordinal: ordinal++, value: { recordKind, ...value } });
      }
    } else if (category === 'runtime') {
      let ordinal = 0;
      for (const [recordKind, values] of Object.entries({ composeService: document.composeServices ?? [], hostedService: document.hostedServices ?? [], httpClient: document.httpClients ?? [], webhook: document.webhooks ?? [], recurringJob: document.recurringJobRegistrations ?? [] })) {
        for (const value of values) {
          const path = recordKind === 'composeService' ? 'docker-compose.yml' : value.path ?? value.registrationPath ?? '';
          records.push({ key: `${recordKind}:${path}:${value.name ?? value.contract ?? value.api ?? ''}:${ordinal}`, path, kind: recordKind, ordinal: ordinal++, value });
        }
      }
    } else if (category === 'sensitive') {
      let ordinal = 0;
      records.push({ key: 'summary', path: '', kind: 'summary', ordinal: ordinal++, value: document.summary ?? {} });
      for (const [recordKind, values] of Object.entries({ field: document.fields ?? [], boundary: document.boundaryFiles ?? [], potentialLogging: document.potentialLogging ?? [], externalTransfer: document.externalTransfers ?? [] })) {
        for (const value of values) records.push({ key: `${recordKind}:${value.path ?? ''}:${value.name ?? value.line ?? ''}:${ordinal}`, path: value.path ?? '', kind: recordKind, ordinal: ordinal++, value });
      }
    } else if (category === 'domain') {
      let ordinal = 0;
      for (const [recordKind, values] of Object.entries({ domainType: document.domainTypes ?? [], invariant: document.invariants ?? [], persistenceMapping: document.persistenceMappings ?? [] })) {
        for (const value of values) records.push({ key: `${recordKind}:${value.path ?? ''}:${value.name ?? value.type ?? value.entity ?? value.line ?? ''}:${ordinal}`, path: value.path ?? '', kind: recordKind, ordinal: ordinal++, value });
      }
    } else if (category === 'architecture-health') {
      let ordinal = 0;
      for (const [recordKind, values] of Object.entries({ dependencyViolation: document.projectDependencyViolations ?? [], unusedProjectAllowance: document.unusedProjectAllowances ?? [], untrackedProject: document.untrackedProductionProjects ?? [], moduleCycle: document.moduleCycleNodes ?? [], ambiguousContract: document.ambiguousBackendContracts ?? [], unconsumedBackendContract: document.unconsumedBackendContracts ?? [], selectorUnreferenced: document.selectorUnreferencedComponents ?? [], componentWithoutSpec: document.componentsWithoutDirectSpecs ?? [], criticalSymbolWithoutTest: document.criticalSymbolsWithoutTestReferences ?? [], debtMarker: document.explicitDebtMarkers ?? [] })) {
        for (const value of values) records.push({ key: `${recordKind}:${value.path ?? ''}:${value.name ?? value.class ?? value.module ?? value.line ?? ''}:${ordinal}`, path: value.path ?? '', kind: recordKind, ordinal: ordinal++, value });
      }
    }
    replaceCategory.run(category);
    for (const record of records) insert.run(category, String(record.key), String(record.path), sourcePath, String(record.kind), Number(record.ordinal), JSON.stringify(record.value));
    database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)').run(metadataKey, contentHash);
    refreshed += 1;
  }
  database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)')
    .run('query_document_schema_version', queryDocumentSchemaVersion);
  return refreshed;
}

function refreshCompiledIndexes(database) {
  const sources = [
    ['repository-catalog', '.llm-wiki/generated/repository-catalog.json'],
    ['csharp-symbols', '.llm-wiki/generated/csharp-symbol-index.json'],
    ['frontend', '.llm-wiki/generated/frontend-index.json'],
  ];
  const replaceIndex = database.prepare(`
    INSERT OR REPLACE INTO compiled_indexes(index_name, source_path, content_hash, payload_json)
    VALUES (?, ?, ?, ?)
  `);
  const deleteRecords = database.prepare('DELETE FROM compiled_index_records WHERE index_name = ?');
  const insertRecord = database.prepare(`
    INSERT OR REPLACE INTO compiled_index_records(index_name, record_kind, record_key, path, source_ordinal, search_text, payload_json)
    VALUES (?, ?, ?, ?, ?, ?, ?)
  `);
  let refreshed = 0;
  const schemaChanged = database.prepare("SELECT value FROM metadata WHERE key = 'compiled_index_schema_version'").get()?.value !== compiledIndexSchemaVersion;
  for (const [indexName, sourcePath] of sources) {
    const absolutePath = resolve(repositoryRoot, sourcePath);
    if (!existsSync(absolutePath)) continue;
    const text = readFileSync(absolutePath, 'utf8');
    const contentHash = sha256(text.replaceAll('\r\n', '\n'));
    const existingHash = database.prepare('SELECT content_hash contentHash FROM compiled_indexes WHERE index_name = ?').get(indexName)?.contentHash;
    if (!schemaChanged && existingHash === contentHash) continue;
    const document = JSON.parse(text);
    replaceIndex.run(indexName, sourcePath, contentHash, text);
    deleteRecords.run(indexName);
    if (indexName === 'csharp-symbols') {
      for (const [ordinal, symbol] of (document.symbols ?? []).entries()) {
        const recordKey = `${ordinal}:${symbol.name ?? ''}:${symbol.path ?? ''}:${symbol.line ?? ''}`;
        const searchText = `${symbol.name ?? ''} ${symbol.role ?? ''} ${symbol.path ?? ''}`;
        insertRecord.run(indexName, 'symbol', recordKey, symbol.path ?? '', ordinal, searchText, JSON.stringify(symbol));
      }
      for (const [ordinal, registration] of (document.dependencyInjectionRegistrations ?? []).entries()) {
        const recordKey = `${ordinal}:${registration.service ?? ''}:${registration.implementation ?? ''}:${registration.path ?? ''}:${registration.line ?? ''}`;
        const searchText = `${registration.service ?? ''} ${registration.implementation ?? ''} ${registration.path ?? ''}`;
        insertRecord.run(indexName, 'dependency-injection', recordKey, registration.path ?? '', ordinal, searchText, JSON.stringify(registration));
      }
      for (const [ordinal, implementation] of (document.interfaceImplementations ?? []).entries()) {
        const recordKey = `${ordinal}:${implementation.interface ?? ''}:${implementation.implementation ?? ''}:${implementation.path ?? ''}`;
        const searchText = `${implementation.interface ?? ''} ${implementation.implementation ?? ''} ${implementation.path ?? ''}`;
        insertRecord.run(indexName, 'interface-implementation', recordKey, implementation.path ?? '', ordinal, searchText, JSON.stringify(implementation));
      }
    } else if (indexName === 'frontend') {
      for (const [ordinal, feature] of (document.features ?? []).entries()) {
        const recordKey = `${ordinal}:${feature.area ?? ''}:${feature.name ?? ''}:${feature.root ?? ''}`;
        const searchText = [feature.area, feature.name, feature.root, ...(feature.symbols ?? []), ...(feature.routes ?? []), ...(feature.tests ?? [])].filter(Boolean).join(' ');
        insertRecord.run(indexName, 'feature', recordKey, feature.root ?? '', ordinal, searchText, JSON.stringify(feature));
      }
      for (const [ordinal, symbol] of (document.symbols ?? []).entries()) {
        const recordKey = `${ordinal}:${symbol.name ?? ''}:${symbol.path ?? ''}:${symbol.line ?? ''}`;
        const searchText = `${symbol.name ?? ''} ${symbol.role ?? ''} ${symbol.selector ?? ''} ${symbol.path ?? ''}`;
        insertRecord.run(indexName, 'symbol', recordKey, symbol.path ?? '', ordinal, searchText, JSON.stringify(symbol));
      }
      for (const [ordinal, route] of (document.routes ?? []).entries()) {
        const recordKey = `${ordinal}:${route.path ?? ''}:${route.source ?? ''}:${route.line ?? ''}`;
        const searchText = `${route.path ?? ''} ${route.source ?? ''}`;
        insertRecord.run(indexName, 'route', recordKey, route.source ?? '', ordinal, searchText, JSON.stringify(route));
      }
      for (const [ordinal, localization] of (document.localization ?? []).entries()) {
        const recordKey = `${ordinal}:${localization.name ?? ''}`;
        insertRecord.run(indexName, 'localization', recordKey, localization.name ?? '', ordinal, localization.name ?? '', JSON.stringify(localization));
      }
    }
    refreshed += 1;
  }
  database.prepare('INSERT OR REPLACE INTO metadata(key, value) VALUES (?, ?)')
    .run('compiled_index_schema_version', compiledIndexSchemaVersion);
  const indexes = database.prepare(`
    SELECT index_name indexName, source_path sourcePath, content_hash contentHash
    FROM compiled_indexes ORDER BY index_name
  `).all();
  const records = database.prepare(`
    SELECT index_name indexName, record_kind recordKind, COUNT(*) count
    FROM compiled_index_records GROUP BY index_name, record_kind ORDER BY index_name, record_kind
  `).all();
  return {
    refreshed,
    indexes,
    records,
    fingerprint: sha256(JSON.stringify(indexes.map((item) => [item.indexName, item.contentHash]))),
  };
}

function normalizedSearchTerms(...values) {
  return [...new Set(values
    .filter(Boolean)
    .flatMap((value) => String(value).toLocaleLowerCase('en-US').match(/[\p{L}\p{N}]+/gu) ?? [])
    .filter((term) => term.length >= 2))];
}

function pathsHaveAffinity(path, scopePaths) {
  const normalizedPath = String(path ?? '').replaceAll('\\', '/');
  for (const scopePath of scopePaths) {
    const normalizedScope = scopePath.replaceAll('\\', '/').replace(/\/$/, '');
    const scopeDirectory = /\.[^/]+$/.test(normalizedScope)
      ? normalizedScope.slice(0, normalizedScope.lastIndexOf('/'))
      : normalizedScope;
    if (normalizedPath === normalizedScope
      || normalizedPath.startsWith(`${scopeDirectory}/`)
      || normalizedScope.startsWith(`${normalizedPath.replace(/\/$/, '')}/`)) return true;
    const feature = normalizedScope.match(/\/features\/([^/]+)\//i)?.[1];
    if (feature && normalizedPath.toLocaleLowerCase('en-US').includes(`/features/${feature.toLocaleLowerCase('en-US')}/`)) return true;
  }
  return false;
}

function compiledContext(database, options) {
  const started = performance.now();
  const catalogRow = database.prepare(`
    SELECT source_path sourcePath, content_hash contentHash, payload_json payloadJson
    FROM compiled_indexes WHERE index_name = 'repository-catalog'
  `).get();
  const symbolIndex = database.prepare(`
    SELECT source_path sourcePath, content_hash contentHash
    FROM compiled_indexes WHERE index_name = 'csharp-symbols'
  `).get();
  const frontendIndex = database.prepare(`
    SELECT source_path sourcePath, content_hash contentHash
    FROM compiled_indexes WHERE index_name = 'frontend'
  `).get();
  if (!catalogRow || !symbolIndex || !frontendIndex) {
    return {
      ready: false,
      unavailableReason: 'compiled-index-projection-missing',
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }
  const catalogSourceText = readFileSync(resolve(repositoryRoot, catalogRow.sourcePath), 'utf8');
  const symbolSourceText = readFileSync(resolve(repositoryRoot, symbolIndex.sourcePath), 'utf8');
  const frontendSourceText = readFileSync(resolve(repositoryRoot, frontendIndex.sourcePath), 'utf8');
  const currentCatalogHash = normalizedTextHash(catalogSourceText);
  const currentSymbolHash = normalizedTextHash(symbolSourceText);
  const currentFrontendHash = normalizedTextHash(frontendSourceText);
  if (currentCatalogHash !== catalogRow.contentHash || currentSymbolHash !== symbolIndex.contentHash || currentFrontendHash !== frontendIndex.contentHash) {
    return {
      ready: false,
      unavailableReason: 'compiled-index-projection-stale',
      sourceHashes: {
        repositoryCatalog: { projected: catalogRow.contentHash, current: currentCatalogHash },
        csharpSymbols: { projected: symbolIndex.contentHash, current: currentSymbolHash },
        frontend: { projected: frontendIndex.contentHash, current: currentFrontendHash },
      },
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }
  const query = options.query ?? '';
  const module = options.module ?? '';
  const scopePaths = (options.path ?? '').split(';').filter(Boolean);
  const selectionMode = options['compiled-mode'] ?? 'context';
  const includeFrontendFeatures = options['include-frontend-features'] === 'true';
  if (!['context', 'changed-paths'].includes(selectionMode)) {
    throw new Error(`Unsupported compiled-context selection mode: ${selectionMode}`);
  }
  const terms = normalizedSearchTerms(module, query);
  const phrases = [module, query].filter(Boolean).map((value) => value.toLocaleLowerCase('en-US'));
  const catalog = JSON.parse(catalogRow.payloadJson);
  const matchedModule = (() => {
    if (!module) return undefined;
    return [...(catalog.applicationModules ?? []), ...(catalog.extractedApplicationModules ?? [])]
      .find((item) => String(item.name ?? '').toLocaleLowerCase('en-US') === module.toLocaleLowerCase('en-US'))
      ?? [...(catalog.applicationModules ?? []), ...(catalog.extractedApplicationModules ?? [])]
        .find((item) => String(item.name ?? '').toLocaleLowerCase('en-US').includes(module.toLocaleLowerCase('en-US')));
  })();
  const moduleProjectDirectory = matchedModule?.project
    ? String(matchedModule.project).replaceAll('\\', '/').replace(/\/[^/]+$/, '')
    : '';
  let rows;
  let selected;
  let frontendRows;
  let frontendSelected;
  let scannedRecords;
  if (selectionMode === 'changed-paths') {
    scannedRecords = database.prepare(`
      SELECT COUNT(*) count FROM compiled_index_records
      WHERE index_name = 'csharp-symbols' AND record_kind = 'symbol'
    `).get().count + database.prepare(`
      SELECT COUNT(*) count FROM compiled_index_records
      WHERE index_name = 'frontend' AND record_kind = 'symbol'
    `).get().count;
    if (includeFrontendFeatures) {
      scannedRecords += database.prepare(`
        SELECT COUNT(*) count FROM compiled_index_records
        WHERE index_name = 'frontend' AND record_kind = 'feature'
      `).get().count;
    }
    if (scopePaths.length === 0) {
      rows = [];
      frontendRows = [];
    } else {
      const placeholders = scopePaths.map(() => '?').join(', ');
      rows = database.prepare(`
        SELECT record_kind recordKind, path, search_text searchText, payload_json payloadJson
        FROM compiled_index_records
        WHERE index_name = 'csharp-symbols' AND record_kind = 'symbol' AND path IN (${placeholders})
        ORDER BY source_ordinal
      `).all(...scopePaths);
      frontendRows = database.prepare(`
        SELECT record_kind recordKind, path, search_text searchText, payload_json payloadJson
        FROM compiled_index_records
        WHERE index_name = 'frontend' AND record_kind = 'symbol' AND path IN (${placeholders})
        ORDER BY source_ordinal
      `).all(...scopePaths);
    }
    if (includeFrontendFeatures) {
      frontendRows.push(...database.prepare(`
        SELECT record_kind recordKind, path, search_text searchText, payload_json payloadJson
        FROM compiled_index_records
        WHERE index_name = 'frontend' AND record_kind = 'feature'
        ORDER BY source_ordinal
      `).all());
    }
    selected = rows;
    frontendSelected = frontendRows;
  } else {
    rows = database.prepare(`
      SELECT record_kind recordKind, path, search_text searchText, payload_json payloadJson
      FROM compiled_index_records
      WHERE index_name = 'csharp-symbols' AND record_kind IN ('symbol', 'dependency-injection')
      ORDER BY record_kind, source_ordinal
    `).all();
    frontendRows = database.prepare(`
      SELECT record_kind recordKind, path, search_text searchText, payload_json payloadJson
      FROM compiled_index_records
      WHERE index_name = 'frontend' AND record_kind IN ('feature', 'symbol', 'route', 'localization')
      ORDER BY record_kind, source_ordinal
    `).all();
    scannedRecords = rows.length + frontendRows.length;
    selected = rows.filter((row) => {
      const searchable = row.searchText.toLocaleLowerCase('en-US');
      const textMatch = terms.some((term) => searchable.includes(term))
        || phrases.some((phrase) => searchable.includes(phrase));
      const scopeMatch = pathsHaveAffinity(row.path, scopePaths);
      const moduleMatch = moduleProjectDirectory && (row.path === moduleProjectDirectory || row.path.startsWith(`${moduleProjectDirectory}/`));
      return textMatch || scopeMatch || moduleMatch;
    });
    const localizationTerms = new Set(['i18n', 'locale', 'localization', 'translation']);
    frontendSelected = frontendRows.filter((row) => {
      const searchable = row.searchText.toLocaleLowerCase('en-US');
      const textMatch = terms.some((term) => searchable.includes(term))
        || phrases.some((phrase) => searchable.includes(phrase));
      const scopeMatch = pathsHaveAffinity(row.path, scopePaths);
      const localizationMatch = row.recordKind === 'localization' && terms.some((term) => localizationTerms.has(term));
      return textMatch || scopeMatch || localizationMatch;
    });
  }
  const contextCatalog = {
    applicationModules: (catalog.applicationModules ?? []).map((item) => ({
      name: item.name,
      dependencies: item.dependencies ?? [],
      origin: item.origin,
      project: item.project,
    })),
    extractedApplicationModules: (catalog.extractedApplicationModules ?? []).map((item) => ({
      name: item.name,
      project: item.project,
    })),
    dotnet: {
      projects: (catalog.dotnet?.projects ?? []).map((item) => ({
        name: item.name,
        path: item.path,
        isTestProject: item.isTestProject,
      })),
    },
    frontend: {
      projects: (catalog.frontend?.projects ?? []).map((item) => ({
        name: item.name,
        projectType: item.projectType,
        root: item.root,
        sourceRoot: item.sourceRoot,
      })),
    },
    http: {
      controllers: (catalog.http?.controllers ?? []).map((item) => ({
        name: item.name,
        path: item.path,
        routePrefix: item.routePrefix,
        endpoints: (item.endpoints ?? []).map((endpoint) => ({
          verb: endpoint.verb,
          route: endpoint.route,
          line: endpoint.line,
        })),
      })),
    },
    knowledgeSources: {
      agentGuides: catalog.knowledgeSources?.agentGuides ?? [],
    },
  };
  return {
    ready: true,
    source: 'sqlite-compiled-index',
    selectionMode,
    catalog: contextCatalog,
    symbols: selected.filter((row) => row.recordKind === 'symbol').map((row) => JSON.parse(row.payloadJson)),
    dependencyInjectionRegistrations: selected
      .filter((row) => row.recordKind === 'dependency-injection')
      .map((row) => JSON.parse(row.payloadJson)),
    frontendFeatures: selectionMode === 'changed-paths' ? [] : frontendSelected.filter((row) => row.recordKind === 'feature').map((row) => JSON.parse(row.payloadJson)),
    frontendFeatureCatalog: includeFrontendFeatures
      ? frontendSelected.filter((row) => row.recordKind === 'feature').map((row) => {
        const feature = JSON.parse(row.payloadJson);
        return { name: feature.name, root: feature.root };
      })
      : [],
    frontendSymbols: frontendSelected.filter((row) => row.recordKind === 'symbol').map((row) => JSON.parse(row.payloadJson)),
    frontendRoutes: frontendSelected.filter((row) => row.recordKind === 'route').map((row) => JSON.parse(row.payloadJson)),
    frontendLocalization: frontendSelected.filter((row) => row.recordKind === 'localization').map((row) => JSON.parse(row.payloadJson)),
    sourceHashes: {
      repositoryCatalog: catalogRow.contentHash,
      csharpSymbols: symbolIndex.contentHash,
      frontend: frontendIndex.contentHash,
    },
    sourceBytesVerified: {
      repositoryCatalog: Buffer.byteLength(catalogSourceText, 'utf8'),
      csharpSymbols: Buffer.byteLength(symbolSourceText, 'utf8'),
      frontend: Buffer.byteLength(frontendSourceText, 'utf8'),
    },
    scannedRecords,
    returnedRecords: selected.length + frontendSelected.length,
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
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
    SELECT category, record_key recordKey, path, source_path sourcePath, record_kind recordKind, payload_json payloadJson
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
    const sourceBody = file.language === 'powershell' || file.language === 'json'
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
  let compiledIndexes;
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
    compiledIndexes = refreshCompiledIndexes(database);
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
    try {
      database.exec('ROLLBACK');
    } catch (rollbackError) {
      error.rollbackError = rollbackError instanceof Error ? rollbackError.message : String(rollbackError);
    }
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
    compiledIndexes,
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

function backendContractContext(database, view, query, limit) {
  const started = performance.now();
  const sourcePath = '.llm-wiki/generated/backend-contract-index.json';
  const projectedHash = database.prepare("SELECT value FROM metadata WHERE key = 'query_source:contracts'").get()?.value;
  if (!projectedHash) {
    return { ready: false, unavailableReason: 'backend-contract-projection-missing', durationMs: Math.round((performance.now() - started) * 100) / 100 };
  }
  const currentHash = normalizedTextHash(readFileSync(resolve(repositoryRoot, sourcePath), 'utf8'));
  if (projectedHash !== currentHash) {
    return {
      ready: false,
      unavailableReason: 'backend-contract-projection-stale',
      sourceHashes: { projected: projectedHash, current: currentHash },
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }
  const supportedViews = new Set(['all', 'contracts', 'consumers', 'production', 'tests', 'ambiguous', 'unconsumed']);
  if (!supportedViews.has(view)) throw new Error(`Unsupported backend-contract view: ${view}`);
  const selectPayloads = (recordKind, predicate = '') => database.prepare(`
    SELECT payload_json payloadJson
    FROM query_documents q
    WHERE category = 'contracts' AND record_kind = ?
      AND (? = '' OR instr(lower(payload_json), lower(?)) > 0)
      ${predicate}
    ORDER BY source_ordinal
    LIMIT ?
  `).all(recordKind, query, query, limit).map((row) => JSON.parse(row.payloadJson));
  const groups = {};
  if (view === 'all' || view === 'contracts') groups.contracts = selectPayloads('contract');
  if (view === 'all' || view === 'consumers') groups.consumers = selectPayloads('consumer');
  if (view === 'production') groups.productionConsumers = selectPayloads('consumer', "AND COALESCE(json_extract(payload_json, '$.isTest'), 0) = 0");
  if (view === 'tests') groups.testConsumers = selectPayloads('consumer', "AND COALESCE(json_extract(payload_json, '$.isTest'), 0) = 1");
  if (view === 'ambiguous') groups.ambiguousContracts = selectPayloads('contract', "AND COALESCE(json_extract(payload_json, '$.ambiguous'), 0) = 1");
  if (view === 'unconsumed') groups.unconsumedContracts = selectPayloads('contract', `
    AND json_extract(q.payload_json, '$.name') COLLATE NOCASE NOT IN (
      SELECT DISTINCT json_extract(consumer.payload_json, '$.contract') COLLATE NOCASE
      FROM query_documents consumer
      WHERE consumer.category = 'contracts' AND consumer.record_kind = 'consumer'
        AND json_extract(consumer.payload_json, '$.contract') IS NOT NULL
    )
  `);
  return {
    ready: true,
    source: 'sqlite-query-documents',
    view,
    groups,
    sourceHash: projectedHash,
    scannedRecords: database.prepare("SELECT COUNT(*) count FROM query_documents WHERE category = 'contracts'").get().count,
    returnedRecords: Object.values(groups).reduce((count, records) => count + records.length, 0),
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
}

function frontendContractContext(database, view, query, limit) {
  const started = performance.now();
  const sourcePath = '.llm-wiki/generated/frontend-contract-index.json';
  const projectedHash = database.prepare("SELECT value FROM metadata WHERE key = 'query_source:frontend-contracts'").get()?.value;
  if (!projectedHash) {
    return { ready: false, unavailableReason: 'frontend-contract-projection-missing', durationMs: Math.round((performance.now() - started) * 100) / 100 };
  }
  const currentHash = normalizedTextHash(readFileSync(resolve(repositoryRoot, sourcePath), 'utf8'));
  if (projectedHash !== currentHash) {
    return {
      ready: false,
      unavailableReason: 'frontend-contract-projection-stale',
      sourceHashes: { projected: projectedHash, current: currentHash },
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }
  const supportedViews = new Set(['all', 'components', 'consumers', 'api', 'translations', 'spec-gaps']);
  if (!supportedViews.has(view)) throw new Error(`Unsupported frontend-contract view: ${view}`);
  const selectPayloads = (recordKind, predicate = '') => database.prepare(`
    SELECT payload_json payloadJson
    FROM query_documents
    WHERE category = 'frontend-contracts' AND record_kind = ?
      AND (? = '' OR instr(lower(payload_json), lower(?)) > 0)
      ${predicate}
    ORDER BY source_ordinal
    LIMIT ?
  `).all(recordKind, query, query, limit).map((row) => JSON.parse(row.payloadJson));
  const groups = {};
  if (view === 'all' || view === 'components') groups.components = selectPayloads('component');
  if (view === 'spec-gaps') groups.specGaps = selectPayloads('component', "AND json_extract(payload_json, '$.specPath') IS NULL");
  if (view === 'all' || view === 'consumers') groups.consumers = selectPayloads('consumer');
  if (view === 'all' || view === 'api') groups.apiCalls = selectPayloads('api-call');
  if (view === 'all' || view === 'translations') groups.translations = selectPayloads('translation');
  return {
    ready: true,
    source: 'sqlite-query-documents',
    view,
    groups,
    sourceHash: projectedHash,
    scannedRecords: database.prepare("SELECT COUNT(*) count FROM query_documents WHERE category = 'frontend-contracts'").get().count,
    returnedRecords: Object.values(groups).reduce((count, records) => count + records.length, 0),
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
}

function taskBriefImpactContext(database, changedPaths) {
  const started = performance.now();
  const sources = [
    ['quality', 'risks', '.llm-wiki/generated/quality-index.json'],
    ['runtime', 'runtime', '.llm-wiki/generated/runtime-topology.json'],
    ['sensitiveData', 'sensitive', '.llm-wiki/generated/sensitive-data-index.json'],
    ['frontendContract', 'frontend-contracts', '.llm-wiki/generated/frontend-contract-index.json'],
    ['domainData', 'domain', '.llm-wiki/generated/domain-data-index.json'],
    ['backendContract', 'contracts', '.llm-wiki/generated/backend-contract-index.json'],
    ['architectureHealth', 'architecture-health', '.llm-wiki/generated/architecture-health-index.json'],
  ];
  const sourceHashes = {};
  let sourceBytesVerified = 0;
  for (const [name, category, sourcePath] of sources) {
    const projectedHash = database.prepare("SELECT value FROM metadata WHERE key = ?").get(`query_source:${category}`)?.value;
    if (!projectedHash) {
      return {
        ready: false,
        unavailableReason: `task-brief-impact-${category}-projection-missing`,
        durationMs: Math.round((performance.now() - started) * 100) / 100,
      };
    }
    const sourceText = readFileSync(resolve(repositoryRoot, sourcePath), 'utf8');
    sourceBytesVerified += Buffer.byteLength(sourceText, 'utf8');
    const currentHash = normalizedTextHash(sourceText);
    if (projectedHash !== currentHash) {
      return {
        ready: false,
        unavailableReason: `task-brief-impact-${category}-projection-stale`,
        sourceHashes: { [name]: { projected: projectedHash, current: currentHash } },
        durationMs: Math.round((performance.now() - started) * 100) / 100,
      };
    }
    sourceHashes[name] = projectedHash;
  }

  const paths = [...new Set((changedPaths ?? []).map((path) => String(path).replaceAll('\\', '/')).filter(Boolean))]
    .sort((left, right) => left.toLowerCase().localeCompare(right.toLowerCase()) || left.localeCompare(right));
  const pathPlaceholders = paths.map(() => '?').join(', ');
  let sourceBytesMaterialized = 0;
  const parseRows = (rows) => rows.map((row) => {
    sourceBytesMaterialized += Buffer.byteLength(row.payloadJson, 'utf8');
    return JSON.parse(row.payloadJson);
  });
  const selectAll = (category, recordKind) => parseRows(database.prepare(`
    SELECT payload_json payloadJson
    FROM query_documents
    WHERE category = ? AND record_kind = ?
    ORDER BY source_ordinal
  `).all(category, recordKind));
  const selectByPaths = (category, recordKind, expressions = ['q.path']) => {
    if (paths.length === 0) return [];
    const predicates = expressions.map((expression) => `${expression} COLLATE NOCASE IN (${pathPlaceholders})`);
    const parameters = expressions.flatMap(() => paths);
    return parseRows(database.prepare(`
      SELECT q.payload_json payloadJson
      FROM query_documents q
      WHERE q.category = ? AND q.record_kind = ? AND (${predicates.join(' OR ')})
      ORDER BY q.source_ordinal
    `).all(category, recordKind, ...parameters));
  };

  const changedBackendContracts = (() => {
    if (paths.length === 0) return [];
    return parseRows(database.prepare(`
      SELECT q.payload_json payloadJson
      FROM query_documents q
      WHERE q.category = 'contracts' AND q.record_kind = 'contract'
        AND EXISTS (
          SELECT 1 FROM json_each(q.payload_json, '$.definitionPaths') definition
          WHERE definition.value COLLATE NOCASE IN (${pathPlaceholders})
        )
      ORDER BY q.source_ordinal
    `).all(...paths));
  })();
  const changedBackendContractNames = [...new Set(changedBackendContracts.map((item) => String(item.name ?? '')).filter(Boolean))];
  const changedBackendConsumers = changedBackendContractNames.length === 0 ? [] : (() => {
    const placeholders = changedBackendContractNames.map(() => '?').join(', ');
    return parseRows(database.prepare(`
      SELECT payload_json payloadJson
      FROM query_documents
      WHERE category = 'contracts' AND record_kind = 'consumer'
        AND json_extract(payload_json, '$.contract') COLLATE NOCASE IN (${placeholders})
      ORDER BY source_ordinal
    `).all(...changedBackendContractNames));
  })();

  const groups = {
    quality: {
      files: selectByPaths('risks', 'file').map(({ recordKind, ...item }) => item),
      criticalSymbols: selectByPaths('risks', 'criticalSymbol').map(({ recordKind, ...item }) => item),
    },
    runtime: {
      composeServices: selectByPaths('runtime', 'composeService'),
      hostedServices: selectByPaths('runtime', 'hostedService'),
      httpClients: selectByPaths('runtime', 'httpClient'),
      webhooks: selectByPaths('runtime', 'webhook'),
      recurringJobRegistrations: selectByPaths('runtime', 'recurringJob'),
    },
    sensitiveData: {
      fields: selectByPaths('sensitive', 'field'),
      boundaryFiles: selectByPaths('sensitive', 'boundary'),
      potentialLogging: selectByPaths('sensitive', 'potentialLogging'),
      externalTransfers: selectByPaths('sensitive', 'externalTransfer'),
    },
    frontendContract: {
      components: selectByPaths('frontend-contracts', 'component', ['q.path', "json_extract(q.payload_json, '$.templatePath')"]),
      apiCalls: selectByPaths('frontend-contracts', 'api-call'),
      translationUsage: selectByPaths('frontend-contracts', 'translation'),
      consumerEdges: selectByPaths('frontend-contracts', 'consumer', ["json_extract(q.payload_json, '$.componentPath')", 'q.path']),
    },
    domainData: {
      domainTypes: selectByPaths('domain', 'domainType'),
      invariants: selectByPaths('domain', 'invariant'),
      persistenceMappings: selectByPaths('domain', 'persistenceMapping'),
    },
    backendContract: {
      contracts: changedBackendContracts,
      consumerEdges: changedBackendConsumers,
    },
    architectureHealth: {
      projectDependencyViolations: selectAll('architecture-health', 'dependencyViolation'),
      untrackedProductionProjects: selectAll('architecture-health', 'untrackedProject'),
      moduleCycleNodes: selectAll('architecture-health', 'moduleCycle'),
      selectorUnreferencedComponents: selectByPaths('architecture-health', 'selectorUnreferenced', ['q.path', "json_extract(q.payload_json, '$.templatePath')"]),
      componentsWithoutDirectSpecs: selectByPaths('architecture-health', 'componentWithoutSpec'),
      criticalSymbolsWithoutTestReferences: selectByPaths('architecture-health', 'criticalSymbolWithoutTest'),
      explicitDebtMarkers: selectByPaths('architecture-health', 'debtMarker'),
    },
  };
  const returnedRecords = Object.values(groups).reduce((total, group) => total
    + Object.values(group).reduce((count, records) => count + records.length, 0), 0);
  const categories = sources.map(([, category]) => category);
  const categoryPlaceholders = categories.map(() => '?').join(', ');
  return {
    ready: true,
    source: 'sqlite-task-brief-impact',
    selectionMode: 'exact-changed-paths',
    changedPaths: paths,
    groups,
    sourceHashes,
    scannedRecords: database.prepare(`SELECT COUNT(*) count FROM query_documents WHERE category IN (${categoryPlaceholders})`).get(...categories).count,
    candidateRecords: returnedRecords,
    returnedRecords,
    sourceBytesVerified,
    sourceBytesMaterialized,
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
}

function sensitiveDataContext(database, view, queryTerms, scopePaths, filterRequested, limit) {
  const started = performance.now();
  const sourcePath = '.llm-wiki/generated/sensitive-data-index.json';
  const projectedHash = database.prepare("SELECT value FROM metadata WHERE key = 'query_source:sensitive'").get()?.value;
  if (!projectedHash) {
    return { ready: false, unavailableReason: 'sensitive-data-projection-missing', durationMs: Math.round((performance.now() - started) * 100) / 100 };
  }
  const sourceText = readFileSync(resolve(repositoryRoot, sourcePath), 'utf8');
  const currentHash = normalizedTextHash(sourceText);
  if (projectedHash !== currentHash) {
    return {
      ready: false,
      unavailableReason: 'sensitive-data-projection-stale',
      sourceHashes: { projected: projectedHash, current: currentHash },
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }

  const normalizedView = ['all', 'credential', 'identity', 'health', 'financial', 'privateContent', 'logging', 'boundaries', 'external'].includes(view) ? view : 'all';
  const predicates = [];
  const parameters = [];
  if (normalizedView === 'logging') {
    predicates.push("record_kind = 'potentialLogging'");
  } else if (normalizedView === 'boundaries') {
    predicates.push("record_kind = 'boundary'");
  } else if (normalizedView === 'external') {
    predicates.push("record_kind = 'externalTransfer'");
  } else if (normalizedView === 'all') {
    predicates.push("record_kind IN ('field', 'externalTransfer')");
  } else {
    predicates.push("record_kind = 'field'");
    predicates.push("json_extract(payload_json, '$.category') = ?");
    parameters.push(normalizedView);
  }
  const rows = database.prepare(`
    SELECT payload_json payloadJson
    FROM query_documents
    WHERE category = 'sensitive' AND ${predicates.join(' AND ')}
    ORDER BY source_ordinal
  `).all(...parameters);
  const terms = [...new Set((queryTerms ?? []).map((term) => String(term).toLowerCase()).filter(Boolean))];
  const paths = [...new Set((scopePaths ?? []).map((path) => String(path).replaceAll('\\', '/')).filter(Boolean))]
    .sort((left, right) => left.toLowerCase().localeCompare(right.toLowerCase()) || left.localeCompare(right));
  const matches = [];
  for (const row of rows) {
    const item = JSON.parse(row.payloadJson);
    let score = 0;
    let scopeMatch = false;
    if (filterRequested) {
      const searchText = row.payloadJson.toLowerCase();
      const matchCount = terms.filter((term) => searchText.includes(term)).length;
      const itemPath = String(item.path ?? '');
      scopeMatch = paths.some((scopePath) => {
        const scopeDirectory = extname(scopePath) ? dirname(scopePath) : scopePath;
        const normalizedDirectory = scopeDirectory.replaceAll('\\', '/').replace(/\/$/, '');
        return itemPath.toLowerCase() === scopePath.toLowerCase()
          || itemPath.startsWith(`${normalizedDirectory}/`);
      });
      score = matchCount + (scopeMatch ? 20 : 0);
      const minimumMatches = paths.length > 0 && !scopeMatch ? 2 : 1;
      if (!scopeMatch && matchCount < minimumMatches) continue;
    }
    matches.push({ item, score, scopeMatch });
    if (!filterRequested && matches.length >= limit) break;
  }
  const summaryRow = database.prepare(`
    SELECT payload_json payloadJson
    FROM query_documents
    WHERE category = 'sensitive' AND record_kind = 'summary'
    LIMIT 1
  `).get();
  const summary = summaryRow ? JSON.parse(summaryRow.payloadJson) : {};
  const sourceBytesMaterialized = matches.reduce((total, match) => total + Buffer.byteLength(JSON.stringify(match.item), 'utf8'), 0)
    + Buffer.byteLength(JSON.stringify(summary), 'utf8');
  return {
    ready: true,
    source: 'sqlite-sensitive-data',
    view: normalizedView,
    matches,
    summary,
    sourceHash: projectedHash,
    scannedRecords: database.prepare("SELECT COUNT(*) count FROM query_documents WHERE category = 'sensitive' AND record_kind <> 'summary'").get().count,
    candidateRecords: rows.length,
    returnedRecords: matches.length,
    sourceBytesVerified: Buffer.byteLength(sourceText, 'utf8'),
    sourceBytesMaterialized,
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
}

function frontendRuntimeOwnerContext(database, query, candidatePaths, limit) {
  const started = performance.now();
  const sourcePath = '.llm-wiki/generated/frontend-contract-index.json';
  const projectedHash = database.prepare("SELECT value FROM metadata WHERE key = 'query_source:frontend-contracts'").get()?.value;
  if (!projectedHash) {
    return { ready: false, unavailableReason: 'frontend-contract-projection-missing', durationMs: Math.round((performance.now() - started) * 100) / 100 };
  }
  const currentHash = normalizedTextHash(readFileSync(resolve(repositoryRoot, sourcePath), 'utf8'));
  if (projectedHash !== currentHash) {
    return {
      ready: false,
      unavailableReason: 'frontend-contract-projection-stale',
      sourceHashes: { projected: projectedHash, current: currentHash },
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }

  const ignored = new Set(['change', 'component', 'frontend', 'improve', 'layout', 'result', 'style', 'template', 'visual', 'with']);
  const normalizedQuery = String(query ?? '').toLowerCase();
  const tokens = [...new Set([...normalizedQuery.matchAll(/[\p{L}\p{N}]{3,}/gu)].map((match) => match[0]).filter((token) => !ignored.has(token)))].sort();
  if (/(^|[^\p{L}\p{N}])ai($|[^\p{L}\p{N}])/iu.test(String(query ?? '')) && !tokens.includes('ai')) tokens.push('ai');
  tokens.sort();
  const paths = [...new Set((candidatePaths ?? []).map((path) => String(path).replaceAll('\\', '/')).filter(Boolean))]
    .sort((left, right) => left.toLowerCase().localeCompare(right.toLowerCase()) || left.localeCompare(right));

  const predicates = [];
  const parameters = [];
  for (const token of tokens) {
    predicates.push('instr(lower(payload_json), ?) > 0');
    parameters.push(token.toLowerCase());
  }
  for (const path of paths) {
    const directory = dirname(path).replaceAll('\\', '/');
    predicates.push("(lower(json_extract(payload_json, '$.path')) = lower(?) OR lower(json_extract(payload_json, '$.templatePath')) = lower(?) OR lower(json_extract(payload_json, '$.path')) LIKE lower(?))");
    parameters.push(path, path, `${directory}/%`);
  }
  const candidateRows = predicates.length === 0 ? [] : database.prepare(`
    SELECT record_key recordKey, source_ordinal sourceOrdinal, payload_json payloadJson
    FROM query_documents
    WHERE category = 'frontend-contracts' AND record_kind = 'component'
      AND (${predicates.join(' OR ')})
    ORDER BY source_ordinal
  `).all(...parameters);
  const components = candidateRows.map((row) => ({ row, component: JSON.parse(row.payloadJson) }));
  const ranked = components.map(({ row, component }) => {
    const componentPath = String(component.path ?? '');
    const componentDirectory = dirname(componentPath).replaceAll('\\', '/');
    const explicit = paths.some((path) => path.toLowerCase() === componentPath.toLowerCase()
      || path.toLowerCase() === String(component.templatePath ?? '').toLowerCase()
      || dirname(path).replaceAll('\\', '/').toLowerCase() === componentDirectory.toLowerCase());
    const inputNames = (component.inputs ?? []).map((item) => String(item?.name ?? '')).filter(Boolean);
    const outputNames = (component.outputs ?? []).map((item) => String(item?.name ?? '')).filter(Boolean);
    const search = `${component.class ?? ''} ${component.selector ?? ''} ${componentPath} ${component.templatePath ?? ''} ${inputNames.join(' ')} ${outputNames.join(' ')}`.toLowerCase();
    const semanticScore = tokens.filter((token) => search.includes(token)).length;
    return { row, component, score: semanticScore + (explicit ? 100 : 0), explicit };
  }).filter((item) => item.explicit || item.score > 0);
  const maximumScore = ranked.length === 0 ? null : Math.max(...ranked.map((item) => item.score));
  const owners = ranked
    .filter((item) => item.score === maximumScore)
    .sort((left, right) => String(left.component.path ?? '').toLowerCase().localeCompare(String(right.component.path ?? '').toLowerCase())
      || String(left.component.path ?? '').localeCompare(String(right.component.path ?? '')))
    .slice(0, limit);

  const usedRecords = new Set(owners.map((owner) => owner.row.recordKey));
  const findFirstEdge = database.prepare(`
    SELECT record_key recordKey, payload_json payloadJson
    FROM query_documents
    WHERE category = 'frontend-contracts' AND record_kind = 'consumer'
      AND json_extract(payload_json, '$.componentPath') = ?
    ORDER BY json_extract(payload_json, '$.consumerPath') COLLATE NOCASE, source_ordinal
    LIMIT 1
  `);
  const findComponentByTemplate = database.prepare(`
    SELECT record_key recordKey, payload_json payloadJson
    FROM query_documents
    WHERE category = 'frontend-contracts' AND record_kind = 'component'
      AND json_extract(payload_json, '$.templatePath') = ? COLLATE NOCASE
    ORDER BY source_ordinal
    LIMIT 1
  `);
  const uniqueSorted = (values) => {
    const unique = new Map();
    for (const value of values.map((item) => String(item ?? '')).filter(Boolean)) {
      if (!unique.has(value.toLowerCase())) unique.set(value.toLowerCase(), value);
    }
    return [...unique.values()].sort((left, right) => left.toLowerCase().localeCompare(right.toLowerCase()) || left.localeCompare(right));
  };
  const runtimeOwners = owners.map((match) => {
    const component = match.component;
    const chain = [];
    const visited = new Set();
    let current = component;
    for (let depth = 0; depth <= 5; depth += 1) {
      const currentPath = String(current?.path ?? '');
      if (!current || visited.has(currentPath)) break;
      visited.add(currentPath);
      const edgeRow = findFirstEdge.get(currentPath);
      if (!edgeRow) break;
      usedRecords.add(edgeRow.recordKey);
      const edge = JSON.parse(edgeRow.payloadJson);
      const consumerRow = findComponentByTemplate.get(String(edge.consumerPath ?? ''));
      if (consumerRow) usedRecords.add(consumerRow.recordKey);
      const consumer = consumerRow ? JSON.parse(consumerRow.payloadJson) : null;
      chain.push({
        depth: depth + 1,
        selector: String(edge.selector ?? ''),
        renderedBy: String(edge.consumerPath ?? ''),
        consumerComponent: String(consumer?.class ?? ''),
        consumerPath: String(consumer?.path ?? ''),
      });
      current = consumer;
    }
    const componentPath = String(component.path ?? '');
    const directory = dirname(componentPath).replaceAll('\\', '/');
    const baseName = basename(componentPath, extname(componentPath));
    const stylePath = `${directory}/${baseName}.scss`;
    return {
      class: String(component.class ?? ''),
      selector: String(component.selector ?? ''),
      componentPath,
      templatePath: String(component.templatePath ?? ''),
      stylePath,
      specPath: String(component.specPath ?? ''),
      score: match.score,
      explicitPathMatch: match.explicit,
      renderChain: chain,
      recommendedScope: uniqueSorted([componentPath, component.templatePath, stylePath, component.specPath]),
    };
  });
  const runtimeOwner = {
    schemaVersion: 1,
    query: String(query ?? ''),
    candidatePaths: paths,
    ownerCount: runtimeOwners.length,
    confidence: runtimeOwners.length === 1 && (runtimeOwners[0].explicitPathMatch || runtimeOwners[0].score >= 2)
      ? 'high' : runtimeOwners.length > 0 ? 'medium' : 'low',
    owners: runtimeOwners,
    note: 'Confirm the visible entry action and rendered consumer chain in current templates before editing the recommended scope.',
  };
  return {
    ready: true,
    source: 'sqlite-query-documents',
    sourceHash: projectedHash,
    scannedRecords: database.prepare("SELECT COUNT(*) count FROM query_documents WHERE category = 'frontend-contracts'").get().count,
    candidateRecords: candidateRows.length,
    returnedRecords: usedRecords.size,
    runtimeOwner,
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
}

function frontendTraceContext(database, query, limit) {
  const started = performance.now();
  const frontendPath = '.llm-wiki/generated/frontend-index.json';
  const contractPath = '.llm-wiki/generated/frontend-contract-index.json';
  const projectedFrontendHash = database.prepare("SELECT content_hash contentHash FROM compiled_indexes WHERE index_name = 'frontend'").get()?.contentHash;
  const projectedContractHash = database.prepare("SELECT value FROM metadata WHERE key = 'query_source:frontend-contracts'").get()?.value;
  if (!projectedFrontendHash || !projectedContractHash) {
    return { ready: false, unavailableReason: 'frontend-trace-projection-missing', durationMs: Math.round((performance.now() - started) * 100) / 100 };
  }
  const currentFrontendHash = normalizedTextHash(readFileSync(resolve(repositoryRoot, frontendPath), 'utf8'));
  const currentContractHash = normalizedTextHash(readFileSync(resolve(repositoryRoot, contractPath), 'utf8'));
  if (projectedFrontendHash !== currentFrontendHash || projectedContractHash !== currentContractHash) {
    return {
      ready: false,
      unavailableReason: 'frontend-trace-projection-stale',
      sourceHashes: {
        frontend: { projected: projectedFrontendHash, current: currentFrontendHash },
        frontendContract: { projected: projectedContractHash, current: currentContractHash },
      },
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }

  const queryText = String(query ?? '').toLowerCase();
  const queryTerms = [...new Set(queryText.match(/[a-z0-9]+/g) ?? [])].filter((term) => term.length >= 3).sort();
  const symbolRows = database.prepare(`
    SELECT record_key recordKey, source_ordinal sourceOrdinal, payload_json payloadJson
    FROM compiled_index_records
    WHERE index_name = 'frontend' AND record_kind = 'symbol'
    ORDER BY source_ordinal
  `).all();
  const symbols = symbolRows.map((row) => ({ row, value: JSON.parse(row.payloadJson) }));
  const matches = symbols.map(({ row, value: symbol }) => {
    const selector = String(symbol.selector ?? '');
    const searchable = `${symbol.name ?? ''} ${selector} ${symbol.path ?? ''}`.toLowerCase();
    const matchedTerms = queryTerms.filter((term) => searchable.includes(term));
    let score = matchedTerms.length * 10;
    if (String(symbol.name ?? '').toLowerCase() === queryText || selector.toLowerCase() === queryText) score += 100;
    return { row, symbol, score, matchedTerms };
  }).filter((match) => match.score > 0)
    .sort((left, right) => right.score - left.score
      || String(left.symbol.name ?? '').toLowerCase().localeCompare(String(right.symbol.name ?? '').toLowerCase())
      || left.row.sourceOrdinal - right.row.sourceOrdinal)
    .slice(0, limit);
  const scannedRecords = symbolRows.length + database.prepare("SELECT COUNT(*) count FROM compiled_index_records WHERE index_name = 'frontend' AND record_kind = 'route'").get().count
    + database.prepare("SELECT COUNT(*) count FROM query_documents WHERE category = 'frontend-contracts'").get().count;
  if (matches.length === 0) {
    return {
      ready: true,
      source: 'sqlite-compiled-trace',
      sourceHashes: { frontend: projectedFrontendHash, frontendContract: projectedContractHash },
      scannedRecords,
      candidateRecords: 0,
      returnedRecords: 0,
      trace: { matched: false, query: String(query ?? ''), traces: [] },
      durationMs: Math.round((performance.now() - started) * 100) / 100,
    };
  }

  const routeRows = database.prepare(`
    SELECT record_key recordKey, source_ordinal sourceOrdinal, payload_json payloadJson
    FROM compiled_index_records
    WHERE index_name = 'frontend' AND record_kind = 'route'
    ORDER BY source_ordinal
  `).all();
  const routes = routeRows.map((row) => ({ row, value: JSON.parse(row.payloadJson) }));
  const documentCache = new Map();
  const getDocument = (path) => {
    if (!documentCache.has(path)) {
      const absolutePath = resolve(repositoryRoot, path);
      documentCache.set(path, existsSync(absolutePath) ? readFileSync(absolutePath, 'utf8') : null);
    }
    return documentCache.get(path);
  };
  const compareText = (left, right) => String(left ?? '').toLowerCase().localeCompare(String(right ?? '').toLowerCase())
    || String(left ?? '').localeCompare(String(right ?? ''));
  const uniqueSorted = (items, compare, key) => {
    const sorted = [...items].sort(compare);
    const seen = new Set();
    return sorted.filter((item) => {
      const identity = key(item).toLowerCase();
      if (seen.has(identity)) return false;
      seen.add(identity);
      return true;
    });
  };
  const escapeRegex = (value) => String(value).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const findConsumers = (symbolName, excludePath) => {
    const pattern = new RegExp(`\\b${escapeRegex(symbolName)}\\b`, 'iu');
    const found = [];
    for (const { value: candidate } of symbols) {
      if (String(candidate.path ?? '').toLowerCase() === String(excludePath ?? '').toLowerCase()) continue;
      const source = getDocument(String(candidate.path ?? ''));
      if (source !== null && pattern.test(source)) {
        found.push({ name: candidate.name, role: candidate.role, path: candidate.path, line: candidate.line });
      }
    }
    return uniqueSorted(found,
      (left, right) => compareText(left.path, right.path) || compareText(left.name, right.name),
      (item) => `${item.path}\0${item.name}`);
  };
  const componentByClass = database.prepare(`
    SELECT record_key recordKey, payload_json payloadJson
    FROM query_documents
    WHERE category = 'frontend-contracts' AND record_kind = 'component'
      AND json_extract(payload_json, '$.class') = ? COLLATE NOCASE
    ORDER BY source_ordinal LIMIT 1
  `);
  const consumersByComponent = database.prepare(`
    SELECT record_key recordKey, payload_json payloadJson
    FROM query_documents
    WHERE category = 'frontend-contracts' AND record_kind = 'consumer'
      AND json_extract(payload_json, '$.component') = ? COLLATE NOCASE
    ORDER BY source_ordinal
  `);
  const usedRecords = new Set();
  const traces = [];
  for (const match of matches) {
    usedRecords.add(`frontend:${match.row.recordKey}`);
    const target = match.symbol;
    const related = [];
    const queue = [{ symbol: target, depth: 0, relation: 'target' }];
    const visited = new Set();
    while (queue.length > 0 && related.length < 40) {
      const current = queue.shift();
      if (visited.has(String(current.symbol.name ?? ''))) continue;
      visited.add(String(current.symbol.name ?? ''));
      if (current.depth > 0) {
        related.push({
          name: current.symbol.name,
          role: current.symbol.role,
          path: current.symbol.path,
          line: current.symbol.line,
          relation: current.relation,
          depth: current.depth,
        });
      }
      if (current.depth >= 6) continue;
      for (const consumer of findConsumers(String(current.symbol.name ?? ''), String(current.symbol.path ?? ''))) {
        queue.push({ symbol: consumer, depth: current.depth + 1, relation: 'consumer' });
      }
      const source = getDocument(String(current.symbol.path ?? ''));
      if (source !== null) {
        for (const { value: dependency } of symbols) {
          if (String(dependency.name ?? '').toLowerCase() === String(current.symbol.name ?? '').toLowerCase()
            || !['Component', 'Facade', 'Service', 'ApiClient'].includes(String(dependency.role ?? ''))
            || !/^Ai/i.test(String(dependency.name ?? ''))) continue;
          if (new RegExp(`\\b${escapeRegex(dependency.name)}\\b`, 'iu').test(source)) {
            queue.push({ symbol: dependency, depth: current.depth + 1, relation: 'dependency' });
          }
        }
      }
    }
    const upstream = related.filter((item) => item.relation === 'consumer');
    const componentRow = componentByClass.get(String(target.name ?? ''));
    if (componentRow) usedRecords.add(`contract:${componentRow.recordKey}`);
    const componentContract = componentRow ? JSON.parse(componentRow.payloadJson) : null;
    const selectorRows = componentContract ? consumersByComponent.all(String(target.name ?? '')) : [];
    for (const row of selectorRows) usedRecords.add(`contract:${row.recordKey}`);
    const selectorValues = selectorRows.map((row) => JSON.parse(row.payloadJson));
    const selectorConsumers = selectorValues.length === 0 ? null : selectorValues.length === 1 ? selectorValues[0] : selectorValues;
    const relatedPaths = uniqueSorted([
      String(target.path ?? ''),
      ...related.map((item) => String(item.path ?? '')),
      ...selectorValues.map((item) => String(item.consumerPath ?? '')),
    ].filter(Boolean), compareText, (item) => item);
    let apiRows = [];
    if (relatedPaths.length > 0) {
      const placeholders = relatedPaths.map(() => '?').join(',');
      apiRows = database.prepare(`
        SELECT record_key recordKey, payload_json payloadJson
        FROM query_documents
        WHERE category = 'frontend-contracts' AND record_kind = 'api-call'
          AND lower(path) IN (${placeholders})
      `).all(...relatedPaths.map((path) => path.toLowerCase()));
    }
    for (const row of apiRows) usedRecords.add(`contract:${row.recordKey}`);
    const apiCalls = uniqueSorted(apiRows.map((row) => JSON.parse(row.payloadJson)),
      (left, right) => compareText(left.path, right.path) || Number(left.line ?? 0) - Number(right.line ?? 0),
      (item) => `${item.path}\0${item.line}`);
    const relatedFeatures = uniqueSorted(related.map((item) => {
      const featureMatch = String(item.path ?? '').match(/\/features\/(?<feature>[^/]+)\//i);
      return featureMatch?.groups?.feature ?? '';
    }).filter(Boolean), compareText, (item) => item);
    const selectedRoutes = uniqueSorted(routes.filter(({ value: route }) => relatedFeatures.some((feature) =>
      String(route.path ?? '').toLowerCase() === feature.toLowerCase()
      || new RegExp(`/features/${escapeRegex(feature)}/`, 'iu').test(String(route.source ?? '')))).map(({ row, value }) => {
        usedRecords.add(`frontend:${row.recordKey}`);
        return value;
      }),
    (left, right) => compareText(left.source, right.source) || Number(left.line ?? 0) - Number(right.line ?? 0),
    (item) => `${item.source}\0${item.line}`);
    const sortedRelated = uniqueSorted(related,
      (left, right) => left.depth - right.depth || compareText(left.relation, right.relation) || compareText(left.path, right.path) || compareText(left.name, right.name),
      (item) => `${item.depth}\0${item.relation}\0${item.path}\0${item.name}`);
    const sortedUpstream = uniqueSorted(upstream,
      (left, right) => left.depth - right.depth || compareText(left.path, right.path) || compareText(left.name, right.name),
      (item) => `${item.depth}\0${item.path}\0${item.name}`);
    const testPaths = uniqueSorted([String(target.path ?? ''), ...related.map((item) => String(item.path ?? ''))]
      .map((path) => path.replace(/\.ts$/i, '.spec.ts')).filter((path) => path && existsSync(resolve(repositoryRoot, path))), compareText, (item) => item);
    traces.push({
      symbol: target,
      match: { score: match.score, queryTerms, matchedTerms: match.matchedTerms },
      routes: selectedRoutes,
      relatedSymbols: sortedRelated,
      upstreamConsumers: sortedUpstream,
      selectorConsumers,
      contract: componentContract,
      apiCalls,
      tests: testPaths,
    });
  }
  return {
    ready: true,
    source: 'sqlite-compiled-trace',
    sourceHashes: { frontend: projectedFrontendHash, frontendContract: projectedContractHash },
    scannedRecords,
    candidateRecords: matches.length,
    returnedRecords: usedRecords.size,
    trace: { matched: true, query: String(query ?? ''), traces },
    durationMs: Math.round((performance.now() - started) * 100) / 100,
  };
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
  const configuredTerms = contextSearchRanking.queryTermExpansions ?? {};
  for (const term of direct) {
    if (Object.hasOwn(configuredTerms, term)) expanded.push(...configuredTerms[term]);
    for (const [prefixGroup, expansions] of Object.entries(contextSearchRanking.queryPrefixExpansions ?? {})) {
      if (prefixGroup.split('|').some((prefix) => term.startsWith(prefix))) expanded.push(...expansions);
    }
  }
  return expanded;
}

function negatedRoleTermGroups(query) {
  const policy = contextSearchRanking.negatedRolePenalty ?? {};
  const markers = (policy.markers ?? ['not', 'не']).map((marker) => String(marker).toLowerCase());
  const roleTerms = new Set((policy.roleTerms ?? []).map((term) => String(term).toLowerCase()));
  if (markers.length === 0 || roleTerms.size === 0) return [];
  const markerPattern = markers.map((marker) => marker.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|');
  const expression = new RegExp(`(?:^|\\s)(?:${markerPattern})\\s+(.+?)(?=(?:\\s+(?:и\\s+)?(?:${markerPattern})\\s+)|[,;:—–]|\\s+(?:а|but)\\s+|$)`, 'giu');
  return [...expandSearchText(query).toLowerCase().matchAll(expression)]
    .map((match) => directSearchTerms(match[1]).flatMap((term) =>
      roleTerms.has(term)
        ? [term]
        : configuredSearchTermExpansions([term]).filter((expanded) => roleTerms.has(expanded))))
    .map((terms) => [...new Set(terms)])
    .filter((terms) => terms.length > 0);
}

function negatedRoleAlternatives(negativeRoleGroups) {
  const configured = contextSearchRanking.negatedRoleAlternatives ?? {};
  return [...new Set(negativeRoleGroups
    .flat()
    .flatMap((term) => configured[term] ?? []))];
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
  const negativeRoleGroups = negatedRoleTermGroups(query);
  const negativeRoleTerms = new Set(negativeRoleGroups.flat());
  const alternativeTerms = negatedRoleAlternatives(negativeRoleGroups);
  const maximumQueryTerms = Number(contextSearchRanking.maximumQueryTerms ?? 24);
  const terms = [...new Set([
    ...searchTerms(query).filter((term) => !negativeRoleTerms.has(term)),
    ...alternativeTerms,
  ])].slice(0, maximumQueryTerms);
  const directTerms = directSearchTerms(query).filter((term) => !negativeRoleTerms.has(term));
  const boostTerms = [...new Set([
    ...rankingTerms(query).filter((term) => !negativeRoleTerms.has(term)),
    ...alternativeTerms,
  ])].slice(0, maximumQueryTerms);
  const explicitlyRequestsTest = boostTerms.includes('test');
  const explicitlyRequestsMcp = /(^|\W)mcp(\W|$)/iu.test(String(query));
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
  const candidateLimit = Number(contextSearchRanking.candidatePoolLimit ?? 500);
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
  const identityLimit = Number(contextSearchRanking.identityCandidatePoolLimit ?? 100);
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
  const normalizedQueryForRuntime = expandSearchText(query).toLowerCase();
  const runtimeSuffixes = /(^|\s)(?:node|javascript|mjs)(\s|$)/.test(normalizedQueryForRuntime)
    ? ['%.mjs', '%.js', '%.cjs']
    : !explicitlyRequestsMcp && /(^|\s)(?:powershell|pwsh|ps1)(\s|$)/.test(normalizedQueryForRuntime) ? ['%.ps1'] : [];
  const runtimeCandidatesToPrepend = [];
  for (const suffix of runtimeSuffixes) {
    const runtimeCandidates = database.prepare(`
      SELECT record_type recordType, record_key recordKey, path, source_path sourcePath,
        category, title, 0.0 lexicalRank
      FROM context_search
      WHERE path LIKE ?
      ORDER BY path
    `).all(suffix);
    for (const runtimeCandidate of runtimeCandidates) {
      const key = `${runtimeCandidate.recordType}\0${runtimeCandidate.recordKey}\0${runtimeCandidate.path}`;
      if (candidateIndexes.has(key)) continue;
      candidateIndexes.set(key, candidates.length);
      runtimeCandidatesToPrepend.push(runtimeCandidate);
    }
  }
  candidates.unshift(...runtimeCandidatesToPrepend);
  const normalizedQuery = expandSearchText(query).toLowerCase();
  const moduleTerm = String(filters.module ?? '').toLowerCase();
  const scopePaths = String(filters.path ?? '').split(';').filter(Boolean).map((path) => path.replaceAll('\\', '/').toLowerCase());
  const changeType = String(filters.changeType ?? 'Any').toLowerCase();
  const eligibleTermsForBoost = (boost) => boost.directOnly ? directTerms : boostTerms;
  const boostMatchesQuery = (boost, defaultMinimum = 1) => {
    const eligibleTerms = eligibleTermsForBoost(boost);
    if ((boost.excludedQueryTerms ?? []).some((term) => eligibleTerms.includes(String(term).toLowerCase()))) return false;
    return (boost.queryTerms ?? []).filter((term) => eligibleTerms.includes(String(term).toLowerCase())).length
      >= Number(boost.minimumMatches ?? defaultMinimum);
  };
  const boostMatchesChangeType = (boost) => !(boost.changeTypes?.length)
    || boost.changeTypes.some((candidate) => String(candidate).toLowerCase() === changeType);
  const applicableIdentityBoosts = (contextSearchRanking.identityBoosts ?? [])
    .filter((boost) => !(explicitlyRequestsMcp && boost.id === 'explicit-powershell-file-intent'))
    .filter((boost) => boostMatchesChangeType(boost) && boostMatchesQuery(boost));
  const applicableStructuralRoleBoosts = (contextSearchRanking.structuralRoleBoosts ?? [])
    .filter((boost) => boostMatchesChangeType(boost) && boostMatchesQuery(boost, 0));
  const applicablePathBoosts = (contextSearchRanking.pathBoosts ?? [])
    .filter((boost) => boostMatchesQuery(boost));
  const ranked = candidates.map((item, index) => {
    const path = String(item.path ?? '').replaceAll('\\', '/');
    const normalizedPath = path.toLowerCase();
    const isTest = /(^|\/)(?:tests?|[^/]+\.tests?)(\/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$/i.test(path);
    const fileName = basename(path);
    const isExplicitTestCandidate = isTest ||
      (/^test[-_.]?/i.test(fileName) && /\.(?:cs|ps1|ts|js|mjs|cjs)$/i.test(fileName));
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
    const genericAffinity = contextSearchRanking.genericAffinities ?? {};
    const explicitRoleMatches = (genericAffinity.roleTerms ?? []).filter((term) => {
      const normalizedTerm = String(term).toLowerCase();
      if (['consumer', 'consumers'].includes(normalizedTerm) && !boostTerms.includes('powershell')) return false;
      return boostTerms.includes(normalizedTerm) && searchableFileIdentity.includes(normalizedTerm);
    });
    const explicitRoleScore = Math.min(
      explicitRoleMatches.reduce((total, term) => total + Number(
        genericAffinity.roleScoreOverrides?.[term] ?? genericAffinity.roleScorePerMatch ?? 0), 0),
      Number(genericAffinity.maximumRoleScore ?? Number.MAX_SAFE_INTEGER));
    if (explicitRoleScore > 0) {
      score += explicitRoleScore;
      reasons.push(`generic file-role affinity ${explicitRoleMatches.join(', ')}`);
    }
    const applyGenericPathAffinity = (id, intentTerms, pathValues, value, suffix = false) => {
      const intentMatched = (intentTerms ?? []).some((term) => boostTerms.includes(String(term).toLowerCase()));
      const pathMatched = (pathValues ?? []).some((candidate) => suffix
        ? normalizedPath.endsWith(String(candidate).toLowerCase())
        : normalizedPath.includes(String(candidate).replaceAll('\\', '/').toLowerCase()));
      if (!intentMatched || !pathMatched || Number(value ?? 0) === 0) return;
      score += Number(value);
      reasons.push(`generic ${id} affinity`);
    };
    if (changeType !== 'database') {
      applyGenericPathAffinity('domain-layer', genericAffinity.domainIntentTerms,
        genericAffinity.domainPathPrefixes, genericAffinity.domainScore);
    }
    applyGenericPathAffinity('api-layer', genericAffinity.apiIntentTerms,
      genericAffinity.apiPathFragments, genericAffinity.apiScore);
    const applyChangeTypePathAffinity = (id, expectedType, pathValues, value) => {
      if (changeType !== expectedType || !(pathValues ?? []).some((candidate) =>
        normalizedPath.includes(String(candidate).replaceAll('\\', '/').toLowerCase()))) return;
      score += Number(value ?? 0);
      reasons.push(`generic ${id} affinity`);
    };
    applyChangeTypePathAffinity('api-change-type', 'api', genericAffinity.apiPathFragments, genericAffinity.apiScore);
    applyChangeTypePathAffinity('database-change-type', 'database', genericAffinity.databasePathFragments, genericAffinity.databaseScore);
    applyGenericPathAffinity('admin-scope', genericAffinity.adminIntentTerms,
      genericAffinity.adminPathFragments, genericAffinity.adminScore);
    applyGenericPathAffinity('integration-layer', genericAffinity.integrationIntentTerms,
      genericAffinity.integrationPathPrefixes, genericAffinity.integrationScore);
    applyGenericPathAffinity('node-runtime', genericAffinity.nodeIntentTerms,
      genericAffinity.nodePathSuffixes, genericAffinity.nodeScore, true);
    if (!explicitlyRequestsMcp) {
      applyGenericPathAffinity('powershell-runtime', genericAffinity.powershellIntentTerms,
        genericAffinity.powershellPathSuffixes, genericAffinity.powershellScore, true);
    }
    if (!['frontend', 'tests'].includes(changeType) && !boostTerms.includes('how') && !explicitlyRequestsMcp) {
      applyGenericPathAffinity('wiki-tooling', genericAffinity.wikiToolIntentTerms,
        genericAffinity.wikiToolPathPrefixes, genericAffinity.wikiToolScore);
    }
    if (changeType === 'tests' && isExplicitTestCandidate && explicitlyRequestsTest) {
      const explicitTestAffinity = contextSearchRanking.explicitTestAffinity ?? {};
      const explicitTestScore = Math.min(
        identityMatches.length * Number(explicitTestAffinity.scorePerMatch ?? 0),
        Number(explicitTestAffinity.maximumScore ?? 0));
      score += explicitTestScore;
      if (explicitTestScore > 0) reasons.push(`explicit test behavior affinity ${identityMatches.length} terms`);
    }
    if (changeType === 'tests' && !isExplicitTestCandidate && explicitlyRequestsTest) {
      score -= Number(contextSearchRanking.explicitTestAffinity?.nonTestPenalty ?? 0);
      reasons.push('production candidate penalty for explicit test intent');
    }
    for (const boost of applicableIdentityBoosts) {
      const eligibleQueryTerms = boost.directOnly ? directTerms : boostTerms;
      if (changeType === 'tests' && isTest && !String(boost.id ?? '').toLowerCase().includes('test')) continue;
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
    for (const boost of applicableStructuralRoleBoosts) {
      if (boost.excludeTests === true && isTest) continue;
      if (boost.recordTypes?.length
        && !boost.recordTypes.some((candidate) => String(candidate).toLowerCase() === String(item.recordType ?? '').toLowerCase())) continue;
      if (boost.pathPrefixes?.length
        && !boost.pathPrefixes.some((prefix) => normalizedPath.startsWith(String(prefix).replaceAll('\\', '/').toLowerCase()))) continue;
      if (boost.excludedPathPrefixes?.length
        && boost.excludedPathPrefixes.some((prefix) => normalizedPath.startsWith(String(prefix).replaceAll('\\', '/').toLowerCase()))) continue;
      if (boost.pathSuffixes?.length
        && !boost.pathSuffixes.some((suffix) => normalizedPath.endsWith(String(suffix).toLowerCase()))) continue;
      const eligibleQueryTerms = boost.directOnly ? directTerms : boostTerms;
      const queryMatches = (boost.queryTerms ?? []).filter((term) => eligibleQueryTerms.includes(String(term).toLowerCase()));
      const eligibleIdentity = boost.identityScope === 'file'
        ? searchableFileIdentity
        : boost.identityScope === 'identity' ? searchableIdentity : searchablePath;
      const candidateMatches = (boost.candidateTerms ?? []).filter((term) =>
        eligibleIdentity.includes(String(term).toLowerCase()));
      const minimumAffinityTermLength = Number(boost.minimumAffinityTermLength ?? affinity.minimumTermLength ?? 3);
      const affinityQueryTerms = boost.affinityDirectOnly === false ? boostTerms : directTerms;
      const queryIdentityMatches = affinityQueryTerms.filter((term) =>
        term.length >= minimumAffinityTermLength && eligibleIdentity.includes(term));
      if (queryMatches.length < Number(boost.minimumMatches ?? 0)
        || candidateMatches.length < Number(boost.minimumCandidateMatches ?? 0)
        || queryIdentityMatches.length < Number(boost.minimumQueryIdentityMatches ?? 0)) continue;
      const variableScore = Math.min(
        queryIdentityMatches.length * Number(boost.scorePerQueryIdentityMatch ?? 0),
        Number(boost.maximumQueryIdentityScore ?? Number.MAX_SAFE_INTEGER));
      score += Number(boost.score ?? 0) + variableScore;
      matchedRankingPolicy = true;
      reasons.push(`structural role ${boost.id} (${queryIdentityMatches.join(', ')})`);
    }
    for (const boost of applicablePathBoosts) {
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
    const negatedRolePolicy = contextSearchRanking.negatedRolePenalty ?? {};
    for (const negativeRoleGroup of negativeRoleGroups) {
      const matchedNegativeRoles = negativeRoleGroup.filter((term) => searchableFileIdentity.includes(term));
      const requiredMatches = Math.min(2, negativeRoleGroup.length);
      if (matchedNegativeRoles.length < requiredMatches) continue;
      const penalty = Math.min(
        matchedNegativeRoles.length * Number(negatedRolePolicy.scorePerMatch ?? 0),
        Number(negatedRolePolicy.maximumScorePerPhrase ?? Number.MAX_SAFE_INTEGER));
      score -= penalty;
      reasons.push(`negated role penalty ${matchedNegativeRoles.join(', ')}`);
    }
    if (isExplicitTestCandidate && changeType !== 'tests') {
      score -= Number(contextSearchRanking.nonTestPenalty ?? 25);
      reasons.push('test candidate ranked after production');
    }
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
    if (changeType !== 'tests' && /\.[^./]+\.cs$/i.test(path)) {
      score -= Number(contextSearchRanking.companionFilePenalty ?? 0);
      reasons.push('companion file ranked after primary declaration');
    }
    if (isCode) score += 20;
    if (item.recordType === 'agent-guide') {
      const requestsGuidance = boostTerms.some((term) =>
        ['agent', 'agents', 'guide', 'guidance', 'instruction', 'instructions', 'policy', 'rule', 'rules'].includes(term));
      score += requestsGuidance ? Number(contextSearchRanking.agentGuideBoost ?? 15) : -Number(contextSearchRanking.agentGuideBoost ?? 15);
      reasons.push(requestsGuidance ? 'agent guide affinity' : 'agent guide penalty for code intent');
    }
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
  else if (action === 'search-batch') {
    const inputPath = resolve(repositoryRoot, options.input ?? '');
    const requests = JSON.parse(readFileSync(inputPath, 'utf8'));
    result = {
      requestCount: requests.length,
      results: requests.map((request) => searchContext(database, request.query ?? '', Number(request.limit ?? 20), {
        module: request.module, path: request.path, changeType: request.changeType,
      })),
    };
  }
  else if (action === 'relations') result = relations(database, (options.path ?? '').split(';').filter(Boolean), (options.kind ?? '').split(';').filter(Boolean), Number(options.limit ?? 100));
  else if (action === 'coverage') result = coverage(database);
  else if (action === 'fingerprint') result = fingerprint(database, (options.path ?? '').split(';').filter(Boolean));
  else if (action === 'query') result = queryDocuments(database, options.category ?? 'modules', options.query ?? '', Number(options.limit ?? 50));
  else if (action === 'backend-contract') result = backendContractContext(database, options.view ?? 'all', options.query ?? '', Number(options.limit ?? 30));
  else if (action === 'frontend-contract') result = frontendContractContext(database, options.view ?? 'all', options.query ?? '', Number(options.limit ?? 30));
  else if (action === 'task-brief-impact') result = taskBriefImpactContext(database, (options.path ?? '').split(';').filter(Boolean));
  else if (action === 'sensitive-data') result = sensitiveDataContext(database, options.view ?? 'all', (options.query ?? '').split(';').filter(Boolean), (options.path ?? '').split(';').filter(Boolean), options.filter === 'true', Number(options.limit ?? 30));
  else if (action === 'frontend-runtime-owner') result = frontendRuntimeOwnerContext(database, options.query ?? '', (options.path ?? '').split(';').filter(Boolean), Number(options.limit ?? 5));
  else if (action === 'frontend-trace') result = frontendTraceContext(database, options.query ?? '', Number(options.limit ?? 10));
  else if (action === 'compiled-context') result = compiledContext(database, options);
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
    compiledIndexes: database.prepare('SELECT index_name indexName, source_path sourcePath, content_hash contentHash FROM compiled_indexes ORDER BY index_name').all(),
  };
  process.stdout.write(`${JSON.stringify(result)}\n`);
} finally {
  database?.close();
}
