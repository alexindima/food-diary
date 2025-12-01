using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities;
using MediatR;

namespace FoodDiary.Application.Images.Commands.GetUploadUrl;

public sealed class GetImageUploadUrlCommandHandler(
    IImageStorageService imageStorageService,
    IImageAssetRepository imageAssetRepository) : IRequestHandler<GetImageUploadUrlCommand, GetImageUploadUrlResult>
{
    public async Task<GetImageUploadUrlResult> Handle(GetImageUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var presign = await imageStorageService.CreatePresignedUploadAsync(
            request.UserId,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            cancellationToken);

        var asset = ImageAsset.Create(request.UserId, presign.ObjectKey, presign.FileUrl);
        asset = await imageAssetRepository.AddAsync(asset, cancellationToken);

        return new GetImageUploadUrlResult(
            presign.UploadUrl,
            presign.FileUrl,
            presign.ObjectKey,
            presign.ExpirationUtc,
            asset.Id);
    }
}
