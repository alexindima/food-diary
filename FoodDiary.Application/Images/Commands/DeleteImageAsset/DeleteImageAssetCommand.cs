using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed record DeleteImageAssetCommand(UserId UserId, ImageAssetId AssetId) : IRequest<DeleteImageAssetResult>;
