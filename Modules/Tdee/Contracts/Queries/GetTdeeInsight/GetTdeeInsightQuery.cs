using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Tdee.Contracts.Models;

namespace FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;

public record GetTdeeInsightQuery(
    Guid? UserId, DateOnly? CurrentDate = null, string? TimeZoneId = null, int? TimeZoneOffsetMinutes = null) : IQuery<Result<TdeeInsightModel>>, IUserRequest;
