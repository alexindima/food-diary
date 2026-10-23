namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

// Latency budgets require a quiet host; other collections retain their parallel execution.
[CollectionDefinition(Name, DisableParallelization = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgresPerformanceCollection {
    public const string Name = "PostgreSQL API performance";
}
