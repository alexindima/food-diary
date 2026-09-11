using System.Text.Json;
using System.Runtime.CompilerServices;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.Persistence.Ai;

public sealed class FoodRecognitionJobStore(DbContextOptions<FoodDiaryDbContext> options, TimeProvider timeProvider)
    : IFoodRecognitionJobStore {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<Result<FoodRecognitionJobModel>> CreateAsync(FoodRecognitionJobModel job, CancellationToken cancellationToken) =>
        InTransactionAsync((context, token) => CreateCoreAsync(context, job, token), cancellationToken);

    private static async Task<Result<FoodRecognitionJobModel>> CreateCoreAsync(
        FoodDiaryDbContext context, FoodRecognitionJobModel job, CancellationToken cancellationToken) {
        // Lock task id before owner admission: concurrent conflicting owners cannot race the unique key.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({job.Id.ToString()}, 732))", cancellationToken).ConfigureAwait(false);
        // Serialize admissions per user, including the outstanding-job limit and idempotency check.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({job.UserId.ToString()}, 731))", cancellationToken).ConfigureAwait(false);
        FoodRecognitionJob? existing = await context.Set<FoodRecognitionJob>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == job.Id, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return existing.UserId.Value == job.UserId && existing.ImageAssetId.Value == job.ImageAssetId && string.Equals(existing.Description, job.Description, StringComparison.Ordinal)
                ? Result.Success(ToModel(existing))
                : Result.Failure<FoodRecognitionJobModel>(AiErrors.RecognitionConflict());
        }
        var userId = new UserId(job.UserId);
        int active = await context.Set<FoodRecognitionJob>()
            .CountAsync(x => x.UserId == userId && (x.Status == "Queued" || x.Status == "Running"), cancellationToken).ConfigureAwait(false);
        if (active >= 2) {
            return Result.Failure<FoodRecognitionJobModel>(AiErrors.RecognitionQueueFull());
        }
        context.Set<FoodRecognitionJob>().Add(new FoodRecognitionJob {
            Id = job.Id, UserId = userId, ImageAssetId = new ImageAssetId(job.ImageAssetId), ImageUrl = job.ImageUrl,
            Description = job.Description, CreatedOnUtc = job.CreatedOnUtc, UpdatedOnUtc = job.UpdatedOnUtc,
        });
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(job);
    }

    public async Task<FoodRecognitionJobModel?> GetAsync(Guid userId, Guid jobId, CancellationToken cancellationToken) {
        var context = new FoodDiaryDbContext(options);
        await using ConfiguredAsyncDisposable contextDisposal = context.ConfigureAwait(false);
        var owner = new UserId(userId);
        FoodRecognitionJob? job = await context.Set<FoodRecognitionJob>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == jobId && x.UserId == owner, cancellationToken).ConfigureAwait(false);
        return job is null ? null : ToModel(job);
    }

    public async Task<IReadOnlyList<FoodRecognitionJobModel>> ListAsync(Guid userId, CancellationToken cancellationToken) {
        var context = new FoodDiaryDbContext(options);
        await using ConfiguredAsyncDisposable contextDisposal = context.ConfigureAwait(false);
        var owner = new UserId(userId);
        DateTime cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
        List<FoodRecognitionJob> jobs = await context.Set<FoodRecognitionJob>().AsNoTracking()
            .Where(x => x.UserId == owner && x.CreatedOnUtc >= cutoff)
            .OrderByDescending(x => x.CreatedOnUtc).Take(10).ToListAsync(cancellationToken).ConfigureAwait(false);
        return jobs.Select(ToModel).ToArray();
    }

    public Task<FoodRecognitionJobModel?> ClaimAsync(CancellationToken cancellationToken) =>
        InTransactionAsync(ClaimCoreAsync, cancellationToken);

    private async Task<FoodRecognitionJobModel?> ClaimCoreAsync(FoodDiaryDbContext context, CancellationToken cancellationToken) {
        // Persist Running before any provider call. SKIP LOCKED permits independent worker replicas.
        List<FoodRecognitionJob> jobs = await context.Set<FoodRecognitionJob>().FromSqlRaw(
            """SELECT * FROM "FoodRecognitionJobs" WHERE "Status" = 'Queued' ORDER BY "CreatedOnUtc" LIMIT 1 FOR UPDATE SKIP LOCKED""")
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        FoodRecognitionJob? job = jobs.FirstOrDefault();
        if (job is null) {
            return null;
        }
        job.Status = "Running";
        job.UpdatedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToModel(job);
    }

    public async Task<bool> SaveVisionAsync(Guid jobId, FoodVisionModel vision, CancellationToken cancellationToken) {
        var context = new FoodDiaryDbContext(options);
        await using ConfiguredAsyncDisposable contextDisposal = context.ConfigureAwait(false);
        string json = JsonSerializer.Serialize(vision, JsonOptions);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Set<FoodRecognitionJob>().Where(x => x.Id == jobId && x.Status == "Running")
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.VisionJson, json).SetProperty(x => x.UpdatedOnUtc, now),
                cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task CompleteAsync(Guid jobId, FoodNutritionModel? nutrition, string? errorCode, string? nutritionErrorCode, CancellationToken cancellationToken) {
        var context = new FoodDiaryDbContext(options);
        await using ConfiguredAsyncDisposable contextDisposal = context.ConfigureAwait(false);
        string? json = nutrition is null ? null : JsonSerializer.Serialize(nutrition, JsonOptions);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        string status = errorCode is null ? "Succeeded" : "Failed";
        // Terminal/expired tasks cannot be overwritten by late workers.
        await context.Set<FoodRecognitionJob>().Where(x => x.Id == jobId && x.Status == "Running")
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, status)
                .SetProperty(x => x.NutritionJson, json).SetProperty(x => x.ErrorCode, errorCode)
                .SetProperty(x => x.NutritionErrorCode, nutritionErrorCode).SetProperty(x => x.UpdatedOnUtc, now),
                cancellationToken).ConfigureAwait(false);
    }

    public async Task MaintainAsync(CancellationToken cancellationToken) {
        var context = new FoodDiaryDbContext(options);
        await using ConfiguredAsyncDisposable contextDisposal = context.ConfigureAwait(false);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        DateTime interrupted = now.AddMinutes(-5);
        DateTime queued = now.AddDays(-1);
        DateTime retention = now.AddDays(-7);
        await context.Set<FoodRecognitionJob>()
            .Where(x => (x.Status == "Running" && x.UpdatedOnUtc < interrupted) || (x.Status == "Queued" && x.CreatedOnUtc < queued))
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, "Failed")
                .SetProperty(x => x.ErrorCode, "Ai.RecognitionInterrupted").SetProperty(x => x.UpdatedOnUtc, now), cancellationToken).ConfigureAwait(false);
        await context.Set<FoodRecognitionJob>()
            .Where(x => x.UpdatedOnUtc < retention && (x.Status == "Succeeded" || x.Status == "Failed"))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FoodRecognitionJobUpdate>> GetUpdatesAsync(DateTime sinceUtc, CancellationToken cancellationToken) {
        var context = new FoodDiaryDbContext(options);
        await using ConfiguredAsyncDisposable contextDisposal = context.ConfigureAwait(false);
        return await context.Set<FoodRecognitionJob>().AsNoTracking().Where(x => x.UpdatedOnUtc >= sinceUtc)
            .Select(x => new FoodRecognitionJobUpdate(x.Id, x.UserId.Value)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private static FoodRecognitionJobModel ToModel(FoodRecognitionJob job) => new(
        job.Id, job.UserId.Value, job.ImageAssetId.Value, job.ImageUrl, job.Description, job.Status, job.CreatedOnUtc, job.UpdatedOnUtc,
        job.VisionJson is null ? null : JsonSerializer.Deserialize<FoodVisionModel>(job.VisionJson, JsonOptions),
        job.NutritionJson is null ? null : JsonSerializer.Deserialize<FoodNutritionModel>(job.NutritionJson, JsonOptions),
        job.ErrorCode, job.NutritionErrorCode);

    private async Task<T> InTransactionAsync<T>(Func<FoodDiaryDbContext, CancellationToken, Task<T>> action, CancellationToken cancellationToken) {
        var strategyContext = new FoodDiaryDbContext(options);
        await using ConfiguredAsyncDisposable strategyDisposal = strategyContext.ConfigureAwait(false);
        IExecutionStrategy strategy = strategyContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => {
            var context = new FoodDiaryDbContext(options);
            await using ConfiguredAsyncDisposable contextDisposal = context.ConfigureAwait(false);
            IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable transactionDisposal = transaction.ConfigureAwait(false);
            T result = await action(context, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }).ConfigureAwait(false);
    }
}
