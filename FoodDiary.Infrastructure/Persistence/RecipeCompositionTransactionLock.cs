using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

internal static class RecipeCompositionTransactionLock {
    private const long LockKey = 0x524543495045;

    public static Task AcquireAsync(FoodDiaryDbContext context, CancellationToken cancellationToken) =>
        context.Database.IsRelational()
            ? context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({LockKey})",
                cancellationToken)
            : Task.CompletedTask;
}
