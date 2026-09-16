# Ai module guidelines

Own focused application/domain and mocked provider/persistence unit suites. Use project-and-folder test namespaces; preserve ExcludeFromCodeCoverage and ordinary VSTest execution. Central shared PostgreSQL fixture/suites and mixed Admin/Users/Meals/HTTP/DI suites stay central; execute full Infrastructure.IntegrationTests without filter for this extraction.

Quota/job PostgreSQL tests now exercise AiDbContext after central migration/foreign-row setup. SharedAiContextIntegrationTests verifies common saves, prompt revision rollback, transaction rejoining and independent quota commits through real DI. Prompt cache unit fixtures register the owner context.

Application.Tests also owns the real nested UpsertAiPrompt transaction scenario and AI usage read-model tests extracted from the retired central Application.Tests suite.

Presentation.Tests references the owner's Application.Abstractions directly to verify FoodRecognitionNotifier retries and shutdown against its job-update reader port.
