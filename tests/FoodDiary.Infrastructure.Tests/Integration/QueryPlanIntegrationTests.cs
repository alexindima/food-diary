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
public sealed class QueryPlanIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const int SeedCount = 1500;

    [RequiresDockerFact]
    public async Task ProductPagingQuery_UsesCompositeOwnershipIndex() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"products-plan-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var products = Enumerable.Range(0, SeedCount)
            .Select(index => Product.Create(
                user.Id,
                $"Plan Product {index:D4}",
                MeasurementUnit.G,
                100,
                25,
                100,
                10,
                5,
                20,
                3,
                0,
                visibility: Visibility.Private))
            .ToArray();

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        await AnalyzeTableAsync(context, QueryPlanTable.Products);

        var plan = await ExplainAnalyzeAsync(
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
            $"Expected product paging plan to use IX_Products_UserId_CreatedOnUtc, but got:{Environment.NewLine}{plan}");
    }

    [RequiresDockerFact]
    public async Task RecipePagingQuery_UsesCompositeOwnershipIndex() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipes-plan-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var recipes = Enumerable.Range(0, SeedCount)
            .Select(index => Recipe.Create(
                user.Id,
                $"Plan Recipe {index:D4}",
                servings: 2,
                description: $"Description {index:D4}",
                visibility: Visibility.Private))
            .ToArray();

        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();
        await AnalyzeTableAsync(context, QueryPlanTable.Recipes);

        var plan = await ExplainAnalyzeAsync(
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
            $"Expected recipe paging plan to use IX_Recipes_UserId_CreatedOnUtc, but got:{Environment.NewLine}{plan}");
    }

    [RequiresDockerFact]
    public async Task MealPagingQuery_WithDateRange_UsesCompositeOwnershipDateIndex() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-plan-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mealTypes = new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack };
        var meals = Enumerable.Range(0, SeedCount)
            .Select(index => Meal.Create(
                user.Id,
                startDate.AddDays(index),
                mealTypes[index % mealTypes.Length],
                comment: $"Plan Meal {index:D4}"))
            .ToArray();

        context.Meals.AddRange(meals);
        await context.SaveChangesAsync();
        await AnalyzeTableAsync(context, QueryPlanTable.Meals);

        var plan = await ExplainAnalyzeAsync(
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
            $"Expected meal paging plan to use IX_Meals_UserId_Date_CreatedOnUtc, but got:{Environment.NewLine}{plan}");
    }

    private static async Task<JsonDocument> ExplainAnalyzeAsync(
        FoodDiaryDbContext context,
        string sql,
        params NpgsqlParameter[] parameters) {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) {sql}";
        command.Parameters.AddRange(parameters);

        var raw = Convert.ToString(await command.ExecuteScalarAsync());
        Assert.False(string.IsNullOrWhiteSpace(raw));
        return JsonDocument.Parse(raw!);
    }

    private static async Task AnalyzeTableAsync(FoodDiaryDbContext context, QueryPlanTable table) {
        var sql = table switch {
            QueryPlanTable.Products => "ANALYZE \"Products\"",
            QueryPlanTable.Recipes => "ANALYZE \"Recipes\"",
            QueryPlanTable.Meals => "ANALYZE \"Meals\"",
            _ => throw new ArgumentOutOfRangeException(nameof(table), table, "Unsupported table for query-plan analysis.")
        };

        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private static bool ContainsIndexName(JsonDocument plan, string indexName) {
        return ContainsIndexName(plan.RootElement, indexName);
    }

    private static bool ContainsIndexName(JsonElement element, string indexName) {
        switch (element.ValueKind) {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject()) {
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
                foreach (var item in element.EnumerateArray()) {
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
        Products,
        Recipes,
        Meals
    }
}
