import assert from 'node:assert/strict';
import test from 'node:test';
import { assertNoDevelopmentRuntime } from './check-production-bundles.mjs';

const stats = input => ({ outputs: { 'main.js': { inputs: { [input]: { bytesInOutput: 20 } } } } });
test('production excludes development runtime in eager and lazy chunks', () => {
    assert.doesNotThrow(() => assertNoDevelopmentRuntime(stats('src/app/composition/webmcp/local-webmcp.providers.production.ts')));
    // Angular stats retain the original input name for a replaced empty provider.
    assert.doesNotThrow(() => assertNoDevelopmentRuntime(stats('src/app/composition/webmcp/local-webmcp.providers.ts')));
    assert.doesNotThrow(() => assertNoDevelopmentRuntime(stats('projects/fd-webmcp/src/lib/webmcp-screen.ts')));
    for (const input of [
        'src/app/composition/webmcp/local-webmcp-pilot.service.ts',
        'projects/fd-webmcp/src/lib/executor.ts',
        'projects/fooddiary-admin/src/app/shared/development/admin-webmcp.service.ts',
    ]) {
        const fixture = stats(input);
        assert.throws(() => assertNoDevelopmentRuntime(fixture), /shipped in production/u);
        fixture.outputs['lazy.js'] = fixture.outputs['main.js'];
        delete fixture.outputs['main.js'];
        assert.throws(() => assertNoDevelopmentRuntime(fixture), /shipped in production/u);
    }
    assert.throws(() => assertNoDevelopmentRuntime({}), /metadata is missing/u);
    assert.throws(() => assertNoDevelopmentRuntime({ outputs: { 'styles.css': {} } }), /No executed production/u);
    assert.throws(() => assertNoDevelopmentRuntime({ outputs: { 'main.js': {} } }), /Missing production input metadata/u);
});
