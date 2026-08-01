---
id: workflow-frontend-visual-evidence
title: Capture frontend visual evidence
kind: workflow
status: current
summary: Verify rendered UI changes with browser identity, console, interaction, responsive, and screenshot evidence.
tags:
  - workflow
  - frontend
  - visual-qa
sources:
  - FoodDiary.Web.Client/package.json
  - FoodDiary.Web.Client/AGENTS.md
---

# Capture frontend visual evidence

For a rendered UI change, define one target flow:

`entry route -> user action or state -> expected rendered result`

Prefer the Codex in-app Browser when available; otherwise use the repository
Playwright workflow and record the fallback reason. Keep screenshots and temporary
scripts outside the repository unless committed artifacts were explicitly
requested.

Required evidence:

- URL and title identify the intended page.
- The DOM contains meaningful content and no framework error overlay.
- Relevant console errors and warnings are absent or explained.
- The target interaction produces an observed state change.
- A screenshot at the viewport affected by the requested scope supports the result.
- A mobile viewport is checked when responsive or mobile behavior is in scope;
  otherwise mobile is recorded as explicitly out of scope.
- Keyboard focus, clipping, overlap, wrapping, missing assets, loading, empty,
  error, and permission states are checked when relevant.
- Both English and Russian rendering are checked when UI copy changes.

Record completion in the evidence bundle:

```powershell
./.llm-wiki/wiki.ps1 evidence-review -Id frontend-visual-evidence `
  -Status completed `
  -Reason "Route, viewports, interaction, console, and screenshot location"
```
