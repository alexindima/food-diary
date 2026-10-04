import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';

const schemaVersion = '1';
const sources = new Map([
  ['catalog', 'repository-catalog.json'], ['symbols', 'csharp-symbol-index.json'],
  ['frontend', 'frontend-index.json'], ['frontend-contract', 'frontend-contract-index.json'],
  ['backend-contract', 'backend-contract-index.json'], ['architecture-health', 'architecture-health-index.json'],
  ['domain-data', 'domain-data-index.json'], ['configuration', 'configuration-index.json'],
  ['quality', 'quality-index.json'], ['runtime', 'runtime-topology.json'],
  ['sensitive-data', 'sensitive-data-index.json'],
]);
const hash = text => createHash('sha256').update(text.replaceAll('\r\n', '\n')).digest('hex');

function readSource(root, index) {
  if (index === 'modules') {
    const sourcePath = '.llm-wiki/generated/modules';
    const directory = resolve(root, sourcePath);
    if (!existsSync(directory)) return null;
    const items = readdirSync(directory, { withFileTypes: true })
      .filter(item => item.isFile()).map(item => `${sourcePath}/${item.name}`).sort();
    const text = JSON.stringify({ items });
    return { sourcePath, text, contentHash: hash(text) };
  }
  const file = sources.get(index);
  if (!file) throw new Error(`Unsupported compiled index: ${index}`);
  const sourcePath = `.llm-wiki/generated/${file}`;
  if (!existsSync(resolve(root, sourcePath))) return null;
  const text = readFileSync(resolve(root, sourcePath), 'utf8');
  return { sourcePath, text, contentHash: hash(text) };
}

function collectSections(document, prefix = '', depth = 0, sections = []) {
  if (!document || typeof document !== 'object' || depth > 2) return sections;
  for (const [key, value] of Object.entries(document)) {
    const name = prefix ? `${prefix}.${key}` : key;
    if (Array.isArray(value)) sections.push({ name, values: value });
    else if (value && typeof value === 'object') collectSections(value, name, depth + 1, sections);
  }
  return sections;
}

export function refreshStandaloneIndexes(database, root) {
  database.exec(`
    CREATE TABLE IF NOT EXISTS standalone_indexes(
      index_name TEXT PRIMARY KEY, source_path TEXT NOT NULL,
      content_hash TEXT NOT NULL, header_json TEXT NOT NULL
    ) WITHOUT ROWID;
    CREATE TABLE IF NOT EXISTS standalone_index_sections(
      index_name TEXT NOT NULL, section_name TEXT NOT NULL, ordinal INTEGER NOT NULL,
      query_only INTEGER NOT NULL DEFAULT 0,
      PRIMARY KEY(index_name, section_name)
    ) WITHOUT ROWID;
    CREATE TABLE IF NOT EXISTS standalone_index_records(
      index_name TEXT NOT NULL, section_name TEXT NOT NULL, ordinal INTEGER NOT NULL,
      search_text TEXT NOT NULL, payload_json TEXT NOT NULL,
      PRIMARY KEY(index_name, section_name, ordinal)
    ) WITHOUT ROWID;
  `);
  const storedVersion = database.prepare("SELECT value FROM metadata WHERE key='standalone_index_schema_version'").get()?.value;
  const insertIndex = database.prepare('INSERT OR REPLACE INTO standalone_indexes VALUES (?, ?, ?, ?)');
  const insertSection = database.prepare('INSERT INTO standalone_index_sections VALUES (?, ?, ?, ?)');
  const insertRecord = database.prepare('INSERT INTO standalone_index_records VALUES (?, ?, ?, ?, ?)');
  let refreshed = 0;
  for (const index of [...sources.keys(), 'modules']) {
    const source = readSource(root, index);
    if (!source) {
      for (const table of ['standalone_index_records', 'standalone_index_sections', 'standalone_indexes']) {
        database.prepare(`DELETE FROM ${table} WHERE index_name=?`).run(index);
      }
      continue;
    }
    const previous = database.prepare('SELECT content_hash FROM standalone_indexes WHERE index_name=?').get(index);
    if (storedVersion === schemaVersion && previous?.content_hash === source.contentHash) continue;
    const document = JSON.parse(source.text);
    database.prepare('DELETE FROM standalone_index_records WHERE index_name=?').run(index);
    database.prepare('DELETE FROM standalone_index_sections WHERE index_name=?').run(index);
    insertIndex.run(index, source.sourcePath, source.contentHash, JSON.stringify({
      generatedAtUtc: document.generatedAtUtc ?? null, summary: document.summary ?? null,
    }));
    const sections = collectSections(document).map(section => ({ ...section, queryOnly: false }));
    if (index === 'configuration') {
      sections.push({ name: 'configurationKeys', queryOnly: true, values: (document.configurationFiles ?? []).flatMap(file => (file.keys ?? []).map(key => ({ path: file.path, key }))) });
      sections.push({ name: 'environmentVariables', queryOnly: true, values: (document.environmentFiles ?? []).flatMap(file => (file.variables ?? []).map(variable => ({ path: file.path, variable }))) });
    }
    for (const [ordinal, section] of sections.entries()) {
      insertSection.run(index, section.name, ordinal, section.queryOnly ? 1 : 0);
      for (const [recordOrdinal, value] of section.values.entries()) {
        const payload = JSON.stringify(value);
        const searchText = index === 'modules' ? String(value).split('/').at(-1).toLowerCase() : payload.toLowerCase();
        insertRecord.run(index, section.name, recordOrdinal, searchText, payload);
      }
    }
    refreshed++;
  }
  database.prepare('INSERT OR REPLACE INTO metadata(key,value) VALUES (?,?)').run('standalone_index_schema_version', schemaVersion);
  return refreshed;
}

export function standaloneIndexStatus(database, root, index) {
  const source = readSource(root, index);
  if (!source) return { ready: false, unavailableReason: 'compiled-source-missing', index };
  const hasTable = database.prepare("SELECT 1 FROM sqlite_master WHERE type='table' AND name='standalone_indexes'").get();
  if (!hasTable) return { ready: false, unavailableReason: 'standalone-index-projection-missing', index };
  const storedVersion = database.prepare("SELECT value FROM metadata WHERE key='standalone_index_schema_version'").get()?.value;
  if (storedVersion !== schemaVersion) return { ready: false, unavailableReason: 'standalone-index-schema-stale', index };
  const row = database.prepare('SELECT source_path,content_hash,header_json FROM standalone_indexes WHERE index_name=?').get(index);
  if (!row || row.content_hash !== source.contentHash) return { ready: false, unavailableReason: 'standalone-index-projection-stale', index };
  return { ready: true, index, sourcePath: row.source_path, sourceHash: row.content_hash, header: JSON.parse(row.header_json) };
}

export function queryStandaloneIndex(database, root, { index, query = '', limit = 12 }) {
  if (!Number.isInteger(limit) || limit < 1 || limit > 50) throw new Error('Compiled-index limit must be between 1 and 50.');
  const status = standaloneIndexStatus(database, root, index);
  if (!status.ready) return status;
  const terms = [...new Set((String(query).toLowerCase().match(/[\p{L}\p{N}_-]+/gu) ?? []).filter(term => term.length > 1))];
  const predicates = terms.map(() => "instr(search_text, ?) > 0");
  const where = ['index_name=?', 'section_name=?', ...predicates].join(' AND ');
  const countRecords = database.prepare(`SELECT COUNT(*) count FROM standalone_index_records WHERE ${where}`);
  const selectRecords = database.prepare(`SELECT payload_json FROM standalone_index_records WHERE ${where} ORDER BY ordinal LIMIT ?`);
  let sections = database.prepare('SELECT section_name,query_only FROM standalone_index_sections WHERE index_name=? ORDER BY ordinal').all(index);
  if (index === 'configuration' && String(query).trim()) sections = ['optionTypes','configurationKeys','environmentVariables'].map(section_name => ({ section_name }));
  else sections = sections.filter(section => !section.query_only);
  const result = {
    schemaVersion: 1, index, sourcePath: status.sourcePath, readOnly: true,
    query, freshness: 'not-verified; use -Check or wiki.ps1 verify',
    generatedAtUtc: status.header.generatedAtUtc, summary: status.header.summary,
    sections: sections.map(section => ({
      name: section.section_name,
      count: Number(countRecords.get(index, section.section_name, ...terms).count),
      items: selectRecords.all(index, section.section_name, ...terms, limit).map(row => JSON.parse(row.payload_json)),
    })),
    compiledIndex: { source: 'sqlite-standalone-index', fresh: true, sourceHash: status.sourceHash },
  };
  if (index === 'modules') {
    result.count = result.sections[0]?.count ?? 0;
    result.items = result.sections[0]?.items ?? [];
    delete result.sections;
    delete result.generatedAtUtc;
    delete result.summary;
  }
  return { ready: true, index: result };
}

export function querySecurityEvidence(database, root, limit = 12) {
  if (!Number.isInteger(limit) || limit < 1 || limit > 50) throw new Error('Security evidence limit must be between 1 and 50.');
  const statuses = ['quality', 'runtime', 'sensitive-data'].map(index => standaloneIndexStatus(database, root, index));
  const unavailable = statuses.find(status => !status.ready);
  if (unavailable) return unavailable;
  const securityPattern = /authenticat|authoriz|access.?token|refresh.?token|tokenhash|secret|apikey|signingkey|password|webhook|securityheader|idempot|deduplic|replay|signature|hmac|csp|cors|ssrf|webpush|upload|dmarc|smtp|ratelimit|rate.?limit|permission|access.?guard/i;
  database.function('security_symbol_match', { deterministic: true }, (name, path, role) =>
    !String(path).startsWith('.llm-wiki/') && securityPattern.test(`${name} ${path} ${role}`) ? 1 : 0);
  database.function('security_control_family', { deterministic: true }, (name, path) => {
    const text = `${name} ${path}`.toLowerCase();
    const families = [
      [/webhook|mailgun|idempot|deduplic|replay|signature|hmac/, 'webhook-authenticity-replay'],
      [/webpush|ssrf/, 'outbound-endpoint-validation'], [/csp|cors|xss|securityheader/, 'browser-boundary'],
      [/upload|image/, 'untrusted-content'], [/smtp|dmarc/, 'mail-ingress'], [/ratelimit|rate.?limit/, 'resource-abuse-control'],
      [/authoriz|permission|access.?guard/, 'authorization'], [/authenticat|token|secret|apikey|signingkey|password/, 'authentication-token'],
    ];
    return families.find(([pattern]) => pattern.test(text))?.[1] ?? 'security-control';
  });
  const parseRows = rows => rows.map(row => JSON.parse(row.payload_json));
  const securityTestSignals = parseRows(database.prepare(`
    SELECT payload_json FROM standalone_index_records
    WHERE index_name='quality' AND section_name='criticalSymbols'
      AND security_symbol_match(json_extract(payload_json,'$.name'),json_extract(payload_json,'$.path'),json_extract(payload_json,'$.role'))=1
    ORDER BY CAST(json_extract(payload_json,'$.testReferenceCount') AS INTEGER),
      security_control_family(json_extract(payload_json,'$.name'),json_extract(payload_json,'$.path')),
      json_extract(payload_json,'$.path'), ordinal LIMIT ?
  `).all(limit));
  const section = (index, name, predicate = '1') => parseRows(database.prepare(`
    SELECT payload_json FROM standalone_index_records WHERE index_name=? AND section_name=? AND (${predicate}) ORDER BY ordinal LIMIT ?
  `).all(index, name, limit));
  return { ready: true, securityTestSignals,
    runtime: { webhooks: section('runtime','webhooks'), networkPolicies: section('runtime','networkPolicies'),
      composeServices: section('runtime','composeServices', "coalesce(json_array_length(payload_json,'$.ports'),0)>0 OR coalesce(json_array_length(payload_json,'$.environmentKeys'),0)>0 OR coalesce(json_array_length(payload_json,'$.networks'),0)>0") },
    sensitive: { summary: statuses[2].header.summary, externalTransfers: section('sensitive-data','externalTransfers'), potentialLogging: section('sensitive-data','potentialLogging') },
  };
}
