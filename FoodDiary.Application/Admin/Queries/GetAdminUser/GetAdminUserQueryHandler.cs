using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Queries.GetAdminUser;

public sealed class GetAdminUserQueryHandler(IAdminUserReadService userReadService)
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
        AdminUserModel? user = await userReadService.GetByIdIncludingDeletedAsync(userId, cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result.Failure<AdminUserModel>(Errors.User.NotFound(userId))
            : Result.Success(user);
    }
}
