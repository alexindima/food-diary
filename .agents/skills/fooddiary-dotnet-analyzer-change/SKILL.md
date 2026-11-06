---
name: fooddiary-dotnet-analyzer-change
description: Configure or diagnose FoodDiary .NET analyzer rules and Rider/build diagnostic differences, including Roslyn, Meziantou and Roslynator. Use for analyzer settings and diagnostic-driven fixes, not ordinary backend feature work or coverage measurement.
---

# FoodDiary .NET analyzer changes

Read the current root and owning project/test guides. Locate the diagnostic ID,
its provider and configured severity in `.editorconfig`, `Directory.Build.props`
and `Directory.Packages.props` before adding a package or custom rule. Resolve
the current installed analyzer version and inspect its official documentation or
source when behavior is uncertain; Rider's suggestion name alone is insufficient.

Reproduce the smallest current-source example in the owning project. Compare
actual build output with the IDE inspection: language/code-style settings,
compiler diagnostics, analyzer warnings and Rider-only inspections have distinct
execution boundaries. Preserve namespace/layout, nullable and K&R defaults.

For cancellation diagnostics, distinguish an available CancellationToken overload
from a token actually available in scope. Exercise extension methods, optional
parameters and overload resolution using existing Roslyn-based fixtures where
present. Do not use text matching to infer callable overloads.

For primary constructors, FoodDiary already configures `IDE0290` and primary
constructor preference. The observed parameterless-constructor Rider suggestion
was outside IDE0290's supported transformation; verify the current rule before
changing severity or duplicating it. Prefer the smallest behavior-preserving
edit, or state that an IDE-only inspection has no equivalent build diagnostic.

Run focused analyzer/architecture fixtures and the owning build. For rule changes,
include a positive example that produces the diagnostic and a negative example
that must stay clean. If IDE parity is part of the request, verify the current
Rider result separately; a passing CLI build does not prove editor highlighting.
Keep settings in their existing shared owner. Use the existing delivery skill
only when Git publication is authorized.
