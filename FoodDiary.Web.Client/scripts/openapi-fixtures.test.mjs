import assert from 'node:assert/strict';
import test from 'node:test';
import { invalidJsonFixture, jsonFixture, OpenApiFixtures } from '../e2e/support/openapi-fixtures.mjs';
import { hydrationEntryFixture, userFixture } from '../src/testing/api-fixtures.ts';

const contract = new OpenApiFixtures();

test('shared user and hydration builders obey the actual SDK schemas', () => {
    contract.assertSchema('UserHttpResponse', userFixture());
    contract.assertSchema('HydrationEntryHttpResponse', hydrationEntryFixture());
});

test('valid overrides stay isolated across scenarios', () => {
    const changed = userFixture({ language: 'ru', isEmailConfirmed: false });
    assert.equal(changed.language, 'ru');
    assert.equal(userFixture().language, 'en');
    contract.assertSchema('UserHttpResponse', changed);
});

test('rejects wrong UUIDs, timestamps and quantity types without coercion', () => {
    assert.throws(() => contract.assertSchema('HydrationEntryHttpResponse', hydrationEntryFixture({ id: 'water-added' })), /uuid/u);
    assert.throws(
        () => contract.assertSchema('HydrationEntryHttpResponse', hydrationEntryFixture({ timestampUtc: 'yesterday' })),
        /date-time/u,
    );
    assert.throws(() => contract.assertSchema('HydrationEntryHttpResponse', { ...hydrationEntryFixture(), amountMl: '250' }), /integer/u);
});

test('rejects undeclared endpoint, verb and status instead of returning empty success', () => {
    assert.throws(() => jsonFixture(contract, 'GET', '/api/v1/users/typo-route', {}), /Undeclared/u);
    assert.throws(() => jsonFixture(contract, 'DELETE', '/api/v1/users/info', {}), /Undeclared/u);
    assert.throws(() => jsonFixture(contract, 'GET', '/api/v1/users/info', {}, 299), /Undeclared fixture status/u);
});

test('resolves literal routes before parameter routes and ignores query values', () => {
    assert.equal(contract.operation('GET', '/api/v1/products/overview?search=x').path, '/api/v{version}/products/overview');
    jsonFixture(contract, 'GET', '/api/v1/users/info?ignored=true', userFixture());
});

test('response schemas enforce required fields where the contract declares them', () => {
    const required = new OpenApiFixtures({
        components: { schemas: { Required: { type: 'object', required: ['name'], properties: { name: { type: 'string' } } } } },
        paths: {},
    });
    assert.throws(() => required.assertSchema('Required', {}), /required/u);
});

test('malformed response scenarios require an explicit reason', () => {
    assert.throws(() => invalidJsonFixture({}, ''), /reason/u);
    assert.equal(invalidJsonFixture({}, 'Recover after missing user identity').status, 200);
});

test('nullable enum semantics stay constrained by their declared enum', () => {
    const enums = new OpenApiFixtures({ components: { schemas: { Choice: { type: 'string', nullable: true, enum: ['a'] } } }, paths: {} });
    enums.assertSchema('Choice', 'a');
    assert.throws(() => enums.assertSchema('Choice', null), /allowed values/u);
});
