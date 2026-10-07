import assert from 'node:assert/strict';
import { test } from 'node:test';
import { botApiContract } from './bot-api-contract.mjs';

const path = '/api/v{version}/bot';
const scopes = [{ name: 'Bot', path, method: 'POST', operationId: 'Run' }];
function sourceDocument() {
    return {
        openapi: '3.0.4',
        paths: {
            [path]: {
                post: {
                    security: [{ Bearer: [] }],
                    requestBody: { content: { 'application/json': { schema: { $ref: '#/components/schemas/Input' } } } },
                    responses: {
                        200: {
                            description: 'OK',
                            content: { 'application/json': { schema: { $ref: '#/components/schemas/FoodNutritionHttpResponse' } } },
                        },
                    },
                },
                get: { responses: {} },
            },
            '/api/v{version}/admin': { get: { responses: {} } },
        },
        components: {
            schemas: {
                Input: {
                    type: 'object',
                    properties: { id: { type: 'string', format: 'uuid' }, timestamp: { type: 'string', format: 'date-time' } },
                },
                FoodNutritionHttpResponse: {
                    type: 'object',
                    properties: Object.fromEntries(
                        ['calories', 'protein', 'fat', 'carbs'].map(key => [key, { type: 'number', format: 'double', nullable: true }]),
                    ),
                },
                Unrelated: { type: 'object' },
            },
            securitySchemes: { Bearer: { type: 'http', scheme: 'bearer' } },
        },
    };
}
test('selects only declared bot methods and retains wire metadata without mutating the host schema', () => {
    const source = sourceDocument();
    const before = structuredClone(source);
    const contract = botApiContract(source, scopes);
    assert.deepEqual(source, before);
    assert.deepEqual(Object.keys(contract.paths), [path]);
    assert.deepEqual(Object.keys(contract.paths[path]), ['post']);
    assert.deepEqual(contract.paths[path].post.security, [{ Bearer: [] }]);
    assert.equal(contract.components.schemas.Unrelated, undefined);
    assert.equal(contract.components.schemas.FoodNutritionHttpResponse.properties.calories.format, 'double');
    assert.equal(contract.components.schemas.FoodNutritionHttpResponse.properties.calories['x-bot-decimal'], true);
    assert.equal(contract.components.schemas.FoodNutritionHttpResponse.properties.calories.nullable, true);
});
test('fails when a frozen bot operation disappears', () => {
    const source = sourceDocument();
    delete source.paths[path].post;
    assert.throws(() => botApiContract(source, scopes), /operation disappeared/u);
});
test('fails when nutrition changes from a number representation', () => {
    const source = sourceDocument();
    source.components.schemas.FoodNutritionHttpResponse.properties.protein.type = 'string';
    assert.throws(() => botApiContract(source, scopes), /nutrition number contract changed/u);
});
