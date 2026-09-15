namespace FoodDiary.Modules.Images.Presentation.Requests;

public sealed record GetImageUploadUrlHttpRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes);
