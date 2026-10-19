import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

import { ESLint } from 'eslint';

const cwd = fs.realpathSync(path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'));
const eslint = new ESLint({ cwd });

async function lint(source, filePath) {
    const [result] = await eslint.lintText(source, { filePath });
    return new Set(result.messages.map(message => message.ruleId));
}

test('flat config keeps lifecycle and feature restrictions active together', async () => {
    const config = await eslint.calculateConfigForFile('src/app/features/products/lib/list/product-list.facade.ts');
    assert.equal(config.rules['scope-imports/angular-lifecycle'][0], 2);
    assert.equal(config.rules['scope-imports/feature-lib-api'][0], 2);
    assert.equal(config.rules['scope-imports/feature-routes'][0], 2);
});

test('shared API restrictions survive other app scopes', async () => {
    const rules = await lint(
        "import { ProductService } from '../../features/products/api/product.service';\nexport const forbidden = ProductService;\n",
        'src/app/shared/api/export.service.ts',
    );
    assert.ok(rules.has('scope-imports/shared-api'));
    assert.ok(rules.has('boundaries/dependencies'));
});

test('extensionless foreign TypeScript imports are resolved and rejected', async () => {
    const rules = await lint(
        "import { HydrationService } from '../../../hydration/api/hydration.service';\nexport const forbidden = HydrationService;\n",
        'src/app/features/products/lib/list/product-list.facade.ts',
    );
    assert.ok(rules.has('boundaries/dependencies'));
    assert.ok(!rules.has('boundaries/no-unknown-dependencies'));
    assert.ok(rules.has('scope-imports/feature-lib-api'));
});

test('foreign implementation cannot be hidden in a public contract', async () => {
    const rules = await lint("export { MealService } from '../api/meal.service';\n", 'src/app/features/meals/contracts/meal-actions.ts');
    assert.ok(rules.has('boundaries/dependencies'));
});

test('admin public contracts cannot expose raw API clients', async () => {
    const rules = await lint(
        "export { AdminTemplateHistoryService } from '../api/admin-template-history.service';\n",
        'projects/fooddiary-admin/src/app/features/admin-template-history/contracts/history.ts',
    );
    assert.ok(rules.has('boundaries/dependencies'));
});

test('unresolved internal imports fail closed', async () => {
    const rules = await lint(
        "import { missing } from './missing-feature-contract';\nexport const forbidden = missing;\n",
        'src/app/features/products/lib/list/product-list.facade.ts',
    );
    assert.ok(rules.has('boundaries/no-unknown-dependencies'));
});

test('explicit capabilities and owned implementations are allowed', async () => {
    const rules = await lint(
        "import { HYDRATION_ACTIONS } from '../../../hydration/contracts/hydration-actions';\nimport { ProductService } from '../../api/product.service';\nexport const allowed = [HYDRATION_ACTIONS, ProductService];\n",
        'src/app/features/products/lib/list/product-list.facade.ts',
    );
    assert.ok(!rules.has('boundaries/dependencies'));
    assert.ok(!rules.has('boundaries/no-unknown-dependencies'));
});

test('WebMCP library cannot import either application or UI libraries', async () => {
    for (const source of [
        "import { AuthService } from '../../../../src/app/services/auth.service';\nexport const forbidden = AuthService;\n",
        "import { AdminAuthService } from '../../../fooddiary-admin/src/app/features/admin-auth/lib/admin-auth.service';\nexport const forbidden = AdminAuthService;\n",
        "import { FdTourService } from 'fd-tour';\nexport const forbidden = FdTourService;\n",
    ]) {
        const rules = await lint(source, 'projects/fd-webmcp/src/lib/webmcp-executor.ts');
        assert.ok(rules.has('boundaries/dependencies'));
    }
});

test('WebMCP consumers must use the public package entry point', async () => {
    const rules = await lint(
        "import { WebmcpExecutor } from '../../../../projects/fd-webmcp/src/lib/webmcp-executor';\nexport const forbidden = WebmcpExecutor;\n",
        'src/app/composition/webmcp/local-webmcp-pilot.service.ts',
    );
    assert.ok(rules.has('scope-imports/webmcp-public-api'));
});

test('WebMCP library uses relative imports internally', async () => {
    const rules = await lint(
        "import { WebmcpError } from 'fd-webmcp';\nexport const forbidden = WebmcpError;\n",
        'projects/fd-webmcp/src/lib/webmcp-executor.ts',
    );
    assert.ok(rules.has('scope-imports/webmcp-self-import'));
});

test('client and admin may consume the WebMCP public API', async () => {
    for (const file of [
        'src/app/composition/webmcp/local-webmcp-pilot.service.ts',
        'projects/fooddiary-admin/src/app/shared/development/admin-webmcp.service.ts',
    ]) {
        const rules = await lint("import { WebmcpExecutor } from 'fd-webmcp';\nexport const allowed = WebmcpExecutor;\n", file);
        assert.ok(!rules.has('boundaries/dependencies'));
        assert.ok(!rules.has('boundaries/no-unknown-dependencies'));
        assert.ok(!rules.has('scope-imports/webmcp-public-api'));
    }
});
