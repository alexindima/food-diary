namespace FoodDiary.Modules.Images.Presentation.Responses;

public sealed record GetImageUploadUrlHttpResponse(
    string UploadUrl,
    string FileUrl,
    DateTime ExpiresAtUtc,
    Guid AssetId);
