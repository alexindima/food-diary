using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserCurrentWaistProvider(FoodDiaryDbContext context) : IUserCurrentWaistProvider {
    public async Task<double?> GetCurrentWaistAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.WaistEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.CreatedOnUtc)
            .Select(entry => (double?)entry.CircumferenceCm)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
