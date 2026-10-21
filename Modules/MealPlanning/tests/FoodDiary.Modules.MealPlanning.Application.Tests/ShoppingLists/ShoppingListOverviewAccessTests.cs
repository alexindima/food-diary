using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingListOverview;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.ShoppingLists;

[ExcludeFromCodeCoverage]
public sealed class ShoppingListOverviewAccessTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Overview_RejectsAMissingOrForbiddenUserBeforeReadingTheirLists(bool missingUser) {
        IShoppingListReadModelRepository repository = Substitute.For<IShoppingListReadModelRepository>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        var user = UserId.New();
        var forbidden = new Error("Users.Forbidden", "Not allowed", ErrorKind.Forbidden);
        access.EnsureCanAccessAsync(user, Arg.Any<CancellationToken>()).Returns(forbidden);

        Error error = ResultAssert.Failure(await new GetShoppingListOverviewQueryHandler(repository, access)
            .Handle(new GetShoppingListOverviewQuery(missingUser ? null : user.Value), CancellationToken.None));

        Assert.Empty(repository.ReceivedCalls());
        if (missingUser) { Assert.Empty(access.ReceivedCalls()); } else { Assert.Equal(forbidden, error); }
    }
}
