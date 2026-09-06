using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Shared;

/// <summary>Top-level transaction runners own a clean unit of work, including any owner capabilities they invoke.</summary>
internal static class SharedTransactionBoundary {
    public static void EnsureCleanEntry(FoodDiaryDbContext context) {
        if (context.ChangeTracker.HasChanges()) {
            throw new InvalidOperationException("A top-level transaction cannot save pending changes from its caller. Enter the transaction before mutating tracked entities.");
        }

        if (context.Database.IsRelational() && context.Database.CurrentTransaction is not null) {
            throw new InvalidOperationException("A top-level transaction cannot be nested in an existing transaction.");
        }
    }
}
