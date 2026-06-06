using System.Globalization;
using System.Text.Json;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class QueryPlanIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const int SeedCount = 1500;
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() {
        WriteIndented = true,
    };

    [RequiresDockerFact]
    public async Task ProductPagingQuery_UsesCompositeOwnershipIndex() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"products-plan-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        Product[] products = [.. Enumerable.Range(0, SeedCount)
            .Select(index => Product.Create(
                user.Id,
                string.Create(CultureInfo.InvariantCulture, $"Plan Product {index:D4}"),
                MeasurementUnit.G,
                100,
                25,
                100,
                10,
                5,
                20,
                3,
                0,
                visibility: Visibility.Private))];

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        await AnalyzeTableAsync(context, QueryPlanTable.Products);

        JsonDocument plan = await ExplainAnalyzeAsync(
            context,
            """
            SELECT p."Id", p."CreatedOnUtc"
            FROM "Products" AS p
            WHERE p."UserId" = @userId
            ORDER BY p."CreatedOnUtc" DESC
            LIMIT 25
            """,
            new NpgsqlParameter<Guid>("userId", user.Id.Value));

        Assert.True(
            ContainsIndexName(plan, "IX_Products_UserId_CreatedOnUtc"),
            $"Expected product paging plan to use IX_Products_UserId_CreatedOnUtc, but got:{Environment.NewLine}{FormatPlan(plan)}");
    }

    [RequiresDockerFact]
    public async Task RecipePagingQuery_UsesCompositeOwnershipIndex() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipes-plan-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        Recipe[] recipes = [.. Enumerable.Range(0, SeedCount)
            .Select(index => Recipe.Create(
                user.Id,
                string.Create(CultureInfo.InvariantCulture, $"Plan Recipe {index:D4}"),
                servings: 2,
                description: string.Create(CultureInfo.InvariantCulture, $"Description {index:D4}"),
                visibility: Visibility.Private))];

        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();
        await AnalyzeTableAsync(context, QueryPlanTable.Recipes);

        JsonDocument plan = await ExplainAnalyzeAsync(
            context,
            """
            SELECT r."Id", r."CreatedOnUtc"
            FROM "Recipes" AS r
            WHERE r."UserId" = @userId
            ORDER BY r."CreatedOnUtc" DESC
            LIMIT 25
            """,
            new NpgsqlParameter<Guid>("userId", user.Id.Value));

        Assert.True(
            ContainsIndexName(plan, "IX_Recipes_UserId_CreatedOnUtc"),
            $"Expected recipe paging plan to use IX_Recipes_UserId_CreatedOnUtc, but got:{Environment.NewLine}{FormatPlan(plan)}");
    }

    [RequiresDockerFact]
    public async Task ProductSearchQuery_UsesTrigramNameIndex() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        User[] users = [.. Enumerable.Range(0, 12).Select(index => User.Create(string.Create(CultureInfo.InvariantCulture, $"products-search-plan-{index}-{Guid.NewGuid():N}@example.com"), "hash"))];

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        Product[] products = [.. users.SelectMany((user, userIndex) =>
                Enumerable.Range(0, SeedCount)
                    .Select(index => Product.Create(
                        user.Id,
                        userIndex == users.Length / 2 && index == SeedCount / 2
                            ? "Needle Cocoa Product"
                            : string.Create(CultureInfo.InvariantCulture, $"Background Product {userIndex:D2}-{index:D4}"),
                        MeasurementUnit.G,
                        100,
                        25,
                        100,
                        10,
                        5,
                        20,
                        3,
                        0,
                        visibility: Visibility.Private)))];

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        await AnalyzeTableAsync(context, QueryPlanTable.Products);

        JsonDocument plan = await ExplainAnalyzeAsync(
            context,
            """
            SELECT p."Id", p."CreatedOnUtc"
            FROM "Products" AS p
            WHERE p."Name" ILIKE @search
            LIMIT 25
            """,
            disableSequentialScan: true,
            new NpgsqlParameter<string>("search", "%Needle%"));

        Assert.True(
            ContainsIndexName(plan, "IX_Products_Name"),
            $"Expected product search plan to use trigram index IX_Products_Name, but got:{Environment.NewLine}{FormatPlan(plan)}");
    }

    [RequiresDockerFact]
    public async Task RecipeSearchQuery_UsesTrigramNameIndex() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        User[] users = [.. Enumerable.Range(0, 12).Select(index => User.Create(string.Create(CultureInfo.InvariantCulture, $"recipes-search-plan-{index}-{Guid.NewGuid():N}@example.com"), "hash"))];

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        Recipe[] recipes = [.. users.SelectMany((user, userIndex) =>
                Enumerable.Range(0, SeedCount)
                    .Select(index => Recipe.Create(
                        user.Id,
                        userIndex == users.Length / 2 && index == SeedCount / 2
                            ? "Needle Soup Recipe"
                            : string.Create(CultureInfo.InvariantCulture, $"Background Recipe {userIndex:D2}-{index:D4}"),
                        servings: 2,
                        description: string.Create(CultureInfo.InvariantCulture, $"Description {userIndex:D2}-{index:D4}"),
                        visibility: Visibility.Private)))];

        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();
        await AnalyzeTableAsync(context, QueryPlanTable.Recipes);

        JsonDocument plan = await ExplainAnalyzeAsync(
            context,
            """
            SELECT r."Id", r."CreatedOnUtc"
            FROM "Recipes" AS r
            WHERE r."Name" ILIKE @search
            LIMIT 25
            """,
            disableSequentialScan: true,
            new NpgsqlParameter<string>("search", "%Needle%"));

        Assert.True(
            ContainsIndexName(plan, "IX_Recipes_Name"),
            $"Expected recipe search plan to use trigram index IX_Recipes_Name, but got:{Environment.NewLine}{FormatPlan(plan)}");
    }

    [RequiresDockerFact]
    public async Task MealPagingQuery_WithDateRange_UsesCompositeOwnershipDateIndex() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-plan-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        MealType[] mealTypes = [MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack];
        Meal[] meals = [.. Enumerable.Range(0, SeedCount)
            .Select(index => Meal.Create(
                user.Id,
                startDate.AddDays(index),
                mealTypes[index % mealTypes.Length],
                comment: string.Create(CultureInfo.InvariantCulture, $"Plan Meal {index:D4}")))];

        context.Meals.AddRange(meals);
        await context.SaveChangesAsync();
        await AnalyzeTableAsync(context, QueryPlanTable.Meals);

        JsonDocument plan = await ExplainAnalyzeAsync(
            context,
            """
            SELECT m."Id", m."Date", m."CreatedOnUtc"
            FROM "Meals" AS m
            WHERE m."UserId" = @userId
              AND m."Date" >= @dateFrom
              AND m."Date" <= @dateTo
            ORDER BY m."Date" DESC, m."CreatedOnUtc" DESC
            LIMIT 25
            """,
            new NpgsqlParameter<Guid>("userId", user.Id.Value),
            new NpgsqlParameter<DateTime>("dateFrom", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
            new NpgsqlParameter<DateTime>("dateTo", new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)));

        Assert.True(
            ContainsIndexName(plan, "IX_Meals_UserId_Date_CreatedOnUtc"),
            $"Expected meal paging plan to use IX_Meals_UserId_Date_CreatedOnUtc, but got:{Environment.NewLine}{FormatPlan(plan)}");
    }

    private static async Task<JsonDocument> ExplainAnalyzeAsync(
        FoodDiaryDbContext context,
        string sql,
        params NpgsqlParameter[] parameters) {
        return await ExplainAnalyzeAsync(context, sql, disableSequentialScan: false, parameters).ConfigureAwait(false);
    }

    private static async Task<JsonDocument> ExplainAnalyzeAsync(
        FoodDiaryDbContext context,
        string sql,
        bool disableSequentialScan = false,
        params NpgsqlParameter[] parameters) {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        NpgsqlCommand command = connection.CreateCommand();
        await using (command.ConfigureAwait(false)) {
            command.CommandText = $"EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) {sql}";
            if (disableSequentialScan) {
                command.CommandText = $"SET LOCAL enable_seqscan = off; {command.CommandText}";
            }

            command.Parameters.AddRange(parameters);

            string? raw = Convert.ToString(await command.ExecuteScalarAsync().ConfigureAwait(false));
            Assert.False(string.IsNullOrWhiteSpace(raw));
            return JsonDocument.Parse(raw!);
        }
    }

    private static async Task AnalyzeTableAsync(FoodDiaryDbContext context, QueryPlanTable table) {
        string sql = table switch {
            QueryPlanTable.Products => "ANALYZE \"Products\"",
            QueryPlanTable.Recipes => "ANALYZE \"Recipes\"",
            QueryPlanTable.Meals => "ANALYZE \"Meals\"",
            _ => throw new ArgumentOutOfRangeException(nameof(table), table, "Unsupported table for query-plan analysis."),
        };

        await context.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);
    }

    private static bool ContainsIndexName(JsonDocument plan, string indexName) {
        return ContainsIndexName(plan.RootElement, indexName);
    }

    private static string FormatPlan(JsonDocument plan) {
        return JsonSerializer.Serialize(plan.RootElement, IndentedJsonOptions);
    }

    private static bool ContainsIndexName(JsonElement element, string indexName) {
        switch (element.ValueKind) {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject()) {
                    if (property.NameEquals("Index Name")
                        && string.Equals(property.Value.GetString(), indexName, StringComparison.Ordinal)) {
                        return true;
                    }

                    if (ContainsIndexName(property.Value, indexName)) {
                        return true;
                    }
                }

                return false;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray()) {
                    if (ContainsIndexName(item, indexName)) {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    private enum QueryPlanTable {
        Products = 0,
        Recipes = 1,
        Meals = 2,
    }
}
