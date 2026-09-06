using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Models;

public sealed record ImageAssetReadModel(ImageAssetId Id, string Url);
