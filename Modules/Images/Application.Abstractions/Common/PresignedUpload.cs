namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public sealed record PresignedUpload(
    string UploadUrl,
    string FileUrl,
    string ObjectKey,
    DateTime ExpirationUtc);
