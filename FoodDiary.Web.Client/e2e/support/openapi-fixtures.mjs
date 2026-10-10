import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import Ajv from 'ajv';
import addFormats from 'ajv-formats';

const defaultDocument = new URL('../../api-sdk/user.openapi.json', import.meta.url);

/** Stable UUIDs keep dynamically generated wire fixtures consistent with frozen scenarios. */
export function fixtureUuid(label) {
    const bytes = createHash('sha1')
        .update(Buffer.from('6ba7b8119dad11d180b400c04fd430c8', 'hex'))
        .update(`fooddiary-fixture:${label}`)
        .digest()
        .subarray(0, 16);
    bytes[6] = (bytes[6] & 0x0f) | 0x50;
    bytes[8] = (bytes[8] & 0x3f) | 0x80;
    const hex = bytes.toString('hex');
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

function normalizeSchema(value) {
    if (Array.isArray(value)) return value.map(normalizeSchema);
    if (value === null || typeof value !== 'object') return value;
    const result = Object.fromEntries(
        Object.entries(value)
            .filter(([key]) => key !== 'nullable')
            .map(([key, child]) => [key, normalizeSchema(child)]),
    );
    // Swagger represents nullable references with allOf and no explicit type.
    if (value.nullable !== true) return result;
    return value.type === undefined ? { anyOf: [result, { type: 'null' }] } : { ...result, nullable: true };
}

/** Only trusted, checked-in schemas and synthetic test payloads enter this validator. */
export class OpenApiFixtures {
    constructor(document = JSON.parse(readFileSync(defaultDocument, 'utf8'))) {
        this.document = document;
        this.ajv = new Ajv({ strict: false, allErrors: true, coerceTypes: false, useDefaults: false });
        addFormats(this.ajv);
        this.ajv.addSchema({ $id: 'fooddiary-fixtures', components: normalizeSchema(document.components) });
        this.validators = new Map();
        this.routes = Object.entries(document.paths).map(([path, operations]) => ({
            path,
            operations,
            pattern: new RegExp(
                `^${path
                    .split(/(\{[^}]+\})/u)
                    .map(part => (part.startsWith('{') ? '[^/]+' : part.replace(/[.*+?^${}()|[\]\\]/gu, '\\$&')))
                    .join('')}/?$`,
                'u',
            ),
        }));
    }

    operation(method, url) {
        const pathname = new URL(url, 'http://fixture.local').pathname;
        const matches = this.routes.filter(route => route.pattern.test(pathname) && route.operations[method.toLowerCase()]);
        // Prefer a literal route over a parameter route such as /products/{id}.
        matches.sort((left, right) => (left.path.match(/\{/gu)?.length ?? 0) - (right.path.match(/\{/gu)?.length ?? 0));
        const route = matches[0];
        if (!route) throw new Error(`Undeclared fixture operation: ${method.toUpperCase()} ${pathname}`);
        return { path: route.path, ...route.operations[method.toLowerCase()] };
    }

    assertSchema(schemaName, body) {
        if (!this.document.components.schemas[schemaName]) throw new Error(`Unknown fixture schema: ${schemaName}`);
        this.validate({ $ref: `fooddiary-fixtures#/components/schemas/${schemaName}` }, body, schemaName);
        return body;
    }

    assertResponse(method, url, status, body) {
        const operation = this.operation(method, url);
        const response = operation.responses[String(status)] ?? operation.responses.default;
        if (!response) throw new Error(`Undeclared fixture status: ${operation.operationId} ${status}`);
        const schema = response.content?.['application/json']?.schema;
        if (!schema) {
            if (body !== undefined) throw new Error(`Fixture ${operation.operationId} ${status} must have no JSON body`);
            return;
        }
        this.validate(schema, body, `${operation.operationId} ${status}`);
    }

    validate(schema, body, label) {
        const normalized = normalizeSchema(
            JSON.parse(JSON.stringify(schema).replaceAll('"#/components/', '"fooddiary-fixtures#/components/')),
        );
        const key = JSON.stringify(normalized);
        let validator = this.validators.get(key);
        if (!validator) {
            validator = this.ajv.compile(normalized);
            this.validators.set(key, validator);
        }
        if (!validator(body)) throw new Error(`Invalid fixture ${label}: ${this.ajv.errorsText(validator.errors, { dataVar: 'body' })}`);
    }
}

export function jsonFixture(contract, method, url, body, status = 200) {
    contract.assertResponse(method, url, status, body);
    return { status, contentType: 'application/json', body: JSON.stringify(body) };
}

/** Malformed transport is a deliberate scenario, never a success fallback. */
export function invalidJsonFixture(body, reason, status = 200) {
    if (typeof reason !== 'string' || reason.trim().length === 0) throw new Error('An invalid fixture needs a scenario reason');
    return { status, contentType: 'application/json', body: JSON.stringify(body) };
}
