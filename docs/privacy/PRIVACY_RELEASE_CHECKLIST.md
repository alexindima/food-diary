# Privacy Release Checklist

This checklist is an engineering handoff, not legal advice. The policy must not
be treated as release-ready until the product owner supplies the facts below and
qualified counsel approves the jurisdiction-specific wording.

## Required owner-provided facts

- legal operator/controller name and organizational form
- registration and tax identifiers where publication is required
- physical and privacy-contact addresses
- supported jurisdictions and intended user age
- production hosting regions and database/object-storage locations
- complete processor/subprocessor list, including AI, email, analytics, error tracking, push, storage, and infrastructure providers
- actual retention periods and deletion/backup behavior for every data category
- cross-border transfer locations and the mechanism approved by counsel
- procedure and response address for access, correction, export, consent withdrawal, and deletion requests
- effective date, policy version, and policy-change notification process

## Engineering evidence to attach

- data inventory mapped to database tables, object storage, caches, logs, queues, and backups
- purpose and lawful-basis decision supplied by counsel for each category
- consent records and version captured for optional AI or analytics processing
- account deletion test proving primary data, queued work, assets, and downstream provider requests are handled as documented
- export test proving the produced archive matches the declared scope
- retention jobs and monitoring evidence for logs, login events, attribution events, tokens, outboxes, and deleted accounts
- processor configuration evidence showing actual training, retention, region, and logging settings
- screenshots of registration consent, privacy page, AI consent, settings, export, and deletion flows in both locales

## Repository-derived processor inventory

The following integrations exist in source and therefore require an explicit
production-enabled/disabled decision and, when enabled, matching policy text:

| Integration | Repository evidence | Privacy review question |
| --- | --- | --- |
| OpenAI | `FoodDiary.Integrations/Services/OpenAi` | Which inputs are sent, which models/region are used, and what provider retention setting is active? |
| S3-compatible object storage | `FoodDiary.Integrations/Services/S3*` | Where is the bucket hosted, who operates it, and how are deleted objects/backups expired? |
| MailRelay and downstream SMTP/MX delivery | `FoodDiary.Integrations/Services/RelayEmailTransport.cs` and `MailRelay/` | Which delivery operators receive addresses/content and what logs are retained? |
| Web Push | `FoodDiary.Integrations/Services/WebPush*` | Which browser push services receive subscription endpoints and payload metadata? |
| Google identity | `FoodDiary.Integrations/Authentication/GoogleTokenValidator.cs` | Is Google login enabled and which identity claims are stored? |
| Google Fit | `FoodDiary.Integrations/Wearables/GoogleFitClient.cs` | Is wearable import enabled, which scopes are requested, and how can access be revoked? |
| Telegram | `FoodDiary.Integrations/Authentication/Telegram*` and `FoodDiary.Telegram.Bot/` | Which Telegram identifiers/messages are stored and for how long? |
| PostgreSQL, Redis, RabbitMQ | deployment composition and infrastructure projects | Record operator, region, encryption, backup, access, and retention configuration for each environment. |

Source presence does not prove that an integration is enabled in production.
The release evidence must combine this inventory with deployed configuration
and provider contracts; unsupported assumptions must not be copied into policy
text.

## Current policy claims requiring verification

The localized policy currently contains concrete claims about GDPR legal bases,
international transfers, provider behavior, and retention periods. Before a
release, compare every claim in `assets/i18n/en/privacy.json` and
`assets/i18n/ru/privacy.json` with production configuration and signed provider
terms. Do not infer the Russian policy from the English/EU text; counsel must
approve each supported jurisdiction explicitly.

## Release gate

Release approval requires named sign-off from the product owner, engineering
owner, security/privacy owner, and qualified counsel. Missing facts must remain
visible release blockers; never replace them with invented company details or
generic placeholders in the published page.
