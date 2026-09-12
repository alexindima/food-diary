# Telegram account access completion

Accepted scope: optional verified email and password for Telegram accounts, usable in a normal browser and Mini App; clear access-method states in profile. Google linking remains explicit, with existing identity/email conflict checks. No automatic account merging. Google provider configuration is separate from email ownership.

## Selected boundary

Keep the existing Mini App backup-email endpoint compatible. Add authenticated OIDC start/complete slices for browser email linking. Reuse the Telegram provider, browser-binding cookie, single-consumption ticket store and existing email-verification delivery/confirmation. A separate ticket purpose binds the requested email, current user and security version to nonce/PKCE and browser. Completion must prove the same Telegram identity and current security version. Never accept an email from callback parameters. Do not issue a new login session or switch accounts during this operation.

The existing Users capability owns assigning the verified email, rejecting address conflicts and revoking sessions. No persistence schema or provider scope change is needed. Confirmation continues through the existing email endpoint; the user signs in again and can then set a password. Existing Google linking adopts the verified Google email for an email-less account and requires a matching email when one is already assigned.

## Acceptance

- Browser and Mini App users can request email verification; address stays unassigned until confirmation.
- Expired/replayed attempts, another browser, another signed-in account, another Telegram identity and changed security version cannot send or attach an email.
- Profile presents missing email, pending confirmation with resend cooldown/edit, confirmed email and eligible password setup; copy works in English and Russian.
- Password setup is not offered without a confirmed email. Last-method disconnect remains blocked.
- Callback clears query credentials before rendering and returns to profile with success/failure state.
- Verify focused backend/frontend tests, API contract snapshots, build, locale checks and isolated stand readiness. Actual provider/email journeys remain distinct from mocked tests.

Local test mail is captured by Mailpit; it is not delivered to an external mailbox.

## Verification on 2026-09-12

- Focused backup-email backend tests: 12 passed. PostgreSQL-backed Telegram/OIDC and Swagger contract tests: 17 passed. Focused frontend tests: 38 passed.
- Frontend build, scoped ESLint, locale checks and scoped whitespace checks passed. API/client images rebuilt and deployed to the isolated Telegram test stand; HTTPS readiness returned 200 and anonymous email-link start returned 401.
- Playwright CLI was used because the Browser plugin was unavailable. Desktop and 390px mobile profile checks used mocked API responses: email entry is visible, password setup is absent without verified email, a failed send does not claim delivery, and there is no horizontal overflow. Screenshots and logs are under `.artifacts/telegram-client/backup-email-*`. This does not replace the live Telegram and email-confirmation journey.
- Broad architecture suite: 1274 passed, two failures outside this change (Users contract source count and UserRelatedDataReadService persistence capability review). Wiki verification is incomplete at source-impact review in the already heavily modified workspace; no blanket review of unrelated changes was recorded.
- Remaining manual acceptance: request confirmation from a real linked Telegram session, open the captured Mailpit verification message, sign in again and set a password. Google remains disabled by test-stand configuration. Rate-limit adjustments are separate and were not implemented here.
