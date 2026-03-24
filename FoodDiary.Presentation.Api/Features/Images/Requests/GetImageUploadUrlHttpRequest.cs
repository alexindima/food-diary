namespace FoodDiary.Presentation.Api.Features.Images.Requests;

public sealed record GetImageUploadUrlHttpRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes);
