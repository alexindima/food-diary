using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingHistory;

public record GetFastingHistoryQuery(
    Guid? UserId,
    DateTime From,
    DateTime To,
    int Page,
    int Limit) : IQuery<Result<PagedResponse<FastingSessionModel>>>, IUserRequest;
