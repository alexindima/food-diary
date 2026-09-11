# Plans

This directory contains active product, feature, SEO, and integration plans when they are still current.

Plans are not automatically current architecture or operational guidance. Treat them as planning context unless they are referenced by a current `AGENTS.md`, `docs/ARCHITECTURE.md`, ADR, or active issue.

## Active Plans

- [Telegram client: registration, account linking, photo meals and statistics](TELEGRAM_CLIENT_IMPLEMENTATION_PLAN.md)
- `ADAPTIVE_COACH_MVP_PLAN.md`
- [Wiki Git precedents and workflow improvements](wiki-git-precedents-improvement-plan.md)

Implemented or stale plans should be removed once durable decisions are captured elsewhere.

When a plan becomes implemented:
- move durable decisions into `docs/adr/`,
- move operational guidance into `docs/backend/`, `docs/frontend/`, or a project `AGENTS.md`.
- remove the plan itself if it no longer drives active work.
