# MailInbox Data Lifecycle

MailInbox accepts untrusted SMTP traffic and can receive personal data, attachments, credentials sent by mistake, and network identifiers contained in reports. Its database is an operational inbox, not an archive.

## Admission and storage boundaries

- The SMTP listener limits concurrent connections globally and per source address.
- Message admission is bounded per session, source address, and envelope sender. Source and sender values are hashed before they become in-memory rate-limit keys.
- Message size, envelope recipient count, MIME part count, extracted body length, and concurrent MIME processing are bounded.
- Daily message and raw-byte quotas are updated atomically with the message insert. A quota rejection rolls back the insert and returns a temporary SMTP failure.
- Raw MIME is stored as PostgreSQL `bytea`; the ingest path does not convert arbitrary MIME bytes to UTF-8 before persistence.
- A fingerprint of the canonical envelope and raw MIME bytes deduplicates SMTP retries inside the configured window. `Message-Id` is not trusted as the sole idempotency key.

## Retention policy

The currently supported categories use the same conservative default lifecycle. Category is still exposed explicitly so a future policy change must be intentional and reviewed.

| Category | Full content | Permitted metadata |
| --- | --- | --- |
| General operational mail | 30 days | 365 days |
| DMARC aggregate reports | 30 days | 365 days |

Full content includes raw MIME, attachments embedded in raw MIME, text body, and HTML body. After the content period, the retention worker nulls those fields and records `ContentPurgedAtUtc`. The permitted metadata period retains only the message identifier, envelope addresses, recipients, subject, status/read state, size, deduplication key/bucket, and receive/purge timestamps. After the metadata period, the entire row is deleted.

Retention runs immediately when the Web API host starts and then at `MailInboxStorage:CleanupInterval`. Each transaction is bounded by `CleanupBatchSize` and uses locked, skip-locked candidates so multiple workers do not contend on the same rows. Deletion telemetry and logs contain aggregate counts only, never addresses, subjects, message bodies, or raw MIME.

There is no implicit or indefinite legal hold. If a legal hold becomes necessary, it requires a separately reviewed durable hold model, authorization policy, audit trail, and explicit release workflow before retention may be bypassed.

## Access, export, and deletion

Message lists and details are available only through the API-key-protected MailInbox HTTP boundary. Raw MIME is returned only by the authorized details endpoint and becomes unavailable after content purge.

MailInbox messages are not keyed to a FoodDiary user and an email address is not sufficient proof of account ownership. Automated primary-account export or deletion therefore must not guess an association. A privacy request that names MailInbox content is handled as an authorized operational request against the isolated MailInbox store; normal retention remains the default deletion mechanism.

## Operational evidence

Monitor these bounded metrics and outcomes:

- `fooddiary.mailinbox.admission.events` for accepted and rejected SMTP admission;
- `fooddiary.mailinbox.ingestion.events`, duration, and message-size histograms;
- `fooddiary.mailinbox.retention.events` for content purge and metadata deletion counts;
- `storage_quota` and `overloaded` ingestion outcomes for capacity alerts.

Configuration lives under `MailInboxSmtp` and `MailInboxStorage`. Production changes that increase message size, concurrency, quotas, or retention must be reviewed together because they multiply memory and storage exposure.
