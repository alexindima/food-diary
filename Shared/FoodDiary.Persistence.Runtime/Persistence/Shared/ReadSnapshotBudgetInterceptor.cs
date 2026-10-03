using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class ReadSnapshotBudgetInterceptor : DbCommandInterceptor {
    internal int? MaximumQueries { get; set; }
    internal int QueryCount { get; set; }

    private void ObserveCommand() {
        if (MaximumQueries is { } maximum && ++QueryCount > maximum) {
            throw new InvalidOperationException("The read snapshot exceeded its database query budget.");
        }
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
        ObserveCommand();
        return ValueTask.FromResult(result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<object> result, CancellationToken cancellationToken = default) {
        ObserveCommand();
        return ValueTask.FromResult(result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default) {
        ObserveCommand();
        return ValueTask.FromResult(result);
    }
}
