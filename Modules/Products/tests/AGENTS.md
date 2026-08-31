# Products tests

Products-only application tests live in FoodDiary.Modules.Products.Application.Tests. Mixed Favorites overview composition, shared PostgreSQL/Domain/HTTP/host tests stay central. Preserve assertions, no duplicate test cases, no cross-test-project references; mark helpers with ExcludeFromCodeCoverage.

Products repository PostgreSQL cases live in FoodDiary.Modules.Products.Infrastructure.IntegrationTests. Only the two shared central Postgres fixture sources are linked; they remain central-owned. Run this suite unfiltered in addition to the full central infrastructure integration suite. Keep the 1500-row seed and 250ms budget unchanged. No collector execution.

ProductInvariantTests belongs to FoodDiary.Modules.Products.Domain.Tests and references the central Domain assembly. Its 55 Fact/Theory methods and local helper remain unchanged; shared navigation does not require retaining this focused test file centrally. Mixed domain tests remain central.
