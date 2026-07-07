using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;

public sealed class GetWeeklyCheckInQueryHandler(
    IWeeklyCheckInReadService weeklyCheckInReadService,
    IWeeklyCheckInUserProfileService weeklyCheckInUserProfileService,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetWeeklyCheckInQuery, Result<WeeklyCheckInModel>> {
    public async Task<Result<WeeklyCheckInModel>> Handle(
        GetWeeklyCheckInQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<WeeklyCheckInModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<WeeklyCheckInUserProfile> profileResult = await weeklyCheckInUserProfileService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(profileResult.Error);
        }

        WeeklyCheckInUserProfile profile = profileResult.Value;
        DateTime today = dateTimeProvider.GetUtcNow().UtcDateTime.Date;
        DateTime thisWeekStart = today.AddDays(-6);
        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);

        Result<WeekSummaryModel> thisWeekSummaryResult = await weeklyCheckInReadService.LoadWeekSummaryAsync(userId, thisWeekStart, today, cancellationToken).ConfigureAwait(false);
        if (thisWeekSummaryResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(thisWeekSummaryResult.Error);
        }

        Result<WeekSummaryModel> lastWeekSummaryResult = await weeklyCheckInReadService.LoadWeekSummaryAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken).ConfigureAwait(false);
        if (lastWeekSummaryResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(lastWeekSummaryResult.Error);
        }

        WeekSummaryModel thisWeekSummary = thisWeekSummaryResult.Value;
        WeekSummaryModel lastWeekSummary = lastWeekSummaryResult.Value;
        WeekTrendModel trends = WeeklyCheckInCalculator.BuildTrends(thisWeekSummary, lastWeekSummary);
        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(thisWeekSummary, trends, profile.DailyCalorieTarget);

        return Result.Success(new WeeklyCheckInModel(thisWeekSummary, lastWeekSummary, trends, suggestions));
    }
}
