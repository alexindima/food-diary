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
  - .agents/skills/fooddiary-browser-qa/SKILL.md
---

# Capture frontend visual evidence

For a rendered UI change, define one target flow:

`entry route -> user action or state -> expected rendered result`

Use Chrome through Playwright by default for interactive checks, including full
journey audits. Follow
[fooddiary-browser-qa](../../.agents/skills/fooddiary-browser-qa/SKILL.md) for the
Chrome channel, visible window, task-owned session and fallback rules. Preserve
an explicitly requested browser or existing tab. Keep screenshots and temporary
scripts outside the repository unless committed artifacts were explicitly requested.

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

For a file-upload flow, use the executable Wiki facade. It is plan-only unless
`-Run` is supplied; run mode uses the repository Playwright installation,
optionally restores an authenticated storage state, uploads the fixture, waits
for the result, fails on console/page errors, and writes a screenshot:

```powershell
./.llm-wiki/wiki.ps1 visual-qa `
  -Url 'http://127.0.0.1:4200/dashboard' `
  -FixturePath '.artifacts/fixtures/meal.jpg' `
  -TriggerSelector '[aria-label="Recognize from photo"]' `
  -ResultSelector 'fd-ai-photo-result' `
  -Run
```

Record completion in the evidence bundle:

```powershell
./.llm-wiki/wiki.ps1 evidence-review -Id frontend-visual-evidence `
  -Status completed `
  -Reason "Route, viewports, interaction, console, and screenshot location"
```
