# ADR 0021: Extract ContentReports as a logical module

ContentReports-owned application, contracts, domain, EF model, repository adapter, and unit tests live under `Modules/ContentReports`. The legacy `FoodDiary.Application.ContentReports` assembly name and CLR namespaces, EF identity/mapping, HTTP routes, authorization, and central migrations remain stable. Admin depends only on module Contracts; hosts compose module Infrastructure.

## Canonical layout follow-up

The initial legacy assembly/namespace compatibility constraint is superseded for in-repository CLR identities. ContentReports now uses canonical `FoodDiary.Modules.ContentReports.<Project>` assembly names and project-relative namespaces; Application.Abstractions and PersistenceModel are sibling projects. Consumers and EF CLR metadata are updated together. Enum values, relational schema, HTTP routes/payloads, authorization, module ownership and central migrations remain unchanged.

Moderation remains an owner Contracts request handled by Application. Its caller retains transaction completion. The owner context translates only the exact public ContentReports UserId/TargetType/TargetId unique constraint into a concurrency conflict; it does not retry or commit independently. PostgreSQL coverage verifies one winner and rollback of the losing shared transaction.
