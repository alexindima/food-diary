using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Hydration;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;

public class GetHydrationDailyTotalQueryHandler(
    IHydrationEntryRepository repository,
    IUserRepository userRepository)
    : IQueryHandler<GetHydrationDailyTotalQuery, Result<HydrationDailyResponse>>
{
    public async Task<Result<HydrationDailyResponse>> Handle(
        GetHydrationDailyTotalQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null)
        {
            return Result.Failure<HydrationDailyResponse>(Errors.User.NotFound());
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        if (user is null)
        {
            return Result.Failure<HydrationDailyResponse>(Errors.User.NotFound(query.UserId.Value.Value));
        }

        var total = await repository.GetDailyTotalAsync(query.UserId.Value, query.DateUtc, cancellationToken);
        var goal = user.HydrationGoal ?? user.WaterGoal;

        var response = new HydrationDailyResponse(query.DateUtc.Date, total, goal);
        return Result.Success(response);
    }
}
