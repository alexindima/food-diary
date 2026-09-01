using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserCurrentWeightProvider(FoodDiaryDbContext context) : IUserCurrentWeightProvider {
    public async Task<double?> GetCurrentWeightAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.WeightEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.CreatedOnUtc)
            .Select(entry => (double?)entry.WeightKg)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
