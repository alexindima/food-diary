using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed record DeleteImageAssetCommand(Guid UserId, ImageAssetId AssetId) : IRequest<Result>;
