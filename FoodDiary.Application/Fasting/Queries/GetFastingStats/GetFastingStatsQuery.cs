using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingStats;

public record GetFastingStatsQuery(Guid? UserId) : IQuery<Result<FastingStatsModel>>, IUserRequest;
