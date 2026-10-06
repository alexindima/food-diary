import assert from 'node:assert/strict';
import { test } from 'node:test';
import { canonicalJson, productsContract } from './api-sdk-contract.mjs';

function sourceDocument() {
    const root = '/api/v{version}/products';
    const operation = () => ({
        security: [{ Bearer: [] }],
        responses: { 200: { description: 'OK', content: { 'application/json': { schema: { $ref: '#/components/schemas/Product' } } } } },
    });
    return {
        openapi: '3.0.4',
        paths: {
            [root]: { get: operation(), post: operation() },
            [`${root}/overview`]: { get: operation() },
            [`${root}/recent`]: { get: operation() },
            [`${root}/suggestions`]: { get: operation() },
            [`${root}/public/{id}`]: { get: { ...operation(), security: [] } },
            [`${root}/{id}`]: { get: operation(), patch: operation(), delete: operation() },
            [`${root}/{id}/duplicate`]: { post: operation() },
            '/api/v{version}/admin/users': { get: { responses: {} } },
        },
        components: {
            schemas: {
                Product: {
                    type: 'object',
                    properties: { image: { $ref: '#/components/schemas/Image' }, name: { type: 'string', nullable: true } },
                },
                Image: { type: 'object', properties: { owner: { $ref: '#/components/schemas/Product' } } },
                AdminUser: { type: 'object' },
            },
            securitySchemes: { Bearer: { type: 'http', scheme: 'bearer' } },
        },
    };
}

test('selects only Products and its transitive schemas, including cycles, without mutating the host document', () => {
    const source = sourceDocument();
    const original = structuredClone(source);
    const result = productsContract(source);
    assert.deepEqual(source, original);
    assert.deepEqual(Object.keys(result.components.schemas).sort(), ['Image', 'Product']);
    assert.equal(result.paths['/api/v{version}/admin/users'], undefined);
    assert.deepEqual(result.components.schemas.Product, source.components.schemas.Product);
    assert.deepEqual(result.components.securitySchemes, source.components.securitySchemes);
    assert.deepEqual(result.paths['/api/v{version}/products'].get.security, [{ Bearer: [] }]);
    assert.deepEqual(result.paths['/api/v{version}/products/public/{id}'].get.security, []);
});

test('requires an explicit SDK review when a Products operation disappears or is added', () => {
    const removed = sourceDocument();
    delete removed.paths['/api/v{version}/products/{id}'].delete;
    assert.throws(() => productsContract(removed), /operation disappeared.*DELETE/);
    const added = sourceDocument();
    added.paths['/api/v{version}/products/new'] = { get: { responses: {} } };
    assert.throws(() => productsContract(added), /Name the new Products SDK operation/);
});

test('fails closed on unresolved or external schema references', () => {
    const missing = sourceDocument();
    delete missing.components.schemas.Image;
    assert.throws(() => productsContract(missing), /Missing SDK schema: Image/);
    const external = sourceDocument();
    external.components.schemas.Image = { $ref: 'https://example.com/image.json' };
    assert.throws(() => productsContract(external), /Unsupported SDK reference/);
});

test('canonicalizes object key order while preserving array order and nullability', () => {
    assert.equal(canonicalJson({ z: null, a: { y: 2, x: 1 } }), canonicalJson({ a: { x: 1, y: 2 }, z: null }));
    assert.notEqual(canonicalJson({ required: ['a', 'b'] }), canonicalJson({ required: ['b', 'a'] }));
});

test('rejects compact snapshots instead of treating them as a complete OpenAPI document', () => {
    assert.throws(() => productsContract({ OpenApi: '3.0.4', Endpoints: [] }), /complete OpenAPI/);
});
