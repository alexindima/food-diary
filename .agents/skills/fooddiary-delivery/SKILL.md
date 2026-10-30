---
name: fooddiary-delivery
description: Complete authorized FoodDiary Git delivery and monitor GitHub CI and deployment to completion for the published revision, reconciling the main checkout, worktrees, local branches, and remote branches. Use for commit, push, branch handoff, release monitoring, or CI follow-up requests, not routine code edits.
---

# FoodDiary delivery

Make the requested final Git and runtime state explicit, then verify it after delivery.

## Establish the target

Read the current repository AGENTS.md and `docs/TESTING_STRATEGY.md`. Inspect the requested checkout, Git root/common directory, current branch and HEAD, working changes, branch ownership in `git worktree list`, and the relevant remote ref. Resolve paths and deployment behavior from current sources.

Translate the request into a concise target state, for example: "main checkout on master, task files committed locally, unrelated working changes preserved". State this as an update and proceed within the user's existing authorization; do not require a new confirmation for an already authorized operation.

A commit on a local master in another worktree does not mean the main checkout is on master, and a local branch does not prove the remote has that commit. Keep these facts separate in the handoff. Preserve unrelated changes and stage only the intended scope. Use managed worktree tools for their lifecycle when applicable.

If a Git lock prevents progress, inspect its exact path, active operations, and ownership. Do not infer that a lock is stale solely from its age or emptiness. Follow tool approval results and repository rules; a policy rejection is not permission to try another deletion mechanism.

## Verify and publish

Use the existing Wiki adaptive route and change-aware test plan where the change requires them. Avoid creating a second release checklist that copies their policies. Local hooks check whitespace, and pre-push additionally checks affected Wiki contracts and source reviews against the committed revision; run focused behavior checks during development and keep the full CI gate. Run commit and push with hooks enabled; inspect the hook output on failure and fix the cause before retrying.

Check runtimes and Docker readiness before a long verification step that needs them. For an operating-system-sensitive CI failure, record exact SHA, workflow, job, step, and error, then reproduce on the relevant OS or clearly state the remaining verification gap. Local Windows success alone does not establish Linux correctness.

After a timed-out push, inspect local Git state and the remote branch before retrying. Resolve whether the requested branch triggers production deployment from the actual workflow. Preserve the user's authorized scope; a retrospective or local commit request does not authorize a push or deployment.

## Follow the intended revision

For recovery after interruption, shared-checkout commits or a worktree transfer,
read [recovery and scope](references/recovery-and-scope.md). Reuse current executed
evidence through the supported commands before considering another long run.

For every authorized deployment, actively follow both `CI Tests` and the resulting `Deploy` run to their terminal results. Resolve the workflow chain from current sources and associate every observation with the exact published SHA; for a `workflow_run` deployment, use its triggering run's `head_sha`, not an unrelated current branch tip. A successful push or green CI does not finish the release. Distinguish queued, running, failed, cancelled, skipped, successful, and superseded revisions.

Prefer connected GitHub tools when they expose the required run metadata and logs; use authenticated `gh` as a fallback. For example, `gh run list --commit <sha>` discovers CI runs, and `gh run view <run-id> --json status,conclusion,event,headSha,jobs,url` identifies the exact run and failed steps. Correlate the resulting deployment with its triggering CI run and source SHA. Wait with bounded calls and back off while state is unchanged; keep the user informed during an active release. Do not silently finish with checks still running. If monitoring cannot continue because access or execution is unavailable, report the unresolved run and blocker explicitly.

The user's standing request already authorizes read-only monitoring for each authorized release; do not ask for monitoring permission again. It does not authorize a new push, rerun, workflow dispatch, merge, or deployment. Continue monitoring in the active task. If follow-up must continue after the turn, use or reuse a temporary thread heartbeat bound to that exact release, include the target SHA and known run IDs, and stop it when the release reaches its final result. Avoid duplicate monitors. Stay quiet while state is unchanged and notify only on meaningful changes, completion, failure, or required user action.

On a failure, inspect the failed job logs before proposing a fix. Record a verified reusable resolution in Wiki failures when appropriate rather than making one historical symptom a permanent skill rule. After a fix, follow the new revision and state whether CI, deployment, and application health have each been verified.

Finish with the main checkout path/branch/HEAD, intended working-tree state, local and remote commit status, applicable CI/deployment links, and any remaining blocker. Do not describe work as published or deployed until the corresponding evidence exists.
