# Cross-module use cases through owner requests

Admin, Ai, Billing and BodyMetrics use owner Contracts requests for the migrated cross-module business operations. Consumer Application projects dispatch through ISender; each operation is implemented by an owner Application handler. No consumer references the supplying Application implementation.

## Migrated operations

| Owner | Operations | Scope |
| --- | ---: | --- |
| BodyMetrics | 6 | Weight and waist entries, latest entry and summaries |
| Ai | 6 | Prompt templates/revisions, prompt mutation, usage and completed recognition |
| Lessons | 5 | Administrative list, create, update, delete and import |
| Gamification | 3 | Achievement definition list, create and update |
| ContentReports | 4 | Administrative list/count, review and dismiss |
| Identity | 5 | Email templates/revisions/mutation and login events/device summary |
| Users | 13 | Administrative user operations, billing profiles, trial, premium roles and access |
| Marketing | 1 | Premium conversion recording |

There are 43 operations. Admin authorization and response shaping remain at the existing entrypoints. Dashboard, Statistics, WeeklyCheckIn and Tdee consume BodyMetrics requests. Meals consumes the completed-recognition request, including owner, completion and result-completeness checks.

Billing references Marketing.Contracts for RecordPremiumConversionCommand. Marketing no longer implements a Billing-owned conversion port or references Billing.Contracts.

## Transaction and validation ownership

The migrated owner commands stage writes using IRequest<T>. They do not implement the automatic transactional-command marker: the existing outer use case still owns SaveChanges, rollback and post-commit work. Background workflows with independent per-item commits retain those boundaries. Do not add ICommand<T> mechanically to these nested requests.

AI prompt and Identity email-template normalization and supported-language validation live in the owner handlers. Existing errors, user scoping, date ranges, ordering, cancellation and idempotency remain part of the contract. Mediator dispatch by itself grants no authorization.

Technical ports remain appropriate for repositories, provider adapters, mail delivery, token issuance and narrowly scoped profile/access capabilities. This migration does not ban all interfaces or move those capabilities into unrelated business modules.

Module-operation telemetry recognizes both legacy Application assemblies and canonical module Application/Contracts assemblies. Moving a request to Contracts must not relabel the operation as Other; payloads remain excluded from metric tags.

## Regression protection

CrossModuleRequestBoundaryTests rejects service interfaces exported by the four migrated Contracts assemblies and verifies the request/handler pair and caller-owned commit semantics for all 43 operations. Existing dependency-matrix, aggregate-boundary and ownership tests cover the changed graph and source locations.

OwnerRequestTransactionTests exercises real mediator dispatch with the transaction pipeline for success and failure of an outer command. SharedAiContextIntegrationTests additionally exercises the owner prompt request with real PostgreSQL save and rollback. Existing consumer tests dispatch to the actual relocated handlers where appropriate; focused mocks match request values.

Tests are executed as one consolidated batch after implementation and compilation, without rebuilding for each test project. Execution receipts are recorded separately from this design description.
