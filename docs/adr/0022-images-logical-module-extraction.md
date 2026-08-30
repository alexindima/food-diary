# ADR 0022: Images logical module extraction

## Status

Accepted.

## Decision

Move Images use cases, application-facing ports, `ImageAsset` EF configuration, repository adapters, and module-owned application tests under `Modules/Images`. Preserve the `FoodDiary.Application.Images` assembly and existing CLR namespaces. Keep S3 in Integrations, HTTP transport in Presentation, hosts as composition roots, and migrations/snapshot in central Infrastructure.

`ImageAsset` remains in central Domain as a compatibility seam because central `MealAiSession` owns an EF navigation to it. The deletion-outbox persistence type/configuration remains central because the shared outbox replay/claiming engine and DbContext consume it. These seams avoid project cycles and model identity changes.

## Consequences

Consumers compile against the module Application or Abstractions projects; hosts also register module Infrastructure. No database migration is expected because CLR entity identity, mappings, tables, and shared migration history are unchanged.
