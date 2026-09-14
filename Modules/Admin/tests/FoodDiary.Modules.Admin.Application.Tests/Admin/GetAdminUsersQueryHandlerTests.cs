using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Queries.GetUsersForAdministration;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUsers;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class GetAdminUsersQueryHandlerTests {
    [Fact]
    public async Task GetAdminUsersQueryHandler_NormalizesPagingAndCalculatesTotalPages() {
        ISender readService = Substitute.For<ISender>();
        readService.Send(new GetUsersForAdministrationQuery(Search: "alex", Page: 1, Limit: 20, Status: UserAccountStatusFilter.Active), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<UserAdminReadModel>(), 41));
        GetAdminUsersQueryHandler handler = new(readService);

        Result<PagedResponse<AdminUserModel>> result = await handler.Handle(
            new GetAdminUsersQuery(0, 500, "alex", UserAccountStatusFilter.Active),
            CancellationToken.None);

        PagedResponse<AdminUserModel> response = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(1, response.Page),
            () => Assert.Equal(20, response.Limit),
            () => Assert.Equal(3, response.TotalPages),
            () => Assert.Equal(41, response.TotalItems));
    }
}
