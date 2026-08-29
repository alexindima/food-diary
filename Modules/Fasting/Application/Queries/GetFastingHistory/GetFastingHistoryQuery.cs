using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingHistory;

public record GetFastingHistoryQuery(
    Guid? UserId,
    DateTime From,
    DateTime To,
    int Page,
    int Limit) : IQuery<Result<PagedResponse<FastingSessionModel>>>, IUserRequest;
