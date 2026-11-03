using FoodDiary.Persistence.PostgreSql;
using FoodDiary.Modules.Ai.PersistenceModel;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Modules.Ai.Infrastructure.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Modules.Ai.Infrastructure.Persistence;

public sealed class AiQuotaRepository(
    DbContextOptions<AiDbContext> contextOptions,
    TimeProvider timeProvider) : IAiQuotaRepository {
    public async Task<AiQuotaReservationStatus> ReserveAsync(
        AiQuotaReservationRequest request,
        CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        ValidateReservation(request, nowUtc);

        return await ExecuteInTransactionAsync(async (context, token) => {
            await EnsurePeriodAsync(context, request, nowUtc, token);
            AiQuotaPeriod period = await GetPeriodForUpdateAsync(
                context,
                request.UserId.Value,
                request.PeriodStartUtc,
                token);
            await ExpirePendingReservationsAsync(context, period, nowUtc, token);
            await context.SaveChangesAsync(token);

            AiQuotaReservation? existing = await context.AiQuotaReservations
                .SingleOrDefaultAsync(x => x.RequestId == request.RequestId, token);
            if (existing is not null) {
                if (!existing.BelongsTo(request.UserId, request.PeriodStartUtc, request.Operation)) {
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
                context.AiQuotaReservations.Add(AiQuotaReservation.Create(
                    request.RequestId, request.UserId, request.PeriodStartUtc, request.Operation,
                    request.InputTokens, request.OutputTokens, request.ExpiresOnUtc, nowUtc));
            } else {
                existing.Reacquire(request.InputTokens, request.OutputTokens, request.ExpiresOnUtc, nowUtc);
            }

            await context.SaveChangesAsync(token);
            return AiQuotaReservationStatus.Acquired;
        }, cancellationToken);
    }

    public async Task ReconcileAsync(
        string requestId,
        AiQuotaUsage usage,
        CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await ExecuteInTransactionAsync(async (context, token) => {
            AiQuotaReservation reservation = await GetReservationForUpdateAsync(context, requestId, token);

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
                token);
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
            context.AiUsages.Add(AiUsage.CreateWithTokens(
                reservation.UserId,
                usage.Operation,
                usage.Model,
                usage.Tokens,
                isTrial: reservation.PeriodStartUtc == AiQuotaReservationRequest.TrialPeriodKeyUtc));
            await context.SaveChangesAsync(token);
        }, cancellationToken);
    }

    public async Task ReleaseAsync(string requestId, CancellationToken cancellationToken = default) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await ExecuteInTransactionAsync(async (context, token) => {
            AiQuotaReservation reservation = await GetReservationForUpdateAsync(context, requestId, token);
            if (reservation.State != AiQuotaReservationState.Pending) {
                return;
            }

            AiQuotaPeriod period = await GetPeriodForUpdateAsync(
                context,
                reservation.UserId.Value,
                reservation.PeriodStartUtc,
                token);
            period.Release(reservation.ReservedInputTokens, reservation.ReservedOutputTokens, nowUtc);
            reservation.Release(nowUtc);
            await context.SaveChangesAsync(token);
        }, cancellationToken);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<AiDbContext, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken) {
        var strategyContext = new AiDbContext(contextOptions);
        await using IAsyncDisposable configuredStrategyContext = strategyContext;
        IExecutionStrategy strategy = strategyContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => {
            var context = new AiDbContext(contextOptions);
            await using IAsyncDisposable configuredContext = context;
            IDbContextTransaction transaction = await context.Database
                .BeginTransactionAsync(cancellationToken);
            await using (transaction) {
                T result = await operation(context, cancellationToken);
                await VerifiedTransactionCommit.CommitAsync(transaction, cancellationToken);
                return result;
            }
        });
    }

    private async Task ExecuteInTransactionAsync(
        Func<AiDbContext, CancellationToken, Task> operation,
        CancellationToken cancellationToken) {
        await ExecuteInTransactionAsync(async (context, token) => {
            await operation(context, token);
            return true;
        }, cancellationToken);
    }

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
            (request.TrialStartedAtUtc is { } trialStartedAt
                ? request.PeriodStartUtc != AiQuotaReservationRequest.TrialPeriodKeyUtc ||
                    trialStartedAt.Kind != DateTimeKind.Utc || trialStartedAt > nowUtc
                : request.PeriodStartUtc.Day != 1) ||
            request.PeriodStartUtc.TimeOfDay != TimeSpan.Zero ||
            request.ExpiresOnUtc.Kind != DateTimeKind.Utc ||
            request.ExpiresOnUtc <= nowUtc) {
            throw new ArgumentException("AI quota reservation is invalid.", nameof(request));
        }
    }

    private static async Task EnsurePeriodAsync(
        AiDbContext context,
        AiQuotaReservationRequest request,
        DateTime nowUtc,
        CancellationToken cancellationToken) {
        bool isTrial = request.TrialStartedAtUtc is not null;
        DateTime usageStartUtc = request.TrialStartedAtUtc ?? request.PeriodStartUtc;
        DateTime usageEndUtc = isTrial ? nowUtc : request.PeriodStartUtc.AddMonths(1);
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
              AND "CreatedOnUtc" >= {usageStartUtc}
              AND "CreatedOnUtc" < {usageEndUtc}
              AND ("IsTrial" IS NULL OR (NOT {isTrial} AND NOT "IsTrial"))
            ON CONFLICT ("UserId", "PeriodStartUtc") DO NOTHING
            """, cancellationToken);
    }

    private static Task<AiQuotaPeriod> GetPeriodForUpdateAsync(
        AiDbContext context,
        Guid userId,
        DateTime periodStartUtc,
        CancellationToken cancellationToken) =>
        context.AiQuotaPeriods
            .FromSqlInterpolated($"SELECT * FROM \"AiQuotaPeriods\" WHERE \"UserId\" = {userId} AND \"PeriodStartUtc\" = {periodStartUtc} FOR UPDATE")
            .SingleAsync(cancellationToken);

    private static Task<AiQuotaReservation> GetReservationForUpdateAsync(
        AiDbContext context,
        string requestId,
        CancellationToken cancellationToken) =>
        context.AiQuotaReservations
            .FromSqlInterpolated($"SELECT * FROM \"AiQuotaReservations\" WHERE \"RequestId\" = {requestId} FOR UPDATE")
            .SingleAsync(cancellationToken);

    private static async Task ExpirePendingReservationsAsync(
        AiDbContext context,
        AiQuotaPeriod period,
        DateTime nowUtc,
        CancellationToken cancellationToken) {
        List<AiQuotaReservation> expired = await context.AiQuotaReservations
            .Where(x => x.UserId == period.UserId &&
                x.PeriodStartUtc == period.PeriodStartUtc &&
                x.State == AiQuotaReservationState.Pending &&
                x.ExpiresOnUtc <= nowUtc)
            .ToListAsync(cancellationToken);
        foreach (AiQuotaReservation reservation in expired) {
            period.ConsumeReserved(
                reservation.ReservedInputTokens,
                reservation.ReservedOutputTokens,
                reservation.ReservedInputTokens,
                reservation.ReservedOutputTokens,
                nowUtc);
            reservation.MarkOrphaned(nowUtc);
        }

        AiQuotaTelemetry.RecordOrphans(expired.Count);
    }
}
