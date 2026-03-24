using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed class GetUserAiUsageSummaryQueryHandler(
    IUserRepository userRepository,
    IAiUsageRepository aiUsageRepository)
    : IQueryHandler<GetUserAiUsageSummaryQuery, Result<UserAiUsageModel>> {
    public async Task<Result<UserAiUsageModel>> Handle(
        GetUserAiUsageSummaryQuery query,
        CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId);
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null) {
            return Result.Failure<UserAiUsageModel>(User.NotFound(userId));
        }

        var (monthStartUtc, monthEndUtc) = GetMonthBoundsUtc(DateTime.UtcNow);
        var totals = await aiUsageRepository.GetUserTotalsAsync(
            userId,
            monthStartUtc,
            monthEndUtc,
            cancellationToken);

        return Result.Success(new UserAiUsageModel(
            user.AiInputTokenLimit,
            user.AiOutputTokenLimit,
            totals.InputTokens,
            totals.OutputTokens,
            monthEndUtc));
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetMonthBoundsUtc(DateTime nowUtc) {
        var start = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }
}
