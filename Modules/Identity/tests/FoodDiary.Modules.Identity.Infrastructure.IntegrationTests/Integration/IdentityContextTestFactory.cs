using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
internal static class IdentityContextTestFactory {
    public static async Task<IdentityDbContext> CreateAsync(PostgresDatabaseFixture fixture) {
        await using FoodDiaryDbContext database = await fixture.CreateDbContextAsync();
        return Create(database.Database.GetConnectionString()!);
    }

    public static IdentityDbContext Create(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
}
