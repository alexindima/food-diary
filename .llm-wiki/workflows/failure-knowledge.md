---
id: workflow-failure-knowledge
title: Reuse failure knowledge
kind: workflow
status: current
summary: Search recurring failure signatures before debugging and record durable, verified resolutions.
tags:
  - workflow
  - debugging
  - failures
sources:
  - .llm-wiki/knowledge/failures.json
  - .llm-wiki/tools/Manage-LlmWikiFailures.ps1
---

# Reuse failure knowledge

Before investigating a CI, build, migration, or runtime failure:

```powershell
./.llm-wiki/wiki.ps1 failures -Query "error fragment"
```

After confirming a reusable root cause and fix, record it:

```powershell
./.llm-wiki/wiki.ps1 failure-add -Id short-stable-id `
  -Symptom "Observable error signature" `
  -Cause "Verified root cause" `
  -Fix "Smallest durable resolution" `
  -PathPattern "affected/path/" `
  -Verification "exact command or observation"
```

Do not record guesses, secrets, tokens, personal data, or incident-only details.
