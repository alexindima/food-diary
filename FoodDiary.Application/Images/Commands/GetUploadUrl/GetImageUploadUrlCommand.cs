using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed record GetImageUploadUrlCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSizeBytes) : IRequest<Result<GetImageUploadUrlResult>>;

public sealed record GetImageUploadUrlResult(
    string UploadUrl,
    string FileUrl,
    string ObjectKey,
    DateTime ExpiresAtUtc,
    ImageAssetId AssetId);
