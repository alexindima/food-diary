---
name: fooddiary-telegram-stars-acceptance
description: Verify FoodDiary Telegram Stars purchase, renewal, cancellation, refund and entitlement acceptance in the dedicated test environment. Use for provider acceptance and test-account blockers, not generic Telegram bot menus or Paddle checkout.
---

# FoodDiary Telegram Stars acceptance

Read current `docs/backend/TELEGRAM_STARS.md`,
`docs/backend/TELEGRAM_TEST_STAND.md` and the owning backend/bot/frontend guides.
Use the existing local-runtime skill for environment recovery. Inspect the
existing stand and observed bot identity before proposing another bot or tunnel.

Resolve API and bot test-environment flags, the intended bot/account and frontend
origin without printing tokens or resolved credential-bearing Compose settings.
The isolated FoodDiary stack and Telegram's dedicated test backend are separate
facts. A bot whose name contains "test" does not establish the latter.
Keep one polling receiver per token and preserve existing update ownership.

Verify current test-account/client instructions in official Telegram documentation
before advising login steps. Record the exact client/build and observed error;
do not invent code lengths, test numbers or a successful login from documentation
alone. If the test account cannot sign in, preserve that blocker and continue
independent local contract checks without claiming provider acceptance.

Use the acceptance scenarios in the current Stars runbook: purchase, renewal,
cancellation from each supported client, failed renewal, refund, duplicate/out-of-
order delivery, provider/API interruption and exact UTC expiry. Derive expected
amount, currency and period from current code/configuration. Treat provider
`successful_payment`, persisted charge identity and API entitlement as separate
evidence; an invoice, approved pre-checkout or Mini App callback alone does not
prove payment or Premium access. Verify replay does not extend access twice.

Distinguish synthetic gateway/UI fixtures, real test-provider results and live
production results in the existing task evidence. Use the original test charge
and current runbook when an authorized refund/cancellation is required. The
skill authorizes no live purchase, production enablement, refund or provider
configuration change beyond the user's actual request.

Official starting points: [Stars payments](https://core.telegram.org/bots/payments-stars)
and [Bot API](https://core.telegram.org/bots/api). Recheck current provider behavior
there; do not freeze test-client workarounds or prices into this skill.
