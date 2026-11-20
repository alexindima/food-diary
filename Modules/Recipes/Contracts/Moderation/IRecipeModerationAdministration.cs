namespace FoodDiary.Modules.Recipes.Contracts.Moderation;

public interface IRecipeModerationAdministration {
    Task<RecipeModerationPage> ListAsync(string? status, int page, int limit, CancellationToken cancellationToken);
    Task<RecipeModerationItem?> GetAsync(Guid revisionId, CancellationToken cancellationToken);
    Task<string?> GetImageAsync(Guid revisionId, Guid assetId, CancellationToken cancellationToken);
    Task<RecipeModerationActionOutcome> ResolveAsync(Guid revisionId, Guid actorId, bool approve, string? reason, CancellationToken cancellationToken);
    Task<RecipeModerationActionOutcome> RetryAsync(Guid revisionId, Guid actorId, CancellationToken cancellationToken);
    Task<RecipeModerationPolicyView> GetPolicyAsync(CancellationToken cancellationToken);
    Task<RecipeModerationPolicyView> PublishPolicyAsync(Guid actorId, RecipeModerationPolicyInput input, CancellationToken cancellationToken);
    Task<RecipeModerationPolicyView?> RestorePolicyAsync(Guid versionId, Guid actorId, CancellationToken cancellationToken);
}
