using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Users;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public class GetDesiredWaistQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetDesiredWaistQuery, Result<UserDesiredWaistResponse>>
{
    public async Task<Result<UserDesiredWaistResponse>> Handle(
        GetDesiredWaistQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<UserDesiredWaistResponse>(Errors.User.NotFound());
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        if (user is null)
        {
            return Result.Failure<UserDesiredWaistResponse>(Errors.User.NotFound(query.UserId.Value));
        }

        return Result.Success(new UserDesiredWaistResponse(user.DesiredWaist));
    }
}
