using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;

public class GetHydrationDailyTotalQueryHandler(
    IHydrationEntryRepository repository,
    IUserRepository userRepository)
    : IQueryHandler<GetHydrationDailyTotalQuery, Result<HydrationDailyModel>> {
    public async Task<Result<HydrationDailyModel>> Handle(
        GetHydrationDailyTotalQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<HydrationDailyModel>(Errors.User.NotFound());
        }

        var userId = new UserId(query.UserId.Value);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<HydrationDailyModel>(Errors.User.NotFound(userId.Value));
        }

        var dateUtc = NormalizeToUtcDate(query.DateUtc);
        var total = await repository.GetDailyTotalAsync(userId, dateUtc, cancellationToken);
        var goal = user.HydrationGoal ?? user.WaterGoal;

        var response = new HydrationDailyModel(dateUtc, total, goal);
        return Result.Success(response);
    }

    private static DateTime NormalizeToUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
