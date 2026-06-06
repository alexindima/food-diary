using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Initializer;

[ExcludeFromCodeCoverage]
internal static class UsdaDataSeeder {
    public static async Task SeedAsync(FoodDiaryDbContext dbContext, string csvDirectory) {
        if (!Directory.Exists(csvDirectory)) {
            throw new DirectoryNotFoundException($"USDA CSV directory not found: {csvDirectory}");
        }

        string connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string configured.");

        var connection = new NpgsqlConnection(connectionString);
        await using (connection.ConfigureAwait(false)) {
            await connection.OpenAsync().ConfigureAwait(false);

            long existingCount = await CountRowsAsync(connection, "\"UsdaFoods\"").ConfigureAwait(false);
            if (existingCount > 0) {
                Console.WriteLine($"UsdaFoods already has {existingCount} rows. Skipping seed. Use --force to re-seed.");
                return;
            }

            Console.WriteLine("Seeding USDA reference data...");

            long foodCount = await ImportFoodsAsync(connection, csvDirectory).ConfigureAwait(false);
            Console.WriteLine($"  Foods: {foodCount:N0} rows");

            long nutrientCount = await ImportNutrientsAsync(connection, csvDirectory).ConfigureAwait(false);
            Console.WriteLine($"  Nutrients: {nutrientCount:N0} rows");

            long foodNutrientCount = await ImportFoodNutrientsAsync(connection, csvDirectory).ConfigureAwait(false);
            Console.WriteLine($"  FoodNutrients: {foodNutrientCount:N0} rows");

            long portionCount = await ImportFoodPortionsAsync(connection, csvDirectory).ConfigureAwait(false);
            Console.WriteLine($"  FoodPortions: {portionCount:N0} rows");

            await SeedDailyReferenceValuesAsync(connection).ConfigureAwait(false);

            Console.WriteLine("USDA seed completed.");
        }
    }

    public static async Task ForceSeedAsync(FoodDiaryDbContext dbContext, string csvDirectory) {
        if (!Directory.Exists(csvDirectory)) {
            throw new DirectoryNotFoundException($"USDA CSV directory not found: {csvDirectory}");
        }

        string connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string configured.");

        var connection = new NpgsqlConnection(connectionString);
        await using (connection.ConfigureAwait(false)) {
            await connection.OpenAsync().ConfigureAwait(false);

            Console.WriteLine("Clearing existing USDA data...");
            await ExecuteNonQueryAsync(connection, """
            TRUNCATE "UsdaFoodNutrients", "UsdaFoodPortions", "UsdaFoods", "UsdaNutrients" CASCADE
            """).ConfigureAwait(false);

            Console.WriteLine("Seeding USDA reference data...");

            long foodCount = await ImportFoodsAsync(connection, csvDirectory).ConfigureAwait(false);
            Console.WriteLine($"  Foods: {foodCount:N0} rows");

            long nutrientCount = await ImportNutrientsAsync(connection, csvDirectory).ConfigureAwait(false);
            Console.WriteLine($"  Nutrients: {nutrientCount:N0} rows");

            long foodNutrientCount = await ImportFoodNutrientsAsync(connection, csvDirectory).ConfigureAwait(false);
            Console.WriteLine($"  FoodNutrients: {foodNutrientCount:N0} rows");

            long portionCount = await ImportFoodPortionsAsync(connection, csvDirectory).ConfigureAwait(false);
            Console.WriteLine($"  FoodPortions: {portionCount:N0} rows");

            await SeedDailyReferenceValuesAsync(connection).ConfigureAwait(false);

            Console.WriteLine("USDA seed completed.");
        }
    }

    private static async Task SeedDailyReferenceValuesAsync(NpgsqlConnection connection) {
        // FDA Daily Values for adults and children 4+ (standard nutrition label values)
        // Source: https://www.fda.gov/food/nutrition-facts-label/daily-value-nutrition-and-supplement-facts-labels
        string sql = """
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
        await ExecuteNonQueryAsync(connection, sql).ConfigureAwait(false);
        Console.WriteLine("  DailyReferenceValues: 22 rows (FDA Daily Values)");
    }

    private static async Task<long> ImportFoodsAsync(NpgsqlConnection connection, string csvDirectory) {
        string foodCsvPath = Path.Combine(csvDirectory, "food.csv");
        string categoryCsvPath = Path.Combine(csvDirectory, "food_category.csv");

        if (!File.Exists(foodCsvPath)) {
            throw new FileNotFoundException("food.csv not found in USDA CSV directory.", foodCsvPath);
        }

        // Load food categories if available
        var categories = new Dictionary<int, string>();
        if (File.Exists(categoryCsvPath)) {
            await foreach (string? line in UsdaCsvReader.ReadLinesAsync(categoryCsvPath).ConfigureAwait(false)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                if (fields.Length >= 2 && int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int catId)) {
                    categories[catId] = fields[1];
                }
            }
        }

        long count = 0;
        NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoods" ("FdcId", "Description", "FoodCategoryId", "FoodCategory") FROM STDIN (FORMAT BINARY)""").ConfigureAwait(false);
        await using (writer.ConfigureAwait(false)) {
            await foreach (string? line in UsdaCsvReader.ReadLinesAsync(foodCsvPath).ConfigureAwait(false)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // food.csv: fdc_id, data_type, description, food_category_id, publication_date
                if (fields.Length < 4) continue;
                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fdcId)) continue;

                // Only import SR Legacy foods
                if (!string.Equals(fields[1], "sr_legacy_food", StringComparison.Ordinal)) continue;

                int? foodCategoryId = int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int catId2) ? catId2 : null;
                string? foodCategory = foodCategoryId.HasValue && categories.TryGetValue(foodCategoryId.Value, out string? catName)
                    ? catName : null;

                await writer.StartRowAsync().ConfigureAwait(false);
                await writer.WriteAsync(fdcId, NpgsqlTypes.NpgsqlDbType.Integer).ConfigureAwait(false);
                await writer.WriteAsync(UsdaCsvReader.Truncate(fields[2], 512), NpgsqlTypes.NpgsqlDbType.Varchar).ConfigureAwait(false);
                if (foodCategoryId.HasValue) {
                    await writer.WriteAsync(foodCategoryId.Value, NpgsqlTypes.NpgsqlDbType.Integer).ConfigureAwait(false);
                } else {
                    await writer.WriteNullAsync().ConfigureAwait(false);
                }
                if (foodCategory is not null) {
                    await writer.WriteAsync(UsdaCsvReader.Truncate(foodCategory, 256), NpgsqlTypes.NpgsqlDbType.Varchar).ConfigureAwait(false);
                } else {
                    await writer.WriteNullAsync().ConfigureAwait(false);
                }
                count++;
            }

            await writer.CompleteAsync().ConfigureAwait(false);
            return count;
        }
    }

    private static async Task<long> ImportNutrientsAsync(NpgsqlConnection connection, string csvDirectory) {
        string csvPath = Path.Combine(csvDirectory, "nutrient.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("nutrient.csv not found in USDA CSV directory.", csvPath);
        }

        long count = 0;
        NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaNutrients" ("Id", "Name", "UnitName") FROM STDIN (FORMAT BINARY)""").ConfigureAwait(false);
        await using (writer.ConfigureAwait(false)) {
            await foreach (string? line in UsdaCsvReader.ReadLinesAsync(csvPath).ConfigureAwait(false)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // nutrient.csv: id, name, unit_name, nutrient_nbr, rank
                if (fields.Length < 3) continue;
                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;

                await writer.StartRowAsync().ConfigureAwait(false);
                await writer.WriteAsync(id, NpgsqlTypes.NpgsqlDbType.Integer).ConfigureAwait(false);
                await writer.WriteAsync(UsdaCsvReader.Truncate(fields[1], 256), NpgsqlTypes.NpgsqlDbType.Varchar).ConfigureAwait(false);
                await writer.WriteAsync(UsdaCsvReader.Truncate(fields[2], 32), NpgsqlTypes.NpgsqlDbType.Varchar).ConfigureAwait(false);
                count++;
            }

            await writer.CompleteAsync().ConfigureAwait(false);
            return count;
        }
    }

    private static async Task<long> ImportFoodNutrientsAsync(NpgsqlConnection connection, string csvDirectory) {
        string csvPath = Path.Combine(csvDirectory, "food_nutrient.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("food_nutrient.csv not found in USDA CSV directory.", csvPath);
        }

        // Load valid FDC IDs to filter only SR Legacy foods
        HashSet<int> validFdcIds = await LoadValidFdcIdsAsync(connection).ConfigureAwait(false);

        long count = 0;
        NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoodNutrients" ("Id", "FdcId", "NutrientId", "Amount") FROM STDIN (FORMAT BINARY)""").ConfigureAwait(false);
        await using (writer.ConfigureAwait(false)) {
            await foreach (string? line in UsdaCsvReader.ReadLinesAsync(csvPath).ConfigureAwait(false)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // food_nutrient.csv: id, fdc_id, nutrient_id, amount, ...
                if (fields.Length < 4) continue;
                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fdcId)) continue;
                if (!int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int nutrientId)) continue;
                if (!double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double amount)) continue;

                if (!validFdcIds.Contains(fdcId)) continue;

                await writer.StartRowAsync().ConfigureAwait(false);
                await writer.WriteAsync(id, NpgsqlTypes.NpgsqlDbType.Integer).ConfigureAwait(false);
                await writer.WriteAsync(fdcId, NpgsqlTypes.NpgsqlDbType.Integer).ConfigureAwait(false);
                await writer.WriteAsync(nutrientId, NpgsqlTypes.NpgsqlDbType.Integer).ConfigureAwait(false);
                await writer.WriteAsync(amount, NpgsqlTypes.NpgsqlDbType.Double).ConfigureAwait(false);
                count++;
            }

            await writer.CompleteAsync().ConfigureAwait(false);
            return count;
        }
    }

    private static async Task<long> ImportFoodPortionsAsync(NpgsqlConnection connection, string csvDirectory) {
        string csvPath = Path.Combine(csvDirectory, "food_portion.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("food_portion.csv not found in USDA CSV directory.", csvPath);
        }

        HashSet<int> validFdcIds = await LoadValidFdcIdsAsync(connection).ConfigureAwait(false);

        long count = 0;
        NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoodPortions" ("Id", "FdcId", "Amount", "MeasureUnitName", "GramWeight", "PortionDescription", "Modifier") FROM STDIN (FORMAT BINARY)""").ConfigureAwait(false);
        await using (writer.ConfigureAwait(false)) {
            await foreach (string? line in UsdaCsvReader.ReadLinesAsync(csvPath).ConfigureAwait(false)) {
                string[] fields = UsdaCsvReader.ParseLine(line);
                // food_portion.csv: id, fdc_id, seq_num, amount, measure_unit_id, portion_description, modifier, gram_weight, ...
                if (fields.Length < 8) continue;
                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fdcId)) continue;

                if (!validFdcIds.Contains(fdcId)) continue;

                if (!double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double amount)) {
                    amount = 1.0;
                }

                string? portionDescription = string.IsNullOrWhiteSpace(fields[5]) ? null : fields[5];
                string? modifier = string.IsNullOrWhiteSpace(fields[6]) ? null : fields[6];

                if (!double.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out double gramWeight)) continue;

                // measure_unit_name is not in CSV directly; use portion_description as fallback
                string measureUnitName = modifier ?? portionDescription ?? "serving";

                await writer.StartRowAsync().ConfigureAwait(false);
                await writer.WriteAsync(id, NpgsqlTypes.NpgsqlDbType.Integer).ConfigureAwait(false);
                await writer.WriteAsync(fdcId, NpgsqlTypes.NpgsqlDbType.Integer).ConfigureAwait(false);
                await writer.WriteAsync(amount, NpgsqlTypes.NpgsqlDbType.Double).ConfigureAwait(false);
                await writer.WriteAsync(UsdaCsvReader.Truncate(measureUnitName, 128), NpgsqlTypes.NpgsqlDbType.Varchar).ConfigureAwait(false);
                await writer.WriteAsync(gramWeight, NpgsqlTypes.NpgsqlDbType.Double).ConfigureAwait(false);
                if (portionDescription is not null) {
                    await writer.WriteAsync(UsdaCsvReader.Truncate(portionDescription, 256), NpgsqlTypes.NpgsqlDbType.Varchar).ConfigureAwait(false);
                } else {
                    await writer.WriteNullAsync().ConfigureAwait(false);
                }
                if (modifier is not null) {
                    await writer.WriteAsync(UsdaCsvReader.Truncate(modifier, 128), NpgsqlTypes.NpgsqlDbType.Varchar).ConfigureAwait(false);
                } else {
                    await writer.WriteNullAsync().ConfigureAwait(false);
                }
                count++;
            }

            await writer.CompleteAsync().ConfigureAwait(false);
            return count;
        }
    }

    private static async Task<HashSet<int>> LoadValidFdcIdsAsync(NpgsqlConnection connection) {
        var ids = new HashSet<int>();
        var cmd = new NpgsqlCommand("""SELECT "FdcId" FROM "UsdaFoods" """, connection);
        await using (cmd.ConfigureAwait(false)) {
            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            await using (reader.ConfigureAwait(false)) {
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    ids.Add(reader.GetInt32(0));
                }
                return ids;
            }
        }
    }

    private static async Task<long> CountRowsAsync(NpgsqlConnection connection, string tableName) {
        var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {tableName}", connection);
        await using (cmd.ConfigureAwait(false)) {
            object? result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }
    }

    private static async Task ExecuteNonQueryAsync(NpgsqlConnection connection, string sql) {
        var cmd = new NpgsqlCommand(sql, connection);
        await using (cmd.ConfigureAwait(false)) {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

}
