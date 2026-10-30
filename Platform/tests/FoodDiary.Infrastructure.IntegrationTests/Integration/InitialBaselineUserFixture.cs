using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
internal static class InitialBaselineUserFixture {
    public static async Task<UserId> InsertAsync(FoodDiaryDbContext context, string email, string language = "en") {
        var userId = UserId.New();
        DateTime createdAt = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
        // Freeze the baseline shape: current User.Create also inserts newer goal-history state.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Users" ("Id", "Email", "Password", "TelegramLinkVerifiedForRecovery", "CreatedOnUtc")
            VALUES ({userId.Value}, {email}, 'test-hash', FALSE, {createdAt});
            INSERT INTO "UserPreferences" ("Id", "Language", "CreatedOnUtc")
            VALUES ({userId.Value}, {language}, {createdAt});
            INSERT INTO "UserNutritionProfiles" ("Id", "ActivityLevel", "CalorieCyclingEnabled", "CreatedOnUtc")
            VALUES ({userId.Value}, 'Moderate', FALSE, {createdAt});
            """);
        return userId;
    }
}
