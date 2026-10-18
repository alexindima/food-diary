import { spawnSync } from 'node:child_process';
import { join } from 'node:path';
import { ownedContainer } from './owned-runtime-client.mjs';

export const UUID = /^(?!00000000-0000-0000-0000-000000000000$)[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/u;
const storageClient = 'quay.io/minio/mc@sha256:a7fe349ef4bd8521fb8497f55c6042871b2ae640607cf99d9bede5e9bdf11727';
const literal = value => `'${String(value).replaceAll("'", "''")}'`;
const identifier = value => `"${String(value).replaceAll('"', '""')}"`;

/** Only internal SELECT statements; results contain counts/whitelisted metadata, never row payloads. */
export function queryOwnedDatabase(state, query) {
    const container = ownedContainer(state, 'postgres');
    const result = spawnSync('docker', ['exec', '-i', container, 'psql', '-X', '-qAt', '-v', 'ON_ERROR_STOP=1', '-U', 'postgres', '-d', 'fooddiary_task'], {
        input: `BEGIN READ ONLY; SET LOCAL statement_timeout = '15s'; ${query}; COMMIT;`,
        encoding: 'utf8', windowsHide: true, timeout: 25_000, maxBuffer: 4 * 1024 * 1024,
    });
    if (result.status !== 0) throw new Error('Owned database observation failed; no cleanup success can be inferred');
    return JSON.parse(result.stdout.trim() || 'null');
}

export function fixtureIdentities(manifest) {
    const ids = [...Object.values(manifest.accounts).map(item => item.id), ...manifest.entities.map(item => item.id)];
    if (ids.some(id => !UUID.test(id))) throw new Error('Invalid manifest entity identity');
    return [...new Set(ids)];
}

export function validateFixtureObjects(manifest) {
    for (const object of manifest.objects) {
        if (!['fooddiary-public', 'fooddiary-staging'].includes(object.bucket) || typeof object.key !== 'string' || /[\\\r\n\0]/u.test(object.key) || object.key.split('/').some(part => part === '.' || part === '..') || !Object.values(manifest.accounts).some(account => object.key.startsWith(`users/${account.id}/images/`))) {
            throw new Error('Storage object is outside the fixture manifest scope');
        }
    }
}

export function scanFixtureReferences(state, manifest) {
    const ids = fixtureIdentities(manifest);
    const needles = [...ids, ...manifest.objects.map(item => item.key), `webmcp-fixture-${manifest.runId}-`];
    const columns = queryOwnedDatabase(state, `SELECT coalesce(json_agg(c), '[]') FROM (SELECT table_schema, table_name, column_name, data_type FROM information_schema.columns WHERE table_schema NOT IN ('pg_catalog','information_schema') AND (data_type = 'uuid' OR (data_type IN ('text','character varying','jsonb','json') AND table_name ~* 'outbox|queue|attribution|audit|image|receipt'))) c`);
    const queries = columns.map(column => {
        const table = `${identifier(column.table_schema)}.${identifier(column.table_name)}`;
        const field = `${identifier(column.column_name)}::text`;
        const predicate = column.data_type === 'uuid'
            ? ids.length === 0 ? 'false' : `${field} IN (${ids.map(literal).join(',')})`
            : needles.map(value => `strpos(${field}, ${literal(value)}) > 0`).join(' OR ');
        return `SELECT ${literal(column.table_name)} AS "table", ${literal(column.column_name)} AS "column", count(*)::int AS "count" FROM ${table} WHERE ${predicate}`;
    });
    return queries.length === 0 ? [] : queryOwnedDatabase(state, `SELECT coalesce(json_agg(matches), '[]') FROM (${queries.join(' UNION ALL ')}) matches WHERE "count" > 0`);
}

export function observeFixtureObjects(state, manifest) {
    validateFixtureObjects(manifest);
    const storage = ownedContainer(state, 'storage');
    return manifest.objects.map(object => {
        if (!['fooddiary-public', 'fooddiary-staging'].includes(object.bucket) || !Object.values(manifest.accounts).some(account => object.key.startsWith(`users/${account.id}/images/`)) || /[\r\n\0]/u.test(object.key)) {
            throw new Error('Storage object is outside the fixture manifest scope');
        }
        const result = spawnSync('docker', ['run', '--rm', '--network', `container:${storage}`, '--env-file', join(state.directory, 'storage.env'), '--entrypoint', '/bin/sh', storageClient, '-c', 'mc alias set local http://127.0.0.1:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null && mc stat --json "$1"', 'fixture-stat', `local/${object.bucket}/${object.key}`], {
            encoding: 'utf8', windowsHide: true, timeout: 30_000, maxBuffer: 64 * 1024,
        });
        let status = 'unknown';
        try {
            const lines = result.stdout.trim().split(/\r?\n/u).map(line => JSON.parse(line));
            const observation = lines.at(-1);
            status = objectStatPresence(result.status, observation);
        } catch { /* Provider errors remain unknown and raw output is never returned. */ }
        return { bucket: object.bucket, key: object.key, status };
    });
}

export function objectStatPresence(exitCode, observation) {
    if (exitCode === 0 && observation?.status === 'success') return 'present';
    if (observation?.status === 'error' && (observation.error?.cause?.message === 'Object does not exist' || ['NoSuchKey', 'NoSuchObject', 'NoSuchBucket'].includes(observation.error?.cause?.error?.Code))) return 'absent';
    return 'unknown';
}

export function summarizeFixtureCleanup(references, objects) {
    // Processed deletion receipts are history; any pending/dead-lettered row is checked separately by the caller.
    const retainedReceipts = references.filter(item => item.table === 'ImageObjectDeletionOutbox');
    const remaining = references.filter(item => item.table !== 'ImageObjectDeletionOutbox');
    return { remaining, retainedReceipts, objects, databaseClear: remaining.length === 0, storageClear: objects.every(item => item.status === 'absent') };
}
