using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Infrastructure.Services;

internal sealed class DatabaseCommandTelemetryInterceptor : DbCommandInterceptor {
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData) {
        RecordFailure(eventData, eventData.Exception);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default) {
        RecordFailure(eventData, eventData.Exception);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    internal static void RecordFailure(CommandErrorEventData eventData, Exception exception) {
        InfrastructureTelemetry.RecordDatabaseCommandFailure(
            ResolveOperation(eventData.ExecuteMethod),
            eventData.CommandSource.ToString(),
            ResolveErrorType(exception));
    }

    private static string ResolveOperation(DbCommandMethod commandMethod) {
        return commandMethod switch {
            DbCommandMethod.ExecuteReader => "reader",
            DbCommandMethod.ExecuteScalar => "scalar",
            DbCommandMethod.ExecuteNonQuery => "non_query",
            _ => "unknown"
        };
    }

    private static string ResolveErrorType(Exception exception) {
        return exception switch {
            TimeoutException => "timeout",
            OperationCanceledException => "canceled",
            DbException dbException => dbException.GetType().Name,
            _ => exception.GetType().Name
        };
    }
}
