namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public sealed record ImageObjectValidationResult(bool IsValid, string? ErrorCode = null, string? Message = null);
