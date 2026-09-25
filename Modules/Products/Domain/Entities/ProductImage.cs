using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
namespace FoodDiary.Modules.Products.Domain.Entities;

public sealed class ProductImage {
    public ImageAssetId ImageAssetId { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public int Position { get; private set; }
    private ProductImage() { }
    public ProductImage(ImageAssetId imageAssetId, string imageUrl, int position) {
        if (imageAssetId.Value == Guid.Empty) { throw new ArgumentException("Image id is required.", nameof(imageAssetId)); }
        if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.Length > Product.ImageUrlMaxLength) { throw new ArgumentException("Invalid image URL.", nameof(imageUrl)); }
        ImageAssetId = imageAssetId;
        ImageUrl = imageUrl;
        Position = position;
    }
    internal void SetPosition(int position) => Position = position;
}
