using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Modules.Identity.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Identity.PersistenceModel.Authentication;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
internal static class IdentityContextTestFactory {
    public static async Task<IdentityDbContext> CreateAsync(PostgresDatabaseFixture fixture) {
        await using FoodDiaryDbContext database = await fixture.CreateDbContextAsync();
        await database.Set<TelegramAssertionPolicy>().ExecuteUpdateAsync(update => update.SetProperty(policy => policy.NotBeforeUtc, DateTime.UnixEpoch));
        return Create(database.Database.GetConnectionString()!);
    }

    public static IdentityDbContext Create(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
}
