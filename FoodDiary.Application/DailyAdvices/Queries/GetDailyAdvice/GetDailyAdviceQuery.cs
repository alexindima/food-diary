using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

public record GetDailyAdviceQuery(
    UserId? UserId,
    DateTime Date,
    string Locale) : IQuery<Result<DailyAdviceModel>>;
