using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Tdee.Models;

namespace FoodDiary.Application.Tdee.Queries.GetTdeeInsight;

public record GetTdeeInsightQuery(
    Guid? UserId) : IQuery<Result<TdeeInsightModel>>, IUserRequest;
