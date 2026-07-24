using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendationTemplate;

public sealed record CreateRecommendationTemplateCommand(
    Guid? UserId,
    string Name,
    string Text) : ICommand<Result<RecommendationTemplateModel>>, IUserRequest;
