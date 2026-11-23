using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Entities;

public sealed class RecipePublicationRevision {
    public Guid Id { get; set; }
    public RecipeId RecipeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PolicyVersionId { get; set; }
    public string ChangeJson { get; set; } = string.Empty;
    public string ContentJson { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public int Attempts { get; set; }
    public int PromotionAttempts { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public string? PrimaryResult { get; set; }
    public string? ShadowResult { get; set; }
    public DateTimeOffset? ShadowNextAttemptAt { get; set; }
    public int ShadowAttempts { get; set; }
    public Guid? ShadowLeaseToken { get; set; }
    public DateTimeOffset? ShadowLeaseUntil { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? Reason { get; set; }
    private readonly List<RecipePublicationImageReference> _images = [];
    public IReadOnlyCollection<RecipePublicationImageReference> Images => _images;

    public void AddImage(Guid assetId) => _images.Add(new RecipePublicationImageReference {
        RevisionId = Id,
        AssetId = (ImageAssetId)assetId,
    });
}
