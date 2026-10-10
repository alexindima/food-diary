import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';
import ts from 'typescript';
import { fixtureUuid, jsonFixture, OpenApiFixtures } from '../e2e/support/openapi-fixtures.mjs';
import { hydrationEntryFixture, userFixture } from '../src/testing/api-fixtures.ts';

test('all default authenticated smoke fixtures obey their SDK response schemas', () => {
    const source = readFileSync(new URL('../e2e/client-smoke/client-smoke.spec.ts', import.meta.url), 'utf8');
    const parsed = ts.createSourceFile('fixtures.ts', source, ts.ScriptTarget.Latest, true);
    const declarations = parsed.statements.filter(node => ts.isFunctionDeclaration(node) || ts.isVariableStatement(node));
    const printer = ts.createPrinter();
    const selected = declarations.map(node => printer.printNode(ts.EmitHint.Unspecified, node, parsed)).join('\n');
    const javascript = ts.transpileModule(selected, {
        compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS },
    }).outputText;
    const context = { OpenApiFixtures, jsonFixture, fixtureUuid, hydrationEntryFixture, userFixture, Buffer, URL, console };
    vm.createContext(context);
    vm.runInContext(`${javascript}\nthis.fixtureScenarios = CLIENT_API_MOCKS;`, context);
    const contract = new OpenApiFixtures();
    const failures = [];
    for (const scenario of context.fixtureScenarios) {
        const suffix = scenario.path;
        assert.ok(suffix, 'Each scenario needs an explicit path for contract verification');
        const path = `/api/v1${suffix}`;
        const body = JSON.parse(JSON.stringify(scenario.createResponse()));
        try {
            contract.assertResponse('GET', path, body === null ? 204 : 200, body === null ? undefined : body);
        } catch (error) {
            failures.push(`${path}: ${error.message}`);
        }
    }
    assert.deepEqual(failures, []);
});
