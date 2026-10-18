import { createHmac, timingSafeEqual } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { ownedContainer } from './owned-runtime-client.mjs';
import { fixtureIdentities, UUID } from './owned-fixture-evidence.mjs';

// Seal while every account is independently observable, before owner deletion.
export function fixtureOwnershipProof(manifest, secret) {
    if (!UUID.test(manifest.runId) || !UUID.test(manifest.runtimeId) || typeof secret !== 'string' || secret.length < 16) throw new Error('Fixture ownership cannot be sealed');
    const payload = JSON.stringify({ runtimeId: manifest.runtimeId, runId: manifest.runId,
        ids: fixtureIdentities(manifest).sort(), objects: manifest.objects.map(object => `${object.bucket}/${object.key}`).sort() });
    return createHmac('sha256', secret).update(payload).digest('hex');
}

export function verifyFixtureOwnership(manifest, secret) {
    if (!/^[a-f0-9]{64}$/u.test(manifest.ownershipProof ?? '') ||
        !timingSafeEqual(Buffer.from(manifest.ownershipProof, 'hex'), Buffer.from(fixtureOwnershipProof(manifest, secret), 'hex'))) {
        throw new Error('Fixture ownership receipt is missing or changed; refuse cleanup');
    }
}

/** Disposable owned-runtime maintenance, after normal owner purge succeeds.
 * Production account deletion never uses this SQL. */
export function cleanupFixtureResidue(state, manifest, secret) {
    verifyFixtureOwnership(manifest, secret);
    if (manifest.runtimeId !== state.id) throw new Error('Fixture runtime mismatch');
    const users = Object.values(manifest.accounts).map(account => account.id);
    if (users.length === 0) return { email: 0, achievements: 0, audit: 0 };
    const literals = values => values.map(value => `'${value}'`).join(','); // UUIDs validated above.
    const userIds = literals(users);
    const targets = literals(fixtureIdentities(manifest));
    const marker = `webmcp-fixture-${manifest.runId}-`;
    const addresses = [`${marker}client@example.test`, `${marker}dietologist@example.test`];
    const recipients = [addresses.slice(0, 1), addresses.slice(1), addresses, addresses.toReversed()]
        .map(value => `'${JSON.stringify(value)}'`).join(',');
    const ownedAudit = `"ActorUserId" IN (${userIds}) AND ("SubjectClientUserId" IS NULL OR "SubjectClientUserId" IN (${userIds})) AND ("TargetId" IS NULL OR "TargetId" IN (${targets}))`;
    const relatedAudit = `"ActorUserId" IN (${userIds}) OR "SubjectClientUserId" IN (${userIds}) OR "TargetId" IN (${targets})`;
    const script = `BEGIN;
SET LOCAL statement_timeout = '15s'; SET LOCAL lock_timeout = '5s';
LOCK TABLE "Users", "EmailOutbox", "AchievementEvaluationOutbox", "AuditEntries" IN SHARE ROW EXCLUSIVE MODE;
DO $fixture$ BEGIN
 IF EXISTS (SELECT 1 FROM "Users" WHERE "Id" IN (${userIds})) THEN RAISE EXCEPTION 'Owner purge must finish first'; END IF;
 IF EXISTS (SELECT 1 FROM "EmailOutbox" WHERE strpos("ToAddressesJson", '${marker}') > 0 AND "ToAddressesJson" NOT IN (${recipients})) THEN RAISE EXCEPTION 'Mixed fixture recipients'; END IF;
 IF EXISTS (SELECT 1 FROM "EmailOutbox" WHERE "ToAddressesJson" IN (${recipients}) AND "LockedUntilUtc" > CURRENT_TIMESTAMP) THEN RAISE EXCEPTION 'Fixture mail is leased'; END IF;
 IF EXISTS (SELECT 1 FROM "AchievementEvaluationOutbox" WHERE "UserId" IN (${userIds}) AND "LockedUntilUtc" > CURRENT_TIMESTAMP) THEN RAISE EXCEPTION 'Fixture evaluation is leased'; END IF;
 IF EXISTS (SELECT 1 FROM "AuditEntries" WHERE (${relatedAudit}) AND NOT (${ownedAudit})) THEN RAISE EXCEPTION 'Fixture audit has foreign references'; END IF;
END $fixture$;
WITH email AS (DELETE FROM "EmailOutbox" WHERE "ToAddressesJson" IN (${recipients}) RETURNING 1),
achievement AS (DELETE FROM "AchievementEvaluationOutbox" WHERE "UserId" IN (${userIds}) RETURNING 1),
audit AS (DELETE FROM "AuditEntries" WHERE ${ownedAudit} RETURNING 1)
SELECT json_build_object('email',(SELECT count(*) FROM email),'achievements',(SELECT count(*) FROM achievement),'audit',(SELECT count(*) FROM audit));
COMMIT;`;
    const result = spawnSync('docker', ['exec', '-i', ownedContainer(state, 'postgres'), 'psql', '-X', '-qAt', '-v', 'ON_ERROR_STOP=1', '-U', 'postgres', '-d', 'fooddiary_task'], {
        input: script, encoding: 'utf8', windowsHide: true, timeout: 25_000, maxBuffer: 64 * 1024,
    });
    if (result.status !== 0) throw new Error('Fixture residue cleanup refused or failed; no success inferred');
    return JSON.parse(result.stdout.trim());
}
