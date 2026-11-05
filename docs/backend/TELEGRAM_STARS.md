# Telegram Stars Premium

FoodDiary offers an automatically renewing **500 XTR / 30 days** Premium
subscription. The price is configurable; 30 days means 2,592,000 seconds, not a
calendar month. Stars purchase prices vary by platform and country: 500 Stars
must not be advertised as a guaranteed USD 6.50 checkout price.

Premium belongs to the FoodDiary account that created the invoice. The website
keeps Paddle monthly/yearly checkout and offers Stars to linked Telegram
accounts. The Telegram Mini App offers only Stars. An existing paid subscription
must be canceled and allowed to expire before starting a different provider;
there is no automatic provider migration or prorating.

## Configuration

New checkouts are disabled by default. API settings:

| Setting | Default / purpose |
| --- | --- |
| `TelegramStars:CheckoutEnabled` | `false`; enable only after the acceptance checks below |
| `TelegramStars:PremiumPrice` | `500`; integer from 1 through 10,000 |
| `TelegramStars:UseTestEnvironment` | `false`; Bot API test endpoint when true |
| `TelegramAuth:BotToken` | Existing bot token; reused for invoice creation and cancellation |
| `TelegramBot:ApiSecret` | Existing shared bot-to-API secret, at least 16 characters |

The bot uses `TelegramBot:Token`, `TelegramBot:ApiSecret`,
`TelegramBot:ApiBaseUrl`, `TelegramBot:WebAppUrl` and
`TelegramBot:UseTestEnvironment`. The API and bot must target the same bot and
Telegram environment. In environment variables, replace `:` with `__`.
Keep tokens and secrets in the existing secret store; never put them in source,
commands, screenshots or logs. No Paddle provider token is used for Stars.

Keep exactly one polling bot instance per token. Do not install a competing
webhook or test poller against the production token. The durable receiver requests
`message`, `callback_query`, `pre_checkout_query`, and `subscription` updates even
when diary operations are disabled. It advances the polling offset only after
financial updates have committed in the API. A failed API call retries the update.

## Payment and access

The API creates an opaque, server-owned invoice contract containing the account,
Telegram payer, amount and currency. Pre-checkout requires the matching linked
account, XTR, the saved amount, a fresh invoice (15 minutes), and one reserved
checkout. A forwarded link cannot grant access to another account. Re-delivery
of the same pre-checkout is safe; a new attempt needs a new invoice.

Only a confirmed recurring payment grants access. Neither an invoice link,
pre-checkout approval, nor the Mini App's `paid` callback grants Premium. The
Mini App waits for the API entitlement. A renewal uses Telegram's paid expiration
and advances the common account expiration. Duplicate updates/charges and old
payments do not extend access twice or shorten a newer paid period.

`Users.PremiumAccessEndsAtUtc` bounds paid Stars privileges at sign-in, session
refresh, and AI authorization. The exact UTC expiration removes Premium even if
the bot misses a renewal-state notification. A null value preserves existing
permanent grants and existing Paddle behavior.

Cancellation uses the first recurring charge ID with
`editUserStarSubscription`. It stops future renewal and preserves the paid
period. The bot also accepts Telegram's subscription state updates. Cancellation
and confirmed-payment intake continue when new checkout is disabled.

## Refunds and support

`/premium` opens subscription settings; `/terms` opens the page containing the
subscription terms; `/paysupport` gives `admin@fooddiary.club`. The same terms and
support address appear on the website. Support requests need the receipt and
payment date, never a password or login code.

There is no new public refund endpoint or automatic refund button. An authorized
operator can issue a Stars refund with Telegram's `refundStarPayment`, using the
saved original Telegram charge ID. The bot's `refunded_payment` update records
the refund. A refund of the current paid period revokes its access immediately
and cancels renewal; an older-period refund preserves a newer paid period.

Revocation and a pending cancellation intent commit before Telegram cancellation
HTTP. If cancellation fails, intake remains unacknowledged and retries the durable
intent; a duplicate refund does not duplicate the financial record. Do not clear
that intent manually to unblock checkout. Refund-before-payment delivery cannot
restore refunded access when the delayed payment arrives.
Financial recovery uses the saved subscription rather than browser account
authorization, so cancellation also completes for an inactive or deleted account.

## Rollout and acceptance

1. Apply `20261105145019_AddPremiumAccessExpiration` and deploy the API with
   checkout disabled. The nullable column is additive and leaves existing grants
   unchanged. Deploy the matching bot and frontend before enabling checkout.
2. Use Telegram's dedicated test environment and a separate test bot/account.
   Set both test-environment flags consistently. Keep production checkout off.
3. Link the test account; check website Paddle plus Stars and Mini App Stars
   only. Complete a 500 XTR invoice and verify that both clients see the same
   Premium account and the provider expiration.
4. Verify a provider renewal, cancellation from both FoodDiary and Telegram,
   insufficient Stars/failed renewal, refund, duplicate delivery, and an API
   outage during delivery. Confirm no duplicate charge records or access grants.
5. Verify the support command/terms and expiry at the exact UTC boundary. Enable
   production checkout only after recording this provider acceptance separately.

Automated tests cover gateway requests, authenticated transport, replay/order
rules, refund recovery, expiration and real PostgreSQL transaction concurrency.
Rendered Chrome checks use controlled API/native-invoice fixtures. They do not
prove a real Telegram purchase, renewal, payout or production configuration.

Watch API failures on `/billing/telegram-stars/updates`, bot intake retries,
pre-checkout rejections/timeouts, cancellation failures and pending refund
intents. Correlate by bounded update/receipt identifiers in protected billing
history; never log raw updates, bot tokens, credentials or invoice URLs. Existing
rate limits apply. Financial records contain the minimized payment envelope,
not diary content; existing billing retention/access rules apply.

Telegram retains undelivered updates for at most 24 hours. Recovery beyond that
window needs operator reconciliation with `getStarTransactions` and protected
billing records; this change does not add a reconciliation job or assume delivery
is permanent. A recovered history discrepancy must be resolved before claiming
renewal acceptance. Short outages retry automatically. If the first payment is
missing, do not infer access from the client callback.

To stop sales, set `TelegramStars:CheckoutEnabled=false` while keeping API, bot,
token, secret and payment intake running for existing subscriptions/refunds.
Rolling the bot back to a version that ignores payment updates is unsafe after
sales start. Prefer a forward fix; preserve the nullable column and receipts on
rollback. Disabling sales does not itself cancel existing renewals.

## Official contracts

- [Digital goods and Stars payments](https://core.telegram.org/bots/payments-stars)
- [Recurring invoice links](https://core.telegram.org/bots/api#createinvoicelink)
- [Successful payment fields](https://core.telegram.org/bots/api#successfulpayment)
- [Subscription changes](https://core.telegram.org/bots/api#subscription)
- [Cancel a Stars subscription](https://core.telegram.org/bots/api#edituserstarsubscription)
- [Refund a Stars payment](https://core.telegram.org/bots/api#refundstarpayment)
- [Transaction reconciliation](https://core.telegram.org/bots/api#getstartransactions)
- [Update retention](https://core.telegram.org/bots/api#getting-updates)
