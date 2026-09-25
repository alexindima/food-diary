using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Ai.PersistenceModel;

internal sealed class FoodRecognitionJob {
    public bool IsProductLabel { get; set; }
    public ICollection<FoodRecognitionJobImage> AdditionalImages { get; set; } = [];
    public Guid Id { get; set; }
    public UserId UserId { get; set; }
    public ImageAssetId ImageAssetId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Queued";
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }
    public string? VisionJson { get; set; }
    public string? NutritionJson { get; set; }
    public string? ErrorCode { get; set; }
    public string? NutritionErrorCode { get; set; }
}
