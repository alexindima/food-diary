# FoodDiary Stripe Integration Plan

## Goal
Add a practical first Stripe integration for the current FoodDiary architecture:

- Angular web client
- ASP.NET Core API host
- Application layer with CQRS
- Infrastructure layer with EF Core and external services
- existing `Premium` role used in API authorization and frontend gating

The first version should support:

- buying a premium subscription from the web app
- activating Premium access automatically after successful payment
- keeping Premium access in sync when subscription status changes
- allowing users to manage billing safely

## Recommended First Release Scope

Implement Stripe as a web-first subscription flow.

Start with:

- one premium product
- monthly and yearly prices
- Stripe Checkout
- Stripe Customer Portal
- Stripe webhooks
- automatic assignment and removal of the existing `Premium` role

Do not start with:

- in-app App Store / Google Play subscriptions
- multiple premium tiers
- usage-based billing
- custom card forms
- mobile-native billing flows

## Why This Fits the Current Codebase

The repository already has:

- `Premium` role in domain and JWT issuance
- frontend premium page and premium gating
- profile overview endpoint
- admin summary counting premium users

This means the simplest reliable model is:

- Stripe is the billing system
- the app database stores billing linkage and sync state
- `Premium` role remains the access mechanism

Core idea:

- Stripe events change billing state
- billing state changes update the user's `Premium` role
- refreshed JWT reflects the current access

## Recommended Product Model

### Product

- `FoodDiary Premium`

### Prices

- monthly recurring price
- yearly recurring price

### Trial

- optional `7-day` trial

### Access Rule

Premium access is granted when the user has an active or trialing subscription.

Premium access is removed when the user no longer has premium entitlement according to the billing state.

## Architecture Overview

### Stripe Responsibilities

- host checkout
- process recurring payments
- host billing management portal
- send webhook events

### FoodDiary Responsibilities

- create checkout sessions for authenticated users
- map Stripe customer/subscription records to local users
- persist billing state for audit and UI
- grant or revoke `Premium` role
- expose billing status to frontend

## Minimal Domain/Data Model

The current codebase can work with `Premium` role as the enforcement mechanism, but Stripe still needs local persistence.

Recommended first table:

- `BillingSubscriptions`

Suggested fields:

- `Id`
- `UserId`
- `Provider` = `Stripe`
- `StripeCustomerId`
- `StripeSubscriptionId`
- `StripePriceId`
- `Status`
- `CurrentPeriodStartUtc`
- `CurrentPeriodEndUtc`
- `CancelAtPeriodEnd`
- `CanceledAtUtc`
- `TrialStartUtc`
- `TrialEndUtc`
- `LastWebhookEventId`
- `LastSyncedAtUtc`
- `CreatedOnUtc`
- `ModifiedOnUtc`

Recommended indexes:

- unique on `StripeCustomerId`
- unique on `StripeSubscriptionId`
- index on `UserId`
- index on `(UserId, Status)`

Recommended status values:

- `trialing`
- `active`
- `past_due`
- `canceled`
- `incomplete`
- `incomplete_expired`
- `unpaid`

## Access Mapping Rule

For the first version, treat these statuses as premium-enabled:

- `trialing`
- `active`

Treat these as premium-disabled:

- `canceled`
- `incomplete_expired`
- `unpaid`

Treat `past_due` conservatively.

Recommended first release rule:

- keep premium access during `past_due` only if Stripe still reports the subscription as effectively active in the current period

If you want the simplest first pass:

- `past_due` still keeps Premium
- webhook sync or portal actions eventually downgrade when Stripe moves to `canceled` or `unpaid`

## Backend Design

### Application Layer

Add a dedicated billing feature area.

Suggested structure:

- `FoodDiary.Application/Billing/Commands/CreateCheckoutSession/`
- `FoodDiary.Application/Billing/Commands/CreatePortalSession/`
- `FoodDiary.Application/Billing/Commands/HandleStripeWebhook/`
- `FoodDiary.Application/Billing/Queries/GetBillingOverview/`
- `FoodDiary.Application/Billing/Common/`
- `FoodDiary.Application/Billing/Models/`
- `FoodDiary.Application/Billing/Services/`

Suggested application abstractions:

- `IBillingSubscriptionRepository`
- `IStripeBillingGateway`
- `IBillingAccessService`

Responsibilities:

- create checkout session for current user
- create customer portal session for current user
- map webhook payloads into internal subscription updates
- apply or remove `Premium` role

### Infrastructure Layer

Add:

- Stripe SDK package
- Stripe options binding
- EF mapping for `BillingSubscriptions`
- Stripe service implementation

Suggested infrastructure files:

- `Options/StripeOptions.cs`
- `Billing/StripeBillingGateway.cs`
- `Persistence/Billing/BillingSubscriptionRepository.cs`
- `Persistence/Configurations/BillingSubscriptionConfiguration.cs`

Recommended `StripeOptions` fields:

- `SecretKey`
- `WebhookSecret`
- `PublishableKey`
- `PremiumMonthlyPriceId`
- `PremiumYearlyPriceId`
- `SuccessUrl`
- `CancelUrl`
- `PortalReturnUrl`

Use `ValidateOnStart()` the same way the repo already handles other integrations.

### Presentation Layer

Add a dedicated billing controller.

Suggested endpoints:

- `POST /api/v1/billing/checkout-session`
- `POST /api/v1/billing/portal-session`
- `GET /api/v1/billing/overview`
- `POST /api/v1/billing/webhooks/stripe`

Notes:

- checkout and portal endpoints require authenticated user
- webhook endpoint should not require JWT auth
- webhook endpoint must validate Stripe signature using the webhook secret

### Web API Host

Add options and composition wiring only.

Suggested host work:

- bind Stripe options
- register Stripe infrastructure services
- allow raw body access for webhook signature validation
- keep billing transport in `FoodDiary.Presentation.Api`

## Webhook Handling

The webhook flow is the critical part.

Recommended events to handle first:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`

Optional later:

- `invoice.paid`
- `invoice.payment_failed`

Recommended first-release behavior:

1. Identify the local user.
2. Upsert `BillingSubscriptions`.
3. Recompute whether the user should have `Premium`.
4. Add or remove the `Premium` role.
5. Save changes idempotently.

### User Matching Strategy

Best approach for checkout creation:

- create checkout session server-side for authenticated user
- include local `UserId` in Stripe metadata
- reuse or create `StripeCustomerId` for that user

Then webhooks can match:

- by `StripeCustomerId`
- or by metadata fallback

## Role Synchronization

The repo already uses roles in JWT and frontend checks.

Recommended sync rule:

- Stripe changes local billing subscription state
- billing application service decides whether Premium should be granted
- user roles are updated through `user.ReplaceRoles(...)`

Important:

- never let the frontend decide premium access from Stripe directly
- Stripe is billing truth
- the app database is entitlement sync state
- JWT role is runtime access token

## JWT Refresh Strategy

After successful checkout, the user's stored JWT may still be stale and not yet include `Premium`.

Recommended first-release solution:

1. Stripe Checkout redirects to frontend success page.
2. Frontend calls `GET /billing/overview`.
3. If billing is active but current JWT is stale, frontend calls existing refresh token flow.
4. New JWT now includes the updated `Premium` role.

This keeps the existing auth architecture intact.

## Frontend Design

### New Frontend API Service

Suggested service:

- `src/app/shared/api/billing.service.ts`

Methods:

- `createCheckoutSession(plan: 'monthly' | 'yearly')`
- `createPortalSession()`
- `getBillingOverview()`

Suggested overview model:

- `isPremium`
- `subscriptionStatus`
- `plan`
- `currentPeriodEndUtc`
- `cancelAtPeriodEnd`
- `manageBillingAvailable`

### Premium Page

The existing premium page should become the first real purchase entry point.

Recommended UX:

- show plan comparison
- monthly and yearly CTA buttons
- start checkout on button click
- redirect browser to Stripe Checkout URL

### Success Flow

Recommended route:

- `/premium/success`

Recommended behavior:

- show loading state
- call `billingService.getBillingOverview()`
- call token refresh if needed
- redirect back to premium or dashboard with success message

### Billing Management

Add a `Manage subscription` button on:

- premium page
- profile page later

Click flow:

- call backend `portal-session`
- redirect to Stripe Customer Portal

## Profile/Overview Enrichment

The current profile overview does not expose billing state.

For the first version, that is acceptable if frontend uses a dedicated billing overview endpoint.

Recommended later enhancement:

- include `BillingOverview` in `ProfileOverviewModel`

But do not block initial Stripe integration on that change.

## Configuration

### Backend Secret Config

Do not commit real values.

Add placeholders to example config for:

- Stripe secret key
- Stripe publishable key
- Stripe webhook secret
- price IDs
- success/cancel/portal URLs

### Frontend Config

Frontend likely only needs:

- premium route wiring
- optionally Stripe publishable key if using future direct client integration

For Checkout redirect flow, the frontend does not need Stripe.js at first.

That is a good first-release simplification.

## Recommended Implementation Order

### Phase 1. Backend Billing Core

1. Add Stripe SDK and options.
2. Add `BillingSubscriptions` entity and migration.
3. Add repository and gateway abstractions.
4. Add webhook handler logic.
5. Add role synchronization service.

### Phase 2. Checkout + Portal Endpoints

1. Add checkout session command and endpoint.
2. Add customer portal session command and endpoint.
3. Add billing overview query and endpoint.

### Phase 3. Frontend Purchase Flow

1. Add billing API service.
2. Upgrade premium page to show plans and CTA.
3. Redirect to Stripe Checkout.
4. Add success/cancel handling.
5. Refresh auth session after successful purchase.

### Phase 4. Premium Gating Expansion

After the billing flow is stable, extend premium gating to:

- Adaptive Coach
- Weekly Check-In recommendations
- Fasting Insights
- Dietologist features

## Recommended First Technical Decisions

### 1. Use Stripe Checkout, not custom Elements

Reason:

- less PCI complexity
- faster implementation
- fewer frontend moving parts
- works well for web-first launch

### 2. Use Stripe Customer Portal

Reason:

- simplest billing management flow
- no need to build cancel/resume/change plan UI first

### 3. Keep `Premium` role as the app access flag

Reason:

- already integrated into API and frontend
- minimal refactor
- aligns with current architecture

### 4. Add billing persistence, not just role toggling

Reason:

- role alone is not enough for audit, UI, debugging, or future plan upgrades
- local billing state is needed for observability and future mobile reconciliation

## Risks and Mitigations

### Risk: stale JWT after purchase

Mitigation:

- refresh token after successful checkout return

### Risk: webhook retries or duplicate events

Mitigation:

- process webhooks idempotently
- store last processed Stripe event ID if needed

### Risk: user buys twice

Mitigation:

- if billing overview already shows active premium, route user to manage billing instead of starting a new checkout

### Risk: admin manually changes Premium role

Mitigation:

- allow this for support cases, but treat Stripe sync as the normal automated source for premium status

## Recommended Next Step

Start implementation with the backend foundation:

1. Stripe options
2. `BillingSubscriptions` entity + migration
3. Stripe gateway service
4. webhook endpoint
5. checkout session endpoint

That gives the smallest end-to-end slice that can actually activate Premium access.
