using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Application.Abstractions.Models;
using FoodDiary.Modules.Meals.Application.Queries.GetMealsOverview;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;
using FoodDiary.Mediator;
using FoodDiary.Modules.Meals.Application.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoritesOverview;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteIds;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Tests;

public partial class MealsFeatureTests {
    [Theory]
    [InlineData("Asia/Tbilisi", null, 22)]
    [InlineData("Pacific/Kiritimati", null, 22)]
    [InlineData("America/New_York", null, 21)]
    [InlineData(null, 240, 22)]
    [InlineData(null, -660, 21)]
    public async Task Overview_AggregatesDistinctLocalPageDates_WithUserAndCancellation(string? zoneId, int? offset, int day) {
        var user = User.Create("summary@example.com", "hash");
        IMealProjectionReadRepository repository = Substitute.For<IMealProjectionReadRepository>();
        var instant = new DateTime(2026, 9, 21, 21, 0, 0, DateTimeKind.Utc);
        IReadOnlyList<MealProjectionReadModel> items = [ToMealProjectionReadModel(Meal.Create(user.Id, instant, MealType.Dinner)), ToMealProjectionReadModel(Meal.Create(user.Id, instant.AddMinutes(10), MealType.Dinner))];
        using var cancellation = new CancellationTokenSource();
        repository.GetPagedMealProjectionsAsync(user.Id, 2, 2, Arg.Any<MealQueryFilters>(), cancellation.Token).Returns((items, 10));
        IReadOnlyList<MealDaySummary> summaries = [new(new DateOnly(2026, 9, day), 2500, 8)];
        repository.GetDaySummariesAsync(user.Id, Arg.Any<IReadOnlyCollection<DateOnly>>(), Arg.Any<TimeZoneInfo>(), cancellation.Token).Returns(summaries);
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReadMealFavoriteIdsQuery>(), cancellation.Token).Returns(new Dictionary<MealId, FavoriteMealId>());
        var handler = new GetMealsOverviewQueryHandler(repository, sender, CreateCurrentUserAccessService(user));
        Result<MealOverviewModel> result = await handler.Handle(new GetMealsOverviewQuery(user.Id.Value, 2, 2, DateFrom: null, DateTo: null, MealTypes: ["Dinner"], TimeZoneId: zoneId, TimeZoneOffsetMinutes: offset, IncludeFavorites: false), cancellation.Token);
        ResultAssert.Success(result);
        Assert.Same(summaries, result.Value.DaySummaries);
        Assert.Empty(result.Value.FavoriteItems);
        await repository.Received(1).GetDaySummariesAsync(user.Id, Arg.Is<IReadOnlyCollection<DateOnly>>(dates => dates.Count == 1 && dates.Contains(new DateOnly(2026, 9, day))), Arg.Any<TimeZoneInfo>(), cancellation.Token);
        Assert.DoesNotContain(sender.ReceivedCalls(), call => call.GetArguments().OfType<ReadMealFavoritesOverviewQuery>().Any());
    }

    [Fact]
    public async Task Overview_EmptyPage_SkipsAggregation() {
        IMealProjectionReadRepository repository = Substitute.For<IMealProjectionReadRepository>();
        var user = User.Create("empty-summary@example.com", "hash");
        repository.GetPagedMealProjectionsAsync(user.Id, 1, 10, Arg.Any<MealQueryFilters>(), Arg.Any<CancellationToken>()).Returns((Array.Empty<MealProjectionReadModel>(), 0));
        var handler = new GetMealsOverviewQueryHandler(repository, CreateFavoriteSender(), CreateCurrentUserAccessService(user));
        Result<MealOverviewModel> result = await handler.Handle(new GetMealsOverviewQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null), CancellationToken.None);
        ResultAssert.Success(result);
        Assert.Empty(result.Value.DaySummaries);
        await repository.DidNotReceiveWithAnyArgs().GetDaySummariesAsync(default, default!, default!, default);
    }

    [Theory]
    [InlineData("not/a-zone", null)]
    [InlineData(null, 841)]
    [InlineData(null, -841)]
    public async Task Overview_InvalidTimezone_IsRejectedBeforeReading(string? zoneId, int? offset) {
        IMealProjectionReadRepository repository = Substitute.For<IMealProjectionReadRepository>();
        var user = User.Create("invalid-summary@example.com", "hash");
        var query = new GetMealsOverviewQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null, TimeZoneId: zoneId, TimeZoneOffsetMinutes: offset);
        Assert.False((await new GetMealsOverviewQueryValidator().ValidateAsync(query)).IsValid);
        var handler = new GetMealsOverviewQueryHandler(repository, CreateFavoriteSender(), CreateCurrentUserAccessService(user));
        ResultAssert.Failure(await handler.Handle(query, CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }
}
