namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public sealed record DeleteImageAssetResult(bool Deleted, string? ErrorCode = null);
