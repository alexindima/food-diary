using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Ai.PersistenceModel;

internal sealed class FoodRecognitionJobImage {
    public Guid JobId { get; set; }
    public ImageAssetId ImageAssetId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int Position { get; set; }
}
