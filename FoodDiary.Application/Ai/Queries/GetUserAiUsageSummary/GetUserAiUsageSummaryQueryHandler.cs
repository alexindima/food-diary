using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Ai;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed class GetUserAiUsageSummaryQueryHandler(
    IUserRepository userRepository,
    IAiUsageRepository aiUsageRepository)
    : IQueryHandler<GetUserAiUsageSummaryQuery, Result<UserAiUsageResponse>>
{
    public async Task<Result<UserAiUsageResponse>> Handle(
        GetUserAiUsageSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId);
        if (user is null)
        {
            return Result.Failure<UserAiUsageResponse>(User.NotFound(query.UserId.Value));
        }

        var (monthStartUtc, monthEndUtc) = GetMonthBoundsUtc(DateTime.UtcNow);
        var totals = await aiUsageRepository.GetUserTotalsAsync(
            query.UserId,
            monthStartUtc,
            monthEndUtc,
            cancellationToken);

        return Result.Success(new UserAiUsageResponse(
            user.AiInputTokenLimit,
            user.AiOutputTokenLimit,
            totals.InputTokens,
            totals.OutputTokens,
            monthEndUtc));
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetMonthBoundsUtc(DateTime nowUtc)
    {
        var start = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }
}
