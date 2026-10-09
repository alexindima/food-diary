import assert from 'node:assert/strict';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import ts from 'typescript';

test('semantic IDs, time meanings and meal quantity branches reject mixed inputs', () => {
    const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
    const config = ts.readConfigFile(path.join(root, 'tsconfig.json'), ts.sys.readFile);
    assert.equal(config.error, undefined);
    const workspaceOptions = ts.parseJsonConfigFileContent(config.config, ts.sys, root).options;
    const program = ts.createProgram(
        [
            path.join(root, 'scripts/semantic-types.fixture.ts'),
            path.join(root, 'scripts/recipe-semantic-types.fixture.ts'),
            path.join(root, 'scripts/workflow-semantic-types.fixture.ts'),
            path.join(root, 'src/types/barcode-detector.d.ts'),
        ],
        {
            ...workspaceOptions,
            strict: true,
            noEmit: true,
            skipLibCheck: true,
            target: ts.ScriptTarget.ES2022,
            module: ts.ModuleKind.ESNext,
            moduleResolution: ts.ModuleResolutionKind.Bundler,
            experimentalDecorators: true,
        },
    );
    const diagnostics = ts.getPreEmitDiagnostics(program);
    assert.equal(
        diagnostics.length,
        0,
        ts.formatDiagnosticsWithColorAndContext(diagnostics, {
            getCurrentDirectory: () => root,
            getCanonicalFileName: file => file,
            getNewLine: () => '\n',
        }),
    );
});
