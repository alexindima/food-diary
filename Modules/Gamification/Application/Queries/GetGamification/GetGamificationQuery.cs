using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Gamification.Application.Models;

namespace FoodDiary.Modules.Gamification.Application.Queries.GetGamification;

public record GetGamificationQuery(
    Guid? UserId) : IQuery<Result<GamificationModel>>, IUserRequest;
