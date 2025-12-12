using System;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.DailyAdvices.Services;
using FoodDiary.Contracts.DailyAdvices;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

public class GetDailyAdviceQueryHandler(
    IDailyAdviceRepository adviceRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetDailyAdviceQuery, Result<DailyAdviceResponse>>
{
    public async Task<Result<DailyAdviceResponse>> Handle(GetDailyAdviceQuery query, CancellationToken cancellationToken)
    {
        if (query.UserId is null || query.UserId == UserId.Empty)
        {
            return Result.Failure<DailyAdviceResponse>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(query.UserId.Value);
        if (user is null)
        {
            return Result.Failure<DailyAdviceResponse>(Errors.User.NotFound(query.UserId.Value.Value));
        }

        var locale = NormalizeLocale(query.Locale);
        var advices = await adviceRepository.GetByLocaleAsync(locale, cancellationToken);

        if (advices.Count == 0 && !string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase))
        {
            locale = "en";
            advices = await adviceRepository.GetByLocaleAsync(locale, cancellationToken);
        }

        if (advices.Count == 0)
        {
            return Result.Failure<DailyAdviceResponse>(Errors.DailyAdvice.NotFound(locale));
        }

        var advice = DailyAdviceSelector.SelectForDate(advices, query.Date, locale);
        if (advice is null)
        {
            return Result.Failure<DailyAdviceResponse>(Errors.DailyAdvice.NotFound(locale));
        }

        var response = new DailyAdviceResponse(
            advice.Id.Value,
            advice.Locale,
            advice.Value,
            advice.Tag,
            advice.Weight);

        return Result.Success(response);
    }

    private static string NormalizeLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return "en";
        }

        var normalized = locale.Trim().ToLowerInvariant();
        var separatorIndex = normalized.IndexOfAny(new[] { '-', '_' });
        return separatorIndex > 0 ? normalized[..separatorIndex] : normalized;
    }
}
