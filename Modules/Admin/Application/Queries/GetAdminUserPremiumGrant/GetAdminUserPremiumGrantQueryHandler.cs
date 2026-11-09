using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserManualPremiumGrant;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUserPremiumGrant;

public sealed class GetAdminUserPremiumGrantQueryHandler(ISender users)
    : IQueryHandler<GetAdminUserPremiumGrantQuery, Result<UserManualPremiumGrantModel>> {
    public async Task<Result<UserManualPremiumGrantModel>> Handle(GetAdminUserPremiumGrantQuery query, CancellationToken cancellationToken) {
        Result<UserId> target = UserIdParser.Parse(query.UserId, Errors.Validation.Invalid("userId", "User id must not be empty."));
        if (target.IsFailure) {
            return UserIdParser.ToFailure<UserManualPremiumGrantModel>(target);
        }
        UserManualPremiumGrantModel? grant = await users.Send(new GetUserManualPremiumGrantQuery(target.Value), cancellationToken);
        return grant is null
            ? Result.Failure<UserManualPremiumGrantModel>(UserErrors.NotFound(target.Value))
            : Result.Success(grant);
    }
}
