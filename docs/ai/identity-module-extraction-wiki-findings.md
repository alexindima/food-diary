# Identity module extraction Wiki findings

The adaptive Wiki research classified the change as critical and found the Admin physical extraction (`7bd83153cb`) as the closest precedent. Its initial ranked scope over-emphasized the refresh-token flow, so the final boundary was verified against current source, tests, project references, EF configuration, and the immediately preceding Users extraction (`1e85022a6`).

The governed `start` workflow could not be used without violating task isolation: the repository tool requires every governed workspace under `.artifacts/llm-wiki/tasks/<name>`, while this task explicitly prohibited touching `.artifacts/llm-wiki` and allowed only `.artifacts/identity-extraction`. Research, design, decision, diff, test-plan, update, and verify commands that do not require the forbidden workspace remain the evidence route. This tooling limitation is not reported as a green governed baseline.

The evidence supports a one-project application extraction, not symmetric Domain, Infrastructure, PersistenceModel, Contracts, or module-local Abstractions projects. The durable compatibility boundary is documented in `identity-ownership-inventory.md`.
