# AI-Assisted Code Review

Use this rubric for human and agent review. It supplements scoped `AGENTS.md`,
architecture tests, and feature-specific acceptance criteria.

## Review Order

1. Establish the requested behavior and affected trust boundaries.
2. Run `./.llm-wiki/wiki.ps1 diff` and inspect the applicable guides.
3. Review correctness and failure behavior before style or cleanup.
4. Verify tests and generated artifacts cover the actual risk.
5. Record unresolved policy obligations in the evidence bundle.

## Correctness

- Does the change implement every acceptance criterion?
- Are empty, invalid, duplicate, concurrent, retry, and cancellation paths safe?
- Are time, culture, locale, pagination, and boundary values handled explicitly?
- Are errors returned through normal result/transport contracts instead of
  accidental exceptions?
- Does async work propagate `CancellationToken` and preserve idempotency where
  retries are possible?

## Architecture And Ownership

- Is code placed in the owning feature and correct layer?
- Do hosts remain composition roots and presentation projects own transport?
- Do application handlers use the narrowest suitable read/write contract?
- Do cross-module mutations go through the owning module?
- Are MailRelay and MailInbox accessed only through approved client packages?
- Does the executable module graph remain acyclic?

## Data, Transactions, And Side Effects

- Are aggregate invariants preserved before persistence?
- Are database changes, domain events, outbox messages, notifications, and
  external calls ordered consistently with transaction semantics?
- Can a retry duplicate payment, email, notification, webhook, or job effects?
- Are queries bounded and free from obvious N+1 or unbounded materialization?
- Do migrations include implementation/designer pairs, coverage exclusions,
  formatting, and safe forward/backward operational behavior?

## API And Compatibility

- Are authorization, status codes, request validation, and response mapping
  explicit?
- Does a Swagger-visible change require contract snapshot updates?
- Are existing clients protected from unintended route, payload, enum, or null
  semantics changes?
- Are webhook signatures, replay protection, provider idempotency, and error
  responses preserved?

## Security And Privacy

- Is access scoped to the current user/tenant and rechecked server-side?
- Can identifiers be substituted to access another user's data?
- Are secrets, tokens, personal data, and provider payloads excluded from logs?
- Are inputs safe against injection, SSRF, unsafe file content, and resource
  exhaustion where applicable?
- Does personal-data creation, export, retention, deletion, and recovery follow
  the documented lifecycle?

## Frontend And UX

- Does state have the correct component, route, feature, or session ownership?
- Is browser-only behavior SSR-safe?
- Are loading, empty, error, offline, and retry states usable?
- Are keyboard navigation, focus, labels, contrast, reduced motion, and mobile
  layout verified?
- Are English and Russian messages updated together and rendered correctly?
- Are UI-kit primitives and design tokens reused instead of bypassed?

## Tests And Evidence

- Do tests assert behavior rather than implementation details?
- Is the narrowest relevant suite present, including regression coverage?
- Are architecture, contract, integration, visual, migration, or localization
  checks included when triggered by the change policy?
- Were commands actually run, with failures disclosed?
- Does `evidence-validate` pass, or are unresolved risks clearly handed off?

## Finding Quality

Report only actionable findings supported by a concrete code path or missing
requirement. Include:

- affected file and tight line range;
- triggering input/state;
- observable consequence;
- why current tests or guards do not prevent it;
- smallest appropriate remediation direction.

Use the same agent-neutral finding contract in Markdown and JSON:

- `severity`: `critical`, `major`, `warning`, or `info`;
- `kind`: `defect`, `suggestion`, or `question`;
- `location`: affected path and the tightest supported start/end line;
- `trigger`, `consequence`, `testGap`, `remediation`, and supporting `evidence`.

Severity expresses impact; kind expresses reviewer intent. A discussion or
missing product decision is a `question`, not a severity level. Use
`anchorStatus: missing` when no current-source location is known rather than
inventing a file or line. Critical and major findings block approval. Questions
block only when the unresolved decision changes requested behavior, safety, or
compatibility.

Do not report speculative style preferences as defects. If evidence is
insufficient, ask for validation or describe the uncertainty instead of
asserting a bug.
