using NpgsqlTypes;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Initializer;

[ExcludeFromCodeCoverage]
internal static class UsdaDataSeeder {
    private const int DefaultCommandTimeoutSeconds = 120;

    public static Task SeedAsync(
        FoodDiaryDbContext dbContext, string csvDirectory, CancellationToken cancellationToken = default) =>
        SeedCoreAsync(dbContext, csvDirectory, force: false, cancellationToken);

    public static Task ForceSeedAsync(
        FoodDiaryDbContext dbContext, string csvDirectory, CancellationToken cancellationToken = default) =>
        SeedCoreAsync(dbContext, csvDirectory, force: true, cancellationToken);

    private static async Task SeedCoreAsync(
        FoodDiaryDbContext dbContext, string csvDirectory, bool force, CancellationToken cancellationToken) {
        if (!Directory.Exists(csvDirectory)) {
            throw new DirectoryNotFoundException($"USDA CSV directory not found: {csvDirectory}");
        }

        string connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string configured.");
        var settings = new NpgsqlConnectionStringBuilder(connectionString);
        if (!settings.ShouldSerialize("Command Timeout")) {
            settings.CommandTimeout = DefaultCommandTimeoutSeconds;
        }

        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ExecuteNonQueryAsync(connection, transaction,
            "SELECT pg_advisory_xact_lock(hashtextextended('fooddiary:seed-usda', 0))", cancellationToken);

        (bool hasFoods, bool hasNutrients, bool hasDailyReferenceValues) =
            await ReadSeedStateAsync(connection, transaction, cancellationToken);
        // The previous initializer wrote the daily values in a single statement after all CSV imports.
        if (!force && hasFoods && hasDailyReferenceValues) {
            Console.WriteLine("USDA reference data is already seeded. Skipping seed. Use --force to re-seed.");
            return;
        }

        if (force || hasFoods || hasNutrients) {
            Console.WriteLine(force ? "Replacing existing USDA data..." : "Recovering an incomplete USDA seed...");
            await ExecuteNonQueryAsync(connection, transaction, """
                TRUNCATE "UsdaFoodNutrients", "UsdaFoodPortions", "DailyReferenceValues", "UsdaFoods", "UsdaNutrients"
                """, cancellationToken);
        }

        Console.WriteLine("Seeding USDA reference data...");

        long foodCount = await ImportFoodsAsync(connection, csvDirectory, cancellationToken);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Foods: {foodCount:N0} rows"));

        long nutrientCount = await ImportNutrientsAsync(connection, csvDirectory, cancellationToken);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  Nutrients: {nutrientCount:N0} rows"));

        HashSet<int> validFdcIds = await LoadValidFdcIdsAsync(connection, transaction, cancellationToken);
        long foodNutrientCount = await ImportFoodNutrientsAsync(connection, csvDirectory, validFdcIds, cancellationToken);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  FoodNutrients: {foodNutrientCount:N0} rows"));

        long portionCount = await ImportFoodPortionsAsync(connection, csvDirectory, validFdcIds, cancellationToken);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  FoodPortions: {portionCount:N0} rows"));

        await SeedDailyReferenceValuesAsync(connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        Console.WriteLine("USDA seed completed.");
    }

    private static async Task SeedDailyReferenceValuesAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) {
        // FDA Daily Values for adults and children 4+ (standard nutrition label values)
        // Source: https://www.fda.gov/food/nutrition-facts-label/daily-value-nutrition-and-supplement-facts-labels
        const string sql = """
            INSERT INTO "DailyReferenceValues" ("NutrientId", "Value", "Unit", "AgeGroup", "Gender")
            VALUES
                -- Vitamins
                (1106, 900, 'mcg', 'adult', 'all'),
                (1165, 1.2, 'mg', 'adult', 'all'),
                (1166, 1.3, 'mg', 'adult', 'all'),
                (1167, 16, 'mg', 'adult', 'all'),
                (1170, 5, 'mg', 'adult', 'all'),
                (1175, 1.7, 'mg', 'adult', 'all'),
                (1177, 400, 'mcg', 'adult', 'all'),
                (1178, 2.4, 'mcg', 'adult', 'all'),
                (1162, 90, 'mg', 'adult', 'all'),
                (1110, 20, 'mcg', 'adult', 'all'),
                (1109, 15, 'mg', 'adult', 'all'),
                (1185, 120, 'mcg', 'adult', 'all'),
                -- Minerals
                (1087, 1300, 'mg', 'adult', 'all'),
                (1089, 18, 'mg', 'adult', 'all'),
                (1090, 420, 'mg', 'adult', 'all'),
                (1091, 1250, 'mg', 'adult', 'all'),
                (1092, 4700, 'mg', 'adult', 'all'),
                (1093, 2300, 'mg', 'adult', 'all'),
                (1095, 11, 'mg', 'adult', 'all'),
                (1098, 0.9, 'mg', 'adult', 'all'),
                (1101, 2.3, 'mg', 'adult', 'all'),
                (1103, 55, 'mcg', 'adult', 'all')
            ON CONFLICT ("NutrientId", "AgeGroup", "Gender") DO UPDATE SET
                "Value" = EXCLUDED."Value",
                "Unit" = EXCLUDED."Unit"
            """;
        await ExecuteNonQueryAsync(connection, transaction, sql, cancellationToken);
        Console.WriteLine("  DailyReferenceValues: 22 rows (FDA Daily Values)");
    }

    private static async Task<long> ImportFoodsAsync(
        NpgsqlConnection connection, string csvDirectory, CancellationToken cancellationToken) {
        string foodCsvPath = Path.Combine(csvDirectory, "food.csv");
        string categoryCsvPath = Path.Combine(csvDirectory, "food_category.csv");

        if (!File.Exists(foodCsvPath)) {
            throw new FileNotFoundException("food.csv not found in USDA CSV directory.", foodCsvPath);
        }

        // Load food categories if available
        var categories = new Dictionary<int, string>();
        if (File.Exists(categoryCsvPath)) {
            await foreach (string line in UsdaCsvReader.ReadLinesAsync(categoryCsvPath, cancellationToken)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // food_category.csv: id, code, description
                if (fields.Length >= 3 && int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int catId)) {
                    categories[catId] = fields[2];
                }
            }
        }

        long count = 0;
        NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoods" ("FdcId", "Description", "FoodCategoryId", "FoodCategory") FROM STDIN (FORMAT BINARY)""",
            cancellationToken);
        await using (writer) {
            await foreach (string line in UsdaCsvReader.ReadLinesAsync(foodCsvPath, cancellationToken)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // food.csv: fdc_id, data_type, description, food_category_id, publication_date
                if (fields.Length < 4) {
                    continue;
                }

                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fdcId)) {
                    continue;
                }

                // Only import SR Legacy foods
                if (!string.Equals(fields[1], "sr_legacy_food", StringComparison.Ordinal)) {
                    continue;
                }

                int? foodCategoryId = int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int catId2) ? catId2 : null;
                string? foodCategory = foodCategoryId.HasValue && categories.TryGetValue(foodCategoryId.Value, out string? catName)
                    ? catName : null;

                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(fdcId, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(UsdaCsvReader.Truncate(fields[2], 512), NpgsqlDbType.Varchar, cancellationToken);
                if (foodCategoryId.HasValue) {
                    await writer.WriteAsync(foodCategoryId.Value, NpgsqlDbType.Integer, cancellationToken);
                } else {
                    await writer.WriteNullAsync(cancellationToken);
                }
                if (foodCategory is not null) {
                    await writer.WriteAsync(UsdaCsvReader.Truncate(foodCategory, 256), NpgsqlDbType.Varchar, cancellationToken);
                } else {
                    await writer.WriteNullAsync(cancellationToken);
                }
                count++;
            }

            await writer.CompleteAsync(cancellationToken);
            return count;
        }
    }

    private static async Task<long> ImportNutrientsAsync(
        NpgsqlConnection connection, string csvDirectory, CancellationToken cancellationToken) {
        string csvPath = Path.Combine(csvDirectory, "nutrient.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("nutrient.csv not found in USDA CSV directory.", csvPath);
        }

        long count = 0;
        NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaNutrients" ("Id", "Name", "UnitName") FROM STDIN (FORMAT BINARY)""",
            cancellationToken);
        await using (writer) {
            await foreach (string line in UsdaCsvReader.ReadLinesAsync(csvPath, cancellationToken)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // nutrient.csv: id, name, unit_name, nutrient_nbr, rank
                if (fields.Length < 3) {
                    continue;
                }

                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) {
                    continue;
                }

                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(id, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(UsdaCsvReader.Truncate(fields[1], 256), NpgsqlDbType.Varchar, cancellationToken);
                await writer.WriteAsync(UsdaCsvReader.Truncate(fields[2], 32), NpgsqlDbType.Varchar, cancellationToken);
                count++;
            }

            await writer.CompleteAsync(cancellationToken);
            return count;
        }
    }

    private static async Task<long> ImportFoodNutrientsAsync(
        NpgsqlConnection connection, string csvDirectory, HashSet<int> validFdcIds, CancellationToken cancellationToken) {
        string csvPath = Path.Combine(csvDirectory, "food_nutrient.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("food_nutrient.csv not found in USDA CSV directory.", csvPath);
        }

        long count = 0;
        NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoodNutrients" ("Id", "FdcId", "NutrientId", "Amount") FROM STDIN (FORMAT BINARY)""",
            cancellationToken);
        await using (writer) {
            await foreach (string line in UsdaCsvReader.ReadLinesAsync(csvPath, cancellationToken)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // food_nutrient.csv: id, fdc_id, nutrient_id, amount, ...
                if (fields.Length < 4) {
                    continue;
                }

                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) {
                    continue;
                }

                if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fdcId)) {
                    continue;
                }

                if (!int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int nutrientId)) {
                    continue;
                }

                if (!double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double amount)) {
                    continue;
                }

                if (!validFdcIds.Contains(fdcId)) {
                    continue;
                }

                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(id, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(fdcId, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(nutrientId, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(amount, NpgsqlDbType.Double, cancellationToken);
                count++;
            }

            await writer.CompleteAsync(cancellationToken);
            return count;
        }
    }

    private static async Task<long> ImportFoodPortionsAsync(
        NpgsqlConnection connection, string csvDirectory, HashSet<int> validFdcIds, CancellationToken cancellationToken) {
        string csvPath = Path.Combine(csvDirectory, "food_portion.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("food_portion.csv not found in USDA CSV directory.", csvPath);
        }

        long count = 0;
        NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoodPortions" ("Id", "FdcId", "Amount", "MeasureUnitName", "GramWeight", "PortionDescription", "Modifier") FROM STDIN (FORMAT BINARY)""",
            cancellationToken);
        await using (writer) {
            await foreach (string line in UsdaCsvReader.ReadLinesAsync(csvPath, cancellationToken)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // food_portion.csv: id, fdc_id, seq_num, amount, measure_unit_id, portion_description, modifier, gram_weight, ...
                if (fields.Length < 8) {
                    continue;
                }

                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) {
                    continue;
                }

                if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fdcId)) {
                    continue;
                }

                if (!validFdcIds.Contains(fdcId)) {
                    continue;
                }

                if (!double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double amount)) {
                    amount = 1.0;
                }

                string? portionDescription = string.IsNullOrWhiteSpace(fields[5]) ? null : fields[5];
                string? modifier = string.IsNullOrWhiteSpace(fields[6]) ? null : fields[6];

                if (!double.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out double gramWeight)) {
                    continue;
                }

                // measure_unit_name is not in CSV directly; use portion_description as fallback
                string measureUnitName = modifier ?? portionDescription ?? "serving";

                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(id, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(fdcId, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(amount, NpgsqlDbType.Double, cancellationToken);
                await writer.WriteAsync(UsdaCsvReader.Truncate(measureUnitName, 128), NpgsqlDbType.Varchar, cancellationToken);
                await writer.WriteAsync(gramWeight, NpgsqlDbType.Double, cancellationToken);
                if (portionDescription is not null) {
                    await writer.WriteAsync(UsdaCsvReader.Truncate(portionDescription, 256), NpgsqlDbType.Varchar, cancellationToken);
                } else {
                    await writer.WriteNullAsync(cancellationToken);
                }
                if (modifier is not null) {
                    await writer.WriteAsync(UsdaCsvReader.Truncate(modifier, 128), NpgsqlDbType.Varchar, cancellationToken);
                } else {
                    await writer.WriteNullAsync(cancellationToken);
                }
                count++;
            }

            await writer.CompleteAsync(cancellationToken);
            return count;
        }
    }

    private static async Task<HashSet<int>> LoadValidFdcIdsAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) {
        var ids = new HashSet<int>();
        await using var command = new NpgsqlCommand("""SELECT "FdcId" FROM "UsdaFoods" """, connection, transaction);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            ids.Add(reader.GetInt32(0));
        }
        return ids;
    }

    private static async Task<(bool HasFoods, bool HasNutrients, bool HasDailyReferenceValues)> ReadSeedStateAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken) {
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (SELECT 1 FROM "UsdaFoods"),
                   EXISTS (SELECT 1 FROM "UsdaNutrients"),
                   EXISTS (SELECT 1 FROM "DailyReferenceValues")
            """, connection, transaction);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return (reader.GetBoolean(0), reader.GetBoolean(1), reader.GetBoolean(2));
    }

    private static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, CancellationToken cancellationToken) {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

}
