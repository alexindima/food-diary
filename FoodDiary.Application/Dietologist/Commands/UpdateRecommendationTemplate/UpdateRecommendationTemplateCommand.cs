using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.UpdateRecommendationTemplate;

public sealed record UpdateRecommendationTemplateCommand(
    Guid? UserId,
    Guid TemplateId,
    string Name,
    string Text) : ICommand<Result<RecommendationTemplateModel>>, IUserRequest;
