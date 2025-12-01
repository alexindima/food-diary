namespace FoodDiary.Contracts.Images;

public sealed record GetImageUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes);
