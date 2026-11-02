import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, readdir, rm, stat, utimes, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';
import { writeGeneratedFile } from './generated-file.mjs';

const ownedRoot = fileURLToPath(new URL('../../.artifacts/frontend-generators/', import.meta.url));

async function fixture(t) {
    await mkdir(ownedRoot, { recursive: true });
    const root = await mkdtemp(join(ownedRoot, 'case-'));
    t.after(async () => {
        assert.equal(dirname(root), ownedRoot.replace(/[\\/]$/u, ''));
        await rm(root, { recursive: true, force: true });
    });
    return root;
}

test('publishes complete UTF-8 output over a previous artifact without leaving temporary files', async t => {
    const root = await fixture(t);
    const target = join(root, 'output.json');
    await writeFile(target, 'old');
    await writeGeneratedFile(target, '{"value":"Привет"}\n');
    assert.equal(await readFile(target, 'utf8'), '{"value":"Привет"}\n');
    assert.deepEqual(await readdir(root), ['output.json']);
});

test('keeps the timestamp of an unchanged generated output', async t => {
    const root = await fixture(t);
    const target = join(root, 'output.json');
    await writeFile(target, 'same');
    await utimes(target, new Date('2001-01-01T00:00:00Z'), new Date('2001-01-01T00:00:00Z'));
    const before = (await stat(target)).mtimeMs;
    await writeGeneratedFile(target, Buffer.from('same'));
    assert.equal((await stat(target)).mtimeMs, before);
});

test('refuses a directory target and preserves its contents', async t => {
    const root = await fixture(t);
    const target = join(root, 'output.json');
    await mkdir(target);
    await writeFile(join(target, 'keep'), 'existing');
    await assert.rejects(writeGeneratedFile(target, 'replacement'));
    assert.equal(await readFile(join(target, 'keep'), 'utf8'), 'existing');
    assert.deepEqual(await readdir(root), ['output.json']);
});
