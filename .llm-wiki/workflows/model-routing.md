---
id: workflow-model-routing
title: Route AI tasks to governed model capacity
kind: workflow
status: current
area: ai-development
summary: Select the least costly model and reasoning effort that satisfy deterministic task-complexity and risk floors.
tags:
  - models
  - routing
  - risk
  - cost
sources:
  - .llm-wiki/tools/Manage-LlmWikiModelRouting.ps1
  - .llm-wiki/tools/Manage-LlmWikiModelRoutingOutcome.ps1
  - .llm-wiki/tools/Manage-LlmWikiVerificationPlan.ps1
  - .llm-wiki/knowledge/model-routing-outcomes.json
  - .llm-wiki/policies/workspace-policies.json
---

# Route AI tasks to governed model capacity

Model routing is separate from agent routing. The scheduler chooses an available
agent with suitable repository capabilities and historical reliability. The
model route states the minimum governed model and reasoning effort that the task
requires.

`task-verification-plan` creates `model-routing.json` automatically after risk,
failure prediction, cost, and verification selection are current. The
complexity score combines calibrated risk, predicted verification failures,
verification breadth, change scopes, and additional API, database, and
security-sensitive work. Policy then applies an independent risk floor: high
and critical tasks cannot be downgraded merely because another signal is low.

```powershell
./.llm-wiki/wiki.ps1 task-model-route-show `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name>

./.llm-wiki/wiki.ps1 task-model-route-verify `
  -WorkspacePath .artifacts/llm-wiki/tasks/<name> `
  -FailOnInvalid
```

The receipt records every candidate route, its relative cost, eligibility, and
blocking reasons. It binds the task packet, risk calibration, failure
prediction, verification plan, policy, and generator. Verification independently
recomputes all signals and the recommendation, so editing and rehashing a route
cannot authorize a cheaper model below the canonical complexity or risk floor.

Model identifiers and cost units are governed configuration, not assumptions
inside task prompts. Updating them is a policy change that invalidates existing
routes and requires regeneration.

## Outcome feedback

`task-finish` records the actual readiness, confidence, critique, verification
accuracy, repair count, and failure penalties for the route that was used.
Events form an append-only hash chain in
`.llm-wiki/knowledge/model-routing-outcomes.json`.

```powershell
./.llm-wiki/wiki.ps1 model-route-outcome-metrics
./.llm-wiki/wiki.ps1 model-route-outcome-health
./.llm-wiki/wiki.ps1 model-route-outcome-verify -FailOnInvalid
```

Profiles use a governed prior for small samples and compare recent outcomes with
their historical baseline. If an eligible route becomes degraded, future tasks
that would have selected it are escalated by one rank.

## Quality-cost optimization

Once both the required route and an immediately higher route have enough healthy
real-task samples, routing compares their posterior outcome quality with governed
relative cost. A higher route is eligible only when it clears the configured
minimum quality gain; the weighted utility score then decides whether that gain
is worth its cost. The receipt exposes sample counts, posterior quality, quality
gain, normalized cost score, utility, and every optimization block for each
candidate.

Optimization is deliberately bounded:

- complexity and risk floors are evaluated before optimization;
- learned routing can only move upward, never downward;
- one decision can move at most the configured number of ranks;
- insufficient or degraded evidence cannot justify a quality-cost promotion;
- verification recomputes the complete candidate table and recommendation.

This keeps the cold-start behavior deterministic and least-cost, while allowing
real project evidence to spend more only where the measured quality gain supports
it.
