using System.Text.Json.Nodes;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserStateMigrationIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string BeforeSplit = "20261003001656_AddDurableAtomicCommandReceiptsAndPushDeliveryProgress";
    private const string LegacyValues = """
        {"DashboardLayout":{"web":["meals","stats"]},"Language":"ru","Theme":"dark","UiStyle":"modern",
         "SurfaceStyle":"soft","PushNotificationsEnabled":true,"FastingPushNotificationsEnabled":false,
         "SocialPushNotificationsEnabled":false,"FastingCheckInReminderHours":16,"FastingCheckInFollowUpReminderHours":32,
         "TimeZoneId":"Asia/Tbilisi","BirthDate":"1996-01-02T00:00:00+00:00","Gender":"Male",
         "WeightKg":82.5,"DesiredWeightKg":75,"DesiredWaistCm":80,"HeightCm":180,"ActivityLevel":"High",
         "DailyCalorieTarget":2100,"ProteinTarget":120,"FatTarget":70,"CarbTarget":220,"FiberTarget":30,
         "StepGoal":10000,"WaterGoal":2.5,"HydrationGoal":2.8,"CalorieCyclingEnabled":true,"MondayCalories":2001,
         "TuesdayCalories":2002,"WednesdayCalories":2003,"ThursdayCalories":2004,"FridayCalories":2005,
         "SaturdayCalories":2006,"SundayCalories":2007}
        """;

    [RequiresDockerFact]
    public async Task UpgradeAndDowngrade_PreserveEveryMovedValueAndLatestChanges() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connectionString);
        IMigrator migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(BeforeSplit);
        var userId = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Users" ("Id", "Email", "Password", "ActivityLevel", "CreatedOnUtc")
            VALUES ({userId}, 'migration@example.com', 'hash', 'Moderate', {DateTime.UtcNow});
            """);
        JsonObject expected = Assert.IsType<JsonObject>(JsonNode.Parse(LegacyValues));
        string columns = string.Join(", ", expected.Select(field => $"\"{field.Key}\""));
        // Fixed test data only: seed the previous schema, independently of the current EF model.
        string seedSql = $"UPDATE \"Users\" SET ({columns}) = (SELECT {columns} FROM jsonb_populate_record(NULL::\"Users\", cast({{0}} AS jsonb)))";
        await context.Database.ExecuteSqlRawAsync(seedSql, LegacyValues);
        await migrator.MigrateAsync();
        string actual = await context.Database.SqlQuery<string>($"""
            SELECT ((to_jsonb(p) || to_jsonb(n)) - 'Id' - 'CreatedOnUtc' - 'ModifiedOnUtc')::text AS "Value"
            FROM "UserPreferences" p JOIN "UserNutritionProfiles" n ON n."Id" = p."Id"
            """).SingleAsync();
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(actual)), actual);
        await context.Database.ExecuteSqlRawAsync("UPDATE \"UserPreferences\" SET \"Theme\" = 'leaf'; UPDATE \"UserNutritionProfiles\" SET \"DailyCalorieTarget\" = 2300;");
        expected["Theme"] = "leaf";
        expected["DailyCalorieTarget"] = 2300;
        await migrator.MigrateAsync(BeforeSplit);
        string restoreSql = $"SELECT jsonb_build_object({string.Join(", ", expected.Select(field => $"'{field.Key}', \"{field.Key}\""))})::text AS \"Value\" FROM \"Users\"";
        string restored = await context.Database.SqlQueryRaw<string>(restoreSql).SingleAsync();
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(restored)), restored);
        await migrator.MigrateAsync();
        Assert.Equal("leaf", await context.Users.Select(user => user.Preferences.Theme).SingleAsync());
        Assert.Equal(2300, await context.Users.Select(user => user.NutritionProfile.DailyCalorieTarget).SingleAsync());
    }
}
