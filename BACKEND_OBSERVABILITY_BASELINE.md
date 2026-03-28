# Backend Observability Baseline

Date: 2026-03-28
Scope: backend transport, business-flow, and operational telemetry

## Purpose

Define the first observability baseline for the backend.

This document is the execution artifact for `B06` in `BACKEND_10_OF_10_PLAN.md`.

## Existing Signals

The backend already had:

- request count and request duration
- unhandled request exception count
- rate-limit rejection count
- presentation operation counters and durations
- OpenTelemetry export wiring

## First Added Business-Flow Signals

The first observability expansion adds a dedicated counter for high-value business routes at the HTTP host boundary.

Metric:

- `fooddiary.api.business_flow.events`

Current tagged flows:

- `auth.register`
- `auth.login`
- `auth.refresh`
- `auth.restore`
- `auth.password-reset.request`
- `auth.password-reset.confirm`
- `auth.verify-email`
- `auth.verify-email.resend`
- `images.upload-url`
- `images.delete`
- `users.delete`

Current outcome tags:

- `success`
- `client_error`
- `server_error`

## Why This First

This baseline gives immediately useful production signals without scattering telemetry logic across many handlers:

- auth regressions become visible quickly
- image lifecycle issues become measurable
- account deletion traffic becomes visible
- the signals stay stable even if implementation details behind the route change

## Review Guidance

When a backend change touches one of the listed flows:

- confirm the flow is still classified correctly
- confirm the expected success/failure outcomes are still meaningful
- add a new flow tag if a new critical route is introduced

## Next Observability Candidates

- AI quota rejections and model fallback counts
- cleanup job outcomes and removed-item counts
- cache hit/miss metrics for cached admin and user-scoped endpoints
- database retry/failure counters
- dashboard-level alert suggestions and runbook links
