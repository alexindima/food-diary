import assert from 'node:assert/strict';
import * as layout from './code-graph-path-layout.mjs';
import { directIdentifierTermMatchesMinimum, implicitImplementationIntent, rankingModuleIdentity, rankingPathIdentities } from './code-graph-path-layout.mjs';

// Synthetic names deliberately independent of evaluation queries and targets.
const moved = [
  ['Modules/Inventory/Application/Commands/Reserve/ReserveHandler.cs', 'FoodDiary.Application.Inventory/Commands/Reserve/ReserveHandler.cs'],
  ['Modules/Inventory/Application/Abstractions/Stock/IStockStore.cs', 'FoodDiary.Application.Abstractions/Stock/IStockStore.cs'],
  ['Modules/Inventory/Domain/Entities/Stock/Stock.cs', 'FoodDiary.Domain/Entities/Stock/Stock.cs'],
  ['Modules/Inventory/Infrastructure/Persistence/Stock/StockStore.cs', 'FoodDiary.Infrastructure/Persistence/Stock/StockStore.cs'],
  ['Modules/Inventory/Infrastructure/Model/Stock/StockReservation.cs', 'FoodDiary.Infrastructure/Persistence/Stock/StockReservation.cs'],
  ['Modules/Inventory/Infrastructure/Providers/Services/SupplierClient.cs', 'FoodDiary.Integrations/Services/SupplierClient.cs'],
  ['Modules/Shipping/Infrastructure/Providers/Options/CarrierOptions.cs', 'FoodDiary.Integrations/Options/CarrierOptions.cs'],
  ['Shared/FoodDiary.Integrations.Http/Http/SampleTransport.cs', 'FoodDiary.Integrations/Http/SampleTransport.cs'],
  ['Shared/FoodDiary.Inventory.PersistenceModel/StockRecord.cs', 'FoodDiary.Infrastructure/Persistence/Inventory/StockRecord.cs'],
  ['Shared/FoodDiary.Inventory.PersistenceModel/Configurations/StockConfiguration.cs', 'FoodDiary.Infrastructure/Persistence/Configurations/Inventory/StockConfiguration.cs'],
  ['Modules/Inventory/Presentation/Features/Stock/Mappings/StockResponseMappings.cs', 'FoodDiary.Presentation.Api/Features/Stock/Mappings/StockResponseMappings.cs'],
  ['Modules/Inventory/Contracts/Stock/IStockReader.cs', 'FoodDiary.Application.Abstractions/Stock/IStockReader.cs'],
  ['FoodDiary.ReadModel.Composition/Inventory/StockReader.cs', 'FoodDiary.Infrastructure/Persistence/Inventory/StockReader.cs'],
];
for (const [current, legacy] of moved) {
  assert.deepEqual(rankingPathIdentities(current), [current.toLowerCase(), legacy.toLowerCase()]);
  assert.deepEqual(rankingPathIdentities(current.replaceAll('/', '\\')), rankingPathIdentities(current));
  assert.deepEqual(rankingPathIdentities(legacy), [legacy.toLowerCase()]);
  for (const prefix of [legacy.slice(0, legacy.lastIndexOf('/') + 1), legacy.split('/')[0] + '/']) {
    assert.equal(rankingPathIdentities(current).some(path => path.startsWith(prefix.toLowerCase())), true);
  }
}
for (const path of ['Modules/Inventory/tests/Inventory.Tests/Domain/Test.cs', 'Modules/Inventory/Domain/tests/Test.cs', 'Modules/Inventory/Application/Stock.test.ts', 'ModulesExtra/Inventory/Domain/Entity.cs', 'nested/Modules/Inventory/Domain/Entity.cs', 'Modules/Inventory/Domain.Contracts/Dto.cs', 'Modules/Inventory/ContractsExtra/Dto.cs', 'Modules/Inventory/Contracts/tests/Test.cs', 'Modules/Inventory/PresentationExtra/Dto.cs', 'Modules/Inventory/Presentation/tests/Test.cs']) {
  assert.deepEqual(rankingPathIdentities(path), [path.toLowerCase()]);
}
const abstractions = rankingPathIdentities(moved[1][0]);
for (const path of ['FoodDiary.ReadModel.CompositionExtra/Inventory/StockReader.cs', 'FoodDiary.ReadModel.Composition/tests/StockReader.cs', 'FoodDiary.ReadModel.Composition/Inventory/tests/StockReader.cs', 'FoodDiary.ReadModel.Composition/DependencyInjection.cs']) {
  assert.deepEqual(rankingPathIdentities(path), [path.toLowerCase()]);
}
for (const path of ['Modules/Inventory/Infrastructure/ProvidersExtra/Client.cs', 'Modules/Inventory/Infrastructure/Persistence/Providers/Client.cs', 'Modules/Inventory/Contracts/Providers/Dto.cs', 'Modules/Inventory/Infrastructure/Providers/tests/ClientTests.cs', 'Modules/Inventory/Infrastructure/Providers/Client.test.ts']) {
  assert.equal(rankingPathIdentities(path).some(alias => alias.startsWith('fooddiary.integrations/')), false);
}
assert.equal(abstractions.some(path => path.startsWith('fooddiary.application.inventory/')), false);
assert.equal(abstractions.some(path => path.startsWith('fooddiary.application.abstractions/')), true);
assert.equal(rankingModuleIdentity(moved[0][0]), rankingModuleIdentity(moved[0][1]));
assert.equal(rankingModuleIdentity('Modules/InventoryExtras/Application/Handler.cs'), 'inventoryextras');
assert.equal(rankingModuleIdentity('FoodDiary.ReadModel.Composition/Inventory/StockReader.cs'), 'inventory');
assert.equal(rankingModuleIdentity('FoodDiary.ReadModel.CompositionExtra/Inventory/StockReader.cs'), 'readmodelcompositionextra');
const intentPolicy = { domainIntentTerms: ['entity'], apiIntentTerms: ['endpoint'],
  databaseIntentTerms: ['storage'], integrationIntentTerms: ['adapter'], infrastructureIntentTerms: ['implementation', 'persist'],
  infrastructureMinimumMatches: 2, infrastructureExcludedIntentTerms: ['translation'] };
for (const term of ['entity', 'endpoint', 'storage', 'adapter']) {
  assert.equal(implicitImplementationIntent('Any', ['inventory', term], intentPolicy), true);
}
assert.equal(implicitImplementationIntent('Any', ['inventory'], intentPolicy), false);
assert.equal(implicitImplementationIntent('Frontend', ['entity'], intentPolicy), false);
assert.equal(implicitImplementationIntent('Any', ['entity'], {}), false);
assert.equal(implicitImplementationIntent('Any', ['implementation'], intentPolicy), false);
assert.equal(implicitImplementationIntent('Any', ['implementation', 'persist'], intentPolicy), true);
assert.equal(implicitImplementationIntent('Any', ['implementation', 'persist', 'translation'], intentPolicy), false);
for (const layer of ['Application', 'Domain', 'Infrastructure', 'Infrastructure.Integration']) {
  const path = `Modules/Inventory/tests/FoodDiary.Modules.Inventory.${layer}.Tests/Stock/StockTests.cs`;
  const aliases = rankingPathIdentities(path);
  assert.deepEqual(aliases, [path.toLowerCase(), `tests/fooddiary.${layer.toLowerCase()}.tests/stock/stocktests.cs`]);
  assert.equal(aliases.some(alias => alias.startsWith('fooddiary.application.inventory/')), false);
}
const foreignTest = 'Modules/Inventory/tests/FoodDiary.Modules.Other.Application.Tests/Test.cs';
assert.deepEqual(rankingPathIdentities(foreignTest), [foreignTest.toLowerCase()]);
for (const module of ['Inventory', 'Shipping']) {
  const path = `Modules/${module}/tests/FoodDiary.Modules.${module}.Infrastructure.IntegrationTests/Integration/StockStoreTests.cs`;
  const legacy = 'tests/fooddiary.infrastructure.integrationtests/integration/stockstoretests.cs';
  assert.deepEqual(rankingPathIdentities(path), [path.toLowerCase(), legacy]);
  assert.deepEqual(rankingPathIdentities(path.replaceAll('/', '\\')), [path.toLowerCase(), legacy]);
}
for (const path of [
  'Modules/Inventory/tests/FoodDiary.Modules.Shipping.Infrastructure.IntegrationTests/StockTests.cs',
  'Modules/Inventory/tests/FoodDiary.Modules.Inventory.Infrastructure.IntegrationTestsExtra/StockTests.cs',
  'Modules/Inventory/tests/FoodDiary.Modules.Inventory.Infrastructure.IntegrationTests/',
]) {
  assert.deepEqual(rankingPathIdentities(path), [path.toLowerCase()]);
}
assert.equal(directIdentifierTermMatchesMinimum('r7', 3), true);
assert.equal(directIdentifierTermMatchesMinimum('r7probe', 3), true);
assert.equal(directIdentifierTermMatchesMinimum('of', 3), false);
assert.equal(directIdentifierTermMatchesMinimum('7', 3), false);
assert.equal(directIdentifierTermMatchesMinimum('r-', 3), false);
for (const path of ['Shared/FoodDiary.Domain.Primitives/Value.cs', 'Shared\\FoodDiary.Domain.Primitives\\Value.cs']) {
  assert.deepEqual(rankingPathIdentities(path), [path.replaceAll('\\', '/').toLowerCase(), 'fooddiary.domain/value.cs']);
}
for (const root of ['Shared', 'Tooling']) {
  const path = `${root}/tests/FoodDiary.Sample.Tests/ValueTests.cs`;
  assert.deepEqual(rankingPathIdentities(path), [path.toLowerCase(), 'tests/fooddiary.sample.tests/valuetests.cs']);
}
for (const path of ['Shared/FoodDiary.Domain.PrimitivesExtra/Value.cs', 'Shared/FoodDiary.Domain.Primitives/tests/ValueTests.cs', 'Shared/FoodDiary.Domain.Primitives/Value.test.js', 'Shared/FoodDiary.Integrations.HttpExtra/Http/SampleTransport.cs', 'Shared/FoodDiary.Integrations.Http/tests/SampleTransport.cs', 'Shared/FoodDiary.Integrations.Http/Sample.test.js']) {
  assert.deepEqual(rankingPathIdentities(path), [path.toLowerCase()]);
}
const entryPolicy = { ...intentPolicy, moduleIdentityMinimumLength: 8, moduleIdentityLeadingTermCount: 1, genericAffinities: intentPolicy };
assert.deepEqual(layout.hyphenatedIdentifierTerms('Stock-count stock-count remote-cache-entry'), ['stockcount', 'remotecacheentry']);
assert.deepEqual(layout.hyphenatedIdentifierTerms('a-b stock count stock--count 3-4'), []);
assert.deepEqual(layout.hyphenatedIdentifierTerms('склад-остаток'), ['складостаток']);
for (const path of ['Shared/FoodDiary.Inventory.PersistenceModelExtra/Stock.cs', 'Shared/FoodDiary.Inventory.PersistenceModel/tests/StockTests.cs', 'Shared/FoodDiary.Inventory.PersistenceModel/Stock.test.ts', 'Shared/FoodDiary..PersistenceModel/Stock.cs']) {
  assert.deepEqual(rankingPathIdentities(path), [path.toLowerCase()]);
}
const entry = 'Modules/Inventory/Application/Abstractions/IStockService.cs';
assert.equal(layout.isModuleEntryPointQuery(entry, 'Backend', ['inventory', 'lookup'], ['inventory', 'lookup'], entryPolicy), true);
for (const term of ['adapter', 'endpoint', 'entity', 'storage', 'implementation', 'options']) {
  assert.equal(layout.isModuleEntryPointQuery(entry, 'Backend', ['inventory', term], ['inventory', term], entryPolicy), false);
}
for (const [path, changeType, direct] of [[entry, 'Tests', ['inventory']], [entry, 'Backend', ['shipping']], ['Modules/Inventory/Infrastructure/Stock.cs', 'Backend', ['inventory']], ['Modules/Inventory/Application/tests/IStockService.cs', 'Backend', ['inventory']]]) {
  assert.equal(layout.isModuleEntryPointQuery(path, changeType, direct, direct, entryPolicy), false);
}
const testRows = Array.from({ length: 16 }, (_, i) => ({ path: `tests/Suite${i}/ProviderTests.cs`, identity: i === 0 ? 'acme client tests' : 'provider client tests' }));
const weights = layout.testIdentityWeights(['acme', 'client', 'ab'], testRows, { minimumTermLength: 3, scorePerMatch: 35, maximumScore: 175 });
assert.equal(weights.get('acme'), 105);
assert.equal(weights.get('client'), 0);
assert.equal(weights.has('ab'), false);
assert.deepEqual(layout.testIdentityWeights(['acme', 'client', 'ab'], [...testRows, ...testRows].reverse(), { minimumTermLength: 3, scorePerMatch: 35, maximumScore: 175 }), weights);
const scaffoldWeights = layout.testIdentityWeights(['acme', 'feature', 'tests', 'spec'], testRows,
  { minimumTermLength: 3, scorePerMatch: 35, maximumScore: 175 });
assert.deepEqual([...scaffoldWeights.keys()], ['acme']);
console.log('Ranking layout regression PASS: layer selectors, exclusions, exact module identity, Windows paths, legacy compatibility and negative boundaries.');
