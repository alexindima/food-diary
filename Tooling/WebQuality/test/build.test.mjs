import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { inputFingerprint, outputFingerprint, verifyBuild } from '../lib/build.mjs';
import { writeJson } from '../lib/files.mjs';

test('production evidence rejects changed source inputs and changed output assets', async t => {
    const root = await mkdtemp(path.join(tmpdir(), 'fd-web-quality-build-'));
    t.after(async () => {
        assert.equal(path.dirname(root), tmpdir());
        await rm(root, { recursive: true, force: true });
    });
    const frontend = path.join(root, 'FoodDiary.Web.Client');
    for (const directory of ['src', 'projects', 'assets/i18n', 'scripts', 'dist/browser'])
        await mkdir(path.join(frontend, directory), { recursive: true });
    const source = path.join(frontend, 'src/main.ts');
    const output = path.join(frontend, 'dist/browser/main.js');
    await writeFile(source, 'original source');
    await writeFile(output, 'original output');
    await writeFile(path.join(frontend, 'tsconfig.app.json'), '{}');
    const receipt = {
        schemaVersion: 1,
        staticRoot: 'FoodDiary.Web.Client/dist/browser',
        inputs: await inputFingerprint(frontend),
        outputs: await outputFingerprint(path.dirname(output)),
    };
    const receiptFile = path.join(root, 'receipt.json');
    await writeJson(receiptFile, receipt);
    await verifyBuild(root, path.dirname(output), receiptFile);
    await writeFile(path.join(frontend, 'assets/i18n/en.json'), 'generated locale output');
    await verifyBuild(root, path.dirname(output), receiptFile);
    await writeFile(source, 'changed source');
    await assert.rejects(verifyBuild(root, path.dirname(output), receiptFile), /stale/u);
    await writeFile(source, 'original source');
    await writeFile(output, 'changed output');
    await assert.rejects(verifyBuild(root, path.dirname(output), receiptFile), /assets changed/u);
    await writeFile(output, 'original output');
    await writeFile(path.join(frontend, 'tsconfig.app.json'), '{"changed":true}');
    await assert.rejects(verifyBuild(root, path.dirname(output), receiptFile), /stale/u);
});
