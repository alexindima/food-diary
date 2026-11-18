extern alias Initializer;

using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Initializer;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Usda.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InitializerProgram = Initializer::Program;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UsdaCatalogImportIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const int FdcId = 168462;
    private const string Manifest = """
        {"version":1,"products":[{"fdcId":168462,"name":"Гречка","productType":"Grain","category":"Крупы"}]}
        """;

    [RequiresDockerFact]
    public async Task ImportAsync_DryRunDoesNotWriteAndRepeatedImportPreservesThePublishedProduct() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        database.UsdaFoods.Add(new UsdaFood { FdcId = FdcId, Description = "Buckwheat" });
        (int Id, string Unit, double Amount)[] nutrients = [
            (1008, "KCAL", 343), (1003, "g", 13.25), (1004, "g", 3.4),
            (1005, "g", 71.5), (1079, "g", 10), (1018, "g", 0),
        ];
        foreach ((int id, string unit, double amount) in nutrients) {
            database.UsdaNutrients.Add(new UsdaNutrient { Id = id, Name = "Test nutrient", UnitName = unit });
            database.UsdaFoodNutrients.Add(new UsdaFoodNutrient { Id = id, NutrientId = id, FdcId = FdcId, Amount = amount });
        }
        await database.SaveChangesAsync();

        string path = Path.GetTempFileName();
        try {
            await File.WriteAllTextAsync(path, Manifest);
            await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
            await RunImportAsync(provider, path, dryRun: true);
            Assert.False(await database.Products.AnyAsync());
            Assert.False(await database.Users.AnyAsync(user => user.Id == SystemContentOwner.Id));

            await RunImportAsync(provider, path, dryRun: false);
            var product = await database.Products.Select(item => new {
                item.Id, item.UserId, item.UsdaFdcId, item.Name, item.UsdaEnglishName, item.Category,
                item.ProductType, item.Visibility, item.BaseUnit, item.BaseAmount, item.CaloriesPerBase,
                item.ProteinsPerBase, item.FatsPerBase, item.CarbsPerBase, item.FiberPerBase,
                item.AlcoholPerBase, item.ModifiedOnUtc,
            }).SingleAsync();
            Assert.Multiple(
                () => Assert.Equal(SystemContentOwner.Id, product.UserId),
                () => Assert.Equal(FdcId, product.UsdaFdcId),
                () => Assert.Equal("Гречка", product.Name),
                () => Assert.Equal("Buckwheat", product.UsdaEnglishName),
                () => Assert.Equal("Крупы", product.Category),
                () => Assert.Equal(ProductType.Grain, product.ProductType),
                () => Assert.Equal(Visibility.Public, product.Visibility),
                () => Assert.Equal(MeasurementUnit.G, product.BaseUnit),
                () => Assert.Equal(100, product.BaseAmount),
                () => Assert.Equal(343, product.CaloriesPerBase),
                () => Assert.Equal(13.25, product.ProteinsPerBase),
                () => Assert.Equal(3.4, product.FatsPerBase),
                () => Assert.Equal(71.5, product.CarbsPerBase),
                () => Assert.Equal(10, product.FiberPerBase),
                () => Assert.Equal(0, product.AlcoholPerBase));

            await RunImportAsync(provider, path, dryRun: false);
            var reloaded = await database.Products.Select(item => new { item.Id, item.ModifiedOnUtc }).SingleAsync();
            Assert.Equal(product.Id, reloaded.Id);
            Assert.Equal(product.ModifiedOnUtc, reloaded.ModifiedOnUtc);
        } finally {
            File.Delete(path);
        }
    }

    private static ServiceProvider CreateProvider(string connectionString) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
            }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        InitializerProgram.ConfigureServices(services, configuration,
            new InitializerCommand("import-usda-catalog", TargetMigration: null, ConnectionString: null));
        return services.BuildServiceProvider();
    }

    private static async Task RunImportAsync(ServiceProvider provider, string path, bool dryRun) {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<UsdaCatalogImport>().RunAsync(path, dryRun, CancellationToken.None);
    }
}
