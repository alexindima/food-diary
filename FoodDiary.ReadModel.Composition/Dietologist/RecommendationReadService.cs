using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Dietologist;

internal sealed class RecommendationReadService(ICompositionReadContext context) : IRecommendationReadModelRepository {
    public async Task<IReadOnlyList<RecommendationReadModel>> GetByClientReadModelsAsync(
        UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .AsNoTracking()
            .Where(r => r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .Join(context.Users.AsNoTracking(), r => r.DietologistUserId, user => user.Id, (r, user) => new RecommendationReadModel(
                r.Id.Value,
                r.DietologistUserId.Value,
                user.FirstName,
                user.LastName,
                r.Text,
                r.IsRead,
                r.CreatedOnUtc,
                r.ReadAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RecommendationReadModel>> GetByDietologistAndClientReadModelsAsync(
        UserId dietologistUserId, UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .AsNoTracking()
            .Where(r => r.DietologistUserId == dietologistUserId && r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .Join(context.Users.AsNoTracking(), r => r.DietologistUserId, user => user.Id, (r, user) => new RecommendationReadModel(
                r.Id.Value,
                r.DietologistUserId.Value,
                user.FirstName,
                user.LastName,
                r.Text,
                r.IsRead,
                r.CreatedOnUtc,
                r.ReadAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
