using System.Text.Json;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Infrastructure.Persistence;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

internal sealed class RecipePublicationStore(
    RecipesDbContext context,
    IModuleTransactionCoordinator transactions,
    IConfiguration configuration,
    TimeProvider clock) : IRecipePublicationQueue {
    internal DbSet<RecipePublicationRevision> Revisions => context.Set<RecipePublicationRevision>();
    internal DbSet<RecipePublicationPolicy> Policies => context.Set<RecipePublicationPolicy>();
    internal RecipesDbContext Context => context;

    public bool Enabled => configuration["RecipeModeration:Enabled"] is "true" or "True" or "1";
    internal bool MistralAvailable => !string.IsNullOrWhiteSpace(configuration["Mistral:ApiKey"]);

    public async Task<RecipePublicationState> StageAsync(
        RecipeId recipeId, UserId ownerId, string changeJson, RecipeModerationContent content, CancellationToken cancellationToken) {
        DateTimeOffset now = clock.GetUtcNow();
        await SupersedeAsync(recipeId, now, cancellationToken);
        RecipeModerationPolicyVersion policy = await GetActivePolicyAsync(cancellationToken);
        var revisionId = Guid.NewGuid();
        bool observe = MistralAvailable && policy.Rules.MistralSamplePercent > 0 &&
            !string.IsNullOrWhiteSpace(content.Text) && (uint)revisionId.GetHashCode() % 100 < policy.Rules.MistralSamplePercent;
        var revision = new RecipePublicationRevision {
            Id = revisionId,
            RecipeId = recipeId,
            OwnerId = ownerId.Value,
            PolicyVersionId = policy.Id,
            ChangeJson = changeJson,
            ContentJson = JsonSerializer.Serialize(content with {
                OwnerId = ownerId.Value,
                Images = [.. content.Images.Select(image => image with { Url = string.Empty })],
            }),
            Status = RecipePublicationStatuses.Pending,
            IsCurrent = true,
            CreatedAt = now,
            NextAttemptAt = now,
            ShadowNextAttemptAt = observe ? now : null,
        };
        foreach (Guid assetId in content.Images.Select(image => image.AssetId).OfType<Guid>().Distinct()) {
            revision.AddImage(assetId);
        }

        Revisions.Add(revision);
        return ToPublicationState(revision);
    }

    public async Task<RecipePublicationState?> GetCurrentAsync(RecipeId recipeId, CancellationToken cancellationToken) {
        RecipePublicationRevision? current = await Revisions.AsNoTracking()
            .SingleOrDefaultAsync(revision => revision.RecipeId == recipeId && revision.IsCurrent, cancellationToken);
        return current is null ? null : ToPublicationState(current);
    }

    public async Task<IReadOnlyDictionary<RecipeId, RecipePublicationState>> GetCurrentAsync(
        IReadOnlyCollection<RecipeId> recipeIds, CancellationToken cancellationToken) {
        if (recipeIds.Count == 0) {
            return new Dictionary<RecipeId, RecipePublicationState>();
        }

        var states = await Revisions.AsNoTracking().Where(revision => revision.IsCurrent && recipeIds.Contains(revision.RecipeId))
            .Select(revision => new { revision.RecipeId, revision.Id, revision.Status, revision.Reason }).ToListAsync(cancellationToken);
        return states.ToDictionary(revision => revision.RecipeId, revision => new RecipePublicationState(
            revision.Id, RecipePublicationStatuses.ForDisplay(revision.Status), revision.Reason));
    }

    public async Task CancelAsync(RecipeId recipeId, CancellationToken cancellationToken) {
        await SupersedeAsync(recipeId, clock.GetUtcNow(), cancellationToken);
        await Revisions.Where(revision => revision.RecipeId == recipeId && revision.ShadowNextAttemptAt.HasValue)
            .ExecuteUpdateAsync(update => update
                .SetProperty(revision => revision.ShadowNextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(revision => revision.ShadowLeaseToken, (Guid?)null), cancellationToken);
    }

    public Task<bool> CanApplyAsync(RecipeId recipeId, RecipePublicationClaim claim, CancellationToken cancellationToken) =>
        ActiveApproval(recipeId, claim).AnyAsync(cancellationToken);

    public async Task<bool> CompleteAsync(RecipeId recipeId, RecipePublicationClaim claim, CancellationToken cancellationToken) {
        RecipePublicationRevision? revision = await ActiveApproval(recipeId, claim).SingleOrDefaultAsync(cancellationToken);
        if (revision is null) {
            return false;
        }

        revision.Status = RecipePublicationStatuses.Published;
        revision.IsCurrent = false;
        revision.UpdatedAt = clock.GetUtcNow();
        revision.NextAttemptAt = null;
        revision.LeaseToken = null;
        revision.LeaseUntil = null;
        if (string.Equals(revision.Reason, RecipePublicationStatuses.PhotoPromotionUnavailable, StringComparison.Ordinal)) {
            revision.Reason = null;
        }
        return true;
    }

    private IQueryable<RecipePublicationRevision> ActiveApproval(RecipeId recipeId, RecipePublicationClaim claim) {
        DateTimeOffset now = clock.GetUtcNow();
        return Revisions.Where(revision => revision.Id == claim.RevisionId && revision.RecipeId == recipeId &&
            revision.IsCurrent && revision.Status == RecipePublicationStatuses.Approved &&
            revision.LeaseToken == claim.LeaseToken && revision.LeaseUntil > now);
    }

    internal async Task<RecipeModerationPolicyVersion> GetActivePolicyAsync(CancellationToken cancellationToken) {
        RecipePublicationPolicy? policy = await Policies.AsNoTracking().OrderByDescending(item => item.PublishedAt)
            .ThenByDescending(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        return policy is null ? RecipeModerationPolicyRules.Default : ToPolicyVersion(policy);
    }

    internal async Task<RecipeModerationPolicyInput> GetRulesAsync(Guid id, CancellationToken cancellationToken) {
        if (id == RecipeModerationPolicyRules.DefaultVersionId) {
            return RecipeModerationPolicyRules.Default.Rules;
        }

        string json = await Policies.AsNoTracking().Where(policy => policy.Id == id)
            .Select(policy => policy.RulesJson).SingleAsync(cancellationToken);
        return RecipeModerationPolicyRules.Deserialize(json);
    }

    internal Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        transactions.ExecuteSerializableAsync(operation, cancellationToken);

    private Task<int> SupersedeAsync(RecipeId id, DateTimeOffset now, CancellationToken cancellationToken) =>
        Revisions.Where(revision => revision.RecipeId == id && revision.IsCurrent)
            .ExecuteUpdateAsync(update => update
                .SetProperty(revision => revision.IsCurrent, valueExpression: false)
                .SetProperty(revision => revision.Status, RecipePublicationStatuses.Superseded)
                .SetProperty(revision => revision.UpdatedAt, now)
                .SetProperty(revision => revision.NextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(revision => revision.ShadowNextAttemptAt, (DateTimeOffset?)null)
                .SetProperty(revision => revision.LeaseToken, (Guid?)null)
                .SetProperty(revision => revision.ShadowLeaseToken, (Guid?)null), cancellationToken);

    internal static RecipeModerationPolicyVersion ToPolicyVersion(RecipePublicationPolicy policy) =>
        new(policy.Id, policy.PublishedAt, policy.PublishedBy, RecipeModerationPolicyRules.Deserialize(policy.RulesJson));

    private static RecipePublicationState ToPublicationState(RecipePublicationRevision revision) =>
        new(revision.Id, RecipePublicationStatuses.ForDisplay(revision.Status), revision.Reason, revision.ChangeJson);
}
