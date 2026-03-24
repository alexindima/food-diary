namespace FoodDiary.Presentation.Api.Features.Images.Responses;

public sealed record GetImageUploadUrlHttpResponse(
    string UploadUrl,
    string FileUrl,
    string ObjectKey,
    DateTime ExpiresAtUtc,
    Guid AssetId);
