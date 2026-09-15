using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.UpdateRecommendationTemplate;

public sealed record UpdateRecommendationTemplateCommand(
    Guid? UserId,
    Guid TemplateId,
    string Name,
    string Text) : ICommand<Result<RecommendationTemplateModel>>, IUserRequest;
