using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Application.Queries.GetDailyMicronutrients;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Usda.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsdaProviderBudgetTests {
    private static readonly UserId UserId = UserId.New();
    private static readonly DateTime Date = new(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TooManyDistinctProviderFoods_ReturnsExplicitLimitWithoutNetworkLookups() {
        GetDailyMicronutrientsQueryHandler handler = CreateHandler(21, out IUsdaFoodSearchService provider);

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(new GetDailyMicronutrientsQuery(UserId.Value, Date), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Usda.ProviderLookupLimitExceeded", result.Error.Code);
        await provider.DidNotReceiveWithAnyArgs().GetFoodDetailAsync(default, default);
    }

    [Fact]
    public async Task ProviderFoodLimitBoundary_ResolvesEveryFoodWithoutTruncation() {
        GetDailyMicronutrientsQueryHandler handler = CreateHandler(20, out IUsdaFoodSearchService provider);

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(new GetDailyMicronutrientsQuery(UserId.Value, Date), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(200, Assert.Single(result.Value.Nutrients).TotalAmount);
        await provider.ReceivedWithAnyArgs(20).GetFoodDetailAsync(default, default);
    }

    [Fact]
    public async Task AggregateDeadline_ReturnsDependencyFailureAndStopsFurtherLookups() {
        Action? expire = null;
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan)
            .Returns(call => {
                TimerCallback callback = call.Arg<TimerCallback>();
                object? state = call.ArgAt<object?>(1);
                expire = () => callback(state);
                return Substitute.For<ITimer>();
            });
        GetDailyMicronutrientsQueryHandler handler = CreateHandler(3, out IUsdaFoodSearchService provider, clock);
        provider.GetFoodDetailAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call => {
            Assert.NotNull(expire);
            expire();
            return Task.FromCanceled<UsdaFoodDetailModel?>(call.Arg<CancellationToken>());
        });

        Result<DailyMicronutrientSummaryModel> result = await handler.Handle(new GetDailyMicronutrientsQuery(UserId.Value, Date), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Usda.ProviderLookupTimedOut", result.Error.Code),
            () => Assert.Equal(ErrorKind.ExternalFailure, result.Error.Kind));
        await provider.ReceivedWithAnyArgs(1).GetFoodDetailAsync(default, default);
    }

    [Fact]
    public async Task CallerCancellation_PropagatesInsteadOfBecomingDependencyFailure() {
        using var cancellation = new CancellationTokenSource();
        GetDailyMicronutrientsQueryHandler handler = CreateHandler(3, out IUsdaFoodSearchService provider);
        provider.GetFoodDetailAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async call => {
            await cancellation.CancelAsync();
            return await Task.FromCanceled<UsdaFoodDetailModel?>(call.Arg<CancellationToken>());
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.Handle(new GetDailyMicronutrientsQuery(UserId.Value, Date), cancellation.Token));
        await provider.ReceivedWithAnyArgs(1).GetFoodDetailAsync(default, default);
    }

    private static GetDailyMicronutrientsQueryHandler CreateHandler(int distinctFoods, out IUsdaFoodSearchService provider, TimeProvider? clock = null) {
        IUsdaMealNutritionReadService meals = Substitute.For<IUsdaMealNutritionReadService>();
        meals.GetForDateAsync(UserId, Date, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<UsdaMealProductNutritionReadModel>)[.. Enumerable.Range(1, distinctFoods)
                .Select(id => new UsdaMealProductNutritionReadModel(100, MeasurementUnit.G, id))]);
        IUsdaFoodReadModelRepository repository = Substitute.For<IUsdaFoodReadModelRepository>();
        repository.GetNutrientReadModelsByFdcIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, IReadOnlyList<UsdaNutrientReadModel>>());
        repository.GetDailyReferenceValueReadModelsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, UsdaDailyReferenceValueReadModel>());
        provider = Substitute.For<IUsdaFoodSearchService>();
        provider.GetFoodDetailAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call =>
            new UsdaFoodDetailModel(call.Arg<int>(), "Provider fixture", "Branded",
                [new(1003, "Protein", "g", 10, DailyValue: null, PercentDailyValue: null)], [], HealthScores: null));
        return new GetDailyMicronutrientsQueryHandler(meals, repository, Substitute.For<ICurrentUserAccessService>(), provider, clock ?? TimeProvider.System);
    }
}
