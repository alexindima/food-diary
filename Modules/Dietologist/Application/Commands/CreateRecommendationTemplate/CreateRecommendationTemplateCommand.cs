using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.CreateRecommendationTemplate;

public sealed record CreateRecommendationTemplateCommand(
    Guid? UserId,
    string Name,
    string Text) : ICommand<Result<RecommendationTemplateModel>>, IUserRequest;
