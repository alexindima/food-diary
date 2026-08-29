using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingStats;

public record GetFastingStatsQuery(Guid? UserId) : IQuery<Result<FastingStatsModel>>, IUserRequest;
