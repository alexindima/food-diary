using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Domain.Entities.Admin;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminImpersonationSessionRepository(FoodDiaryDbContext context) : IAdminImpersonationSessionRepository {
    public async Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default) {
        context.AdminImpersonationSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
    }
}
