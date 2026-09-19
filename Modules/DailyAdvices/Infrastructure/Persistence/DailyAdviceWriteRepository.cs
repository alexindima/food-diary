using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;

public sealed class DailyAdviceWriteRepository(DbSet<DailyAdvice> adviceSet) : IDailyAdviceWriteRepository {
    public async Task<IReadOnlyList<DailyAdvice>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await adviceSet.ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DailyAdvice>> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        await adviceSet.Where(advice => advice.GroupId == groupId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public void RemoveRange(IReadOnlyList<DailyAdvice> advices) => adviceSet.RemoveRange(advices);

    public async Task AddRangeAsync(IReadOnlyList<DailyAdvice> advices, CancellationToken cancellationToken = default) =>
        await adviceSet.AddRangeAsync(advices, cancellationToken).ConfigureAwait(false);
}
