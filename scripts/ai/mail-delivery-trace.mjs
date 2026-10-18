import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { ownedApi } from './owned-runtime-client.mjs';
import { ownedAdminToken, requestOwnedApi } from './owned-api-requests.mjs';
import { queryOwnedDatabase, UUID } from './owned-fixture-evidence.mjs';

const unknown = reason => ({ observation: 'unknown', reason });
const date = value => typeof value === 'string' && Number.isFinite(Date.parse(value)) ? new Date(value).toISOString() : null;
const status = value => ['pending', 'processing', 'sent', 'failed', 'deadlettered', 'queued', 'new', 'read', 'processed', 'completed', 'ignored', 'rejected', 'draft', 'running', 'open'].find(candidate => candidate === String(value).toLowerCase()) ?? 'unknown';
const count = value => Number.isSafeInteger(value) && value >= 0 ? value : null;

/** Whitelist projection: raw mail bodies, headers, recipients, errors and URLs never enter the report. */
export function projectMailTrace(outbox, relay, inbox, bug) {
    return {
        applicationOutbox: outbox === null ? unknown('No matching application outbox row was observed') : {
            observation: 'observed', id: outbox.id, attempts: count(outbox.attempts), createdAtUtc: date(outbox.createdAtUtc),
            processedAtUtc: date(outbox.processedAtUtc), deadLetteredAtUtc: date(outbox.deadLetteredAtUtc),
            meaning: 'Processed means handed to the configured relay transport, not delivered to the recipient',
        },
        relay: relay === null ? unknown('No correlated relay journal entry was observed') : {
            observation: 'observed', id: UUID.test(relay.id ?? '') ? relay.id : null, status: status(relay.status),
            attempts: count(relay.attemptCount), sentAtUtc: date(relay.sentAtUtc),
        },
        smtp: unknown('Independent SMTP acceptance is not exposed by this runtime; relay Sent alone is insufficient'),
        inbox: inbox === null ? unknown('No explicitly selected inbox metadata entry was observed') : {
            observation: 'observed', id: inbox.id, receivedAtUtc: date(inbox.receivedAtUtc), status: status(inbox.status),
            correlatedToOutbound: false,
        },
        bugTriage: bug === null ? unknown('No explicitly selected BugTriage entry was observed') : {
            observation: 'observed', id: bug.id, status: status(bug.status), attempt: count(bug.attempt),
            matchesSelectedInbox: inbox !== null && bug.sourceMessageId === inbox.id,
        },
        endToEndDelivery: 'not-verified',
    };
}

export async function traceOwnedMail(name, outboxId, inboxId = null, bugId = null) {
    if (![outboxId, ...(inboxId === null ? [] : [inboxId]), ...(bugId === null ? [] : [bugId])].every(value => UUID.test(value))) throw new Error('Mail trace requires exact lowercase UUIDs');
    const client = await ownedApi(name);
    const rows = queryOwnedDatabase(client.state, `SELECT coalesce(json_agg(metadata), '[]') FROM (SELECT "Id" AS id, "AttemptCount" AS attempts, "CreatedOnUtc" AS "createdAtUtc", "ProcessedOnUtc" AS "processedAtUtc", "DeadLetteredOnUtc" AS "deadLetteredAtUtc" FROM "EmailOutbox" WHERE "Id" = '${outboxId}') metadata`);
    const outbox = rows[0] ?? null;
    let relay = null, inbox = null, bug = null;
    const observations = [];
    try {
        const token = await ownedAdminToken(client);
        const correlations = queryOwnedDatabase(client.state, `SELECT coalesce(json_agg("CorrelationId"), '[]') FROM "EmailOutbox" WHERE "Id" = '${outboxId}'`);
        const correlation = correlations[0] ?? `fooddiary-email-outbox:${outboxId.replaceAll('-', '')}`;
        const [relayResult, inboxResult, bugResult] = await Promise.allSettled([
            requestOwnedApi(client.state, token, 'GET', `/api/v1/admin/outgoing-emails?limit=100&correlationId=${encodeURIComponent(correlation)}`),
            // Metadata-only list; never fetch the inbox details/content endpoint.
            inboxId === null ? Promise.resolve(null) : requestOwnedApi(client.state, token, 'GET', '/api/v1/admin/mail-inbox/messages/page?limit=100'),
            bugId === null ? Promise.resolve(null) : requestOwnedApi(client.state, token, 'GET', `/api/v1/admin/bugs?limit=1&id=${bugId}`),
        ]);
        if (relayResult.status === 'fulfilled') {
            const journal = relayResult.value;
            if (journal.items?.length === 1) relay = journal.items[0];
            else if ((journal.items?.length ?? 0) > 1) observations.push('Relay correlation was ambiguous; no entry was selected');
        } else observations.push('Relay journal was unavailable');
        if (inboxId !== null && inboxResult.status === 'fulfilled') {
            inbox = inboxResult.value?.items?.find(item => item.id === inboxId) ?? null;
            if (inbox === null) observations.push('Selected inbox ID was not in the bounded metadata page');
        } else if (inboxId !== null) observations.push('Inbox metadata was unavailable');
        if (bugId !== null && bugResult.status === 'fulfilled') bug = bugResult.value?.items?.find(item => item.id === bugId) ?? null;
        else if (bugId !== null) observations.push('BugTriage metadata was unavailable');
    } catch { observations.push('Owned service metadata was unavailable; unobserved stages remain unknown'); }
    return { runtimeId: client.state.id, observedAtUtc: new Date().toISOString(), ...projectMailTrace(outbox, relay, inbox, bug), observations };
}

if (process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url) {
    try {
        const args = process.argv.slice(2);
        const allowed = ['--name', '--outbox-id', '--inbox-id', '--bug-id'];
        if (args.length % 2 !== 0 || args.some((value, index) => index % 2 === 0 && !allowed.includes(value))) throw new Error('Use mail-delivery-trace.mjs --name <runtime> --outbox-id <UUID> [--inbox-id <UUID>] [--bug-id <UUID>]');
        const values = Object.fromEntries(Array.from({ length: args.length / 2 }, (_, index) => [args[index * 2], args[index * 2 + 1]]));
        console.log(JSON.stringify(await traceOwnedMail(values['--name'], values['--outbox-id'], values['--inbox-id'] ?? null, values['--bug-id'] ?? null), null, 2));
    } catch { console.error('Owned mail trace failed; no message content or private transport output is exposed'); process.exitCode = 1; }
}
