namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed record GetImageUploadUrlResult(
    string UploadUrl,
    string FileUrl,
    DateTime ExpiresAtUtc,
    Guid AssetId);
