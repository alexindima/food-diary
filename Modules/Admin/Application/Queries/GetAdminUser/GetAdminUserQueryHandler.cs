using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserForAdministration;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadLatestWeightEntry;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

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
        if (user is null) {
            return Result.Failure<AdminUserModel>(UserErrors.NotFound(userId));
        }
        WeightEntryModel? latestWeight = await userReadService.Send(new ReadLatestWeightEntryQuery(userId), cancellationToken).ConfigureAwait(false);
        return Result.Success(user.ToAdminModel() with {
            LatestWeightKg = latestWeight?.WeightKg,
            LatestWeightDate = latestWeight?.Date,
        });
    }
}
