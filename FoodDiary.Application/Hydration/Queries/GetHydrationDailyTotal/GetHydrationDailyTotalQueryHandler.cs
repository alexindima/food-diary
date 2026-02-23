using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;

public class GetHydrationDailyTotalQueryHandler(
    IHydrationEntryRepository repository,
    IUserRepository userRepository)
    : IQueryHandler<GetHydrationDailyTotalQuery, Result<HydrationDailyResponse>> {
    public async Task<Result<HydrationDailyResponse>> Handle(
        GetHydrationDailyTotalQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<HydrationDailyResponse>(Errors.User.NotFound());
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        if (user is null) {
            return Result.Failure<HydrationDailyResponse>(Errors.User.NotFound(query.UserId.Value.Value));
        }

        var dateUtc = NormalizeToUtcDate(query.DateUtc);
        var total = await repository.GetDailyTotalAsync(query.UserId.Value, dateUtc, cancellationToken);
        var goal = user.HydrationGoal ?? user.WaterGoal;

        var response = new HydrationDailyResponse(dateUtc, total, goal);
        return Result.Success(response);
    }

    private static DateTime NormalizeToUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
