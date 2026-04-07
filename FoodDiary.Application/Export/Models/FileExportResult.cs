namespace FoodDiary.Application.Export.Models;

public sealed record FileExportResult(
    byte[] Content,
    string ContentType,
    string FileName);
