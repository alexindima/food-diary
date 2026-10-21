using FoodDiary.Modules.Products.Application.Commands.UpdateProduct;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Products.Application.Tests;

public partial class ProductsFeatureTests {
    [Theory]
    [InlineData(true, false, "Ml", 100)]
    [InlineData(false, true, "Pcs", 1)]
    [InlineData(true, true, "Ml", 100)]
    public async Task MeasurementUpdate_UsesTheRequestedOrExistingBasisWithoutScalingUnmentionedNutrients(
        bool changeUnit, bool explicitBasis, string expectedUnit, double expectedBasis) {
        var user = User.Create("measurement-update@example.test", "hash");
        var product = Product.Create(user.Id, "Food", MeasurementUnit.Pcs, 1, 2, 100, 12.5, 3.25, 8.75, 2.5, 0.75);
        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        var handler = new UpdateProductCommandHandler(repository, repository, cleanup, new StubUserRepository(user),
            AllowImageAssetAccessService.Instance, ProductTransactionRunner);
        var command = new UpdateProductCommand(user.Id.Value, product.Id.Value,
            Barcode: null, ClearBarcode: false, Name: null, Brand: null, ClearBrand: false, ProductType: null,
            Category: null, ClearCategory: false, Description: null, ClearDescription: false, Comment: null, ClearComment: false,
            ImageUrl: null, ClearImageUrl: false, ImageAssetId: null, ClearImageAssetId: false,
            BaseUnit: changeUnit ? "Ml" : null, BaseAmount: explicitBasis ? expectedBasis : null,
            DefaultPortionAmount: 3, CaloriesPerBase: 99.25, ProteinsPerBase: null, FatsPerBase: null, CarbsPerBase: null,
            FiberPerBase: null, AlcoholPerBase: null, Visibility: null);

        ProductModel result = ResultAssert.Success(await handler.Handle(command, CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(expectedUnit, result.BaseUnit),
            () => Assert.Equal(expectedBasis, result.BaseAmount),
            () => Assert.Equal(3, result.DefaultPortionAmount),
            () => Assert.Equal(99.25, result.CaloriesPerBase),
            () => Assert.Equal(12.5, result.ProteinsPerBase),
            () => Assert.Equal(3.25, result.FatsPerBase),
            () => Assert.Equal(8.75, result.CarbsPerBase),
            () => Assert.Equal(2.5, result.FiberPerBase),
            () => Assert.Equal(0.75, result.AlcoholPerBase),
            () => Assert.Equal("Food", result.Name),
            () => Assert.True(repository.GetByIdForUpdateCalled),
            () => Assert.True(repository.UpdateCalled),
            () => Assert.Empty(cleanup.RequestedAssetIds));
    }
}
