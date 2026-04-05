using System.Globalization;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Initializer;

internal static class UsdaDataSeeder {
    public static async Task SeedAsync(FoodDiaryDbContext dbContext, string csvDirectory) {
        if (!Directory.Exists(csvDirectory)) {
            throw new DirectoryNotFoundException($"USDA CSV directory not found: {csvDirectory}");
        }

        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string configured.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var existingCount = await CountRowsAsync(connection, "\"UsdaFoods\"");
        if (existingCount > 0) {
            Console.WriteLine($"UsdaFoods already has {existingCount} rows. Skipping seed. Use --force to re-seed.");
            return;
        }

        Console.WriteLine("Seeding USDA reference data...");

        var foodCount = await ImportFoodsAsync(connection, csvDirectory);
        Console.WriteLine($"  Foods: {foodCount:N0} rows");

        var nutrientCount = await ImportNutrientsAsync(connection, csvDirectory);
        Console.WriteLine($"  Nutrients: {nutrientCount:N0} rows");

        var foodNutrientCount = await ImportFoodNutrientsAsync(connection, csvDirectory);
        Console.WriteLine($"  FoodNutrients: {foodNutrientCount:N0} rows");

        var portionCount = await ImportFoodPortionsAsync(connection, csvDirectory);
        Console.WriteLine($"  FoodPortions: {portionCount:N0} rows");

        await SeedDailyReferenceValuesAsync(connection);

        Console.WriteLine("USDA seed completed.");
    }

    public static async Task ForceSeedAsync(FoodDiaryDbContext dbContext, string csvDirectory) {
        if (!Directory.Exists(csvDirectory)) {
            throw new DirectoryNotFoundException($"USDA CSV directory not found: {csvDirectory}");
        }

        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No connection string configured.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        Console.WriteLine("Clearing existing USDA data...");
        await ExecuteNonQueryAsync(connection, """
            TRUNCATE "UsdaFoodNutrients", "UsdaFoodPortions", "UsdaFoods", "UsdaNutrients" CASCADE
            """);

        Console.WriteLine("Seeding USDA reference data...");

        var foodCount = await ImportFoodsAsync(connection, csvDirectory);
        Console.WriteLine($"  Foods: {foodCount:N0} rows");

        var nutrientCount = await ImportNutrientsAsync(connection, csvDirectory);
        Console.WriteLine($"  Nutrients: {nutrientCount:N0} rows");

        var foodNutrientCount = await ImportFoodNutrientsAsync(connection, csvDirectory);
        Console.WriteLine($"  FoodNutrients: {foodNutrientCount:N0} rows");

        var portionCount = await ImportFoodPortionsAsync(connection, csvDirectory);
        Console.WriteLine($"  FoodPortions: {portionCount:N0} rows");

        await SeedDailyReferenceValuesAsync(connection);

        Console.WriteLine("USDA seed completed.");
    }

    private static async Task SeedDailyReferenceValuesAsync(NpgsqlConnection connection) {
        // FDA Daily Values for adults and children 4+ (standard nutrition label values)
        // Source: https://www.fda.gov/food/nutrition-facts-label/daily-value-nutrition-and-supplement-facts-labels
        var sql = """
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
        await ExecuteNonQueryAsync(connection, sql);
        Console.WriteLine("  DailyReferenceValues: 22 rows (FDA Daily Values)");
    }

    private static async Task<long> ImportFoodsAsync(NpgsqlConnection connection, string csvDirectory) {
        var foodCsvPath = Path.Combine(csvDirectory, "food.csv");
        var categoryCsvPath = Path.Combine(csvDirectory, "food_category.csv");

        if (!File.Exists(foodCsvPath)) {
            throw new FileNotFoundException("food.csv not found in USDA CSV directory.", foodCsvPath);
        }

        // Load food categories if available
        var categories = new Dictionary<int, string>();
        if (File.Exists(categoryCsvPath)) {
            await foreach (var line in ReadCsvLinesAsync(categoryCsvPath)) {
                var fields = ParseCsvLine(line);
                if (fields.Length >= 2 && int.TryParse(fields[0], out var catId)) {
                    categories[catId] = fields[1];
                }
            }
        }

        long count = 0;
        await using var writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoods" ("FdcId", "Description", "FoodCategoryId", "FoodCategory") FROM STDIN (FORMAT BINARY)""");

        await foreach (var line in ReadCsvLinesAsync(foodCsvPath)) {
            var fields = ParseCsvLine(line);
            // food.csv: fdc_id, data_type, description, food_category_id, publication_date
            if (fields.Length < 4) continue;
            if (!int.TryParse(fields[0], out var fdcId)) continue;

            // Only import SR Legacy foods
            if (fields[1] != "sr_legacy_food") continue;

            int? foodCategoryId = int.TryParse(fields[3], out var catId2) ? catId2 : null;
            var foodCategory = foodCategoryId.HasValue && categories.TryGetValue(foodCategoryId.Value, out var catName)
                ? catName : null;

            await writer.StartRowAsync();
            await writer.WriteAsync(fdcId, NpgsqlTypes.NpgsqlDbType.Integer);
            await writer.WriteAsync(Truncate(fields[2], 512), NpgsqlTypes.NpgsqlDbType.Varchar);
            if (foodCategoryId.HasValue) {
                await writer.WriteAsync(foodCategoryId.Value, NpgsqlTypes.NpgsqlDbType.Integer);
            } else {
                await writer.WriteNullAsync();
            }
            if (foodCategory is not null) {
                await writer.WriteAsync(Truncate(foodCategory, 256), NpgsqlTypes.NpgsqlDbType.Varchar);
            } else {
                await writer.WriteNullAsync();
            }
            count++;
        }

        await writer.CompleteAsync();
        return count;
    }

    private static async Task<long> ImportNutrientsAsync(NpgsqlConnection connection, string csvDirectory) {
        var csvPath = Path.Combine(csvDirectory, "nutrient.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("nutrient.csv not found in USDA CSV directory.", csvPath);
        }

        long count = 0;
        await using var writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaNutrients" ("Id", "Name", "UnitName") FROM STDIN (FORMAT BINARY)""");

        await foreach (var line in ReadCsvLinesAsync(csvPath)) {
            var fields = ParseCsvLine(line);
            // nutrient.csv: id, name, unit_name, nutrient_nbr, rank
            if (fields.Length < 3) continue;
            if (!int.TryParse(fields[0], out var id)) continue;

            await writer.StartRowAsync();
            await writer.WriteAsync(id, NpgsqlTypes.NpgsqlDbType.Integer);
            await writer.WriteAsync(Truncate(fields[1], 256), NpgsqlTypes.NpgsqlDbType.Varchar);
            await writer.WriteAsync(Truncate(fields[2], 32), NpgsqlTypes.NpgsqlDbType.Varchar);
            count++;
        }

        await writer.CompleteAsync();
        return count;
    }

    private static async Task<long> ImportFoodNutrientsAsync(NpgsqlConnection connection, string csvDirectory) {
        var csvPath = Path.Combine(csvDirectory, "food_nutrient.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("food_nutrient.csv not found in USDA CSV directory.", csvPath);
        }

        // Load valid FDC IDs to filter only SR Legacy foods
        var validFdcIds = await LoadValidFdcIdsAsync(connection);

        long count = 0;
        await using var writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoodNutrients" ("Id", "FdcId", "NutrientId", "Amount") FROM STDIN (FORMAT BINARY)""");

        await foreach (var line in ReadCsvLinesAsync(csvPath)) {
            var fields = ParseCsvLine(line);
            // food_nutrient.csv: id, fdc_id, nutrient_id, amount, ...
            if (fields.Length < 4) continue;
            if (!int.TryParse(fields[0], out var id)) continue;
            if (!int.TryParse(fields[1], out var fdcId)) continue;
            if (!int.TryParse(fields[2], out var nutrientId)) continue;
            if (!double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)) continue;

            if (!validFdcIds.Contains(fdcId)) continue;

            await writer.StartRowAsync();
            await writer.WriteAsync(id, NpgsqlTypes.NpgsqlDbType.Integer);
            await writer.WriteAsync(fdcId, NpgsqlTypes.NpgsqlDbType.Integer);
            await writer.WriteAsync(nutrientId, NpgsqlTypes.NpgsqlDbType.Integer);
            await writer.WriteAsync(amount, NpgsqlTypes.NpgsqlDbType.Double);
            count++;
        }

        await writer.CompleteAsync();
        return count;
    }

    private static async Task<long> ImportFoodPortionsAsync(NpgsqlConnection connection, string csvDirectory) {
        var csvPath = Path.Combine(csvDirectory, "food_portion.csv");
        if (!File.Exists(csvPath)) {
            throw new FileNotFoundException("food_portion.csv not found in USDA CSV directory.", csvPath);
        }

        var validFdcIds = await LoadValidFdcIdsAsync(connection);

        long count = 0;
        await using var writer = await connection.BeginBinaryImportAsync(
            """COPY "UsdaFoodPortions" ("Id", "FdcId", "Amount", "MeasureUnitName", "GramWeight", "PortionDescription", "Modifier") FROM STDIN (FORMAT BINARY)""");

        await foreach (var line in ReadCsvLinesAsync(csvPath)) {
            var fields = ParseCsvLine(line);
            // food_portion.csv: id, fdc_id, seq_num, amount, measure_unit_id, portion_description, modifier, gram_weight, ...
            if (fields.Length < 8) continue;
            if (!int.TryParse(fields[0], out var id)) continue;
            if (!int.TryParse(fields[1], out var fdcId)) continue;

            if (!validFdcIds.Contains(fdcId)) continue;

            if (!double.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)) {
                amount = 1.0;
            }

            var portionDescription = string.IsNullOrWhiteSpace(fields[5]) ? null : fields[5];
            var modifier = string.IsNullOrWhiteSpace(fields[6]) ? null : fields[6];

            if (!double.TryParse(fields[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var gramWeight)) continue;

            // measure_unit_name is not in CSV directly; use portion_description as fallback
            var measureUnitName = modifier ?? portionDescription ?? "serving";

            await writer.StartRowAsync();
            await writer.WriteAsync(id, NpgsqlTypes.NpgsqlDbType.Integer);
            await writer.WriteAsync(fdcId, NpgsqlTypes.NpgsqlDbType.Integer);
            await writer.WriteAsync(amount, NpgsqlTypes.NpgsqlDbType.Double);
            await writer.WriteAsync(Truncate(measureUnitName, 128), NpgsqlTypes.NpgsqlDbType.Varchar);
            await writer.WriteAsync(gramWeight, NpgsqlTypes.NpgsqlDbType.Double);
            if (portionDescription is not null) {
                await writer.WriteAsync(Truncate(portionDescription, 256), NpgsqlTypes.NpgsqlDbType.Varchar);
            } else {
                await writer.WriteNullAsync();
            }
            if (modifier is not null) {
                await writer.WriteAsync(Truncate(modifier, 128), NpgsqlTypes.NpgsqlDbType.Varchar);
            } else {
                await writer.WriteNullAsync();
            }
            count++;
        }

        await writer.CompleteAsync();
        return count;
    }

    private static async Task<HashSet<int>> LoadValidFdcIdsAsync(NpgsqlConnection connection) {
        var ids = new HashSet<int>();
        await using var cmd = new NpgsqlCommand("""SELECT "FdcId" FROM "UsdaFoods" """, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            ids.Add(reader.GetInt32(0));
        }
        return ids;
    }

    private static async Task<long> CountRowsAsync(NpgsqlConnection connection, string tableName) {
        await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {tableName}", connection);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static async Task ExecuteNonQueryAsync(NpgsqlConnection connection, string sql) {
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async IAsyncEnumerable<string> ReadCsvLinesAsync(string filePath) {
        using var reader = new StreamReader(filePath);
        // Skip header
        await reader.ReadLineAsync();
        while (await reader.ReadLineAsync() is { } line) {
            if (!string.IsNullOrWhiteSpace(line)) {
                yield return line;
            }
        }
    }

    private static string[] ParseCsvLine(string line) {
        var fields = new List<string>();
        var inQuotes = false;
        var start = 0;

        for (var i = 0; i < line.Length; i++) {
            if (line[i] == '"') {
                inQuotes = !inQuotes;
            } else if (line[i] == ',' && !inQuotes) {
                fields.Add(ExtractField(line, start, i));
                start = i + 1;
            }
        }

        fields.Add(ExtractField(line, start, line.Length));
        return fields.ToArray();
    }

    private static string ExtractField(string line, int start, int end) {
        var field = line[start..end].Trim();
        if (field.Length >= 2 && field[0] == '"' && field[^1] == '"') {
            field = field[1..^1].Replace("\"\"", "\"");
        }
        return field;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
