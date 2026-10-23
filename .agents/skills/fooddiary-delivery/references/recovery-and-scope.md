# Recovery and exact delivery scope

Resolve checkout, branch, HEAD and current delta first. Inspect live process
handles and their start identity before deciding an interrupted check stopped.
An observation timeout, quiet log or old runtime record is not terminal evidence.

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

Follow an authorized deployed SHA through CI/deployment, image digest or release
manifest, frontend release identity and API health. Report unknown links. A
frontend capture or healthy endpoint alone does not establish backend revision.
