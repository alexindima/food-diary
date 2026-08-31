import assert from 'node:assert/strict';
import { directIdentifierTermMatchesMinimum, implicitImplementationIntent, rankingModuleIdentity, rankingPathIdentities } from './code-graph-path-layout.mjs';

// Synthetic names deliberately independent of evaluation queries and targets.
const moved = [
  ['Modules/Inventory/Application/Commands/Reserve/ReserveHandler.cs', 'FoodDiary.Application.Inventory/Commands/Reserve/ReserveHandler.cs'],
  ['Modules/Inventory/Application/Abstractions/Stock/IStockStore.cs', 'FoodDiary.Application.Abstractions/Stock/IStockStore.cs'],
  ['Modules/Inventory/Domain/Entities/Stock/Stock.cs', 'FoodDiary.Domain/Entities/Stock/Stock.cs'],
  ['Modules/Inventory/Infrastructure/Persistence/Stock/StockStore.cs', 'FoodDiary.Infrastructure/Persistence/Stock/StockStore.cs'],
  ['Modules/Inventory/Infrastructure/Model/Stock/StockReservation.cs', 'FoodDiary.Infrastructure/Persistence/Stock/StockReservation.cs'],
];
for (const [current, legacy] of moved) {
  assert.deepEqual(rankingPathIdentities(current), [current.toLowerCase(), legacy.toLowerCase()]);
  assert.deepEqual(rankingPathIdentities(current.replaceAll('/', '\\')), rankingPathIdentities(current));
  assert.deepEqual(rankingPathIdentities(legacy), [legacy.toLowerCase()]);
  for (const prefix of [legacy.slice(0, legacy.lastIndexOf('/') + 1), legacy.split('/')[0] + '/']) {
    assert.equal(rankingPathIdentities(current).some(path => path.startsWith(prefix.toLowerCase())), true);
  }
}
for (const path of ['Modules/Inventory/tests/Inventory.Tests/Domain/Test.cs', 'Modules/Inventory/Domain/tests/Test.cs', 'Modules/Inventory/Application/Stock.test.ts', 'ModulesExtra/Inventory/Domain/Entity.cs', 'nested/Modules/Inventory/Domain/Entity.cs', 'Modules/Inventory/Contracts/Dto.cs']) {
  assert.deepEqual(rankingPathIdentities(path), [path.toLowerCase()]);
}
const abstractions = rankingPathIdentities(moved[1][0]);
assert.equal(abstractions.some(path => path.startsWith('fooddiary.application.inventory/')), false);
assert.equal(abstractions.some(path => path.startsWith('fooddiary.application.abstractions/')), true);
assert.equal(rankingModuleIdentity(moved[0][0]), rankingModuleIdentity(moved[0][1]));
assert.equal(rankingModuleIdentity('Modules/InventoryExtras/Application/Handler.cs'), 'inventoryextras');
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
assert.equal(directIdentifierTermMatchesMinimum('r7', 3), true);
assert.equal(directIdentifierTermMatchesMinimum('r7probe', 3), true);
assert.equal(directIdentifierTermMatchesMinimum('of', 3), false);
assert.equal(directIdentifierTermMatchesMinimum('7', 3), false);
assert.equal(directIdentifierTermMatchesMinimum('r-', 3), false);
console.log('Ranking layout regression PASS: layer selectors, exclusions, exact module identity, Windows paths, legacy compatibility and negative boundaries.');
