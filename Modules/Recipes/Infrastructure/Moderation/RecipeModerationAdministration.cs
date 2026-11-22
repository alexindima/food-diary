using FoodDiary.Results;
using System.Text.Json;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

internal sealed class RecipeModerationAdministration(
    RecipePublicationStore store,
    IImageAssetContentService imageContent,
    TimeProvider clock) : IRecipeModerationAdministration {
    public async Task<RecipeModerationPage> ListAsync(string? status, int page, int limit, CancellationToken cancellationToken) {
        page = Math.Clamp(page, 1, 10_000);
        limit = Math.Clamp(limit, 1, 100);
        IQueryable<RecipePublicationRevision> revisions = store.Revisions.AsNoTracking();
        IQueryable<RecipePublicationRevision> selected = status switch {
            null or "" => revisions,
            RecipePublicationStatuses.Pending => revisions.Where(revision =>
                revision.Status == RecipePublicationStatuses.Pending || revision.Status == RecipePublicationStatuses.Approved),
            _ => revisions.Where(revision => revision.Status == status),
        };
        int total = await selected.CountAsync(cancellationToken);
        List<RecipePublicationRevision> items = await selected.OrderByDescending(revision => revision.CreatedAt).ThenBy(revision => revision.Id)
            .Skip((page - 1) * limit).Take(limit).ToListAsync(cancellationToken);
        Dictionary<string, int> counts = await revisions.Where(revision => revision.IsCurrent)
            .GroupBy(revision => revision.Status).Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, StringComparer.Ordinal, cancellationToken);
        return new([.. items.Select(ToItem)], total, page, limit,
            counts.GetValueOrDefault(RecipePublicationStatuses.Pending) + counts.GetValueOrDefault(RecipePublicationStatuses.Approved),
            counts.GetValueOrDefault(RecipePublicationStatuses.NeedsReview), counts.GetValueOrDefault(RecipePublicationStatuses.CheckDelayed));
    }

    public async Task<RecipeModerationItem?> GetAsync(Guid revisionId, CancellationToken cancellationToken) {
        RecipePublicationRevision? revision = await store.Revisions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == revisionId, cancellationToken);
        return revision is null ? null : ToItem(revision);
    }

    public async Task<string?> GetImageAsync(Guid revisionId, Guid assetId, CancellationToken cancellationToken) {
        RecipePublicationRevision? revision = await store.Revisions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == revisionId, cancellationToken);
        if (revision is null || !ReadContent(revision).Images.Any(image => image.AssetId == assetId)) {
            return null;
        }

        Result<string> result = await imageContent.GetDataUrlAsync((ImageAssetId)assetId, (UserId)revision.OwnerId, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    public Task<RecipeModerationActionOutcome> ResolveAsync(
        Guid revisionId, Guid actorId, bool approve, string? reason, CancellationToken cancellationToken) {
        reason = reason?.Trim();
        if (reason?.Length > 2000 || (!approve && string.IsNullOrEmpty(reason))) {
            throw new ArgumentException("Provide a reason of up to 2000 characters when requesting changes.", nameof(reason));
        }

        return ChangeAsync(revisionId, RecipePublicationStatuses.NeedsReview, revision => {
            revision.Status = approve ? RecipePublicationStatuses.Approved : RecipePublicationStatuses.ChangesRequested;
            revision.Reason = reason;
            revision.ReviewedBy = actorId;
            revision.NextAttemptAt = approve ? clock.GetUtcNow() : null;
        }, cancellationToken);
    }

    public Task<RecipeModerationActionOutcome> RetryAsync(Guid revisionId, Guid actorId, CancellationToken cancellationToken) =>
        ChangeAsync(revisionId, RecipePublicationStatuses.CheckDelayed, revision => {
            if (string.Equals(revision.Reason, RecipePublicationStatuses.PhotoPromotionUnavailable, StringComparison.Ordinal)) {
                revision.Status = RecipePublicationStatuses.Approved;
                revision.PromotionAttempts = 0;
            } else {
                revision.Status = RecipePublicationStatuses.Pending;
                revision.Attempts = 0;
                revision.ReviewedBy = actorId;
            }
            revision.Reason = null;
            revision.NextAttemptAt = clock.GetUtcNow();
        }, cancellationToken);

    public async Task<RecipeModerationPolicyView> GetPolicyAsync(CancellationToken cancellationToken) {
        List<RecipePublicationPolicy> policies = await store.Policies.AsNoTracking().OrderByDescending(policy => policy.PublishedAt)
            .ThenByDescending(policy => policy.Id).ToListAsync(cancellationToken);
        RecipeModerationPolicyVersion[] versions = [.. policies.Select(RecipePublicationStore.ToPolicyVersion), RecipeModerationPolicyRules.Default];
        return new(versions[0], versions, store.MistralAvailable);
    }

    public async Task<RecipeModerationPolicyView> PublishPolicyAsync(
        Guid actorId, RecipeModerationPolicyInput input, CancellationToken cancellationToken) {
        RecipeModerationPolicyInput rules = RecipeModerationPolicyRules.Validate(input);
        await store.ExecuteAsync(token => SavePolicyAsync(actorId, rules, token), cancellationToken);
        return await GetPolicyAsync(cancellationToken);
    }

    public async Task<RecipeModerationPolicyView?> RestorePolicyAsync(Guid versionId, Guid actorId, CancellationToken cancellationToken) {
        RecipeModerationPolicyVersion? version = (await GetPolicyAsync(cancellationToken)).History
            .SingleOrDefault(item => item.Id == versionId);
        return version is null ? null : await PublishPolicyAsync(actorId, version.Rules, cancellationToken);
    }

    private async Task<Guid> SavePolicyAsync(Guid actorId, RecipeModerationPolicyInput rules, CancellationToken cancellationToken) {
        RecipeModerationPolicyVersion active = await store.GetActivePolicyAsync(cancellationToken);
        DateTimeOffset now = clock.GetUtcNow();
        var policy = new RecipePublicationPolicy {
            Id = Guid.NewGuid(),
            PublishedAt = now > active.PublishedAt ? now : active.PublishedAt.AddMicroseconds(1),
            PublishedBy = actorId,
            RulesJson = JsonSerializer.Serialize(rules),
        };
        store.Policies.Add(policy);
        return policy.Id;
    }

    private Task<RecipeModerationActionOutcome> ChangeAsync(
        Guid id, string expectedStatus, Action<RecipePublicationRevision> change, CancellationToken cancellationToken) =>
        store.ExecuteAsync(async token => {
            RecipePublicationRevision? revision = await store.Revisions.SingleOrDefaultAsync(item => item.Id == id, token);
            if (revision is null) {
                return RecipeModerationActionOutcome.NotFound;
            }

            if (!revision.IsCurrent || !string.Equals(revision.Status, expectedStatus, StringComparison.Ordinal)) {
                return RecipeModerationActionOutcome.Conflict;
            }

            change(revision);
            revision.UpdatedAt = clock.GetUtcNow();
            return RecipeModerationActionOutcome.Applied;
        }, cancellationToken);

    private static RecipeModerationContent ReadContent(RecipePublicationRevision revision) =>
        JsonSerializer.Deserialize<RecipeModerationContent>(revision.ContentJson)
        ?? throw new InvalidOperationException("A recipe publication is missing its moderated content.");

    private static RecipeModerationItem ToItem(RecipePublicationRevision revision) {
        RecipeModerationContent content = ReadContent(revision);
        return new(revision.Id, revision.RecipeId.Value, revision.OwnerId,
            RecipePublicationStatuses.ForDisplay(revision.Status), content.Text, revision.CreatedAt, revision.UpdatedAt,
            revision.PolicyVersionId, revision.Reason, revision.PrimaryResult, revision.ShadowResult, revision.Attempts,
            [.. content.Images.Where(image => image.AssetId.HasValue)
                .Select(image => new RecipeModerationPreviewImage(image.AssetId!.Value, image.Field))]);
    }
}
