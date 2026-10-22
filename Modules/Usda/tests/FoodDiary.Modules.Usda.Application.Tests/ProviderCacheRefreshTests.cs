using System.Collections;
using System.Collections.Concurrent;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Application.Queries.GetDailyMicronutrients;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Usda.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProviderCacheRefreshTests {
    [Fact]
    public async Task LocalNutrientsRefreshedAfterTheMissingRead_AreUsedWithoutRequestingTheProvider() {
        var user = UserId.New();
        var date = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var missingRead = new ManualResetEventSlim();
        using var refreshCompleted = new ManualResetEventSlim();
        var values = new ConcurrentDictionary<int, IReadOnlyList<UsdaNutrientReadModel>>();
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> local =
            new RefreshingNutrients(values, missingRead, refreshCompleted, timeout.Token);
        IUsdaMealNutritionReadService meals = Substitute.For<IUsdaMealNutritionReadService>();
        meals.GetForDateAsync(user, date, 1001, timeout.Token).Returns((IReadOnlyList<UsdaMealProductNutritionReadModel>)[
            new(150, MeasurementUnit.G, 42),
        ]);
        IUsdaFoodReadModelRepository repository = Substitute.For<IUsdaFoodReadModelRepository>();
        repository.GetNutrientReadModelsByFdcIdsAsync(Arg.Any<IEnumerable<int>>(), timeout.Token).Returns(local);
        repository.GetDailyReferenceValueReadModelsAsync(cancellationToken: timeout.Token)
            .Returns(new Dictionary<int, UsdaDailyReferenceValueReadModel> { [1162] = new(1162, 60, "mg") });
        IUsdaFoodSearchService provider = Substitute.For<IUsdaFoodSearchService>();
        var handler = new GetDailyMicronutrientsQueryHandler(meals, repository,
            Substitute.For<ICurrentUserAccessService>(), provider, TimeProvider.System);
        var refresh = Task.Run(() => {
            try {
                missingRead.Wait(timeout.Token);
                values[42] = [new(1162, "Vitamin C", "mg", 12)];
            } finally {
                refreshCompleted.Set();
            }
        }, timeout.Token);

        try {
            DailyMicronutrientSummaryModel result = ResultAssert.Success(
                await handler.Handle(new GetDailyMicronutrientsQuery(user.Value, date), timeout.Token));
            await refresh;

            DailyMicronutrientModel nutrient = Assert.Single(result.Nutrients);
            Assert.Multiple(
                () => Assert.Equal(date, result.Date),
                () => Assert.Equal(1, result.LinkedProductCount),
                () => Assert.Equal(1, result.TotalProductCount),
                () => Assert.Equal(1162, nutrient.NutrientId),
                () => Assert.Equal("Vitamin C", nutrient.Name),
                () => Assert.Equal("mg", nutrient.Unit),
                () => Assert.Equal(18, nutrient.TotalAmount),
                () => Assert.Equal(60, nutrient.DailyValue),
                () => Assert.Equal(30, nutrient.PercentDailyValue),
                () => Assert.Equal(12, Assert.Single(values[42]).Amount));
            Assert.Empty(provider.ReceivedCalls());
            await meals.Received(1).GetForDateAsync(user, date, 1001, timeout.Token);
        } finally {
            await timeout.CancelAsync();
            try { await refresh.WaitAsync(TimeSpan.FromSeconds(5)); } catch (OperationCanceledException) { }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RefreshingNutrients(
        ConcurrentDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> values,
        ManualResetEventSlim missingRead,
        ManualResetEventSlim refreshCompleted,
        CancellationToken cancellationToken)
        : IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> {
        private int _coordinated;
        public IReadOnlyList<UsdaNutrientReadModel> this[int key] => values[key];
        public IEnumerable<int> Keys => values.Keys;
        public IEnumerable<IReadOnlyList<UsdaNutrientReadModel>> Values => values.Values;
        public int Count => values.Count;

        public bool ContainsKey(int key) {
            bool present = values.ContainsKey(key);
            if (!present && key == 42 && Interlocked.Exchange(ref _coordinated, 1) == 0) {
                // The read is real: coordinate a separate writer after that read and before enumeration.
                missingRead.Set();
                refreshCompleted.Wait(cancellationToken);
            }
            return present;
        }

        public bool TryGetValue(int key, [MaybeNullWhen(false)] out IReadOnlyList<UsdaNutrientReadModel> value) =>
            values.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<int, IReadOnlyList<UsdaNutrientReadModel>>> GetEnumerator() => values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
