using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Admin.Infrastructure.Persistence;

public sealed class AdminImpersonationSessionRepository(
    DbSet<AdminImpersonationSession> sessions) : IAdminImpersonationSessionWriteRepository {
    public async Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default) {
        await sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
    }

}
