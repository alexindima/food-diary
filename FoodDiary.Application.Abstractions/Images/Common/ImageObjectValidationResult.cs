namespace FoodDiary.Application.Abstractions.Images.Common;

public sealed record ImageObjectValidationResult(bool IsValid, string? ErrorCode = null, string? Message = null);
