using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.ArchiveRecommendationTemplate;

public sealed record ArchiveRecommendationTemplateCommand(
    Guid? UserId,
    Guid TemplateId) : ICommand<Result>, IUserRequest;
