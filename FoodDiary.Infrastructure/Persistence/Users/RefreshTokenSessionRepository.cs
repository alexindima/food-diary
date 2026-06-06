using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class RefreshTokenSessionRepository(FoodDiaryDbContext context) : IRefreshTokenSessionRepository {
    public Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.UserRefreshTokenSessions
            .FirstOrDefaultAsync(session => session.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserRefreshTokenSession>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.UserRefreshTokenSessions
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .OrderByDescending(session => session.LastRotatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) {
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) {
        context.UserRefreshTokenSessions.Update(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
