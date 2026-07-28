---
id: system.knowledge-model
kind: system
status: current
sources:
  - AGENTS.md
  - docs/README.md
  - docs/adr/README.md
---

# Knowledge Model

The wiki is a derived artifact that reduces repeated repository exploration. It
does not replace repository instructions or design records.

## Knowledge Layers

| Layer | Examples | Purpose |
| --- | --- | --- |
| Evidence | Code, tests, project files, configuration | Proves current behavior and structure |
| Decisions | Accepted ADRs | Records why durable decisions were made |
| Living guidance | `docs/`, scoped `AGENTS.md` | Describes current architecture and working rules |
| Compiled knowledge | `.llm-wiki/` | Connects and summarizes the layers above |

Change policies are a separate executable layer: they translate changed paths
into mandatory checks, structural invariants, and explicit review obligations.
Evidence bundles record how those obligations were resolved without promoting
task-local execution data into canonical repository knowledge.

Task briefs combine these layers into a compact plan, ownership impact expands
direct changes through the executable module graph, and task contracts detect
scope drift. API compatibility and AI evals provide regression gates, while the
failure knowledge base stores only verified, reusable diagnostic resolutions.
The configuration index exposes key names and consumers without storing values;
dependency and rollout reports connect code changes to environment and
deployment obligations.
The quality index adds a deliberately weaker-than-coverage signal for structural
hotspots and missing direct test references so agents can spend review effort
where it is most likely to matter.
The runtime topology connects deployable services to hosted workers, external
clients, webhook surfaces, and recurring jobs so asynchronous and network blast
radius is visible before code changes.
The sensitive-data index supplies candidate lifecycle and logging review leads
without recording runtime values or presenting name matching as proof.

Accepted ADRs are historical records. Current inventories and operational
instructions belong in living documentation, as defined by the
[ADR lifecycle](../../docs/adr/README.md).

## Update Policy

A page should be reviewed when:

- one of its declared sources changes;
- a referenced project, module, route, or workflow is renamed;
- an ADR affecting the page is accepted, deprecated, or superseded;
- architecture tests change a boundary described by the page.

The freshness check compares a Git change set with every page's declared
`sources`. An affected page passes when it is updated in the same change set or
is explicitly marked `stale`.

An update may:

- refresh the summary when sources still agree;
- add a source when a claim needs stronger provenance;
- split a page when it has multiple unrelated responsibilities;
- mark the page `stale` when it cannot yet be reconciled.

The wiki must not resolve a contradiction by inventing a new rule. Record the
conflict and update the canonical source through the normal review process.
