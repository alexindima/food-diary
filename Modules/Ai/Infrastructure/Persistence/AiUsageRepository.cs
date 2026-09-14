using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Ai.Infrastructure.Persistence;

public sealed class AiUsageRepository(DbSet<AiUsage> usages, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IAiUsageWriteRepository {
    public async Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        await usages.AddAsync(usage, cancellationToken).ConfigureAwait(false);
    }
}
