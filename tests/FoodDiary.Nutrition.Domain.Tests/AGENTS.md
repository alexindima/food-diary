# Nutrition domain tests

Own food quality calculation, grade and food unit contracts for the shared Nutrition
Domain project. Follow root tests/AGENTS.md and the Shared/Core test layout.
Preserve moved test assertions. Keep Product/Meal/Recipe aggregate and projection
tests with those owners or existing central mixed suites. Do not reference other
test projects or production aggregate assemblies from this focused suite.

Mark test types with ExcludeFromCodeCoverage and run without collectors.
