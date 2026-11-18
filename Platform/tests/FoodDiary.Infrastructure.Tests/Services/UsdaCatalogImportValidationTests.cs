using FoodDiary.Initializer;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class UsdaCatalogImportValidationTests {
    private const int FdcId = 168462;
    private const string Item = """{"fdcId":168462,"name":"Buckwheat","productType":"Grain","category":"Grains"}""";
    private const string Manifest = """{"version":1,"products":[""" + Item + "]}";

    [Theory]
    [InlineData("""{"version":2,"products":[""" + Item + "]}")]
    [InlineData("""{"version":1,"products":[]}""")]
    [InlineData("""{"version":1,"products":[null]}""")]
    [InlineData("""{"version":1,"products":[{"fdcId":0,"name":"Buckwheat","productType":"Grain","category":"Grains"}]}""")]
    [InlineData("""{"version":1,"products":[{"fdcId":168462,"name":" ","productType":"Grain","category":"Grains"}]}""")]
    [InlineData("""{"version":1,"products":[{"fdcId":168462,"name":"Buckwheat","productType":" ","category":"Grains"}]}""")]
    [InlineData("""{"version":1,"products":[{"fdcId":168462,"name":"Buckwheat","productType":"Grain","category":" "}]}""")]
    [InlineData("""{"version":1,"products":[""" + Item + "," + Item + "]}")]
    public async Task ImportAsync_RejectsInvalidManifestBeforeReadingCatalog(string manifest) {
        IUsdaCatalogReadService source = Substitute.For<IUsdaCatalogReadService>();

        InvalidOperationException error = await RejectAsync(manifest, source);

        Assert.Contains("version 1 manifest with unique positive FDC IDs", error.Message, StringComparison.Ordinal);
        await source.DidNotReceive().ReadLocalFoodsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_RequiresLocalUsdaRecord() {
        IUsdaCatalogReadService source = Substitute.For<IUsdaCatalogReadService>();
        source.ReadLocalFoodsAsync(Arg.Any<IReadOnlyList<int>>(), CancellationToken.None)
            .Returns(new Dictionary<int, UsdaCatalogFoodModel>());

        InvalidOperationException error = await RejectAsync(Manifest, source);

        Assert.Contains("USDA 168462 is missing", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, 343)]
    [InlineData("g", 343)]
    [InlineData("kcal", double.NaN)]
    [InlineData("kcal", -1)]
    public async Task ImportAsync_RejectsMissingOrInvalidEnergyBeforeLookingUpProducts(string? unit, double amount) {
        IUsdaCatalogReadService source = Substitute.For<IUsdaCatalogReadService>();
        source.ReadLocalFoodsAsync(Arg.Any<IReadOnlyList<int>>(), CancellationToken.None)
            .Returns(new Dictionary<int, UsdaCatalogFoodModel> {
                [FdcId] = new("Buckwheat", unit is null ? [] : [new UsdaNutrientReadModel(1008, "Energy", unit, amount)]),
            });

        InvalidOperationException error = await RejectAsync(Manifest, source);

        Assert.Equal("Invalid or missing USDA nutrient 1008 for food 168462. Expected kcal per 100 g.", error.Message);
    }

    private static async Task<InvalidOperationException> RejectAsync(string manifest, IUsdaCatalogReadService source) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused;Username=unused",
            }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        global::Program.ConfigureServices(services, configuration,
            new InitializerCommand("import-usda-catalog", TargetMigration: null, ConnectionString: null));
        services.AddScoped(_ => source);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        string path = Path.GetTempFileName();
        try {
            await File.WriteAllTextAsync(path, manifest);
            UsdaCatalogImport importer = scope.ServiceProvider.GetRequiredService<UsdaCatalogImport>();
            return await Assert.ThrowsAsync<InvalidOperationException>(
                () => importer.RunAsync(path, dryRun: false, CancellationToken.None));
        } finally {
            File.Delete(path);
        }
    }
}
