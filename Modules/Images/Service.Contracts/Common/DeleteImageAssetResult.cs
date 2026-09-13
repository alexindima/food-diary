namespace FoodDiary.Application.Abstractions.Images.Common;

public sealed record DeleteImageAssetResult(bool Deleted, string? ErrorCode = null);
