# Paddle Production Runbook

## Purpose

This runbook covers the controlled transition of FoodDiary billing from Paddle sandbox to production. Paddle sandbox and production are separate environments: credentials, catalog IDs, customers, subscriptions, and webhook destinations are not transferable.

The application must remain able to receive production webhooks even when new checkouts are disabled. `Paddle:CheckoutEnabled` is the checkout kill switch; it must not be used to disable webhook processing.

## Product decision

FoodDiary treats Paddle subscription item quantity as a unit or seat count, not as access duration. The supported subscription quantity is exactly `1`.

- Do not configure a monthly recurring price where `quantity: 5` means two months of access. Paddle would bill five units on every renewal.
- FoodDiary offers exactly two recurring plans: monthly and yearly.
- Do not create or expose a two-month or multi-quantity plan without a new explicit product decision and implementation.
- Set the minimum and maximum quantity to `1` for both FoodDiary subscription prices in the Paddle catalog.

A Paddle subscription webhook with quantity other than `1` fails closed and does not grant premium access.

## Required production configuration

Store all secrets outside the repository. Never copy sandbox values into production.

| Setting | Production requirement |
| --- | --- |
| `Billing:Provider` | `Paddle` |
| `Paddle:Environment` | `Production` |
| `Paddle:CheckoutEnabled` | `false` during preparation; `true` only at launch |
| `Paddle:ApiBaseUrl` | `https://api.paddle.com` |
| `Paddle:ApiKey` | Live API key with only required permissions |
| `Paddle:ClientSideToken` | Live client-side token; must not start with `test_` |
| `Paddle:WebhookSecretKey` | Secret from the production webhook destination |
| `Paddle:NotificationSettingId` | Production destination ID (`ntfset_...`) used to recover failed deliveries |
| `Paddle:PremiumMonthlyPriceId` | Live recurring monthly price ID |
| `Paddle:PremiumYearlyPriceId` | Live recurring yearly price ID |
| `Paddle:CheckoutUrl` | Approved HTTPS production checkout page |
| `Paddle:WebhookTimestampToleranceSeconds` | `300` unless incident response requires a documented change |

Startup configuration validation intentionally rejects mixed sandbox/live settings.

## Paddle dashboard preparation

1. Confirm the legal entity, supported supplier jurisdiction, payout account, tax category, statement descriptor, support email, refund policy, terms, privacy policy, and approved production domain.
2. Recreate the production product and recurring prices. Record the internal mapping `monthly` and `yearly` to the new live price IDs.
3. Set allowed quantity to exactly `1` for both prices.
4. Create a production webhook destination pointing to `/api/v1/billing/webhooks/paddle`.
5. Subscribe at minimum to:
   - `subscription.created`
   - `subscription.updated`
   - `subscription.canceled`
   - `subscription.paused`
   - `subscription.resumed`
   - `transaction.completed`
   - `transaction.past_due`
   - `adjustment.created`
   - `adjustment.updated`
6. Give the API key only the customer, transaction, subscription, portal, and notification permissions used by the integration.
   Both customer and transaction read permissions are required for safe checkout recovery after an ambiguous network timeout.
   `notification.read` and `notification.write` are required by the hourly failed-notification replay job.

## Pre-deployment verification

1. Back up the production database and verify restore instructions.
2. Deploy the application and the `AddBillingFinancialBreakdown` and `AddBillingWebhookInboxAndPaymentOccurrence` migrations with checkout disabled.
3. Verify application startup succeeds with production settings. A validation failure is a release blocker.
4. Verify the public billing configuration does not expose Paddle while checkout is disabled.
5. Send a signed Paddle webhook test and confirm an HTTP success, a received inbox row, and then a processed row in Admin > Billing > Webhooks.
6. Confirm an invalid signature and an old signed timestamp are rejected.
7. Confirm admin revenue defaults to the current UTC month and displays each currency separately.

## Controlled launch

1. Enable checkout for an internal account only, if the deployment platform supports a staged audience. Otherwise use a short supervised launch window.
2. Set `Paddle:CheckoutEnabled=true` and restart the application normally.
3. Complete one low-value real purchase with a real payment method.
4. Verify all of the following before opening checkout broadly:
   - one checkout transaction was created;
   - only one local pending checkout/payment record exists;
   - `transaction.completed` is stored once;
   - the Paddle subscription ID and current billing period are stored;
   - premium access is granted from the server-side webhook, not from the browser callback;
   - Admin > Billing shows amount, tax, Paddle fee, earnings, and payout earnings;
   - Paddle dashboard and FoodDiary show the same transaction/customer/subscription IDs.
5. Issue a full refund for the control purchase. Confirm the approved adjustment is linked to the original transaction and reduces the monthly summary exactly once.

## Monitoring and alerts

During the first 24 hours, inspect at least hourly; during the first week, inspect daily.

- Any webhook signature validation failure.
- Any webhook processing HTTP 4xx/5xx response.
- Webhook events repeatedly redelivered by Paddle.
- Any webhook inbox row still `received` after two minutes, or `failed` after its scheduled retry.
- Any webhook inbox row at 10 attempts; automatic retries stop there and the event requires investigation.
- Pending checkout older than 15 minutes.
- More than one Paddle `draft` or `ready` API transaction with the same FoodDiary `checkout_reference`.
- `transaction.completed` without a resolvable FoodDiary user.
- Paddle subscription quantity other than `1`.
- Successful transaction without tax/fee/earnings data after Paddle processing completes.
- Local active subscription whose Paddle state is canceled, paused, or past due.
- Duplicate external payment IDs or more than one active provider subscription per user.
- Difference between FoodDiary monthly totals and Paddle transaction/adjustment reports.

## Daily and monthly reconciliation

FoodDiary's collected total is customer money captured minus approved refunds, credits, and chargebacks plus reversals. Paddle earnings are a separate value after Paddle fees. Bank cash is proven only by Paddle payout reconciliation.

Daily:

1. Export or query completed Paddle transactions and approved adjustments for the UTC date range.
2. Compare counts, IDs, currencies, customer totals, taxes, fees, and earnings with Admin > Billing.
3. Investigate missing IDs before manually changing entitlements.

Monthly:

1. Generate Paddle transaction and adjustment reports for the closed period.
2. Reconcile transaction-currency earnings.
3. Reconcile payout-currency earnings and adjustments with the Paddle payout statement.
4. Reconcile the Paddle payout with the bank receipt. Do not treat the customer gross or FoodDiary collected total as bank cash.
5. Preserve the reports according to the accounting retention policy.

## Incident rollback

If checkout or entitlement behavior is unsafe:

1. Set `Paddle:CheckoutEnabled=false` and restart the application.
2. Keep the webhook endpoint and production credentials active so existing payments and subscriptions continue to synchronize.
3. Do not switch existing production subscriptions to sandbox IDs.
4. Identify affected transactions by Paddle ID and reconcile them before granting or revoking access manually.
5. Refund or cancel only through an approved business process in Paddle.
6. Re-enable checkout only after the failing scenario has a regression test and the control purchase/refund flow passes again.

## Launch approval checklist

- [ ] Paddle account and production domain approved.
- [ ] Payout account and tax category confirmed.
- [ ] Live catalog IDs recorded and quantities restricted to `1`.
- [ ] Live secrets stored outside source control and environment validation passes.
- [ ] Production webhook destination and required event subscriptions configured.
- [ ] Database migration applied and backup/restore verified.
- [ ] Checkout kill switch tested.
- [ ] Signed webhook, replay, invalid signature, and stale timestamp tested.
- [ ] Real control purchase and approved refund reconciled end to end.
- [ ] Admin collected, fee, earnings, and payout figures verified against Paddle.
- [ ] Monitoring owner and first-week review schedule assigned.
- [ ] Terms, privacy, refund, support, and cancellation paths verified on the production site.
