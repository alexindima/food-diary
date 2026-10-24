import { access, readFile, readdir, realpath } from 'node:fs/promises';
import path from 'node:path';
import { listFiles, portable, readJson, sha256, writeJson } from './files.mjs';
import { runNode } from './process.mjs';

async function fingerprint(root, files) {
    const records = [];
    for (const file of files.sort()) records.push(`${portable(path.relative(root, file))}:${sha256(await readFile(file))}`);
    return sha256(records.join('\n'));
}

export async function inputFingerprint(frontend) {
    const files = [];
    for (const directory of ['src', 'projects', 'assets', 'scripts']) files.push(...(await listFiles(path.join(frontend, directory))));
    // Legacy merged locales are build outputs generated from the split locale sources.
    const inputs = files.filter(file => !/^assets\/i18n\/(?:en|ru)\.json$/u.test(portable(path.relative(frontend, file))));
    for (const file of await readdir(frontend))
        if (/^(?:package(?:-lock)?|angular|tsconfig.*|ngsw-config)\.json$/u.test(file)) inputs.push(path.join(frontend, file));
    return fingerprint(frontend, inputs);
}

export async function outputFingerprint(staticRoot) {
    return fingerprint(staticRoot, await listFiles(staticRoot));
}

async function npmCli() {
    const nodeDirectory = path.dirname(process.execPath);
    const candidates = [
        process.env.npm_execpath,
        path.join(nodeDirectory, 'node_modules/npm/bin/npm-cli.js'),
        path.join(nodeDirectory, '../lib/node_modules/npm/bin/npm-cli.js'),
    ].filter(Boolean);
    try {
        candidates.push(await realpath(path.join(nodeDirectory, 'npm')));
    } catch {
        /* Windows uses npm.cmd instead. */
    }
    for (const candidate of candidates) {
        if (!candidate.endsWith('npm-cli.js')) continue;
        try {
            await access(candidate);
            return candidate;
        } catch {
            /* Try the next standard npm layout. */
        }
    }
    throw new Error('Cannot locate npm-cli.js beside Node. Run this command through npm or use a standard Node/npm installation.');
}

export async function buildProduction(repositoryRoot, reportDirectory) {
    const frontend = path.join(repositoryRoot, 'FoodDiary.Web.Client');
    const before = await inputFingerprint(frontend);
    const code = await runNode([await npmCli(), 'run', 'build:prod'], { cwd: frontend, log: path.join(reportDirectory, 'build.log') });
    if (code !== 0) throw new Error(`Production build failed (${code}); see ${reportDirectory}/build.log.`);
    const after = await inputFingerprint(frontend);
    if (before !== after) throw new Error('Build inputs changed during production build. Rebuild from a stable checkout.');
    const staticRoot = path.join(frontend, 'dist/browser');
    const receipt = {
        schemaVersion: 1,
        createdAt: new Date().toISOString(),
        command: 'npm run build:prod',
        staticRoot: portable(path.relative(repositoryRoot, staticRoot)),
        inputs: after,
        outputs: await outputFingerprint(staticRoot),
    };
    await writeJson(path.join(repositoryRoot, '.artifacts/web-quality/build.json'), receipt);
    return receipt;
}

export async function verifyBuild(
    repositoryRoot,
    staticRoot,
    receiptFile = path.join(repositoryRoot, '.artifacts/web-quality/build.json'),
) {
    const receipt = await readJson(receiptFile);
    if (receipt.schemaVersion !== 1 || path.resolve(repositoryRoot, receipt.staticRoot) !== path.resolve(staticRoot))
        throw new Error('Build receipt does not describe this static root.');
    if (receipt.inputs !== (await inputFingerprint(path.join(repositoryRoot, 'FoodDiary.Web.Client'))))
        throw new Error('Production build is stale: source inputs changed. Run WebQuality build.');
    if (receipt.outputs !== (await outputFingerprint(staticRoot)))
        throw new Error('Production assets changed after the recorded build. Run WebQuality build.');
    return receipt;
}
