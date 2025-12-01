using FoodDiary.Domain.ValueObjects;
using MediatR;

namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed record GetImageUploadUrlCommand(
    UserId UserId,
    string FileName,
    string ContentType,
    long FileSizeBytes) : IRequest<GetImageUploadUrlResult>;

public sealed record GetImageUploadUrlResult(
    string UploadUrl,
    string FileUrl,
    string ObjectKey,
    DateTime ExpiresAtUtc,
    ImageAssetId AssetId);
