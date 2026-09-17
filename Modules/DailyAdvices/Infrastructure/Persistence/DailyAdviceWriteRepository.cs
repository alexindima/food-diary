using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;

public sealed class DailyAdviceWriteRepository(DbSet<DailyAdvice> adviceSet) : IDailyAdviceWriteRepository {
    public async Task AddRangeAsync(IReadOnlyList<DailyAdvice> advices, CancellationToken cancellationToken = default) =>
        await adviceSet.AddRangeAsync(advices, cancellationToken).ConfigureAwait(false);
}
