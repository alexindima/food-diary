import { test } from 'node:test';
import assert from 'node:assert/strict';
import { DatabaseSync } from 'node:sqlite';
import { mkdirSync, mkdtempSync, writeFileSync, rmSync } from 'node:fs';
import { resolve, sep } from 'node:path';
import { refreshStandaloneIndexes, queryStandaloneIndex, querySecurityEvidence } from './code-graph-index-query.mjs';

test('standalone SQLite indexes preserve counts, order, nested sections and literal Unicode queries', () => {
  const parent = resolve(import.meta.dirname, '../../.artifacts/llm-wiki/index-query-fixtures');
  mkdirSync(parent, { recursive: true });
  const root = mkdtempSync(resolve(parent, 'contract-'));
  const generated = resolve(root, '.llm-wiki/generated');
  mkdirSync(resolve(generated, 'modules'), { recursive: true });
  const database = new DatabaseSync(':memory:');
  database.exec('CREATE TABLE metadata(key TEXT PRIMARY KEY,value TEXT NOT NULL);');
  const write = (name, document) => writeFileSync(resolve(generated, name), JSON.stringify(document));
  try {
    write('repository-catalog.json', { summary: { projects: 3 }, dotnet: { projects: [{ name: 'Beta' }, { name: 'Alpha' }, { name: 'Gamma' }] }, empty: [] });
    write('csharp-symbol-index.json', { symbols: [{ name: 'First', path: 'Папка с пробелом/Тип.cs' }, { name: 'literal_value' }, { name: 'Last' }] });
    write('configuration-index.json', { optionTypes: [{ name: 'MailOptions' }], configurationFiles: [{ path: 'appsettings.json', keys: ['Mail:Host','Storage:Path'] }], environmentFiles: [{ path: '.env.example', variables: ['MAIL_HOST'] }] });
    for (const file of ['frontend-index.json','frontend-contract-index.json','backend-contract-index.json','architecture-health-index.json','domain-data-index.json','quality-index.json','runtime-topology.json','sensitive-data-index.json']) {
      write(file, { entries: [{ name: 'First' }, { name: 'Second' }] });
    }
    const entries = [{ name: 'First' }, { name: 'Second' }];
    write('quality-index.json', { entries, criticalSymbols: [
      { name: 'RefreshToken', path: 'Modules/Identity/Token.cs', role: 'handler', testReferenceCount: 3 },
      { name: 'AuthorizeAccess', path: 'Modules/Identity/Access.cs', role: 'handler', testReferenceCount: 0 },
      { name: 'TokenHelper', path: '.llm-wiki/tools/token.ps1', testReferenceCount: 0 },
      { name: 'Ordinary', path: 'Modules/Other.cs', role: 'other', testReferenceCount: 0 },
    ] });
    write('runtime-topology.json', { entries, webhooks: [{ name: 'Hook' }], networkPolicies: [], composeServices: [
      { name: 'internal', ports: [], environmentKeys: [], networks: [] },
      { name: 'exposed', ports: ['80'], environmentKeys: [], networks: [] },
    ] });
    write('sensitive-data-index.json', { entries, summary: { health: 2 }, externalTransfers: [{ path: 'Provider.cs' }], potentialLogging: [] });
    writeFileSync(resolve(generated, 'modules/zeta.md'), 'zeta');
    writeFileSync(resolve(generated, 'modules/user.md'), 'user');
    assert.equal(queryStandaloneIndex(database, root, { index: 'symbols' }).unavailableReason, 'standalone-index-projection-missing');
    assert.equal(refreshStandaloneIndexes(database, root), 12);
    assert.equal(refreshStandaloneIndexes(database, root), 0);
    database.prepare("UPDATE metadata SET value='old' WHERE key='standalone_index_schema_version'").run();
    assert.equal(queryStandaloneIndex(database, root, { index: 'symbols' }).unavailableReason, 'standalone-index-schema-stale');
    assert.equal(refreshStandaloneIndexes(database, root), 12);
    const catalog = queryStandaloneIndex(database, root, { index: 'catalog', limit: 2 }).index;
    assert.deepEqual(catalog.summary, { projects: 3 });
    assert.deepEqual(catalog.sections, [
      { name: 'dotnet.projects', count: 3, items: [{ name: 'Beta' }, { name: 'Alpha' }] },
      { name: 'empty', count: 0, items: [] },
    ]);
    assert.equal(catalog.compiledIndex.source, 'sqlite-standalone-index');
    const unicode = queryStandaloneIndex(database, root, { index: 'symbols', query: 'Папка Тип', limit: 2 }).index.sections[0];
    assert.equal(unicode.count, 1);
    assert.equal(unicode.items[0].path, 'Папка с пробелом/Тип.cs');
    assert.deepEqual(queryStandaloneIndex(database, root, { index: 'symbols', query: 'literal_value' }).index.sections[0].items, [{ name: 'literal_value' }]);
    assert.equal(queryStandaloneIndex(database, root, { index: 'symbols', query: "missing ' OR 1=1 --" }).index.sections[0].count, 0);
    const configuration = queryStandaloneIndex(database, root, { index: 'configuration', query: 'Mail' }).index;
    assert.deepEqual(configuration.sections.map(section => [section.name, section.count]), [['optionTypes',1],['configurationKeys',1],['environmentVariables',1]]);
    assert.deepEqual(configuration.sections[1].items, [{ path: 'appsettings.json', key: 'Mail:Host' }]);
    const modules = queryStandaloneIndex(database, root, { index: 'modules', query: 'user', limit: 1 }).index;
    assert.equal(modules.count, 1);
    assert.deepEqual(modules.items, ['.llm-wiki/generated/modules/user.md']);
    for (const index of ['frontend','frontend-contract','backend-contract','architecture-health','domain-data','quality','runtime','sensitive-data']) {
      const result = queryStandaloneIndex(database, root, { index, limit: 1 });
      assert.equal(result.ready, true);
      assert.deepEqual(result.index.sections.find(section => section.name === 'entries'), { name: 'entries', count: 2, items: [{ name: 'First' }] });
    }
    const security = querySecurityEvidence(database, root, 1);
    assert.equal(security.ready, true);
    assert.deepEqual(security.securityTestSignals.map(item => item.name), ['AuthorizeAccess']);
    assert.deepEqual(security.runtime.composeServices.map(item => item.name), ['exposed']);
    assert.deepEqual(security.sensitive.summary, { health: 2 });
    assert.throws(() => querySecurityEvidence(database, root, 0), /between 1 and 50/);
    write('csharp-symbol-index.json', { symbols: [{ name: 'Updated' }] });
    assert.equal(queryStandaloneIndex(database, root, { index: 'symbols' }).unavailableReason, 'standalone-index-projection-stale');
    assert.equal(refreshStandaloneIndexes(database, root), 1);
    assert.deepEqual(queryStandaloneIndex(database, root, { index: 'symbols' }).index.sections[0].items, [{ name: 'Updated' }]);
    rmSync(resolve(generated, 'csharp-symbol-index.json'));
    assert.equal(queryStandaloneIndex(database, root, { index: 'symbols' }).unavailableReason, 'compiled-source-missing');
    refreshStandaloneIndexes(database, root);
    assert.equal(database.prepare("SELECT COUNT(*) count FROM standalone_index_records WHERE index_name='symbols'").get().count, 0);
    assert.throws(() => queryStandaloneIndex(database, root, { index: 'catalog', limit: 0 }), /between 1 and 50/);
  } finally {
    database.close();
    assert.ok(resolve(root).startsWith(parent + sep));
    rmSync(root, { recursive: true, force: true });
  }
});
