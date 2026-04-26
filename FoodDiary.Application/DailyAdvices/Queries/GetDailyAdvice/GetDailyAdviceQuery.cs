using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.DailyAdvices.Models;

namespace FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

public record GetDailyAdviceQuery(
    Guid? UserId,
    DateTime Date,
    string Locale) : IQuery<Result<DailyAdviceModel>>, IUserRequest;
