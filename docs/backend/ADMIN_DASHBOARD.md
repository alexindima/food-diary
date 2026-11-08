# Admin dashboard overview

`GET /api/v1/admin/dashboard/overview` requires the Admin role. The original dashboard endpoint remains compatible. The overview accepts inclusive UTC `from` and `to` dates, or `allTime=true` without dates. Default: current month through today. Invalid, future or reversed dates return 400; custom ranges are bounded to 3661 inclusive days. All time starts at the Unix epoch and has no previous-period comparison. Other ranges compare the immediately preceding equal number of calendar days, not the previous calendar month. Today's data is partial.

The response separates period metrics from current total accounts, Premium accounts and pending moderation reports. These current counters do not represent historical snapshots. Premium is an entitlement, not a paid subscriber count. Registrations are based on retained accounts; permanently purged accounts cannot be reconstructed. Paying users are distinct users with positive completed transaction amounts in the range.

Revenue totals reuse the Admin Billing repository, including refunds, credits, chargebacks and reversals. Amounts remain separated by currency; net here is before taxes and fees, not profit. Payment charts show gross completed transaction amounts using occurred time with creation time as fallback. AI charts aggregate stored input/output token usage, not estimated monetary cost. No historical exchange rates or model prices are invented.

Periods of up to 90 days use daily buckets, longer periods use monthly buckets; partial boundary months retain the requested date predicates. Missing buckets are filled with zero. The all-time empty result has no trend points. Data unavailable due to request failure is shown as an error, not zeros. The screen cancels obsolete requests and stores the date selection in its URL.

Admin Infrastructure owns the no-tracking aggregate reader, consistent with the existing Admin billing reporting boundary and ADR 0029. Explicit Users/Billing/Ai domain references document the types already available through the shared context, without granting write access. Its capabilities are reviewed in the persistence manifest. No new storage, migration, external call or deployment topology is introduced. Aggregates are sequential reads, not an atomic financial ledger snapshot; concurrent transactions may briefly affect adjacent cards differently.

Verification: overview query date validation/comparison tests, PostgreSQL currency/deduplication/boundary/empty-bucket tests, actual OpenAPI snapshots, admin build/unit tests, and synthetic browser checks. Deploy the API before the new admin bundle. Rollback the bundle first if the overview endpoint is unavailable. Existing billing and legacy dashboard contracts remain usable.

Mail moderation statuses and sender suspension remain a separate unfinished feature; the overview links to the inbox without presenting invented queue counts. Login activity and fasting telemetry are no longer fetched by the dashboard.

## Account activity

Accounts show `lastActiveAtUtc` ("Last active" / "Был активен") and accept inclusive UTC calendar filters `lastActiveFrom` and `lastActiveTo`. This replaces the account list's last-login field and filters. Login events remain available in Login activity; authentication timestamps and session lifetimes keep their existing meaning.

The user application records opening an authenticated visible session, returning to a visible tab, and pointer, keyboard or wheel interactions through `POST /api/v1/users/activity`. Both client attempts and persisted updates are throttled to five minutes. There is no heartbeat timer: idle tabs, hidden tabs, token renewal, HTTP polling and realtime traffic do not record activity. The endpoint derives the user from authentication and the instant from the server clock, and rejects impersonation. Administrative browsing never updates the selected account's activity.

Users owns an optional `UserActivities` row, updated atomically and independently of account/profile concurrency and security state. Account purge cascades the row; session logout and session retention do not remove it. Accounts without a recorded foreground event show "No data" / "Нет данных". The migration deliberately does not backfill login timestamps. The timestamp is included in the user's personal-data archive.

Apply the additive `AddUserForegroundActivity` migration and deploy the API before the new user/admin bundles. The API retains its original last-login field and filters for existing clients; the updated admin UI exclusively uses activity. Old user bundles remain usable but do not report foreground activity. Rolling back the bundles/API does not require dropping the activity table.
