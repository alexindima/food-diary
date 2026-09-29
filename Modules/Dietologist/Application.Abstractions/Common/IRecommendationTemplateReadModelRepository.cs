using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationTemplateReadModelRepository {
    Task<IReadOnlyList<RecommendationTemplateReadModel>> SearchAsync(
        UserId dietologistUserId,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    async Task<IReadOnlyList<RecommendationTemplateReadModel>> SearchAsync(
        UserId dietologistUserId,
        string? search,
        bool includeArchived,
        int page,
        int limit,
        CancellationToken cancellationToken = default) =>
        (await SearchAsync(dietologistUserId, search, includeArchived, cancellationToken).ConfigureAwait(false))
            .Skip((page - 1) * limit).Take(limit).ToList();
}
