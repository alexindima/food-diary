namespace FoodDiary.Modules.Fasting.Infrastructure.Tests;

[CollectionDefinition("postgres-database")]
[ExcludeFromCodeCoverage]
public sealed class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture> {
    public const string Name = "postgres-database";
}
