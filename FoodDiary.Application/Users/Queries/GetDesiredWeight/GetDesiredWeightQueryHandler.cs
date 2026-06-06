using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public class GetDesiredWeightQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetDesiredWeightQuery, Result<UserDesiredWeightModel>> {
    public async Task<Result<UserDesiredWeightModel>> Handle(
        GetDesiredWeightQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<UserDesiredWeightModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return accessError is not null
            ? Result.Failure<UserDesiredWeightModel>(accessError)
            : Result.Success(new UserDesiredWeightModel(user!.DesiredWeight));
    }
}
