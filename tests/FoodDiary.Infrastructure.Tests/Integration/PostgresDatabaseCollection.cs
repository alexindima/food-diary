namespace FoodDiary.Infrastructure.Tests.Integration;

[CollectionDefinition("postgres-database")]
[ExcludeFromCodeCoverage]
public sealed class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture> {
    public const string Name = "postgres-database";
}
