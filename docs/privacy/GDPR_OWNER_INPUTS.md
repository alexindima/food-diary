# GDPR release facts and decisions

The engineering controls do not constitute legal approval. Complete these facts
before enabling the EU/EEA release. Do not publish invented operator details,
provider agreements or transfer guarantees.

## Current checkpoint — 2 November 2026

- The owner confirmed registration as an individual entrepreneur in Georgia.
  The registered name, correspondence address and final privacy contact are
  still to be supplied. A monitored application-domain email is suitable; no
  specific new mailbox has yet been confirmed as operational.
- The owner reports HOSTKEY hosting with the primary server in the Netherlands.
  Verify the billing entity and actual account terms, DPA, backup locations,
  subprocessors and support access before treating these as established
  deployment guarantees. HOSTKEY publishes [general terms](https://hostkey.com/about-us/legal/)
  and a [customer privacy statement](https://hostkey.com/about-us/legal/privacy-statement/);
  the applicable DPA for hosted application-user data has not been confirmed.
- Launch countries and minimum user age remain undecided. EU/EEA and US markets
  are being considered; neither is an approved jurisdiction-specific release.
- The owner requested a local code checkpoint and deferred the remaining legal
  and production release review until closer to launch. This checkpoint is not
  approval to process public users' health data. The release blockers below
  remain open, including withdrawal/erasure and real PostgreSQL verification.

## Facts to provide

1. **Controller:** legal name (or individual operator), legal form, country of
   establishment, service/postal address, required registration identifiers, and
   an email monitored for privacy requests. Identify who answers those requests.
2. **Audience:** intended EU/EEA countries, other supported countries, minimum
   age, whether children can use the service, and whether nutrition professionals
   receive data as independent controllers or as processors.
3. **Infrastructure:** actual operators and regions for PostgreSQL, object
   storage, backups, Redis, RabbitMQ, telemetry/logs, email and the application.
   Include remote administrative/support access and backup expiry/restore rules.
4. **External providers:** production-enabled AI, identity, wearable, Telegram,
   email, push, billing, support/bug-report and infrastructure providers. Supply
   the applicable data processing agreements, subprocessors, transfer locations
   and safeguards, and actual AI retention/training/residency settings. Do not
   send credentials or API keys.
5. **Retention:** business/statutory requirements for payments, tax/accounting
   documents, support correspondence, security/audit logs and consent evidence;
   confirm the account recovery window and backup erasure schedule. Engineering
   defaults are not a legal retention justification.

## Decisions for qualified privacy counsel

- The owner selected offering whole-account deletion after health consent
  withdrawal, with an optional archive download and a separate deletion
  confirmation. Cancelling deletion never restores consent. This uses the
  existing delayed account-deletion lifecycle, not immediate erasure.
- Finalize withdrawal and erasure scope for each purpose, including existing
  health and attribution records when the user declines whole-account deletion.
  Identify any separate lawful retention grounds. A pause or a second optional
  deletion step cannot replace erasure when consent is the only applicable basis.
  See [GDPR Article 17](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng/).
- Purpose/lawful basis under Article 6 and the applicable Article 9 condition
  for health tracking; approve each consent purpose and both localized notices.
- Required privacy-policy information, recipients, legitimate-interest
  assessments and objection handling; review supported national ePrivacy rules.
- Applicability of an EU representative, DPO, DPIA and national high-risk lists;
  age verification/parental consent where relevant.
- Processor/controller roles, international transfer mechanisms and any required
  transfer impact assessment or supplementary measures.
- The DSAR procedure and identity checks; response within the applicable deadline
  (normally one month), applicable exceptions and request tracking.
- Incident ownership, breach assessment and notification procedure, including
  the GDPR 72-hour notification deadline when its conditions apply.

## Engineering evidence required for release

- Run migrations and real PostgreSQL consent/export/erasure checks.
- Verify fresh/existing account, grant, decline, stale notice, withdrawal,
  offline failure, account switching and both locales in the browser.
- Verify that host privacy enforcement and background wearable/reminder checks
  are enabled and that cleanup/outbox jobs actually run in production.
- Reconcile provider-held data, correspondence, original image files, logs and
  backups with the self-service archive and the manual access/erasure procedure.
- Review the archived notice texts under `consent-notices/`; a material text or
  purpose change requires a new notice version. Existing consent history must
  never be relabelled as consent to a later notice.

Release sign-off remains governed by `PRIVACY_RELEASE_CHECKLIST.md`.
