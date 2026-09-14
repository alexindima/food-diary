# Ai module guidelines

Ai owns usage, prompts and quota orchestration/ledger. Follow docs/ai/ai-ownership-inventory.md. Preserve all provider, consent, image access, quota and cancellation behavior. No live provider calls. Contracts owns the existing administration and completed-recognition interfaces plus their DTOs. Internal provider, quota and repository ports remain in Application.Abstractions; see Contracts/AGENTS.md.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.

Ai runtime persistence uses AiDbContext; shared migrations and purge coordination remain central. Usage reporting remains host-composed. Quotas and recognition jobs retain independent short transactions.

All seven production projects are siblings. Do not nest projects or repeat an Ai grouping folder inside them. Use namespaces matching project filenames and physical folders, without RootNamespace overrides; retain only the legacy Application assembly name. AiNamespaceTests and AiProjectLayoutTests enforce this structure.

Presentation uses Controllers, Requests, Responses, Models, Mappings, Hubs, Services and Extensions directly; do not add a Features/Ai wrapper. Keep public owner capabilities distinct from internal provider/repository ports. Use Users.Contracts profile types directly and keep usage-summary logic in its handler.
