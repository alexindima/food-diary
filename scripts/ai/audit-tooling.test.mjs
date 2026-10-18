import assert from 'node:assert/strict';
import test from 'node:test';
import { randomUUID } from 'node:crypto';
import { fixturePlan } from './owned-fixtures.mjs';
import { fixtureIdentities, objectStatPresence, summarizeFixtureCleanup, validateFixtureObjects } from './owned-fixture-evidence.mjs';
import { projectMailTrace } from './mail-delivery-trace.mjs';
import { requestOwnedApi } from './owned-api-requests.mjs';
import { fixtureOwnershipProof, verifyFixtureOwnership } from './owned-fixture-residue.mjs';

test('sealed cleanup ownership cannot be expanded or moved to another runtime after account deletion', () => {
    const secret = 'synthetic-test-only-ownership-secret';
    const user = randomUUID();
    const manifest = { runtimeId: randomUUID(), runId: randomUUID(), accounts: { client: { id: user } }, entities: [{ id: randomUUID() }], objects: [] };
    manifest.ownershipProof = fixtureOwnershipProof(manifest, secret);
    verifyFixtureOwnership(manifest, secret);
    for (const changed of [
        { ...manifest, runId: randomUUID() },
        { ...manifest, runtimeId: randomUUID() },
        { ...manifest, accounts: { client: { id: randomUUID() } } },
        { ...manifest, entities: [...manifest.entities, { id: randomUUID() }] },
        { ...manifest, objects: [{ bucket: 'fooddiary-public', key: `users/${user}/images/other.png` }] },
    ]) assert.throws(() => verifyFixtureOwnership(changed, secret), /ownership/u);
    assert.throws(() => verifyFixtureOwnership(manifest, 'a-different-synthetic-secret'), /ownership/u);
});

test('history fixture has an independent 25-day numeric oracle, including fractional nested servings', () => {
    const plan = fixturePlan(randomUUID());
    assert.equal(new Set(plan.dates).size, 25);
    assert.equal(plan.dates[0], '2025-01-01T12:00:00Z');
    assert.equal(plan.dates.at(-1), '2025-01-25T12:00:00Z');
    assert.deepEqual(plan.expected, { trackedDays: 25, mealCount: 25, totalCalories: 3850, averageCalories: 154, childRecipeCalories: 308, parentRecipeCalories: 77 });
});

test('fixture evidence refuses foreign identities and traversal before observing storage', () => {
    const user = randomUUID();
    const manifest = { accounts: { client: { id: user } }, entities: [], objects: [{ bucket: 'fooddiary-public', key: `users/${user}/images/file.png` }] };
    assert.deepEqual(fixtureIdentities(manifest), [user]);
    validateFixtureObjects(manifest);
    for (const key of [`users/${randomUUID()}/images/file.png`, `users/${user}/images/../../other/file.png`, `users/${user}/images/\\other`]) {
        assert.throws(() => validateFixtureObjects({ ...manifest, objects: [{ bucket: 'fooddiary-public', key }] }), /scope/u);
    }
    assert.throws(() => fixtureIdentities({ ...manifest, entities: [{ id: 'unvalidated' }] }), /identity/u);
});

test('cleanup never treats an unobserved storage object or remaining attribution as clear', () => {
    const result = summarizeFixtureCleanup([{ table: 'MarketingAttributionEvents', column: 'UserId', count: 1 }], [{ status: 'unknown' }]);
    assert.equal(result.databaseClear, false);
    assert.equal(result.storageClear, false);
    assert.deepEqual(summarizeFixtureCleanup([], [{ status: 'absent' }]), { remaining: [], retainedReceipts: [], objects: [{ status: 'absent' }], databaseClear: true, storageClear: true });
});

test('relay sent and inbox presence do not fabricate SMTP or end-to-end delivery proof', () => {
    const inboxId = randomUUID();
    const result = projectMailTrace(
        { id: randomUUID(), attempts: 1, createdAtUtc: '2026-10-19T00:00:00Z', textBody: 'private-sentinel', subject: 'private-sentinel' },
        { id: randomUUID(), status: 'Sent', attemptCount: 1, textBody: 'private-sentinel', to: ['private-sentinel'], htmlBody: 'private-sentinel' },
        { id: inboxId, status: 'New', receivedAtUtc: '2026-10-19T00:00:00Z', rawMime: 'private-sentinel' },
        { id: randomUUID(), status: 'Completed', attempt: 1, sourceMessageId: inboxId, summary: 'private-sentinel', mergeRequestUrl: 'private-sentinel' },
    );
    assert.equal(result.smtp.observation, 'unknown');
    assert.equal(result.endToEndDelivery, 'not-verified');
    assert.equal(result.inbox.correlatedToOutbound, false);
    assert.equal(result.bugTriage.matchesSelectedInbox, true);
    assert.ok(!JSON.stringify(result).includes('private-sentinel'));
});

test('unobserved provider stages remain unknown', () => {
    const result = projectMailTrace(null, null, null, null);
    for (const stage of ['applicationOutbox', 'relay', 'smtp', 'inbox', 'bugTriage']) assert.equal(result[stage].observation, 'unknown');
});

test('authenticated pinned storage stat distinguishes missing keys from authorization or transport failures', () => {
    assert.equal(objectStatPresence(0, { status: 'success' }), 'present');
    assert.equal(objectStatPresence(1, { status: 'error', error: { cause: { message: 'Object does not exist' } } }), 'absent');
    assert.equal(objectStatPresence(1, { status: 'error', error: { cause: { error: { Code: 'AccessDenied' } } } }), 'unknown');
    assert.equal(objectStatPresence(null, null), 'unknown');
});

test('owned API helper refuses foreign destinations and absolute paths before fetch', async () => {
    await assert.rejects(requestOwnedApi({ apiUrl: 'https://example.invalid', ports: { api: 5000 } }, null, 'GET', '/api/v1/users/info'), /origin/u);
    await assert.rejects(requestOwnedApi({ apiUrl: 'http://127.0.0.1:5000', ports: { api: 5000 } }, null, 'GET', 'https://example.invalid'), /relative/u);
});
