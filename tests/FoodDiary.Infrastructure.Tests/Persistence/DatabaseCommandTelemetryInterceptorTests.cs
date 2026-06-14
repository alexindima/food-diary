using System.Diagnostics.Metrics;
using System.Data.Common;
using System.Reflection;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class DatabaseCommandTelemetryInterceptorTests {
    private const string InfrastructureMeterName = "FoodDiary.Infrastructure";

    [Fact]
    public void RecordFailure_EmitsDatabaseFailureMetricWithExpectedTags() {
        long? failureCount = null;
        string? operation = null;
        string? source = null;
        string? errorType = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            failureCount = value;
            operation = GetTagValue(tags, "fooddiary.db.operation");
            source = GetTagValue(tags, "fooddiary.db.source");
            errorType = GetTagValue(tags, "error.type");
        });

        InfrastructureTelemetry.RecordDatabaseCommandFailure("reader", "LinqQuery", "PostgresException");

        Assert.Equal(1, failureCount);
        Assert.Equal("reader", operation);
        Assert.Equal("LinqQuery", source);
        Assert.Equal("PostgresException", errorType);
    }

    [Fact]
    public void RecordStorageOperation_EmitsStorageMetricWithExpectedTags() {
        long? operationCount = null;
        string? operation = null;
        string? outcome = null;
        string? errorType = null;
        using MeterListener listener = CreateInfrastructureListener(
            onFailure: null,
            onStorage: (value, tags) => {
                operationCount = value;
                operation = GetTagValue(tags, "fooddiary.storage.operation");
                outcome = GetTagValue(tags, "fooddiary.storage.outcome");
                errorType = GetTagValue(tags, "error.type");
            });

        InfrastructureTelemetry.RecordStorageOperation("delete", "failed", "IOException");

        Assert.Equal(1, operationCount);
        Assert.Equal("delete", operation);
        Assert.Equal("failed", outcome);
        Assert.Equal("IOException", errorType);
    }

    [Fact]
    public async Task CommandFailed_RecordsFailureFromEventData() {
        long? failureCount = null;
        string? operation = null;
        string? source = null;
        string? errorType = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            failureCount = value;
            operation = GetTagValue(tags, "fooddiary.db.operation");
            source = GetTagValue(tags, "fooddiary.db.source");
            errorType = GetTagValue(tags, "error.type");
        });
        var interceptor = new DatabaseCommandTelemetryInterceptor();
        await using var command = new NpgsqlCommand("select 1");
        CommandErrorEventData eventData = CreateCommandErrorEventData(
            command,
            DbCommandMethod.ExecuteScalar,
            CommandSource.Migrations,
            new TimeoutException());

#pragma warning disable MA0042
        interceptor.CommandFailed(command, eventData);
#pragma warning restore MA0042

        Assert.Equal(1, failureCount);
        Assert.Equal("scalar", operation);
        Assert.Equal("Migrations", source);
        Assert.Equal("timeout", errorType);
    }

    [Fact]
    public async Task CommandFailedAsync_RecordsFailureFromEventData() {
        long? failureCount = null;
        string? operation = null;
        string? source = null;
        string? errorType = null;
        using MeterListener listener = CreateInfrastructureListener((value, tags) => {
            failureCount = value;
            operation = GetTagValue(tags, "fooddiary.db.operation");
            source = GetTagValue(tags, "fooddiary.db.source");
            errorType = GetTagValue(tags, "error.type");
        });
        var interceptor = new DatabaseCommandTelemetryInterceptor();
        await using var command = new NpgsqlCommand("select 1");
        CommandErrorEventData eventData = CreateCommandErrorEventData(
            command,
            DbCommandMethod.ExecuteNonQuery,
            CommandSource.SaveChanges,
            new OperationCanceledException());

        await interceptor.CommandFailedAsync(command, eventData);

        Assert.Equal(1, failureCount);
        Assert.Equal("non_query", operation);
        Assert.Equal("SaveChanges", source);
        Assert.Equal("canceled", errorType);
    }

    [Theory]
    [InlineData((int)DbCommandMethod.ExecuteReader, "reader")]
    [InlineData((int)DbCommandMethod.ExecuteScalar, "scalar")]
    [InlineData((int)DbCommandMethod.ExecuteNonQuery, "non_query")]
    [InlineData(999, "unknown")]
    public void ResolveOperation_MapsCommandMethods(int commandMethod, string expected) {
        string result = InvokePrivateStatic<string>("ResolveOperation", (DbCommandMethod)commandMethod);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveErrorType_WhenExceptionIsTimeout_ReturnsTimeout() {
        string result = InvokePrivateStatic<string>("ResolveErrorType", new TimeoutException());

        Assert.Equal("timeout", result);
    }

    [Fact]
    public void ResolveErrorType_WhenExceptionIsOperationCanceled_ReturnsCanceled() {
        string result = InvokePrivateStatic<string>("ResolveErrorType", new OperationCanceledException());

        Assert.Equal("canceled", result);
    }

    [Fact]
    public void ResolveErrorType_WhenExceptionIsDbException_ReturnsConcreteTypeName() {
        string result = InvokePrivateStatic<string>("ResolveErrorType", new TestDbException());

        Assert.Equal(nameof(TestDbException), result);
    }

    [Fact]
    public void ResolveErrorType_WhenExceptionIsOtherException_ReturnsConcreteTypeName() {
        string result = InvokePrivateStatic<string>("ResolveErrorType", new InvalidOperationException());

        Assert.Equal(nameof(InvalidOperationException), result);
    }

    private static MeterListener CreateInfrastructureListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onFailure,
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>>? onStorage = null) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (!string.Equals(instrument.Meter.Name, InfrastructureMeterName, StringComparison.Ordinal)) {
                return;
            }

            if (instrument.Name is "fooddiary.db.command.failures" or "fooddiary.storage.operations") {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (string.Equals(instrument.Name, "fooddiary.db.command.failures", StringComparison.Ordinal)) {
                onFailure?.Invoke(value, tags);
            } else if (string.Equals(instrument.Name, "fooddiary.storage.operations", StringComparison.Ordinal)) {
                onStorage?.Invoke(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (KeyValuePair<string, object?> tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private static T InvokePrivateStatic<T>(string methodName, params object[] args) {
        MethodInfo method = typeof(DatabaseCommandTelemetryInterceptor).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return (T)method.Invoke(null, args)!;
    }

    private static CommandErrorEventData CreateCommandErrorEventData(
        DbCommand command,
        DbCommandMethod commandMethod,
        CommandSource commandSource,
        Exception exception) =>
        new(
            eventDefinition: null!,
            messageGenerator: static (_, _) => string.Empty,
            connection: command.Connection!,
            command,
            command.CommandText,
            context: null,
            commandMethod,
            commandId: Guid.NewGuid(),
            connectionId: Guid.NewGuid(),
            exception,
            async: false,
            logParameterValues: false,
            startTime: DateTimeOffset.UtcNow,
            duration: TimeSpan.FromMilliseconds(1),
            commandSource);

    [ExcludeFromCodeCoverage]
    private sealed class TestDbException : DbException {
    }
}
