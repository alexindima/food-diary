using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Usda.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UsdaReferenceMigrationIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string PreviousMigration = "20261006060809_ImproveDefaultEmailTemplateContrast";
    private const int LocalFdcId = 171077;
    private const int ProviderFdcId = 2667078;

    [Fact]
    public void ProductModel_KeepsIndexedExternalFdcIdentifierWithoutCatalogForeignKey() {
        using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=usda_reference_model;Username=unused").Options);
        IEntityType product = Assert.IsAssignableFrom<IEntityType>(context.Model.FindEntityType(typeof(Product)));
        Assert.Multiple(
            () => Assert.DoesNotContain(product.GetForeignKeys(), foreignKey => foreignKey.Properties.Any(property => string.Equals(property.Name, nameof(Product.UsdaFdcId), StringComparison.Ordinal))),
            () => Assert.Contains(product.GetIndexes(), index => index.Properties.Count == 1 && string.Equals(index.Properties[0].Name, nameof(Product.UsdaFdcId), StringComparison.Ordinal)),
            () => Assert.False(context.Database.HasPendingModelChanges()));
    }

    [RequiresDockerFact]
    public async Task ProviderReferenceUpgradeAndRollback_PreserveProductsAndLocalLinks() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connectionString);
        await context.Database.MigrateAsync();
        var user = User.Create($"usda-reference-{Guid.NewGuid():N}@example.com", "hash");
        Product localProduct = CreateProduct(user, "Local reference");
        Product providerProduct = CreateProduct(user, "Provider reference");
        context.Users.Add(user);
        context.UsdaFoods.Add(new UsdaFood { FdcId = LocalFdcId, Description = "Local reference fixture" });
        context.Products.AddRange(localProduct, providerProduct);
        await context.SaveChangesAsync();

        IMigrator migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);
        localProduct.LinkToUsdaFood(LocalFdcId);
        await context.SaveChangesAsync();
        await context.Database.MigrateAsync();
        providerProduct.LinkToUsdaFood(ProviderFdcId);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Product imported = await context.Products.SingleAsync(product => product.Id == providerProduct.Id);
        Assert.Equal(ProviderFdcId, imported.UsdaFdcId);

        await migrator.MigrateAsync(PreviousMigration);
        context.ChangeTracker.Clear();
        Product[] products = [.. await context.Products.ToListAsync()];
        Assert.Multiple(
            () => Assert.Equal(2, products.Length),
            () => Assert.Equal(LocalFdcId, products.Single(product => product.Id == localProduct.Id).UsdaFdcId),
            () => Assert.Null(products.Single(product => product.Id == providerProduct.Id).UsdaFdcId),
            () => Assert.All(products, product => Assert.Equal(203, product.CaloriesPerBase)));
        await context.Database.MigrateAsync();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    private static Product CreateProduct(User user, string name) =>
        Product.Create(user.Id, name, MeasurementUnit.G, baseAmount: 100, defaultPortionAmount: null,
            caloriesPerBase: 203, proteinsPerBase: 1.4, fatsPerBase: 0, carbsPerBase: 47.3,
            fiberPerBase: 0, alcoholPerBase: 0, visibility: Visibility.Private);
}
