# Cycles enum ownership

Source baseline: `7a9a3fad8bb937583f406fd55e7fe5c5d85b8cb5`.

`BleedingType`, `CycleSymptomCategory`, and `OvulationTestResult` belong to
`Modules/Cycles/Domain/Enums`. Their `FoodDiary.Domain.Enums` namespaces, member
names, and numeric values are unchanged, including `CycleSymptomCategory.Other = 99`.

Cycles Application, Application.Abstractions and PersistenceModel already reference
Cycles Domain. Direct enum consumers in Cycles Infrastructure, Export Application,
Presentation and their test projects now reference the same owner explicitly.
Read capabilities, aggregate boundaries, EF mappings, historical migrations and
HTTP models retain their existing owners and behavior. No reverse reference from
central Domain is introduced. No solution or Docker source-list change is needed.

The assembly identity changes, requiring a coordinated rebuild of consumers.
Preserving CLR namespaces is not a binary compatibility promise. CycleId and all
other residual central Domain types are outside this change.

Verification evidence belongs to `.artifacts/cycles-domain-enums-evidence` and the
named Wiki workspace `.artifacts/llm-wiki/tasks/cycles-domain-enums-extraction`.
Enum contract tests freeze member names and numbers; architecture tests assert the
new assembly owner, source location and absence of a reverse project reference.
PostgreSQL, HTTP and EF model checks must execute before delivery; discovery results
and generated receipts do not establish test coverage.
