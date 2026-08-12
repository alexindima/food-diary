---
id: workflow-change-packet
kind: workflow
status: current
title: Compile a change packet
summary: Compute diff, policy, ownership, test plan, rollout, ADR context, task brief, and implementation plan once for a shared change input.
tags:
  - workflow
  - performance
  - context
  - packet
sources:
  - .llm-wiki/tools/Get-LlmWikiChangePacket.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Get-LlmWikiImplementationPlan.ps1
---

# Compile a Change Packet

A packet exposes intent-inferred paths with low confidence. Confirm them with
`-PlannedPath` before treating risk, ownership, or required checks as authoritative.

Use one compiled packet when several change-aware views are needed:

```powershell
./.llm-wiki/wiki.ps1 packet `
  -Objective "Safely evolve fasting start" `
  -ChangedPath FoodDiary.Application/Fasting/Commands/StartFasting/StartFastingCommand.cs `
  -OutputPath .artifacts/llm-wiki/change-packet.json
```

The packet contains the exact diff classification, policy result, ownership graph, test plan, rollout plan, ADR context, task brief, and implementation plan. Shared intermediate objects are computed once.
The objective is forwarded into the compiled brief even when intermediate diff,
policy, ownership, and test-plan objects are injected. This prevents a clean
pre-implementation packet from degrading into an unscoped abbreviated brief.
For a brief or test plan without a packet or diff, use their direct
`-ProposedPath` input; packet compilation continues to use its explicit
`-ChangedPath` snapshot.

Its SHA-256 fingerprint covers Git HEAD, base/head refs, normalized changed paths, and objective. Recompile when any of those inputs change; a packet is a snapshot, not a durable source of truth.
