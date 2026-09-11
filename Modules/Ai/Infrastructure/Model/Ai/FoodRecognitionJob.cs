using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Ai;

internal sealed class FoodRecognitionJob {
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
