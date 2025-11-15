using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Users;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public class GetDesiredWeightQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetDesiredWeightQuery, Result<UserDesiredWeightResponse>>
{
    public async Task<Result<UserDesiredWeightResponse>> Handle(
        GetDesiredWeightQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<UserDesiredWeightResponse>(Errors.User.NotFound());
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        if (user is null)
        {
            return Result.Failure<UserDesiredWeightResponse>(Errors.User.NotFound(query.UserId.Value));
        }

        return Result.Success(new UserDesiredWeightResponse(user.DesiredWeight));
    }
}
