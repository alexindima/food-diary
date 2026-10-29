using System.Text.Json.Nodes;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserStatePersistenceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string ProfileValues = """
        {"DashboardLayout":{"web":["meals","stats"]},"Language":"ru","Theme":"dark","UiStyle":"modern",
         "SurfaceStyle":"soft","PushNotificationsEnabled":true,"FastingPushNotificationsEnabled":false,
         "SocialPushNotificationsEnabled":false,"FastingCheckInReminderHours":16,"FastingCheckInFollowUpReminderHours":32,
         "TimeZoneId":"Asia/Tbilisi","BirthDate":"1996-01-02T00:00:00+00:00","Gender":"Male",
         "WeightKg":82.5,"DesiredWeightKg":75,"DesiredWaistCm":80,"HeightCm":180,"ActivityLevel":"High",
         "DailyCalorieTarget":2100,"ProteinTarget":120,"FatTarget":70,"CarbTarget":220,"FiberTarget":30,
         "GoalRevision":0,"StepGoal":10000,"WaterGoal":2.5,"HydrationGoal":2.8,"CalorieCyclingEnabled":true,"MondayCalories":2001,
         "TuesdayCalories":2002,"WednesdayCalories":2003,"ThursdayCalories":2004,"FridayCalories":2005,
         "SaturdayCalories":2006,"SundayCalories":2007}
        """;

    [RequiresDockerFact]
    public async Task OwnedProfiles_PreserveEveryMappedValueAndLatestChanges() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        context.Users.Add(User.Create("state-roundtrip@example.com", "hash"));
        await context.SaveChangesAsync();
        JsonObject expected = Assert.IsType<JsonObject>(JsonNode.Parse(ProfileValues));
        foreach (string table in new[] { "UserPreferences", "UserNutritionProfiles" }) {
            string[] columns = await context.Database.SqlQuery<string>($"""
                SELECT column_name::text AS "Value" FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = {table}
                AND column_name NOT IN ('Id', 'CreatedOnUtc', 'ModifiedOnUtc') ORDER BY ordinal_position
                """).ToArrayAsync();
            string names = string.Join(", ", columns.Select(column => $"\"{column}\""));
            // Table/column names come only from the two fixed profile tables above.
            string seed = $"UPDATE \"{table}\" SET ({names}) = (SELECT {names} FROM jsonb_populate_record(NULL::\"{table}\", cast({{0}} AS jsonb)))";
            await context.Database.ExecuteSqlRawAsync(seed, ProfileValues);
        }
        context.ChangeTracker.Clear();
        string actual = await context.Database.SqlQuery<string>($"""
            SELECT ((to_jsonb(p) || to_jsonb(n)) - 'Id' - 'CreatedOnUtc' - 'ModifiedOnUtc')::text AS "Value"
            FROM "UserPreferences" p JOIN "UserNutritionProfiles" n ON n."Id" = p."Id"
            """).SingleAsync();
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(actual)), actual);
        await context.Database.ExecuteSqlRawAsync("UPDATE \"UserPreferences\" SET \"Theme\" = 'leaf'; UPDATE \"UserNutritionProfiles\" SET \"DailyCalorieTarget\" = 2300;");
        expected["Theme"] = "leaf";
        expected["DailyCalorieTarget"] = 2300;
        context.ChangeTracker.Clear();
        Assert.Equal("leaf", await context.Users.Select(user => user.Preferences.Theme).SingleAsync());
        Assert.Equal(2300, await context.Users.Select(user => user.NutritionProfile.DailyCalorieTarget).SingleAsync());
    }
}
