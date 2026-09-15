using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;

namespace FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvice;

public record GetDailyAdviceQuery(
    Guid? UserId,
    DateTime Date,
    string Locale) : IQuery<Result<DailyAdviceModel>>, IUserRequest;
