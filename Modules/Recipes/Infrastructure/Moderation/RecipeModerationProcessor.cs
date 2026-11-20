using System.Globalization;
using FoodDiary.Results;
using System.Text.Json;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

internal sealed class RecipeModerationProcessor(
    RecipePublicationStore store,
    IRecipePublicationApplier applier,
    IRecipeImagePromotionService imagePromotion,
    IEnumerable<IRecipeModerationProvider> providers,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<RecipeModerationProcessor> logger) : IRecipeModerationProcessor {
    private const int MaximumAttempts = 6;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public async Task<bool> ProcessNextAsync(bool observation, CancellationToken cancellationToken) {
        if (!store.Enabled || configuration["RecipeModeration:ProcessingEnabled"] is "false" or "False" or "0") {
            return false;
        }

        RecipePublicationRevision? revision = await store.ExecuteAsync(token => ClaimAsync(observation, token), cancellationToken);
        if (revision is null) {
            return false;
        }

        if (!observation && revision.Status is RecipePublicationStatuses.Approved) {
            await PublishAsync(revision, cancellationToken);
            return true;
        }

        await CheckAsync(revision, observation, cancellationToken);
        return true;
    }

    private async Task PublishAsync(RecipePublicationRevision revision, CancellationToken cancellationToken) {
        store.Context.ChangeTracker.Clear();
        if (revision.LeaseToken is not { } leaseToken) {
            return;
        }

        var claim = new RecipePublicationClaim(revision.Id, leaseToken);
        if (!await PromotePhotosAsync(revision, claim, cancellationToken)) {
            return;
        }

        Result result = await applier.ApplyAsync(revision.RecipeId, (UserId)revision.OwnerId, claim, revision.ChangeJson, cancellationToken);
        if (result.IsFailure) {
            await FinishAsync(revision, observation: false, current => {
                current.Status = RecipePublicationStatuses.NeedsReview;
                current.Reason = "UnableToApply";
                current.NextAttemptAt = null;
            }, cancellationToken);
        }
    }

    private async Task<bool> PromotePhotosAsync(
        RecipePublicationRevision revision, RecipePublicationClaim claim, CancellationToken cancellationToken) {
        if (revision.PromotionAttempts > MaximumAttempts) {
            await DelayPhotoPromotionAsync(revision, cancellationToken);
            return false;
        }

        RecipeModerationContent content = ReadContent(revision);
        TimeSpan remaining = revision.LeaseUntil.GetValueOrDefault() - clock.GetUtcNow() - TimeSpan.FromSeconds(30);
        if (remaining <= TimeSpan.Zero) {
            return false;
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(remaining);
        try {
            foreach (Guid assetId in content.Images.Select(image => image.AssetId).OfType<Guid>().Distinct()) {
                if (!await store.CanApplyAsync(revision.RecipeId, claim, deadline.Token)) {
                    return false;
                }

                Result promoted = await imagePromotion.PromoteAsync((ImageAssetId)assetId, (UserId)revision.OwnerId, deadline.Token);
                if (promoted.IsFailure) {
                    await DelayPhotoPromotionAsync(revision, cancellationToken);
                    return false;
                }
            }
            return true;
        } catch (OperationCanceledException) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
            await DelayPhotoPromotionAsync(revision, cancellationToken);
            return false;
        }
    }

    private Task<bool> DelayPhotoPromotionAsync(RecipePublicationRevision claim, CancellationToken cancellationToken) =>
        FinishAsync(claim, observation: false, revision => {
            revision.Reason = RecipePublicationStatuses.PhotoPromotionUnavailable;
            if (revision.PromotionAttempts < MaximumAttempts) {
                revision.NextAttemptAt = clock.GetUtcNow().Add(RetryDelay(revision.PromotionAttempts));
            } else {
                revision.Status = RecipePublicationStatuses.CheckDelayed;
                revision.NextAttemptAt = null;
            }
        }, cancellationToken);

    private async Task CheckAsync(RecipePublicationRevision revision, bool observation, CancellationToken cancellationToken) {
        IRecipeModerationProvider? provider = providers.FirstOrDefault(item => string.Equals(item.Name, observation ? "Mistral" : "OpenAi", StringComparison.Ordinal));
        if (provider?.IsAvailable != true) {
            await FinishAsync(revision, observation, current => {
                if (observation) {
                    current.ShadowNextAttemptAt = null;
                } else {
                    current.Status = RecipePublicationStatuses.CheckDelayed;
                    current.Reason = "ProviderUnavailable";
                    current.NextAttemptAt = null;
                }
            }, cancellationToken);
            return;
        }

        RecipeModerationContent content = ReadContent(revision);
        if (observation) {
            content = content with { Images = [] };
        }

        int cost = observation ? 1 : (string.IsNullOrWhiteSpace(content.Text) ? 0 : 1) + content.Images.Count;
        if (cost > ReadLimit(provider.Name, "RequestsPerMinute", 200) || cost > ReadLimit(provider.Name, "RequestsPerDay", 4500)) {
            await FinishAsync(revision, observation, current => DelayCheck(current, observation,
                new RecipeModerationProviderException("The request exceeds the configured moderation budget.", retryable: false)), cancellationToken);
            return;
        }

        DateTimeOffset? permittedAt = await ReserveRequestsAsync(provider.Name, cost, cancellationToken);
        if (permittedAt is { } next) {
            await FinishAsync(revision, observation, current => {
                if (observation) {
                    current.ShadowAttempts--;
                    current.ShadowNextAttemptAt = next;
                } else {
                    current.Attempts--;
                    current.NextAttemptAt = next;
                }
            }, cancellationToken);
            return;
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(LeaseDuration - TimeSpan.FromSeconds(30));
        try {
            RecipeModerationCheck check = await provider.CheckAsync(content, deadline.Token);
            await FinishAsync(revision, observation, async (current, token) => {
                if (observation) {
                    current.ShadowResult = JsonSerializer.Serialize(check);
                    current.ShadowNextAttemptAt = null;
                    return;
                }

                RecipeModerationPolicyInput rules = await store.GetRulesAsync(current.PolicyVersionId, token);
                bool review = check.Signals.Any(signal => signal.Categories.Any(category => category.Value &&
                    (string.Equals(signal.Field, "text", StringComparison.OrdinalIgnoreCase)
                        ? rules.TextReviewCategories : rules.ImageReviewCategories).Contains(category.Key, StringComparer.Ordinal)));
                current.PrimaryResult = JsonSerializer.Serialize(check);
                current.Status = review ? RecipePublicationStatuses.NeedsReview : RecipePublicationStatuses.Approved;
                current.Reason = review ? "Flagged" : null;
                current.NextAttemptAt = review ? null : clock.GetUtcNow();
            }, cancellationToken);
        } catch (OperationCanceledException) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
            await FinishAsync(revision, observation, current => DelayCheck(current, observation,
                new RecipeModerationProviderException("The moderation request timed out.", retryable: true)), cancellationToken);
        } catch (RecipeModerationProviderException error) {
            logger.LogWarning("Moderation check failed for revision {RevisionId}, provider {Provider}, retryable {Retryable}.",
                revision.Id, provider.Name, error.Retryable);
            await FinishAsync(revision, observation, current => DelayCheck(current, observation, error), cancellationToken);
        }
    }

    private void DelayCheck(RecipePublicationRevision revision, bool observation, RecipeModerationProviderException error) {
        int attempts = observation ? revision.ShadowAttempts : revision.Attempts;
        DateTimeOffset? next = null;
        if (error.Retryable && attempts < MaximumAttempts) {
            TimeSpan backoff = RetryDelay(attempts);
            TimeSpan delay = error.RetryAfter is { } retryAfter && retryAfter > backoff ? retryAfter : backoff;
            next = clock.GetUtcNow().Add(delay);
        }

        if (observation) {
            revision.ShadowNextAttemptAt = next;
        } else {
            revision.NextAttemptAt = next;
            if (next is null) {
                revision.Status = error.RequiresContentChanges ? RecipePublicationStatuses.ChangesRequested : RecipePublicationStatuses.CheckDelayed;
                revision.Reason = error.RequiresContentChanges ? "ReplaceImage" : "CheckUnavailable";
            }
        }
    }

    private async Task<RecipePublicationRevision?> ClaimAsync(bool observation, CancellationToken cancellationToken) {
        DateTimeOffset now = clock.GetUtcNow();
        FormattableString sql = observation
            ? (FormattableString)$"""
                SELECT *, xmin FROM "RecipePublicationRevisions"
                WHERE "ShadowNextAttemptAt" <= {now} AND ("ShadowLeaseUntil" IS NULL OR "ShadowLeaseUntil" <= {now})
                ORDER BY "ShadowNextAttemptAt", "CreatedAt", "Id" LIMIT 1 FOR UPDATE SKIP LOCKED
                """
            : (FormattableString)$"""
                SELECT *, xmin FROM "RecipePublicationRevisions"
                WHERE "IsCurrent" AND "NextAttemptAt" <= {now} AND ("LeaseUntil" IS NULL OR "LeaseUntil" <= {now})
                ORDER BY "NextAttemptAt", "CreatedAt", "Id" LIMIT 1 FOR UPDATE SKIP LOCKED
                """;
        List<RecipePublicationRevision> rows = await store.Revisions.FromSqlInterpolated(sql).ToListAsync(cancellationToken);
        RecipePublicationRevision? revision = rows.SingleOrDefault();
        if (revision is null) {
            return null;
        }

        if (observation) {
            revision.ShadowLeaseToken = Guid.NewGuid();
            revision.ShadowLeaseUntil = now.Add(LeaseDuration);
            revision.ShadowAttempts++;
        } else {
            revision.LeaseToken = Guid.NewGuid();
            revision.LeaseUntil = now.Add(LeaseDuration);
            if (revision.Status is RecipePublicationStatuses.Pending) {
                revision.Attempts++;
            } else {
                revision.PromotionAttempts++;
            }
        }

        return revision;
    }

    private Task<bool> FinishAsync(RecipePublicationRevision claim, bool observation, Action<RecipePublicationRevision> change,
        CancellationToken cancellationToken) => FinishAsync(claim, observation, (current, _) => {
            change(current);
            return Task.CompletedTask;
        }, cancellationToken);

    private Task<bool> FinishAsync(RecipePublicationRevision claim, bool observation,
        Func<RecipePublicationRevision, CancellationToken, Task> change, CancellationToken cancellationToken) {
        Guid? lease = observation ? claim.ShadowLeaseToken : claim.LeaseToken;
        return store.ExecuteAsync(async token => {
            store.Context.ChangeTracker.Clear();
            RecipePublicationRevision? current = await store.Revisions.SingleOrDefaultAsync(item => item.Id == claim.Id, token);
            DateTimeOffset now = clock.GetUtcNow();
            if (current is null || lease is null || (observation
                ? current.ShadowLeaseToken != lease || current.ShadowLeaseUntil <= now
                : !current.IsCurrent || current.LeaseToken != lease || current.LeaseUntil <= now)) {
                return false;
            }

            await change(current, token);
            if (observation) {
                current.ShadowLeaseToken = null;
                current.ShadowLeaseUntil = null;
            } else {
                current.LeaseToken = null;
                current.LeaseUntil = null;
                current.UpdatedAt = now;
            }

            return true;
        }, cancellationToken);
    }

    private Task<DateTimeOffset?> ReserveRequestsAsync(string provider, int cost, CancellationToken cancellationToken) =>
        store.ExecuteAsync<DateTimeOffset?>(async token => {
            DateTimeOffset now = clock.GetUtcNow();
            int perMinute = ReadLimit(provider, "RequestsPerMinute", 200);
            int perDay = ReadLimit(provider, "RequestsPerDay", 4500);
            DateTimeOffset day = new(now.UtcDateTime.Date, TimeSpan.Zero);
            DateTimeOffset minute = day.AddMinutes((now.Hour * 60) + now.Minute);
            if (!await TryReserveAsync(provider, day, 86400, cost, perDay, token)) {
                return day.AddDays(1);
            }

            if (!await TryReserveAsync(provider, minute, 60, cost, perMinute, token)) {
                await store.Context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE "RecipeModerationBudgets" SET "Requests" = "Requests" - {cost}
                    WHERE "Provider" = {provider} AND "WindowStart" = {day} AND "WindowSeconds" = 86400
                    """, token);
                return minute.AddMinutes(1);
            }

            await store.Context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM "RecipeModerationBudgets" WHERE "Provider" = {provider} AND "WindowStart" < {day.AddDays(-1)}
                """, token);
            return null;
        }, cancellationToken);

    private async Task<bool> TryReserveAsync(
        string provider, DateTimeOffset start, int windowSeconds, int cost, int limit, CancellationToken cancellationToken) =>
        cost <= limit && await store.Context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "RecipeModerationBudgets" ("Provider", "WindowStart", "WindowSeconds", "Requests")
            VALUES ({provider}, {start}, {windowSeconds}, {cost})
            ON CONFLICT ("Provider", "WindowStart", "WindowSeconds")
            DO UPDATE SET "Requests" = "RecipeModerationBudgets"."Requests" + EXCLUDED."Requests"
            WHERE "RecipeModerationBudgets"."Requests" + EXCLUDED."Requests" <= {limit}
            """, cancellationToken) == 1;

    private int ReadLimit(string provider, string limit, int fallback) =>
        int.TryParse(configuration[$"RecipeModeration:{provider}:{limit}"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) && result > 0 ? result : fallback;

    private static RecipeModerationContent ReadContent(RecipePublicationRevision revision) =>
        JsonSerializer.Deserialize<RecipeModerationContent>(revision.ContentJson)
            ?? throw new InvalidOperationException("The moderation submission has no content.");

    private static TimeSpan RetryDelay(int attempts) =>
        TimeSpan.FromSeconds((Math.Pow(2, attempts) * 5) + Random.Shared.Next(0, 5));
}
