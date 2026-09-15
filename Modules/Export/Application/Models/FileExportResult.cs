namespace FoodDiary.Modules.Export.Application.Models;

public sealed record FileExportResult(
    byte[] Content,
    string ContentType,
    string FileName);
