using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingStats;

public record GetFastingStatsQuery(Guid? UserId) : IQuery<Result<FastingStatsModel>>, IUserRequest;
