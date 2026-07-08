using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingStats;

public record GetFastingStatsQuery(Guid? UserId) : IQuery<Result<FastingStatsModel>>, IUserRequest;
