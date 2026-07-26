---
id: system.repository-catalog
kind: system
status: current
sources:
  - .llm-wiki/tools/Build-LlmWikiCatalog.ps1
  - Directory.Build.props
  - Directory.Packages.props
  - FoodDiary.Web.Client/angular.json
  - docs/architecture/module-dependencies.json
---

# Repository Catalog

[`repository-catalog.json`](../generated/repository-catalog.json) is the
deterministic machine-readable inventory used by agents for repository
discovery.

It contains:

- .NET projects, target frameworks, project references, and package references;
- the explicit list of test projects;
- Angular workspace projects and build targets;
- application modules and their declared dependencies;
- controllers and literal attribute-routed HTTP endpoints;
- repository instruction guides and long-form documentation pages.

## Limitations

The HTTP inventory extracts literal ASP.NET Core `[Route]` and `[Http*]`
attributes. It is a navigation aid, not an API contract. Runtime conventions,
inherited routes, dynamically composed routes, payloads, response codes, and
Swagger behavior must be verified in presentation code and contract snapshots.

## Maintenance

Regenerate:

```powershell
./.llm-wiki/tools/Build-LlmWikiCatalog.ps1
```

Verify reproducibility:

```powershell
./.llm-wiki/tools/Build-LlmWikiCatalog.ps1 -Check
```

CI runs check mode and fails when structural repository changes are not
reflected in the committed catalog.
