using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Meals.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MealActivityReadServiceTests {
    [Fact]
    public async Task GetCountAsync_ForwardsUserFiltersAndCancellation() {
        IMealActivityReadRepository repository = Substitute.For<IMealActivityReadRepository>();
        var user = UserId.New();
        var filters = new MealQueryFilters(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow, HasImage: true);
        using var cancellation = new CancellationTokenSource();
        repository.GetCountAsync(user, filters, cancellation.Token).Returns(7);

        Assert.Equal(7, await new MealActivityReadService(repository).GetCountAsync(user, filters, cancellation.Token));
        await repository.Received(1).GetCountAsync(user, filters, cancellation.Token);
    }
}
