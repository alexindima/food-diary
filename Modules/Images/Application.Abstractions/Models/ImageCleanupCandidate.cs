using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Application.Abstractions.Models;

public sealed record ImageCleanupCandidate(ImageAssetId Id, DateTime CreatedOnUtc);
