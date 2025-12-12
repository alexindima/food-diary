using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.DailyAdvices;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

public record GetDailyAdviceQuery(
    UserId? UserId,
    DateTime Date,
    string Locale) : IQuery<Result<DailyAdviceResponse>>;
