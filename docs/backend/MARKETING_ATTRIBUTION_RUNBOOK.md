# Marketing Attribution Runbook

## Purpose

Use this checklist before and after launching paid campaigns. It verifies that campaign attribution captures visits, signups, and premium starts end to end.

## Campaign URL Convention

- `utm_source`: lowercase traffic source, for example `telegram`, `instagram`, `google`.
- `utm_medium`: channel type, for example `social`, `cpc`, `email`, `referral`.
- `utm_campaign`: stable campaign key, for example `2026_07_launch`.
- `utm_content`: creative or placement variant, for example `story_a`, `banner_blue`.
- `utm_term`: keyword or audience segment when relevant.

Example:

```text
https://fooddiary.club/?utm_source=telegram&utm_medium=social&utm_campaign=2026_07_launch&utm_content=story_a
```

## Staging Smoke Test

1. Apply the latest database migrations.
2. Open a fresh browser profile or clear local storage for the app origin.
3. Open the campaign URL with UTM parameters.
4. Register a new account from that session.
5. Open the admin app and go to `Acquisition`.
6. Confirm the dashboard shows:
   - `Visits` increased by 1.
   - `Signups` increased by 1.
   - the campaign appears under `Top campaigns`.
   - recent events include `page_landing` and `signup_completed`.
7. Complete a test premium payment for the same account.
8. Confirm the admin dashboard shows:
   - `Signup to premium` is updated.
   - the campaign row has `1 premium starts`.
   - recent events include `premium_started`.

## SQL Fallback Checks

Use these only when the admin UI is unavailable.

```sql
select event_type, utm_source, utm_medium, utm_campaign, user_id, occurred_at_utc
from marketing_attribution_events
where occurred_at_utc >= now() - interval '24 hours'
order by occurred_at_utc desc
limit 50;
```

```sql
select utm_source, utm_medium, utm_campaign,
       count(*) filter (where event_type = 'page_landing') as visits,
       count(*) filter (where event_type = 'signup_completed') as signups,
       count(*) filter (where event_type = 'premium_started') as premium_starts
from marketing_attribution_events
where occurred_at_utc >= now() - interval '24 hours'
group by utm_source, utm_medium, utm_campaign
order by visits desc;
```

## Retention

Raw attribution events are cleaned by `MarketingAttributionCleanupJob`.

Default settings:

- section: `MarketingAttributionCleanup`
- enabled: `true`
- retention: `365` days
- batch size: `500`
- cron: `30 3 * * *`
