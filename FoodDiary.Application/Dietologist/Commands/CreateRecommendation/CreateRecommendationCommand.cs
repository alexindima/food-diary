using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendation;

public record CreateRecommendationCommand(
    Guid? UserId,
    Guid ClientUserId,
    string Text) : ICommand<Result<RecommendationModel>>, IUserRequest;
