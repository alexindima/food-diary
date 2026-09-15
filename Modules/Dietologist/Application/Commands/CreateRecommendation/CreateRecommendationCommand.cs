using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Commands.CreateRecommendation;

public record CreateRecommendationCommand(
    Guid? UserId,
    Guid ClientUserId,
    string Text) : ICommand<Result<RecommendationModel>>, IUserRequest;
