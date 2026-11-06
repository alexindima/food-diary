# Recovery and exact delivery scope

Resolve checkout, branch, HEAD and current delta first. Inspect live process
handles and their start identity before deciding an interrupted check stopped.
An observation timeout, quiet log or old runtime record is not terminal evidence.

For a completed task's authorized commit/push, reuse its finished scope and
current receipts. Do not create a new broad workspace just to publish it.
If a workspace needs completion, use the actual final Git delta; restored paths
must not remain accidental scope. Preview `task-finish -DryRun` with the same
explicit path set as the real finish. A failed completion is not permission to
replace executed proof with manual pass labels; inspect lineage and use recovery.

For a completed canonical check whose evidence publication was interrupted:

```powershell
./.llm-wiki/wiki.ps1 evidence-recover -EvidencePath <bundle.json> -Id <check-id> -ExecutionRecordPath <original.log.execution.json>
```

Keep the original log/record. Recovery validates hashes, exact checkout, canonical
command and current source fingerprint. It restores the actual exit code without
rerunning. Changed sources/logs need new verification. Import published receipts
into compatible workspaces with `task-evidence-import`; a manual pass label is
not execution proof.

For an authorized commit in a shared dirty checkout, designate one staging owner
and inspect existing staged paths. For an isolated whole-file edit,
`git commit --only -- <path>` can preserve unrelated staged work; inspect the
resulting commit and remaining index. Do not use it for a file containing foreign
edits. For shared locale/Wiki files, prepare owned hunks against HEAD and verify
the other content remains.

For large path sets, use an owned NUL-delimited manifest with
`--pathspec-from-file` and `--pathspec-file-nul`, avoiding Windows command length
limits. A path manifest does not prove ownership of every hunk. Keep hooks enabled.

For a worktree created over a large checkpoint, compare its base with the target.
Transfer the owned patch rather than the checkpoint's foreign delta. Inspect
conflicts/content and verify final target sources. A prior check remains historical
until the existing receipt mechanism establishes compatibility.

When local `master` is ahead of the requested release, choose the intended remote
base and transfer only the reviewed owned patch into a suitable existing checkout.
Rebuild derived Wiki outputs against that final source set instead of transferring
unrelated checkpoint indexes. Reconcile the main checkout only after the exact
published SHA and the user's desired local state are known.

`create_worktree` both creates and attaches its checkout. It can become the chat's
primary workspace, which `archive_worktree` cannot remove. Prefer the current
checkout for a read-only verification; inspect `list_artifacts` before creating
another. `attach_worktree` supports an existing managed checkout owned by this
chat, not an arbitrary replacement directory. Do not invent a detach/switch API
or use raw Git deletion to bypass a primary/shared-worktree rejection. Preserve
the checkout and report the lifecycle limitation when no supported return exists.

During release monitoring, use connected tools for the run metadata they actually
expose. A PR-only list cannot establish a push CI or `workflow_run` deployment;
use `gh run list --commit <sha>` and inspect the triggering run relation. A
`404 BlobNotFound` for an unfinished job log does not prove deployment failure:
check job status, wait for completion, then fetch available logs. Use one bounded
poll for the current SHA, report only meaningful transitions and retain run IDs
for recovery. No workflow dispatch/rerun is implied by read-only monitoring.

Follow an authorized deployed SHA through CI/deployment, image digest or release
manifest, frontend release identity and API health. Report unknown links. A
frontend capture or healthy endpoint alone does not establish backend revision.
