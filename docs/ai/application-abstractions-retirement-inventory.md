# Application abstractions retirement inventory

## Decision

The physical `FoodDiary.Application.Abstractions` project is retired. Its broad dependency hub is replaced by small shared technical contract projects and existing module-owned contract projects. Existing CLR namespaces are intentionally preserved so this is a source-compatible coordinated rebuild, not a binary-forwarding layer.

## New shared contract owners

| Project | Responsibility | Production C# files |
| --- | --- | ---: |
| `Shared/FoodDiary.Application.Contracts` | mediator request contracts, transaction/unit-of-work seams, shared result/error helpers, paging and generic validation | 18 |
| `Shared/FoodDiary.Audit.Contracts` | structured audit write/read contracts and read model | 4 |
| `Shared/FoodDiary.Authentication.Contracts` | host-neutral administrative SSO code storage contract | 1 |
| `Shared/FoodDiary.Email.Contracts` | email message/options, transport, outbox and telemetry contracts | 6 |
| `Shared/FoodDiary.Nutrition.Contracts` | cross-module manual nutrition limits | 1 |
| `Shared/FoodDiary.Outbox.Management.Contracts` | dead-letter inspection and replay contracts/models | 3 |

`Shared/FoodDiary.Outbox.Abstractions` remains the minimal message-record contract and is not merged with operational outbox management.

## Module-owned relocations

- `UserIdParser` is owned by `Modules/Users/Contracts/Common/Validation`.
- `CurrentUserAccessResolver` is owned by `Modules/Users/Contracts/Users/Common`.
- Existing feature ports remain in their module `Application/Abstractions` or `Contracts` projects; consumers reference those owners directly.

## Removed central surface

- `FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj`
- its solution entry, Docker inputs and NuGet lockfile
- the central physical source directory after its 37 C# files were assigned to the owners above

The namespace prefix `FoodDiary.Application.Abstractions` remains valid for moved contracts. Namespace compatibility must not be interpreted as ownership by a retired assembly.

## Dependency policy

- Consumers reference the smallest owning shared or module contract project directly.
- Executable hosts remain composition roots.
- Shared contracts contain no infrastructure/provider implementation.
- The six new shared projects have exact dependency guardrails; the generic application contract project depends only on the shared mediator, result and domain primitive libraries.
- The retired project path and lockfile are prohibited by architecture tests.

## Compatibility and runtime scope

No HTTP route, payload, status code, Swagger contract, database model, migration, provider request, job schedule or application behavior is intentionally changed. All consuming assemblies and hosts must be rebuilt together because no compatibility forwarding assembly is retained.
