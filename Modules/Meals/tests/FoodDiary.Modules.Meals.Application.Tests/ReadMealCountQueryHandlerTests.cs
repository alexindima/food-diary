using FoodDiary.Modules.Meals.Application.Queries.ReadTotalMealCount;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealCount;
using FoodDiary.Modules.Meals.Application.Queries.ReadMealCount;
using FoodDiary.Modules.Meals.Application.Queries.ReadDistinctMealDates;
using FoodDiary.Testing;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ReadMealCountQueryHandlerTests {
    [Fact]
    public async Task GetCountAsync_ForwardsUserFiltersAndCancellation() {
        IMealActivityReadRepository repository = Substitute.For<IMealActivityReadRepository>();
        var user = UserId.New();
        var filters = new MealQueryFilters(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow, HasImage: true);
        using var cancellation = new CancellationTokenSource();
        repository.GetCountAsync(user, filters, cancellation.Token).Returns(7);

        Assert.Equal(7, await RequestTestSender.Create(new ReadMealCountQueryHandler(repository), new ReadDistinctMealDatesQueryHandler(repository), new ReadTotalMealCountQueryHandler(repository)).Send(new ReadMealCountQuery(user, filters), cancellation.Token));
        await repository.Received(1).GetCountAsync(user, filters, cancellation.Token);
    }
}
