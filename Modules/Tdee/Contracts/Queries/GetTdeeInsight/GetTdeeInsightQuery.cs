using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Tdee.Contracts.Models;

namespace FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;

public record GetTdeeInsightQuery(
    Guid? UserId) : IQuery<Result<TdeeInsightModel>>, IUserRequest;
