using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Queries.GetUserForAdministration;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUser;

public sealed class GetAdminUserQueryHandler(ISender userReadService)
    : IQueryHandler<GetAdminUserQuery, Result<AdminUserModel>> {
    public async Task<Result<AdminUserModel>> Handle(
        GetAdminUserQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            query.UserId,
            Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<AdminUserModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        UserAdminReadModel? user = await userReadService.Send(new GetUserForAdministrationQuery(UserId: userId), cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result.Failure<AdminUserModel>(UserErrors.NotFound(userId))
            : Result.Success(user.ToAdminModel());
    }
}
