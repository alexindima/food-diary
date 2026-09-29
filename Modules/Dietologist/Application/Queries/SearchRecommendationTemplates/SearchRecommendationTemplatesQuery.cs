using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.SearchRecommendationTemplates;

public sealed record SearchRecommendationTemplatesQuery(
    Guid? UserId,
    string? Search,
    bool IncludeArchived,
    int Page = 1,
    int Limit = 50) : IQuery<Result<IReadOnlyList<RecommendationTemplateModel>>>, IUserRequest;
