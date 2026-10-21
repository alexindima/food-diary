---
name: fooddiary-browser-qa
description: Verify rendered FoodDiary UI fixes, visual checks, interaction regressions, and user or admin journey audits with Chrome and local WebMCP. Preserve an explicitly requested browser or existing tab.
---

# FoodDiary browser QA

## Choose the browser

Chrome is the repository default for interactive browser verification, including
focused UI fixes and full journey audits. An explicit browser, tab mention, or
request to use an existing signed-in session takes precedence.

- For a new QA session, use Playwright with the Chrome channel and a visible
  window: `playwright-cli -s=<task-session> open <frontend-url> --browser=chrome --headed`.
  Load the available `playwright` skill and use its supported CLI entry point.
  Check the installed command's help before relying on unfamiliar options.
- For an explicitly requested existing Chrome tab, use the connected browser
  tools and their current documentation. Preserve that tab's session.
- For an explicitly requested built-in Browser, use the available browser tools
  and the documentation returned by their runtime. Do not assume the API from
  an older plugin example applies to the current tool.
- If the default Chrome path is unavailable, use the built-in Browser when it
  can perform the required checks and explain the concrete fallback reason.
  If the user requires a particular browser, report the blocker instead of
  substituting another one. A permission denial is not a reason to bypass it
  through another browser.

The desktop app's built-in Browser is included automatically. Determine its
availability from the callable tools and runtime inventory, not from whether a
plugin or skill named `browser` appears in the catalog. Do not recommend
installing a Browser plugin merely because that skill name is absent.

The visible-window default is for interactive desktop QA. Preserve headless
execution for repository automated test commands and CI unless the task calls
for an interactive inspection.

## Own the session

Resolve the frontend/admin URL and intended API from the current runtime. Reuse
this task's browser session when it already serves the right checkout; otherwise
create a distinct named session. Do not reuse or close another chat's browser
session, or point a fresh QA session at the user's personal Chrome profile.

Record the browser/channel, control method, session name, URL and viewport in
the verification evidence. Keep credentials and tokens out of logs and
screenshots. Stop only temporary browser sessions owned by this task when they
are no longer needed.

## Start local QA with WebMCP

At the start of local development UI verification, check for FoodDiary WebMCP
and use its published tools when they cover scenario preparation or state
inspection. Read [WebMCP connection and bug example](references/webmcp.md) for
the connection steps, tool selection and a complete example. Fetch the actual
tool catalog, then read screen context and available capabilities before
choosing inputs. If unavailable, record the concrete reason and continue the
supported browser checks; do not silently omit discovery.

Chrome remains the default for interactive verification. When its control
method lacks WebMCP, use a task-owned built-in Browser helper tab against the
same local runtime and intended test account. Preserve an explicit browser or
existing-tab requirement. Record both browsers and their respective checks;
unsaved forms and live screen context belong to the tab where they were opened.
A helper's draft does not prepare the Chrome tab. When Chrome is the verification
target, reproduce its interaction there independently.

Use WebMCP to navigate to preconditions, inspect published filters/loading/errors,
find existing catalog items or prepare clean unsaved editors. Exercise the
specific control under test through the real UI, then compare the rendered
result with fresh context. Saving, deletion, keyboard/focus behavior, visual
layout and persistence checks use their normal UI or existing audit workflow.
Running `test:ci:webmcp` alone is library verification, not use of the page tools.

## Verify the requested outcome

For a focused change, reproduce the failing interaction, make the bounded fix,
and recheck its observable result with a current DOM snapshot or screenshot.
Check page identity, meaningful rendered content, framework error overlays,
relevant console errors, and the requested keyboard or viewport variants.
Report checks that the chosen runtime cannot perform as untested.

For a broad user/admin audit, follow
[fooddiary-journey-audit](../fooddiary-journey-audit/SKILL.md) for scenario coverage,
owned fixtures, network-failure probes and cleanup. Chrome through Playwright
supports that repeatable pass; opening its first page does not complete it.

This repository browser choice applies alongside generic frontend plugin
guidance. Continue to use that guidance for relevant QA checks, without replacing
the user's Chrome preference or requiring a separately installed Browser plugin.
