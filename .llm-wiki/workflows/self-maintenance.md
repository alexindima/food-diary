---
id: workflow.self-maintenance
kind: workflow
status: current
sources:
  - .llm-wiki/tools/Invoke-LlmWikiSelfMaintenance.ps1
  - .llm-wiki/tools/code-graph-maintenance.mjs
  - .llm-wiki/tools/wiki-source-maintenance.mjs
  - .llm-wiki/tools/code-graph-performance.test.mjs
  - .llm-wiki/tools/code-graph.mjs
  - .llm-wiki/wiki.ps1
  - Modules/Products/FoodQuality/AGENTS.md
---

# Wiki self-maintenance

Use `wiki.ps1 health -QualityArea Wiki -Format Json` for read-only diagnosis.
Use `wiki.ps1 repair-verify` to repair and recheck through the existing writer
and verification path. A clean task delta does not skip this diagnosis/repair.

The checker independently discovers current module project identities and their
physical roots from Git files. It compares their expected module/layer against
the search projection, reporting actual and expected values with source paths.
The writer uses the same project inventory when compiling documents, so moving
an existing project to a differently named directory needs no folder-specific
patch. Specific architectural roles still need definitions: a new unknown role
is reported instead of silently being treated as application code. Products
FoodQuality is a pure domain formula according to its owning guide.

Each graph build checks ownership and transactionally repairs mismatched derived
rows even when document-content fingerprints are unchanged. This repairs corrupt
metadata without changing application sources. Repeating repair is idempotent.

Markdown provenance checks examine `sources` and relative links in Wiki pages.
Only exact-content renames reported by `git diff --find-renames=100% <BaseRef>`
can rewrite links automatically, and only when the old target is absent and the
new target exists. Repair preserves narrative text and anchors. Generated pages
are reported but never hand-edited; rerun their owning generator. Missing paths
without confirmed moves remain unresolved, with the exact offending Wiki page.
This mechanism does not infer whether a natural-language claim is still true.

The local JSON result exposes changed pages, unresolved findings and next action.
`repair-verify` fails if rechecking still finds problems; it never reports an
unknown architectural role as successfully repaired. Focused regressions cover
relocated roots, unknown roles, corrupted rows, idempotence and source-link moves.

## Reader and recovery contracts

| Entry point | Preparation / fallback |
| --- | --- |
| CLI `context` | May refresh derived SQLite in an isolated source snapshot; backend requests can omit TypeScript. |
| MCP context | Persistent in-process SQLite reader; fingerprint validation and bounded recovery; no implicit JSON fallback. |
| `brief`, `research`, `diff`, and selected planning facades | May choose JSON baseline with a warning if TypeScript is absent and no explicit source was requested. |
| `health -QualityArea Wiki` | Reports provenance/ownership gaps without repair; missing projection is explicit. |
| `graph-build`, `repair-verify` | Deliberate writers; repair applies bounded deterministic changes and rechecks. |

For planning callers requiring SQL, specify `-CompiledIndexSource Sqlite`.
