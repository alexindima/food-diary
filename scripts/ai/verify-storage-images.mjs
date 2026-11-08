import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { randomBytes, randomUUID } from 'node:crypto';
import { mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { join, resolve, sep } from 'node:path';
import { repositoryRoot } from './generate-feature.mjs';
import { storageImages } from './storage-images.mjs';
import { objectStatPresence } from './owned-fixture-evidence.mjs';

const owner = randomUUID();
const root = resolve(repositoryRoot, '.artifacts/storage-verification');
const directory = join(root, owner);
mkdirSync(directory, { recursive: true });
const environment = join(directory, 'storage.env');
writeFileSync(environment, `MINIO_ROOT_USER=fdverify-${owner.slice(0, 8)}\nMINIO_ROOT_PASSWORD=${randomBytes(24).toString('hex')}\n`, { mode: 0o600 });
let container;

function docker(args, { allowFailure = false, label = 'Storage command failed' } = {}) {
    const result = spawnSync('docker', args, { encoding: 'utf8', windowsHide: true, timeout: 120_000, maxBuffer: 1024 * 1024 });
    // Never echo command output: client initialization can contain credentials.
    if (!allowFailure && (result.error || result.status !== 0)) throw new Error(label);
    return result;
}

function client(script, allowFailure = false) {
    return docker(['run', '--rm', '--network', `container:${container}`, '--env-file', environment,
        '--entrypoint', '/bin/sh', storageImages.client, '-c',
        'mc alias set local http://127.0.0.1:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null && ' + script], { allowFailure });
}

try {
    for (const image of Object.values(storageImages)) {
        assert.match(image, /@sha256:[a-f0-9]{64}$/u, 'Storage dependencies must use immutable digests');
        docker(['pull', image], { label: 'Storage image unavailable for an uncached runner' });
    }
    container = docker(['run', '--detach', '--label', `fooddiary.storage-verification=${owner}`, '--env-file', environment,
        '--publish', '127.0.0.1::9000', storageImages.server, 'server', '/data', '--console-address', ':9001']).stdout.trim();
    assert.match(container, /^[a-f0-9]{64}$/u);
    const binding = docker(['port', container, '9000/tcp']).stdout.trim();
    assert.match(binding, /^127\.0\.0\.1:[0-9]+$/u);
    const base = `http://${binding}`;
    const deadline = Date.now() + 45_000;
    let ready = false;
    while (Date.now() < deadline) {
        try { ready = (await fetch(`${base}/minio/health/ready`, { signal: AbortSignal.timeout(2000), redirect: 'error' })).ok; } catch { /* Startup only. */ }
        if (ready) break;
        await new Promise(resolveWait => setTimeout(resolveWait, 250));
    }
    assert.ok(ready, 'S3-compatible server must become ready');
    client("mc mb local/fooddiary-fixture >/dev/null && printf 'owned-storage-fixture' | mc pipe local/fooddiary-fixture/probe >/dev/null && mc anonymous set download local/fooddiary-fixture >/dev/null");
    const present = client('mc stat --json local/fooddiary-fixture/probe');
    assert.equal(objectStatPresence(present.status, JSON.parse(present.stdout.trim())), 'present');
    const response = await fetch(`${base}/fooddiary-fixture/probe`, { signal: AbortSignal.timeout(5000), redirect: 'error' });
    assert.equal(response.status, 200);
    assert.equal(await response.text(), 'owned-storage-fixture');
    client('mc rm local/fooddiary-fixture/probe >/dev/null');
    const absent = client('mc stat --json local/fooddiary-fixture/probe', true);
    assert.equal(objectStatPresence(absent.status, JSON.parse(absent.stdout.trim())), 'absent', 'Cleanup must prove that deleted objects are absent');
    console.log(JSON.stringify({ status: 'passed', anonymousPulls: 2, ready: true, put: true, stat: true, publicGet: true, deletedObjectProvenAbsent: true }));
} finally {
    if (container) {
        const observedOwner = docker(['inspect', '--format', '{{ index .Config.Labels "fooddiary.storage-verification" }}', container]).stdout.trim();
        assert.equal(observedOwner, owner, 'Only the verifier-owned container can be removed');
        docker(['rm', '--force', '--volumes', container]);
    }
    assert.ok(resolve(directory).startsWith(root + sep));
    rmSync(directory, { recursive: true, force: true });
}
