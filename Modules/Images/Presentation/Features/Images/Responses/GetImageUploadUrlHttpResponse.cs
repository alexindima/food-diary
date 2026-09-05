namespace FoodDiary.Presentation.Api.Features.Images.Responses;

public sealed record GetImageUploadUrlHttpResponse(
    string UploadUrl,
    string FileUrl,
    DateTime ExpiresAtUtc,
    Guid AssetId);
