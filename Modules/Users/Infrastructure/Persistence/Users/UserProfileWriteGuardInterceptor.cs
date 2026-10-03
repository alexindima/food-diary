using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

/// <summary>Serializes profile writes with account deletion/security changes without updating the account row.</summary>
internal sealed class UserProfileWriteGuardInterceptor : SaveChangesInterceptor {
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default) {
        if (eventData.Context is not UsersDbContext context || !context.Database.IsRelational()) {
            return result;
        }
        Guid[] users = [.. context.ChangeTracker.Entries()
            .Where(entry => (entry.State is EntityState.Added or EntityState.Modified)
                && (entry.Entity is UserPreferences or UserNutritionProfile))
            .Select(entry => entry.Entity is UserPreferences preferences ? preferences.Id.Value : ((UserNutritionProfile)entry.Entity).Id.Value)
            .Distinct().Order()];
        foreach (Guid userId in users) {
            EntityEntry<User>? tracked = context.ChangeTracker.Entries<User>().SingleOrDefault(entry => entry.Entity.Id.Value == userId);
            if (tracked?.State == EntityState.Added) {
                continue;
            }
            if (context.Database.CurrentTransaction is null) {
                throw new InvalidOperationException("Profile writes require the coordinated unit of work transaction.");
            }
            AccountWriteState? state = await context.Database.SqlQuery<AccountWriteState>(
                $"SELECT \"SecurityVersion\", \"DeletedAt\" FROM \"Users\" WHERE \"Id\" = {userId} FOR SHARE")
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (state is null || state.DeletedAt is not null || (tracked is not null
                && state.SecurityVersion != tracked.OriginalValues.GetValue<long>(nameof(User.SecurityVersion)))) {
                throw new DbUpdateConcurrencyException("The account changed before its profile could be saved.");
            }
        }
        return result;
    }

    private sealed record AccountWriteState(long SecurityVersion, DateTime? DeletedAt);
}
