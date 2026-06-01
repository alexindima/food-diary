namespace FoodDiary.Infrastructure.Tests.Integration;

[CollectionDefinition("postgres-database")]
public sealed class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture> {
    public const string Name = "postgres-database";
}
