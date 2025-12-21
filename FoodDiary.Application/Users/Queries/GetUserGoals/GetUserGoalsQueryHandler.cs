using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Goals;

namespace FoodDiary.Application.Users.Queries.GetUserGoals;

public class GetUserGoalsQueryHandler : IQueryHandler<GetUserGoalsQuery, Result<GoalsResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetUserGoalsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GoalsResponse>> Handle(GetUserGoalsQuery query, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId!.Value);
        if (user is null)
        {
            return Result.Failure<GoalsResponse>(User.NotFound(query.UserId.Value));
        }

        return Result.Success(user.ToGoalsResponse());
    }
}
