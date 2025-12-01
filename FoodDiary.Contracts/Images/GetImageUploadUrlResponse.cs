namespace FoodDiary.Contracts.Images;

public sealed record GetImageUploadUrlResponse(
    string UploadUrl,
    string FileUrl,
    string ObjectKey,
    DateTime ExpiresAtUtc,
    Guid AssetId);
