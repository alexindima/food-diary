# Legacy domain test suite

This directory contains only `FoodDiary.Domain.Tests`, the remaining mixed domain
suite. New tests belong with the production component they protect.

See [test ownership](../docs/architecture/TEST_PROJECT_OWNERSHIP.md),
[testing strategy](../docs/TESTING_STRATEGY.md), and
[common test rules](../Tooling/Testing/AGENTS.md) for current paths and commands.

Run the remaining suite with:

```powershell
dotnet test tests/FoodDiary.Domain.Tests/FoodDiary.Domain.Tests.csproj
```
