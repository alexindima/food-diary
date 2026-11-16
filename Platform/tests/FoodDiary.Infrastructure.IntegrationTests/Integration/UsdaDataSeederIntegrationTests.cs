using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Initializer;
using FoodDiary.Modules.Usda.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UsdaDataSeederIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const int FdcId = 167512;

    [RequiresDockerFact]
    public async Task SeedAsync_RecoversLegacyPartialImportAndStoresCategoryName() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        context.UsdaFoods.Add(new UsdaFood {
            FdcId = FdcId,
            Description = "Previously imported food",
            FoodCategoryId = 18,
            FoodCategory = "1800",
        });
        await context.SaveChangesAsync();

        DirectoryInfo archive = CreateArchive();
        try {
            await UsdaDataSeeder.SeedAsync(context, archive.FullName);

            UsdaFood food = await context.UsdaFoods.AsNoTracking().SingleAsync();
            Assert.Multiple(
                () => Assert.Equal("SR Legacy biscuit", food.Description),
                () => Assert.Equal(18, food.FoodCategoryId),
                () => Assert.Equal("Baked Products", food.FoodCategory));
            Assert.Equal(1, await context.UsdaFoodNutrients.CountAsync());
            Assert.Equal(1, await context.UsdaFoodPortions.CountAsync());
            Assert.Equal(22, await context.DailyReferenceValues.CountAsync());

            await File.WriteAllTextAsync(Path.Combine(archive.FullName, "food.csv"),
                "fdc_id,data_type,description,food_category_id,publication_date\n167512,sr_legacy_food,Replacement,18,2019-04-01\n");
            await UsdaDataSeeder.SeedAsync(context, archive.FullName);
            Assert.Equal("SR Legacy biscuit", await context.UsdaFoods.Select(item => item.Description).SingleAsync());
        } finally {
            archive.Delete(recursive: true);
        }
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SeedAsync_WhenNutrientCopyTimesOut_PreservesThePreviousState(bool force) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        DirectoryInfo archive = CreateArchive();
        try {
            if (force) {
                await UsdaDataSeeder.SeedAsync(context, archive.FullName);
                await context.UsdaFoods.ExecuteUpdateAsync(update => update.SetProperty(food => food.Description, "Existing complete catalog"));
            }

            await context.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION delay_usda_nutrient_copy() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM pg_sleep(10);
                    RETURN NULL;
                END;
                $$;
                CREATE TRIGGER delay_usda_nutrient_copy BEFORE INSERT ON "UsdaFoodNutrients"
                FOR EACH STATEMENT EXECUTE FUNCTION delay_usda_nutrient_copy();
                """);
            var timeoutConnection = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString()) {
                Options = "-c statement_timeout=1000",
            };
            await using FoodDiaryDbContext timeoutContext = databaseFixture.CreateDbContext(timeoutConnection.ConnectionString);

            PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => force
                ? UsdaDataSeeder.ForceSeedAsync(timeoutContext, archive.FullName)
                : UsdaDataSeeder.SeedAsync(timeoutContext, archive.FullName));
            Assert.Equal(PostgresErrorCodes.QueryCanceled, error.SqlState);
            Assert.Contains("delay_usda_nutrient_copy", error.Where, StringComparison.Ordinal);
            string[] foods = await context.UsdaFoods.Select(food => food.Description).ToArrayAsync();
            string[] expectedFoods = force ? ["Existing complete catalog"] : [];
            Assert.True(expectedFoods.SequenceEqual(foods), "A timed-out USDA import must leave the prior catalog intact.");
            Assert.Equal(force ? 1 : 0, await context.UsdaFoodNutrients.CountAsync());
            Assert.Equal(force ? 1 : 0, await context.UsdaFoodPortions.CountAsync());
            Assert.Equal(force ? 22 : 0, await context.DailyReferenceValues.CountAsync());

            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER delay_usda_nutrient_copy ON \"UsdaFoodNutrients\"");
            if (force) {
                await UsdaDataSeeder.ForceSeedAsync(context, archive.FullName);
            } else {
                await UsdaDataSeeder.SeedAsync(context, archive.FullName);
            }
            Assert.Equal("SR Legacy biscuit", await context.UsdaFoods.Select(food => food.Description).SingleAsync());
            Assert.Equal(1, await context.UsdaFoodNutrients.CountAsync());
            Assert.Equal(1, await context.UsdaFoodPortions.CountAsync());
            Assert.Equal(22, await context.DailyReferenceValues.CountAsync());
        } finally {
            archive.Delete(recursive: true);
        }
    }

    private static DirectoryInfo CreateArchive() {
        DirectoryInfo archive = Directory.CreateTempSubdirectory("fooddiary-usda-");
        int[] nutrientIds = [1106, 1165, 1166, 1167, 1170, 1175, 1177, 1178, 1162, 1110, 1109, 1185, 1087, 1089, 1090, 1091, 1092, 1093, 1095, 1098, 1101, 1103];
        File.WriteAllText(Path.Combine(archive.FullName, "food.csv"),
            "fdc_id,data_type,description,food_category_id,publication_date\n167512,sr_legacy_food,SR Legacy biscuit,18,2019-04-01\n");
        File.WriteAllText(Path.Combine(archive.FullName, "food_category.csv"), "id,code,description\n18,1800,Baked Products\n");
        File.WriteAllLines(Path.Combine(archive.FullName, "nutrient.csv"), [
            "id,name,unit_name", .. nutrientIds.Select(id => FormattableString.Invariant($"{id},Test nutrient,mg")),
        ]);
        File.WriteAllText(Path.Combine(archive.FullName, "food_nutrient.csv"), "id,fdc_id,nutrient_id,amount\n1,167512,1106,0.5\n");
        File.WriteAllText(Path.Combine(archive.FullName, "food_portion.csv"),
            "id,fdc_id,seq_num,amount,measure_unit_id,portion_description,modifier,gram_weight\n1,167512,1,1,9999,,serving,34\n");
        return archive;
    }
}
