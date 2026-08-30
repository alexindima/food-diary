using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recommendations;

public sealed class RecommendationRepository(FoodDiaryDbContext context) : IRecommendationRepository {
    public async Task<IReadOnlyList<RecommendationReadModel>> GetByClientReadModelsAsync(
        UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .AsNoTracking()
            .Where(r => r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .Select(r => new RecommendationReadModel(
                r.Id.Value,
                r.DietologistUserId.Value,
                r.DietologistUser.FirstName,
                r.DietologistUser.LastName,
                r.Text,
                r.IsRead,
                r.CreatedOnUtc,
                r.ReadAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Recommendation>> GetByClientAsync(
        UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .AsNoTracking()
            .Include(r => r.DietologistUser)
            .Where(r => r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RecommendationReadModel>> GetByDietologistAndClientReadModelsAsync(
        UserId dietologistUserId, UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .AsNoTracking()
            .Where(r => r.DietologistUserId == dietologistUserId && r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .Select(r => new RecommendationReadModel(
                r.Id.Value,
                r.DietologistUserId.Value,
                r.DietologistUser.FirstName,
                r.DietologistUser.LastName,
                r.Text,
                r.IsRead,
                r.CreatedOnUtc,
                r.ReadAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Recommendation>> GetByDietologistAndClientAsync(
        UserId dietologistUserId, UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .AsNoTracking()
            .Include(r => r.DietologistUser)
            .Where(r => r.DietologistUserId == dietologistUserId && r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Recommendation?> GetByIdAsync(
        RecommendationId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<Recommendation> query = context.Recommendations;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query
            .Include(r => r.DietologistUser)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Recommendation> AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default) {
        await context.Recommendations.AddAsync(recommendation, cancellationToken).ConfigureAwait(false);
        return recommendation;
    }

    public async Task UpdateAsync(Recommendation recommendation, CancellationToken cancellationToken = default) {
        context.Recommendations.Update(recommendation);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<int> GetUnreadCountAsync(UserId clientUserId, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .CountAsync(r => r.ClientUserId == clientUserId && !r.IsRead, cancellationToken).ConfigureAwait(false);
    }

}
