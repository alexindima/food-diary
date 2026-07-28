---
id: workflow-instruction-effectiveness
title: Instruction Effectiveness Learning
kind: workflow
status: current
area: ai-development
summary: Measure how each applicable AGENTS.md version correlates with real task outcomes and surface governed improvement candidates.
tags:
  - instructions
  - prompts
  - learning
  - outcomes
sources:
  - .llm-wiki/tools/Manage-LlmWikiInstructionOutcome.ps1
  - .llm-wiki/knowledge/instruction-outcomes.json
  - .llm-wiki/policies/workspace-policies.json
---

# Instruction Effectiveness Learning

`task-finish` records the exact SHA-256 fingerprint of the root and applicable
project `AGENTS.md` files together with the sealed task outcome. Events are stored
in an append-only hash chain, so later instruction edits do not rewrite historical
evidence.

```powershell
./.llm-wiki/wiki.ps1 instruction-outcome-metrics
./.llm-wiki/wiki.ps1 instruction-outcome-candidates
./.llm-wiki/wiki.ps1 instruction-outcome-verify -FailOnInvalid
```

Profiles are keyed by instruction path and content fingerprint. This matters
because two revisions of the same file must not share an effectiveness score.
Metrics include sample count, average and recent outcome scores, success rate,
repair attempts, and recent-versus-baseline drift. Every observation also binds
the task risk, governed complexity score and band, model route, and context
strategy used for that task.

An improvement candidate is emitted only after the governed minimum sample count
and degradation thresholds are met. Candidates also say whether the observed
fingerprint is still current; stale versions remain useful history but are not
eligible for action.

Candidates are evidence for review, not permission to edit authoritative
instructions. Use the existing learning shadow and canary workflow to test a
proposed revision before adoption. Instruction learning never silently changes
`AGENTS.md`, policy, risk floors, or verification requirements.

## Revision experiments

After reviewing a degraded candidate, edit the applicable `AGENTS.md` as the
candidate revision and start a fingerprint-bound experiment:

```powershell
./.llm-wiki/wiki.ps1 instruction-experiment-start -Id <candidate-id> -Reason "State the hypothesis"
./.llm-wiki/wiki.ps1 instruction-experiment-forecast -Id <experiment-id>
./.llm-wiki/wiki.ps1 instruction-experiment-evaluate -Id <experiment-id>
./.llm-wiki/wiki.ps1 instruction-experiment-stop -Id <experiment-id> -Reason "Record the decision"
./.llm-wiki/wiki.ps1 instruction-experiment-verify -FailOnInvalid
```

The experiment preserves the degraded version as the baseline cohort and binds
the edited file hash as the candidate cohort. Later completed tasks populate the
candidate profile naturally. Evaluation requires independent minimum samples for
both fingerprints and compares average outcome plus success rate.

The verdict uses only matched risk/complexity cohorts present in both revisions.
Each matched stratum is weighted by the smaller cohort size so an abundance of
easy tasks on one side cannot dominate the comparison. Model route and context
strategy remain recorded attribution signals for diagnosis. Without at least one
adequately sampled matched cohort the result stays `inconclusive`, even when the
global averages look favorable. This guards against task-mix confounding and
Simpson's paradox.

Matched outcome deltas also carry a policy-governed confidence interval derived
from per-cohort sample variance. Success-rate uncertainty uses a smoothed
Beta-binomial variance so tiny cohorts with 0% or 100% success do not appear
perfectly certain. `Adopt` requires the conservative lower bounds to clear both
the quality-gain and non-regression thresholds. `Rollback` requires an upper
bound already below a safety threshold. Overlapping intervals remain
`inconclusive`; point estimates alone never authorize adoption.

Every `instruction-experiment-evaluate` call is a durable sequential look, not a
free read. The look is appended to the experiment hash chain with its exact
outcome-registry fingerprint, cohort sample counts, intervals, and verdict.
Policy caps the total number of looks, requires new candidate samples between
looks, and uses a stricter multiple-look-adjusted z-score. This prevents optional
stopping and repeated peeking from inflating false-positive adoption.

`instruction-experiment-stop` does not silently recompute a more favorable
result. It seals the latest recorded look and refuses to stop when outcome data
changed afterward; record a fresh look first. Thus the final decision is
auditable against the evidence that was actually reviewed.

## Power and sample planning

`instruction-experiment-forecast` estimates the required observations per
matched cohort before another sequential look. It combines the policy's minimum
detectable outcome gain, multiple-look-adjusted threshold, target power, and the
observed pooled cohort variance. When a candidate cohort has no variance estimate
yet, the governed default standard deviation prevents a zero-cost forecast.

The result reports required samples per side, current baseline/candidate counts,
remaining samples, assumptions, and whether the estimate hit the policy cap.
This is planning evidence rather than a guarantee: a later variance change can
raise or lower the forecast. It lets maintainers abandon impractical experiments
early instead of accumulating an unbounded `inconclusive` cohort.

The deterministic verdict is:

- `adopt` when the candidate clears the governed outcome gain without reducing
  success rate;
- `rollback` on any governed outcome or success regression;
- `inconclusive` until evidence is sufficient or the gain is material.

Stopping an experiment seals the evaluation and recommendation in the append-only
experiment registry. The tool recommends rollback but never rewrites
authoritative instructions automatically.

## Integrity model

Each event binds the completion fingerprint, retrospective hash, ordered
instruction source set, individual file hashes, measured outcome, policy
fingerprint, and previous event hash. Workspace receipts can be verified against
the global registry.
