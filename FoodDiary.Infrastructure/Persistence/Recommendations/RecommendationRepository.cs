using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recommendations;

public class RecommendationRepository(FoodDiaryDbContext context) : IRecommendationRepository {
    public async Task<IReadOnlyList<Recommendation>> GetByClientAsync(
        UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .AsNoTracking()
            .Include(r => r.DietologistUser)
            .Where(r => r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Recommendation>> GetByDietologistAndClientAsync(
        UserId dietologistUserId, UserId clientUserId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .AsNoTracking()
            .Include(r => r.DietologistUser)
            .Where(r => r.DietologistUserId == dietologistUserId && r.ClientUserId == clientUserId)
            .OrderByDescending(r => r.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Recommendation?> GetByIdAsync(
        RecommendationId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<Recommendation> query = context.Recommendations;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query
            .Include(r => r.DietologistUser)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Recommendation> AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default) {
        context.Recommendations.Add(recommendation);
        await context.SaveChangesAsync(cancellationToken);
        return recommendation;
    }

    public async Task UpdateAsync(Recommendation recommendation, CancellationToken cancellationToken = default) {
        context.Recommendations.Update(recommendation);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(UserId clientUserId, CancellationToken cancellationToken = default) {
        return await context.Recommendations
            .CountAsync(r => r.ClientUserId == clientUserId && !r.IsRead, cancellationToken);
    }
}
