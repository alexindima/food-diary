using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminUsers;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class GetAdminUsersQueryHandlerTests {
    [Fact]
    public async Task GetAdminUsersQueryHandler_NormalizesPagingAndCalculatesTotalPages() {
        IAdminUserReadService readService = Substitute.For<IAdminUserReadService>();
        readService.GetPagedAsync("alex", 1, 20, UserAccountStatusFilter.Active, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AdminUserModel>(), 41));
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
