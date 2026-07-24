using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.SearchRecommendationTemplates;

public sealed record SearchRecommendationTemplatesQuery(
    Guid? UserId,
    string? Search,
    bool IncludeArchived) : IQuery<Result<IReadOnlyList<RecommendationTemplateModel>>>, IUserRequest;
