import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { copyFile, mkdir, mkdtemp, readFile, readdir, realpath, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';

const execute = promisify(execFile);
const repository = fileURLToPath(new URL('../../', import.meta.url));
const guard = path.join(repository, 'scripts/ci/Invoke-WikiPrePush.ps1');
const artifactRoot = path.join(repository, '.artifacts/pre-push-guard-tests');
const git = async (root, ...args) => (await execute('git', ['-C', root, ...args], { windowsHide: true })).stdout.trim();

async function write(root, relative, source) {
    const file = path.join(root, relative);
    await mkdir(path.dirname(file), { recursive: true });
    await writeFile(file, source);
}

async function commit(root, subject) {
    await git(root, 'add', '-A');
    await git(root, '-c', 'user.name=Wiki guard test', '-c', 'user.email=wiki@example.invalid', 'commit', '--quiet', '-m', subject);
    return git(root, 'rev-parse', 'HEAD');
}

async function fixture(run, { stale = false } = {}) {
    await mkdir(artifactRoot, { recursive: true });
    const root = await mkdtemp(path.join(artifactRoot, 'space юникод-'));
    try {
        await git(root, 'init', '--quiet', '-b', 'master');
        for (const file of ['LlmWikiGitPaths.ps1', 'LlmWikiProcess.ps1']) {
            const relative = '.llm-wiki/tools/' + file;
            await mkdir(path.dirname(path.join(root, relative)), { recursive: true });
            await copyFile(path.join(repository, relative), path.join(root, relative));
        }
        await write(root, '.llm-wiki/wiki.ps1', `param($Command, $BaseRef, [string[]]$ChangedPath, [switch]$FailOnUnreviewed)
if ($Command -notin @('lint','impact')) { throw 'Unexpected pre-push check scope.' }
if ($Command -eq 'impact' -and (-not $FailOnUnreviewed -or -not $BaseRef -or -not $ChangedPath)) { throw 'Source-impact enforcement needs the outgoing scope.' }
if ((Get-Location).Path -match '[\\\\/](\\.artifacts|bin|obj)[\\\\/]') { throw 'Source checkout is inside an excluded output directory.' }
if (Test-Path hang.txt) { Start-Sleep -Seconds 20 }
Write-Host ('Committed paths: ' + ($ChangedPath -join '|'))
if ($Command -eq 'impact' -and (Get-Content wiki-state.txt -Raw).Trim() -eq 'stale') { throw 'Wiki source reviews are missing.' }
Write-Host 'Committed Wiki checks passed.'
`);
        await write(root, 'wiki-state.txt', stale ? 'stale' : 'fresh');
        await write(root, 'source.txt', 'base\n');
        const base = await commit(root, 'base');
        await git(root, 'update-ref', 'refs/remotes/origin/master', base);
        await run(root, base);
        assert.equal((await git(root, 'worktree', 'list', '--porcelain')).match(/^worktree /gmu)?.length, 1, 'Owned checkout must be cleaned up');
    } finally {
        const absolute = await realpath(root);
        assert.ok(absolute.startsWith((await realpath(artifactRoot)) + path.sep));
        await rm(absolute, { recursive: true, force: true });
    }
}

async function check(root, head, base, timeoutSeconds = 60) {
    return execute('pwsh', ['-NoLogo', '-NoProfile', '-File', guard, '-RepositoryRoot', root,
        '-LocalObjectName', head, '-RemoteObjectName', base, '-TimeoutSeconds', String(timeoutSeconds)], { windowsHide: true, timeout: 30_000, maxBuffer: 4 * 1024 * 1024 });
}

test('committed source reviews cannot be hidden by a clean-looking dirty overlay', async () => fixture(async (root, base) => {
    await write(root, 'wiki-state.txt', 'stale');
    const head = await commit(root, 'unreviewed source');
    await write(root, 'wiki-state.txt', 'fresh');
    await assert.rejects(check(root, head, base), error => /Wiki source reviews are missing/u.test(error.stdout + error.stderr));
    assert.equal(await readFile(path.join(root, 'wiki-state.txt'), 'utf8'), 'fresh');
    assert.equal(await git(root, 'rev-parse', 'HEAD'), head);
}));

test('unrelated dirty files do not block a valid committed revision or get modified', async () => fixture(async (root, base) => {
    await write(root, 'source.txt', 'published\n');
    const head = await commit(root, 'valid source');
    await write(root, 'wiki-state.txt', 'stale');
    await write(root, 'FoodDiary.Web.Client/node_modules/keep.txt', 'shared dependency');
    const result = await check(root, head, base);
    assert.match(result.stdout, /Committed Wiki checks passed/u);
    assert.equal(await readFile(path.join(root, 'FoodDiary.Web.Client/node_modules/keep.txt'), 'utf8'), 'shared dependency');
    assert.equal(await readFile(path.join(root, 'wiki-state.txt'), 'utf8'), 'stale');
}));

test('a pushed ref is checked even when HEAD points at another commit', async () => fixture(async (root, base) => {
    await write(root, 'wiki-state.txt', 'stale');
    const pushed = await commit(root, 'bad pushed ref');
    await write(root, 'wiki-state.txt', 'fresh');
    const head = await commit(root, 'different current ref');
    await assert.rejects(check(root, pushed, base), error => /Wiki source reviews are missing/u.test(error.stdout + error.stderr));
    assert.equal(await git(root, 'rev-parse', 'HEAD'), head);
}));

test('deletions, both rename endpoints, spaces and Unicode reach the Wiki scope', async () => fixture(async (root, base) => {
    await write(root, 'deleted.cs', 'delete me');
    await write(root, 'old name.cs', 'move me');
    const before = await commit(root, 'files to change');
    await rm(path.join(root, 'deleted.cs'));
    await git(root, 'mv', 'old name.cs', 'новое имя.cs');
    const head = await commit(root, 'delete and rename');
    const result = await check(root, head, before);
    const artifacts = path.join(root, '.artifacts/pre-push-wiki');
    const [run] = await readdir(artifacts);
    const request = JSON.parse(await readFile(path.join(artifacts, run, 'request.json'), 'utf8'));
    for (const name of ['deleted.cs', 'old name.cs', 'новое имя.cs']) assert.ok(request.changedPaths.includes(name), name);
}));

test('new refs exclude history already known on the remote', async () => fixture(async (root, base) => {
    await write(root, 'wiki-state.txt', 'fresh');
    await write(root, 'only-new.cs', 'new');
    const head = await commit(root, 'new branch');
    const result = await check(root, head, '0'.repeat(40));
    assert.match(result.stdout, /only-new\.cs/u);
    assert.doesNotMatch(result.stdout, /Committed paths:.*LlmWikiGitPaths/u);
}, { stale: true }));

test('a revision with no outgoing commits skips the Wiki process', async () => fixture(async (root, base) => {
    const result = await check(root, base, base);
    assert.match(result.stdout, /no new commits/u);
}, { stale: true }));

test('unavailable remote objects use known remote history to bound the scope', async () => fixture(async (root, base) => {
    await write(root, 'only-new.cs', 'new');
    const head = await commit(root, 'new source');
    const result = await check(root, head, 'f'.repeat(40));
    assert.match(result.stdout, /only-new\.cs/u);
    assert.doesNotMatch(result.stdout, /Committed paths:.*LlmWikiGitPaths/u);
}));

test('a timed-out check fails closed and removes its owned checkout', async () => fixture(async (root, base) => {
    await write(root, 'hang.txt', 'slow Wiki process');
    const head = await commit(root, 'slow source');
    await assert.rejects(check(root, head, base, 5), error => /timeout=5s/u.test(error.stdout + error.stderr));
    assert.equal(await git(root, 'rev-parse', 'HEAD'), head);
}));
