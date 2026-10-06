using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Application.Commands.LinkProductToUsdaFood;
using FoodDiary.Modules.Usda.Application.Queries.GetDailyMicronutrients;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Usda.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsdaProviderReferenceTests {
    private const int ProviderFdcId = 2667078;

    [Fact]
    public async Task LinkProviderOnlyFood_ValidatesProviderAndKeepsUserOwnership() {
        var userId = UserId.New();
        var productId = ProductId.New();
        IUsdaProductLinkService links = Substitute.For<IUsdaProductLinkService>();
        links.IsAccessibleForUpdateAsync(productId, userId, Arg.Any<CancellationToken>()).Returns(Result.Success());
        links.LinkAsync(productId, userId, ProviderFdcId, Arg.Any<CancellationToken>()).Returns(Result.Success());
        IUsdaFoodSearchService provider = Substitute.For<IUsdaFoodSearchService>();
        provider.GetFoodDetailAsync(ProviderFdcId, Arg.Any<CancellationToken>()).Returns(ProviderDetail());
        var handler = new LinkProductToUsdaFoodCommandHandler(links, Substitute.For<IUsdaFoodReadRepository>(),
            Substitute.For<ICurrentUserAccessService>(), provider);

        Result result = await handler.Handle(new LinkProductToUsdaFoodCommand(userId.Value, productId.Value, ProviderFdcId), CancellationToken.None);

        ResultAssert.Success(result);
        await provider.Received(1).GetFoodDetailAsync(ProviderFdcId, Arg.Any<CancellationToken>());
        await links.Received(1).LinkAsync(productId, userId, ProviderFdcId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkInaccessibleProduct_DoesNotContactProviderOrMutate() {
        var userId = UserId.New();
        var productId = ProductId.New();
        IUsdaProductLinkService links = Substitute.For<IUsdaProductLinkService>();
        links.IsAccessibleForUpdateAsync(productId, userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Product.NotAccessible", "Inaccessible", Kind: ErrorKind.NotFound)));
        IUsdaFoodSearchService provider = Substitute.For<IUsdaFoodSearchService>();
        var handler = new LinkProductToUsdaFoodCommandHandler(links, Substitute.For<IUsdaFoodReadRepository>(),
            Substitute.For<ICurrentUserAccessService>(), provider);

        Result result = await handler.Handle(new LinkProductToUsdaFoodCommand(userId.Value, productId.Value, ProviderFdcId), CancellationToken.None);

        ResultAssert.Failure(result);
        await provider.DidNotReceiveWithAnyArgs().GetFoodDetailAsync(default, default);
        await links.DidNotReceiveWithAnyArgs().LinkAsync(default, default, default, default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LinkMissingOrMismatchedProviderFood_DoesNotMutate(bool mismatched) {
        var userId = UserId.New();
        var productId = ProductId.New();
        IUsdaProductLinkService links = Substitute.For<IUsdaProductLinkService>();
        links.IsAccessibleForUpdateAsync(productId, userId, Arg.Any<CancellationToken>()).Returns(Result.Success());
        IUsdaFoodSearchService provider = Substitute.For<IUsdaFoodSearchService>();
        provider.GetFoodDetailAsync(ProviderFdcId, Arg.Any<CancellationToken>())
            .Returns(mismatched ? ProviderDetail() with { FdcId = ProviderFdcId + 1 } : null);
        var handler = new LinkProductToUsdaFoodCommandHandler(links, Substitute.For<IUsdaFoodReadRepository>(),
            Substitute.For<ICurrentUserAccessService>(), provider);

        Result result = await handler.Handle(new LinkProductToUsdaFoodCommand(userId.Value, productId.Value, ProviderFdcId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(UsdaErrors.FoodNotFound(ProviderFdcId).Code, result.Error.Code);
        await links.DidNotReceiveWithAnyArgs().LinkAsync(default, default, default, default);
    }

    [Fact]
    public async Task DailyProviderNutrients_AreResolvedOnceAndScaledByGrams() {
        var userId = UserId.New();
        var date = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
        IUsdaMealNutritionReadService meals = Substitute.For<IUsdaMealNutritionReadService>();
        meals.GetForDateAsync(userId, date, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<UsdaMealProductNutritionReadModel>)[
                new(50, MeasurementUnit.G, ProviderFdcId), new(150, MeasurementUnit.G, ProviderFdcId)]);
        IUsdaFoodReadModelRepository repository = Substitute.For<IUsdaFoodReadModelRepository>();
        repository.GetNutrientReadModelsByFdcIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, IReadOnlyList<UsdaNutrientReadModel>>());
        repository.GetDailyReferenceValueReadModelsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, UsdaDailyReferenceValueReadModel>());
        IUsdaFoodSearchService provider = Substitute.For<IUsdaFoodSearchService>();
        provider.GetFoodDetailAsync(ProviderFdcId, Arg.Any<CancellationToken>()).Returns(ProviderDetail());
        var handler = new GetDailyMicronutrientsQueryHandler(meals, repository, Substitute.For<ICurrentUserAccessService>(), provider, TimeProvider.System);

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(new GetDailyMicronutrientsQuery(userId.Value, date), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(20, Assert.Single(result.Value.Nutrients).TotalAmount);
        await provider.Received(1).GetFoodDetailAsync(ProviderFdcId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DailyLocalNutrients_DoNotRequireProviderAvailability() {
        var userId = UserId.New();
        var date = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
        IUsdaMealNutritionReadService meals = Substitute.For<IUsdaMealNutritionReadService>();
        meals.GetForDateAsync(userId, date, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<UsdaMealProductNutritionReadModel>)[new(200, MeasurementUnit.G, ProviderFdcId)]);
        IUsdaFoodReadModelRepository repository = Substitute.For<IUsdaFoodReadModelRepository>();
        repository.GetNutrientReadModelsByFdcIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, IReadOnlyList<UsdaNutrientReadModel>> {
                [ProviderFdcId] = [new(1003, "Protein", "g", 10)],
            });
        repository.GetDailyReferenceValueReadModelsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, UsdaDailyReferenceValueReadModel>());
        IUsdaFoodSearchService provider = Substitute.For<IUsdaFoodSearchService>();
        var handler = new GetDailyMicronutrientsQueryHandler(meals, repository, Substitute.For<ICurrentUserAccessService>(), provider, TimeProvider.System);

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(new GetDailyMicronutrientsQuery(userId.Value, date), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(20, Assert.Single(result.Value.Nutrients).TotalAmount);
        await provider.DidNotReceiveWithAnyArgs().GetFoodDetailAsync(default, default);
    }

    private static UsdaFoodDetailModel ProviderDetail() =>
        new(ProviderFdcId, "Public branded fixture", "Branded",
            [new(1003, "Protein", "g", 10, DailyValue: null, PercentDailyValue: null)], [], HealthScores: null);
}
