using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingHistory;

public record GetFastingHistoryQuery(
    Guid? UserId,
    DateTime From,
    DateTime To) : IQuery<Result<IReadOnlyList<FastingSessionModel>>>, IUserRequest;
