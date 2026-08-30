# Cycles module extraction: ownership and Wiki findings

## Verified ownership inventory

The extraction was based on current source, project references, tests, EF metadata, DI registrations, Docker build inputs, and host consumers rather than on Wiki mappings alone.

- `Modules/Cycles/Application` owns cycle commands, queries, validation, prediction/calculation services, and the existing `ICycleReadService` implementation. Its assembly name remains `FoodDiary.Application.Cycles` for compatibility.
- `Modules/Cycles/Application/Abstractions` owns `ICycleRepository`, cycle read models, and cycle errors. The central application-abstractions assembly retains a compatibility facade for the public error members and therefore references this project.
- `Modules/Cycles/Domain` owns the cycle aggregate, entries, IDs, and enums. It intentionally continues to use the central `UserId` and the internal central `DomainGuard`; the latter is exposed only through an explicit friend-assembly seam.
- `Modules/Cycles/Infrastructure/Model` owns the EF configurations and model-registration extension. `FoodDiaryDbContext`, its `DbSet` properties, historical migrations, and the model snapshot remain central so migration ownership and runtime identity do not change.
- `Modules/Cycles/Infrastructure` owns repository implementation and module registration. Web API and Initializer remain composition roots and call the module facade.
- Focused application, domain, time-normalization, and DI registration tests now live under `Modules/Cycles/tests`.
- Dashboard, Export, and Presentation are read consumers. Central user cleanup remains the database-lifecycle owner for deletion across aggregates. No independent Contracts project was justified by the current dependency graph.

The EF relationship from cycle profiles to users is now configured as a unidirectional relationship. This removes central `User.Cycles` ownership without changing the `UserId` foreign key, cascade behavior, schema, table names, or historical migrations.

## Wiki evaluation

The adaptive workflow helped by identifying the earlier application-only extraction commit, classifying the work as an architectural change, surfacing the Cycles/Dashboard/Export neighborhood in the brief, and flagging cycle and fertility fields as health-sensitive data in the privacy report.

Verified gaps were:

- `ownership` returned no direct or downstream modules, although current project references and source showed Dashboard, Export, Presentation, host, DI, and persistence seams.
- `test-plan` found no focused test files even though cycle tests existed in central application/domain test projects.
- `design` continued to report a generic unresolved boundary after the intended compatibility seams were stated.
- TypeScript-based discovery was unavailable and fell back to JSON; the fallback remained usable but less precise.
- Governed task continuity still persisted a SQLite requirement after the reported JSON fallback, so `task-refresh -CompiledIndexSource Json` failed until frontend prerequisites were installed and `graph-build` completed.
- The verification plan emitted two check IDs for the identical full infrastructure-integration command and did not coalesce them, causing two successful 123-test PostgreSQL runs (352 s and 399 s).
- The pre-extraction backend module map described Cycles as an application project with central domain and persistence folders, so every mapping was verified before edits.

No module-specific workaround was added to Wiki generators. The durable module map and ownership documentation were corrected from the verified implementation, and generated Wiki artifacts were refreshed only after the code and project graph stabilized.

`journeys` and `topology` were not required: HTTP behavior, payloads, routes, external providers, workers, and schedules did not change. `dependencies` and `rollout` were checked because the project graph, Docker restore graph, and composition roots changed; no deployment ordering or data migration was introduced.
