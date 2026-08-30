namespace FoodDiary.Application.Abstractions.Images.Common;

public sealed record PresignedUpload(
    string UploadUrl,
    string FileUrl,
    string ObjectKey,
    DateTime ExpirationUtc);
