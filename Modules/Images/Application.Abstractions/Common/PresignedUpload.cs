using FoodDiary.Modules.Images.Domain.ValueObjects;
namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public sealed record PresignedUpload(
    SignedImageUploadUrl UploadUrl,
    PublicImageUrl FileUrl,
    ObjectStorageKey ObjectKey,
    DateTime ExpirationUtc);
