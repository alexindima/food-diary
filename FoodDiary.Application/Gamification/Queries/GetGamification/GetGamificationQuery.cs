using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Gamification.Models;

namespace FoodDiary.Application.Gamification.Queries.GetGamification;

public record GetGamificationQuery(
    Guid? UserId) : IQuery<Result<GamificationModel>>, IUserRequest;
