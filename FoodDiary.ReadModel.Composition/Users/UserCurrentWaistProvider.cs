using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Users;

public sealed class UserCurrentWaistProvider(ICompositionReadContext context) : IUserCurrentWaistProvider {
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
