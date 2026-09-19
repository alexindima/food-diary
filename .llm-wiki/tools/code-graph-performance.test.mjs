import assert from 'node:assert/strict';
import { test } from 'node:test';
import { DatabaseSync } from 'node:sqlite';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { searchContextBatch } from './code-graph-batch.mjs';
import { runGraphProcess } from './code-graph-process.mjs';
import { englishMorphologicalVariants } from './code-graph-query-terms.mjs';
import { findIdentityCandidates } from './code-graph-identity.mjs';
import { contextPathOwnership, exactFileIdentity } from './code-graph-path-layout.mjs';
import { discoverProjectOwnership, projectOwnership, inspectProjectionOwnership, repairWikiReferences } from './code-graph-maintenance.mjs';

test('maintenance detects missing, orphaned and duplicate feature rows without pretending repair succeeded', () => {
  const db = new DatabaseSync(':memory:');
  try {
    db.exec(`CREATE TABLE context_search(path); CREATE TABLE context_search_features(context_rowid, module, layer);
      INSERT INTO context_search VALUES ('Modules/Identity/Domain/Token.cs'), ('Modules/Identity/Domain/User.cs');
      INSERT INTO context_search_features VALUES (2, 'Identity', 'domain'), (2, 'Identity', 'domain'), (99, 'Identity', 'domain');`);
    const result = inspectProjectionOwnership(db, discoverProjectOwnership(['Modules/Identity/Domain/FoodDiary.Modules.Identity.Domain.csproj']), true);
    assert.equal(result.unresolvedCount, 3);
    assert.equal(result.repairedRecords, 0);
    assert.deepEqual(result.findings.map(item => item.kind).sort(), ['duplicate-projection-features', 'missing-projection-features', 'orphan-projection-features']);
  } finally { db.close(); }
});

test('Markdown repair preserves literals and formatting while repairing real destination spans', () => {
  const literal = '[Example](../../src/Old.cs)';
  const preserved = ['```md\n' + literal + '\n```', '~~~~\n' + literal + '\n~~~~',
    '> ```md\n> ' + literal + '\n> ```', '    ' + literal, '`' + literal + '`',
    '``prefix `' + literal + '``', '<!-- ' + literal + ' -->', '<pre>' + literal + '</pre>', '\\[Example](../../src/Old.cs)'];
  const original = preserved.join('\r\n\r\n') + '\r\n😀 [real](../../src/Old.cs#section "title")\r\n[ref]: <../../src/Old File.cs> "title"\r\n[use][ref]\r\n';
  const result = repairWikiReferences(original, '.llm-wiki/system/example.md',
    new Map([['src/Old.cs', 'src/New.cs'], ['src/Old File.cs', 'src/New File.cs']]), new Set(['src/New.cs', 'src/New File.cs']));
  assert.equal(result.repaired, 2);
  for (const value of preserved) assert.ok(result.text.includes(value), value);
  assert.ok(result.text.includes('😀 [real](../../src/New.cs#section "title")\r\n'));
  assert.ok(result.text.includes('[ref]: <../../src/New%20File.cs> "title"\r\n'));
  const titleLiteral = `[real](../../src/Old.cs "${literal}")\n[ref]: ../../src/Old.cs "${literal}"\n`;
  const withTitles = repairWikiReferences(titleLiteral, '.llm-wiki/system/example.md',
    new Map([['src/Old.cs', 'src/New.cs']]), new Set(['src/New.cs']));
  assert.equal(withTitles.repaired, 2);
  assert.equal(withTitles.text.split(literal).length, 3);
});

test('Wiki provenance repair only follows confirmed moves and preserves narrative', () => {
  const original = '---\r\nid: example\r\nsources:\r\n  - Modules/Old.cs\r\n  - missing.cs\r\n---\r\nOld behavior is unchanged. [Source](../../Modules/Old.cs#rule)\r\n';
  const result = repairWikiReferences(original, '.llm-wiki/system/example.md',
    new Map([['Modules/Old.cs', 'Modules/New.cs']]), new Set(['Modules/New.cs']));
  assert.equal(result.repaired, 2);
  assert.ok(result.text.includes('  - Modules/New.cs\r\n'));
  assert.ok(result.text.includes('[Source](../../Modules/New.cs#rule)'));
  assert.ok(result.text.includes('Old behavior is unchanged.'));
  assert.ok(result.text.includes('  - missing.cs'));
  assert.equal(result.findings.filter(item => !item.repairable).length, 1);
  assert.equal(repairWikiReferences(result.text, '.llm-wiki/system/example.md',
    new Map([['Modules/Old.cs', 'Modules/New.cs']]), new Set(['Modules/New.cs'])).repaired, 0);
});

test('self-maintenance discovers relocated project roots and repairs derived data idempotently', () => {
  const projects = discoverProjectOwnership([
    'Modules/Identity/Storage/FoodDiary.Modules.Identity.PersistenceModel.csproj',
    'Modules/Identity/Specs/FoodDiary.Modules.Identity.Domain.Tests.csproj',
    'Modules/NewOwner/Future/FoodDiary.Modules.NewOwner.FutureRole.csproj',
  ]);
  assert.equal(projectOwnership('Modules/Identity/Storage/TokenConfiguration.cs', projects).layer, 'persistence');
  const db = new DatabaseSync(':memory:');
  try {
    db.exec(`CREATE TABLE context_search(path); CREATE TABLE context_search_features(context_rowid, module, layer);
      INSERT INTO context_search VALUES ('Modules/Identity/Storage/TokenConfiguration.cs');
      INSERT INTO context_search_features VALUES (1, 'Modules', 'other');`);
    const before = inspectProjectionOwnership(db, projects);
    assert.equal(before.findingCount, 2);
    assert.equal(db.prepare('SELECT layer FROM context_search_features').get().layer, 'other');
    const repaired = inspectProjectionOwnership(db, projects, true);
    assert.equal(repaired.repairedRecords, 1);
    assert.notEqual(before.fingerprint, repaired.fingerprint);
    assert.equal(inspectProjectionOwnership(db, projects).fingerprint, repaired.fingerprint);
    assert.equal(repaired.unresolvedCount, 1);
    assert.equal(db.prepare('SELECT module FROM context_search_features').get().module, 'Identity');
    assert.equal(inspectProjectionOwnership(db, projects, true).repairedRecords, 0);
    assert.equal(inspectProjectionOwnership(db, projects).findings[0].kind, 'unknown-project-role');
  } finally { db.close(); }
});

test('physical module ownership covers sibling layers, tests and misleading names', () => {
  for (const [root, layer] of Object.entries({ Application: 'application', 'Application.Abstractions': 'abstractions', Domain: 'domain',
    'Domain.Contracts': 'domain', Infrastructure: 'infrastructure', PersistenceModel: 'persistence', Presentation: 'api', Contracts: 'contracts', ApplicationExtra: 'other' })) {
    assert.deepEqual(contextPathOwnership(`Modules/Identity/${root}/Token.cs`), { module: 'Identity', layer });
  }
  assert.deepEqual(contextPathOwnership('Modules/Identity/tests/FoodDiary.Modules.Identity.Domain.Tests/TokenTests.cs'), { module: 'Identity', layer: 'tests' });
  assert.deepEqual(contextPathOwnership('Modules/Ai/Application/Abstractions/IClient.cs'), { module: 'Ai', layer: 'abstractions' });
  assert.deepEqual(contextPathOwnership('Modules/Ai/Infrastructure/Model/Config.cs'), { module: 'Ai', layer: 'persistence' });
  assert.equal(contextPathOwnership('FoodDiary.Web.Client/src/app/services/domain.service.ts').layer, 'frontend');
});

test('project discovery includes services, shared libraries, hosts and unknown identities after relocation', () => {
  const projects = discoverProjectOwnership([
    'Moved/Shared/FoodDiary.Email.PersistenceModel.csproj',
    'Moved/Service/FoodDiary.MailRelay.Application.csproj',
    'Moved/Host/FoodDiary.Web.Api.csproj',
    'Moved/Unknown/Future.Component.csproj',
  ]);
  assert.equal(projects.length, 4);
  assert.equal(projectOwnership('Moved/Shared/Email.cs', projects).layer, 'persistence');
  assert.equal(projectOwnership('Moved/Service/Send.cs', projects).layer, 'application');
  assert.equal(projectOwnership('Moved/Host/Program.cs', projects).layer, 'api');
  assert.equal(projectOwnership('Moved/Unknown/Thing.cs', projects).layer, 'unknown');
});

test('exact file identity preserves paths and test suffixes without substring guesses', () => {
  const path = 'Modules/Identity/tests/RefreshTokenCommandHandlerTests.cs';
  for (const query of [path, path.replaceAll('/', '\\'), 'RefreshTokenCommandHandlerTests.cs', 'RefreshTokenCommandHandlerTests']) {
    assert.equal(exactFileIdentity(path, query), true);
    assert.equal(exactFileIdentity('Modules/Identity/Application/RefreshTokenCommandHandler.cs', query), false);
  }
  assert.equal(exactFileIdentity(path, 'where is RefreshTokenCommandHandlerTests'), false);
});

test('English plural recall preserves previous alternatives and recognizes es plurals', () => {
  for (const [plural, singular] of [['indexes', 'index'], ['boxes', 'box'], ['classes', 'class'], ['watches', 'watch'], ['brushes', 'brush'], ['buzzes', 'buzz']]) {
    assert.ok(englishMorphologicalVariants(plural).includes(singular));
    assert.ok(englishMorphologicalVariants(plural).includes(plural.slice(0, -1)));
  }
  assert.deepEqual(englishMorphologicalVariants('policies'), ['policy']);
  assert.deepEqual(englishMorphologicalVariants('users'), ['user']);
  assert.deepEqual(englishMorphologicalVariants('class'), []);
  assert.deepEqual(englishMorphologicalVariants('индексы'), []);
  assert.ok(englishMorphologicalVariants('running').includes('run'));
  assert.ok(englishMorphologicalVariants('retried').includes('retry'));
  assert.ok(englishMorphologicalVariants('verified').includes('verify'));
});

test('bounded identity recall is independent of body length and uses stable ties', () => {
  const db = new DatabaseSync(':memory:');
  try {
    db.exec(`
      CREATE TABLE context_search(record_type, record_key, path, source_path, category, title, body);
      CREATE VIRTUAL TABLE context_search_identity USING fts5(path, title);
      CREATE TABLE context_search_features(context_rowid, layer, module, role, is_test, extension);
    `);
    for (const [id, path] of [[1, 'b/index.md'], [2, 'a/index.md']]) {
      db.prepare('INSERT INTO context_search(rowid, record_type, record_key, path, source_path, category, title, body) VALUES (?, ?, ?, ?, ?, ?, ?, ?)')
        .run(id, 'wiki-page', path, path, path, 'wiki-page', 'Index', 'unrelated detail '.repeat(id * 10000));
      db.prepare('INSERT INTO context_search_identity(rowid, path, title) VALUES (?, ?, ?)').run(id, path, 'Index');
      db.prepare('INSERT INTO context_search_features VALUES (?, ?, ?, ?, ?, ?)').run(id, '', '', 'workflow', 0, '.md');
    }
    const before = findIdentityCandidates(db, 'title : "index"*', 1);
    assert.equal(before.length, 1);
    assert.equal(before[0].path, 'a/index.md');
    db.exec("UPDATE context_search SET body = 'short'");
    assert.deepEqual(findIdentityCandidates(db, 'title : "index"*', 1), before);
    assert.deepEqual(findIdentityCandidates(db, 'title : "absent"*', 1), []);
  } finally { db.close(); }
});

test('batch metadata and rows share one snapshot; next batch sees committed changes', () => {
  const root = mkdtempSync(join(tmpdir(), 'wiki-batch-'));
  const reader = new DatabaseSync(join(root, 'graph.sqlite'));
  const writer = new DatabaseSync(join(root, 'graph.sqlite'));
  try {
    reader.exec('PRAGMA journal_mode=WAL; CREATE VIRTUAL TABLE context_search USING fts5(body); INSERT INTO context_search VALUES (\'first\')');
    let countReads = 0;
    const observed = {
      exec: sql => reader.exec(sql),
      prepare: sql => { countReads++; return reader.prepare(sql); },
    };
    const first = searchContextBatch(observed, [{ query: 'a', limit: 10 }, { query: 'b' }], (db, query, limit, filters, state) => {
      if (query === 'a') writer.exec("INSERT INTO context_search VALUES ('second')");
      return { count: state.indexedDocuments, rows: reader.prepare('SELECT body FROM context_search').all().length, limit };
    });
    assert.deepEqual(first.results, [{ count: 1, rows: 1, limit: 10 }, { count: 1, rows: 1, limit: 20 }]);
    assert.equal(countReads, 1);
    assert.equal(searchContextBatch(reader, [{}], (db, q, l, f, state) => state.indexedDocuments).results[0], 2);
    const failure = new Error('original search error');
    assert.throws(() => searchContextBatch(reader, [{}], () => { throw failure; }), error => error === failure);
    assert.equal(searchContextBatch(reader, [{}], (db, q, l, f, state) => state.indexedDocuments).results[0], 2);
  } finally {
    writer.close(); reader.close(); rmSync(root, { recursive: true, force: true });
  }
});

test('child failures preserve stdout start, stderr and exit status', () => {
  assert.throws(() => runGraphProcess(process.execPath, ['-e',
    "process.stdout.write('COMPILER_ERROR_START\\n' + 'x'.repeat(20000)); process.stderr.write('STDERR_END'); process.exitCode=7;",
  ]), error => error.message.includes('COMPILER_ERROR_START') && error.message.includes('STDERR_END') && error.message.includes('exit=7'));
  assert.equal(runGraphProcess(process.execPath, ['-e', "process.stdout.write('ok')"]), 'ok');
  assert.throws(() => runGraphProcess(join(tmpdir(), 'nonexistent-wiki-command'), []), /ENOENT/);
});

test('snapshot cleanup cannot hide the original search failure', () => {
  const original = new Error('search failed');
  const cleanup = new Error('rollback failed');
  const database = {
    exec: sql => { if (sql === 'ROLLBACK') throw cleanup; },
    prepare: () => ({ get: () => ({ count: 0 }) }),
  };
  assert.throws(() => searchContextBatch(database, [{}], () => { throw original; }),
    error => error === original && error.rollbackError === cleanup);
});
