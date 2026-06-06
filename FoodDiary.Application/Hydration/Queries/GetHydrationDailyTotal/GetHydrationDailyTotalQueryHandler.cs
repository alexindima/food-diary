using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;

public class GetHydrationDailyTotalQueryHandler(
    IHydrationEntryRepository repository,
    IUserRepository userRepository)
    : IQueryHandler<GetHydrationDailyTotalQuery, Result<HydrationDailyModel>> {
    public async Task<Result<HydrationDailyModel>> Handle(
        GetHydrationDailyTotalQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<HydrationDailyModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<HydrationDailyModel>(accessError);
        }

        User currentUser = user!;
        DateTime dateUtc = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateUtc);
        int total = await repository.GetDailyTotalAsync(userId, dateUtc, cancellationToken).ConfigureAwait(false);
        double? goal = currentUser.HydrationGoal ?? currentUser.WaterGoal;

        var response = new HydrationDailyModel(dateUtc, total, goal);
        return Result.Success(response);
    }
}
