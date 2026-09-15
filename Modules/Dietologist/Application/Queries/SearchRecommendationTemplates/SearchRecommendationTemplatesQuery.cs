using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.SearchRecommendationTemplates;

public sealed record SearchRecommendationTemplatesQuery(
    Guid? UserId,
    string? Search,
    bool IncludeArchived) : IQuery<Result<IReadOnlyList<RecommendationTemplateModel>>>, IUserRequest;
