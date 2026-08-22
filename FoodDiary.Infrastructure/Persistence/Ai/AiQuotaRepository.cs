using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Runtime.CompilerServices;

namespace FoodDiary.Infrastructure.Persistence.Ai;

public sealed class AiQuotaRepository(
    DbContextOptions<FoodDiaryDbContext> contextOptions,
    TimeProvider timeProvider) : IAiQuotaRepository {
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public async Task<AiQuotaReservationStatus> ReserveAsync(
        AiQuotaReservationRequest request,
        CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        ValidateReservation(request, nowUtc);

        return await ExecuteInTransactionAsync(async (context, token) => {
            await EnsurePeriodAsync(context, request, nowUtc, token).ConfigureAwait(false);
            AiQuotaPeriod period = await GetPeriodForUpdateAsync(
                context,
                request.UserId.Value,
                request.PeriodStartUtc,
                token).ConfigureAwait(false);
            await ExpirePendingReservationsAsync(context, period, nowUtc, token).ConfigureAwait(false);
            await context.SaveChangesAsync(token).ConfigureAwait(false);

            AiQuotaReservation? existing = await context.AiQuotaReservations
                .SingleOrDefaultAsync(x => x.RequestId == request.RequestId, token)
                .ConfigureAwait(false);
            if (existing is not null) {
                if (!existing.BelongsTo(request)) {
                    return AiQuotaReservationStatus.Duplicate;
                }

                switch (existing.State) {
                    case AiQuotaReservationState.Pending:
                        return AiQuotaReservationStatus.InProgress;
                    case AiQuotaReservationState.Completed:
                    case AiQuotaReservationState.Orphaned:
                        return AiQuotaReservationStatus.Duplicate;
                    case AiQuotaReservationState.Released:
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported AI quota reservation state.");
                }
            }

            if (!period.CanReserve(request.InputTokens, request.OutputTokens, request.InputTokenLimit, request.OutputTokenLimit)) {
                return AiQuotaReservationStatus.QuotaExceeded;
            }

            period.Reserve(request.InputTokens, request.OutputTokens, nowUtc);
            if (existing is null) {
                context.AiQuotaReservations.Add(AiQuotaReservation.Create(request, nowUtc));
            } else {
                existing.Reacquire(request, nowUtc);
            }

            await context.SaveChangesAsync(token).ConfigureAwait(false);
            return AiQuotaReservationStatus.Acquired;
        }, cancellationToken).ConfigureAwait(false);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public async Task ReconcileAsync(
        string requestId,
        AiQuotaUsage usage,
        CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await ExecuteInTransactionAsync(async (context, token) => {
            AiQuotaReservation reservation = await GetReservationForUpdateAsync(context, requestId, token).ConfigureAwait(false);

            if (reservation.State == AiQuotaReservationState.Completed) {
                return;
            }

            if (reservation.State is not (AiQuotaReservationState.Pending or AiQuotaReservationState.Orphaned)) {
                throw new InvalidOperationException("Only pending or orphaned AI quota reservations can be reconciled.");
            }

            if (!string.Equals(reservation.Operation, usage.Operation, StringComparison.Ordinal) ||
                usage.InputTokens < 0 ||
                usage.OutputTokens < 0 ||
                usage.TotalTokens < (long)usage.InputTokens + usage.OutputTokens ||
                usage.InputTokens > reservation.ReservedInputTokens ||
                usage.OutputTokens > reservation.ReservedOutputTokens) {
                throw new InvalidOperationException("AI usage cannot be reconciled outside its reserved token budget.");
            }

            AiQuotaPeriod period = await GetPeriodForUpdateAsync(
                context,
                reservation.UserId.Value,
                reservation.PeriodStartUtc,
                token).ConfigureAwait(false);
            if (reservation.State == AiQuotaReservationState.Pending) {
                period.ConsumeReserved(
                    reservation.ReservedInputTokens,
                    reservation.ReservedOutputTokens,
                    usage.InputTokens,
                    usage.OutputTokens,
                    nowUtc);
            } else {
                period.ReconcileOrphan(
                    reservation.ReservedInputTokens,
                    reservation.ReservedOutputTokens,
                    usage.InputTokens,
                    usage.OutputTokens,
                    nowUtc);
            }

            reservation.Complete(usage.InputTokens, usage.OutputTokens, nowUtc);
            context.AiUsages.Add(AiUsage.Create(
                reservation.UserId,
                usage.Operation,
                usage.Model,
                usage.InputTokens,
                usage.OutputTokens,
                usage.TotalTokens));
            await context.SaveChangesAsync(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(string requestId, CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await ExecuteInTransactionAsync(async (context, token) => {
            AiQuotaReservation reservation = await GetReservationForUpdateAsync(context, requestId, token).ConfigureAwait(false);
            if (reservation.State != AiQuotaReservationState.Pending) {
                return;
            }

            AiQuotaPeriod period = await GetPeriodForUpdateAsync(
                context,
                reservation.UserId.Value,
                reservation.PeriodStartUtc,
                token).ConfigureAwait(false);
            period.Release(reservation.ReservedInputTokens, reservation.ReservedOutputTokens, nowUtc);
            reservation.Release(nowUtc);
            await context.SaveChangesAsync(token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<FoodDiaryDbContext, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken) {
        var strategyContext = new FoodDiaryDbContext(contextOptions);
        await using ConfiguredAsyncDisposable configuredStrategyContext = strategyContext.ConfigureAwait(false);
        IExecutionStrategy strategy = strategyContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => {
            var context = new FoodDiaryDbContext(contextOptions);
            await using ConfiguredAsyncDisposable configuredContext = context.ConfigureAwait(false);
            IDbContextTransaction transaction = await context.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                T result = await operation(context, cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
        }).ConfigureAwait(false);
    }

    private async Task ExecuteInTransactionAsync(
        Func<FoodDiaryDbContext, CancellationToken, Task> operation,
        CancellationToken cancellationToken) {
        await ExecuteInTransactionAsync(async (context, token) => {
            await operation(context, token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static void ValidateReservation(AiQuotaReservationRequest request, DateTime nowUtc) {
        if (request.RequestId.Length != 64 ||
            !request.RequestId.All(static character => char.IsAsciiHexDigit(character)) ||
            string.IsNullOrWhiteSpace(request.Operation) ||
            request.Operation.Length > 32 ||
            request.InputTokens <= 0 ||
            request.OutputTokens <= 0 ||
            request.InputTokenLimit < 0 ||
            request.OutputTokenLimit < 0 ||
            request.PeriodStartUtc.Kind != DateTimeKind.Utc ||
            request.PeriodStartUtc.Day != 1 ||
            request.PeriodStartUtc.TimeOfDay != TimeSpan.Zero ||
            request.ExpiresOnUtc.Kind != DateTimeKind.Utc ||
            request.ExpiresOnUtc <= nowUtc) {
            throw new ArgumentException("AI quota reservation is invalid.", nameof(request));
        }
    }

    private static async Task EnsurePeriodAsync(
        FoodDiaryDbContext context,
        AiQuotaReservationRequest request,
        DateTime nowUtc,
        CancellationToken cancellationToken) {
        DateTime periodEndUtc = request.PeriodStartUtc.AddMonths(1);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "AiQuotaPeriods"
                ("UserId", "PeriodStartUtc", "ConsumedInputTokens", "ConsumedOutputTokens", "ReservedInputTokens", "ReservedOutputTokens", "UpdatedOnUtc")
            SELECT
                {request.UserId.Value},
                {request.PeriodStartUtc},
                COALESCE(SUM("InputTokens"), 0)::bigint,
                COALESCE(SUM("OutputTokens"), 0)::bigint,
                0,
                0,
                {nowUtc}
            FROM "AiUsages"
            WHERE "UserId" = {request.UserId.Value}
              AND "CreatedOnUtc" >= {request.PeriodStartUtc}
              AND "CreatedOnUtc" < {periodEndUtc}
            ON CONFLICT ("UserId", "PeriodStartUtc") DO NOTHING
            """, cancellationToken).ConfigureAwait(false);
    }

    private static Task<AiQuotaPeriod> GetPeriodForUpdateAsync(
        FoodDiaryDbContext context,
        Guid userId,
        DateTime periodStartUtc,
        CancellationToken cancellationToken) =>
        context.AiQuotaPeriods
            .FromSqlInterpolated($"SELECT * FROM \"AiQuotaPeriods\" WHERE \"UserId\" = {userId} AND \"PeriodStartUtc\" = {periodStartUtc} FOR UPDATE")
            .SingleAsync(cancellationToken);

    private static Task<AiQuotaReservation> GetReservationForUpdateAsync(
        FoodDiaryDbContext context,
        string requestId,
        CancellationToken cancellationToken) =>
        context.AiQuotaReservations
            .FromSqlInterpolated($"SELECT * FROM \"AiQuotaReservations\" WHERE \"RequestId\" = {requestId} FOR UPDATE")
            .SingleAsync(cancellationToken);

    private static async Task ExpirePendingReservationsAsync(
        FoodDiaryDbContext context,
        AiQuotaPeriod period,
        DateTime nowUtc,
        CancellationToken cancellationToken) {
        List<AiQuotaReservation> expired = await context.AiQuotaReservations
            .Where(x => x.UserId == period.UserId &&
                x.PeriodStartUtc == period.PeriodStartUtc &&
                x.State == AiQuotaReservationState.Pending &&
                x.ExpiresOnUtc <= nowUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (AiQuotaReservation reservation in expired) {
            period.ConsumeReserved(
                reservation.ReservedInputTokens,
                reservation.ReservedOutputTokens,
                reservation.ReservedInputTokens,
                reservation.ReservedOutputTokens,
                nowUtc);
            reservation.MarkOrphaned(nowUtc);
        }

        InfrastructureTelemetry.RecordAiQuotaOrphans(expired.Count);
    }
}
