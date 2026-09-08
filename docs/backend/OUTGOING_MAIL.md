# Outgoing mail and bug acknowledgements

## Ownership and behavior

Admin displays MailRelay's durable queue through the shared `IOutgoingEmailJournal` contract and `FoodDiary.Email.MailRelay` adapter. The Admin HTTP endpoint is admin-only and disables response caching. The journal filters by purpose, status and exact recipient with pages of at most 100 records. It shows timestamps, attempts, sender, recipients and correlation. `sent` means SMTP acceptance, not confirmed inbox delivery. Provider errors and raw HTML are not returned. Content and subject are withheld for all purposes except `bug_report_received`, including historical `other` mail, so credentials and private invitation links never reach the browser.

New verification, account-created, password-reset, dietologist-invitation and template-test mail carries an explicit purpose through the application outbox and relay queue. Existing records remain `other`. The queue remains the delivery source of truth; the journal does not send or retry mail.

The optional Admin acknowledgement worker polls persisted MailInbox exports independently of BugTriage analysis. It reads Identity-owned editable `bug_report_received` templates (`en` and `ru`), then enqueues a rendered message using `no-reply@fooddiary.club`, `Reply-To: bugs@fooddiary.club`, `Auto-Submitted: auto-replied` and `X-Auto-Response-Suppress: All`. The acknowledgement references the original Message-ID and carries the MailInbox stored ID as correlation. Cyrillic in the subject/plain text selects Russian, otherwise English. An inactive template suppresses acknowledgement; no hard-coded fallback overrides it.

The worker ignores empty/mismatched envelope senders, system senders, FoodDiary senders, mailing lists, automated mail, multipart delivery reports, and messages with reply/thread headers. It never quotes the untrusted report. Replies remain available in MailInbox; this change does not merge BugTriage reports or add a human reply composer.

MailRelay deduplicates using a hash of normalized recipient and original Message-ID (stored ID when absent). After enqueue succeeds, Admin records only the opaque incoming ID in `BugAcknowledgementReceipts`. A crash before recording is safe to retry. A receipt write failure propagates; duplicate receipt inserts are detached and verified. These technical saves run inside a dedicated background scope with no foreign aggregate mutations. Receipt fingerprints and persistence capability inventory document that ownership. Polling revisits retained mail with a descending cursor; a ten-minute scan timeout bounds a run. Expired MailInbox content cannot be acknowledged.

## Rollout

1. Update/initialize MailRelay before the main app. Its idempotent schema initializer adds purpose and reply metadata columns and a journal ordering index.
2. Apply main database migrations `AddEmailPurposeAndBugAcknowledgement` and `AddBugAcknowledgementReceipts`. The first seeds missing English/Russian templates without overwriting administrator edits.
3. Publish the API and admin frontend. Existing MailRelay and MailInbox client configuration is reused. The worker is disabled by default.
4. Enable `BugAcknowledgement__Enabled=true`. No activation date is required: by default, the worker includes retained historical mail (from the Unix epoch), while existing receipts and relay idempotency prevent repeat acknowledgements. Optionally set `BugAcknowledgement__StartAtUtc` to restrict processing to newer mail. `BugAcknowledgement__PollInterval` defaults to five minutes. Restart the API. Enable only after verifying the existing relay sender-domain policy, DKIM/SPF routing and reception at `bugs@fooddiary.club` support the chosen sender; creating a separate inbox for `no-reply` is unnecessary for this implementation.
5. Send an authorized synthetic report. Verify one acknowledgement, original-thread headers, its incoming correlation, an outgoing journal row and no second reply on a repeated scan or automatic response. Production activation and real email delivery are separate from local tests.

Rollback: disable the worker first. Retain acknowledgement receipts and MailRelay idempotency rows to prevent resending. The added schema columns are backward compatible. Template seeds are deliberately retained by migration rollback to preserve subsequent administrator edits. No production configuration or real mailbox was changed during implementation.

## Verification

Focused tests cover automatic/reply suppression, duplicate Message-IDs, configured templates, dispatch-failure receipts, SMTP reply headers, PostgreSQL queue deduplication, persisted reply metadata, journal filters and secret-content withholding. API contract snapshots cover the new endpoint. Browser checks use synthetic mocked responses and exercise detail/empty states on desktop and mobile; they do not prove production delivery.
