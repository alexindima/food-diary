# Purpose-specific privacy controls and account erasure

Status: Proposed

Date: 2026-11-02

## Context

The planned EU/EEA launch requires separate health, AI and optional campaign
choices. Existing AI timestamps do not prove which notice was presented. Account
purge previously retained free text and images when transferring referenced
catalog content, including full transfers to a configured successor.

## Decision

Users owns an append-only purpose/version/time decision history and current
effective choices. Grants require the notice actually presented by the client;
withdrawal remains possible with an older notice. No historical versioned consent
is invented. Local optional tracking starts only after opt-in. Delayed signup
attribution cannot override an explicit account withdrawal.

Export owns the authenticated archive use case. ReadModel.Composition implements
an explicitly projected, bounded archive through the read facade; mapped entities,
credentials and foreign private records are never serialized. The manifest states
the scope, and the existing protected fertility export retains its access check.
This does not replace a complete manual access-request procedure.

This proposal extends the retention decision in ADR 0054: configured and automatic
successors receive only referenced composition after owner-side text and image
redaction. The same owner locks, per-user transaction and rollback guarantees
remain. Images stay with the erased account for outbox-backed deletion. This is
data minimization, without a claim of irreversible anonymity.

## Alternatives

A single policy checkbox cannot express independent optional purposes. Browser-only
state cannot enforce account-linked background processing. Arbitrary entity
serialization risks credentials and foreign data. Full catalog reassignment
preserves unnecessary personal content. These alternatives were rejected.

## Consequences and unresolved release decisions

Consent schema, SDK and HTTP snapshots change together. Archived localized notices
are checked against client/server versions. Review and real PostgreSQL evidence
are required before acceptance and release.

The health-only erasure workflow after consent withdrawal, remaining background
processing, legacy system-owned content, processor/transfer arrangements, controller
identity and lawful retention remain release blockers. See
`../privacy/GDPR_OWNER_INPUTS.md`; this ADR grants no legal approval.
