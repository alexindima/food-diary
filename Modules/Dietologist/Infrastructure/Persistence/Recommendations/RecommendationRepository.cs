using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recommendations;

public sealed class RecommendationRepository(DbSet<Recommendation> records, IRecommendationReadModelRepository readModels) : IRecommendationRepository {
    public Task<IReadOnlyList<RecommendationReadModel>> GetByClientReadModelsAsync(
        UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) =>
        readModels.GetByClientReadModelsAsync(clientUserId, limit, cancellationToken);

    public async Task<IReadOnlyList<Recommendation>> GetByClientAsync(
        UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await records
            .AsNoTracking()
            .Where(r => r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<RecommendationReadModel>> GetByDietologistAndClientReadModelsAsync(
        UserId dietologistUserId, UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) =>
        readModels.GetByDietologistAndClientReadModelsAsync(dietologistUserId, clientUserId, limit, cancellationToken);

    public async Task<IReadOnlyList<Recommendation>> GetByDietologistAndClientAsync(
        UserId dietologistUserId, UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await records
            .AsNoTracking()
            .Where(r => r.DietologistUserId == dietologistUserId && r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Recommendation?> GetByIdAsync(
        RecommendationId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<Recommendation> query = records;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Recommendation> AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default) {
        await records.AddAsync(recommendation, cancellationToken).ConfigureAwait(false);
        return recommendation;
    }

    public async Task UpdateAsync(Recommendation recommendation, CancellationToken cancellationToken = default) {
        records.Update(recommendation);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<int> GetUnreadCountAsync(UserId clientUserId, CancellationToken cancellationToken = default) {
        return await records
            .CountAsync(r => r.ClientUserId == clientUserId && !r.IsRead, cancellationToken).ConfigureAwait(false);
    }

}
