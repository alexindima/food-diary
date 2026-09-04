# Ai module guidelines

Ai owns usage, prompts and quota orchestration/ledger. Follow docs/ai/ai-ownership-inventory.md. Preserve all provider, consent, image access, quota and cancellation behavior. No live provider calls. No empty Contracts project: existing semantic administration surface remains compatible in Application/Common.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.
