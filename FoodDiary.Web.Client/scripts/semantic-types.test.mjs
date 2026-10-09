import assert from 'node:assert/strict';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import ts from 'typescript';

test('semantic IDs, time meanings and meal quantity branches reject mixed inputs', () => {
    const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
    const program = ts.createProgram([path.join(root, 'scripts/semantic-types.fixture.ts')], {
        strict: true,
        noEmit: true,
        skipLibCheck: true,
        target: ts.ScriptTarget.ES2022,
        module: ts.ModuleKind.ESNext,
        moduleResolution: ts.ModuleResolutionKind.Bundler,
        experimentalDecorators: true,
    });
    const diagnostics = ts.getPreEmitDiagnostics(program);
    assert.equal(diagnostics.length, 0, ts.formatDiagnosticsWithColorAndContext(diagnostics, {
        getCurrentDirectory: () => root,
        getCanonicalFileName: file => file,
        getNewLine: () => '\n',
    }));
});
